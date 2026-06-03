import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
FOLDER = ROOT / "Assets" / "Data" / "Mathematicians"
PATTERN = re.compile(r"\\?u([0-9a-fA-F]{4})")
BROKEN = re.compile(r'(?<!\\)u0[0-9a-fA-F]{3}')


def decode(s: str) -> str:
    return PATTERN.sub(lambda m: chr(int(m.group(1), 16)), s)


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
