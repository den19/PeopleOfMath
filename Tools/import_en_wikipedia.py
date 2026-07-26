# -*- coding: utf-8 -*-
"""
Fast fill of empty *En fields from English Wikipedia via Wikidata sitelinks.

Much faster than machine-translating long RU texts. Only writes fields that are
currently empty; does not overwrite existing EN content.

Usage:
  python Tools/import_en_wikipedia.py
  python Tools/import_en_wikipedia.py --ids euler,gauss
  python Tools/import_en_wikipedia.py --force-interesting-facts
"""
from __future__ import annotations

import argparse
import json
import re
import sys
import time
import urllib.error
import urllib.parse
import urllib.request
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(Path(__file__).resolve().parent))

from _inventory_en_fields import (  # noqa: E402
    ASSETS,
    extract_field,
    is_empty,
    set_field,
)

USER_AGENT = "PeopleOfMath/1.0 (offline encyclopedia; contact: local dev)"
MAX_SHORT_BIO = 350
MAX_BLOCK = 9200
MAX_FACTS = 6000
REQUEST_TIMEOUT = 30
MIN_INTERVAL_S = 1.2
_last_request = 0.0

ACHIEVEMENT_MARKERS_EN = (
    "Scientific career",
    "Scientific work",
    "Research",
    "Achievements",
    "Contributions",
    "Major works",
    "Works",
    "Career",
    "Mathematics",
    "Legacy",
)

PERSONAL_MARKERS_EN = (
    "Personal life",
    "Biography",
    "Family",
    "Private life",
    "Early life",
    "Death",
)

FACT_MARKERS_EN = (
    "Legacy",
    "In popular culture",
    "Anecdotes",
    "Trivia",
    "Honours",
    "Honors",
    "Recognition",
    "Miscellaneous",
)


class RateLimitError(RuntimeError):
    """HTTP 429/503 — stop the run instead of hammering the API."""


def is_rate_limited(exc: BaseException) -> bool:
    if isinstance(exc, urllib.error.HTTPError):
        return exc.code in (429, 503)
    msg = str(exc).lower()
    return "429" in msg or "too many request" in msg or "too_many_requests" in msg


def http_get(url: str, retries: int = 6) -> str:
    import random
    from email.utils import parsedate_to_datetime

    req = urllib.request.Request(url, headers={"User-Agent": USER_AGENT})
    last: Exception | None = None
    for attempt in range(retries):
        _throttle()
        try:
            with urllib.request.urlopen(req, timeout=REQUEST_TIMEOUT) as resp:
                _mark_request()
                return resp.read().decode("utf-8", errors="replace")
        except urllib.error.HTTPError as ex:
            last = ex
            if not is_rate_limited(ex):
                raise
            retry_after = ex.headers.get("Retry-After", "")
            wait = 30.0
            if retry_after.strip().isdigit():
                wait = float(min(int(retry_after.strip()), 120))
            else:
                wait = min(120.0, 15.0 * (2**attempt) + random.uniform(0, 3))
            print(f"RATE LIMIT {ex.code} — waiting {wait:.0f}s ({attempt + 1}/{retries})", flush=True)
            time.sleep(wait)
            if attempt >= retries - 1:
                raise RateLimitError(f"Stopped after rate limit: {url}") from ex
        except (urllib.error.URLError, TimeoutError) as ex:
            last = ex
            time.sleep(2.0 + attempt)
    raise RuntimeError(f"GET failed {url}: {last}") from last


def batch_en_titles(wikidata_ids: list[str]) -> dict[str, str]:
    """wikidataId -> enwiki page title (underscores)."""
    out: dict[str, str] = {}
    chunk_size = 50
    for i in range(0, len(wikidata_ids), chunk_size):
        chunk = wikidata_ids[i : i + chunk_size]
        ids = "|".join(chunk)
        url = (
            "https://www.wikidata.org/w/api.php?"
            + urllib.parse.urlencode(
                {
                    "action": "wbgetentities",
                    "ids": ids,
                    "props": "sitelinks",
                    "sitefilter": "enwiki",
                    "format": "json",
                }
            )
        )
        data = json.loads(http_get(url))
        for qid, ent in (data.get("entities") or {}).items():
            title = (
                ent.get("sitelinks", {})
                .get("enwiki", {})
                .get("title")
            )
            if title:
                out[qid] = title.replace(" ", "_")
        time.sleep(MIN_INTERVAL_S)
    return out


