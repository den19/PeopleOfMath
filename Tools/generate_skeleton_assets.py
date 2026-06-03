import json
import uuid
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CATALOG = ROOT / "Assets" / "Data" / "mathematicians_catalog.json"
OUT = ROOT / "Assets" / "Data" / "Mathematicians"
SCRIPT_GUID = "a672b690bf32c7d4bb64dcb90c19974b"

TEMPLATE = """%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 0}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {script_guid}, type: 3}}
  m_Name: {id}
  m_EditorClassIdentifier: PeopleOfMath::PeopleOfMath.Data.MathematicianData
  id: {id}
  wikiTitleRu: {wiki_title}
  wikidataId: {wikidata}
  fullNameRu: {wiki_title}
  fullNameEn: 
  birthDate: 
  deathDate: 
  countryKeys:
{countries}
  centuryKeys:
{centuries}
  branchKeys:
{branches}
  achievementsRu: 
  achievementsEn: 
  personalLifeRu: 
  personalLifeEn: 
  shortBioRu: 
  shortBioEn: 
  wikipediaUrlRu: 
  portraits: []
"""

META_TEMPLATE = """fileFormatVersion: 2
guid: {guid}
NativeFormatImporter:
  externalObjects: {{}}
  mainObjectFileID: 11400000
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""


def yaml_quote(s: str) -> str:
    if not s:
        return '""'
    if any(c in s for c in ':[]{}#&*!|>\'"%@`'):
        esc = s.replace("\\", "\\\\").replace('"', '\\"')
        return f'"{esc}"'
    return s


def list_block(key, items):
    if not items:
        return f"  {key}: []"
    lines = [f"  {key}:"]
    for item in items:
        lines.append(f"  - {item}")
    return "\n".join(lines)


def main():
    data = json.loads(CATALOG.read_text(encoding="utf-8"))
    created = 0
    skipped = 0
    for entry in data["mathematicians"]:
        asset_path = OUT / f"{entry['id']}.asset"
        if asset_path.exists():
            skipped += 1
            continue

        countries = list_block("countryKeys", entry.get("countryKeys", []))
        centuries = list_block("centuryKeys", entry.get("centuryKeys", []))
        branches = list_block("branchKeys", entry.get("branchKeys", []))
        title = yaml_quote(entry["wikiTitleRu"])
        wd = entry.get("wikidataId") or ""

        body = TEMPLATE.format(
            script_guid=SCRIPT_GUID,
            id=entry["id"],
            wiki_title=title,
            wikidata=wd,
            countries=countries,
            centuries=centuries,
            branches=branches,
        )
        asset_path.write_text(body, encoding="utf-8")
        meta_path = asset_path.with_suffix(".asset.meta")
        if not meta_path.exists():
            meta_path.write_text(
                META_TEMPLATE.format(guid=uuid.uuid4().hex), encoding="utf-8"
            )
        created += 1

    print(f"Created {created}, skipped {skipped}, total catalog {len(data['mathematicians'])}")


if __name__ == "__main__":
    main()
