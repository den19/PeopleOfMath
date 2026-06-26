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

        [SerializeField] Button button;
        [SerializeField] TMP_Text label;
        [SerializeField] Image fillImage;

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

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(OnClicked);
            }
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
            if (fillImage == null)
                return;

            fillImage.color = state switch
            {
                QuizAnswerButtonState.Correct => new Color(0.2f, 0.75f, 0.35f, 0.85f),
                QuizAnswerButtonState.Wrong => new Color(0.85f, 0.25f, 0.25f, 0.85f),
                QuizAnswerButtonState.HighlightCorrect => new Color(0.2f, 0.75f, 0.35f, 0.55f),
                _ => UiTheme.ButtonSecondaryFill
            };

            if (label != null)
            {
                label.color = state is QuizAnswerButtonState.Correct or QuizAnswerButtonState.HighlightCorrect
                    ? Color.white
                    : UiTheme.TextPrimary;
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
