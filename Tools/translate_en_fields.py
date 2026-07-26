# -*- coding: utf-8 -*-
"""
Translate empty *En mathematician fields from Russian (*Ru) counterparts.

Uses local OPUS-MT (CTranslate2) — offline, no external APIs, no rate limits.
Resume cache: Tools/_en_translate_cache.json
"""
from __future__ import annotations

import argparse
import hashlib
import json
import re
import sys
import time
import traceback
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(Path(__file__).resolve().parent))

from _inventory_en_fields import (  # noqa: E402
    ASSETS,
    FIELDS,
    extract_field,
    find_field_span,
    is_empty,
)

CACHE_PATH = Path(__file__).resolve().parent / "_en_translate_cache.json"
REPORT_PATH = Path(__file__).resolve().parent / "_en_translate_report.json"

# Local Opus-MT via CTranslate2 (offline, no rate limits).
MAX_CHUNK = 450  # sentence-ish pieces; opus-mt is sentence-oriented
REQUEST_SLEEP_S = 0.0
ASSET_SLEEP_S = 0.0
MAX_RETRIES = 1
MODEL_DIR = Path(__file__).resolve().parent / "models" / "opus-mt-ru-en-ct2"
HF_TOKENIZER = "Helsinki-NLP/opus-mt-ru-en"

# Standard English forms for missing fullNameEn (well-known conventional names).
FULL_NAME_EN: dict[str, str] = {
    "abel": "Niels Henrik Abel",
    "al_khwarizmi": "Muhammad ibn Musa al-Khwarizmi",
    "apollonius": "Apollonius of Perga",
    "arnold": "Vladimir Igorovich Arnold",
    "artin": "Emil Artin",
    "atiyah": "Michael Atiyah",
    "banach": "Stefan Banach",
    "bernoulli_daniel": "Daniel Bernoulli",
    "bernoulli_jakob": "Jacob Bernoulli",
    "bhaskara": "Bhaskara II",
    "bolzano": "Bernard Bolzano",
    "boole": "George Boole",
    "brahmagupta": "Brahmagupta",
}


def split_chunks(text: str, max_len: int = MAX_CHUNK) -> list[tuple[str, str]]:
    """Split into chunks preferring paragraph/sentence boundaries."""
    if len(text) <= max_len:
        return [("", text)]

    parts: list[str] = []
    buf = text
    while buf:
        if len(buf) <= max_len:
            parts.append(buf)
            break
        window = buf[:max_len]
        cut = -1
        for sep in ("\n\n", "\n", ". ", "! ", "? ", "; ", ", ", " "):
            idx = window.rfind(sep)
            if idx >= max_len // 5:
                cut = idx + len(sep)
                break
        if cut < 0:
            cut = max_len
        parts.append(buf[:cut])
        buf = buf[cut:]

    return [("", p) for p in parts]


_translator = None
_tokenizer = None


def get_local_mt():
    global _translator, _tokenizer
    if _translator is None:
        import ctranslate2
        from transformers import AutoTokenizer

        if not MODEL_DIR.exists():
            raise RuntimeError(f"Missing local model at {MODEL_DIR}")
        _translator = ctranslate2.Translator(str(MODEL_DIR), device="cpu")
        _tokenizer = AutoTokenizer.from_pretrained(HF_TOKENIZER)
    return _translator, _tokenizer


def translate_chunk_once(chunk: str) -> str:
    """Offline Opus-MT translation for one chunk."""
    translator, tok = get_local_mt()
    # Skip pure non-Cyrillic (emoji headers, markdown tables of numbers, etc.)
    cyr = sum(1 for c in chunk if "\u0400" <= c <= "\u04FF")
    if cyr == 0:
        return chunk
    tokens = tok.convert_ids_to_tokens(tok.encode(chunk))
    # Guard against extremely long token sequences
    if len(tokens) > 480:
        tokens = tokens[:480]
    results = translator.translate_batch([tokens])
    hyp = results[0].hypotheses[0]
    return tok.decode(tok.convert_tokens_to_ids(hyp), skip_special_tokens=True)


def translate_text(ru: str) -> str:
    if not ru or not ru.strip():
        return ""
    cyr = sum(1 for c in ru if "\u0400" <= c <= "\u04FF")
    if cyr == 0:
        return ru

    # Translate paragraph-by-paragraph to preserve blank lines / structure.
    paragraphs = ru.split("\n")
    out_lines: list[str] = []
    for line in paragraphs:
        if not line.strip():
            out_lines.append(line)
            continue
        cyr_l = sum(1 for c in line if "\u0400" <= c <= "\u04FF")
        if cyr_l == 0:
            out_lines.append(line)
            continue
        pieces: list[str] = []
        for _, chunk in split_chunks(line, MAX_CHUNK):
            pieces.append(translate_chunk_once(chunk))
        out_lines.append("".join(pieces))
    return "\n".join(out_lines)


def unwrap_yaml_softwraps(s: str) -> str:
    """Join Unity YAML continuation wraps; keep blank lines and single newlines."""
    s = s.replace("\r\n", "\n").replace("\r", "\n")
    # Continuation lines in serialized assets are "\\n" + 2+ spaces of indent.
    s = re.sub(r"\n[ \t]{2,}", " ", s)
    s = re.sub(r"[ \t]{2,}", " ", s)
    s = re.sub(r" *\n *", "\n", s)
    s = re.sub(r"\n{3,}", "\n\n", s)
    return s.strip()


