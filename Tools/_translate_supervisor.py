# -*- coding: utf-8 -*-
"""Process one mathematician asset per subprocess until all En fields are filled."""
from __future__ import annotations

import json
import subprocess
import sys
import time
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TOOLS = Path(__file__).resolve().parent
sys.path.insert(0, str(TOOLS))

from _inventory_en_fields import ASSETS, FIELDS, extract_field, is_empty  # noqa: E402

PYTHON = sys.executable
TRANSLATE = TOOLS / "translate_en_fields.py"
LOG = TOOLS / "_en_supervisor.log"


def needing_ids() -> list[str]:
    ids: list[str] = []
    for path in sorted(ASSETS.glob("*.asset")):
        text = path.read_text(encoding="utf-8")
        for f in FIELDS:
            ru = extract_field(text, f + "Ru")
            en = extract_field(text, f + "En")
            if not is_empty(ru) and is_empty(en):
                ids.append(path.stem)
                break
    return ids


def log(msg: str) -> None:
    line = msg + "\n"
    sys.stdout.write(line)
    sys.stdout.flush()
    with LOG.open("a", encoding="utf-8") as fh:
        fh.write(line)


def main() -> int:
    round_no = 0
    stall = 0
    while True:
        round_no += 1
        ids = needing_ids()
        log(f"==== SUPERVISOR ROUND {round_no}: {len(ids)} assets needing EN ====")
        if not ids:
            log("COMPLETE")
            return 0

        before = len(ids)
        for asset_id in ids:
            log(f"-- subprocess {asset_id}")
            try:
                proc = subprocess.run(
                    [PYTHON, "-u", str(TRANSLATE), "--ids", asset_id],
                    cwd=str(ROOT),
                    capture_output=True,
                    text=True,
                    encoding="utf-8",
                    errors="replace",
                    timeout=900,
                )
                if proc.stdout:
                    log(proc.stdout.rstrip())
                if proc.stderr:
                    log("STDERR: " + proc.stderr.rstrip())
                log(f"exit={proc.returncode}")
            except subprocess.TimeoutExpired:
                log(f"TIMEOUT {asset_id}")
            except Exception as ex:  # noqa: BLE001
                log(f"EXCEPTION {asset_id}: {ex}")
            time.sleep(1.0)

        after = len(needing_ids())
        log(f"progress {before} -> {after}")
        if after >= before:
            stall += 1
            log(f"no progress stall={stall}; sleeping 20s")
            time.sleep(20)
            if stall >= 8:
                log("ABORT: stalled too many rounds")
                return 1
        else:
            stall = 0


if __name__ == "__main__":
    raise SystemExit(main())
