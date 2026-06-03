using System;
using System.Collections.Generic;

namespace PeopleOfMath.Editor
{
    [Serializable]
    public class MathematicianCatalogRoot
    {
        public List<MathematicianCatalogEntry> mathematicians = new();
    }

    [Serializable]
    public class MathematicianCatalogEntry
    {
        public string id;
        public string wikiTitleRu;
        public string wikidataId;
        public List<string> countryKeys = new();
        public List<string> centuryKeys = new();
        public List<string> branchKeys = new();
    }
}
