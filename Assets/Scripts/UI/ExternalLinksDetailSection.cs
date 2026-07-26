using PeopleOfMath.Data;
using PeopleOfMath.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeopleOfMath.UI
{
    public class ExternalLinksDetailSection : MathematicianDetailSection
    {
        [SerializeField] Button wikipediaButton;
        [SerializeField] Button wikidataButton;
        [SerializeField] TMP_Text wikipediaLabel;
        [SerializeField] TMP_Text wikidataLabel;

        public override void Bind(MathematicianData data, bool english)
        {
            if (data == null)
                return;

            BindLinkButton(
                wikipediaButton,
                wikipediaLabel,
                data.GetWikipediaUrl(english),
                UiStrings.Get("link_wikipedia"));

            BindLinkButton(
                wikidataButton,
                wikidataLabel,
                data.GetWikidataUrl(),
                UiStrings.Get("link_wikidata"));
        }

        public override string GetSectionTitle(bool english) =>
            UiStrings.Get("section_links");

        public override bool HasContent(MathematicianData data, bool english) =>
            data != null &&
            (!string.IsNullOrWhiteSpace(data.GetWikipediaUrl(english)) ||
             !string.IsNullOrWhiteSpace(data.GetWikidataUrl()));

        static void BindLinkButton(Button button, TMP_Text label, string url, string labelText)
        {
            if (button == null)
                return;

            var hasUrl = !string.IsNullOrWhiteSpace(url);
            button.gameObject.SetActive(hasUrl);
            if (!hasUrl)
                return;

            if (label != null)
                label.text = labelText;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => ExternalLinkOpener.Open(url));
        }
    }
}
