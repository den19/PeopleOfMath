# Документация программиста — PeopleOfMath

Справочное Android-приложение о математиках. Этот документ описывает архитектуру runtime-кода, модель данных, UI, квиз и соглашения. Операции импорта контента и сборки — в корневом [README.md](../README.md).

| | |
|---|---|
| Unity | **6000.4.5f1** (URP 2D) |
| Платформа | Android (`com.peopleofmath.app`, min SDK 25, IL2CPP, arm64) |
| Сцена | `Assets/Scenes/Main.unity` |
| Assembly | `Assets/Scripts/PeopleOfMath.asmdef` → namespace `PeopleOfMath.*` |

---

## 1. Обзор архитектуры

```
AppBootstrap
    → Locale / Font / Theme / Favorites
    → NavigationController.ShowHome()
    → OnboardingOverlay (если нужно)

NavigationController  ← стек ScreenContext
    ├── HomePanel / IndexPanel / ListPanel / DetailPanel
    ├── FavoritesPanel / QuizPanel / SettingsPanel / AboutPanel
    └── MathematicianRepository.All  ← ScriptableObject карточки
```

**Принципы:**

- Одна основная сцена; панели включаются/выключаются навигацией.
- Контент — ScriptableObject (`MathematicianData`), не Localization tables.
- UI-строки (кнопки, пустые состояния) — Unity Localization String Table `UI`.
- Пользовательские настройки — PlayerPrefs + статические хелперы с событиями.

---

## 2. Структура репозитория

| Путь | Назначение |
|------|------------|
| `Assets/Scripts/Core/` | Старт приложения, контекст экрана, сброс данных |
| `Assets/Scripts/Data/` | Модель, репозиторий, фильтры, поиск, индекс, портреты, избранное |
| `Assets/Scripts/UI/` | Панели, тема, glass, навигация, виджеты |
| `Assets/Scripts/Quiz/` | Генерация раунда, промпты, статистика |
| `Assets/Scripts/Localization/` | `LocaleHelper`, `UiStrings` |
| `Assets/Scripts/Input/` | Android Back / Escape |
| `Assets/Scripts/Sharing/` | Share / clipboard |
| `Assets/Scripts/Text/` | Markdown → TMP, orphans |
| `Assets/Editor/` | Меню `PeopleOfMath/…`, импорт, патчи сцены, `AndroidApkBuilder`, `AndroidDeployMenu` |
| `Tools/build_apk.ps1` | Релизный APK: бамп версии + подпись `main` → `com.densappstudio.peopleofmath.apk` |
| `Tools/install_apk_adb.ps1` | Сборка + `adb install -r` по USB (`com.peopleofmath.app`) |
| `Tools/deploy_apk_bluetooth.ps1` | Сборка + Bluetooth OBEX / fallback `fsquirt` |
| `Tools/keystore.local.ps1.example` | Шаблон пароля; рабочий файл — gitignored `keystore.local.ps1` |
| `Assets/Data/Mathematicians/` | SO-карточки (`{id}.asset`) |
| `Assets/Data/mathematicians_catalog.json` | Источник импорта |
| `Assets/Resources/Portraits/{id}/` | JPEG портретов для runtime |
| `Assets/Resources/MathematicianCatalog.asset` | Fallback-список для репозитория |
| `Assets/Localization/` | Locales `ru` / `en`, таблица `UI` |
| `Assets/Prefabs/UI/` | Префабы списков, тегов, секций детали |
| `Assets/Shaders/` | Frosted glass / blur |
| `Tools/` | Python-хелперы (скелеты, портреты, EN-перевод) |

---

## 3. Запуск и навигация

### 3.1. Bootstrap

`PeopleOfMath.Core.AppBootstrap` (`Assets/Scripts/Core/AppBootstrap.cs`):

1. `LocaleHelper.InitializeLocale()`
2. `FontSizeHelper.Initialize()`
3. `ThemeHelper.Initialize()`
4. `FavoritesHelper.Initialize()`
5. `navigation.ShowHome()`
6. `OnboardingOverlay.TryShow(...)` при первом запуске

Флаг `_initialized` защищает от повторного старта при domain reload в Editor.

### 3.2. Навигация

`NavigationController` лежит в `Assets/Scripts/UI/NavigationController.cs`, namespace — `PeopleOfMath.Core`.

