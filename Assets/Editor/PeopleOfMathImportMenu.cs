using UnityEditor;
using UnityEngine;

namespace PeopleOfMath.Editor
{
    public static class PeopleOfMathImportMenu
    {
        [MenuItem("PeopleOfMath/Import All (Catalog + Portraits + Refresh)")]
        public static void ImportAll()
        {
            MathematicianImportPipeline.ImportCatalog();
            WikimediaPortraitImporter.ImportAllPortraits();
            WikimediaPortraitImporter.LinkAllFromFolders();
            MathematicianRepositoryRefresh.RefreshAllInOpenScene();
            Debug.Log("Import All finished. Regenerate Main Scene if Detail gallery is missing.");
        }
    }
}
