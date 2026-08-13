# PeopleOfMath

Справочник о математиках для Android. Unity **6000.4.5f1**, URP 2D, одна сцена `Main`.

Архитектура runtime-кода, модель данных, UI и квиз: **[docs/PROGRAMMER.md](docs/PROGRAMMER.md)**.

## Требования

- Unity Hub с редактором **6000.4.5f1**
- Модуль **Android Build Support**
- Доступ в интернет для Editor-импорта текстов и портретов

## Открытие проекта

1. Клонировать репозиторий и открыть папку в Unity Hub.
2. Дождаться импорта пакетов (URP, Input System, Localization, TextMeshPro).
3. **Window → TextMeshPro → Import TMP Essential Resources** (если Unity предложит).
4. Если `Assets/Scenes/Main.unity` отсутствует: **PeopleOfMath → Regenerate Main Scene**.
5. Импорт контента (см. ниже), затем **PeopleOfMath → Refresh Repository List**.
6. Открыть `Assets/Scenes/Main.unity` и нажать **Play**.

## Импорт 100 математиков

Каталог: `Assets/Data/mathematicians_catalog.json` (100 записей: id, `wikiTitleRu`, теги фильтров, Wikidata).

| Меню | Действие |
|------|----------|
| **PeopleOfMath → Import Catalog (RU texts)** | Создаёт/обновляет SO в `Assets/Data/Mathematicians/` из ru.wikipedia. EN у 10 исходных карточек сохраняется. |
| **PeopleOfMath → Import Real Portraits (replace placeholders)** | Удаляет заглушки, скачивает реальные портреты в `Assets/Resources/Portraits/{id}/`, привязка к SO. **Основной шаг для продакшена.** |
| **PeopleOfMath → Import Portraits (Wikimedia)** | Дозагрузка без удаления уже существующих реальных файлов. |
| **PeopleOfMath → Import Portraits (empty folders only)** | Только папки без ≥2 реальных JPEG; паузы 2 s + retry при 429. |
| **PeopleOfMath → Resume Failed Portraits From Report** | Повторить id из `FAIL` / `WARN` в `import_report.txt`. |
| **PeopleOfMath → Link Portraits From Folders** | Привязать JPEG из `Resources/Portraits` к SO (пропускает заглушки). |
| **PeopleOfMath → Clear Placeholder Portraits In Resources** | Удалить только файлы-заглушки (&lt;25 KB / с маркером `.placeholder`). |
| **PeopleOfMath → Refresh Repository List** | Собирает все SO в `MathematicianRepository` на открытой сцене. |
| **PeopleOfMath → Import All (...)** | Каталог + **реальные** портреты + refresh. |
| **PeopleOfMath → Regenerate Main Scene** | Пересобрать UI (галерея на карточке, список на 100 записей). |

Повторный импорт идемпотентен: минимум **2 s** между HTTP-запросами, до **8** повторов с `Retry-After` и circuit breaker **90 s** при серии 429. Без Unity можно создать пустые SO из каталога: `python Tools/generate_skeleton_assets.py` (или **PeopleOfMath → Create Catalog Assets (skeleton)**).

Если у математика меньше 2 фото на Commons — см. отчёт; можно положить файлы вручную в `Assets/Resources/Portraits/{id}/01.jpg` … и **Link Portraits From Folders**.

**Заглушки (dev):** меню **Generate Placeholder Portraits (dev)** пишет в `Assets/Data/Placeholders/` (не в игру). Цветные полосы в `Resources/Portraits` — удалите через **Clear Placeholder Portraits** и запустите **Import Real Portraits**.

### EN переводы

Для новых карточек поля `*En` пустые; в UI используется fallback **EN → RU**. Английский можно дописать вручную в Inspector у `MathematicianData`.

### Лицензии изображений

Импорт принимает только **Public domain**, **CC BY**, **CC BY-SA** (без NC/ND). Подпись лицензии и источника показывается под галереей на карточке.

### Не видно изображений / видны цветные заглушки

1. **PeopleOfMath → Import Real Portraits (replace placeholders)** — основное решение.
2. Или: **Clear Placeholder Portraits In Resources** → **Import Real Portraits**.
3. Без Unity (медленно, с паузами): `python Tools/download_portraits_batch.py --empty-only` (или `python Tools/download_portraits_empty.py`), затем **Link Portraits From Folders** + **Fix Portrait Texture Import (Sprite)**.
4. Отчёты: Unity — `Assets/Data/import_report.txt`, Python — `Assets/Data/import_report_python.txt`.

### HTTP 429 (слишком много запросов)

1. **PeopleOfMath → Import Portraits (empty folders only)** — дозаполняет только пустые/неполные папки, не трогая уже готовые.
2. Или: `python Tools/download_portraits_batch.py --empty-only` (опционально `--ids newton,euler`).
3. Затем **Link Portraits From Folders**.
4. При обрыве: **Resume Failed Portraits From Report** или повторите шаг 1–2 (запуск идемпотентен).

## Сборка Android

Релизный пайплайн пишет APK в **корень проекта**: `com.densappstudio.peopleofmath.apk` (в `.gitignore`).

Каждый релизный билд:

1. Увеличивает последнюю цифру **Version** после последней точки (`1.1.39` → `1.1.40`).
2. Ставит **Bundle Version Code** в то же целое (`40`).
3. Подписывает keystore `C:/git/cloud/den.kolesov..keystore`, alias **`main`** (один пароль на keystore и alias).

Debug-подпись **отключена**: без пароля сборка падает.

### Секреты (один раз)

```powershell
copy Tools\keystore.local.ps1.example Tools\keystore.local.ps1
# отредактируйте: $KeystorePassword = "ваш_пароль"
```