def fetch_summary(title: str) -> dict | None:
    enc = urllib.parse.quote(title, safe="")
    url = f"https://en.wikipedia.org/api/rest_v1/page/summary/{enc}"
    try:
        return json.loads(http_get(url))
    except Exception:
        return None


def fetch_extract(title: str) -> str:
    q = urllib.parse.quote(title.replace("_", " "))
    url = (
        "https://en.wikipedia.org/w/api.php?"
        + urllib.parse.urlencode(
            {
                "action": "query",
                "prop": "extracts",
                "explaintext": "1",
                "format": "json",
                "titles": q,
            }
        )
    )
    raw = http_get(url)
    marker = '"extract":"'
    idx = raw.find(marker)
    if idx < 0:
        return ""
    start = idx + len(marker)
    out: list[str] = []
    i = start
    while i < len(raw):
        c = raw[i]
        if c == "\\" and i + 1 < len(raw):
            n = raw[i + 1]
            if n == "n":
                out.append("\n")
                i += 2
                continue
            if n == "t":
                out.append("\t")
                i += 2
                continue
            if n == '"':
                out.append('"')
                i += 2
                continue
            if n == "\\":
                out.append("\\")
                i += 2
                continue
        if c == '"':
            break
        out.append(c)
        i += 1
    return "".join(out).strip()


def truncate(s: str, n: int) -> str:
    s = s.strip()
    if len(s) <= n:
        return s
    cut = s[:n]
    sp = cut.rfind(" ")
    if sp > n // 2:
        cut = cut[:sp]
    return cut.rstrip() + "…"


def find_marker_split(text: str, personal_markers: tuple[str, ...]) -> int:
    best = -1
    for m in personal_markers:
        i = text.find(m)
        if i > 40 and (best < 0 or i < best):
            best = i
    return best


def split_extract_en(extract: str) -> tuple[str, str]:
    if not extract:
        return "", ""
    idx = find_marker_split(extract, PERSONAL_MARKERS_EN)
    if idx > 0:
        return extract[:idx].strip(), extract[idx:].strip()
    mid = len(extract) // 2
    split = extract.rfind("\n", 0, min(len(extract), mid + 200))
    if split < 100:
        split = mid
    return extract[:split].strip(), extract[split:].strip()


