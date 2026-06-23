#!/usr/bin/env python3
"""Add interestingFactsRu/En to mathematician assets and create InterestingFacts prefab."""

import glob
import uuid
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DETAIL_DIR = ROOT / "Assets" / "Prefabs" / "UI" / "Detail"
DATA_DIR = ROOT / "Assets" / "Data" / "Mathematicians"
SCENE_PATH = ROOT / "Assets" / "Scenes" / "Main.unity"


def create_prefab() -> str:
    src = DETAIL_DIR / "DetailSection_PersonalLife.prefab"
    dst = DETAIL_DIR / "DetailSection_InterestingFacts.prefab"
    meta_path = dst.with_suffix(".prefab.meta")

    if meta_path.exists():
        guid = None
        for line in meta_path.read_text(encoding="utf-8").splitlines():
            if line.startswith("guid: "):
                guid = line.split("guid: ", 1)[1].strip()
                break
        if guid is None:
            raise SystemExit("Existing prefab meta missing guid")
        if not dst.exists():
            content = src.read_text(encoding="utf-8")
            content = content.replace("DetailSection_PersonalLife", "DetailSection_InterestingFacts")
            content = content.replace("sectionKind: 1", "sectionKind: 3")
            dst.write_text(content, encoding="utf-8")
        return guid

    content = src.read_text(encoding="utf-8")
    content = content.replace("DetailSection_PersonalLife", "DetailSection_InterestingFacts")
    content = content.replace("sectionKind: 1", "sectionKind: 3")
    dst.write_text(content, encoding="utf-8")

    guid = uuid.uuid4().hex
    meta = (
        "fileFormatVersion: 2\n"
        f"guid: {guid}\n"
        "PrefabImporter:\n"
        "  externalObjects: {}\n"
        "  userData: \n"
        "  assetBundleName: \n"
        "  assetBundleVariant: \n"
    )
    (dst.with_suffix(".prefab.meta")).write_text(meta, encoding="utf-8")
    return guid


def update_assets() -> int:
    count = 0
    for path in sorted(DATA_DIR.glob("*.asset")):
        text = path.read_text(encoding="utf-8")
        if "interestingFactsRu" in text:
            continue
        if "  shortBioRu:" not in text:
            print(f"SKIP {path.name}: shortBioRu not found")
            continue
        text = text.replace(
            "  shortBioRu:",
            "  interestingFactsRu:\n  interestingFactsEn:\n  shortBioRu:",
            1,
        )
        path.write_text(text, encoding="utf-8")
        count += 1
    return count