Стек: `List<ScreenContext>`. Методы:

| API | Поведение |
|-----|-----------|
| `ShowHome` / `ShowIndex` / `ShowSettings` / `ShowQuiz` / `ShowAbout` | Корень стека (`SetRoot`) |
| `ShowList(FilterKind, key)` | Push фильтрованного списка |
| `ShowSearch(query)` | Push/замена списка поиска |
| `ShowDetail(id)` | Push карточки |
| `ShowFavorites` | Push + slide-анимация |
| `HandleBack` | Pop / особые случаи (секции детали, раунд квиза) |

Экраны (`AppScreen`): Home, Index, List, Detail, Settings, Favorites, Quiz, About.

`DetailOrigin` влияет на подсветку нижней вкладки, когда открыта деталь (откуда пришли: Home / Index / Favorites / Quiz / Search / FilterList).

`ScreenContext` — структура-фабрика (`Home()`, `ListFilter(...)`, `Detail(...)` и т.д.) в `Assets/Scripts/Core/ScreenContext.cs`.

### 3.3. Аппаратная кнопка «Назад»

`BackButtonHandler` слушает Escape (в т.ч. Android Back через Input System Keyboard):

- не Home → `navigation.HandleBack()` / переход на Home для Settings/Index/About;
- Home → двойное нажатие за окно подтверждения → `Application.Quit()` + toast.

---

## 4. Модель данных

### 4.1. `MathematicianData`

ScriptableObject, меню создания: **PeopleOfMath → Mathematician Data**.

Ключевые поля:

- идентификаторы: `id`, `wikiTitleRu`, `wikidataId`;
- имена / даты: `fullNameRu/En`, `birthDate`, `deathDate`;
- таксономия: `countryKeys`, `centuryKeys`, `branchKeys` (строковые ключи);
- тексты RU/EN: `shortBio*`, `achievements*`, `personalLife*`, `interestingFacts*`;
- `wikipediaUrlRu`, `portraits` (`List<PortraitEntry>`).

Геттеры локали:

- `GetFullName`, `GetShortBio`, … — с **fallback EN→RU** (если EN пустой);
- вариант без fallback (`fallbackToOtherLocale: false` / `PickExact`) — для квиза фактов.

Не добавляйте новые ключи таксономии в SO без записи в `Taxonomy`.

### 4.2. Репозиторий и каталог

| Класс | Роль |
|-------|------|
| `MathematicianRepository` | MonoBehaviour на сцене: `All`, `GetById` |
| `MathematicianCatalog` | SO со списком ссылок; `Resources.Load("MathematicianCatalog")` если список репозитория пуст |

После добавления/удаления карточек: **PeopleOfMath → Refresh Repository List**.

### 4.3. Таксономия

`Taxonomy` — статические словари `Countries`, `Centuries`, `Branches` с `LabelPair` (Ru/En).  
Ключи вроде `greece`, `19`, `geometry`. Отображение: `JoinLabels(...)`.

`FilterKind`: `Century`, `Country`, `Branch`.

### 4.4. Сервисы выборки

| Сервис | Файл | Назначение |
|--------|------|------------|
| `FilterService` | `Data/FilterService.cs` | `Filter`, `Count`, `CountAll` |
| `SearchService` | `Data/SearchService.cs` | Ранжированный поиск (имя → bio → achievements → branch) |
| `IndexService` | `Data/IndexService.cs` | Алфавит A–Z / А–Я, `#` для «прочее», `FilterByLetter` |

### 4.5. Порреты

- Файлы: `Assets/Resources/Portraits/{id}/01.jpg`, …
- Runtime: `PortraitResolver.GetPrimaryPortrait`, `ResolveGalleryPortraits`, `LoadPortraitsFromResources`
- Заглушки помечаются TextAsset `*.placeholder` и **не** показываются в галерее
- Атрибуция/лицензия: поля `PortraitEntry` + подпись в галерее

### 4.6. Избранное

`FavoritesHelper` — PlayerPrefs `favorite_mathematician_ids`, события `FavoritesChanged`.

---

## 5. UI

### 5.1. Панели

