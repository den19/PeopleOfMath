"""Proofread mathematician .asset YAML and center Header titles in Main.unity."""
from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(r"c:\git\PeopleOfMath")
ASSETS = ROOT / "Assets" / "Data" / "Mathematicians"
SCENE = ROOT / "Assets" / "Scenes" / "Main.unity"

TEXT_FIELDS = (
    "fullNameRu", "fullNameEn",
    "shortBioRu", "shortBioEn",
    "achievementsRu", "achievementsEn",
    "personalLifeRu", "personalLifeEn",
    "interestingFactsRu", "interestingFactsEn",
)

HTML_ENTITY = {
    "&amp;": "&", "&lt;": "<", "&gt;": ">",
    "&quot;": '"', "&apos;": "'", "&nbsp;": " ",
}


def unity_decode(s: str) -> str:
    def repl_U(m):
        return chr(int(m.group(1), 16))

    def repl_u(m):
        return chr(int(m.group(1), 16))

    def repl_x(m):
        return chr(int(m.group(1), 16))

    s = re.sub(r"\\U([0-9a-fA-F]{8})", repl_U, s)
    s = re.sub(r"\\u([0-9a-fA-F]{4})", repl_u, s)
    s = re.sub(r"\\x([0-9a-fA-F]{2})", repl_x, s)
    s = s.replace(r"\n", "\n").replace(r"\"", '"').replace(r"\\", "\\")
    return s


def unity_encode(s: str) -> str:
    out = []
    for ch in s:
        o = ord(ch)
        if ch == "\\":
            out.append("\\\\")
        elif ch == '"':
            out.append('\\"')
        elif ch == "\n":
            out.append("\\n")
        elif o < 0x20 or o > 0x7E:
            if o > 0xFFFF:
                out.append(f"\\U{o:08X}")
            else:
                out.append(f"\\u{o:04x}")
        else:
            out.append(ch)
    return "".join(out)


def wrap_unity_string(encoded: str, indent: int = 2) -> str:
    """Break long Unity strings only at spaces (never mid-word / mid-escape)."""
    pad = " " * (indent + 2)
    if len(encoded) <= 100:
        return f'"{encoded}"'

    parts: list[str] = []
    line = ""
    i = 0
    while i < len(encoded):
        if encoded[i] == "\\" and i + 1 < len(encoded):
            nxt = encoded[i + 1]
            if nxt == "u" and i + 6 <= len(encoded):
                chunk = encoded[i : i + 6]
                i += 6
            elif nxt == "U" and i + 10 <= len(encoded):
                chunk = encoded[i : i + 10]
                i += 10
            elif nxt == "x" and i + 4 <= len(encoded):
                chunk = encoded[i : i + 4]
                i += 4
            else:
                chunk = encoded[i : i + 2]
                i += 2
        else:
            chunk = encoded[i]
            i += 1

        line += chunk
        # Break only after a literal space, and only when line is long enough.
        if chunk == " " and len(line) >= 88:
            parts.append(line)
            line = ""

    if line:
        parts.append(line)

    if len(parts) == 1:
        return f'"{parts[0]}"'

    body = parts[0].rstrip() + "\n"
    for idx, p in enumerate(parts[1:]):
        body += pad + p.rstrip()
        if idx != len(parts) - 2:
            body += "\n"
    return f'"{body}"'


def editorial_clean(text: str) -> str:
    if not text:
        return text

    for k, v in HTML_ENTITY.items():
        text = re.sub(re.escape(k), v, text, flags=re.IGNORECASE)
    text = re.sub(r"(?:(?<=\s)|(?<=\()|(?<=^))gt;(?=[\s).,]|$)", ">", text)
    text = re.sub(r"(?m)^\s*#{1,6}\s*", "", text)
    text = re.sub(r"#{2,6}\s*", "", text)
    text = re.sub(r"\*\*(.+?)\*\*", r"\1", text, flags=re.DOTALL)
    text = re.sub(r"__(.+?)__", r"\1", text, flags=re.DOTALL)
    text = re.sub(r"(?<!\*)\*(?!\*)([^*\n]+?)(?<!\*)\*(?!\*)", r"\1", text)
    text = re.sub(r"(?m)^\s*[\*\-]\s+", "• ", text)
    text = re.sub(r"(?<![\w*])\*(?![\w*])", "", text)
    text = re.sub(r"[ \t]{2,}", " ", text)
    text = re.sub(r" *\n *", "\n", text)
    text = re.sub(r"\n{3,}", "\n\n", text)
    text = re.sub(r" +([,.;:!?»])", r"\1", text)
    text = separate_emoji_heading_lines(text)
    text = re.sub(r"\n{3,}", "\n\n", text)
    return text.strip()