def patch_scene(prefab_guid: str) -> None:
    text = SCENE_PATH.read_text(encoding="utf-8")
    if "1755000003" in text:
        print("Scene already patched")
        return

    text = text.replace(
        "  - {fileID: 964450171}\n  m_Father: {fileID: 1403506682}",
        "  - {fileID: 964450171}\n  - {fileID: 1755000002}\n  m_Father: {fileID: 1403506682}",
        1,
    )
    text = text.replace(
        "  - {fileID: 964450172}\n  headerTitle:",
        "  - {fileID: 964450172}\n  - {fileID: 1755000003}\n  headerTitle:",
        1,
    )

    insert_after = (
        "  m_EditorClassIdentifier: PeopleOfMath::PeopleOfMath.UI.ScrollTextDetailSection\n"
        "--- !u!1 &1016940498"
    )
    block = f"""  m_EditorClassIdentifier: PeopleOfMath::PeopleOfMath.UI.ScrollTextDetailSection
--- !u!1001 &1755000001
PrefabInstance:
  m_ObjectHideFlags: 0
  serializedVersion: 2
  m_Modification:
    serializedVersion: 3
    m_TransformParent: {{fileID: 161506810}}
    m_Modifications:
    - target: {{fileID: 3243653931323317716, guid: {prefab_guid}, type: 3}}
      propertyPath: m_fontSize
      value: 50
      objectReference: {{fileID: 0}}
    - target: {{fileID: 3243653931323317716, guid: {prefab_guid}, type: 3}}
      propertyPath: m_fontSizeBase
      value: 50
      objectReference: {{fileID: 0}}
    - target: {{fileID: 6990884093317836337, guid: {prefab_guid}, type: 3}}
      propertyPath: m_Name
      value: DetailSection_InterestingFacts
      objectReference: {{fileID: 0}}
    - target: {{fileID: 6990884093317836337, guid: {prefab_guid}, type: 3}}
      propertyPath: m_IsActive
      value: 0
      objectReference: {{fileID: 0}}
    - target: {{fileID: 8007036512175896892, guid: {prefab_guid}, type: 3}}
      propertyPath: m_Pivot.x
      value: 0.5
      objectReference: {{fileID: 0}}
    - target: {{fileID: 8007036512175896892, guid: {prefab_guid}, type: 3}}
      propertyPath: m_Pivot.y
      value: 0.5
      objectReference: {{fileID: 0}}
    - target: {{fileID: 8007036512175896892, guid: {prefab_guid}, type: 3}}
      propertyPath: m_AnchorMax.x
      value: 1
      objectReference: {{fileID: 0}}
    - target: {{fileID: 8007036512175896892, guid: {prefab_guid}, type: 3}}
      propertyPath: m_AnchorMax.y
      value: 1
      objectReference: {{fileID: 0}}
    - target: {{fileID: 8007036512175896892, guid: {prefab_guid}, type: 3}}
      propertyPath: m_AnchorMin.x
      value: 0
      objectReference: {{fileID: 0}}
    - target: {{fileID: 8007036512175896892, guid: {prefab_guid}, type: 3}}
      propertyPath: m_AnchorMin.y
      value: 0
      objectReference: {{fileID: 0}}
    - target: {{fileID: 8007036512175896892, guid: {prefab_guid}, type: 3}}
      propertyPath: m_SizeDelta.x
      value: 0
      objectReference: {{fileID: 0}}
    - target: {{fileID: 8007036512175896892, guid: {prefab_guid}, type: 3}}
      propertyPath: m_SizeDelta.y
      value: 0
      objectReference: {{fileID: 0}}
    - target: {{fileID: 8007036512175896892, guid: {prefab_guid}, type: 3}}
      propertyPath: m_LocalPosition.x
      value: 0
      objectReference: {{fileID: 0}}
    - target: {{fileID: 8007036512175896892, guid: {prefab_guid}, type: 3}}
      propertyPath: m_LocalPosition.y
      value: 0
      objectReference: {{fileID: 0}}
    - target: {{fileID: 8007036512175896892, guid: {prefab_guid}, type: 3}}
      propertyPath: m_LocalPosition.z
      value: 0
      objectReference: {{fileID: 0}}
    - target: {{fileID: 8007036512175896892, guid: {prefab_guid}, type: 3}}
      propertyPath: m_LocalRotation.w
      value: 1
      objectReference: {{fileID: 0}}
    - target: {{fileID: 8007036512175896892, guid: {prefab_guid}, type: 3}}
      propertyPath: m_LocalRotation.x
      value: 0
      objectReference: {{fileID: 0}}
    - target: {{fileID: 8007036512175896892, guid: {prefab_guid}, type: 3}}
      propertyPath: m_LocalRotation.y
      value: 0
      objectReference: {{fileID: 0}}
    - target: {{fileID: 8007036512175896892, guid: {prefab_guid}, type: 3}}
      propertyPath: m_LocalRotation.z
      value: 0
      objectReference: {{fileID: 0}}
    - target: {{fileID: 8007036512175896892, guid: {prefab_guid}, type: 3}}
      propertyPath: m_AnchoredPosition.x
      value: 0
      objectReference: {{fileID: 0}}
    - target: {{fileID: 8007036512175896892, guid: {prefab_guid}, type: 3}}
      propertyPath: m_AnchoredPosition.y
      value: 0
      objectReference: {{fileID: 0}}
    - target: {{fileID: 8007036512175896892, guid: {prefab_guid}, type: 3}}
      propertyPath: m_LocalEulerAnglesHint.x
      value: 0
      objectReference: {{fileID: 0}}
    - target: {{fileID: 8007036512175896892, guid: {prefab_guid}, type: 3}}
      propertyPath: m_LocalEulerAnglesHint.y
      value: 0
      objectReference: {{fileID: 0}}
    - target: {{fileID: 8007036512175896892, guid: {prefab_guid}, type: 3}}
      propertyPath: m_LocalEulerAnglesHint.z
      value: 0
      objectReference: {{fileID: 0}}
    - target: {{fileID: 8405752219745710103, guid: {prefab_guid}, type: 3}}
      propertyPath: m_fontSize
      value: 60
      objectReference: {{fileID: 0}}
    - target: {{fileID: 8405752219745710103, guid: {prefab_guid}, type: 3}}
      propertyPath: m_fontSizeBase
      value: 60
      objectReference: {{fileID: 0}}
    m_RemovedComponents: []
    m_RemovedGameObjects: []
    m_AddedGameObjects: []
    m_AddedComponents: []
  m_SourcePrefab: {{fileID: 100100000, guid: {prefab_guid}, type: 3}}
--- !u!224 &1755000002 stripped
RectTransform:
  m_CorrespondingSourceObject: {{fileID: 8007036512175896892, guid: {prefab_guid}, type: 3}}
  m_PrefabInstance: {{fileID: 1755000001}}
  m_PrefabAsset: {{fileID: 0}}
--- !u!114 &1755000003 stripped
MonoBehaviour:
  m_CorrespondingSourceObject: {{fileID: 3128653467366331772, guid: {prefab_guid}, type: 3}}
  m_PrefabInstance: {{fileID: 1755000001}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 0}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: 78447b61662eb8d40b93f694f0bfb12c, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: PeopleOfMath::PeopleOfMath.UI.ScrollTextDetailSection
--- !u!1 &1016940498"""

    if insert_after not in text:
        raise SystemExit("Scene insert marker not found")
    text = text.replace(insert_after, block, 1)
    SCENE_PATH.write_text(text, encoding="utf-8")


def main() -> None:
    guid = create_prefab()
    assets = update_assets()
    patch_scene(guid)
    print(f"Prefab guid: {guid}")
    print(f"Assets updated: {assets}")


if __name__ == "__main__":
    main()
