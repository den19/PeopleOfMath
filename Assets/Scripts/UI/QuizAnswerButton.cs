using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeopleOfMath.UI
{
    public enum QuizAnswerButtonState
    {
        Neutral,
        Correct,
        Wrong,
        HighlightCorrect
    }

    public class QuizAnswerButton : MonoBehaviour
    {
        const string GlowChildName = "Glow";
        const string FillChildName = "Fill";
        const string StatusIconChildName = "StatusIcon";

        [SerializeField] Button button;
        [SerializeField] TMP_Text label;
        [SerializeField] Image fillImage;
        [SerializeField] TMP_Text statusIcon;

        string _optionId;
        Action<string> _onSelected;

        public string OptionId => _optionId;

        void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();
            if (label == null)
                label = GetComponentInChildren<TMP_Text>();
            if (fillImage == null)
            {
                var fill = transform.Find(FillChildName);
                if (fill != null)
                    fillImage = fill.GetComponent<Image>();
            }

            EnsureStatusIcon();

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(OnClicked);
            }
        }

        void EnsureStatusIcon()
        {
            if (statusIcon != null)
                return;

            var existing = transform.Find(StatusIconChildName);
            if (existing != null)
            {
                statusIcon = existing.GetComponent<TMP_Text>();
                return;
            }

            var go = new GameObject(StatusIconChildName, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(transform, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(-28f, 0f);
            rt.sizeDelta = new Vector2(48f, 48f);

            statusIcon = go.GetComponent<TMP_Text>();
            statusIcon.fontSize = 28f;
            statusIcon.alignment = TextAlignmentOptions.MidlineRight;
            statusIcon.raycastTarget = false;
            statusIcon.gameObject.SetActive(false);
        }

        public void Bind(string optionId, string labelText, Action<string> onSelected)
        {
            _optionId = optionId;
            _onSelected = onSelected;
            if (label != null)
                label.text = labelText;
            SetState(QuizAnswerButtonState.Neutral);
            SetInteractable(true);
        }

        public void SetState(QuizAnswerButtonState state)
        {
            if (fillImage != null)
            {
                fillImage.color = state switch
                {
                    QuizAnswerButtonState.Correct => UiTheme.SemanticSuccessMuted,
                    QuizAnswerButtonState.Wrong => UiTheme.SemanticErrorMuted,
                    QuizAnswerButtonState.HighlightCorrect => UiTheme.SemanticSuccessMuted,
                    _ => UiTheme.ButtonSecondaryFill
                };
            }

            if (label != null)
            {
                label.color = state is QuizAnswerButtonState.Correct or QuizAnswerButtonState.Wrong
                    ? Color.white
                    : UiTheme.CardTextPrimary;
            }

            if (statusIcon != null)
            {
                switch (state)
                {
                    case QuizAnswerButtonState.Correct:
                        statusIcon.gameObject.SetActive(true);
                        statusIcon.text = "\u2713";
                        statusIcon.color = Color.white;
                        break;
                    case QuizAnswerButtonState.Wrong:
                        statusIcon.gameObject.SetActive(true);
                        statusIcon.text = "\u2717";
                        statusIcon.color = Color.white;
                        break;
                    case QuizAnswerButtonState.HighlightCorrect:
                        statusIcon.gameObject.SetActive(true);
                        statusIcon.text = "\u2713";
                        statusIcon.color = UiTheme.SemanticSuccess;
                        break;
                    default:
                        statusIcon.gameObject.SetActive(false);
                        statusIcon.text = "";
                        break;
                }
            }

            var border = fillImage != null ? fillImage.GetComponent<Outline>() : null;
            if (border != null)
            {
                border.effectColor = state switch
                {
                    QuizAnswerButtonState.Correct or QuizAnswerButtonState.HighlightCorrect =>
                        UiTheme.SemanticSuccess,
                    QuizAnswerButtonState.Wrong => UiTheme.SemanticError,
                    _ => UiTheme.ButtonSecondaryBorder
                };
            }
        }

        public void SetInteractable(bool interactable)
        {
            if (button != null)
                button.interactable = interactable;
        }

        void OnClicked() => _onSelected?.Invoke(_optionId);
    }
}
