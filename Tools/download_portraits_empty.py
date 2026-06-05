"""Convenience wrapper: download portraits only for empty Resources/Portraits folders."""
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
BATCH = ROOT / "Tools" / "download_portraits_batch.py"

if __name__ == "__main__":
    extra = sys.argv[1:]
    cmd = [sys.executable, str(BATCH), "--empty-only", *extra]
    raise SystemExit(subprocess.call(cmd))
