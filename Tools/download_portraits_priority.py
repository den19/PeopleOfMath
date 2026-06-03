import importlib.util
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PRIORITY = [
    "pythagoras", "euclid", "archimedes", "newton", "euler", "gauss",
    "lobachevsky", "kovalevskaya", "poincare", "turing",
    "fermat", "galois", "riemann", "cantor", "hilbert",
]

spec = importlib.util.spec_from_file_location(
    "batch", ROOT / "Tools" / "download_portraits_batch.py"
)
batch = importlib.util.module_from_spec(spec)
spec.loader.exec_module(batch)

import json

catalog = json.loads(batch.CATALOG.read_text(encoding="utf-8"))
catalog["mathematicians"] = [e for e in catalog["mathematicians"] if e["id"] in PRIORITY]
tmp = ROOT / "Tools" / "_catalog_priority.json"
tmp.write_text(json.dumps(catalog, ensure_ascii=False, indent=2), encoding="utf-8")
batch.CATALOG = tmp
batch.main()
