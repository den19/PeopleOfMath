# -*- coding: utf-8 -*-
"""
Fill empty *En fields by translating RU locally (OPUS-MT via CTranslate2).

No external MT APIs — no rate limits. Resume-friendly cache in _en_translate_cache.json.

Usage:
  python Tools/translate_en_local.py
  python Tools/translate_en_local.py --ids euler,gauss
  python Tools/translate_en_local.py --only interestingFacts,shortBio
"""
from __future__ import annotations

import argparse
import hashlib
import json
import re
import sys
import time
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(Path(__file__).resolve().parent))

from _inventory_en_fields import ASSETS, FIELDS, extract_field, is_empty, set_field  # noqa: E402
from translate_en_fields import (  # noqa: E402
    CACHE_PATH,
    FULL_NAME_EN,
    preprocess_ru,
    save_cache,
    load_cache,
    unwrap_yaml_softwraps,
)

MODEL_DIR = Path(__file__).resolve().parent / "models" / "opus-mt-ru-en-ct2"
TOKENIZER_NAME = "Helsinki-NLP/opus-mt-ru-en"
MAX_CHARS = 4500  # per translate call; OPUS-MT handles ~512 tokens
BATCH_LINES = 8

_translator = None
_tokenizer = None


def get_engine():
    global _translator, _tokenizer
    if _translator is None:
        import ctranslate2
        from transformers import AutoTokenizer

        if not MODEL_DIR.is_dir():
            raise SystemExit(f"Local model missing: {MODEL_DIR}")
        _translator = ctranslate2.Translator(str(MODEL_DIR), device="cpu")
        _tokenizer = AutoTokenizer.from_pretrained(TOKENIZER_NAME)
    return _translator, _tokenizer


def split_paragraphs(text: str, max_len: int = MAX_CHARS) -> list[str]:
    text = unwrap_yaml_softwraps(text)
    if len(text) <= max_len:
        return [text] if text.strip() else []
    parts: list[str] = []
    buf = text
    while buf:
        if len(buf) <= max_len:
            parts.append(buf)
            break
        window = buf[:max_len]
        cut = -1
        for sep in ("\n\n", "\n", ". ", "! ", "? "):
            idx = window.rfind(sep)
            if idx >= max_len // 4:
                cut = idx + len(sep)
                break
        if cut < 0:
            cut = max_len
        parts.append(buf[:cut])
        buf = buf[cut:]
    return parts


def translate_ru_local(text: str) -> str:
    if not text or not text.strip():
        return ""
    cyr = sum(1 for c in text if "\u0400" <= c <= "\u04FF")
    if cyr == 0:
        return text

    translator, tokenizer = get_engine()
    chunks = split_paragraphs(text)
    out: list[str] = [""] * len(chunks)
    batch: list[str] = []
    batch_idx: list[int] = []

    def flush_batch() -> None:
        nonlocal batch, batch_idx
        if not batch:
            return
        tokenized = [
            tokenizer.convert_ids_to_tokens(tokenizer.encode(s))
            for s in batch
        ]
        results = translator.translate_batch(tokenized)
        for i, res in enumerate(results):
            hyp = res.hypotheses[0]
            en = tokenizer.decode(
                tokenizer.convert_tokens_to_ids(hyp),
                skip_special_tokens=True,
            )
            out[batch_idx[i]] = en
        batch = []
        batch_idx = []

    for i, chunk in enumerate(chunks):
        cyr_c = sum(1 for c in chunk if "\u0400" <= c <= "\u04FF")
        if cyr_c == 0:
            out[i] = chunk
            continue
        batch.append(chunk)
        batch_idx.append(i)
        if len(batch) >= BATCH_LINES:
            flush_batch()
    flush_batch()
    return "".join(out)


def cache_key(asset_id: str, field: str, ru: str) -> str:
    h = hashlib.sha256(ru.encode("utf-8")).hexdigest()[:16]
    return f"{asset_id}|{field}|{h}"


def translate_field(cache: dict, asset_id: str, field: str, ru: str) -> str:
    key = cache_key(asset_id, field, ru)
    hit = cache["translations"].get(key)
    if hit is not None:
        return hit
    en = translate_ru_local(ru)
    cache["translations"][key] = en
    save_cache(cache)
    return en


def process_asset(path: Path, fields: list[str], cache: dict, dry_run: bool) -> dict:
    asset_id = path.stem
    text = path.read_text(encoding="utf-8")
    updated: list[str] = []
    skipped: list[str] = []
    errors: list[str] = []

    for f in fields:
        ru_name = f + "Ru"
        en_name = f + "En"
        ru = extract_field(text, ru_name)
        en = extract_field(text, en_name)
        if is_empty(ru):
            skipped.append(f"{f}:empty_ru")
            continue
        if not is_empty(en):
            skipped.append(f"{f}:already_en")
            continue

        try:
            if f == "fullName" and asset_id in FULL_NAME_EN:
                new_en = FULL_NAME_EN[asset_id]
            elif f == "fullName":
                cleaned = re.sub(r"\s+", " ", preprocess_ru(ru)).strip()
                new_en = translate_field(cache, asset_id, f, cleaned)
            else:
                prepared = preprocess_ru(ru)
                new_en = translate_field(cache, asset_id, f, prepared)

            if not new_en.strip():
                errors.append(f"{f}:empty_translation")
                continue

            if not dry_run:
                text = set_field(text, en_name, new_en)
            updated.append(f)
            print(f"  + {f} ({len(new_en)} chars)", flush=True)
        except Exception as ex:  # noqa: BLE001
            errors.append(f"{f}:{ex}")
            print(f"  ERROR {asset_id}.{f}: {ex}", flush=True)

    if updated and not dry_run:
        path.write_text(text, encoding="utf-8", newline="\n")
        cache.setdefault("written", {})[asset_id] = {
            "fields": updated,
            "ts": time.time(),
            "source": "local_opus_mt",
        }
        save_cache(cache)

    return {"id": asset_id, "updated": updated, "skipped": skipped, "errors": errors}


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument(
        "--only",
        default="fullName,interestingFacts,achievements,shortBio,personalLife",
    )
    ap.add_argument("--ids", default="")
    ap.add_argument("--dry-run", action="store_true")
    ap.add_argument("--limit", type=int, default=0)
    args = ap.parse_args()

    fields = [f.strip() for f in args.only.split(",") if f.strip()]
    for f in fields:
        if f not in FIELDS:
            print(f"Unknown field: {f}", file=sys.stderr)
            return 2

    id_filter = {x.strip() for x in args.ids.split(",") if x.strip()} or None
    cache = load_cache()
    updated_assets = 0

    print("Local OPUS-MT (no external APIs). Loading model ...", flush=True)
    get_engine()

    paths = sorted(ASSETS.glob("*.asset"))
    for path in paths:
        if id_filter and path.stem not in id_filter:
            continue
        print(f"Processing {path.stem} ...", flush=True)
        res = process_asset(path, fields, cache, args.dry_run)
        if res["updated"]:
            updated_assets += 1
            print(f"  updated: {', '.join(res['updated'])}", flush=True)
        if res["errors"]:
            print(f"  errors: {res['errors']}", flush=True)
        if args.limit and updated_assets >= args.limit:
            break

    print(f"Done. Updated assets: {updated_assets}.", flush=True)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