def _is_emoji(ch: str) -> bool:
    v = ord(ch)
    return (
        0x1F300 <= v <= 0x1FAFF
        or 0x1F1E0 <= v <= 0x1F1FF
        or 0x2600 <= v <= 0x27BF
        or 0x2300 <= v <= 0x23FF
        or 0x2190 <= v <= 0x21FF
        or 0x2B00 <= v <= 0x2BFF
    )


def _starts_with_emoji(line: str) -> bool:
    stripped = line.lstrip()
    return bool(stripped) and _is_emoji(stripped[0])


def _split_inline_emoji_segments(line: str) -> list[str]:
    if not line:
        return [line]

    parts: list[str] = []
    start = 0
    chars = list(line)  # code points as single chars for BMP+supplementary via python str
    # Iterate by codepoint index in the string itself
    i = 0
    while i < len(line):
        ch = line[i]
        if i > start and _is_emoji(ch) and line[i - 1].isspace():
            break_at = i
            while break_at > start and line[break_at - 1] == " ":
                break_at -= 1
            if break_at > start:
                parts.append(line[start:break_at])
                start = i
        i += 1
    parts.append(line[start:])
    return parts


def separate_emoji_heading_lines(text: str) -> str:
    expanded: list[str] = []
    for line in text.replace("\r\n", "\n").split("\n"):
        expanded.extend(_split_inline_emoji_segments(line))

    result: list[str] = []
    for i, line in enumerate(expanded):
        if not _starts_with_emoji(line):
            result.append(line)
            continue

        if result and result[-1]:
            result.append("")
        result.append(line.rstrip())
        if i + 1 < len(expanded) and expanded[i + 1]:
            result.append("")

    return "\n".join(result)


FIELD_RE = re.compile(
    r"^(\s*)(" + "|".join(TEXT_FIELDS) + r"):\s*\"((?:\\.|[^\"\\]|\\\n|.)*)\"\s*$",
    re.MULTILINE | re.DOTALL,
)


def extract_quoted_fields(content: str):
    """Yield (start, end, indent, field, raw_inside) for each text field."""
    results = []
    for field in TEXT_FIELDS:
        pattern = re.compile(
            rf"^(\s*)({field}):\s*\"",
            re.MULTILINE,
        )
        for m in pattern.finditer(content):
            start_quote = m.end() - 1  # points at opening "
            i = start_quote + 1
            raw = []
            while i < len(content):
                ch = content[i]
                if ch == "\\" and i + 1 < len(content):
                    nxt = content[i + 1]
                    if nxt == "u" and i + 6 <= len(content):
                        raw.append(content[i : i + 6])
                        i += 6
                        continue
                    if nxt == "U" and i + 10 <= len(content):
                        raw.append(content[i : i + 10])
                        i += 10
                        continue
                    if nxt == "x" and i + 4 <= len(content):
                        raw.append(content[i : i + 4])
                        i += 4
                        continue
                    raw.append(content[i : i + 2])
                    i += 2
                    continue
                if ch == '"':
                    end = i + 1
                    results.append((m.start(), end, m.group(1), field, "".join(raw)))
                    break
                raw.append(ch)
                i += 1
    return results


