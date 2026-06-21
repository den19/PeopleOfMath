import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
FOLDER = ROOT / "Assets" / "Data" / "Mathematicians"
PATTERN_U16 = re.compile(r"\\?u([0-9a-fA-F]{4})")
PATTERN_U32 = re.compile(r"\\U([0-9a-fA-F]{8})")
PATTERN_X = re.compile(r"\\x([0-9a-fA-F]{2})")
BROKEN = re.compile(r'(?<!\\)u0[0-9a-fA-F]{3}|(?<!\\)U0001[0-9a-fA-F]{5}')


def decode(s: str) -> str:
    s = PATTERN_X.sub(lambda m: chr(int(m.group(1), 16)), s)
    s = PATTERN_U32.sub(lambda m: chr(int(m.group(1), 16)), s)
    return PATTERN_U16.sub(lambda m: chr(int(m.group(1), 16)), s)


def fix_file(path: Path) -> bool:
    text = path.read_text(encoding="utf-8")
    if not BROKEN.search(text):
        return False
    new = decode(text)
    if new == text:
        return False
    path.write_text(new, encoding="utf-8")
    return True


def main():
    n = sum(fix_file(p) for p in FOLDER.glob("*.asset"))
    print(f"Fixed {n} asset files")


if __name__ == "__main__":
    main()