def preprocess_ru(s: str) -> str:
    """Normalize common RU abbreviations so MT does not mangle them."""
    s = unwrap_yaml_softwraps(s)
    # Circa / eras — protect before MT
    replacements = [
        (r"\bдо\s*н\.?\s*э\.?", "BCE"),
        (r"\bн\.?\s*э\.?", "CE"),
        (r"\bок\.\s*", "c. "),
        (r"\bок\s+", "c. "),
    ]
    for pat, rep in replacements:
        s = re.sub(pat, rep, s, flags=re.IGNORECASE)
    return s


def encode_unity_quoted(s: str) -> str:
    """UTF-8 double-quoted YAML scalar; escape \\ \" and newlines only (single physical line)."""
    parts: list[str] = ['"']
    for ch in s:
        if ch == "\\":
            parts.append("\\\\")
        elif ch == '"':
            parts.append('\\"')
        elif ch == "\n":
            parts.append("\\n")
        elif ch == "\r":
            continue
        elif ch == "\t":
            parts.append("\\t")
        else:
            parts.append(ch)
    parts.append('"')
    return "".join(parts)


def set_field(text: str, name: str, value: str) -> str:
    span = find_field_span(text, name)
    if span is None:
        raise KeyError(name)
    vs, ve, _ = span
    if value == "":
        encoded = ""
    elif (
        all(ord(c) < 128 and c not in "\n\r\"" for c in value)
        and "\\" not in value
        and value == value.strip()
        and ":" not in value
        and "#" not in value
    ):
        encoded = value
    else:
        encoded = encode_unity_quoted(value)
    return text[:vs] + encoded + text[ve:]


def load_cache() -> dict:
    if CACHE_PATH.exists():
        return json.loads(CACHE_PATH.read_text(encoding="utf-8"))
    return {"translations": {}, "written": {}}


def save_cache(cache: dict) -> None:
    CACHE_PATH.write_text(json.dumps(cache, ensure_ascii=False, indent=0), encoding="utf-8")


def cache_key(asset_id: str, field: str, ru: str) -> str:
    h = hashlib.sha256(ru.encode("utf-8")).hexdigest()[:16]
    return f"{asset_id}|{field}|{h}"


def translate_field(cache: dict, asset_id: str, field: str, ru: str) -> str:
    key = cache_key(asset_id, field, ru)
    hit = cache["translations"].get(key)
    if hit is not None:
        return hit
    en = translate_text(ru)
    cache["translations"][key] = en
    save_cache(cache)
    return en


def process_asset(
    path: Path,
    fields: list[str],
    cache: dict,
    dry_run: bool,
    force: bool = False,
) -> dict:
    asset_id = path.stem
    text = path.read_text(encoding="utf-8")
    updated_fields: list[str] = []
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
        if not is_empty(en) and not force:
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
            updated_fields.append(f)
            print(f"  + {f} ({len(new_en)} chars)", flush=True)
        except Exception as ex:  # noqa: BLE001
            errors.append(f"{f}:{ex}")
            print(f"  ERROR {asset_id}.{f}: {ex}", flush=True)

    if updated_fields and not dry_run:
        path.write_text(text, encoding="utf-8", newline="\n")
        cache.setdefault("written", {})[asset_id] = {
            "fields": updated_fields,
            "ts": time.time(),
        }
        save_cache(cache)

    return {
        "id": asset_id,
        "updated": updated_fields,
        "skipped": skipped,
        "errors": errors,
    }


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument(
        "--only",
        default="fullName,interestingFacts,achievements,shortBio,personalLife",
        help="Comma-separated base field names (without Ru/En)",
    )
    ap.add_argument("--ids", default="", help="Comma-separated asset ids to process")
    ap.add_argument("--dry-run", action="store_true")
    ap.add_argument("--force", action="store_true", help="Overwrite existing En fields")
    ap.add_argument("--limit", type=int, default=0, help="Max assets to update")
    args = ap.parse_args()

    fields = [f.strip() for f in args.only.split(",") if f.strip()]
    for f in fields:
        if f not in FIELDS:
            print(f"Unknown field: {f}", file=sys.stderr)
            return 2

    id_filter = {x.strip() for x in args.ids.split(",") if x.strip()} or None
    cache = load_cache()
    results = []
    updated_assets = 0

    paths = sorted(ASSETS.glob("*.asset"))
    for path in paths:
        if id_filter and path.stem not in id_filter:
            continue
        print(f"Processing {path.stem} ...", flush=True)
        res = process_asset(path, fields, cache, args.dry_run, force=args.force)
        results.append(res)
        if res["updated"]:
            updated_assets += 1
            print(f"  updated: {', '.join(res['updated'])}", flush=True)
        if res["errors"]:
            print(f"  errors: {res['errors']}", flush=True)
        if res["updated"] or res["errors"]:
            time.sleep(ASSET_SLEEP_S)
        if args.limit and updated_assets >= args.limit:
            break

    report = {
        "updated_assets": updated_assets,
        "results": results,
        "dry_run": args.dry_run,
        "fields": fields,
    }
    REPORT_PATH.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"Done. Updated assets: {updated_assets}. Report: {REPORT_PATH}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
