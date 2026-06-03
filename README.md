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
| **PeopleOfMath → Import Portraits (Wikimedia)** | До 4 JPEG (мин. 2) в `Assets/Data/Images/{id}/`, лицензии PD / CC BY / CC BY-SA. Отчёт: `Assets/Data/import_report.txt`. |
| **PeopleOfMath → Refresh Repository List** | Собирает все SO в `MathematicianRepository` на открытой сцене. |
| **PeopleOfMath → Import All (...)** | Каталог + портреты + refresh подряд. |
| **PeopleOfMath → Regenerate Main Scene** | Пересобрать UI (галерея на карточке, список на 100 записей). |

Повторный импорт идемпотентен (пауза между запросами ~800 ms, повтор при HTTP 429). Без Unity можно создать пустые SO из каталога: `python Tools/generate_skeleton_assets.py` (или **PeopleOfMath → Create Catalog Assets (skeleton)**).

Если у математика меньше 2 фото на Commons — см. отчёт; можно положить файлы вручную в `Assets/Data/Images/{id}/01.jpg` … и **Reimport**, затем привязать спрайты в SO.

### EN переводы

Для новых карточек поля `*En` пустые; в UI используется fallback **EN → RU**. Английский можно дописать вручную в Inspector у `MathematicianData`.

### Лицензии изображений

Импорт принимает только **Public domain**, **CC BY**, **CC BY-SA** (без NC/ND). Подпись лицензии и источника показывается под галереей на карточке.

### Не видно изображений на карточке

1. Убедитесь, что портреты есть в `Assets/Resources/Portraits/{id}/01.jpg` (и `02.jpg`), либо в SO заполнен список `portraits`.
2. Запустите **PeopleOfMath → Import Portraits (Wikimedia)** (исправленный поиск по namespace File + Wikidata P18), затем **Link Portraits From Folders**.
3. Для быстрой проверки UI: **PeopleOfMath → Generate Placeholder Portraits (dev)** — цветные заглушки для всех 100 карточек.
4. Альтернатива без Unity: `python Tools/download_portraits_priority.py` (при 429 подождите и повторите), затем в Unity — **Link Portraits From Folders**.
5. Галерея подгружает спрайты из `Resources/Portraits/{id}`, если в SO поле `portraits` пустое.

## Сборка Android

1. **File → Build Settings** — платформа **Android**.
2. Сцена `Assets/Scenes/Main.unity` (index 0).
3. **Build** или **Build And Run**.

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