def proofread_asset(path: Path) -> bool:
    content = path.read_text(encoding="utf-8")
    fields = extract_quoted_fields(content)
    if not fields:
        return False

    # apply from end to start
    fields.sort(key=lambda x: x[0], reverse=True)
    changed = False
    for start, end, indent, field, raw_inside in fields:
        # Unity YAML may include real newlines + indentation spaces inside the quoted value.
        # Normalize continuation indents: newline + spaces -> single space if mid-word break,
        # but keep intentional \n escapes.
        decoded = unity_decode(raw_inside)
        # Collapse YAML visual line-wrap spaces that Unity inserts as "\n    "
        decoded = re.sub(r"\n[ \t]+", " ", decoded)
        cleaned = editorial_clean(decoded)
        if cleaned == decoded:
            continue

        new_raw = unity_encode(cleaned)
        if len(new_raw) < 100:
            replacement = f'{indent}{field}: "{new_raw}"'
        else:
            wrapped = wrap_unity_string(new_raw, indent=len(indent))
            replacement = f"{indent}{field}: {wrapped}"
        content = content[:start] + replacement + content[end:]
        changed = True

    if changed:
        path.write_text(content, encoding="utf-8", newline="\n")
    return changed


TITLE_NAMES = {
    "HomeTitle", "SettingsTitle", "IndexTitle", "FavoritesTitle",
    "QuizTitle", "AboutTitle", "PlainTitle",
}


