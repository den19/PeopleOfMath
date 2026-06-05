"""Download real portraits from Wikimedia into Assets/Resources/Portraits/{id}/"""
import argparse
import json
import random
import re
import time
import urllib.error
import urllib.parse
import urllib.request
from email.utils import parsedate_to_datetime
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CATALOG = ROOT / "Assets" / "Data" / "mathematicians_catalog.json"
PORTRAITS = ROOT / "Assets" / "Resources" / "Portraits"
REPORT = ROOT / "Assets" / "Data" / "import_report_python.txt"
UA = "PeopleOfMath/1.0 (batch portrait download)"
MAX_PER_ID = 2
MIN_IMAGES = 2
MIN_INTERVAL = 2.5
MAX_ATTEMPTS = 8
MAX_BACKOFF = 120
CIRCUIT_BREAKER_SEC = 90
PLACEHOLDER_MAX_BYTES = 25000
BAD = ("tasman", "memphis", "madonna", "banner", "screw", "signature", "diagram", "map", "stamp")

_last_request = 0.0
_circuit_breaker_used = False


def reset_session() -> None:
    global _last_request, _circuit_breaker_used
    _last_request = 0.0
    _circuit_breaker_used = False


def wait_interval() -> None:
    global _last_request
    if _last_request > 0:
        elapsed = time.monotonic() - _last_request
        wait = MIN_INTERVAL - elapsed
        if wait > 0:
            time.sleep(wait)


def parse_retry_after(headers) -> int:
    raw = headers.get("Retry-After")
    if not raw:
        return 0
    raw = raw.strip()
    if raw.isdigit():
        return min(int(raw), 120)
    try:
        dt = parsedate_to_datetime(raw)
        sec = int(max(0, (dt.timestamp() - time.time())))
        return min(sec, 120)
    except (TypeError, ValueError, OverflowError):
        return 0


def is_rate_limited(exc: urllib.error.HTTPError) -> bool:
    return exc.code in (429, 503)


def backoff_seconds(exc: urllib.error.HTTPError, attempt: int) -> float:
    ra = parse_retry_after(exc.headers)
    if ra > 0:
        return float(ra)
    exp = min(MAX_BACKOFF, 15 * (2**attempt))
    return exp + random.uniform(0, 3)


def request_with_backoff(url: str, binary: bool = False) -> bytes | str:
    global _last_request, _circuit_breaker_used
    last_exc = None
    for attempt in range(MAX_ATTEMPTS):
        wait_interval()
        req = urllib.request.Request(url, headers={"User-Agent": UA})
        try:
            with urllib.request.urlopen(req, timeout=120) as r:
                data = r.read()
                _last_request = time.monotonic()
                return data if binary else data.decode("utf-8", errors="replace")
        except urllib.error.HTTPError as e:
            _last_request = time.monotonic()
            last_exc = e
            if not is_rate_limited(e):
                raise
            wait = backoff_seconds(e, attempt)
            print(f"WAIT 429 {e.code} {wait:.0f}s attempt {attempt + 1}/{MAX_ATTEMPTS}")
            time.sleep(wait)
            if not _circuit_breaker_used and attempt >= 2:
                _circuit_breaker_used = True
                print(f"WAIT circuit breaker {CIRCUIT_BREAKER_SEC}s")
                time.sleep(CIRCUIT_BREAKER_SEC)
    if last_exc:
        raise last_exc
    raise RuntimeError(f"Rate limit: failed after {MAX_ATTEMPTS} attempts")


def get(url: str) -> str:
    return request_with_backoff(url, binary=False)


def is_placeholder(path: Path) -> bool:
    if not path.exists():
        return False
    if path.with_suffix(path.suffix + ".placeholder").exists():
        return True
    return path.stat().st_size < PLACEHOLDER_MAX_BYTES


def count_real_portraits(folder: Path) -> int:
    if not folder.exists():
        return 0
    n = 0
    for f in folder.iterdir():
        if f.suffix == ".meta":
            continue
        if f.suffix.lower() not in (".jpg", ".jpeg", ".png", ".webp"):
            continue
        if not is_placeholder(f):
            n += 1
    return n


def has_enough_real(folder: Path) -> bool:
    return count_real_portraits(folder) >= MIN_IMAGES


def clear_placeholders(folder: Path) -> None:
    if not folder.exists():
        return
    for f in list(folder.glob("*")):
        if f.suffix == ".meta":
            continue
        if f.suffix in (".jpg", ".jpeg", ".png", ".webp") and is_placeholder(f):
            f.unlink(missing_ok=True)
            f.with_suffix(f.suffix + ".placeholder").unlink(missing_ok=True)


def tokens_from(text: str, entry_id: str) -> set[str]:
    out = {entry_id.replace("_", " ").lower()}
    if not text:
        return out
    for part in re.split(r"[\s,._-]+", text.lower()):
        if len(part) >= 3:
            out.add(part)
    return out


def score_title(title: str, toks: set[str]) -> int:
    t = title.lower()
    if any(b in t for b in BAD):
        return -1
    if not t.startswith("file:"):
        return -1
    s = 0
    for tok in toks:
        if tok in t:
            s += 12
    if "portrait" in t or "photo" in t:
        s += 4
    return s if s >= 8 else 0


