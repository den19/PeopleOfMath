---
name: ADB Install Pipeline
overview: Пайплайн build → adb install -r по USB, Unity-меню PeopleOfMath/Android для запуска скриптов, эталонные ADB_PIPELINE.en.md / .ru.md в c:\\git\\pipeline\\peopleofmath.
todos:
  - id: adb-ps1
    content: Создать Tools/install_apk_adb.ps1 (resolve adb, build, install -r, device checks)
    status: completed
  - id: unity-menu
    content: Assets/Editor/AndroidDeployMenu.cs — PeopleOfMath/Android меню ADB + Bluetooth
    status: completed
  - id: cursor-rule-adb
    content: Добавить .cursor/rules/install-apk-adb.mdc
    status: completed
  - id: adb-docs
    content: Написать ADB_PIPELINE.en.md и ADB_PIPELINE.ru.md в c:\git\pipeline\peopleofmath
    status: completed
isProject: false
---

# ADB APK Install Pipeline

## Goal

По явной команде («установи APK» / `install_apk_adb.ps1` / Unity-меню):

1. Собрать release APK через [`Tools/build_apk.ps1`](c:\git\PeopleOfMath\Tools\build_apk.ps1) (если не `-SkipBuild`).
2. Установить `{projectRoot}/com.densappstudio.peopleofmath.apk` на телефон по USB: `adb install -r`.
3. Дать Unity-меню для install-only, build+install и Bluetooth-deploy (скрипты из предыдущего пайплайна).

Package id в проекте: `com.peopleofmath.app` (имя файла APK другое — для install важен путь к `.apk`, не package id).

## Current environment (facts)

- `adb` не в PATH; рабочие пути:
  - `C:\Users\den\AppData\Local\Android\Sdk\platform-tools\adb.exe`
  - Unity `6000.4.5f1` … `AndroidPlayer\SDK\platform-tools\adb.exe`
- Сейчас устройство: `1445125581103151` — **unauthorized**. Пайплайн должен явно падать с текстом «разреши USB debugging на телефоне», пока статус не `device`.

## Artifacts

| Path | Role |
|------|------|
| [`Tools/install_apk_adb.ps1`](c:\git\PeopleOfMath\Tools\install_apk_adb.ps1) | Оркестратор: resolve adb → (build) → `adb devices` → `adb install -r` |
| [`Assets/Editor/AndroidDeployMenu.cs`](c:\git\PeopleOfMath\Assets\Editor\AndroidDeployMenu.cs) | Unity-меню PeopleOfMath/Android/… |
| [`.cursor/rules/install-apk-adb.mdc`](c:\git\PeopleOfMath\.cursor\rules\install-apk-adb.mdc) | Agent: «установи APK» → скрипт |
| [`c:\git\pipeline\peopleofmath\ADB_PIPELINE.en.md`](c:\git\pipeline\peopleofmath\ADB_PIPELINE.en.md) | Эталон EN |
| [`c:\git\pipeline\peopleofmath\ADB_PIPELINE.ru.md`](c:\git\pipeline\peopleofmath\ADB_PIPELINE.ru.md) | Эталон RU |

## `install_apk_adb.ps1` behavior

Params: `-SkipBuild`, `-ApkPath`, `-ProjectPath`, `-AdbPath`, `-Serial`, `-SkipEditorLockCheck`.

1. Resolve `adb.exe` (first hit wins):
   - `-AdbPath` / `$env:ADB`
   - `$env:ANDROID_HOME` / `$env:ANDROID_SDK_ROOT` + `\platform-tools\adb.exe`
   - `%LOCALAPPDATA%\Android\Sdk\platform-tools\adb.exe`
   - Unity Hub Editor `*\AndroidPlayer\SDK\platform-tools\adb.exe` matching project Unity version from `ProjectSettings/ProjectVersion.txt`
2. Unless `-SkipBuild`: call `Tools\build_apk.ps1` (same as Bluetooth orchestrator).
3. Require APK exists.
4. `adb start-server`; `adb devices`.
   - 0 devices → fail («подключи USB, включи USB debugging»).
   - any `unauthorized` → fail («на телефоне Accept / разреши отладку»).
   - if `-Serial` set → use `-s`; else if >1 `device` → fail with list; else use the single device.
5. `adb [-s SERIAL] install -r "<apk>"` → exit code from adb; print success.

Default flow matches Bluetooth: **build then install**.

## Unity menu ([`AndroidDeployMenu.cs`](c:\git\PeopleOfMath\Assets\Editor\AndroidDeployMenu.cs))

Under **PeopleOfMath/Android/** (priority ~50 so near Build Release APK):

| Menu | Action |
|------|--------|
| `Install APK via ADB (USB)` | Run `install_apk_adb.ps1 -SkipBuild` via `Process` (powershell), wait, show dialog with stdout/stderr tail |
| `Build and Install APK via ADB (USB)` | Prefer in-Editor: `AndroidApkBuilder.BuildApk(false)` then same adb install helper (avoids Unity lock / second Editor). If build fails → dialog. |
| `Deploy APK via Bluetooth` | Run `deploy_apk_bluetooth.ps1 -SkipBuild` |
| `Build and Deploy APK via Bluetooth` | Run `deploy_apk_bluetooth.ps1` (full build+OBEX; warn Editor should be closed for batch build — for this item use `-SkipBuild` after in-Editor `BuildApk`, then bluetooth push only, to avoid lock) |

Concrete choice for Bluetooth menu items: **in-Editor `BuildApk` when «Build and …», then PowerShell with `-SkipBuild`** for both ADB and Bluetooth — so Editor stays open and lock is not an issue.

Shared helpers in the same file: resolve adb (mirror PS logic), run powershell script, collect exit code + log, `EditorUtility.DisplayDialog`.

## Cursor rule

Triggers: «установи APK», «install apk», «adb install», «залей APK по usb».

Steps: check Editor lock only if building; ensure keystore when building; run `install_apk_adb.ps1`; if unauthorized — tell user to accept on phone; report adb output.

## Docs (same shape as RELEASE / BLUETOOTH)

Goal, artifacts, parameters, flow diagram, prerequisites (USB, debugging, authorize RSA), how to run (CLI + Unity menu + agent), porting checklist, do-not (no auto-deploy after every edit; no silent debug signing).

## Out of scope

- Changing package id / signing / version bump logic.
- Wireless ADB pairing.
- Uninstall / clear data unless install fails due to signature mismatch — then document `adb uninstall com.peopleofmath.app` as manual recovery in the pipeline doc only.