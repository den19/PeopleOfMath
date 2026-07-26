# -*- coding: utf-8 -*-
"""Inventory empty *En fields in mathematician assets."""
from __future__ import annotations

import json
import re
from pathlib import Path

ASSETS = Path(r"c:\git\PeopleOfMath\Assets\Data\Mathematicians")
FIELDS = [
    "fullName",
    "shortBio",
    "achievements",
    "personalLife",
    "interestingFacts",
]

# Next known MonoBehaviour field after each content field (for span detection)
FIELD_ORDER = [
    "id",
    "wikiTitleRu",
    "wikidataId",
    "fullNameRu",
    "fullNameEn",
    "birthDate",
    "deathDate",
    "countryKeys",
    "centuryKeys",
    "branchKeys",
    "achievementsRu",
    "achievementsEn",
    "personalLifeRu",
    "personalLifeEn",
    "shortBioRu",
    "shortBioEn",
    "interestingFactsRu",
    "interestingFactsEn",
    "wikipediaUrlRu",
    "portraits",
]


def decode_unity_string(raw: str) -> str:
    out: list[str] = []
    i = 0
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
            if n == "u" and i + 5 < len(raw):
                hexpart = raw[i + 2 : i + 6]
                try:
                    out.append(chr(int(hexpart, 16)))
                    i += 6
                    continue
                except ValueError:
                    pass
            if n == "U" and i + 9 < len(raw):
                hexpart = raw[i + 2 : i + 10]
                try:
                    out.append(chr(int(hexpart, 16)))
                    i += 10
                    continue
                except ValueError:
                    pass
        out.append(c)
        i += 1
    return "".join(out)


def find_field_span(text: str, name: str) -> tuple[int, int, str] | None:
    """Return (value_start, value_end, raw_value_including_quotes_or_plain)."""
    # Match only as a YAML key at indent 2
    pat = re.compile(rf"^  {re.escape(name)}:", re.M)
    m = pat.search(text)
    if not m:
        return None
    after_colon = m.end()
    # Skip one space if present (Unity style "key: value" or "key: ")
    if after_colon < len(text) and text[after_colon] == " ":
        value_start = after_colon + 1
    else:
        value_start = after_colon

    # Empty if next char is newline
    if value_start >= len(text) or text[value_start] in "\r\n":
        return (value_start, value_start, "")

    if text[value_start] == '"':
        # Parse quoted string
        i = value_start + 1
        while i < len(text):
            c = text[i]
            if c == "\\":
                if i + 1 < len(text) and text[i + 1] == "u":
                    i += 6
                    continue
                if i + 1 < len(text) and text[i + 1] == "U":
                    i += 10
                    continue
                i += 2
                continue
            if c == '"':
                return (value_start, i + 1, text[value_start : i + 1])
            i += 1
        return (value_start, len(text), text[value_start:])

    # Plain scalar until end of line
    end = value_start
    while end < len(text) and text[end] not in "\r\n":
        end += 1
    return (value_start, end, text[value_start:end])


def extract_field(text: str, name: str) -> str | None:
    span = find_field_span(text, name)
    if span is None:
        return None
    _, _, raw = span
    if raw == "":
        return ""
    if raw.startswith('"') and raw.endswith('"'):
        return decode_unity_string(raw[1:-1])
    return raw.strip()


def is_empty(s: str | None) -> bool:
    return s is None or not str(s).strip()


def encode_unity_string(s: str) -> str:
    """Encode string as Unity YAML double-quoted scalar with \\u escapes for non-ASCII."""
    parts = ['"']
    for ch in s:
        o = ord(ch)
        if ch == "\\":
            parts.append("\\\\")
        elif ch == '"':
            parts.append('\\"')
        elif ch == "\n":
            parts.append("\\n")
        elif ch == "\t":
            parts.append("\\t")
        elif ch == "\r":
            parts.append("\\r")
        elif o < 0x20 or o == 0x7F:
            parts.append(f"\\u{o:04X}")
        elif o > 0x7F:
            if o > 0xFFFF:
                parts.append(f"\\U{o:08X}")
            else:
                parts.append(f"\\u{o:04X}")
        else:
            parts.append(ch)
    parts.append('"')
    return "".join(parts)


def set_field(text: str, name: str, value: str) -> str:
    """Replace field value; empty string stays as empty (key: + space + newline)."""
    span = find_field_span(text, name)
    if span is None:
        raise KeyError(name)
    vs, ve, _ = span
    if value == "":
        encoded = ""
    elif all(ord(c) < 128 and c not in '\n\r"' for c in value) and value == value.strip():
        # Simple ASCII plain scalar (names like "Leonhard Euler")
        encoded = value
    else:
        encoded = encode_unity_string(value)
    return text[:vs] + encoded + text[ve:]


def main() -> None:
    stats = {f: {"need": 0, "has_en": 0, "empty_ru": 0} for f in FIELDS}
    need_by_file: list[dict] = []
    total_chars_need = 0
    samples: dict[str, list] = {f: [] for f in FIELDS}

    for path in sorted(ASSETS.glob("*.asset")):
        text = path.read_text(encoding="utf-8")
        needed = []
        for f in FIELDS:
            ru = extract_field(text, f + "Ru")
            en = extract_field(text, f + "En")
            if is_empty(ru):
                stats[f]["empty_ru"] += 1
            elif is_empty(en):
                stats[f]["need"] += 1
                needed.append(f)
                total_chars_need += len(ru)
                if len(samples[f]) < 2:
                    samples[f].append({"file": path.name, "ru_len": len(ru), "ru_head": ru[:80]})
            else:
                stats[f]["has_en"] += 1
        if needed:
            need_by_file.append({"file": path.name, "fields": needed})

    report = {
        "stats": stats,
        "assets_needing_en": len(need_by_file),
        "total_assets": len(list(ASSETS.glob("*.asset"))),
        "approx_ru_chars": total_chars_need,
        "need_by_file": need_by_file,
        "samples": samples,
    }
    out = Path(r"c:\git\PeopleOfMath\Tools\_en_inventory_report.json")
    out.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"Wrote {out}")
    print("STATS:", json.dumps(stats, indent=2))
    print(f"Assets needing EN: {len(need_by_file)} / {report['total_assets']}")
    print(f"Approx RU chars: {total_chars_need}")


if __name__ == "__main__":
    main()
