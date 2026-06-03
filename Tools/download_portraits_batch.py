"""Download portraits from Wikimedia Commons into Assets/Data/Images/{id}/"""
import json
import re
import time
import urllib.parse
import urllib.request
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CATALOG = ROOT / "Assets" / "Data" / "mathematicians_catalog.json"
IMAGES = ROOT / "Assets" / "Resources" / "Portraits"
UA = "PeopleOfMath/1.0 (batch portrait download)"
MAX_PER_ID = 2
DELAY = 1.2


def get(url: str, retries: int = 5) -> str:
    for attempt in range(retries):
        try:
            req = urllib.request.Request(url, headers={"User-Agent": UA})
            with urllib.request.urlopen(req, timeout=60) as r:
                return r.read().decode("utf-8", errors="replace")
        except urllib.error.HTTPError as e:
            if e.code == 429 and attempt < retries - 1:
                time.sleep(8 * (attempt + 1))
                continue
            raise


def search_files(term: str) -> list[dict]:
    enc = urllib.parse.quote(term)
    url = (
        "https://commons.wikimedia.org/w/api.php?action=query&generator=search"
        f"&gsrnamespace=6&gsrsearch={enc}&gsrlimit=6"
        "&prop=imageinfo&iiprop=url|extmetadata&format=json"
    )
    j = get(url)
    out = []
    for title in re.findall(r'"title":"(File:[^"]+)"', j):
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
        t = title.lower()
        if any(x in t for x in ("signature", "diagram", "map", "stamp")):
            continue
        out.append({"title": title, "url": um.group(1), "license": lm.group(1) if lm else ""})
    return out


def wikidata_files(qid: str) -> list[dict]:
    if not qid.startswith("Q"):
        qid = "Q" + qid
    j = get(f"https://www.wikidata.org/w/api.php?action=wbgetentities&ids={qid}&props=claims&format=json")
    files = []
    for name in re.findall(r'"value":"([^"]+\.(?:jpg|jpeg|png))"', j):
        enc = urllib.parse.quote("File:" + name)
        u = f"https://commons.wikimedia.org/w/api.php?action=query&titles={enc}&prop=imageinfo&iiprop=url|extmetadata&format=json"
        j2 = get(u)
        for title in re.findall(r'"title":"(File:[^"]+)"', j2):
            idx = j2.find(title)
            block = j2[idx : idx + 5000]
            um = re.search(r'"url":"(https://upload[^"\\]+)"', block)
            lm = re.search(r'LicenseShortName.*?value":"([^"]+)"', block)
            if um:
                files.append({"title": title, "url": um.group(1), "license": lm.group(1) if lm else "PD"})
    return files


def download(url: str, dest: Path) -> bool:
    for attempt in range(5):
        try:
            req = urllib.request.Request(url, headers={"User-Agent": UA})
            with urllib.request.urlopen(req, timeout=120) as r:
                dest.write_bytes(r.read())
            return dest.stat().st_size > 1000
        except urllib.error.HTTPError as e:
            if e.code == 429 and attempt < 4:
                time.sleep(8 * (attempt + 1))
                continue
            raise
    return False


def main():
    data = json.loads(CATALOG.read_text(encoding="utf-8"))
    ok = 0
    for entry in data["mathematicians"]:
        mid = entry["id"]
        folder = IMAGES / mid
        existing = list(folder.glob("*.jpg")) + list(folder.glob("*.png")) if folder.exists() else []
        if len(existing) >= MAX_PER_ID:
            continue
        folder.mkdir(parents=True, exist_ok=True)
        candidates = []
        if entry.get("wikidataId"):
            candidates.extend(wikidata_files(entry["wikidataId"]))
        q = entry["wikiTitleRu"]
        fam = q.split(",")[0].strip() if "," in q else q
        candidates.extend(search_files(fam + " portrait"))
        seen = set()
        n = 0
        for c in candidates:
            if c["title"] in seen:
                continue
            seen.add(c["title"])
            dest = folder / f"{n + 1:02d}.jpg"
            if dest.exists():
                n += 1
                continue
            try:
                if download(c["url"], dest):
                    print(f"OK {mid}: {c['title']}")
                    n += 1
                    ok += 1
                    if n >= MAX_PER_ID:
                        break
            except Exception as e:
                print(f"FAIL {mid}: {e}")
            time.sleep(DELAY)
    print(f"Downloaded {ok} files. Run Unity: PeopleOfMath -> Link Portraits From Folders")


if __name__ == "__main__":
    main()
