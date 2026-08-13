using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeopleOfMath.UI
{
    public class CalendarDayCell : MonoBehaviour
    {
        [SerializeField] Button button;
        [SerializeField] Image background;
        [SerializeField] Image birthdayMarker;
        [SerializeField] TMP_Text dayLabel;

        int _day;
        System.Action<int> _onSelected;

        void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();
            if (background == null)
                background = GetComponent<Image>();
            if (dayLabel == null)
                dayLabel = GetComponentInChildren<TMP_Text>();
            if (birthdayMarker == null)
                birthdayMarker = transform.Find("BirthdayMarker")?.GetComponent<Image>();
        }

        public void Bind(
            int day,
            bool isToday,
            bool hasBirthday,
            bool isSelected,
            bool isCurrentMonth,
            System.Action<int> onSelected)
        {
            _day = day;
            _onSelected = onSelected;

            if (dayLabel != null)
            {
                dayLabel.text = day > 0 ? day.ToString() : "";
                dayLabel.color = !isCurrentMonth || day <= 0
                    ? Color.clear
                    : isToday
                        ? UiTheme.AccentWarm
                        : UiTheme.CardTextPrimary;
            }

            if (birthdayMarker != null)
            {
                birthdayMarker.gameObject.SetActive(isCurrentMonth && day > 0 && hasBirthday);
                birthdayMarker.color = UiTheme.PrimaryAccent;
            }

            if (background != null)
            {
                if (!isCurrentMonth || day <= 0)
                    background.color = Color.clear;
                else if (isSelected)
                    background.color = UiTheme.AccentMuted;
                else if (isToday)
                    background.color = new Color(
                        UiTheme.AccentWarm.r,
                        UiTheme.AccentWarm.g,
                        UiTheme.AccentWarm.b,
                        0.22f);
                else
                    background.color = Color.clear;
            }

            if (button != null)
            {
                button.interactable = isCurrentMonth && day > 0;
                button.onClick.RemoveAllListeners();
                if (button.interactable)
                    button.onClick.AddListener(() => _onSelected?.Invoke(_day));
            }
        }
    }
}