| Панель | Файл | Задача |
|--------|------|--------|
| `HomePanel` | плитки век/страна/раздел (`AdaptiveBrowseGrid`), поиск, вход в квиз |
| `IndexPanel` | буквенный индекс |
| `ListPanel` | результаты фильтра или поиска |
| `DetailPanel` | постраничные секции карточки |
| `FavoritesPanel` | избранное + stagger reveal; empty state — §5.2.2 |
| `QuizPanel` | меню → игра → feedback → результаты |
| `SettingsPanel` | язык, шрифт, тема, сброс |
| `AboutPanel` | о приложении |

Паттерн: `[SerializeField]` на `NavigationController` + `MathematicianRepository`; подписка на locale/theme/font в `OnEnable` / отписка в `OnDisable`.

Префабы списков при отсутствии ссылки: `Resources.Load` (`MathematicianListItem`, `CategoryTile`).

### 5.2. Сетка Справочника (Home)

Двухколоночные плитки фильтров (`CenturyGroup` / `CountryGroup` / `BranchGroup`) на узких/высоких телефонах не должны вылезать за правый край.

| Файл | Роль |
|------|------|
| `CategoryTileMetrics` | Базовый размер ячейки (492×460), spacing 20, 2 колонки; метрики подписей |
| `AdaptiveBrowseGrid` | Runtime: подгоняет `GridLayoutGroup.cellSize` под доступную ширину |
| `CategoryTile` prefab | Плитка; `LayoutElement` — только preferred, **без** жёсткого `minWidth` |
| `HomeListPanelLayout` / `UiLayoutMetrics` | Editor: padding скролла (32), grid/группы, empty state (§5.2.2) |

**Якоря внутри плитки.** `Media` — fractional top (`anchorMin.y = 1 − MediaHeightRatio`); `Label` / `Count` — от **низа** ячейки (`LabelTopFromBottom` / `CountTopFromBottom`). Так счётчик `(n)` и название остаются внутри плитки, когда `AdaptiveBrowseGrid` сжимает `cellSize`. Не возвращать top-fixed offsets вроде `y = −408` — при меньшей высоте текст уезжает в gap между рядами.

**Как считается ширина.** `AdaptiveBrowseGrid` берёт внутреннюю ширину родителя (минус padding `VerticalLayoutGroup` у `Content`), вычитает spacing и 2 px slack, затем `Floor` на колонку — чтобы `2×cell + spacing` не переполнял rect из‑за округления.

**Когда вызывается.** `OnEnable`, `OnRectTransformDimensionsChange`, и дешёвый `LateUpdate` (только при смене ширины). `LateUpdate` нужен: после `CanvasScaler` (match 0.5) layout-driven смена `sizeDelta` не всегда приходит через `OnRectTransformDimensionsChange`, особенно в первый кадр. `HomePanel` после спавна плиток делает `ForceRebuildLayoutImmediate` + `Apply()`.

**Не ломать.** Не ставить у `CategoryTile` фиксированный `minWidth`/`minHeight` (= базовой ячейке) — это снова раздувает preferred-ширину сетки шире viewport. Не убирать `LateUpdate` «ради оптимизации» без замены на другой гарантированный хук после layout.

Метрики редактора: `PeopleOfMath → Patch Home Category Tiles` (`EnsureHomeGridGroup` вешает `AdaptiveBrowseGrid` на группы).

### 5.2.1. BottomBar Safe Area

`BottomBarSafeArea` на `BottomBar` поднимает таббар и подписи над bottom inset. Высота бара = `148 + inset`, HLG `padding.bottom` растёт на inset, `ContentArea.offsetMin.y` = той же сумме.

**Inset.** Берётся `max(Screen.safeArea.y → canvas, MinBottomCornerInset=20)`: на многих Android скругления **не** входят в `safeArea`, и без floor подписи остаются в зоне угла.

**HLG бара.** Left/right = `HorizontalLayoutPadding` (18) — только на BottomBar, чтобы крайние вкладки не заходили в физические углы. Top/base bottom = `BaseLayoutPadding` (4).

**ContentArea — только вертикально.** Не паддить `ContentArea` слева/справа под safe area — горизонтальный inset сужает viewport и рискует снова вытолкнуть вторую колонку tiles; ширину сетки держит только `AdaptiveBrowseGrid`.

Метрики редактора: `PeopleOfMath → Fix Bottom Bar Layout` / `Patch Bottom Tab Bar` вешают и линкуют компонент.

