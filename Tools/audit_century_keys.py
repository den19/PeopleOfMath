#!/usr/bin/env python3
"""Audit centuryKeys against birth/death dates (life-span rule). Exit 1 on mismatch."""

from __future__ import annotations

import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CATALOG_PATH = ROOT / "Assets" / "Data" / "mathematicians_catalog.json"
ASSETS_DIR = ROOT / "Assets" / "Data" / "Mathematicians"

# Skip cards with known bad or unreliable dates (see project notes).
SKIP_IDS = frozenset({
    "nash",      # deathDate typo: 1835
    "gromov",    # birthDate typo: 1899 instead of 1943
    "hilbert",   # birthDate typo: 1910 instead of 1862
    "hamilton",  # centuryKeys manually set; dates 1616–1651
})


def normalize_date_field(field: str | None) -> str:
    if not field:
        return ""
    s = str(field).strip().strip('"')
    if "\\u" in s:
        try:
            s = s.encode("utf-8").decode("unicode_escape")
        except UnicodeDecodeError:
            pass
    return s


def parse_year(field: str | None) -> int | None:
    s = normalize_date_field(field)
    if not s:
        return None
    bc = bool(re.search(r"до\s*н\.?\s*э|BCE|BC", s, re.I))
    m = re.search(r"(\d{1,2})\.(\d{1,2})\.(\d{4})", s)
    if m:
        y = int(m.group(3))
        return -y if bc else y
    years = [int(x) for x in re.findall(r"\d{3,4}", s)]
    if not years:
        m = re.search(r"~?\s*(\d{3,4})", s)
        if m:
            y = int(m.group(1))
            return -y if bc else y
        return None
    # Prefer calendar years (>= 100); for Russian text dates use the last one.
    calendar = [y for y in years if y >= 100]
    y = (calendar or years)[-1]
    return -y if bc else y


def century_key(year: int) -> str:
    if year < 0:
        c = (-year - 1) // 100 + 1
        return f"{c}bc"
    c = (year - 1) // 100 + 1
    return str(c)


def centuries_for_life(birth: int | None, death: int | None) -> set[str]:
    if birth is None and death is None:
        return set()
    y1 = birth if birth is not None else death
    y2 = death if death is not None else birth
    assert y1 is not None and y2 is not None
    lo, hi = min(y1, y2), max(y1, y2)
    keys: set[str] = set()
    for y in range(lo, hi + 1):
        keys.add(century_key(y))
    return keys


def century_sort_key(key: str) -> tuple[int, int]:
    if key.endswith("bc"):
        return (0, -int(key[:-2]))
    return (1, int(key))


def parse_asset_century_keys(text: str) -> list[str]:
    keys: list[str] = []
    in_block = False
    for line in text.splitlines():
        if line.strip() == "centuryKeys:":
            in_block = True
            continue
        if in_block:
            if line.startswith("  - "):
                keys.append(line[4:].strip())
            elif line.startswith("  ") and line.strip():
                break
            elif not line.startswith("  "):
                break
    return keys


def read_asset_dates(mid: str) -> tuple[str | None, str | None]:
    path = ASSETS_DIR / f"{mid}.asset"
    if not path.exists():
        return None, None
    text = path.read_text(encoding="utf-8")
    birth = re.search(r"^  birthDate: (.*)$", text, re.M)
    death = re.search(r"^  deathDate: (.*)$", text, re.M)
    b = birth.group(1).strip() if birth else None
    d = death.group(1).strip() if death else None
    if b in ("", "null"):
        b = None
    if d in ("", "null"):
        d = None
    return b, d


def load_catalog() -> dict[str, list[str]]:
    data = json.loads(CATALOG_PATH.read_text(encoding="utf-8"))
    return {m["id"]: list(m.get("centuryKeys", [])) for m in data["mathematicians"]}


def main() -> int:
    catalog = load_catalog()
    mismatches: list[str] = []
    catalog_asset_diff: list[str] = []

    for mid, catalog_keys in sorted(catalog.items()):
        asset_path = ASSETS_DIR / f"{mid}.asset"
        if not asset_path.exists():
            catalog_asset_diff.append(f"{mid}: missing asset")
            continue
        asset_keys = parse_asset_century_keys(asset_path.read_text(encoding="utf-8"))
        if sorted(catalog_keys, key=century_sort_key) != sorted(asset_keys, key=century_sort_key):
            catalog_asset_diff.append(
                f"{mid}: catalog={catalog_keys} asset={asset_keys}"
            )

        if mid in SKIP_IDS:
            continue

        birth_s, death_s = read_asset_dates(mid)
        y1, y2 = parse_year(birth_s), parse_year(death_s)
        expected = centuries_for_life(y1, y2)
        if not expected:
            continue

        actual = set(catalog_keys)
        if actual != expected:
            mismatches.append(
                f"{mid}: expected={sorted(expected, key=century_sort_key)} "
                f"actual={sorted(actual, key=century_sort_key)} "
                f"(birth={birth_s!r}, death={death_s!r})"
            )

    if catalog_asset_diff:
        print("Catalog vs asset centuryKeys:")
        for line in catalog_asset_diff:
            print(" ", line)

    if mismatches:
        print("Life-span century mismatches:")
        for line in mismatches:
            print(" ", line)

    if catalog_asset_diff or mismatches:
        return 1

    print(f"OK: {len(catalog)} mathematicians, centuryKeys consistent.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
