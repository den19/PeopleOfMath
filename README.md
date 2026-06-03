# PeopleOfMath

Справочник о математиках для Android (MVP). Unity **6000.4.5f1**, URP 2D, одна сцена `Main`.

## Требования

- Unity Hub с редактором **6000.4.5f1**
- Модуль **Android Build Support**

## Открытие проекта

1. Клонировать репозиторий и открыть папку в Unity Hub.
2. При первом открытии дождаться импорта пакетов (URP, Input System, Localization, TextMeshPro).
3. При первом открытии: **Window → TextMeshPro → Import TMP Essential Resources** (если Unity предложит).
4. Если `Assets/Scenes/Main.unity` отсутствует: **PeopleOfMath → Regenerate Main Scene** (или **Setup Project**).
5. Открыть сцену `Assets/Scenes/Main.unity` и нажать **Play**.

Пакетный setup (редактор закрыт):

```text
"C:\Program Files\Unity\Hub\Editor\6000.4.5f1\Editor\Unity.exe" -batchmode -quit -projectPath "C:\git\PeopleOfMath" -executeMethod PeopleOfMath.Editor.PeopleOfMathProjectSetup.RunBatch
```

## Сборка Android

1. **File → Build Settings** — платформа **Android** (должна быть активна после setup).
2. Сцена `Assets/Scenes/Main.unity` в списке (index 0).
3. **Build** или **Build And Run**.

## Функции MVP

- 10 математиков с биографией на **русском** и **английском** (встроенные переводы).
- Фильтры: век, страна, раздел математики.
- Карточка: даты, страны, разделы, достижения, личная жизнь.
- Настройки языка интерфейса (RU / EN) через Unity Localization.
- Ввод: **Active Input Handling = Both**, UI на Input System UI Input Module.

## Структура

- `Assets/Scripts` — логика и UI
- `Assets/Data/Mathematicians` — ScriptableObject карточки
- `Assets/Localization` — локали и String Table UI
- `Assets/Scenes/Main.unity` — единственная сцена