Файл `Tools/keystore.local.ps1` в `.gitignore` — **не коммитить**. Альтернатива: `$env:ANDROID_KEYSTORE_PASS = "..."`.

### Быстрый способ (рекомендуется)

**В открытом Unity Editor:** меню **PeopleOfMath → Build Release APK (project root)** (нужен `keystore.local.ps1` или env).

**Из терминала** (Unity Editor **должен быть закрыт**):

```powershell
powershell -ExecutionPolicy Bypass -File Tools\build_apk.ps1
```

Опции скрипта:

- `-UnityPath "C:\Program Files\Unity\Hub\Editor\6000.4.5f1\Editor\Unity.exe"` — если Hub не в стандартном пути
- `-SkipEditorLockCheck` — если `Temp\UnityLockfile` остался после краша
- Лог: `build_apk.log` в корне проекта

Эквивалент вручную:

```text
"C:\Program Files\Unity\Hub\Editor\6000.4.5f1\Editor\Unity.exe" -batchmode -nographics -quit -projectPath "C:\git\PeopleOfMath" -executeMethod PeopleOfMath.Editor.AndroidApkBuilder.BuildFromBatch -logFile "C:\git\PeopleOfMath\build_apk.log"
```

Addressables (локализация) собираются вместе с player build (`BuildAddressablesWithPlayerBuild`).

### Классический UI Unity

1. **File → Build Settings** — платформа **Android**.
2. Сцена `Assets/Scenes/Main.unity` (index 0).
3. **Build** или **Build And Run** (версия/подпись — как в Player Settings; релизный бамп только через меню/скрипт выше).

Пакетный setup сцены (не APK; редактор закрыт):

```text
"C:\Program Files\Unity\Hub\Editor\6000.4.5f1\Editor\Unity.exe" -batchmode -quit -projectPath "C:\git\PeopleOfMath" -executeMethod PeopleOfMath.Editor.PeopleOfMathProjectSetup.RunBatch
```

**Агенту Cursor:** по фразе «собери APK» / «build apk» — только `Tools\build_apk.ps1` (релиз: бамп + подпись). Правило: `.cursor/rules/build-apk.mdc`. Автозапуск после правок не настроен.

### Установка по USB (ADB)

Сборка + `adb install -r` на подключённый телефон (package id `com.peopleofmath.app`):

```powershell
powershell -ExecutionPolicy Bypass -File Tools\install_apk_adb.ps1
```

Только установка уже собранного APK: `-SkipBuild`. Несколько устройств: `-Serial <id>`.

Нужны USB-кабель, отладка по USB и статус `device` (не `unauthorized` — Accept на телефоне). Скрипт сам ищет `adb.exe` (SDK / Unity AndroidPlayer).

При конфликте подписи:

```powershell
adb uninstall com.peopleofmath.app
powershell -ExecutionPolicy Bypass -File Tools\install_apk_adb.ps1 -SkipBuild
```

**В Unity Editor:** **PeopleOfMath → Android → Install / Build and Install APK via ADB (USB)** (build-and-install собирает в Editor, затем `-SkipBuild`).

**Агенту Cursor:** «установи APK» / «install apk» / «залей APK по usb» → `Tools\install_apk_adb.ps1`. Правило: `.cursor/rules/install-apk-adb.mdc`.

### Отправка по Bluetooth

```powershell
powershell -ExecutionPolicy Bypass -File Tools\deploy_apk_bluetooth.ps1
```

Известные телефоны: `TECNO POVA 7 Ultra 5G` (`-Phone pova`) и `TECNO CAMON 20 Pro` (`-Phone camon`); без `-Phone` пробуются оба. Headless OBEX, при ошибке — `fsquirt.exe`. На телефоне нужно **Accept**. Только отправка без сборки: `-SkipBuild`.

**В Unity Editor:** **PeopleOfMath → Android → Deploy / Build and Deploy APK via Bluetooth**.

**Агенту Cursor:** «отправь APK» / «send apk» → `Tools\deploy_apk_bluetooth.ps1`. Правило: `.cursor/rules/deploy-apk-bluetooth.mdc`.

## Функции

- До **100** математиков, биография на RU (из Wikipedia), EN — вручную или fallback.
- Фильтры: век, страна, раздел.
- Карточка: **галерея 2–4 портретов** (свайп / мышь), даты, страны, разделы, достижения, личная жизнь.
- Язык интерфейса RU / EN (Unity Localization).
- **Active Input Handling = Both**.

## Структура

- `Assets/Scripts` — логика и UI (см. [документацию программиста](docs/PROGRAMMER.md))
- `Assets/Data/Mathematicians` — ScriptableObject карточки
- `Assets/Resources/Portraits/{id}` — портреты runtime
- `Assets/Data/mathematicians_catalog.json` — каталог импорта
- `Assets/Editor` — `MathematicianImportPipeline`, `WikimediaPortraitImporter`, `AndroidApkBuilder`, `AndroidDeployMenu`
- `Tools/build_apk.ps1` — релизный APK (бамп + подпись) → `com.densappstudio.peopleofmath.apk`
- `Tools/install_apk_adb.ps1` — сборка + установка по USB (`adb install -r`)
- `Tools/deploy_apk_bluetooth.ps1` — сборка + отправка по Bluetooth (OBEX / fsquirt)
- `Tools/keystore.local.ps1.example` — шаблон пароля keystore (локальный `.ps1` в gitignore)
- `Assets/Localization` — String Table UI
- `Assets/Scenes/Main.unity` — основная сцена
- `docs/PROGRAMMER.md` — документация для разработчиков
