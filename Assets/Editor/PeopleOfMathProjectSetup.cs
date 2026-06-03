using System.Collections.Generic;
using System.IO;
using PeopleOfMath.Core;
using PeopleOfMath.Data;
using PeopleOfMath.Input;
using PeopleOfMath.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Localization;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;
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

        [MenuItem("PeopleOfMath/Setup Project")]
        public static void SetupMenu() => Run();

        [MenuItem("PeopleOfMath/Regenerate Main Scene")]
        public static void RegenerateScene()
        {
            if (File.Exists(ScenePath))
                AssetDatabase.DeleteAsset(ScenePath);
            PeopleOfMathAutoSetup.ResetSession();
            Run();
        }

        public static void Run()
        {
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

        static void RunInternal()
        {
            EnsureFolders();
            if (!Application.isBatchMode)
                TryImportTmpResources();
            SetupRenderPipeline();
            SetupPlayerSettings();
            SetupAndroidTarget();
            var mathematicians = MathematicianContentFactory.CreateAll(DataFolder);
            var localization = SetupLocalization();
            var listItemPrefab = CreateListItemPrefab();
            EnsureListItemInResources();
            CreateMainScene(mathematicians, localization, listItemPrefab);
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
                         PrefabFolder, "Assets/Scripts", "Assets/Editor"
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
            public LocalizedString SettingsTitle;
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

            AddUiEntry(collection, "title_home", "Люди математики", "Mathematicians");
            AddUiEntry(collection, "title_settings", "Настройки", "Settings");
            AddUiEntry(collection, "title_detail", "Карточка", "Profile");
            AddUiEntry(collection, "section_century", "По веку", "By century");
            AddUiEntry(collection, "section_country", "По стране", "By country");
            AddUiEntry(collection, "section_branch", "По разделу", "By field");
            AddUiEntry(collection, "filter_century", "{0}", "{0}");
            AddUiEntry(collection, "filter_country", "{0}", "{0}");
            AddUiEntry(collection, "filter_branch", "{0}", "{0}");
            AddUiEntry(collection, "tab_browse", "Справочник", "Browse");
            AddUiEntry(collection, "tab_settings", "Настройки", "Settings");
            AddUiEntry(collection, "btn_back", "Назад", "Back");
            AddUiEntry(collection, "settings_language", "Язык интерфейса", "Interface language");
            AddUiEntry(collection, "btn_russian", "Русский", "Russian");
            AddUiEntry(collection, "btn_english", "English", "English");
            AddUiEntry(collection, "empty_list", "Нет математиков по выбранному фильтру", "No mathematicians for this filter");

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

            return new LocalizationRefs
            {
                Russian = ru,
                English = en,
                UiCollection = collection,
                HomeTitle = MakeLocalized(collection, "title_home"),
                SettingsTitle = MakeLocalized(collection, "title_settings"),
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
            var ls = new LocalizedString
            {
                TableReference = collection.TableCollectionNameReference,
                TableEntryReference = entry.Key
            };
            return ls;
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

        static MathematicianListItem CreateListItemPrefab()
        {
            var path = $"{PrefabFolder}/MathematicianListItem.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
                return existing.GetComponent<MathematicianListItem>();

            var root = new GameObject("MathematicianListItem", typeof(RectTransform), typeof(Image), typeof(Button));
            var rt = root.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 120);
            var img = root.GetComponent<Image>();
            img.color = new Color(0.2f, 0.22f, 0.28f, 1f);

            var nameGo = CreateTmpChild(root.transform, "Name", 20, FontStyles.Bold, new Vector2(10, -10));
            var datesGo = CreateTmpChild(root.transform, "Dates", 14, FontStyles.Normal, new Vector2(10, -38));
            var bioGo = CreateTmpChild(root.transform, "Bio", 13, FontStyles.Italic, new Vector2(10, -58));
            bioGo.GetComponent<RectTransform>().sizeDelta = new Vector2(-20, 50);

            var item = root.AddComponent<MathematicianListItem>();
            var so = new SerializedObject(item);
            so.FindProperty("nameText").objectReferenceValue = nameGo.GetComponent<TMP_Text>();
            so.FindProperty("datesText").objectReferenceValue = datesGo.GetComponent<TMP_Text>();
            so.FindProperty("bioText").objectReferenceValue = bioGo.GetComponent<TMP_Text>();
            so.FindProperty("button").objectReferenceValue = root.GetComponent<Button>();
            so.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<MathematicianListItem>();
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
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.color = Color.white;
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
            AssignMathematicians(repository, mathematicians);
            app.AddComponent<BackButtonHandler>();

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.13f, 0.16f);
            camGo.AddComponent<UniversalAdditionalCameraData>();

            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<InputSystemUIInputModule>();

            var canvasGo = CreateCanvas();
            var header = CreateHeader(canvasGo.transform, loc);
            var content = CreateContentArea(canvasGo.transform);
            var home = CreateHomePanel(content.transform, navigation, loc);
            var list = CreateListPanel(content.transform, navigation, repository, listItemPrefab, loc);
            var detail = CreateDetailPanel(content.transform, repository);
            var settings = CreateSettingsPanel(content.transform, loc);
            var bottom = CreateBottomBar(canvasGo.transform, navigation, loc);

            var headerBinder = header.root.GetComponent<HeaderTitleBinder>();
            WireNavigation(navigation, home, list, detail, settings, header.backButton, headerBinder);
            WireBootstrap(bootstrap, navigation);
            WireBackHandler(app.GetComponent<BackButtonHandler>(), navigation);

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
            var back = CreateButton(header.transform, "BackButton", new Vector2(20, -60), new Vector2(160, 56), loc.UiCollection, "btn_back");
            back.SetActive(false);

            var homeTitle = CreateLocalizedTitle(header.transform, "HomeTitle", loc.HomeTitle);
            var settingsTitle = CreateLocalizedTitle(header.transform, "SettingsTitle", loc.SettingsTitle);
            settingsTitle.SetActive(false);

            var plainTitle = CreateTmpChild(header.transform, "PlainTitle", 22, FontStyles.Bold, new Vector2(180, -50));
            plainTitle.GetComponent<RectTransform>().sizeDelta = new Vector2(-200, 40);
            plainTitle.SetActive(false);

            var binder = header.AddComponent<HeaderTitleBinder>();
            var so = new SerializedObject(binder);
            so.FindProperty("titleText").objectReferenceValue = plainTitle.GetComponent<TMP_Text>();
            so.FindProperty("homeTitleEvent").objectReferenceValue = homeTitle.GetComponent<LocalizeStringEvent>();
            so.FindProperty("settingsTitleEvent").objectReferenceValue = settingsTitle.GetComponent<LocalizeStringEvent>();
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
            var lse = go.AddComponent<LocalizeStringEvent>();
            var so = new SerializedObject(lse);
            AssignLocalized(so.FindProperty("m_StringReference"), localized);
            so.ApplyModifiedPropertiesWithoutUndo();
            return go;
        }

        static GameObject CreateContentArea(Transform canvas)
        {
            return CreatePanel(canvas, "ContentArea", new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 140), new Vector2(0, -120));
        }

        static GameObject CreateHomePanel(Transform parent, NavigationController nav, LocalizationRefs loc)
        {
            var panel = CreatePanel(parent, "HomePanel", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var scroll = CreateScrollView(panel.transform, "HomeScroll");
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
            so.FindProperty("centuryContainer").objectReferenceValue = centuryBox;
            so.FindProperty("countryContainer").objectReferenceValue = countryBox;
            so.FindProperty("branchContainer").objectReferenceValue = branchBox;
            so.FindProperty("filterButtonPrefab").objectReferenceValue = filterPrefab;
            so.ApplyModifiedPropertiesWithoutUndo();
            panel.SetActive(true);
            return panel;
        }

        static Button CreateFilterButtonPrefab()
        {
            var path = $"{PrefabFolder}/FilterButton.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<Button>(path);
            if (existing != null)
                return existing;

            var go = new GameObject("FilterButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            var le = go.GetComponent<LayoutElement>();
            le.preferredHeight = 52;
            var img = go.GetComponent<Image>();
            img.color = new Color(0.28f, 0.35f, 0.48f);
            var label = CreateTmpChild(go.transform, "Label", 18, FontStyles.Normal, new Vector2(16, -14));
            label.GetComponent<RectTransform>().anchorMax = Vector2.one;
            var btn = go.GetComponent<Button>();
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab.GetComponent<Button>();
        }

        static void AddSectionLabel(Transform parent, StringTableCollection collection, string key)
        {
            var label = CreateTmpChild(parent, key, 18, FontStyles.Bold, Vector2.zero);
            var rt = label.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 36);
            var le = label.AddComponent<LayoutElement>();
            le.preferredHeight = 36;
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
            vlg.childForceExpandHeight = false;
            vlg.spacing = 8;
            vlg.padding = new RectOffset(0, 0, 4, 12);
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
            viewport.GetComponent<Image>().color = new Color(0, 0, 0, 0.01f);

            var content = CreatePanel(viewport.transform, "Content", new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            var contentRt = content.GetComponent<RectTransform>();
            contentRt.pivot = new Vector2(0.5f, 1);
            contentRt.anchorMin = new Vector2(0, 1);
            contentRt.anchorMax = new Vector2(1, 1);
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(24, 24, 16, 24);
            vlg.spacing = 12;
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

        static GameObject CreateListPanel(
            Transform parent,
            NavigationController nav,
            MathematicianRepository repo,
            MathematicianListItem prefab,
            LocalizationRefs loc)
        {
            var panel = CreatePanel(parent, "ListPanel", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            panel.SetActive(false);
            var scroll = CreateScrollView(panel.transform, "ListScroll");
            var empty = CreateTmpChild(panel.transform, "Empty", 16, FontStyles.Italic, new Vector2(40, -200));
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

        static GameObject CreateDetailPanel(Transform parent, MathematicianRepository repo)
        {
            var panel = CreatePanel(parent, "DetailPanel", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            panel.SetActive(false);
            var scroll = CreateScrollView(panel.transform, "DetailScroll");
            var content = scroll.content;

            var name = AddDetailField(content, "Name", 26, FontStyles.Bold);
            var dates = AddDetailField(content, "Dates", 16, FontStyles.Normal);
            var countriesLabel = AddDetailField(content, "CountriesLabel", 15, FontStyles.Bold);
            var countries = AddDetailField(content, "Countries", 15, FontStyles.Normal);
            var centuriesLabel = AddDetailField(content, "CenturiesLabel", 15, FontStyles.Bold);
            var centuries = AddDetailField(content, "Centuries", 15, FontStyles.Normal);
            var branchesLabel = AddDetailField(content, "BranchesLabel", 15, FontStyles.Bold);
            var branches = AddDetailField(content, "Branches", 15, FontStyles.Normal);
            var achievementsLabel = AddDetailField(content, "AchievementsLabel", 15, FontStyles.Bold);
            var achievements = AddDetailField(content, "Achievements", 15, FontStyles.Normal, 200);
            var personalLabel = AddDetailField(content, "PersonalLabel", 15, FontStyles.Bold);
            var personal = AddDetailField(content, "Personal", 15, FontStyles.Normal, 200);

            var detail = panel.AddComponent<DetailPanel>();
            var so = new SerializedObject(detail);
            so.FindProperty("repository").objectReferenceValue = repo;
            so.FindProperty("nameText").objectReferenceValue = name;
            so.FindProperty("datesText").objectReferenceValue = dates;
            so.FindProperty("countriesLabel").objectReferenceValue = countriesLabel;
            so.FindProperty("countriesText").objectReferenceValue = countries;
            so.FindProperty("centuriesLabel").objectReferenceValue = centuriesLabel;
            so.FindProperty("centuriesText").objectReferenceValue = centuries;
            so.FindProperty("branchesLabel").objectReferenceValue = branchesLabel;
            so.FindProperty("branchesText").objectReferenceValue = branches;
            so.FindProperty("achievementsLabel").objectReferenceValue = achievementsLabel;
            so.FindProperty("achievementsText").objectReferenceValue = achievements;
            so.FindProperty("personalLifeLabel").objectReferenceValue = personalLabel;
            so.FindProperty("personalLifeText").objectReferenceValue = personal;
            so.ApplyModifiedPropertiesWithoutUndo();
            return panel;
        }

        static TMP_Text AddDetailField(Transform parent, string name, float size, FontStyles style, float height = 36)
        {
            var go = CreateTmpChild(parent, name, size, style, Vector2.zero);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, height);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.flexibleWidth = 1;
            return go.GetComponent<TMP_Text>();
        }

        static GameObject CreateSettingsPanel(Transform parent, LocalizationRefs loc)
        {
            var panel = CreatePanel(parent, "SettingsPanel", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            panel.SetActive(false);

            var langLabel = CreateTmpChild(panel.transform, "LangLabel", 18, FontStyles.Bold, new Vector2(40, -80));
            var lse = langLabel.AddComponent<LocalizeStringEvent>();
            var collection = loc.UiCollection;
            var soLse = new SerializedObject(lse);
            AssignLocalized(soLse.FindProperty("m_StringReference"), MakeLocalized(collection, "settings_language"));
            soLse.ApplyModifiedPropertiesWithoutUndo();

            var ruBtn = CreateButton(panel.transform, "RuButton", new Vector2(40, -160), new Vector2(400, 64), collection, "btn_russian");
            var enBtn = CreateButton(panel.transform, "EnButton", new Vector2(40, -240), new Vector2(400, 64), collection, "btn_english");
            var status = CreateTmpChild(panel.transform, "Status", 16, FontStyles.Italic, new Vector2(40, -320));

            var settings = panel.AddComponent<SettingsPanel>();
            var so = new SerializedObject(settings);
            so.FindProperty("russianButton").objectReferenceValue = ruBtn.GetComponent<Button>();
            so.FindProperty("englishButton").objectReferenceValue = enBtn.GetComponent<Button>();
            so.FindProperty("statusText").objectReferenceValue = status.GetComponent<TMP_Text>();
            so.ApplyModifiedPropertiesWithoutUndo();

            ruBtn.GetComponent<Button>().onClick.AddListener(settings.SelectRussian);
            enBtn.GetComponent<Button>().onClick.AddListener(settings.SelectEnglish);
            return panel;
        }

        static GameObject CreateBottomBar(Transform canvas, NavigationController nav, LocalizationRefs loc)
        {
            var bar = CreatePanel(canvas, "BottomBar", new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(0, 140));
            var browse = CreateButton(bar.transform, "BrowseTab", new Vector2(40, 40), new Vector2(440, 72), loc.UiCollection, "tab_browse");
            var settings = CreateButton(bar.transform, "SettingsTab", new Vector2(520, 40), new Vector2(440, 72), loc.UiCollection, "tab_settings");
            browse.GetComponent<Button>().onClick.AddListener(nav.OnBrowseTabClicked);
            settings.GetComponent<Button>().onClick.AddListener(nav.OnSettingsTabClicked);
            return bar;
        }

        static GameObject CreateButton(
            Transform parent,
            string name,
            Vector2 pos,
            Vector2 size,
            StringTableCollection collection,
            string key)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            go.GetComponent<Image>().color = new Color(0.25f, 0.32f, 0.45f);
            var label = CreateTmpChild(go.transform, "Text", 17, FontStyles.Normal, new Vector2(12, -18));
            label.GetComponent<RectTransform>().anchorMax = Vector2.one;
            var lse = label.AddComponent<LocalizeStringEvent>();
            var so = new SerializedObject(lse);
            AssignLocalized(so.FindProperty("m_StringReference"), MakeLocalized(collection, key));
            so.ApplyModifiedPropertiesWithoutUndo();
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
            go.GetComponent<Image>().color = new Color(0.14f, 0.15f, 0.18f, 0.95f);
            return go;
        }

        static void WireNavigation(
            NavigationController nav,
            GameObject home,
            GameObject list,
            GameObject detail,
            GameObject settings,
            GameObject backButton,
            HeaderTitleBinder header)
        {
            var so = new SerializedObject(nav);
            so.FindProperty("homePanel").objectReferenceValue = home.GetComponent<HomePanel>();
            so.FindProperty("listPanel").objectReferenceValue = list.GetComponent<ListPanel>();
            so.FindProperty("detailPanel").objectReferenceValue = detail.GetComponent<DetailPanel>();
            so.FindProperty("settingsPanel").objectReferenceValue = settings.GetComponent<SettingsPanel>();
            so.FindProperty("headerBackButton").objectReferenceValue = backButton;
            so.FindProperty("headerTitle").objectReferenceValue = header;
            so.ApplyModifiedPropertiesWithoutUndo();
            backButton.GetComponent<Button>().onClick.AddListener(nav.OnBackButtonClicked);
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

        static void AssignMathematicians(MathematicianRepository repository, List<MathematicianData> list)
        {
            var so = new SerializedObject(repository);
            var prop = so.FindProperty("mathematicians");
            prop.ClearArray();
            for (var i = 0; i < list.Count; i++)
            {
                prop.InsertArrayElementAtIndex(i);
                prop.GetArrayElementAtIndex(i).objectReferenceValue = list[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
