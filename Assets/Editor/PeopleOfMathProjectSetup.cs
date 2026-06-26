using System.Collections.Generic;
using System.IO;
using PeopleOfMath.Core;
using PeopleOfMath.Data;
using PeopleOfMath.Input;
using PeopleOfMath.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Events;
using UnityEditor.Localization;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;
using UnityEngine.Events;
using UnityEngine.Localization.Tables;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PeopleOfMath.Editor
{
    public static class PeopleOfMathProjectSetup
    {
        const string ScenePath = "Assets/Scenes/Main.unity";
        const string DataFolder = "Assets/Data/Mathematicians";
        const string SettingsFolder = "Assets/Settings";
        const string LocalizationFolder = "Assets/Localization";
        const string PrefabFolder = "Assets/Prefabs/UI";
        const string DetailPrefabFolder = "Assets/Prefabs/UI/Detail";

        [MenuItem("PeopleOfMath/Setup Project")]
        public static void SetupMenu() => Run(manual: true);

        [MenuItem("PeopleOfMath/Regenerate Main Scene")]
        public static void RegenerateScene()
        {
            if (DeferUntilEditMode(RegenerateScene))
                return;

            if (File.Exists(ScenePath))
                AssetDatabase.DeleteAsset(ScenePath);
            PeopleOfMathAutoSetup.ResetSession();
            Run(manual: true);
        }

        [MenuItem("PeopleOfMath/Patch Search Support")]
        public static void PatchSearchSupport()
        {
            if (DeferUntilEditMode(PatchSearchSupport))
                return;

            if (!File.Exists(ScenePath))
            {
                Debug.LogError($"Scene not found: {ScenePath}");
                return;
            }

            var loc = SetupLocalization();
            AssetDatabase.SaveAssets();

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var home = GameObject.Find("HomePanel");
            var nav = Object.FindFirstObjectByType<NavigationController>();

            if (home != null && nav != null && home.transform.Find("SearchBar") == null)
            {
                var searchBar = CreateSearchBar(home.transform, nav, loc);
                var scroll = home.transform.Find("HomeScroll")?.GetComponent<ScrollRect>();
                if (scroll != null)
                    PinHomeSearchAndScroll(searchBar, scroll);

                var homePanel = home.GetComponent<HomePanel>();
                if (homePanel != null)
                {
                    var so = new SerializedObject(homePanel);
                    so.FindProperty("searchBar").objectReferenceValue = searchBar.GetComponent<SearchBar>();
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            if (home != null)
                HomeListPanelLayout.ApplyToPanel(home);

            EnsureThemedCardOnPrefab($"{PrefabFolder}/SearchBar.prefab", UiCardVariant.Filter);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Search support patched in Main scene.");
        }

        [MenuItem("PeopleOfMath/Patch Detail Swipe Navigation")]
        public static void PatchDetailSwipeNavigation()
        {
            if (DeferUntilEditMode(PatchDetailSwipeNavigation))
                return;

            if (!File.Exists(ScenePath))
            {
                Debug.LogError($"Scene not found: {ScenePath}");
                return;
            }

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var detailPanel = Object.FindFirstObjectByType<DetailPanel>();
            if (detailPanel == null)
            {
                Debug.LogError("DetailPanel not found in Main scene.");
                return;
            }

            var container = detailPanel.transform.Find("SectionContainer");
            if (container == null)
            {
                Debug.LogError("SectionContainer not found under DetailPanel.");
                return;
            }

            var swipeNav = container.GetComponent<DetailSectionSwipeNavigator>();
            if (swipeNav == null)
            {
                swipeNav = container.gameObject.AddComponent<DetailSectionSwipeNavigator>();
                var swipeSo = new SerializedObject(swipeNav);
                swipeSo.FindProperty("detailPanel").objectReferenceValue = detailPanel;
                swipeSo.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Detail swipe navigation patched in Main scene.");
        }

        [MenuItem("PeopleOfMath/Patch Share Buttons")]
        public static void PatchShareButtons()
        {
            if (DeferUntilEditMode(PatchShareButtons))
                return;

            UiSpriteFactory.EnsureSprites();
            SetupLocalization();
            AssetDatabase.SaveAssets();

            PatchShareListItemPrefab($"{PrefabFolder}/MathematicianListItem.prefab");
            PatchShareListItemPrefab("Assets/Resources/MathematicianListItem.prefab");
            PatchIdentitySectionPrefab();

            if (File.Exists(ScenePath))
            {
                var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                PatchShareButtonsInScene();
                EditorSceneManager.SaveScene(scene);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Share buttons patched.");
        }

        static void PatchShareListItemPrefab(string path)
        {
            if (!File.Exists(path))
                return;

            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                ConfigureListItem(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static void PatchIdentitySectionPrefab()
        {
            if (!Directory.Exists(DetailPrefabFolder))
                Directory.CreateDirectory(DetailPrefabFolder);

            var path = $"{DetailPrefabFolder}/DetailSection_Identity.prefab";
            if (!File.Exists(path))
            {
                var root = BuildIdentitySectionPrefab();
                SaveDetailSectionPrefab(root, path);
                Object.DestroyImmediate(root);
                return;
            }

            var existing = PrefabUtility.LoadPrefabContents(path);
            try
            {
                ConfigureIdentitySection(existing);
                PrefabUtility.SaveAsPrefabAsset(existing, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(existing);
            }
        }

        static void PatchShareButtonsInScene()
        {
            var listPanel = GameObject.Find("ListPanel");
            if (listPanel != null)
            {
                var listItem = listPanel.GetComponentInChildren<MathematicianListItem>(true);
                if (listItem != null)
                    ConfigureListItem(listItem.gameObject);
            }

            var identitySection = Object.FindAnyObjectByType<IdentityDetailSection>(FindObjectsInactive.Include);
            if (identitySection != null)
                ConfigureIdentitySection(identitySection.gameObject);
        }

        [MenuItem("PeopleOfMath/Patch Glass Theme Support")]
        public static void PatchGlassThemeSupport()
        {
            if (DeferUntilEditMode(PatchGlassThemeSupport))
                return;

            if (!File.Exists(ScenePath))
            {
                Debug.LogError($"Scene not found: {ScenePath}");
                return;
            }

            var loc = SetupLocalization();
            AddUiEntry(loc.UiCollection, "btn_theme_glass", "Стекло", "Glass");
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(ScenePath);
            PatchGlassThemeInOpenScene(loc.UiCollection);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log("Glass theme support patched in Main scene.");
        }

        [MenuItem("PeopleOfMath/Patch Theme Support")]
        public static void PatchThemeSupport()
        {
            if (DeferUntilEditMode(PatchThemeSupport))
                return;

            if (!File.Exists(ScenePath))
            {
                Debug.LogError($"Scene not found: {ScenePath}");
                return;
            }

            var loc = SetupLocalization();
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(ScenePath);
            PatchThemeInOpenScene(loc.UiCollection);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log("Theme support patched in Main scene.");
        }

        [MenuItem("PeopleOfMath/Patch Index Tab")]
        public static void PatchIndexTab()
        {
            if (DeferUntilEditMode(PatchIndexTab))
                return;

            if (!File.Exists(ScenePath))
            {
                Debug.LogError($"Scene not found: {ScenePath}");
                return;
            }

            var loc = SetupLocalization();
            CreateLetterButtonPrefab();
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(ScenePath);
            PatchIndexInOpenScene(loc);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log("Index tab patched in Main scene.");
        }

        public static void Run(bool manual = false)
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += () => Run(manual);
                return;
            }

            if (EditorApplication.isPlaying)
            {
                if (manual)
                    DeferUntilEditMode(() => Run(manual: true));
                else
                    Debug.LogWarning("PeopleOfMath setup skipped: cannot run during Play mode.");
                return;
            }

            try
            {
                RunInternal();
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
                throw;
            }
        }

        static bool DeferUntilEditMode(System.Action retry)
        {
            if (!EditorApplication.isPlaying)
                return false;

            EditorApplication.isPlaying = false;
            EditorApplication.delayCall += () => retry();
            return true;
        }

        static void RunInternal()
        {
            EnsureFolders();
            UiSpriteFactory.EnsureSprites();
            if (!Application.isBatchMode)
                TryImportTmpResources();
            SetupRenderPipeline();
            SetupPlayerSettings();
            SetupAndroidTarget();
            var mathematicians = MathematicianRepositoryRefresh.LoadAllFromFolder(DataFolder);
            if (mathematicians.Count == 0)
                mathematicians = MathematicianContentFactory.CreateAll(DataFolder);
            var localization = SetupLocalization();
            AssetDatabase.SaveAssets();
            var listItemPrefab = CreateListItemPrefab();
            EnsureListItemInResources();
            CreateMainScene(mathematicians, localization, listItemPrefab);
            MathematicianRepositoryRefresh.RefreshAllInOpenScene();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

            var repo = Object.FindAnyObjectByType<MathematicianRepository>();
            if (repo == null || repo.All.Count == 0)
                Debug.LogError("PeopleOfMath setup: repository is empty after save — run Refresh Repository List.");

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("PeopleOfMath project setup completed.");
        }

        public static void RunBatch()
        {
            Run();
            EditorApplication.Exit(0);
        }

        public static void RunRefreshBatch()
        {
            var list = MathematicianRepositoryRefresh.LoadAllFromFolder(DataFolder);
            MathematicianRepositoryRefresh.UpdateResourcesCatalog(list);
            if (File.Exists(ScenePath))
            {
                EditorSceneManager.OpenScene(ScenePath);
                MathematicianRepositoryRefresh.RefreshAllInOpenScene();
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"PeopleOfMath repository refresh completed with {list.Count} mathematicians.");
            EditorApplication.Exit(0);
        }

        static void TryImportTmpResources()
        {
            if (AssetDatabase.IsValidFolder("Assets/TextMesh Pro"))
                return;
            TMP_PackageResourceImporter.ImportResources(true, false, false);
            AssetDatabase.Refresh();
        }

        static void EnsureFolders()
        {
            foreach (var dir in new[]
                     {
                         "Assets/Scenes", DataFolder, SettingsFolder, LocalizationFolder,
                         PrefabFolder, DetailPrefabFolder, "Assets/Resources", "Assets/Scripts", "Assets/Editor",
                         "Assets/Data/Images", "Assets/Data", "Assets/UI", "Assets/UI/Sprites"
                     })
            {
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
            }
        }

        static void SetupRenderPipeline()
        {
            var rendererPath = $"{SettingsFolder}/Renderer2D.asset";
            var urpPath = $"{SettingsFolder}/URP_2D.asset";

            var renderer = AssetDatabase.LoadAssetAtPath<Renderer2DData>(rendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<Renderer2DData>();
                AssetDatabase.CreateAsset(renderer, rendererPath);
            }

            var urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(urpPath);
            if (urp == null)
            {
                urp = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
                var urpSo = new SerializedObject(urp);
                var rendererList = urpSo.FindProperty("m_RendererDataList");
                rendererList.InsertArrayElementAtIndex(0);
                rendererList.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
                urpSo.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.CreateAsset(urp, urpPath);
            }

            GraphicsSettings.defaultRenderPipeline = urp;
            QualitySettings.renderPipeline = urp;
        }

        static void SetupPlayerSettings()
        {
            PlayerSettings.companyName = "PeopleOfMath";
            PlayerSettings.productName = "PeopleOfMath";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetApplicationIdentifier(
                NamedBuildTarget.Android, "com.peopleofmath.app");
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
        }

        static void SetupAndroidTarget()
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(
                BuildTargetGroup.Android,
                BuildTarget.Android);
        }

        sealed class LocalizationRefs
        {
            public Locale Russian;
            public Locale English;
            public StringTableCollection UiCollection;
            public LocalizedString HomeTitle;
            public LocalizedString IndexTitle;
            public LocalizedString SettingsTitle;
            public LocalizedString FavoritesTitle;
            public LocalizedString DetailTitle;
        }

        static LocalizationRefs SetupLocalization()
        {
            var ruPath = $"{LocalizationFolder}/Russian (ru).asset";
            var enPath = $"{LocalizationFolder}/English (en).asset";

            var ru = AssetDatabase.LoadAssetAtPath<Locale>(ruPath);
            if (ru == null)
            {
                ru = Locale.CreateLocale(new LocaleIdentifier("ru"));
                AssetDatabase.CreateAsset(ru, ruPath);
            }

            var en = AssetDatabase.LoadAssetAtPath<Locale>(enPath);
            if (en == null)
            {
                en = Locale.CreateLocale(new LocaleIdentifier("en"));
                AssetDatabase.CreateAsset(en, enPath);
            }

            var collectionPath = $"{LocalizationFolder}/UI";
            var collection = AssetDatabase.LoadAssetAtPath<StringTableCollection>($"{collectionPath}/UI.asset");
            if (collection == null)
                collection = AssetDatabase.LoadAssetAtPath<StringTableCollection>($"{collectionPath}.asset");
            if (collection == null)
                collection = LocalizationEditorSettings.CreateStringTableCollection("UI", collectionPath);

            AddUiEntry(collection, "title_home",
                "Математики: века, страны, разделы математики",
                "Mathematicians: centuries, countries, branches of mathematics");
            AddUiEntry(collection, "title_settings", "Настройки", "Settings");
            AddUiEntry(collection, "title_index", "Все математики A–Я", "All mathematicians A–Z");
            AddUiEntry(collection, "title_detail", "Карточка", "Profile");
            AddUiEntry(collection, "section_century", "По веку", "By century");
            AddUiEntry(collection, "section_country", "По стране", "By country");
            AddUiEntry(collection, "section_branch", "По разделу", "By field");
            AddUiEntry(collection, "filter_century", "{0}", "{0}");
            AddUiEntry(collection, "filter_country", "{0}", "{0}");
            AddUiEntry(collection, "filter_branch", "{0}", "{0}");
            AddUiEntry(collection, "tab_browse", "Справочник", "Browse");
            AddUiEntry(collection, "tab_index", "Индекс", "Index");
            AddUiEntry(collection, "tab_settings", "Настройки", "Settings");
            AddUiEntry(collection, "btn_back", "Назад", "Back");
            AddUiEntry(collection, "btn_next", "Далее", "Next");
            AddUiEntry(collection, "settings_language", "Язык интерфейса", "Interface language");
            AddUiEntry(collection, "btn_russian", "Русский", "Russian");
            AddUiEntry(collection, "btn_english", "English", "English");
            AddUiEntry(collection, "settings_font_size", "Размер шрифта", "Font size");
            AddUiEntry(collection, "btn_font_normal", "Обычный", "Normal");
            AddUiEntry(collection, "btn_font_large", "Крупный", "Large");
            AddUiEntry(collection, "btn_font_extra_large", "Очень крупный", "Extra large");
            AddUiEntry(collection, "settings_theme", "Тема оформления", "Appearance theme");
            AddUiEntry(collection, "btn_theme_dark", "Тёмная", "Dark");
            AddUiEntry(collection, "btn_theme_light", "Светлая", "Light");
            AddUiEntry(collection, "btn_theme_glass", "Стекло", "Glass");
            AddUiEntry(collection, "empty_list", "Нет математиков по выбранному фильтру", "No mathematicians for this filter");
            AddUiEntry(collection, "empty_index", "Нет математиков на эту букву", "No mathematicians for this letter");
            AddUiEntry(collection, "search_placeholder", "Имя, биография, раздел…", "Name, bio, branch…");
            AddUiEntry(collection, "search_results_title", "Поиск: {0}", "Search: {0}");
            AddUiEntry(collection, "search_results_count", "{0} найдено", "{0} found");
            AddUiEntry(collection, "empty_search", "Ничего не найдено", "No results");
            AddUiEntry(collection, "gallery_license", "Лицензия", "License");
            AddUiEntry(collection, "gallery_source", "Источник", "Source");
            AddUiEntry(collection, "gallery_no_images", "Изображения недоступны", "No images available");
            AddUiEntry(collection, "share_chooser_title", "Поделиться", "Share");
            AddUiEntry(collection, "title_favorites", "Избранное", "Favorites");
            AddUiEntry(collection, "btn_favorites", "Избранное", "Favorites");
            AddUiEntry(collection, "empty_favorites",
                "Пока нет избранных математиков. Нажмите ♥ на карточке, чтобы добавить.",
                "No favorites yet. Tap ♥ on a card to add one.");

            if (!LocalizationEditorSettings.GetLocales().Contains(ru))
                LocalizationEditorSettings.AddLocale(ru);
            if (!LocalizationEditorSettings.GetLocales().Contains(en))
                LocalizationEditorSettings.AddLocale(en);

            var locSettings = LocalizationEditorSettings.ActiveLocalizationSettings;
            if (locSettings == null)
            {
                locSettings = ScriptableObject.CreateInstance<LocalizationSettings>();
                AssetDatabase.CreateAsset(locSettings, $"{LocalizationFolder}/Localization Settings.asset");
                LocalizationEditorSettings.ActiveLocalizationSettings = locSettings;
            }

            EditorUtility.SetDirty(collection);

            return new LocalizationRefs
            {
                Russian = ru,
                English = en,
                UiCollection = collection,
                HomeTitle = MakeLocalized(collection, "title_home"),
                IndexTitle = MakeLocalized(collection, "title_index"),
                SettingsTitle = MakeLocalized(collection, "title_settings"),
                FavoritesTitle = MakeLocalized(collection, "title_favorites"),
                DetailTitle = MakeLocalized(collection, "title_detail"),
            };
        }

        static void AddUiEntry(StringTableCollection collection, string key, string ru, string en)
        {
            var shared = collection.SharedData;
            var sharedEntry = shared.GetEntry(key);
            if (sharedEntry == null)
                sharedEntry = shared.AddKey(key);

            SetTableValue(collection, "ru", sharedEntry.Id, ru);
            SetTableValue(collection, "en", sharedEntry.Id, en);
            EditorUtility.SetDirty(shared);
        }

        static void SetTableValue(StringTableCollection collection, string localeCode, long id, string value)
        {
            var table = collection.GetTable(localeCode) as StringTable;
            if (table == null)
                return;

            var entry = table.GetEntry(id);
            if (entry == null)
                entry = table.AddEntry(id, value);
            else
                entry.Value = value;
            EditorUtility.SetDirty(table);
        }

        static LocalizedString MakeLocalized(StringTableCollection collection, string key)
        {
            var entry = collection.SharedData.GetEntry(key);
            if (entry == null)
            {
                Debug.LogError($"Localization key missing: {key}");
                return new LocalizedString
                {
                    TableReference = collection.TableCollectionNameReference,
                    TableEntryReference = key
                };
            }

            return new LocalizedString
            {
                TableReference = collection.TableCollectionNameReference,
                TableEntryReference = entry.Id
            };
        }

        static void EditPrefabContents(string path, System.Action<GameObject> edit)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                edit(root);
                RemoveMissingScripts(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        public static void RemoveMissingScripts(GameObject root)
        {
            if (root == null)
                return;

            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(root);
            foreach (Transform child in root.transform)
                RemoveMissingScripts(child.gameObject);
        }

        static void EnsureListItemInResources()
        {
            const string src = "Assets/Prefabs/UI/MathematicianListItem.prefab";
            const string dst = "Assets/Resources/MathematicianListItem.prefab";
            if (AssetDatabase.LoadAssetAtPath<MathematicianListItem>(dst) != null)
                return;
            if (!AssetDatabase.LoadAssetAtPath<GameObject>(src))
                return;
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            AssetDatabase.CopyAsset(src, dst);
        }

        static void SyncListItemResourcesCopy(string sourcePath)
        {
            const string dst = "Assets/Resources/MathematicianListItem.prefab";
            if (!AssetDatabase.LoadAssetAtPath<GameObject>(dst))
                return;

            AssetDatabase.CopyAsset(sourcePath, dst);
        }

        static MathematicianListItem CreateListItemPrefab()
        {
            var path = $"{PrefabFolder}/MathematicianListItem.prefab";
            var root = BuildListItemRoot();
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            SyncListItemResourcesCopy(path);
            return prefab.GetComponent<MathematicianListItem>();
        }

        static GameObject BuildListItemRoot()
        {
            var root = new GameObject("MathematicianListItem", typeof(RectTransform), typeof(Image), typeof(Button));
            root.GetComponent<Image>().color = UiTheme.CardFill;

            CreateTmpChild(root.transform, "Name", UiLayoutMetrics.ListItemNameBaseFontSize, FontStyles.Bold, UiLayoutMetrics.ListItemNamePos);
            CreateTmpChild(root.transform, "Dates", UiLayoutMetrics.ListItemDatesBaseFontSize, FontStyles.Normal, UiLayoutMetrics.ListItemDatesPos);
            CreateTmpChild(root.transform, "Bio", UiLayoutMetrics.ListItemBioBaseFontSize, FontStyles.Normal, UiLayoutMetrics.ListItemBioPos);
            CreateListItemPortrait(root);
            ConfigureListItem(root);

            var item = root.AddComponent<MathematicianListItem>();
            var so = new SerializedObject(item);
            so.FindProperty("nameText").objectReferenceValue = root.transform.Find("Name").GetComponent<TMP_Text>();
            so.FindProperty("datesText").objectReferenceValue = root.transform.Find("Dates").GetComponent<TMP_Text>();
            so.FindProperty("bioText").objectReferenceValue = root.transform.Find("Bio").GetComponent<TMP_Text>();
            so.FindProperty("portraitImage").objectReferenceValue = root.transform.Find("Portrait").GetComponent<Image>();
            so.FindProperty("button").objectReferenceValue = root.GetComponent<Button>();
            so.FindProperty("shareButton").objectReferenceValue = ConfigureShareButton(root.transform);
            so.FindProperty("favoriteButton").objectReferenceValue = ConfigureFavoriteButton(root.transform);
            so.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }

        static void ConfigureLayoutRect(RectTransform rt, float height)
        {
            if (rt == null)
                return;

            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0f, height);
        }

        public static void ConfigureListItem(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            ConfigureLayoutRect(rt, UiLayoutMetrics.ListItemRowHeight);

            var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            le.preferredHeight = UiLayoutMetrics.ListItemRowHeight;
            le.flexibleWidth = 1f;

            if (go.GetComponent<RectMask2D>() == null)
                go.AddComponent<RectMask2D>();

            ConfigureListItemPortrait(go);

            ConfigureListItemText(go, "Name", UiLayoutMetrics.ListItemNameFontSize, FontStyles.Bold,
                UiLayoutMetrics.ListItemNamePos, UiLayoutMetrics.ListItemNameHeight, truncate: false, UiTheme.TextPrimary);
            ConfigureListItemText(go, "Dates", UiLayoutMetrics.ListItemDatesFontSize, FontStyles.Normal,
                UiLayoutMetrics.ListItemDatesPos, UiLayoutMetrics.ListItemTextLineHeight, truncate: false, UiTheme.TextSecondary);
            ConfigureListItemText(go, "Bio", UiLayoutMetrics.ListItemBioFontSize, FontStyles.Normal,
                UiLayoutMetrics.ListItemBioPos, UiLayoutMetrics.ListItemBioHeight, truncate: true, UiTheme.TextSecondary);

            UiStyleBuilder.ApplyCardStyle(go, UiCardVariant.ListItem);
            ConfigureLayoutRect(go.GetComponent<RectTransform>(), UiLayoutMetrics.ListItemRowHeight);
            EnsureThemedCard(go, UiCardVariant.ListItem);
            ConfigureListItemShareButton(go);
            ConfigureListItemFavoriteButton(go);
        }

        static void ConfigureListItemFavoriteButton(GameObject root)
        {
            var favoriteButton = ConfigureFavoriteButton(root.transform);
            var item = root.GetComponent<MathematicianListItem>();
            if (item == null)
                return;

            var so = new SerializedObject(item);
            so.FindProperty("favoriteButton").objectReferenceValue = favoriteButton;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void ConfigureListItemShareButton(GameObject root)
        {
            var shareButton = ConfigureShareButton(root.transform);
            var item = root.GetComponent<MathematicianListItem>();
            if (item == null)
                return;

            var so = new SerializedObject(item);
            so.FindProperty("shareButton").objectReferenceValue = shareButton;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static ShareIconButton ConfigureShareButton(Transform parent, Vector2? anchoredPosition = null)
        {
            var shareSize = UiLayoutMetrics.SearchBarClearButtonWidth;
            var position = anchoredPosition ?? new Vector2(-12f, -12f);

            var existing = parent.Find("ShareButton");
            GameObject shareGo;
            if (existing != null)
                shareGo = existing.gameObject;
            else
            {
                shareGo = new GameObject(
                    "ShareButton",
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(Button),
                    typeof(ShareIconButton));
                shareGo.transform.SetParent(parent, false);
            }

            shareGo.transform.SetAsLastSibling();

            var rt = shareGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = position;
            rt.sizeDelta = new Vector2(shareSize, shareSize);

            var iconTransform = shareGo.transform.Find("Icon");
            GameObject iconGo;
            if (iconTransform == null)
            {
                iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconGo.transform.SetParent(shareGo.transform, false);
            }
            else
            {
                iconGo = iconTransform.gameObject;
            }

            var iconRt = iconGo.GetComponent<RectTransform>();
            StretchToParent(iconRt);
            iconRt.offsetMin = new Vector2(8f, 8f);
            iconRt.offsetMax = new Vector2(-8f, -8f);

            var layoutElement = shareGo.GetComponent<LayoutElement>() ?? shareGo.AddComponent<LayoutElement>();
            layoutElement.ignoreLayout = true;
            layoutElement.minWidth = shareSize;
            layoutElement.minHeight = shareSize;
            layoutElement.preferredWidth = shareSize;
            layoutElement.preferredHeight = shareSize;

            var shareButton = shareGo.GetComponent<ShareIconButton>();
            var so = new SerializedObject(shareButton);
            so.FindProperty("iconImage").objectReferenceValue = iconGo.GetComponent<Image>();
            so.ApplyModifiedPropertiesWithoutUndo();
            return shareButton;
        }

        public static FavoriteIconButton ConfigureFavoriteButton(Transform parent, Vector2? anchoredPosition = null)
        {
            var buttonSize = UiLayoutMetrics.SearchBarClearButtonWidth;
            var shareOffset = 12f;
            var position = anchoredPosition ?? new Vector2(
                -shareOffset - buttonSize - ListItemLayoutMetrics.ActionButtonGap,
                -shareOffset);

            var existing = parent.Find("FavoriteButton");
            GameObject favoriteGo;
            if (existing != null)
                favoriteGo = existing.gameObject;
            else
            {
                favoriteGo = new GameObject(
                    "FavoriteButton",
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(Button),
                    typeof(FavoriteIconButton));
                favoriteGo.transform.SetParent(parent, false);
            }

            favoriteGo.transform.SetAsLastSibling();

            var rt = favoriteGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = position;
            rt.sizeDelta = new Vector2(buttonSize, buttonSize);

            var iconTransform = favoriteGo.transform.Find("Icon");
            GameObject iconGo;
            if (iconTransform == null)
            {
                iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconGo.transform.SetParent(favoriteGo.transform, false);
            }
            else
            {
                iconGo = iconTransform.gameObject;
            }

            var iconRt = iconGo.GetComponent<RectTransform>();
            StretchToParent(iconRt);
            iconRt.offsetMin = new Vector2(8f, 8f);
            iconRt.offsetMax = new Vector2(-8f, -8f);

            var layoutElement = favoriteGo.GetComponent<LayoutElement>() ?? favoriteGo.AddComponent<LayoutElement>();
            layoutElement.ignoreLayout = true;
            layoutElement.minWidth = buttonSize;
            layoutElement.minHeight = buttonSize;
            layoutElement.preferredWidth = buttonSize;
            layoutElement.preferredHeight = buttonSize;

            var favoriteButton = favoriteGo.GetComponent<FavoriteIconButton>();
            var so = new SerializedObject(favoriteButton);
            so.FindProperty("iconImage").objectReferenceValue = iconGo.GetComponent<Image>();
            so.ApplyModifiedPropertiesWithoutUndo();
            return favoriteButton;
        }

        static void CreateListItemPortrait(GameObject root)
        {
            var portrait = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
            portrait.transform.SetParent(root.transform, false);
        }

        public static void ConfigureListItemPortrait(GameObject root)
        {
            var portraitTransform = root.transform.Find("Portrait");
            GameObject portraitGo;
            if (portraitTransform == null)
            {
                CreateListItemPortrait(root);
                portraitTransform = root.transform.Find("Portrait");
            }

            portraitGo = portraitTransform.gameObject;
            portraitTransform.SetAsFirstSibling();

            var rt = portraitGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(UiLayoutMetrics.ListItemLeftPadding, 0f);
            rt.sizeDelta = new Vector2(
                UiLayoutMetrics.ListItemThumbnailSize,
                UiLayoutMetrics.ListItemThumbnailSize);

            var img = portraitGo.GetComponent<Image>() ?? portraitGo.AddComponent<Image>();
            img.preserveAspect = true;
            img.raycastTarget = false;
            img.color = UiTheme.PortraitPlaceholder;

            var item = root.GetComponent<MathematicianListItem>();
            if (item != null)
            {
                var so = new SerializedObject(item);
                so.FindProperty("portraitImage").objectReferenceValue = img;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        static void ConfigureListItemText(
            GameObject root,
            string childName,
            float fontSize,
            FontStyles style,
            Vector2 anchoredPos,
            float height,
            bool truncate,
            Color textColor)
        {
            var child = root.transform.Find(childName);
            if (child == null)
                return;

            var childRt = child.GetComponent<RectTransform>();
            childRt.anchoredPosition = anchoredPos;
            childRt.sizeDelta = new Vector2(-UiLayoutMetrics.ListItemTextWidthInset, height);

            var tmp = child.GetComponent<TextMeshProUGUI>();
            if (tmp == null)
                return;

            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = textColor;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.overflowMode = truncate ? TextOverflowModes.Ellipsis : TextOverflowModes.Overflow;
            var so = new SerializedObject(tmp);
            var baseProp = so.FindProperty("m_fontSizeBase");
            if (baseProp != null)
                baseProp.floatValue = fontSize;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static GameObject CreateTmpChild(Transform parent, string name, float size, FontStyles style, Vector2 anchoredPos)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(-20, 24);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = UiLayoutMetrics.ScaleFont(size);
            tmp.fontStyle = style;
            tmp.color = UiTheme.TextPrimary;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            try
            {
                var font = TMP_Settings.defaultFontAsset;
                if (font != null)
                    tmp.font = font;
            }
            catch
            {
                // TMP Essential Resources may not be imported yet; labels still work after import.
            }
            return go;
        }

        static void CreateMainScene(
            List<MathematicianData> mathematicians,
            LocalizationRefs loc,
            MathematicianListItem listItemPrefab)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var app = new GameObject("App");
            var bootstrap = app.AddComponent<AppBootstrap>();
            var navigation = app.AddComponent<NavigationController>();
            var repository = app.AddComponent<MathematicianRepository>();
            app.AddComponent<BackButtonHandler>();

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = UiTheme.Background;
            camGo.AddComponent<UniversalAdditionalCameraData>();

            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<InputSystemUIInputModule>();

            var canvasGo = CreateCanvas();
            CreateGlassBackdrop(canvasGo.transform);
            var header = CreateHeader(canvasGo.transform, loc);
            var content = CreateContentArea(canvasGo.transform);
            var home = CreateHomePanel(content.transform, navigation, loc);
            var index = CreateIndexPanel(content.transform, navigation, repository, listItemPrefab, loc);
            var list = CreateListPanel(content.transform, navigation, repository, listItemPrefab, loc);
            var favorites = CreateFavoritesPanel(content.transform, navigation, repository, listItemPrefab, loc);
            var headerBinder = header.root.GetComponent<HeaderTitleBinder>();
            var detail = CreateDetailPanel(content.transform, repository, navigation, headerBinder, loc);
            var settings = CreateSettingsPanel(content.transform, loc);
            var bottom = CreateBottomBar(canvasGo.transform, navigation, loc);

            WireNavigation(navigation, home, index, list, favorites, detail, settings, header.backButton, headerBinder, bottom);
            WireBootstrap(bootstrap, navigation);
            WireBackHandler(app.GetComponent<BackButtonHandler>(), navigation);
            WireThemeScope(canvasGo, cam, navigation, settings, detail);

            AssignMathematicians(repository, mathematicians);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        static GameObject CreateCanvas()
        {
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            return canvasGo;
        }

        struct HeaderResult
        {
            public GameObject root;
            public GameObject backButton;
        }

        static HeaderResult CreateHeader(Transform canvas, LocalizationRefs loc)
        {
            var header = CreatePanel(canvas, "Header", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -120), new Vector2(0, 0));
            UiStyleBuilder.ApplyNavBarStyle(header);

            var homeTitle = CreateLocalizedTitle(header.transform, "HomeTitle", loc.HomeTitle);
            ConfigureHomeTitle(homeTitle);
            var settingsTitle = CreateLocalizedTitle(header.transform, "SettingsTitle", loc.SettingsTitle);
            settingsTitle.SetActive(false);
            var indexTitle = CreateLocalizedTitle(header.transform, "IndexTitle", loc.IndexTitle);
            indexTitle.SetActive(false);
            var favoritesTitle = CreateLocalizedTitle(header.transform, "FavoritesTitle", loc.FavoritesTitle);
            favoritesTitle.SetActive(false);

            var plainTitle = CreateTmpChild(header.transform, "PlainTitle", 22, FontStyles.Bold, new Vector2(180, -50));
            plainTitle.GetComponent<RectTransform>().sizeDelta = new Vector2(-200, 40);
            plainTitle.GetComponent<TextMeshProUGUI>().raycastTarget = false;
            plainTitle.SetActive(false);

            var back = CreateSceneButton(header.transform, UiButtonLayout.HeaderBack, loc.UiCollection);
            back.SetActive(false);
            back.transform.SetAsLastSibling();

            var binder = header.AddComponent<HeaderTitleBinder>();
            var so = new SerializedObject(binder);
            so.FindProperty("titleText").objectReferenceValue = plainTitle.GetComponent<TMP_Text>();
            so.FindProperty("homeTitleEvent").objectReferenceValue = homeTitle.GetComponent<LocalizeStringEvent>();
            so.FindProperty("indexTitleEvent").objectReferenceValue = indexTitle.GetComponent<LocalizeStringEvent>();
            so.FindProperty("settingsTitleEvent").objectReferenceValue = settingsTitle.GetComponent<LocalizeStringEvent>();
            so.FindProperty("favoritesTitleEvent").objectReferenceValue = favoritesTitle.GetComponent<LocalizeStringEvent>();
            AssignLocalized(so.FindProperty("detailTitle"), loc.DetailTitle);
            so.ApplyModifiedPropertiesWithoutUndo();

            return new HeaderResult { root = header, backButton = back };
        }

        static void AssignLocalized(SerializedProperty prop, LocalizedString source)
        {
            var tableRef = prop.FindPropertyRelative("m_TableReference");
            tableRef.FindPropertyRelative("m_TableCollectionName").stringValue =
                source.TableReference.TableCollectionName;
            var entryRef = prop.FindPropertyRelative("m_TableEntryReference");
            var keyId = entryRef.FindPropertyRelative("m_KeyId");
            if (keyId != null)
                keyId.longValue = source.TableEntryReference.KeyId;
            var keyName = entryRef.FindPropertyRelative("m_Key");
            if (keyName != null)
                keyName.stringValue = source.TableEntryReference.Key;
        }

        static GameObject CreateLocalizedTitle(Transform parent, string name, LocalizedString localized)
        {
            var go = CreateTmpChild(parent, name, 22, FontStyles.Bold, new Vector2(180, -50));
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(-40, 40);
            go.GetComponent<TextMeshProUGUI>().raycastTarget = false;
            var lse = go.AddComponent<LocalizeStringEvent>();
            var so = new SerializedObject(lse);
            AssignLocalized(so.FindProperty("m_StringReference"), localized);
            so.ApplyModifiedPropertiesWithoutUndo();
            WireLocalizeStringToTmp(go);
            return go;
        }

        public static void WireLocalizeStringToTmp(GameObject go)
        {
            var lse = go.GetComponent<LocalizeStringEvent>();
            var tmp = go.GetComponent<TMP_Text>();
            if (lse == null || tmp == null)
                return;

            while (lse.OnUpdateString.GetPersistentEventCount() > 0)
                UnityEventTools.RemovePersistentListener(lse.OnUpdateString, 0);

            var setStringMethod = typeof(TMP_Text).GetProperty("text")!.GetSetMethod();
            var methodDelegate = (UnityAction<string>)System.Delegate.CreateDelegate(
                typeof(UnityAction<string>), tmp, setStringMethod);
            UnityEventTools.AddPersistentListener(lse.OnUpdateString, methodDelegate);
            lse.OnUpdateString.SetPersistentListenerState(0, UnityEventCallState.EditorAndRuntime);
            EditorUtility.SetDirty(lse);
        }

        public static void ConfigureHomeTitle(GameObject go)
        {
            if (go == null)
                return;

            var rt = go.GetComponent<RectTransform>();
            if (rt == null)
                return;

            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, -16);
            rt.sizeDelta = new Vector2(-48, 120);

            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (tmp == null)
                return;

            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 18;
            tmp.fontSizeMax = 40;
            tmp.fontSize = 36;
            tmp.color = UiTheme.TextPrimary;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.horizontalAlignment = HorizontalAlignmentOptions.Center;
            tmp.verticalAlignment = VerticalAlignmentOptions.Top;
            WireLocalizeStringToTmp(go);

            var lse = go.GetComponent<LocalizeStringEvent>();
            if (lse == null)
                return;

            var lseSo = new SerializedObject(lse);
            var waitProp = lseSo.FindProperty("m_WaitForCompletion");
            if (waitProp != null)
                waitProp.boolValue = true;
            lseSo.ApplyModifiedPropertiesWithoutUndo();
            lse.enabled = false;
        }

        static GameObject CreateContentArea(Transform canvas)
        {
            return CreatePanel(canvas, "ContentArea", new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 220), new Vector2(0, -120));
        }

        static GameObject CreateHomePanel(Transform parent, NavigationController nav, LocalizationRefs loc)
        {
            var panel = CreatePanel(parent, "HomePanel", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            AddHomeDecorGlow(panel.transform);
            var searchBar = CreateSearchBar(panel.transform, nav, loc);
            var scroll = CreateScrollView(panel.transform, "HomeScroll");
            PinHomeSearchAndScroll(searchBar, scroll);
            var content = scroll.content;

            AddSectionLabel(content, loc.UiCollection, "section_century");
            var centuryBox = CreateVerticalGroup(content, "CenturyGroup");
            AddSectionLabel(content, loc.UiCollection, "section_country");
            var countryBox = CreateVerticalGroup(content, "CountryGroup");
            AddSectionLabel(content, loc.UiCollection, "section_branch");
            var branchBox = CreateVerticalGroup(content, "BranchGroup");

            var home = panel.AddComponent<HomePanel>();
            var filterPrefab = CreateFilterButtonPrefab();
            var so = new SerializedObject(home);
            so.FindProperty("navigation").objectReferenceValue = nav;
            so.FindProperty("searchBar").objectReferenceValue = searchBar.GetComponent<SearchBar>();
            so.FindProperty("centuryContainer").objectReferenceValue = centuryBox;
            so.FindProperty("countryContainer").objectReferenceValue = countryBox;
            so.FindProperty("branchContainer").objectReferenceValue = branchBox;
            so.FindProperty("filterButtonPrefab").objectReferenceValue = filterPrefab;
            so.ApplyModifiedPropertiesWithoutUndo();
            panel.SetActive(true);
            return panel;
        }

        static void AddHomeDecorGlow(Transform panel)
        {
            var go = new GameObject("DecorGlow", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(panel, false);
            go.transform.SetAsFirstSibling();
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, -40);
            rt.sizeDelta = new Vector2(900, 360);
            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            UiSpriteFactory.EnsureSprites();
            image.sprite = UiSpriteFactory.RoundedRect;
            image.type = Image.Type.Sliced;
            image.color = new Color(0.749f, 0.353f, 0.949f, 0.12f);
        }

        static GameObject CreateSearchBar(Transform parent, NavigationController nav, LocalizationRefs loc)
        {
            var path = $"{PrefabFolder}/SearchBar.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(existing, parent);
                WireSearchBar(instance, nav);
                return instance;
            }

            var go = BuildSearchBarRoot();
            ConfigureSearchBar(go);
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            var placed = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            WireSearchBar(placed, nav);
            return placed;
        }

        static void WireSearchBar(GameObject go, NavigationController nav)
        {
            var searchBar = go.GetComponent<SearchBar>();
            if (searchBar == null)
                return;

            var so = new SerializedObject(searchBar);
            so.FindProperty("navigation").objectReferenceValue = nav;
            so.FindProperty("inputField").objectReferenceValue = go.GetComponentInChildren<TMP_InputField>(true);
            so.FindProperty("clearButton").objectReferenceValue = go.transform.Find("ClearButton")?.GetComponent<Button>();
            so.FindProperty("themedCard").objectReferenceValue = go.GetComponent<UiThemedCard>();
            so.FindProperty("glowImage").objectReferenceValue = go.transform.Find("Glow")?.GetComponent<Image>();
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static GameObject BuildSearchBarRoot()
        {
            var go = new GameObject("SearchBar", typeof(RectTransform), typeof(Image), typeof(SearchBar), typeof(LayoutElement));
            var le = go.GetComponent<LayoutElement>();
            le.preferredHeight = UiLayoutMetrics.SearchBarHeight;
            le.minHeight = UiLayoutMetrics.SearchBarHeight;

            var icon = CreateTmpChild(
                go.transform,
                "Icon",
                UiLayoutMetrics.SearchBarBaseFontSize,
                FontStyles.Normal,
                new Vector2(UiLayoutMetrics.SearchBarIconInset, -UiLayoutMetrics.SearchBarHeight * 0.5f));
            var iconRt = icon.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0, 0.5f);
            iconRt.anchorMax = new Vector2(0, 0.5f);
            iconRt.pivot = new Vector2(0, 0.5f);
            iconRt.sizeDelta = new Vector2(48, 48);
            var iconTmp = icon.GetComponent<TextMeshProUGUI>();
            iconTmp.text = "\u2315";
            iconTmp.alignment = TextAlignmentOptions.Center;
            iconTmp.color = UiTheme.TextSecondary;

            var inputRoot = new GameObject("InputArea", typeof(RectTransform));
            inputRoot.transform.SetParent(go.transform, false);
            var inputRt = inputRoot.GetComponent<RectTransform>();
            StretchToParent(inputRt);
            inputRt.offsetMin = new Vector2(UiLayoutMetrics.SearchBarIconInset + 40f, 8f);
            inputRt.offsetMax = new Vector2(-UiLayoutMetrics.SearchBarClearButtonWidth, -8f);

            var textArea = new GameObject("TextArea", typeof(RectTransform), typeof(RectMask2D));
            textArea.transform.SetParent(inputRoot.transform, false);
            StretchToParent(textArea.GetComponent<RectTransform>());

            var placeholder = CreateTmpChild(
                textArea.transform,
                "Placeholder",
                UiLayoutMetrics.SearchBarBaseFontSize,
                FontStyles.Italic,
                Vector2.zero);
            StretchToParent(placeholder.GetComponent<RectTransform>());
            var placeholderTmp = placeholder.GetComponent<TextMeshProUGUI>();
            placeholderTmp.color = UiTheme.TextSecondary;
            placeholderTmp.alignment = TextAlignmentOptions.MidlineLeft;

            var text = CreateTmpChild(
                textArea.transform,
                "Text",
                UiLayoutMetrics.SearchBarBaseFontSize,
                FontStyles.Normal,
                Vector2.zero);
            StretchToParent(text.GetComponent<RectTransform>());
            var textTmp = text.GetComponent<TextMeshProUGUI>();
            textTmp.alignment = TextAlignmentOptions.MidlineLeft;

            var inputField = inputRoot.AddComponent<TMP_InputField>();
            inputField.textViewport = textArea.GetComponent<RectTransform>();
            inputField.textComponent = textTmp;
            inputField.placeholder = placeholderTmp;
            inputField.lineType = TMP_InputField.LineType.SingleLine;
            inputField.characterValidation = TMP_InputField.CharacterValidation.None;

            var clearGo = new GameObject("ClearButton", typeof(RectTransform), typeof(Image), typeof(Button));
            clearGo.transform.SetParent(go.transform, false);
            var clearRt = clearGo.GetComponent<RectTransform>();
            clearRt.anchorMin = new Vector2(1, 0.5f);
            clearRt.anchorMax = new Vector2(1, 0.5f);
            clearRt.pivot = new Vector2(1, 0.5f);
            clearRt.anchoredPosition = new Vector2(-12f, 0f);
            clearRt.sizeDelta = new Vector2(UiLayoutMetrics.SearchBarClearButtonWidth, UiLayoutMetrics.SearchBarHeight - 16f);
            clearGo.GetComponent<Image>().color = Color.clear;
            var clearLabel = CreateTmpChild(
                clearGo.transform,
                "Label",
                UiLayoutMetrics.SearchBarBaseFontSize,
                FontStyles.Bold,
                Vector2.zero);
            StretchToParent(clearLabel.GetComponent<RectTransform>());
            var clearTmp = clearLabel.GetComponent<TextMeshProUGUI>();
            clearTmp.text = "\u00d7";
            clearTmp.alignment = TextAlignmentOptions.Center;
            clearTmp.color = UiTheme.TextSecondary;
            clearGo.SetActive(false);

            return go;
        }

        public static void ConfigureSearchBar(GameObject go)
        {
            ConfigureLayoutRect(go.GetComponent<RectTransform>(), UiLayoutMetrics.SearchBarHeight);

            var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            le.preferredHeight = UiLayoutMetrics.SearchBarHeight;
            le.minHeight = UiLayoutMetrics.SearchBarHeight;
            le.flexibleWidth = 1f;

            var inputField = go.GetComponentInChildren<TMP_InputField>(true);
            if (inputField != null)
            {
                if (inputField.textComponent != null)
                {
                    inputField.textComponent.fontSize = UiLayoutMetrics.SearchBarFontSize;
                    inputField.textComponent.color = UiTheme.TextPrimary;
                }

                if (inputField.placeholder is TextMeshProUGUI placeholder)
                {
                    placeholder.fontSize = UiLayoutMetrics.SearchBarFontSize;
                    placeholder.color = UiTheme.TextSecondary;
                }
            }

            var icon = go.transform.Find("Icon")?.GetComponent<TextMeshProUGUI>();
            if (icon != null)
            {
                icon.fontSize = UiLayoutMetrics.SearchBarFontSize;
                icon.color = UiTheme.TextSecondary;
            }

            UiStyleBuilder.ApplyCardStyle(go, UiCardVariant.Filter);
            EnsureThemedCard(go, UiCardVariant.Filter);
            EditorUtility.SetDirty(go);
        }

        static void PinHomeSearchAndScroll(GameObject searchBar, ScrollRect scroll)
        {
            var searchRt = searchBar.GetComponent<RectTransform>();
            searchRt.anchorMin = new Vector2(0, 1);
            searchRt.anchorMax = new Vector2(1, 1);
            searchRt.pivot = new Vector2(0.5f, 1);
            searchRt.anchoredPosition = new Vector2(0, -UiLayoutMetrics.SearchBarMarginTop);
            searchRt.sizeDelta = new Vector2(
                -(UiLayoutMetrics.BrowseScrollPaddingLeft + UiLayoutMetrics.BrowseScrollPaddingRight),
                UiLayoutMetrics.SearchBarHeight);
            searchRt.offsetMin = new Vector2(UiLayoutMetrics.BrowseScrollPaddingLeft, searchRt.offsetMin.y);
            searchRt.offsetMax = new Vector2(-UiLayoutMetrics.BrowseScrollPaddingRight, -UiLayoutMetrics.SearchBarMarginTop);

            var scrollRt = scroll.GetComponent<RectTransform>();
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = Vector2.zero;
            scrollRt.offsetMax = new Vector2(0, -UiLayoutMetrics.SearchBarTotalTopInset);
        }

        public static void ConfigureFilterButton(GameObject go)
        {
            ConfigureLayoutRect(go.GetComponent<RectTransform>(), UiLayoutMetrics.FilterButtonHeight);

            var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            le.preferredWidth = -1f;
            le.flexibleWidth = 1f;
            le.preferredHeight = UiLayoutMetrics.FilterButtonHeight;
            le.minHeight = UiLayoutMetrics.FilterButtonHeight;

            var btnFitter = go.GetComponent<ContentSizeFitter>();
            if (btnFitter != null)
                Object.DestroyImmediate(btnFitter);

            var label = go.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                var fontSizeMax = UiLayoutMetrics.FilterButtonFontSize;
                var fontSizeMin = UiLayoutMetrics.FilterButtonFontSizeMin;
                label.fontSize = fontSizeMax;
                label.enableAutoSizing = true;
                label.fontSizeMin = fontSizeMin;
                label.fontSizeMax = fontSizeMax;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.overflowMode = TextOverflowModes.Ellipsis;
                var so = new SerializedObject(label);
                var baseProp = so.FindProperty("m_fontSizeBase");
                if (baseProp != null)
                    baseProp.floatValue = fontSizeMax;
                so.ApplyModifiedPropertiesWithoutUndo();

                var labelRt = label.rectTransform;
                if (labelRt != null)
                {
                    labelRt.anchorMin = new Vector2(0f, 1f);
                    labelRt.anchorMax = new Vector2(1f, 1f);
                    labelRt.pivot = new Vector2(0f, 1f);
                    labelRt.anchoredPosition = UiLayoutMetrics.FilterButtonLabelOffset;
                    labelRt.sizeDelta = new Vector2(
                        -UiLayoutMetrics.FilterButtonLabelHorizontalInset,
                        UiLayoutMetrics.FilterButtonLabelHeight);
                }

                var labelLe = label.GetComponent<LayoutElement>();
                if (labelLe != null)
                    Object.DestroyImmediate(labelLe);

                var labelFitter = label.GetComponent<ContentSizeFitter>();
                if (labelFitter != null)
                    Object.DestroyImmediate(labelFitter);

                var layoutHeight = label.GetComponent<TmpLayoutHeight>();
                if (layoutHeight != null)
                    Object.DestroyImmediate(layoutHeight);

                label.color = UiTheme.TextPrimary;
            }

            UiStyleBuilder.ApplyCardStyle(go, UiCardVariant.Filter);
            ConfigureLayoutRect(go.GetComponent<RectTransform>(), UiLayoutMetrics.FilterButtonHeight);
            EnsureThemedCard(go, UiCardVariant.Filter);
            EditorUtility.SetDirty(go);
        }

        public static void ConfigureDetailTagButton(GameObject go)
        {
            var height = UiLayoutMetrics.ScaleDetailSize(72f);
            ConfigureLayoutRect(go.GetComponent<RectTransform>(), height);

            var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            le.preferredWidth = -1f;
            le.flexibleWidth = 1f;
            le.preferredHeight = height;
            le.minHeight = height;

            var btnFitter = go.GetComponent<ContentSizeFitter>();
            if (btnFitter != null)
                Object.DestroyImmediate(btnFitter);

            var label = go.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                var fontSizeMax = UiLayoutMetrics.ScaleDetailFont(15f);
                var fontSizeMin = fontSizeMax * 0.5f;
                label.fontSize = fontSizeMax;
                label.enableAutoSizing = true;
                label.fontSizeMin = fontSizeMin;
                label.fontSizeMax = fontSizeMax;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.overflowMode = TextOverflowModes.Ellipsis;
                label.alignment = TextAlignmentOptions.Center;
                var so = new SerializedObject(label);
                var baseProp = so.FindProperty("m_fontSizeBase");
                if (baseProp != null)
                    baseProp.floatValue = fontSizeMax;
                so.ApplyModifiedPropertiesWithoutUndo();

                var labelRt = label.rectTransform;
                if (labelRt != null)
                {
                    labelRt.anchorMin = Vector2.zero;
                    labelRt.anchorMax = Vector2.one;
                    labelRt.pivot = new Vector2(0.5f, 0.5f);
                    labelRt.anchoredPosition = Vector2.zero;
                    labelRt.sizeDelta = Vector2.zero;
                }

                var labelLe = label.GetComponent<LayoutElement>();
                if (labelLe != null)
                    Object.DestroyImmediate(labelLe);

                var labelFitter = label.GetComponent<ContentSizeFitter>();
                if (labelFitter != null)
                    Object.DestroyImmediate(labelFitter);

                var layoutHeight = label.GetComponent<TmpLayoutHeight>();
                if (layoutHeight != null)
                    Object.DestroyImmediate(layoutHeight);

                label.color = UiTheme.TextPrimary;
            }

            UiStyleBuilder.ApplyCardStyle(go, UiCardVariant.Filter);
            ConfigureLayoutRect(go.GetComponent<RectTransform>(), height);
            EnsureThemedCard(go, UiCardVariant.Filter);
            EditorUtility.SetDirty(go);
        }

        static Button CreateFilterButtonPrefab()
        {
            var path = $"{PrefabFolder}/FilterButton.prefab";
            var go = BuildFilterButtonRoot();
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab.GetComponent<Button>();
        }

        static Button EnsureDetailTagButtonPrefab()
        {
            var path = $"{PrefabFolder}/DetailTagButton.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<Button>(path);
            if (existing != null)
            {
                ConfigureDetailTagButton(existing.gameObject);
                PrefabUtility.SavePrefabAsset(existing.gameObject);
                return existing;
            }

            return CreateDetailTagButtonPrefab();
        }

        static Button CreateDetailTagButtonPrefab()
        {
            var path = $"{PrefabFolder}/DetailTagButton.prefab";
            var go = BuildDetailTagButtonRoot();
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab.GetComponent<Button>();
        }

        static GameObject BuildDetailTagButtonRoot()
        {
            var go = new GameObject("DetailTagButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.GetComponent<Image>().color = UiTheme.CardFill;
            var label = CreateTmpChild(go.transform, "Label", 15, FontStyles.Normal, Vector2.zero);
            label.GetComponent<RectTransform>().anchorMax = Vector2.one;
            ConfigureDetailTagButton(go);
            return go;
        }

        static GameObject BuildFilterButtonRoot()
        {
            var go = new GameObject("FilterButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.GetComponent<Image>().color = UiTheme.CardFill;
            var label = CreateTmpChild(go.transform, "Label", UiLayoutMetrics.FilterButtonBaseFontSize, FontStyles.Normal, UiLayoutMetrics.FilterButtonLabelOffset);
            label.GetComponent<RectTransform>().anchorMax = Vector2.one;
            ConfigureFilterButton(go);
            return go;
        }

        static void AddSectionLabel(Transform parent, StringTableCollection collection, string key)
        {
            var label = CreateTmpChild(parent, key, UiLayoutMetrics.SectionLabelBaseFontSize, FontStyles.Bold, Vector2.zero);
            HomeListPanelLayout.ConfigureSectionLabel(label);
            var le = label.GetComponent<LayoutElement>() ?? label.AddComponent<LayoutElement>();
            le.preferredHeight = UiLayoutMetrics.SectionLabelHeight;
            var lse = label.AddComponent<LocalizeStringEvent>();
            var ls = MakeLocalized(collection, key);
            var so = new SerializedObject(lse);
            AssignLocalized(so.FindProperty("m_StringReference"), ls);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static Transform CreateVerticalGroup(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            go.transform.SetParent(parent, false);
            var vlg = go.GetComponent<VerticalLayoutGroup>();
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;
            HomeListPanelLayout.ConfigureBrowseGroup(vlg);
            var fitter = go.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var le = go.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            return go.transform;
        }

        static ScrollRect CreateScrollView(Transform parent, string name)
        {
            var root = CreatePanel(parent, name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var viewport = CreatePanel(root.transform, "Viewport", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            viewport.GetComponent<Image>().color = UiTheme.ViewportMask;

            var content = CreatePanel(viewport.transform, "Content", new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            var contentRt = content.GetComponent<RectTransform>();
            contentRt.pivot = new Vector2(0.5f, 1);
            contentRt.anchorMin = new Vector2(0, 1);
            contentRt.anchorMax = new Vector2(1, 1);
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            HomeListPanelLayout.ConfigureBrowseScrollContent(vlg);
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;
            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = root.AddComponent<ScrollRect>();
            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = contentRt;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            return scroll;
        }

        struct SectionNavBarResult
        {
            public GameObject backButton;
            public GameObject nextButton;
            public TMP_Text pageIndicator;
        }

        static GameObject CreateDetailPanel(
            Transform parent,
            MathematicianRepository repo,
            NavigationController nav,
            HeaderTitleBinder header,
            LocalizationRefs loc)
        {
            var panel = CreatePanel(parent, "DetailPanel", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            panel.SetActive(false);

            var container = CreatePanel(
                panel.transform,
                "SectionContainer",
                Vector2.zero,
                Vector2.one,
                new Vector2(0, 90),
                Vector2.zero);
            container.GetComponent<Image>().color = Color.clear;
            container.AddComponent<FontSizeScope>();

            var sectionPrefabs = EnsureDetailSectionPrefabs();
            var sections = new List<MathematicianDetailSection>();
            foreach (var prefab in sectionPrefabs)
            {
                var instance = (MathematicianDetailSection)PrefabUtility.InstantiatePrefab(prefab, container.transform);
                StretchToParent(instance.GetComponent<RectTransform>());
                instance.gameObject.SetActive(false);
                sections.Add(instance);

                if (instance is LabeledTextDetailSection labeledSection)
                {
                    var labeledSo = new SerializedObject(labeledSection);
                    labeledSo.FindProperty("navigation").objectReferenceValue = nav;
                    labeledSo.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            var navBar = CreateSectionNavBar(panel.transform, loc.UiCollection);

            var detail = panel.AddComponent<DetailPanel>();
            var so = new SerializedObject(detail);
            so.FindProperty("repository").objectReferenceValue = repo;
            so.FindProperty("headerTitle").objectReferenceValue = header;
            var sectionsProp = so.FindProperty("sections");
            sectionsProp.arraySize = sections.Count;
            for (var i = 0; i < sections.Count; i++)
                sectionsProp.GetArrayElementAtIndex(i).objectReferenceValue = sections[i];
            so.FindProperty("sectionNextButton").objectReferenceValue = navBar.nextButton.GetComponent<Button>();
            so.FindProperty("pageIndicator").objectReferenceValue = navBar.pageIndicator;
            so.ApplyModifiedPropertiesWithoutUndo();

            var swipeNav = container.AddComponent<DetailSectionSwipeNavigator>();
            var swipeSo = new SerializedObject(swipeNav);
            swipeSo.FindProperty("detailPanel").objectReferenceValue = detail;
            swipeSo.ApplyModifiedPropertiesWithoutUndo();

            WireButtonClick(navBar.backButton.GetComponent<Button>(), nav.OnBackButtonClicked);
            WireButtonClick(navBar.nextButton.GetComponent<Button>(), detail.GoNext);
            return panel;
        }

        static SectionNavBarResult CreateSectionNavBar(
            Transform parent,
            StringTableCollection collection)
        {
            var bar = CreatePanel(parent, "SectionNavBar", new Vector2(0, 0), new Vector2(1, 0), Vector2.zero, Vector2.zero);
            UiButtonLayout.ApplyBottomStretchBarRect(
                bar.GetComponent<RectTransform>(),
                UiButtonLayout.SectionNavBarPosition,
                UiButtonLayout.SectionNavBarSize);
            UiStyleBuilder.ApplyNavBarStyle(bar);

            var back = CreateSceneButton(bar.transform, UiButtonLayout.SectionNavBack, collection);
            var next = CreateSceneButton(bar.transform, UiButtonLayout.SectionNavNext, collection);
            var indicatorGo = CreateTmpChild(bar.transform, "PageIndicator", 16, FontStyles.Normal, new Vector2(0, -28));
            var indicatorRt = indicatorGo.GetComponent<RectTransform>();
            indicatorRt.anchorMin = new Vector2(0.5f, 1);
            indicatorRt.anchorMax = new Vector2(0.5f, 1);
            indicatorRt.pivot = new Vector2(0.5f, 1);
            indicatorRt.sizeDelta = new Vector2(200, 32);
            indicatorGo.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
            indicatorGo.GetComponent<TextMeshProUGUI>().color = UiTheme.PrimaryAccent;

            return new SectionNavBarResult
            {
                backButton = back,
                nextButton = next,
                pageIndicator = indicatorGo.GetComponent<TMP_Text>()
            };
        }

        static MathematicianDetailSection[] EnsureDetailSectionPrefabs()
        {
            if (!Directory.Exists(DetailPrefabFolder))
                Directory.CreateDirectory(DetailPrefabFolder);

            return new[]
            {
                SaveDetailSectionPrefab(BuildPortraitSectionPrefab(), $"{DetailPrefabFolder}/DetailSection_Portraits.prefab"),
                SaveDetailSectionPrefab(BuildIdentitySectionPrefab(), $"{DetailPrefabFolder}/DetailSection_Identity.prefab"),
                SaveDetailSectionPrefab(
                    BuildLabeledTextSectionPrefab(LabeledDetailSectionKind.Countries),
                    $"{DetailPrefabFolder}/DetailSection_Countries.prefab"),
                SaveDetailSectionPrefab(
                    BuildLabeledTextSectionPrefab(LabeledDetailSectionKind.Centuries),
                    $"{DetailPrefabFolder}/DetailSection_Centuries.prefab"),
                SaveDetailSectionPrefab(
                    BuildLabeledTextSectionPrefab(LabeledDetailSectionKind.Fields),
                    $"{DetailPrefabFolder}/DetailSection_Fields.prefab"),
                SaveDetailSectionPrefab(
                    BuildScrollTextSectionPrefab(ScrollDetailSectionKind.ShortBio),
                    $"{DetailPrefabFolder}/DetailSection_ShortBio.prefab"),
                SaveDetailSectionPrefab(
                    BuildScrollTextSectionPrefab(ScrollDetailSectionKind.Achievements),
                    $"{DetailPrefabFolder}/DetailSection_Achievements.prefab"),
                SaveDetailSectionPrefab(
                    BuildScrollTextSectionPrefab(ScrollDetailSectionKind.PersonalLife),
                    $"{DetailPrefabFolder}/DetailSection_PersonalLife.prefab"),
                SaveDetailSectionPrefab(
                    BuildScrollTextSectionPrefab(ScrollDetailSectionKind.InterestingFacts),
                    $"{DetailPrefabFolder}/DetailSection_InterestingFacts.prefab"),
                SaveDetailSectionPrefab(BuildExternalLinksSectionPrefab(), $"{DetailPrefabFolder}/DetailSection_ExternalLinks.prefab")
            };
        }

        static MathematicianDetailSection SaveDetailSectionPrefab(GameObject root, string path)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<MathematicianDetailSection>();
        }

        static GameObject CreateStretchSectionRoot(string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            StretchToParent(rt);
            go.GetComponent<Image>().color = Color.clear;
            return go;
        }

        static void StretchToParent(RectTransform rt)
        {
            if (rt == null)
                return;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        static void AddSectionVerticalLayout(GameObject root, int extraRightPadding = 0)
        {
            var vlg = root.GetComponent<VerticalLayoutGroup>() ?? root.AddComponent<VerticalLayoutGroup>();
            ApplySectionVerticalLayout(vlg, extraRightPadding);
        }

        static void ApplySectionVerticalLayout(VerticalLayoutGroup vlg, int extraRightPadding = 0)
        {
            var pad = UiLayoutMetrics.ScaleDetailPadding(UiLayoutMetrics.DetailSectionPadding);
            vlg.padding = new RectOffset(pad, pad + extraRightPadding, pad, pad);
            vlg.spacing = UiLayoutMetrics.ScaleDetailPadding(UiLayoutMetrics.DetailSectionSpacing);
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
        }

        static int IdentityShareLayoutRightInset =>
            (int)UiLayoutMetrics.SearchBarClearButtonWidth + 16;

        static void EnsureIdentityContentStructure(GameObject root)
        {
            var contentTransform = root.transform.Find("Content");
            GameObject contentGo;
            if (contentTransform == null)
            {
                contentGo = new GameObject("Content", typeof(RectTransform));
                contentGo.transform.SetParent(root.transform, false);
                contentGo.transform.SetAsFirstSibling();
                StretchToParent(contentGo.GetComponent<RectTransform>());
            }
            else
            {
                contentGo = contentTransform.gameObject;
            }

            var rootVlg = root.GetComponent<VerticalLayoutGroup>();
            if (rootVlg != null)
                Object.DestroyImmediate(rootVlg);

            ReparentIdentityField(root.transform, "Name", contentGo.transform);
            ReparentIdentityField(root.transform, "Dates", contentGo.transform);
            AddSectionVerticalLayout(contentGo, IdentityShareLayoutRightInset);
        }

        static void ReparentIdentityField(Transform root, string childName, Transform content)
        {
            var child = root.Find(childName);
            if (child != null && child.parent != content)
                child.SetParent(content, false);
        }

        static GameObject BuildPortraitSectionPrefab()
        {
            var root = CreateStretchSectionRoot("DetailSection_Portraits");
            var gallery = CreatePortraitGallery(root.transform, fillParent: true);
            var section = root.AddComponent<PortraitDetailSection>();
            var so = new SerializedObject(section);
            so.FindProperty("gallery").objectReferenceValue = gallery;
            so.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }

        static GameObject BuildIdentitySectionPrefab()
        {
            var root = CreateStretchSectionRoot("DetailSection_Identity");
            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(root.transform, false);
            StretchToParent(content.GetComponent<RectTransform>());
            AddSectionVerticalLayout(content, IdentityShareLayoutRightInset);
            var name = AddDetailField(content.transform, "Name", 26, FontStyles.Bold, autoHeight: true, autoMinHeightBase: 48f, useContentSizeFitter: false, textColor: UiTheme.TextPrimary);
            var dates = AddDetailField(content.transform, "Dates", 16, FontStyles.Normal, height: 40, textColor: UiTheme.TextSecondary);
            var shareButton = ConfigureShareButton(root.transform, new Vector2(-16f, -16f));
            var section = root.AddComponent<IdentityDetailSection>();
            var so = new SerializedObject(section);
            so.FindProperty("nameText").objectReferenceValue = name;
            so.FindProperty("datesText").objectReferenceValue = dates;
            so.FindProperty("shareButton").objectReferenceValue = shareButton;
            so.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }

        public static void ConfigureIdentitySection(GameObject root)
        {
            if (root == null)
                return;

            EnsureIdentityContentStructure(root);
            var shareButton = ConfigureShareButton(root.transform, new Vector2(-16f, -16f));
            var section = root.GetComponent<IdentityDetailSection>();
            if (section == null)
                return;

            var so = new SerializedObject(section);
            so.FindProperty("shareButton").objectReferenceValue = shareButton;
            var name = root.transform.Find("Content/Name")?.GetComponent<TMP_Text>()
                ?? root.transform.Find("Name")?.GetComponent<TMP_Text>();
            var dates = root.transform.Find("Content/Dates")?.GetComponent<TMP_Text>()
                ?? root.transform.Find("Dates")?.GetComponent<TMP_Text>();
            so.FindProperty("nameText").objectReferenceValue = name;
            so.FindProperty("datesText").objectReferenceValue = dates;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static GameObject BuildExternalLinksSectionPrefab()
        {
            var root = CreateStretchSectionRoot("DetailSection_ExternalLinks");
            AddSectionVerticalLayout(root);

            var row = new GameObject("LinkRow", typeof(RectTransform));
            row.transform.SetParent(root.transform, false);
            var rowLe = row.AddComponent<LayoutElement>();
            rowLe.flexibleWidth = 1;
            rowLe.minHeight = UiLayoutMetrics.ScaleDetailSize(80f);
            rowLe.preferredHeight = UiLayoutMetrics.ScaleDetailSize(80f);

            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            var gap = UiLayoutMetrics.ScaleDetailPadding(UiLayoutMetrics.DetailSectionSpacing);
            hlg.spacing = gap;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;

            var wikipediaButton = BuildExternalLinkButton(row.transform, "WikipediaButton", "Wikipedia");
            var wikidataButton = BuildExternalLinkButton(row.transform, "WikidataButton", "Wikidata");

            var section = root.AddComponent<ExternalLinksDetailSection>();
            var so = new SerializedObject(section);
            so.FindProperty("wikipediaButton").objectReferenceValue = wikipediaButton;
            so.FindProperty("wikidataButton").objectReferenceValue = wikidataButton;
            so.FindProperty("wikipediaLabel").objectReferenceValue =
                wikipediaButton.transform.Find("Text")?.GetComponent<TMP_Text>();
            so.FindProperty("wikidataLabel").objectReferenceValue =
                wikidataButton.transform.Find("Text")?.GetComponent<TMP_Text>();
            so.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }

        static Button BuildExternalLinkButton(Transform parent, string name, string labelText)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.minHeight = UiLayoutMetrics.ScaleDetailSize(72f);
            le.preferredHeight = UiLayoutMetrics.ScaleDetailSize(72f);

            var label = CreateTmpChild(
                go.transform,
                "Text",
                15,
                FontStyles.Normal,
                UiButtonLayout.StandardLabelOffset);
            UiButtonLayout.ConfigureStandardLabel(label);
            label.GetComponent<TextMeshProUGUI>().text = labelText;
            label.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

            UiStyleBuilder.ApplySecondaryButton(go);
            UiButtonLayout.ConfigureStandardLabel(label);
            return go.GetComponent<Button>();
        }

        static GameObject BuildLabeledTextSectionPrefab(LabeledDetailSectionKind kind)
        {
            var root = CreateStretchSectionRoot($"DetailSection_{kind}");
            AddSectionVerticalLayout(root);
            var label = AddDetailField(root.transform, "Label", 15, FontStyles.Bold, height: 36, textColor: UiTheme.TextPrimary);

            var tagContainerGo = new GameObject("TagContainer", typeof(RectTransform));
            tagContainerGo.transform.SetParent(root.transform, false);
            var tagContainerLe = tagContainerGo.AddComponent<LayoutElement>();
            tagContainerLe.flexibleWidth = 1;
            tagContainerLe.flexibleHeight = 1;
            var tagVlg = tagContainerGo.AddComponent<VerticalLayoutGroup>();
            tagVlg.spacing = UiLayoutMetrics.ScaleDetailPadding(UiLayoutMetrics.DetailSectionSpacing);
            tagVlg.childAlignment = TextAnchor.UpperLeft;
            tagVlg.childForceExpandWidth = true;
            tagVlg.childForceExpandHeight = false;
            tagVlg.childControlWidth = true;
            tagVlg.childControlHeight = true;

            var tagButtonPrefab = EnsureDetailTagButtonPrefab();

            var section = root.AddComponent<LabeledTextDetailSection>();
            var so = new SerializedObject(section);
            so.FindProperty("sectionKind").enumValueIndex = (int)kind;
            so.FindProperty("labelText").objectReferenceValue = label;
            so.FindProperty("tagContainer").objectReferenceValue = tagContainerGo.transform;
            so.FindProperty("tagButtonPrefab").objectReferenceValue = tagButtonPrefab;
            so.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }

        static GameObject BuildScrollTextSectionPrefab(ScrollDetailSectionKind kind)
        {
            var root = CreateStretchSectionRoot($"DetailSection_{kind}");
            AddSectionVerticalLayout(root);
            var label = AddDetailField(root.transform, "Label", 15, FontStyles.Bold, height: 36, textColor: UiTheme.TextPrimary);

            var scrollRoot = new GameObject("Scroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollRoot.transform.SetParent(root.transform, false);
            var scrollLe = scrollRoot.AddComponent<LayoutElement>();
            scrollLe.flexibleHeight = 1;
            scrollLe.minHeight = UiLayoutMetrics.ScaleDetailSize(UiLayoutMetrics.DetailScrollMinHeight);
            StretchToParent(scrollRoot.GetComponent<RectTransform>());
            UiStyleBuilder.ApplyScrollBackground(scrollRoot.GetComponent<Image>());

            var viewport = CreatePanel(scrollRoot.transform, "Viewport", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            viewport.GetComponent<Image>().color = UiTheme.ViewportMask;

            var content = CreatePanel(viewport.transform, "Content", new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            var contentRt = content.GetComponent<RectTransform>();
            contentRt.pivot = new Vector2(0.5f, 1);
            contentRt.anchorMin = new Vector2(0, 1);
            contentRt.anchorMax = new Vector2(1, 1);
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            var scrollPad = UiLayoutMetrics.ScaleDetailPadding(UiLayoutMetrics.DetailScrollContentPadding);
            var scrollSpacing = UiLayoutMetrics.ScaleDetailPadding(UiLayoutMetrics.DetailScrollContentSpacing);
            vlg.padding = new RectOffset(scrollPad, scrollPad, scrollSpacing, scrollPad);
            vlg.spacing = scrollSpacing;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;
            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollRoot.GetComponent<ScrollRect>();
            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = contentRt;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            var body = AddDetailField(content.transform, "Body", 15, FontStyles.Normal, autoHeight: true, textColor: UiTheme.TextSecondary);

            var section = root.AddComponent<ScrollTextDetailSection>();
            var so = new SerializedObject(section);
            so.FindProperty("sectionKind").enumValueIndex = (int)kind;
            so.FindProperty("labelText").objectReferenceValue = label;
            so.FindProperty("bodyText").objectReferenceValue = body;
            so.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }

        static GameObject CreateListPanel(
            Transform parent,
            NavigationController nav,
            MathematicianRepository repo,
            MathematicianListItem prefab,
            LocalizationRefs loc)
        {
            var panel = CreatePanel(parent, "ListPanel", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            panel.SetActive(false);
            panel.AddComponent<FontSizeScope>();
            var scroll = CreateScrollView(panel.transform, "ListScroll");
            var empty = CreateTmpChild(panel.transform, "Empty", UiLayoutMetrics.EmptyStateBaseFontSize, FontStyles.Italic, UiLayoutMetrics.EmptyStatePosition);
            HomeListPanelLayout.ConfigureEmptyState(empty);
            empty.SetActive(false);
            var lse = empty.AddComponent<LocalizeStringEvent>();
            var soLse = new SerializedObject(lse);
            AssignLocalized(soLse.FindProperty("m_StringReference"), MakeLocalized(loc.UiCollection, "empty_list"));
            soLse.ApplyModifiedPropertiesWithoutUndo();

            var list = panel.AddComponent<ListPanel>();
            var prefabRef = AssetDatabase.LoadAssetAtPath<MathematicianListItem>(
                $"{PrefabFolder}/MathematicianListItem.prefab") ?? prefab;
            var so = new SerializedObject(list);
            so.FindProperty("navigation").objectReferenceValue = nav;
            so.FindProperty("repository").objectReferenceValue = repo;
            so.FindProperty("listContent").objectReferenceValue = scroll.content;
            so.FindProperty("itemPrefab").objectReferenceValue = prefabRef;
            so.FindProperty("emptyState").objectReferenceValue = empty;
            so.ApplyModifiedPropertiesWithoutUndo();
            return panel;
        }

        static GameObject CreateFavoritesPanel(
            Transform parent,
            NavigationController nav,
            MathematicianRepository repo,
            MathematicianListItem prefab,
            LocalizationRefs loc)
        {
            var panel = CreatePanel(parent, "FavoritesPanel", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            panel.SetActive(false);
            panel.AddComponent<FontSizeScope>();

            var slideBackdropGo = CreateFavoritesSlideBackdrop(panel.transform);
            var scroll = CreateScrollView(panel.transform, "ListScroll");
            var empty = CreateTmpChild(panel.transform, "Empty", UiLayoutMetrics.EmptyStateBaseFontSize, FontStyles.Italic, UiLayoutMetrics.EmptyStatePosition);
            HomeListPanelLayout.ConfigureEmptyState(empty);
            empty.SetActive(false);
            var lse = empty.AddComponent<LocalizeStringEvent>();
            var soLse = new SerializedObject(lse);
            AssignLocalized(soLse.FindProperty("m_StringReference"), MakeLocalized(loc.UiCollection, "empty_favorites"));
            soLse.ApplyModifiedPropertiesWithoutUndo();

            var favorites = panel.AddComponent<FavoritesPanel>();
            var prefabRef = AssetDatabase.LoadAssetAtPath<MathematicianListItem>(
                $"{PrefabFolder}/MathematicianListItem.prefab") ?? prefab;
            var so = new SerializedObject(favorites);
            so.FindProperty("navigation").objectReferenceValue = nav;
            so.FindProperty("repository").objectReferenceValue = repo;
            so.FindProperty("listContent").objectReferenceValue = scroll.content;
            so.FindProperty("itemPrefab").objectReferenceValue = prefabRef;
            so.FindProperty("emptyState").objectReferenceValue = empty;
            so.ApplyModifiedPropertiesWithoutUndo();

            ConfigureFavoritesPanelAnimation(panel);
            slideBackdropGo.transform.SetAsFirstSibling();
            return panel;
        }

        static GameObject CreateFavoritesSlideBackdrop(Transform parent)
        {
            var backdropGo = new GameObject("SlideBackdrop", typeof(RectTransform), typeof(Image));
            backdropGo.transform.SetParent(parent, false);
            ConfigureFavoritesSlideRoot(backdropGo.GetComponent<RectTransform>());
            var image = backdropGo.GetComponent<Image>();
            image.color = UiTheme.Background;
            image.raycastTarget = false;
            return backdropGo;
        }

        static void ConfigureFavoritesSlideRoot(RectTransform slideRoot)
        {
            if (slideRoot == null)
                return;

            slideRoot.anchorMin = Vector2.zero;
            slideRoot.anchorMax = Vector2.one;
            slideRoot.pivot = new Vector2(0.5f, 1f);
            slideRoot.offsetMin = Vector2.zero;
            slideRoot.offsetMax = Vector2.zero;
            slideRoot.anchoredPosition = Vector2.zero;
        }

        static void ConfigureFavoritesPanelAnimation(GameObject panel)
        {
            if (panel == null)
                return;

            var canvasGroup = panel.GetComponent<CanvasGroup>() ?? panel.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;

            var slideRoot = panel.transform.Find("SlideBackdrop") as RectTransform
                ?? panel.transform.Find("SlideRoot") as RectTransform;
            var transition = panel.GetComponent<UiPanelSlideTransition>() ?? panel.AddComponent<UiPanelSlideTransition>();
            var transitionSo = new SerializedObject(transition);
            transitionSo.FindProperty("slideRoot").objectReferenceValue = slideRoot;
            transitionSo.ApplyModifiedPropertiesWithoutUndo();
        }

        static void DetachFavoritesContentFromSlideRoot(Transform panel, Transform slideRoot)
        {
            var listScroll = slideRoot.Find("ListScroll");
            if (listScroll != null)
                listScroll.SetParent(panel, false);

            var empty = slideRoot.Find("Empty");
            if (empty != null)
                empty.SetParent(panel, false);
        }

        static void EnsureFavoritesPanelAnimation(GameObject panel)
        {
            if (panel == null)
                return;

            var panelTransform = panel.transform;
            var legacySlideRoot = panelTransform.Find("SlideRoot") as RectTransform;
            var slideBackdrop = panelTransform.Find("SlideBackdrop") as RectTransform;

            if (legacySlideRoot != null)
            {
                DetachFavoritesContentFromSlideRoot(panelTransform, legacySlideRoot);

                if (slideBackdrop == null)
                {
                    legacySlideRoot.name = "SlideBackdrop";
                    slideBackdrop = legacySlideRoot;
                    if (slideBackdrop.GetComponent<Image>() == null)
                    {
                        var image = slideBackdrop.gameObject.AddComponent<Image>();
                        image.color = UiTheme.Background;
                        image.raycastTarget = false;
                    }
                }
                else
                {
                    Object.DestroyImmediate(legacySlideRoot.gameObject);
                }
            }

            if (slideBackdrop == null)
                CreateFavoritesSlideBackdrop(panelTransform);
            else
                DetachFavoritesContentFromSlideRoot(panelTransform, slideBackdrop);

            slideBackdrop = panelTransform.Find("SlideBackdrop") as RectTransform;
            if (slideBackdrop != null)
            {
                ConfigureFavoritesSlideRoot(slideBackdrop);
                slideBackdrop.SetAsFirstSibling();

                var image = slideBackdrop.GetComponent<Image>();
                if (image == null)
                {
                    image = slideBackdrop.gameObject.AddComponent<Image>();
                    image.color = UiTheme.Background;
                }

                image.raycastTarget = false;
            }

            ConfigureFavoritesPanelAnimation(panel);
        }

        static GameObject CreateIndexPanel(
            Transform parent,
            NavigationController nav,
            MathematicianRepository repo,
            MathematicianListItem prefab,
            LocalizationRefs loc)
        {
            var panel = CreatePanel(parent, "IndexPanel", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            panel.SetActive(false);

            var letterScroll = CreateHorizontalLetterScroll(panel.transform, "LetterScroll");
            var listScroll = CreateScrollView(panel.transform, "ListScroll");
            listScroll.gameObject.AddComponent<FontSizeScope>();
            PinIndexLetterAndList(letterScroll, listScroll);
            letterScroll.transform.SetAsLastSibling();

            var empty = CreateTmpChild(panel.transform, "Empty", UiLayoutMetrics.EmptyStateBaseFontSize, FontStyles.Italic, UiLayoutMetrics.EmptyStatePosition);
            HomeListPanelLayout.ConfigureEmptyState(empty);
            empty.SetActive(false);
            var emptyLse = empty.AddComponent<LocalizeStringEvent>();
            var emptyLseSo = new SerializedObject(emptyLse);
            AssignLocalized(emptyLseSo.FindProperty("m_StringReference"), MakeLocalized(loc.UiCollection, "empty_index"));
            emptyLseSo.ApplyModifiedPropertiesWithoutUndo();

            var index = panel.AddComponent<IndexPanel>();
            var prefabRef = AssetDatabase.LoadAssetAtPath<MathematicianListItem>(
                $"{PrefabFolder}/MathematicianListItem.prefab") ?? prefab;
            var letterPrefab = CreateLetterButtonPrefab();
            var so = new SerializedObject(index);
            so.FindProperty("navigation").objectReferenceValue = nav;
            so.FindProperty("repository").objectReferenceValue = repo;
            so.FindProperty("letterContainer").objectReferenceValue = letterScroll.content;
            so.FindProperty("listContent").objectReferenceValue = listScroll.content;
            so.FindProperty("letterButtonPrefab").objectReferenceValue = letterPrefab;
            so.FindProperty("itemPrefab").objectReferenceValue = prefabRef;
            so.FindProperty("emptyState").objectReferenceValue = empty;
            so.ApplyModifiedPropertiesWithoutUndo();
            return panel;
        }

        static ScrollRect CreateHorizontalLetterScroll(Transform parent, string name)
        {
            var root = CreatePanel(parent, name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var viewport = CreatePanel(root.transform, "Viewport", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            viewport.GetComponent<Image>().color = UiTheme.ViewportMask;

            var content = CreatePanel(viewport.transform, "Content", new Vector2(0, 0), new Vector2(0, 1), Vector2.zero, Vector2.zero);
            var contentRt = content.GetComponent<RectTransform>();
            contentRt.pivot = new Vector2(0, 0.5f);
            contentRt.anchorMin = new Vector2(0, 0);
            contentRt.anchorMax = new Vector2(0, 1);
            var hlg = content.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(
                UiLayoutMetrics.LetterStripPaddingLeft,
                UiLayoutMetrics.LetterStripPaddingRight,
                0,
                0);
            hlg.spacing = UiLayoutMetrics.LetterStripSpacing;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            var scroll = root.AddComponent<ScrollRect>();
            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = contentRt;
            scroll.horizontal = true;
            scroll.vertical = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            return scroll;
        }

        public static void PinIndexLetterAndList(ScrollRect letterScroll, ScrollRect listScroll)
        {
            var topInset = UiLayoutMetrics.LetterStripMarginTop
                + UiLayoutMetrics.LetterStripHeight
                + UiLayoutMetrics.LetterStripMarginBottom;

            var letterRt = letterScroll.GetComponent<RectTransform>();
            letterRt.anchorMin = new Vector2(0, 1);
            letterRt.anchorMax = new Vector2(1, 1);
            letterRt.pivot = new Vector2(0.5f, 1);
            letterRt.anchoredPosition = new Vector2(0, -UiLayoutMetrics.LetterStripMarginTop);
            letterRt.sizeDelta = new Vector2(0, UiLayoutMetrics.LetterStripHeight);

            var listRt = listScroll.GetComponent<RectTransform>();
            listRt.anchorMin = Vector2.zero;
            listRt.anchorMax = Vector2.one;
            listRt.pivot = new Vector2(0.5f, 1f);
            listRt.offsetMin = Vector2.zero;
            listRt.offsetMax = new Vector2(0f, -topInset);
        }

        public static void ConfigureLetterButton(GameObject go)
        {
            var width = UiLayoutMetrics.LetterButtonWidth;
            var height = UiLayoutMetrics.LetterButtonHeight;
            var rt = go.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0f, 0.5f);
                rt.anchorMax = new Vector2(0f, 0.5f);
                rt.pivot = new Vector2(0f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(width, height);
            }

            var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            le.preferredHeight = height;
            le.minWidth = width;
            le.minHeight = height;

            var label = go.transform.Find("Label")?.GetComponent<TextMeshProUGUI>();
            if (label != null)
            {
                label.fontSize = UiLayoutMetrics.LetterButtonFontSize;
                label.alignment = TextAlignmentOptions.Center;
                var labelRt = label.GetComponent<RectTransform>();
                if (labelRt != null)
                {
                    labelRt.anchorMin = Vector2.zero;
                    labelRt.anchorMax = Vector2.one;
                    labelRt.offsetMin = Vector2.zero;
                    labelRt.offsetMax = Vector2.zero;
                }
            }

            UiStyleBuilder.ApplySecondaryButton(go);
        }

        static Button CreateLetterButtonPrefab()
        {
            var path = $"{PrefabFolder}/LetterButton.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<Button>(path);
            if (existing != null)
            {
                EditPrefabContents(path, ConfigureLetterButton);
                return existing;
            }

            var go = BuildLetterButtonRoot();
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            EnsureThemedCardOnPrefab(path, UiCardVariant.Filter);
            return prefab.GetComponent<Button>();
        }

        static GameObject BuildLetterButtonRoot()
        {
            var go = new GameObject("LetterButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.GetComponent<Image>().color = UiTheme.CardFill;
            var label = CreateTmpChild(go.transform, "Label", UiLayoutMetrics.LetterButtonBaseFontSize, FontStyles.Bold, Vector2.zero);
            label.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
            ConfigureLetterButton(go);
            return go;
        }

        static PortraitGalleryView CreatePortraitGallery(Transform contentParent, bool fillParent = false)
        {
            var block = new GameObject("PortraitGallery", typeof(RectTransform));
            block.transform.SetParent(contentParent, false);
            var blockRt = block.GetComponent<RectTransform>();
            if (fillParent)
            {
                StretchToParent(blockRt);
            }
            else
            {
                var le = block.AddComponent<LayoutElement>();
                le.preferredHeight = 340;
                le.flexibleWidth = 1;
            }

            var scrollGo = new GameObject("GalleryScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollGo.transform.SetParent(block.transform, false);
            var scrollRt = scrollGo.GetComponent<RectTransform>();
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = new Vector2(0, UiLayoutMetrics.ScaleDetailSize(UiLayoutMetrics.DetailGalleryBottomInset));
            scrollRt.offsetMax = new Vector2(0, -UiLayoutMetrics.ScaleDetailSize(UiLayoutMetrics.DetailGalleryTopInset));
            UiStyleBuilder.ApplyScrollBackground(scrollGo.GetComponent<Image>());

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(scrollGo.transform, false);
            var vpRt = viewport.GetComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = Vector2.zero;
            vpRt.offsetMax = Vector2.zero;

            var pages = new GameObject("Pages", typeof(RectTransform));
            pages.transform.SetParent(viewport.transform, false);
            var pagesRt = pages.GetComponent<RectTransform>();
            pagesRt.anchorMin = new Vector2(0, 0);
            pagesRt.anchorMax = new Vector2(0, 1);
            pagesRt.pivot = new Vector2(0, 0.5f);
            pagesRt.sizeDelta = new Vector2(400, 0);

            var pageTpl = new GameObject("PageTemplate", typeof(RectTransform), typeof(Image));
            pageTpl.transform.SetParent(pages.transform, false);
            pageTpl.SetActive(false);

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.viewport = vpRt;
            scroll.content = pagesRt;
            scroll.horizontal = true;
            scroll.vertical = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.inertia = false;
            scrollGo.AddComponent<GalleryScrollSnap>();

            var dots = new GameObject("Dots", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            dots.transform.SetParent(block.transform, false);
            var dotsRt = dots.GetComponent<RectTransform>();
            dotsRt.anchorMin = new Vector2(0, 0);
            dotsRt.anchorMax = new Vector2(1, 0);
            dotsRt.pivot = new Vector2(0.5f, 0);
            dotsRt.sizeDelta = new Vector2(0, UiLayoutMetrics.ScaleDetailSize(UiLayoutMetrics.DetailGalleryDotsHeight));
            dotsRt.anchoredPosition = new Vector2(0, UiLayoutMetrics.ScaleDetailPadding(8));
            var hlg = dots.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = UiLayoutMetrics.ScaleDetailPadding(8);
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            var dotTpl = new GameObject("DotTemplate", typeof(RectTransform), typeof(Image));
            dotTpl.transform.SetParent(dots.transform, false);
            dotTpl.GetComponent<RectTransform>().sizeDelta = new Vector2(
                UiLayoutMetrics.ScaleDetailSize(UiLayoutMetrics.DetailGalleryDotSize),
                UiLayoutMetrics.ScaleDetailSize(UiLayoutMetrics.DetailGalleryDotSize));
            dotTpl.GetComponent<Image>().color = UiTheme.GalleryDotInactive;
            dotTpl.SetActive(false);

            var caption = CreateTmpChild(block.transform, "Caption", UiLayoutMetrics.DetailCaptionBaseFontSize, FontStyles.Italic, new Vector2(8, 4));
            var capTmp = caption.GetComponent<TextMeshProUGUI>();
            var detailCaptionSize = UiLayoutMetrics.ScaleDetailFont(UiLayoutMetrics.DetailCaptionBaseFontSize);
            capTmp.fontSize = detailCaptionSize;
            var capFontSo = new SerializedObject(capTmp);
            var capBase = capFontSo.FindProperty("m_fontSizeBase");
            if (capBase != null)
                capBase.floatValue = detailCaptionSize;
            capFontSo.ApplyModifiedPropertiesWithoutUndo();
            var capRt = caption.GetComponent<RectTransform>();
            capRt.anchorMin = new Vector2(0, 0);
            capRt.anchorMax = new Vector2(1, 0);
            capRt.pivot = new Vector2(0, 0);
            capRt.sizeDelta = new Vector2(-16, UiLayoutMetrics.ScaleDetailSize(36f));
            caption.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.TopLeft;
            caption.GetComponent<TextMeshProUGUI>().color = UiTheme.TextSecondary;

            var gallery = block.AddComponent<PortraitGalleryView>();
            var gso = new SerializedObject(gallery);
            gso.FindProperty("scrollRect").objectReferenceValue = scroll;
            gso.FindProperty("pageContainer").objectReferenceValue = pagesRt;
            gso.FindProperty("pageTemplate").objectReferenceValue = pageTpl.GetComponent<Image>();
            gso.FindProperty("captionText").objectReferenceValue = caption.GetComponent<TMP_Text>();
            gso.FindProperty("dotsRoot").objectReferenceValue = dots.transform;
            gso.FindProperty("dotTemplate").objectReferenceValue = dotTpl.GetComponent<Image>();
            gso.FindProperty("snap").objectReferenceValue = scrollGo.GetComponent<GalleryScrollSnap>();
            gso.ApplyModifiedPropertiesWithoutUndo();
            return gallery;
        }

        static TMP_Text AddDetailField(
            Transform parent,
            string name,
            float size,
            FontStyles style,
            float height = 36,
            bool autoHeight = false,
            float autoMinHeightBase = 80f,
            bool useContentSizeFitter = true,
            Color? textColor = null)
        {
            var go = CreateTmpChild(parent, name, size, style, Vector2.zero);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.color = textColor ?? UiTheme.TextPrimary;
            var fontSize = UiLayoutMetrics.ScaleDetailFont(size);
            tmp.fontSize = fontSize;
            var fontSo = new SerializedObject(tmp);
            var baseProp = fontSo.FindProperty("m_fontSizeBase");
            if (baseProp != null)
                baseProp.floatValue = fontSize;
            fontSo.ApplyModifiedPropertiesWithoutUndo();

            var le = go.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;

            if (autoHeight)
            {
                if (useContentSizeFitter)
                {
                    var fitter = go.AddComponent<ContentSizeFitter>();
                    fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                    fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                }

                var scaledMin = UiLayoutMetrics.ScaleDetailSize(autoMinHeightBase);
                le.preferredHeight = -1;
                le.minHeight = scaledMin;

                var layoutHeight = go.AddComponent<TmpLayoutHeight>();
                var lhSo = new SerializedObject(layoutHeight);
                lhSo.FindProperty("minHeight").floatValue = scaledMin;
                lhSo.FindProperty("padding").floatValue =
                    UiLayoutMetrics.DetailFieldPadding * UiLayoutMetrics.DetailContentScale;
                lhSo.ApplyModifiedPropertiesWithoutUndo();

                var rt = go.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(0, scaledMin);
                return tmp;
            }

            var scaledHeight = UiLayoutMetrics.ScaleDetailSize(height);
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, scaledHeight);
            le.preferredHeight = scaledHeight;
            return tmp;
        }

        static GameObject CreateSettingsPanel(Transform parent, LocalizationRefs loc)
        {
            var panel = CreatePanel(parent, "SettingsPanel", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            panel.SetActive(false);

            var langLabel = CreateTmpChild(panel.transform, "LangLabel", 18, FontStyles.Bold, new Vector2(40, -80));
            langLabel.GetComponent<TextMeshProUGUI>().color = UiTheme.TextPrimary;
            var lse = langLabel.AddComponent<LocalizeStringEvent>();
            var collection = loc.UiCollection;
            var soLse = new SerializedObject(lse);
            AssignLocalized(soLse.FindProperty("m_StringReference"), MakeLocalized(collection, "settings_language"));
            soLse.ApplyModifiedPropertiesWithoutUndo();

            var ruBtn = CreateSceneButton(panel.transform, UiButtonLayout.SettingsRussian, collection);
            var enBtn = CreateSceneButton(panel.transform, UiButtonLayout.SettingsEnglish, collection);
            var status = CreateTmpChild(panel.transform, "Status", 16, FontStyles.Italic, new Vector2(40, -320));
            status.GetComponent<TextMeshProUGUI>().color = UiTheme.TextSecondary;
            status.GetComponent<TextMeshProUGUI>().raycastTarget = false;

            var fontLabel = CreateTmpChild(panel.transform, "FontSizeLabel", 18, FontStyles.Bold, new Vector2(40, -400));
            fontLabel.GetComponent<TextMeshProUGUI>().color = UiTheme.TextPrimary;
            var fontLse = fontLabel.AddComponent<LocalizeStringEvent>();
            var fontLseSo = new SerializedObject(fontLse);
            AssignLocalized(fontLseSo.FindProperty("m_StringReference"), MakeLocalized(collection, "settings_font_size"));
            fontLseSo.ApplyModifiedPropertiesWithoutUndo();

            var fontNormalBtn = CreateSceneButton(panel.transform, UiButtonLayout.SettingsFontNormal, collection);
            var fontLargeBtn = CreateSceneButton(panel.transform, UiButtonLayout.SettingsFontLarge, collection);
            var fontExtraLargeBtn = CreateSceneButton(panel.transform, UiButtonLayout.SettingsFontExtraLarge, collection);
            var fontStatus = CreateTmpChild(panel.transform, "FontStatus", 16, FontStyles.Italic, new Vector2(40, -720));
            fontStatus.GetComponent<TextMeshProUGUI>().color = UiTheme.TextSecondary;
            fontStatus.GetComponent<TextMeshProUGUI>().raycastTarget = false;
            AddThemeBinding(fontStatus, UiThemeToken.TextSecondary);

            var themeLabel = CreateTmpChild(panel.transform, "ThemeLabel", 18, FontStyles.Bold, new Vector2(40, -800));
            themeLabel.GetComponent<TextMeshProUGUI>().color = UiTheme.TextPrimary;
            AddThemeBinding(themeLabel, UiThemeToken.TextPrimary);
            var themeLse = themeLabel.AddComponent<LocalizeStringEvent>();
            var themeLseSo = new SerializedObject(themeLse);
            AssignLocalized(themeLseSo.FindProperty("m_StringReference"), MakeLocalized(collection, "settings_theme"));
            themeLseSo.ApplyModifiedPropertiesWithoutUndo();

            var darkBtn = CreateSceneButton(panel.transform, UiButtonLayout.SettingsThemeDark, collection);
            var lightBtn = CreateSceneButton(panel.transform, UiButtonLayout.SettingsThemeLight, collection);
            var glassBtn = CreateSceneButton(panel.transform, UiButtonLayout.SettingsThemeGlass, collection);
            var themeStatus = CreateTmpChild(panel.transform, "ThemeStatus", 16, FontStyles.Italic, new Vector2(40, -1120));
            themeStatus.GetComponent<TextMeshProUGUI>().color = UiTheme.TextSecondary;
            themeStatus.GetComponent<TextMeshProUGUI>().raycastTarget = false;
            AddThemeBinding(themeStatus, UiThemeToken.TextSecondary);

            var settings = panel.AddComponent<SettingsPanel>();
            var so = new SerializedObject(settings);
            so.FindProperty("russianButton").objectReferenceValue = ruBtn.GetComponent<Button>();
            so.FindProperty("englishButton").objectReferenceValue = enBtn.GetComponent<Button>();
            so.FindProperty("statusText").objectReferenceValue = status.GetComponent<TMP_Text>();
            so.FindProperty("fontNormalButton").objectReferenceValue = fontNormalBtn.GetComponent<Button>();
            so.FindProperty("fontLargeButton").objectReferenceValue = fontLargeBtn.GetComponent<Button>();
            so.FindProperty("fontExtraLargeButton").objectReferenceValue = fontExtraLargeBtn.GetComponent<Button>();
            so.FindProperty("fontStatusText").objectReferenceValue = fontStatus.GetComponent<TMP_Text>();
            so.FindProperty("darkThemeButton").objectReferenceValue = darkBtn.GetComponent<Button>();
            so.FindProperty("lightThemeButton").objectReferenceValue = lightBtn.GetComponent<Button>();
            so.FindProperty("glassThemeButton").objectReferenceValue = glassBtn.GetComponent<Button>();
            so.FindProperty("themeStatusText").objectReferenceValue = themeStatus.GetComponent<TMP_Text>();
            so.ApplyModifiedPropertiesWithoutUndo();

            WireButtonClick(ruBtn.GetComponent<Button>(), settings.SelectRussian);
            WireButtonClick(enBtn.GetComponent<Button>(), settings.SelectEnglish);
            WireButtonClick(fontNormalBtn.GetComponent<Button>(), settings.SelectFontNormal);
            WireButtonClick(fontLargeBtn.GetComponent<Button>(), settings.SelectFontLarge);
            WireButtonClick(fontExtraLargeBtn.GetComponent<Button>(), settings.SelectFontExtraLarge);
            WireButtonClick(darkBtn.GetComponent<Button>(), settings.SelectDark);
            WireButtonClick(lightBtn.GetComponent<Button>(), settings.SelectLight);
            WireButtonClick(glassBtn.GetComponent<Button>(), settings.SelectGlass);
            return panel;
        }

        struct BottomBarResult
        {
            public Button browseTab;
            public Button indexTab;
            public Button settingsTab;
            public Button favoritesButton;
        }

        static BottomBarResult CreateBottomBar(Transform canvas, NavigationController nav, LocalizationRefs loc)
        {
            var bar = CreatePanel(canvas, "BottomBar", new Vector2(0, 0), new Vector2(1, 0), Vector2.zero, Vector2.zero);
            UiButtonLayout.ApplyBottomStretchBarRect(
                bar.GetComponent<RectTransform>(),
                UiButtonLayout.BottomBarPosition,
                UiButtonLayout.BottomBarSize);
            UiStyleBuilder.ApplyNavBarStyle(bar);
            var browse = CreateSceneButton(bar.transform, UiButtonLayout.BottomBrowse, loc.UiCollection);
            var index = CreateSceneButton(bar.transform, UiButtonLayout.BottomIndex, loc.UiCollection);
            var settings = CreateSceneButton(bar.transform, UiButtonLayout.BottomSettings, loc.UiCollection);
            var favorites = CreateSceneButton(bar.transform, UiButtonLayout.BottomFavorites, loc.UiCollection);
            WireButtonClick(browse.GetComponent<Button>(), nav.OnBrowseTabClicked);
            WireButtonClick(index.GetComponent<Button>(), nav.OnIndexTabClicked);
            WireButtonClick(settings.GetComponent<Button>(), nav.OnSettingsTabClicked);
            WireButtonClick(favorites.GetComponent<Button>(), nav.OnFavoritesButtonClicked);
            return new BottomBarResult
            {
                browseTab = browse.GetComponent<Button>(),
                indexTab = index.GetComponent<Button>(),
                settingsTab = settings.GetComponent<Button>(),
                favoritesButton = favorites.GetComponent<Button>()
            };
        }

        static GameObject CreateSceneButton(
            Transform parent,
            UiButtonLayout.SceneButton spec,
            StringTableCollection collection)
        {
            var go = new GameObject(spec.Name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            UiButtonLayout.ApplyTopLeftAnchoredRect(go.GetComponent<RectTransform>(), spec.Position, spec.Size);
            go.GetComponent<Image>().color = UiTheme.CardFill;

            var label = CreateTmpChild(go.transform, "Text", UiButtonLayout.StandardLabelFontBase, FontStyles.Normal, UiButtonLayout.StandardLabelOffset);
            UiButtonLayout.ConfigureStandardLabel(label);
            var lse = label.AddComponent<LocalizeStringEvent>();
            var so = new SerializedObject(lse);
            AssignLocalized(so.FindProperty("m_StringReference"), MakeLocalized(collection, spec.LocalizationKey));
            so.ApplyModifiedPropertiesWithoutUndo();
            WireLocalizeStringToTmp(label);

            if (spec.Style == UiButtonStyle.Primary)
                UiStyleBuilder.ApplyPrimaryButton(go);
            else
                UiStyleBuilder.ApplySecondaryButton(go);

            UiButtonLayout.ConfigureStandardLabel(label);
            return go;
        }

        static GameObject CreatePanel(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            go.GetComponent<Image>().color = UiTheme.Background;
            return go;
        }

        static void WireNavigation(
            NavigationController nav,
            GameObject home,
            GameObject index,
            GameObject list,
            GameObject favorites,
            GameObject detail,
            GameObject settings,
            GameObject backButton,
            HeaderTitleBinder header,
            BottomBarResult bottomBar)
        {
            var so = new SerializedObject(nav);
            so.FindProperty("homePanel").objectReferenceValue = home.GetComponent<HomePanel>();
            so.FindProperty("indexPanel").objectReferenceValue = index.GetComponent<IndexPanel>();
            so.FindProperty("listPanel").objectReferenceValue = list.GetComponent<ListPanel>();
            so.FindProperty("favoritesPanel").objectReferenceValue = favorites.GetComponent<FavoritesPanel>();
            so.FindProperty("favoritesTransition").objectReferenceValue = favorites.GetComponent<UiPanelSlideTransition>();
            so.FindProperty("detailPanel").objectReferenceValue = detail.GetComponent<DetailPanel>();
            so.FindProperty("settingsPanel").objectReferenceValue = settings.GetComponent<SettingsPanel>();
            so.FindProperty("headerBackButton").objectReferenceValue = backButton;
            so.FindProperty("headerTitle").objectReferenceValue = header;
            so.FindProperty("browseTab").objectReferenceValue = bottomBar.browseTab;
            so.FindProperty("indexTab").objectReferenceValue = bottomBar.indexTab;
            so.FindProperty("settingsTab").objectReferenceValue = bottomBar.settingsTab;
            so.FindProperty("favoritesButton").objectReferenceValue = bottomBar.favoritesButton;
            so.ApplyModifiedPropertiesWithoutUndo();
            WireButtonClick(backButton.GetComponent<Button>(), nav.OnBackButtonClicked);
        }

        static void WireButtonClick(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null || action == null)
                return;

            button.onClick.RemoveAllListeners();
            UnityEventTools.AddPersistentListener(button.onClick, action);
        }

        static void WireBootstrap(AppBootstrap bootstrap, NavigationController nav)
        {
            var so = new SerializedObject(bootstrap);
            so.FindProperty("navigation").objectReferenceValue = nav;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void WireBackHandler(BackButtonHandler handler, NavigationController nav)
        {
            var so = new SerializedObject(handler);
            so.FindProperty("navigation").objectReferenceValue = nav;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void WireThemeScope(
            GameObject canvasGo,
            Camera cam,
            NavigationController nav,
            GameObject settings,
            GameObject detail)
        {
            var scope = canvasGo.GetComponent<UiThemeScope>() ?? canvasGo.AddComponent<UiThemeScope>();
            var glassController = canvasGo.GetComponent<GlassThemeController>() ?? canvasGo.AddComponent<GlassThemeController>();
            var gallery = detail.GetComponentInChildren<PortraitGalleryView>(true);
            var so = new SerializedObject(scope);
            so.FindProperty("targetCamera").objectReferenceValue = cam;
            so.FindProperty("navigation").objectReferenceValue = nav;
            so.FindProperty("settingsPanel").objectReferenceValue = settings.GetComponent<SettingsPanel>();
            so.FindProperty("portraitGallery").objectReferenceValue = gallery;
            so.FindProperty("glassController").objectReferenceValue = glassController;
            so.ApplyModifiedPropertiesWithoutUndo();

            var glassSo = new SerializedObject(glassController);
            var backdrop = canvasGo.transform.Find("GlassBackdrop")?.GetComponent<GlassBackdropView>();
            glassSo.FindProperty("backdropView").objectReferenceValue = backdrop;
            glassSo.FindProperty("rootCanvas").objectReferenceValue = canvasGo.GetComponent<Canvas>();
            glassSo.ApplyModifiedPropertiesWithoutUndo();

            TagThemeBindings(canvasGo.transform);
        }

        static GameObject CreateGlassBackdrop(Transform canvas)
        {
            var existing = canvas.Find("GlassBackdrop");
            if (existing != null)
                return existing.gameObject;

            var go = new GameObject("GlassBackdrop", typeof(RectTransform), typeof(GlassBackdropView));
            go.transform.SetParent(canvas, false);
            go.transform.SetAsFirstSibling();
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            go.SetActive(false);
            return go;
        }

        static void EnsureThemedCard(GameObject go, UiCardVariant variant)
        {
            if (go.GetComponent<UiThemedCard>() == null)
                go.AddComponent<UiThemedCard>();
        }

        static void AddThemeBinding(GameObject go, UiThemeToken token)
        {
            var binding = go.GetComponent<UiThemeBinding>() ?? go.AddComponent<UiThemeBinding>();
            var so = new SerializedObject(binding);
            so.FindProperty("token").enumValueIndex = (int)token;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void TagThemeBindings(Transform canvas)
        {
            var contentArea = canvas.Find("ContentArea");
            if (contentArea != null)
                AddThemeBinding(contentArea.gameObject, UiThemeToken.Background);

            TagNavBar(canvas.Find("Header"));
            TagNavBar(canvas.Find("BottomBar"));

            foreach (var scroll in canvas.GetComponentsInChildren<ScrollRect>(true))
            {
                if (scroll.viewport != null)
                    AddThemeBinding(scroll.viewport.gameObject, UiThemeToken.ViewportMask);

                var scrollImage = scroll.GetComponent<Image>();
                if (scrollImage != null)
                    AddThemeBinding(scroll.gameObject, UiThemeToken.ScrollBackground);

                if (scroll.content != null)
                {
                    var contentImage = scroll.content.GetComponent<Image>();
                    if (contentImage != null)
                        AddThemeBinding(scroll.content.gameObject, UiThemeToken.Background);
                }
            }

            foreach (var text in canvas.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.GetComponent<UiThemeBinding>() != null)
                    continue;

                var name = text.gameObject.name;
                if (name is "Status" or "FontStatus" or "ThemeStatus" or "Caption" or "Empty")
                    AddThemeBinding(text.gameObject, UiThemeToken.TextSecondary);
                else if (name is "LangLabel" or "FontSizeLabel" or "ThemeLabel"
                         || name.StartsWith("section_")
                         || name is "Name" or "Label" or "HomeTitle" or "IndexTitle" or "SettingsTitle" or "FavoritesTitle" or "PlainTitle")
                    AddThemeBinding(text.gameObject, UiThemeToken.TextPrimary);
                else if (name is "Dates" or "Bio" or "Body")
                    AddThemeBinding(text.gameObject, UiThemeToken.TextSecondary);
            }

            foreach (var node in canvas.GetComponentsInChildren<Transform>(true))
            {
                if (node.name == "DecorGlow" && node.GetComponent<UiThemeBinding>() == null)
                    AddThemeBinding(node.gameObject, UiThemeToken.Glow);
            }

            foreach (var panel in canvas.GetComponentsInChildren<Transform>(true))
            {
                if (!panel.name.EndsWith("Panel"))
                    continue;

                if (panel.GetComponent<Image>() == null || panel.GetComponent<UiThemeBinding>() != null)
                    continue;

                AddThemeBinding(panel.gameObject, UiThemeToken.Background);
            }

            foreach (var gallery in canvas.GetComponentsInChildren<PortraitGalleryView>(true))
            {
                if (gallery.transform.Find("Dots") is { } dotsRoot)
                {
                    var dotTemplate = dotsRoot.Find("DotTemplate");
                    if (dotTemplate != null)
                        AddThemeBinding(dotTemplate.gameObject, UiThemeToken.GalleryDotInactive);
                }
            }
        }

        static void TagNavBar(Transform bar)
        {
            if (bar == null)
                return;

            AddThemeBinding(bar.gameObject, UiThemeToken.NavBar);
            EnsureGlassSurface(bar.gameObject, UiThemeToken.NavBar);
            var topGlow = bar.Find("TopGlow");
            if (topGlow != null)
                AddThemeBinding(topGlow.gameObject, UiThemeToken.NavBarAccent);
        }

        static void EnsureGlassSurface(GameObject go, UiThemeToken tintToken)
        {
            if (go.GetComponent<Image>() == null)
                return;

            var surface = go.GetComponent<UiGlassSurface>() ?? go.AddComponent<UiGlassSurface>();
            var so = new SerializedObject(surface);
            so.FindProperty("targetImage").objectReferenceValue = go.GetComponent<Image>();
            so.FindProperty("useFrostedMaterial").boolValue = true;
            so.FindProperty("tintToken").enumValueIndex = (int)tintToken;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void EnsureGlassSurfaceOnCardFill(GameObject root)
        {
            var fill = root.transform.Find("Fill");
            if (fill == null)
                return;

            EnsureGlassSurface(fill.gameObject, UiThemeToken.CardFill);
        }

        static void PatchThemeInOpenScene(StringTableCollection collection)
        {
            PatchGlassThemeInOpenScene(collection, includeLegacyThemeButtons: true);
        }

        static void PatchGlassThemeInOpenScene(StringTableCollection collection, bool includeLegacyThemeButtons = false)
        {
            var settings = Object.FindFirstObjectByType<SettingsPanel>(FindObjectsInactive.Include);
            if (settings != null)
            {
                var panel = settings.gameObject;
                if (includeLegacyThemeButtons && panel.transform.Find("DarkThemeButton") == null)
                {
                    var themeLabel = CreateTmpChild(panel.transform, "ThemeLabel", 18, FontStyles.Bold, new Vector2(40, -800));
                    themeLabel.GetComponent<TextMeshProUGUI>().color = UiTheme.TextPrimary;
                    AddThemeBinding(themeLabel, UiThemeToken.TextPrimary);
                    var themeLse = themeLabel.AddComponent<LocalizeStringEvent>();
                    var themeLseSo = new SerializedObject(themeLse);
                    AssignLocalized(themeLseSo.FindProperty("m_StringReference"), MakeLocalized(collection, "settings_theme"));
                    themeLseSo.ApplyModifiedPropertiesWithoutUndo();

                    var darkBtn = CreateSceneButton(panel.transform, UiButtonLayout.SettingsThemeDark, collection);
                    var lightBtn = CreateSceneButton(panel.transform, UiButtonLayout.SettingsThemeLight, collection);
                    var themeStatus = CreateTmpChild(panel.transform, "ThemeStatus", 16, FontStyles.Italic, new Vector2(40, -1120));
                    themeStatus.GetComponent<TextMeshProUGUI>().color = UiTheme.TextSecondary;
                    themeStatus.GetComponent<TextMeshProUGUI>().raycastTarget = false;
                    AddThemeBinding(themeStatus, UiThemeToken.TextSecondary);

                    var settingsSo = new SerializedObject(settings);
                    settingsSo.FindProperty("darkThemeButton").objectReferenceValue = darkBtn.GetComponent<Button>();
                    settingsSo.FindProperty("lightThemeButton").objectReferenceValue = lightBtn.GetComponent<Button>();
                    settingsSo.FindProperty("themeStatusText").objectReferenceValue = themeStatus.GetComponent<TMP_Text>();
                    settingsSo.ApplyModifiedPropertiesWithoutUndo();

                    WireButtonClick(darkBtn.GetComponent<Button>(), settings.SelectDark);
                    WireButtonClick(lightBtn.GetComponent<Button>(), settings.SelectLight);
                }

                if (panel.transform.Find("GlassThemeButton") == null)
                {
                    var glassBtn = CreateSceneButton(panel.transform, UiButtonLayout.SettingsThemeGlass, collection);
                    var themeStatus = panel.transform.Find("ThemeStatus")?.GetComponent<TMP_Text>();
                    if (themeStatus != null)
                    {
                        var statusRt = themeStatus.GetComponent<RectTransform>();
                        statusRt.anchoredPosition = new Vector2(40f, -1120f);
                    }

                    var settingsSo = new SerializedObject(settings);
                    settingsSo.FindProperty("glassThemeButton").objectReferenceValue = glassBtn.GetComponent<Button>();
                    if (themeStatus != null)
                        settingsSo.FindProperty("themeStatusText").objectReferenceValue = themeStatus;
                    settingsSo.ApplyModifiedPropertiesWithoutUndo();

                    WireButtonClick(glassBtn.GetComponent<Button>(), settings.SelectGlass);
                }
            }

            var canvas = Object.FindAnyObjectByType<Canvas>();
            var navigation = Object.FindAnyObjectByType<NavigationController>();
            var gallery = Object.FindFirstObjectByType<PortraitGalleryView>(FindObjectsInactive.Include);
            if (canvas != null)
            {
                CreateGlassBackdrop(canvas.transform);

                var glassController = canvas.GetComponent<GlassThemeController>() ?? canvas.gameObject.AddComponent<GlassThemeController>();
                var glassSo = new SerializedObject(glassController);
                glassSo.FindProperty("backdropView").objectReferenceValue = canvas.transform.Find("GlassBackdrop")?.GetComponent<GlassBackdropView>();
                glassSo.FindProperty("rootCanvas").objectReferenceValue = canvas;
                glassSo.ApplyModifiedPropertiesWithoutUndo();

                var scope = canvas.GetComponent<UiThemeScope>() ?? canvas.gameObject.AddComponent<UiThemeScope>();
                var scopeSo = new SerializedObject(scope);
                scopeSo.FindProperty("targetCamera").objectReferenceValue = Camera.main;
                scopeSo.FindProperty("navigation").objectReferenceValue = navigation;
                scopeSo.FindProperty("settingsPanel").objectReferenceValue = settings;
                scopeSo.FindProperty("portraitGallery").objectReferenceValue = gallery;
                scopeSo.FindProperty("glassController").objectReferenceValue = glassController;
                scopeSo.ApplyModifiedPropertiesWithoutUndo();
                TagThemeBindings(canvas.transform);
            }

            EnsureThemedCardOnPrefab($"{PrefabFolder}/FilterButton.prefab", UiCardVariant.Filter);
            EnsureThemedCardOnPrefab($"{PrefabFolder}/DetailTagButton.prefab", UiCardVariant.Filter);
            EnsureThemedCardOnPrefab($"{PrefabFolder}/SearchBar.prefab", UiCardVariant.Filter);
            EnsureThemedCardOnPrefab($"{PrefabFolder}/MathematicianListItem.prefab", UiCardVariant.ListItem);
            EnsureThemedCardOnPrefab("Assets/Resources/MathematicianListItem.prefab", UiCardVariant.ListItem);
        }

        [MenuItem("PeopleOfMath/Patch Favorites Support")]
        public static void PatchFavoritesSupport()
        {
            if (DeferUntilEditMode(PatchFavoritesSupport))
                return;

            if (!File.Exists(ScenePath))
            {
                Debug.LogError($"Scene not found: {ScenePath}");
                return;
            }

            UiSpriteFactory.EnsureSprites();
            var loc = SetupLocalization();
            AssetDatabase.SaveAssets();

            PatchShareListItemPrefab($"{PrefabFolder}/MathematicianListItem.prefab");
            PatchShareListItemPrefab("Assets/Resources/MathematicianListItem.prefab");

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            PatchFavoritesInOpenScene(loc);
            PatchShareButtonsInScene();
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Favorites support patched in Main scene.");
        }

        [MenuItem("PeopleOfMath/Patch Favorites Panel Animation")]
        public static void PatchFavoritesPanelAnimation()
        {
            if (DeferUntilEditMode(PatchFavoritesPanelAnimation))
                return;

            if (!File.Exists(ScenePath))
            {
                Debug.LogError($"Scene not found: {ScenePath}");
                return;
            }

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            PatchFavoritesPanelAnimationInOpenScene();
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Favorites panel animation patched in Main scene.");
        }

        static void PatchFavoritesPanelAnimationInOpenScene()
        {
            var navigation = Object.FindFirstObjectByType<NavigationController>();
            var favoritesPanelGo = GameObject.Find("ContentArea")?.transform.Find("FavoritesPanel")?.gameObject;
            if (favoritesPanelGo == null)
            {
                Debug.LogError("FavoritesPanel not found in Main scene.");
                return;
            }

            EnsureFavoritesPanelAnimation(favoritesPanelGo);

            if (navigation == null)
                return;

            var navSo = new SerializedObject(navigation);
            navSo.FindProperty("favoritesTransition").objectReferenceValue =
                favoritesPanelGo.GetComponent<UiPanelSlideTransition>();
            navSo.ApplyModifiedPropertiesWithoutUndo();
        }

        [MenuItem("PeopleOfMath/Patch Clickable Detail Tags")]
        public static void PatchClickableDetailTags()
        {
            if (DeferUntilEditMode(PatchClickableDetailTags))
                return;

            if (!File.Exists(ScenePath))
            {
                Debug.LogError($"Scene not found: {ScenePath}");
                return;
            }

            UiSpriteFactory.EnsureSprites();
            EnsureDetailTagButtonPrefab();
            EnsureDetailSectionPrefabs();
            AssetDatabase.SaveAssets();

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            PatchClickableDetailTagsInOpenScene();
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Clickable detail tags patched (prefabs + Main scene).");
        }

        static void PatchClickableDetailTagsInOpenScene()
        {
            var navigation = Object.FindFirstObjectByType<NavigationController>();
            if (navigation == null)
            {
                Debug.LogError("NavigationController not found in Main scene.");
                return;
            }

            var tagButtonPrefab = EnsureDetailTagButtonPrefab();
            var labeledSections = Object.FindObjectsByType<LabeledTextDetailSection>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var section in labeledSections)
            {
                var so = new SerializedObject(section);
                so.FindProperty("navigation").objectReferenceValue = navigation;
                if (so.FindProperty("tagButtonPrefab").objectReferenceValue == null)
                    so.FindProperty("tagButtonPrefab").objectReferenceValue = tagButtonPrefab;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        static void PatchIndexInOpenScene(LocalizationRefs loc)
        {
            var navigation = Object.FindFirstObjectByType<NavigationController>();
            var repository = Object.FindFirstObjectByType<MathematicianRepository>();
            if (navigation == null || repository == null)
            {
                Debug.LogError("NavigationController or MathematicianRepository not found in Main scene.");
                return;
            }

            var contentArea = GameObject.Find("ContentArea")?.transform;
            if (contentArea == null)
            {
                Debug.LogError("ContentArea not found in Main scene.");
                return;
            }

            var listItemPrefab = AssetDatabase.LoadAssetAtPath<MathematicianListItem>(
                $"{PrefabFolder}/MathematicianListItem.prefab");

            GameObject indexPanelGo = contentArea.Find("IndexPanel")?.gameObject;
            if (indexPanelGo == null)
            {
                indexPanelGo = CreateIndexPanel(contentArea, navigation, repository, listItemPrefab, loc);
                indexPanelGo.transform.SetSiblingIndex(contentArea.Find("ListPanel")?.GetSiblingIndex() ?? 1);
            }
            else
            {
                HomeListPanelLayout.ApplyToIndexPanel(indexPanelGo);
                var listScrollGo = indexPanelGo.transform.Find("ListScroll")?.gameObject;
                var panelFontScope = indexPanelGo.GetComponent<FontSizeScope>();
                if (panelFontScope != null)
                {
                    Object.DestroyImmediate(panelFontScope);
                    if (listScrollGo != null && listScrollGo.GetComponent<FontSizeScope>() == null)
                        listScrollGo.AddComponent<FontSizeScope>();
                }
                else if (listScrollGo != null && listScrollGo.GetComponent<FontSizeScope>() == null)
                {
                    listScrollGo.AddComponent<FontSizeScope>();
                }
            }

            var header = GameObject.Find("Header");
            if (header != null)
            {
                var binder = header.GetComponent<HeaderTitleBinder>();
                var indexTitle = header.transform.Find("IndexTitle")?.gameObject;
                if (indexTitle == null)
                {
                    indexTitle = CreateLocalizedTitle(header.transform, "IndexTitle", loc.IndexTitle);
                    indexTitle.SetActive(false);
                }

                if (binder != null)
                {
                    var binderSo = new SerializedObject(binder);
                    binderSo.FindProperty("indexTitleEvent").objectReferenceValue =
                        indexTitle.GetComponent<LocalizeStringEvent>();
                    binderSo.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            var bottomBar = GameObject.Find("BottomBar")?.transform;
            Button browseTab = null;
            Button indexTab = null;
            Button settingsTab = null;
            if (bottomBar != null)
            {
                var browseGo = bottomBar.Find("BrowseTab")?.gameObject;
                var settingsGo = bottomBar.Find("SettingsTab")?.gameObject;
                if (browseGo != null)
                {
                    UiButtonLayout.ApplyTopLeftAnchoredRect(
                        browseGo.GetComponent<RectTransform>(),
                        UiButtonLayout.BottomBrowse.Position,
                        UiButtonLayout.BottomBrowse.Size);
                    browseTab = browseGo.GetComponent<Button>();
                }

                if (settingsGo != null)
                {
                    UiButtonLayout.ApplyTopLeftAnchoredRect(
                        settingsGo.GetComponent<RectTransform>(),
                        UiButtonLayout.BottomSettings.Position,
                        UiButtonLayout.BottomSettings.Size);
                    settingsTab = settingsGo.GetComponent<Button>();
                }

                var indexGo = bottomBar.Find("IndexTab")?.gameObject;
                if (indexGo == null)
                {
                    indexGo = CreateSceneButton(bottomBar, UiButtonLayout.BottomIndex, loc.UiCollection);
                    WireButtonClick(indexGo.GetComponent<Button>(), navigation.OnIndexTabClicked);
                }
                else
                {
                    UiButtonLayout.ApplyTopLeftAnchoredRect(
                        indexGo.GetComponent<RectTransform>(),
                        UiButtonLayout.BottomIndex.Position,
                        UiButtonLayout.BottomIndex.Size);
                }

                indexTab = indexGo.GetComponent<Button>();
            }

            var home = contentArea.Find("HomePanel")?.gameObject;
            var list = contentArea.Find("ListPanel")?.gameObject;
            var favorites = contentArea.Find("FavoritesPanel")?.gameObject;
            var detail = contentArea.Find("DetailPanel")?.gameObject;
            var settings = contentArea.Find("SettingsPanel")?.gameObject;
            var backButton = header != null ? header.transform.Find("BackButton")?.gameObject : null;
            var headerBinder = header != null ? header.GetComponent<HeaderTitleBinder>() : null;

            if (home != null && list != null && detail != null && settings != null && backButton != null && headerBinder != null
                && browseTab != null && indexTab != null && settingsTab != null)
            {
                WireNavigation(
                    navigation,
                    home,
                    indexPanelGo,
                    list,
                    favorites != null ? favorites : CreateFavoritesPanel(contentArea, navigation, repository, listItemPrefab, loc),
                    detail,
                    settings,
                    backButton,
                    headerBinder,
                    new BottomBarResult
                    {
                        browseTab = browseTab,
                        indexTab = indexTab,
                        settingsTab = settingsTab,
                        favoritesButton = null
                    });
            }
            else
            {
                var navSo = new SerializedObject(navigation);
                navSo.FindProperty("indexPanel").objectReferenceValue = indexPanelGo.GetComponent<IndexPanel>();
                if (indexTab != null)
                    navSo.FindProperty("indexTab").objectReferenceValue = indexTab;
                navSo.ApplyModifiedPropertiesWithoutUndo();
            }

            EnsureThemedCardOnPrefab($"{PrefabFolder}/LetterButton.prefab", UiCardVariant.Filter);
            CreateLetterButtonPrefab();
        }

        static void PatchFavoritesInOpenScene(LocalizationRefs loc)
        {
            var navigation = Object.FindFirstObjectByType<NavigationController>();
            var repository = Object.FindFirstObjectByType<MathematicianRepository>();
            if (navigation == null || repository == null)
            {
                Debug.LogError("NavigationController or MathematicianRepository not found in Main scene.");
                return;
            }

            var contentArea = GameObject.Find("ContentArea")?.transform;
            if (contentArea == null)
            {
                Debug.LogError("ContentArea not found in Main scene.");
                return;
            }

            var contentRt = contentArea.GetComponent<RectTransform>();
            if (contentRt != null)
                contentRt.offsetMin = new Vector2(0f, UiButtonLayout.BottomBarSize.y);

            var listItemPrefab = AssetDatabase.LoadAssetAtPath<MathematicianListItem>(
                $"{PrefabFolder}/MathematicianListItem.prefab");

            var favoritesPanelGo = contentArea.Find("FavoritesPanel")?.gameObject;
            if (favoritesPanelGo == null)
            {
                favoritesPanelGo = CreateFavoritesPanel(contentArea, navigation, repository, listItemPrefab, loc);
                var listPanel = contentArea.Find("ListPanel");
                if (listPanel != null)
                    favoritesPanelGo.transform.SetSiblingIndex(listPanel.GetSiblingIndex() + 1);
            }
            else
            {
                EnsureFavoritesPanelAnimation(favoritesPanelGo);
            }

            var header = GameObject.Find("Header");
            HeaderTitleBinder headerBinder = null;
            if (header != null)
            {
                headerBinder = header.GetComponent<HeaderTitleBinder>();
                var favoritesTitle = header.transform.Find("FavoritesTitle")?.gameObject;
                if (favoritesTitle == null)
                {
                    favoritesTitle = CreateLocalizedTitle(header.transform, "FavoritesTitle", loc.FavoritesTitle);
                    favoritesTitle.SetActive(false);
                }

                if (headerBinder != null)
                {
                    var binderSo = new SerializedObject(headerBinder);
                    binderSo.FindProperty("favoritesTitleEvent").objectReferenceValue =
                        favoritesTitle.GetComponent<LocalizeStringEvent>();
                    binderSo.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            Button browseTab = null;
            Button indexTab = null;
            Button settingsTab = null;
            Button favoritesButton = null;
            var bottomBar = GameObject.Find("BottomBar")?.transform;
            if (bottomBar != null)
            {
                UiButtonLayout.ApplyBottomStretchBarRect(
                    bottomBar.GetComponent<RectTransform>(),
                    UiButtonLayout.BottomBarPosition,
                    UiButtonLayout.BottomBarSize);

                var browseGo = bottomBar.Find("BrowseTab")?.gameObject;
                if (browseGo != null)
                {
                    UiButtonLayout.ApplyTopLeftAnchoredRect(
                        browseGo.GetComponent<RectTransform>(),
                        UiButtonLayout.BottomBrowse.Position,
                        UiButtonLayout.BottomBrowse.Size);
                    browseTab = browseGo.GetComponent<Button>();
                }

                var indexGo = bottomBar.Find("IndexTab")?.gameObject;
                if (indexGo != null)
                {
                    UiButtonLayout.ApplyTopLeftAnchoredRect(
                        indexGo.GetComponent<RectTransform>(),
                        UiButtonLayout.BottomIndex.Position,
                        UiButtonLayout.BottomIndex.Size);
                    indexTab = indexGo.GetComponent<Button>();
                }

                var settingsGo = bottomBar.Find("SettingsTab")?.gameObject;
                if (settingsGo != null)
                {
                    UiButtonLayout.ApplyTopLeftAnchoredRect(
                        settingsGo.GetComponent<RectTransform>(),
                        UiButtonLayout.BottomSettings.Position,
                        UiButtonLayout.BottomSettings.Size);
                    settingsTab = settingsGo.GetComponent<Button>();
                }

                var favoritesGo = bottomBar.Find("FavoritesTab")?.gameObject;
                if (favoritesGo == null)
                {
                    favoritesGo = CreateSceneButton(bottomBar, UiButtonLayout.BottomFavorites, loc.UiCollection);
                    WireButtonClick(favoritesGo.GetComponent<Button>(), navigation.OnFavoritesButtonClicked);
                }
                else
                {
                    UiButtonLayout.ApplyTopLeftAnchoredRect(
                        favoritesGo.GetComponent<RectTransform>(),
                        UiButtonLayout.BottomFavorites.Position,
                        UiButtonLayout.BottomFavorites.Size);
                    WireButtonClick(favoritesGo.GetComponent<Button>(), navigation.OnFavoritesButtonClicked);
                }

                favoritesButton = favoritesGo.GetComponent<Button>();
            }

            var home = contentArea.Find("HomePanel")?.gameObject;
            var indexPanelGo = contentArea.Find("IndexPanel")?.gameObject;
            var list = contentArea.Find("ListPanel")?.gameObject;
            var detail = contentArea.Find("DetailPanel")?.gameObject;
            var settings = contentArea.Find("SettingsPanel")?.gameObject;
            var backButton = header != null ? header.transform.Find("BackButton")?.gameObject : null;

            if (home != null && indexPanelGo != null && list != null && detail != null && settings != null
                && backButton != null && headerBinder != null && browseTab != null && indexTab != null
                && settingsTab != null && favoritesButton != null)
            {
                WireNavigation(
                    navigation,
                    home,
                    indexPanelGo,
                    list,
                    favoritesPanelGo,
                    detail,
                    settings,
                    backButton,
                    headerBinder,
                    new BottomBarResult
                    {
                        browseTab = browseTab,
                        indexTab = indexTab,
                        settingsTab = settingsTab,
                        favoritesButton = favoritesButton
                    });
            }
            else
            {
                var navSo = new SerializedObject(navigation);
                navSo.FindProperty("favoritesPanel").objectReferenceValue =
                    favoritesPanelGo.GetComponent<FavoritesPanel>();
                navSo.FindProperty("favoritesTransition").objectReferenceValue =
                    favoritesPanelGo.GetComponent<UiPanelSlideTransition>();
                if (favoritesButton != null)
                    navSo.FindProperty("favoritesButton").objectReferenceValue = favoritesButton;
                navSo.ApplyModifiedPropertiesWithoutUndo();
            }

            EnsureThemedCardOnPrefab($"{PrefabFolder}/MathematicianListItem.prefab", UiCardVariant.ListItem);
            EnsureThemedCardOnPrefab("Assets/Resources/MathematicianListItem.prefab", UiCardVariant.ListItem);
        }

        static void EnsureThemedCardOnPrefab(string path, UiCardVariant variant)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
                return;

            var root = PrefabUtility.LoadPrefabContents(path);
            EnsureThemedCard(root, variant);
            EnsureGlassSurfaceOnCardFill(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        static void AssignMathematicians(MathematicianRepository repository, List<MathematicianData> list) =>
            MathematicianRepositoryRefresh.AssignToRepository(repository, list);
    }
}
