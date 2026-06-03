using System.Collections.Generic;
using UnityEngine;

namespace PeopleOfMath.Data
{
    [CreateAssetMenu(fileName = "Mathematician", menuName = "PeopleOfMath/Mathematician Data")]
    public class MathematicianData : ScriptableObject
    {
        public string id;
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

        public string GetFullName(bool english) => english ? fullNameEn : fullNameRu;

        public string GetShortBio(bool english) => english ? shortBioEn : shortBioRu;

        public string GetAchievements(bool english) => english ? achievementsEn : achievementsRu;

        public string GetPersonalLife(bool english) => english ? personalLifeRu : personalLifeRu;

        public string GetLifeDatesLabel(bool english)
        {
            if (string.IsNullOrEmpty(deathDate))
                return birthDate;
            return english
                ? $"{birthDate} — {deathDate}"
                : $"{birthDate} — {deathDate}";
        }

        public string GetCountriesDisplay(bool english)
        {
            return Taxonomy.JoinLabels(countryKeys, Taxonomy.Countries, english);
        }

        public string GetCenturiesDisplay(bool english)
        {
            return Taxonomy.JoinLabels(centuryKeys, Taxonomy.Centuries, english);
        }

        public string GetBranchesDisplay(bool english)
        {
            return Taxonomy.JoinLabels(branchKeys, Taxonomy.Branches, english);
        }
    }
}