def extract_facts_section(extract: str, achievements: str) -> str:
    for m in FACT_MARKERS_EN:
        i = extract.find(m)
        if i >= 0:
            return truncate(extract[i:], MAX_FACTS)
    # Quiz prefers interesting facts; use lead + tail of achievements as fallback.
    if achievements:
        if len(achievements) <= MAX_FACTS:
            return achievements
        head = truncate(achievements[: MAX_FACTS // 2], MAX_FACTS // 2)
        tail_start = max(0, len(achievements) - MAX_FACTS // 2)
        tail = achievements[tail_start:].lstrip()
        return truncate(head + "\n\n" + tail, MAX_FACTS)
    return truncate(extract, MAX_FACTS)


def process_one(
    path: Path,
    en_title: str | None,
    force_facts: bool,
) -> dict:
    asset_id = path.stem
    result = {"id": asset_id, "updated": [], "skipped": [], "errors": []}
    if not en_title:
        result["errors"].append("no_enwiki")
        return result

    text = path.read_text(encoding="utf-8")
    need = {}
    for base in ("fullName", "shortBio", "achievements", "personalLife", "interestingFacts"):
        ru = extract_field(text, base + "Ru")
        en = extract_field(text, base + "En")
        if is_empty(ru):
            continue
        if base == "interestingFacts" and force_facts:
            if is_empty(en):
                need[base] = True
        elif is_empty(en):
            need[base] = True

    if not need:
        result["skipped"].append("all_filled")
        return result

    try:
        summary = fetch_summary(en_title)
        extract = fetch_extract(en_title)
    except Exception as ex:  # noqa: BLE001
        result["errors"].append(str(ex))
        return result

    achievements, personal = split_extract_en(extract)

    if "fullName" in need and summary and summary.get("title"):
        text = set_field(text, "fullNameEn", summary["title"].strip())
        result["updated"].append("fullName")

    if "shortBio" in need:
        bio = ""
        if summary and summary.get("extract"):
            bio = truncate(summary["extract"].strip(), MAX_SHORT_BIO)
        elif extract:
            bio = truncate(extract, MAX_SHORT_BIO)
        if bio:
            text = set_field(text, "shortBioEn", bio)
            result["updated"].append("shortBio")

    if "achievements" in need and achievements:
        text = set_field(text, "achievementsEn", truncate(achievements, MAX_BLOCK))
        result["updated"].append("achievements")

    if "personalLife" in need and personal:
        text = set_field(text, "personalLifeEn", truncate(personal, MAX_BLOCK))
        result["updated"].append("personalLife")

    if "interestingFacts" in need:
        facts = extract_facts_section(extract, achievements)
        if facts:
            text = set_field(text, "interestingFactsEn", facts)
            result["updated"].append("interestingFacts")

    if result["updated"]:
        path.write_text(text, encoding="utf-8", newline="\n")

    return result


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--ids", default="", help="Comma-separated asset ids")
    ap.add_argument(
        "--force-interesting-facts",
        action="store_true",
        help="Fill interestingFactsEn even when other En fields exist",
    )
    args = ap.parse_args()

    id_filter = {x.strip() for x in args.ids.split(",") if x.strip()} or None
    paths = sorted(ASSETS.glob("*.asset"))
    if id_filter:
        paths = [p for p in paths if p.stem in id_filter]

    # Collect wikidata ids
    jobs: list[tuple[Path, str]] = []
    for p in paths:
        t = p.read_text(encoding="utf-8")
        qid = extract_field(t, "wikidataId") or ""
        if qid.startswith("Q"):
            jobs.append((p, qid))

    qids = list({q for _, q in jobs})
    print(f"Resolving {len(qids)} Wikidata ids -> enwiki titles ...", flush=True)
    titles = batch_en_titles(qids)

    work_items: list[tuple[Path, str | None]] = []
    for p, qid in jobs:
        work_items.append((p, titles.get(qid)))

    # Assets without wikidata
    for p in paths:
        if not any(p == wp for wp, _ in jobs):
            work_items.append((p, None))

    updated = 0
    no_wiki = 0
    errors = 0
    results = []

    print(f"Importing EN from Wikipedia ({len(work_items)} assets, sequential) ...", flush=True)
    try:
        for p, title in work_items:
            aid = p.stem
            try:
                res = process_one(p, title, args.force_interesting_facts)
            except RateLimitError as ex:
                print(f"ABORT: {ex}", flush=True)
                results.append({"id": aid, "updated": [], "errors": [str(ex)]})
                break
            except Exception as ex:  # noqa: BLE001
                res = {"id": aid, "updated": [], "errors": [str(ex)]}
            results.append(res)
            if res.get("updated"):
                updated += 1
                print(f"  OK {aid}: {', '.join(res['updated'])}", flush=True)
            elif "no_enwiki" in str(res.get("errors")):
                no_wiki += 1
            elif res.get("errors"):
                errors += 1
                print(f"  ERR {aid}: {res['errors']}", flush=True)
            time.sleep(MIN_INTERVAL_S)
    except RateLimitError as ex:
        print(f"ABORT: {ex}", flush=True)

    report = {
        "updated_assets": updated,
        "no_enwiki": no_wiki,
        "errors": errors,
        "results": results,
    }
    out = Path(__file__).resolve().parent / "_en_wikipedia_import_report.json"
    out.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"Done. Updated {updated} assets. no_enwiki={no_wiki} errors={errors}", flush=True)
    print(f"Report: {out}", flush=True)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