### 5.2.2. Empty state (Favorites / List / Index)

Подсказка «пока пусто» (`Empty` на `FavoritesPanel`, `ListPanel`, `IndexPanel`) — stretch-width TMP под панелью, ключи `empty_favorites` / `empty_list` / `empty_search`.

| Метрика / код | Роль |
|---------------|------|
| `UiLayoutMetrics.EmptyStateHorizontalInset` (80) | Отступ слева и справа |
| `EmptyStateTopOffset` (−400) | Смещение вниз от верха панели |
| `EmptyStateHeight` (≥180) | Высота под многострочный RU/EN текст |
| `HomeListPanelLayout.ConfigureEmptyState` | Якоря stretch, pivot top-center, wrapping |

**Не ломать.** Не класть inset в `anchoredPosition.x` при stretch-якорях `(0,1)–(1,1)`, оставляя `sizeDelta.x ≈ −20`: левый край уезжает вправо, а ширина почти равна родителю — текст вылезает за правый край экрана. Правильно: `anchoredPosition.x = 0`, `sizeDelta.x = −2 × inset`.

Меню: `PeopleOfMath → Apply Home & List Panel Layout (+100%)` также применяет layout к `FavoritesPanel`.

### 5.3. Детальная карточка

Базовый тип: `MathematicianDetailSection` → `Bind(data, english)`, `HasContent`, `GetSectionTitle`.

Реализации: `IdentityDetailSection`, `PortraitDetailSection`, `LabeledTextDetailSection` (кликабельные теги → фильтр), `ScrollTextDetailSection`, `ExternalLinksDetailSection`.

Галерея: `PortraitGalleryView` + `GalleryScrollSnap`.  
Свайп между секциями: `DetailSectionSwipeNavigator` (не перехватывает горизонтальный свайп незавершённой галереи).

### 5.4. Тема

| Компонент | Роль |
|-----------|------|
| `ThemeHelper` | `AppTheme`: Dark (default Syncra), Light, Glassmorphism; prefs `app_theme` |
| `UiTheme` | Палитры и токены (`UiThemeToken`) |
| `UiThemeBinding` | Привязка Graphic/TMP к токену |
| `UiThemeScope` | Реакция на смену темы по иерархии |
| `GlassThemeController` | Blur RT + `UiGlassSurface` |
| Шейдеры | `Assets/Shaders/UiFrostedGlass.shader`, `UiBackdropBlur.shader` |

**Важно:** не переупорядочивайте enum `UiThemeToken` — ломаются сериализованные биндинги в сцене.

### 5.5. Локализация UI

- Locales: `Assets/Localization/`
- Таблица: `UI` (`UI_ru` / `UI_en`)
- Код: `LocaleHelper` (prefs `app_locale`), `UiStrings.Get(key)`

Биографии **не** лежат в Localization — только поля SO.

### 5.6. Размер шрифта

`FontSizeHelper`: Normal / Large (×1.15) / ExtraLarge (×1.30), prefs `app_font_size`.  
`FontSizeScope` применяет масштаб ко всем TMP в поддереве.

### 5.7. Прочие виджеты

`SearchBar` (debounce 2 s), `UiToastView`, `ConfirmDialogOverlay`, `OnboardingOverlay`, `UiPanelSlideTransition`, `FavoriteIconButton`, `ShareIconButton`, `NavTabView`.

---

## 6. Квиз

| Файл | Роль |
|------|------|
| `QuizMode` | `Portrait`, `Fact`, `Mixed` |
| `QuizService` | Пул кандидатов, `GenerateRound` (по умолчанию **10** вопросов, **4** варианта) |
| `QuizPromptExtractor` | Текст факта: interesting facts → achievements → short bio; **без** locale fallback; редакция имени |
| `QuizQuestion` | CorrectId, OptionIds, Kind, Portrait / PromptText |
| `QuizStatsHelper` | Лучшие результаты и число игр в PlayerPrefs |
| `QuizPanel` | UI-состояния; `TryHandleBack`, `IsInActiveRound` |

Дистракторы выбираются с учётом пересечения таксономии с правильным ответом.

Для режима Fact карточка должна иметь непустой текст в **текущей** локали (пустой EN при EN UI → карточка не попадает в пул).

---

## 7. Как добавить математика