def fix_header_titles(scene_path: Path) -> int:
    text = scene_path.read_text(encoding="utf-8")
    patched = 0

    for name in TITLE_NAMES:
        # Find GameObject block then its RectTransform fileID from m_Component first entry
        go_re = re.compile(
            rf"(--- !u!1 &(\d+)\nGameObject:\n(?:.*\n)*?  m_Component:\n(?:  - component: \{{fileID: (\d+)\}}\n)+.*?  m_Name: {name}\n)",
            re.MULTILINE,
        )
        # Simpler approach: find m_Name: X then search backward for RectTransform with matching GameObject

    # Patch by locating each title name and then the following RectTransform sibling that belongs to it.
    # Pattern used in scene: GameObject with m_Name, components listed; RectTransform is usually previous or next.

    # Find all RectTransforms that are for header titles by walking name -> gameObject id -> rect
    name_to_go = {}
    for m in re.finditer(r"^--- !u!1 &(\d+)\nGameObject:\n((?:.*\n)*?)  m_Name: (\w+)\n", text, re.MULTILINE):
        go_id, body, name = m.group(1), m.group(2), m.group(3)
        if name in TITLE_NAMES:
            name_to_go[name] = go_id

    for name, go_id in name_to_go.items():
        # Find RectTransform whose m_GameObject points to this go
        rt_re = re.compile(
            rf"(--- !u!224 &\d+\nRectTransform:\n(?:  .*\n)*?  m_GameObject: \{{fileID: {go_id}\}}\n(?:  .*\n)*?  m_AnchorMin: \{{x: [-\d.]+, y: [-\d.]+\}}\n"
            rf"  m_AnchorMax: \{{x: [-\d.]+, y: [-\d.]+\}}\n"
            rf"  m_AnchoredPosition: \{{x: [-\d.]+, y: [-\d.]+\}}\n"
            rf"  m_SizeDelta: \{{x: [-\d.]+, y: [-\d.]+\}}\n"
            rf"  m_Pivot: \{{x: [-\d.]+, y: [-\d.]+\}}\n)"
        )
        m = rt_re.search(text)
        if not m:
            print(f"  miss rect for {name} ({go_id})")
            continue
        old = m.group(1)
        new = re.sub(
            r"m_AnchorMin: \{x: [-\d.]+, y: [-\d.]+\}",
            "m_AnchorMin: {x: 0, y: 1}",
            old,
        )
        new = re.sub(
            r"m_AnchorMax: \{x: [-\d.]+, y: [-\d.]+\}",
            "m_AnchorMax: {x: 1, y: 1}",
            new,
        )
        new = re.sub(
            r"m_AnchoredPosition: \{x: [-\d.]+, y: [-\d.]+\}",
            "m_AnchoredPosition: {x: 0, y: -60}",
            new,
        )
        new = re.sub(
            r"m_SizeDelta: \{x: [-\d.]+, y: [-\d.]+\}",
            "m_SizeDelta: {x: -56, y: 72}",
            new,
        )
        new = re.sub(
            r"m_Pivot: \{x: [-\d.]+, y: [-\d.]+\}",
            "m_Pivot: {x: 0.5, y: 0.5}",
            new,
        )
        if new != old:
            text = text[: m.start(1)] + new + text[m.end(1) :]
            patched += 1

        # TMP on same GameObject
        tmp_re = re.compile(
            rf"(--- !u!114 &\d+\nMonoBehaviour:\n(?:  .*\n)*?  m_GameObject: \{{fileID: {go_id}\}}\n"
            rf"(?:  .*\n)*?  m_EditorClassIdentifier: Unity\.TextMeshPro::TMPro\.TextMeshProUGUI\n"
            rf"(?:  .*\n)*?  m_fontSize: [-\d.]+\n"
            rf"  m_fontSizeBase: [-\d.]+\n"
            rf"(?:  .*\n)*?  m_fontSizeMin: [-\d.]+\n"
            rf"  m_fontSizeMax: [-\d.]+\n"
            rf"(?:  .*\n)*?  m_HorizontalAlignment: \d+\n"
            rf"  m_VerticalAlignment: \d+\n)"
        )
        tm = tmp_re.search(text)
        if not tm:
            # try looser: find TMP block for this gameObject and patch key fields individually
            block_re = re.compile(
                rf"--- !u!114 &\d+\nMonoBehaviour:\n(?:  .*\n)*?  m_GameObject: \{{fileID: {go_id}\}}\n"
                rf"(?:  .*\n)*?  m_EditorClassIdentifier: Unity\.TextMeshPro::TMPro\.TextMeshProUGUI\n"
                rf"(?:  .*\n){80}"
            )
            bm = block_re.search(text)
            if not bm:
                print(f"  miss tmp for {name}")
                continue
            block = bm.group(0)
            nb = block
            nb = re.sub(r"m_fontSize: [-\d.]+", "m_fontSize: 42", nb, count=1)
            nb = re.sub(r"m_fontSizeBase: [-\d.]+", "m_fontSizeBase: 42", nb, count=1)
            nb = re.sub(r"m_enableAutoSizing: \d", "m_enableAutoSizing: 1", nb, count=1)
            nb = re.sub(r"m_fontSizeMin: [-\d.]+", "m_fontSizeMin: 26", nb, count=1)
            nb = re.sub(r"m_fontSizeMax: [-\d.]+", "m_fontSizeMax: 48", nb, count=1)
            nb = re.sub(r"m_HorizontalAlignment: \d+", "m_HorizontalAlignment: 2", nb, count=1)
            nb = re.sub(r"m_VerticalAlignment: \d+", "m_VerticalAlignment: 512", nb, count=1)  # Middle
            if nb != block:
                text = text[: bm.start()] + nb + text[bm.end() :]
                patched += 1
        else:
            block = tm.group(1)
            nb = block
            nb = re.sub(r"m_fontSize: [-\d.]+", "m_fontSize: 42", nb)
            nb = re.sub(r"m_fontSizeBase: [-\d.]+", "m_fontSizeBase: 42", nb)
            nb = re.sub(r"m_fontSizeMin: [-\d.]+", "m_fontSizeMin: 26", nb)
            nb = re.sub(r"m_fontSizeMax: [-\d.]+", "m_fontSizeMax: 48", nb)
            nb = re.sub(r"m_HorizontalAlignment: \d+", "m_HorizontalAlignment: 2", nb)
            nb = re.sub(r"m_VerticalAlignment: \d+", "m_VerticalAlignment: 512", nb)
            if nb != block:
                text = text[: tm.start(1)] + nb + text[tm.end(1) :]
                patched += 1

    scene_path.write_text(text, encoding="utf-8", newline="\n")
    return patched


def main():
    only = sys.argv[1] if len(sys.argv) > 1 else "all"
    if only in ("all", "assets"):
        changed_assets = 0
        for path in sorted(ASSETS.glob("*.asset")):
            if proofread_asset(path):
                changed_assets += 1
                print(f"cleaned {path.name}")
        print(f"assets cleaned: {changed_assets}")

    if only in ("all", "header"):
        n = fix_header_titles(SCENE)
        print(f"header title patches: {n}")


if __name__ == "__main__":
    main()
