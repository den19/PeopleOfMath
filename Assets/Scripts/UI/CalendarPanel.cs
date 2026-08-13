using System;
using System.Collections.Generic;
using System.Globalization;
using PeopleOfMath.Core;
using PeopleOfMath.Data;
using PeopleOfMath.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace PeopleOfMath.UI
{
    public class CalendarPanel : MonoBehaviour
    {
        const string ListItemResourceName = "MathematicianListItem";
        const int DayCellCount = 42;

        [SerializeField] NavigationController navigation;
        [SerializeField] MathematicianRepository repository;
        [SerializeField] TMP_Text monthLabel;
        [SerializeField] Button prevMonthButton;
        [SerializeField] Button nextMonthButton;
        [SerializeField] Transform weekdayHeader;
        [SerializeField] Transform dayGrid;
        [SerializeField] Transform listContent;
        [SerializeField] MathematicianListItem itemPrefab;
        [SerializeField] GameObject emptyState;
        [SerializeField] TMP_Text anniversaryYearLabel;
        [SerializeField] Transform anniversaryListContent;
        [SerializeField] GameObject anniversaryEmptyState;

        int _year;
        int _month;
        int _selectedDay;
        readonly List<CalendarDayCell> _dayCells = new();
        bool _dayCellsCached;

        void Awake()
        {
            if (itemPrefab == null)
                itemPrefab = Resources.Load<MathematicianListItem>(ListItemResourceName);

            WireMonthButtons();
            CacheDayCells();
        }

        void OnEnable()
        {
            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
            FontSizeHelper.FontSizeChanged += OnFontSizeChanged;
            ThemeHelper.ThemeChanged += OnThemeChanged;
            if (_year == 0)
                ResetToToday();
            Refresh();
        }

        void OnDisable()
        {
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
            FontSizeHelper.FontSizeChanged -= OnFontSizeChanged;
            ThemeHelper.ThemeChanged -= OnThemeChanged;
        }

        void WireMonthButtons()
        {
            if (prevMonthButton != null)
            {
                prevMonthButton.onClick.RemoveAllListeners();
                prevMonthButton.onClick.AddListener(GoPrevMonth);
            }

            if (nextMonthButton != null)
            {
                nextMonthButton.onClick.RemoveAllListeners();
                nextMonthButton.onClick.AddListener(GoNextMonth);
            }
        }

        public void ResetToToday()
        {
            var today = DateTime.Today;
            _year = today.Year;
            _month = today.Month;
            _selectedDay = today.Day;
        }

        void GoPrevMonth()
        {
            _month--;
            if (_month < 1)
            {
                _month = 12;
                _year--;
            }

            ClampSelectedDay();
            Refresh();
        }

        void GoNextMonth()
        {
            _month++;
            if (_month > 12)
            {
                _month = 1;
                _year++;
            }

            ClampSelectedDay();
            Refresh();
        }

        void ClampSelectedDay()
        {
            var daysInMonth = DateTime.DaysInMonth(_year, _month);
            if (_selectedDay > daysInMonth)
                _selectedDay = daysInMonth;
        }

        void OnLocaleChanged(UnityEngine.Localization.Locale _) => Refresh();

        void OnFontSizeChanged() => Refresh();

        void OnThemeChanged()
        {
            RefreshCalendarGrid();
            RefreshThemeList(listContent);
            RefreshThemeList(anniversaryListContent);
        }

        void RefreshThemeList(Transform content)
        {
            if (content == null)
                return;

            foreach (Transform child in content)
            {
                var card = child.GetComponent<UiThemedCard>();
                if (card == null)
                    continue;

                card.Configure(UiCardVariant.ListItem);
                card.Apply();
            }

            GlassThemeController.RefreshAllSurfaces();
        }

        void Refresh()
        {
            RefreshMonthLabel();
            RefreshWeekdayHeader();
            RefreshCalendarGrid();
            RefreshBirthdayList();
            RefreshAnniversaryList();
            GetComponent<FontSizeScope>()?.Apply();
            GlassThemeController.RefreshAllSurfaces();
        }

        void RefreshMonthLabel()
        {
            if (monthLabel == null)
                return;

            var culture = LocaleHelper.IsEnglish
                ? CultureInfo.GetCultureInfo("en-US")
                : CultureInfo.GetCultureInfo("ru-RU");
            var date = new DateTime(_year, _month, 1);
            monthLabel.text = date.ToString("MMMM yyyy", culture);
            monthLabel.color = UiTheme.CardTextPrimary;
        }

        void RefreshWeekdayHeader()
        {
            if (weekdayHeader == null)
                return;

            var culture = LocaleHelper.IsEnglish
                ? CultureInfo.GetCultureInfo("en-US")
                : CultureInfo.GetCultureInfo("ru-RU");
            for (var i = 0; i < weekdayHeader.childCount && i < 7; i++)
            {
                var label = weekdayHeader.GetChild(i).GetComponent<TMP_Text>();
                if (label == null)
                    continue;

                var dow = (DayOfWeek)((i + 1) % 7);
                label.text = culture.DateTimeFormat.GetShortestDayName(dow);
                label.color = UiTheme.CardTextSecondary;
            }
        }

        void CacheDayCells()
        {
            if (_dayCellsCached || dayGrid == null)
                return;

            _dayCells.Clear();
            for (var i = 0; i < dayGrid.childCount; i++)
            {
                var cell = dayGrid.GetChild(i).GetComponent<CalendarDayCell>();
                if (cell != null)
                    _dayCells.Add(cell);
            }

            _dayCellsCached = true;
        }

        void RefreshCalendarGrid()
        {
            if (dayGrid == null)
                return;

            CacheDayCells();

            var birthdayDays = repository != null
                ? BirthDateParser.BirthdayDaysInMonth(repository.All, _month)
                : new HashSet<int>();

            var today = DateTime.Today;
            var first = new DateTime(_year, _month, 1);
            var startOffset = ((int)first.DayOfWeek + 6) % 7;
            var daysInMonth = DateTime.DaysInMonth(_year, _month);
            var count = Mathf.Min(_dayCells.Count, DayCellCount);

            for (var i = 0; i < count; i++)
            {
                var dayNumber = i - startOffset + 1;
                var inMonth = dayNumber >= 1 && dayNumber <= daysInMonth;
                var day = inMonth ? dayNumber : 0;
                var isToday = inMonth && _year == today.Year && _month == today.Month && day == today.Day;
                var hasBirthday = inMonth && birthdayDays.Contains(day);
                var isSelected = inMonth && day == _selectedDay;
                _dayCells[i].gameObject.SetActive(true);
                _dayCells[i].Bind(day, isToday, hasBirthday, isSelected, inMonth, OnDaySelected);
            }
        }

        void OnDaySelected(int day)
        {
            if (day <= 0)
                return;

            _selectedDay = day;
            RefreshCalendarGrid();
            RefreshBirthdayList();
        }

        void RefreshBirthdayList()
        {
            if (listContent == null)
                return;

            foreach (Transform child in listContent)
                Destroy(child.gameObject);

            if (repository == null)
                return;

            var born = BirthDateParser.FindBornOn(repository.All, _month, _selectedDay);
            emptyState?.SetActive(born.Count == 0);
            UpdateEmptyStateMessage(emptyState, "empty_birthdays");

            if (born.Count == 0)
                return;

            if (!EnsureItemPrefab())
                return;

            foreach (var data in born)
            {
                var item = Instantiate(itemPrefab, listContent);
                item.Bind(data, id => navigation.ShowDetail(id));
            }
        }

        void RefreshAnniversaryList()
        {
            if (anniversaryListContent == null)
                return;

            foreach (Transform child in anniversaryListContent)
                Destroy(child.gameObject);

            var currentYear = DateTime.Today.Year;
            UpdateAnniversaryYearLabel(currentYear);

            if (repository == null)
                return;

            var anniversaries = BirthDateParser.FindAnniversaries(repository.All, currentYear);
            anniversaryEmptyState?.SetActive(anniversaries.Count == 0);
            UpdateEmptyStateMessage(anniversaryEmptyState, "empty_anniversaries");

            if (anniversaries.Count == 0)
                return;

            if (!EnsureItemPrefab())
                return;

            var english = LocaleHelper.IsEnglish;
            anniversaries.Sort((a, b) =>
            {
                var byMilestone = b.milestone.CompareTo(a.milestone);
                if (byMilestone != 0)
                    return byMilestone;
                var byYears = b.yearsSince.CompareTo(a.yearsSince);
                if (byYears != 0)
                    return byYears;
                return string.Compare(
                    a.data.GetFullName(english),
                    b.data.GetFullName(english),
                    StringComparison.OrdinalIgnoreCase);
            });

            foreach (var entry in anniversaries)
            {
                var item = Instantiate(itemPrefab, anniversaryListContent);
                item.Bind(entry.data, id => navigation.ShowDetail(id));
            }
        }

        void UpdateAnniversaryYearLabel(int currentYear)
        {
            if (anniversaryYearLabel == null)
                return;

            var fmt = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "title_anniversaries");
            anniversaryYearLabel.text = string.Format(fmt, currentYear);
            anniversaryYearLabel.color = UiTheme.CardTextPrimary;
        }

        bool EnsureItemPrefab()
        {
            if (itemPrefab != null)
                return true;

            Debug.LogError(
                "CalendarPanel: MathematicianListItem prefab is not assigned. " +
                "Run PeopleOfMath → Patch Birthday Calendar or add Assets/Resources/MathematicianListItem.prefab.");
            return false;
        }

        static void UpdateEmptyStateMessage(GameObject empty, string key)
        {
            if (empty == null)
                return;

            var text = empty.GetComponent<TMP_Text>();
            if (text == null)
                return;

            text.text = LocalizationSettings.StringDatabase.GetLocalizedString("UI", key);
        }
    }
}