Краткий чеклист (детали импорта — в README):

1. Запись в `Assets/Data/mathematicians_catalog.json` (`id`, `wikiTitleRu`, `wikidataId`, ключи фильтров).
2. **Import Catalog (RU texts)** или skeleton (`Create Catalog Assets` / `Tools/generate_skeleton_assets.py`).
3. Портреты в `Assets/Resources/Portraits/{id}/` → **Link Portraits From Folders** (+ **Fix Portrait Texture Import** при необходимости).
4. При необходимости заполнить `*En` в Inspector.
5. **Refresh Repository List**.
6. Проверить карточку в Play Mode (фильтры, индекс, квиз, share).

Лицензии изображений: только PD / CC BY / CC BY-SA.

---

## 8. Соглашения по коду

1. Namespaces: `PeopleOfMath.{Core|Data|UI|Quiz|Localization|Input|Sharing|Text}`; Editor — `PeopleOfMath.Editor`.
2. Настройки пользователя — статические хелперы + `event Action …Changed`; панели подписываются в `OnEnable`.
3. Контент — SO; UI-хром — Localization.
4. Ключи таксономии в данных, человекочитаемые подписи — только в `Taxonomy`.
5. Навигация — стек `ScreenContext`, без отдельных Scene для экранов.
6. Editor-меню сосредоточено в `PeopleOfMath/…`; тяжёлая сборка UI — `PeopleOfMathProjectSetup`.
7. Share: Android Intent; в Editor — clipboard (+ лог только в Editor/Development).
8. Сброс всего: `AppDataReset.ResetAll()` (locale, font, theme, favorites, quiz, onboarding).
9. Не трогать без нужды: object pooling списков, glass blur pipeline, семантика стека навигации, порядок `UiThemeToken`.

---

## 9. Сброс данных и отладка

| Действие | Где |
|----------|-----|
| Сброс настроек в приложении | Settings → Reset → `AppDataReset` |
| Сброс онбординга в Editor | **PeopleOfMath → Reset Onboarding** |
| Отчёты импорта | `Assets/Data/import_report.txt`, `import_report_python.txt` |
| Пакетный setup сцены | `PeopleOfMath.Editor.PeopleOfMathProjectSetup.RunBatch` (см. README) |

Active Input Handling = **Both** (старый + новый Input System).

---

## 10. Карта ключевых файлов

### Runtime

```
Assets/Scripts/Core/AppBootstrap.cs
Assets/Scripts/Core/ScreenContext.cs
Assets/Scripts/Core/AppDataReset.cs
Assets/Scripts/UI/NavigationController.cs
Assets/Scripts/Input/BackButtonHandler.cs

Assets/Scripts/Data/MathematicianData.cs
Assets/Scripts/Data/MathematicianRepository.cs
Assets/Scripts/Data/MathematicianCatalog.cs
Assets/Scripts/Data/Taxonomy.cs
Assets/Scripts/Data/FilterService.cs
Assets/Scripts/Data/SearchService.cs
Assets/Scripts/Data/IndexService.cs
Assets/Scripts/Data/PortraitResolver.cs
Assets/Scripts/Data/FavoritesHelper.cs

Assets/Scripts/Quiz/QuizService.cs
Assets/Scripts/Quiz/QuizPromptExtractor.cs
Assets/Scripts/Quiz/QuizStatsHelper.cs

Assets/Scripts/UI/HomePanel.cs
Assets/Scripts/UI/AdaptiveBrowseGrid.cs
Assets/Scripts/UI/CategoryTileMetrics.cs
Assets/Scripts/UI/ThemeHelper.cs
Assets/Scripts/UI/UiTheme.cs
Assets/Scripts/UI/GlassThemeController.cs
Assets/Scripts/UI/FontSizeHelper.cs
Assets/Scripts/Localization/LocaleHelper.cs
Assets/Scripts/Localization/UiStrings.cs
```

### Сборка APK

| | |
|---|---|
| Выход | `{projectRoot}/com.densappstudio.peopleofmath.apk` |
| Меню | `PeopleOfMath → Build Release APK (project root)` |
| Batch | `Tools/build_apk.ps1` → `AndroidApkBuilder.BuildFromBatch` |
| Версия | Перед билдом: last segment `bundleVersion` +1; `bundleVersionCode` = то же число |
| Подпись | `C:/git/cloud/den.kolesov..keystore`, alias `main`; пароль из `Tools/keystore.local.ps1` (`$KeystorePassword`) или `ANDROID_KEYSTORE_PASS` |
| Автозапуск | **Нет** — только по «собери APK» (`.cursor/rules/build-apk.mdc`) |

