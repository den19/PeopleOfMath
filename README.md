# PeopleOfMath

Справочник о математиках для Android. Unity **6000.4.5f1**, URP 2D, одна сцена `Main`.

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

1. **File → Build Settings** — платформа **Android**.
2. Сцена `Assets/Scenes/Main.unity` (index 0).
3. **Build** или **Build And Run** (Addressables пересобираются автоматически перед билдом).

Если меняли только строки локализации и APK собираете вручную: **Window → Asset Management → Addressables → Groups → Build → New Build**, затем снова **Build** APK. Старую версию приложения на устройстве удалите перед установкой.

Пакетный setup сцены (редактор закрыт):

```text
"C:\Program Files\Unity\Hub\Editor\6000.4.5f1\Editor\Unity.exe" -batchmode -quit -projectPath "C:\git\PeopleOfMath" -executeMethod PeopleOfMath.Editor.PeopleOfMathProjectSetup.RunBatch
```

## Функции

- До **100** математиков, биография на RU (из Wikipedia), EN — вручную или fallback.
- Фильтры: век, страна, раздел.
- Карточка: **галерея 2–4 портретов** (свайп / мышь), даты, страны, разделы, достижения, личная жизнь.
- Язык интерфейса RU / EN (Unity Localization).
- **Active Input Handling = Both**.

## Структура

- `Assets/Scripts` — логика и UI (`PortraitGalleryView`, `GalleryScrollSnap`)
- `Assets/Data/Mathematicians` — ScriptableObject карточки
- `Assets/Data/Images/{id}` — портреты
- `Assets/Data/mathematicians_catalog.json` — каталог импорта
- `Assets/Editor` — `MathematicianImportPipeline`, `WikimediaPortraitImporter`
- `Assets/Localization` — String Table UI
- `Assets/Scenes/Main.unity` — основная сцена
