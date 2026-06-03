using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PeopleOfMath.Data
{
    [CreateAssetMenu(fileName = "Mathematician", menuName = "PeopleOfMath/Mathematician Data")]
    public class MathematicianData : ScriptableObject
    {
        public string id;
        public string wikiTitleRu;
        public string wikidataId;
        public string fullNameRu;
        public string fullNameEn;
        public string birthDate;
        public string deathDate;
        public List<string> countryKeys = new();
        public List<string> centuryKeys = new();
        public List<string> branchKeys = new();
        public string achievementsRu;
        public string achievementsEn;
        public string personalLifeRu;
        public string personalLifeEn;
        public string shortBioRu;
        public string shortBioEn;
        public string wikipediaUrlRu;
        public List<PortraitEntry> portraits = new();

        static string Pick(bool english, string ru, string en)
        {
            var text = english && !string.IsNullOrWhiteSpace(en) ? en : ru;
            return UnicodeText.Normalize(text);
        }

        public string GetFullName(bool english) => Pick(english, fullNameRu, fullNameEn);

        public string GetShortBio(bool english) => Pick(english, shortBioRu, shortBioEn);

        public string GetAchievements(bool english) => Pick(english, achievementsRu, achievementsEn);

        public string GetPersonalLife(bool english) => Pick(english, personalLifeRu, personalLifeEn);

        public IReadOnlyList<PortraitEntry> GetValidPortraits() =>
            portraits.Where(p => p != null && p.sprite != null).ToList();

        public string GetLifeDatesLabel(bool english)
        {
            if (string.IsNullOrEmpty(deathDate))
                return birthDate;
            return $"{birthDate} — {deathDate}";
        }

        public string GetCountriesDisplay(bool english) =>
            Taxonomy.JoinLabels(countryKeys, Taxonomy.Countries, english);

        public string GetCenturiesDisplay(bool english) =>
            Taxonomy.JoinLabels(centuryKeys, Taxonomy.Centuries, english);

        public string GetBranchesDisplay(bool english) =>
            Taxonomy.JoinLabels(branchKeys, Taxonomy.Branches, english);

        public string GetPortraitAttribution(PortraitEntry entry, bool english)
        {
            if (entry == null)
                return "";
            var attr = Pick(english, entry.attributionRu, entry.attributionEn);
            if (!string.IsNullOrWhiteSpace(attr))
                return attr;
            return entry.licenseShort ?? "";
        }
    }
}