Без пароля сборка **падает** (debug signing отключён). Перед batch закройте Unity Editor. Подробности — [README.md § Сборка Android](../README.md#сборка-android).

### Установка / деплой APK

| | |
|---|---|
| USB (ADB) | `Tools/install_apk_adb.ps1` → `adb install -r`; package `com.peopleofmath.app` |
| Bluetooth | `Tools/deploy_apk_bluetooth.ps1` → OBEX / `fsquirt`; телефоны в `Tools/phone_targets.ps1` (`-Phone pova` / `-Phone camon`) |
| Меню | `PeopleOfMath → Android → …` (`AndroidDeployMenu`) — install/deploy и build+install/deploy |
| CLI без сборки | `-SkipBuild` (Editor может оставаться открытым) |
| Агент | «установи APK» → `.cursor/rules/install-apk-adb.mdc`; «отправь APK» → `.cursor/rules/deploy-apk-bluetooth.mdc` |

Эталонные пайплайны (перенос на другие проекты): `c:\git\pipeline\peopleofmath\ADB_PIPELINE.*.md`, `BLUETOOTH_DEPLOY_PIPELINE.*.md`.

### Editor / контент

```
Assets/Editor/AndroidApkBuilder.cs
Assets/Editor/AndroidDeployMenu.cs
Assets/Editor/PeopleOfMathImportMenu.cs
Assets/Editor/MathematicianImportPipeline.cs
Assets/Editor/WikimediaPortraitImporter.cs
Assets/Editor/MathematicianRepositoryRefresh.cs
Assets/Editor/PeopleOfMathProjectSetup.cs
Assets/Editor/HomeListPanelLayout.cs
Assets/Editor/UiLayoutMetrics.cs
Assets/Prefabs/UI/CategoryTile.prefab
Assets/Resources/CategoryTile.prefab
Assets/Data/mathematicians_catalog.json
Assets/Data/Mathematicians/
Assets/Resources/Portraits/
Tools/build_apk.ps1
Tools/install_apk_adb.ps1
Tools/deploy_apk_bluetooth.ps1
Tools/
```

---

## 11. Типичные задачи

| Задача | Куда смотреть |
|--------|----------------|
| Новый экран / вкладка | `NavigationController`, `AppScreen`, панель + патч в `PeopleOfMathProjectSetup` |
| Новый фильтр / ключ | `Taxonomy` + каталог JSON + SO |
| Текст интерфейса | Localization table `UI` + `UiStrings` |
| Текст биографии | Поля `MathematicianData` |
| Цвет / тема | `UiTheme` + `UiThemeBinding` (не хардкодить в панелях без нужды) |
| Баг «назад» | `BackButtonHandler`, `HandleBack`, `DetailPanel.TryGoBack`, `QuizPanel.TryHandleBack` |
| Правая колонка плиток уезжает за край | `AdaptiveBrowseGrid`, `CategoryTileMetrics`; не ставить `minWidth` на `CategoryTile` |
| Счётчик `(n)` между рядами плиток | Якоря `Label`/`Count` от низа; не top-fixed `y = −408` |
| Подписи BottomBar режет скругление | `BottomBarSafeArea` (`MinBottomCornerInset`, HLG L/R); не паддить `ContentArea` по X |
| Собрать релизный APK | `AndroidApkBuilder` / `Tools/build_apk.ps1` + `keystore.local.ps1`; **только по просьбе** |
| Установить APK по USB | `Tools/install_apk_adb.ps1` / меню Android; **только по просьбе** |
| Отправить APK по Bluetooth | `Tools/deploy_apk_bluetooth.ps1` / меню Android; **только по просьбе** |
| Нет портрета | `PortraitResolver`, папка Resources, `.placeholder`, меню Link/Import |
| Квиз без фактов на EN | Заполнить `*En` или временно тестировать RU locale |

---

*Документ отражает структуру кода на момент написания. Операционный гайд по импорту и Android-сборке — в [README.md](../README.md).*