def wikidata_p18(qid: str, toks: set[str]) -> list[dict]:
    if not qid.startswith("Q"):
        qid = "Q" + qid
    j = get(f"https://www.wikidata.org/w/api.php?action=wbgetentities&ids={qid}&props=claims&format=json")
    files = []
    for name in re.findall(r'"P18"[\s\S]*?"value"\s*:\s*"([^"]+\.(?:jpg|jpeg|png))"', j):
        enc = urllib.parse.quote("File:" + name)
        u = (
            "https://commons.wikimedia.org/w/api.php?action=query&titles="
            f"{enc}&prop=imageinfo&iiprop=url|extmetadata&format=json"
        )
        j2 = get(u)
        for title in re.findall(r'"title":"(File:[^"]+)"', j2):
            if score_title(title, toks) <= 0:
                continue
            idx = j2.find(title)
            block = j2[idx : idx + 5000]
            um = re.search(r'"url":"(https://upload[^"\\]+)"', block)
            lm = re.search(r'LicenseShortName.*?value":"([^"]+)"', block)
            if um:
                lic = (lm.group(1) if lm else "").lower()
                if "nc" in lic or "nd" in lic:
                    continue
                files.append(
                    {
                        "title": title,
                        "url": um.group(1),
                        "license": lm.group(1) if lm else "PD",
                        "score": score_title(title, toks),
                    }
                )
    return sorted(files, key=lambda x: -x["score"])


def search_files(term: str, toks: set[str]) -> list[dict]:
    enc = urllib.parse.quote(term)
    url = (
        "https://commons.wikimedia.org/w/api.php?action=query&generator=search"
        f"&gsrnamespace=6&gsrsearch={enc}&gsrlimit=8"
        "&prop=imageinfo&iiprop=url|extmetadata&format=json"
    )
    j = get(url)
    out = []
    for title in re.findall(r'"title":"(File:[^"]+)"', j):
        sc = score_title(title, toks)
        if sc <= 0:
            continue
        idx = j.find(title)
        block = j[idx : idx + 5000]
        um = re.search(r'"url":"(https://upload[^"\\]+)"', block)
        lm = re.search(r'LicenseShortName.*?value":"([^"]+)"', block)
        if not um:
            continue
        lic = (lm.group(1) if lm else "").lower()
        if "nc" in lic or "nd" in lic:
            continue
        if not any(x in lic for x in ("public domain", "cc-by", "cc by", "cc0", "pd")):
            continue
        out.append({"title": title, "url": um.group(1), "license": lm.group(1) if lm else "", "score": sc})
    return sorted(out, key=lambda x: -x["score"])


def download(url: str, dest: Path) -> bool:
    data = request_with_backoff(url, binary=True)
    dest.write_bytes(data)
    return dest.stat().st_size > PLACEHOLDER_MAX_BYTES


def parse_args():
    p = argparse.ArgumentParser(description="Download Wikimedia portraits into Resources/Portraits")
    p.add_argument("--empty-only", action="store_true", help="Skip folders with >= 2 real JPEG")
    p.add_argument("--ids", type=str, default="", help="Comma-separated mathematician ids")
    return p.parse_args()


def main():
    args = parse_args()
    reset_session()
    id_filter = {x.strip() for x in args.ids.split(",") if x.strip()} if args.ids else None

    data = json.loads(CATALOG.read_text(encoding="utf-8"))
    lines = [f"Python portrait import {time.strftime('%Y-%m-%d %H:%M')} empty_only={args.empty_only}"]
    ok = 0
    skip = 0
    fail = 0

    for entry in data["mathematicians"]:
        mid = entry["id"]
        if id_filter and mid not in id_filter:
            continue

        folder = PORTRAITS / mid
        if args.empty_only and has_enough_real(folder):
            lines.append(f"SKIP has_images {mid}")
            skip += 1
            continue

        clear_placeholders(folder)
        folder.mkdir(parents=True, exist_ok=True)

        toks = tokens_from(entry.get("wikiTitleRu", ""), mid)
        candidates = []
        if entry.get("wikidataId"):
            try:
                candidates.extend(wikidata_p18(entry["wikidataId"], toks))
            except Exception as e:
                lines.append(f"FAIL {mid}: wikidata {e}")
                fail += 1
                continue

        q = entry.get("wikiTitleRu", "")
        fam = q.split(",")[0].strip() if "," in q else q
        if len(candidates) < MAX_PER_ID and fam:
            try:
                candidates.extend(search_files(fam + " portrait", toks))
            except Exception as e:
                lines.append(f"WARN {mid}: search {e}")

        seen = set()
        n = 0
        for c in candidates:
            if c["title"] in seen:
                continue
            seen.add(c["title"])
            dest = folder / f"{n + 1:02d}.jpg"
            if dest.exists() and not is_placeholder(dest):
                n += 1
                continue
            try:
                if download(c["url"], dest):
                    lines.append(f"OK {mid}: {c['title']}")
                    print(f"OK {mid}: {c['title']}")
                    n += 1
                    ok += 1
                    if n >= MAX_PER_ID:
                        break
            except Exception as e:
                lines.append(f"FAIL {mid}: {e}")
                print(f"FAIL {mid}: {e}")
                fail += 1

        if n < MIN_IMAGES:
            lines.append(f"WARN {mid}: only {n} image(s)")

    lines.append(f"Summary: OK files={ok}, SKIP={skip}, FAIL events={fail}")
    lines.append("Next: Unity → Link Portraits From Folders")
    report_text = "\n".join(lines) + "\n"
    REPORT.write_text(report_text, encoding="utf-8")
    print(report_text)


if __name__ == "__main__":
    main()
