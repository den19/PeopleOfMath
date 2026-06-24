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

            AddUiEntry(collection, "title_home",
                "Математики: века, страны, разделы математики",
                "Mathematicians: centuries, countries, branches of mathematics");
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
            AddUiEntry(collection, "empty_list", "Нет математиков по выбранному фильтру", "No mathematicians for this filter");
            AddUiEntry(collection, "gallery_license", "Лицензия", "License");
            AddUiEntry(collection, "gallery_source", "Источник", "Source");
            AddUiEntry(collection, "gallery_no_images", "Изображения недоступны", "No images available");

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
            var header = CreateHeader(canvasGo.transform, loc);
            var content = CreateContentArea(canvasGo.transform);
            var home = CreateHomePanel(content.transform, navigation, loc);
            var list = CreateListPanel(content.transform, navigation, repository, listItemPrefab, loc);
            var headerBinder = header.root.GetComponent<HeaderTitleBinder>();
            var detail = CreateDetailPanel(content.transform, repository, navigation, headerBinder, loc);
            var settings = CreateSettingsPanel(content.transform, loc);
            var bottom = CreateBottomBar(canvasGo.transform, navigation, loc);

            WireNavigation(navigation, home, list, detail, settings, header.backButton, headerBinder, bottom);
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
            return CreatePanel(canvas, "ContentArea", new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 140), new Vector2(0, -120));
        }

        static GameObject CreateHomePanel(Transform parent, NavigationController nav, LocalizationRefs loc)
        {
            var panel = CreatePanel(parent, "HomePanel", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            AddHomeDecorGlow(panel.transform);
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

        static Button CreateFilterButtonPrefab()
        {
            var path = $"{PrefabFolder}/FilterButton.prefab";
            var go = BuildFilterButtonRoot();
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab.GetComponent<Button>();
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

        static void AddSectionVerticalLayout(GameObject root)
        {
            var vlg = root.AddComponent<VerticalLayoutGroup>();
            var pad = UiLayoutMetrics.ScaleDetailPadding(UiLayoutMetrics.DetailSectionPadding);
            vlg.padding = new RectOffset(pad, pad, pad, pad);
            vlg.spacing = UiLayoutMetrics.ScaleDetailPadding(UiLayoutMetrics.DetailSectionSpacing);
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
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
            AddSectionVerticalLayout(root);
            var name = AddDetailField(root.transform, "Name", 26, FontStyles.Bold, autoHeight: true, autoMinHeightBase: 48f, useContentSizeFitter: false, textColor: UiTheme.TextPrimary);
            var dates = AddDetailField(root.transform, "Dates", 16, FontStyles.Normal, height: 40, textColor: UiTheme.TextSecondary);
            var section = root.AddComponent<IdentityDetailSection>();
            var so = new SerializedObject(section);
            so.FindProperty("nameText").objectReferenceValue = name;
            so.FindProperty("datesText").objectReferenceValue = dates;
            so.ApplyModifiedPropertiesWithoutUndo();
            return root;
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
            var body = AddDetailField(root.transform, "Body", 15, FontStyles.Normal, height: 120, textColor: UiTheme.TextSecondary);
            var section = root.AddComponent<LabeledTextDetailSection>();
            var so = new SerializedObject(section);
            so.FindProperty("sectionKind").enumValueIndex = (int)kind;
            so.FindProperty("labelText").objectReferenceValue = label;
            so.FindProperty("bodyText").objectReferenceValue = body;
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
            var themeStatus = CreateTmpChild(panel.transform, "ThemeStatus", 16, FontStyles.Italic, new Vector2(40, -1040));
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
            so.FindProperty("themeStatusText").objectReferenceValue = themeStatus.GetComponent<TMP_Text>();
            so.ApplyModifiedPropertiesWithoutUndo();

            WireButtonClick(ruBtn.GetComponent<Button>(), settings.SelectRussian);
            WireButtonClick(enBtn.GetComponent<Button>(), settings.SelectEnglish);
            WireButtonClick(fontNormalBtn.GetComponent<Button>(), settings.SelectFontNormal);
            WireButtonClick(fontLargeBtn.GetComponent<Button>(), settings.SelectFontLarge);
            WireButtonClick(fontExtraLargeBtn.GetComponent<Button>(), settings.SelectFontExtraLarge);
            WireButtonClick(darkBtn.GetComponent<Button>(), settings.SelectDark);
            WireButtonClick(lightBtn.GetComponent<Button>(), settings.SelectLight);
            return panel;
        }

        struct BottomBarResult
        {
            public Button browseTab;
            public Button settingsTab;
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
            var settings = CreateSceneButton(bar.transform, UiButtonLayout.BottomSettings, loc.UiCollection);
            WireButtonClick(browse.GetComponent<Button>(), nav.OnBrowseTabClicked);
            WireButtonClick(settings.GetComponent<Button>(), nav.OnSettingsTabClicked);
            return new BottomBarResult
            {
                browseTab = browse.GetComponent<Button>(),
                settingsTab = settings.GetComponent<Button>()
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
            GameObject list,
            GameObject detail,
            GameObject settings,
            GameObject backButton,
            HeaderTitleBinder header,
            BottomBarResult bottomBar)
        {
            var so = new SerializedObject(nav);
            so.FindProperty("homePanel").objectReferenceValue = home.GetComponent<HomePanel>();
            so.FindProperty("listPanel").objectReferenceValue = list.GetComponent<ListPanel>();
            so.FindProperty("detailPanel").objectReferenceValue = detail.GetComponent<DetailPanel>();
            so.FindProperty("settingsPanel").objectReferenceValue = settings.GetComponent<SettingsPanel>();
            so.FindProperty("headerBackButton").objectReferenceValue = backButton;
            so.FindProperty("headerTitle").objectReferenceValue = header;
            so.FindProperty("browseTab").objectReferenceValue = bottomBar.browseTab;
            so.FindProperty("settingsTab").objectReferenceValue = bottomBar.settingsTab;
            so.ApplyModifiedPropertiesWithoutUndo();
            WireButtonClick(backButton.GetComponent<Button>(), nav.OnBackButtonClicked);
        }

        static void WireButtonClick(Button button, UnityAction action)
        {
            if (button == null || action == null)
                return;

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
            var gallery = detail.GetComponentInChildren<PortraitGalleryView>(true);
            var so = new SerializedObject(scope);
            so.FindProperty("targetCamera").objectReferenceValue = cam;
            so.FindProperty("navigation").objectReferenceValue = nav;
            so.FindProperty("settingsPanel").objectReferenceValue = settings.GetComponent<SettingsPanel>();
            so.FindProperty("portraitGallery").objectReferenceValue = gallery;
            so.ApplyModifiedPropertiesWithoutUndo();
            TagThemeBindings(canvasGo.transform);
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
                         || name is "Name" or "Label" or "HomeTitle" or "SettingsTitle" or "PlainTitle")
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
            var topGlow = bar.Find("TopGlow");
            if (topGlow != null)
                AddThemeBinding(topGlow.gameObject, UiThemeToken.NavBarAccent);
        }

        static void PatchThemeInOpenScene(StringTableCollection collection)
        {
            var settings = Object.FindFirstObjectByType<SettingsPanel>(FindObjectsInactive.Include);
            if (settings != null)
            {
                var panel = settings.gameObject;
                if (panel.transform.Find("DarkThemeButton") == null)
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
                    var themeStatus = CreateTmpChild(panel.transform, "ThemeStatus", 16, FontStyles.Italic, new Vector2(40, -1040));
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
            }

            var canvas = Object.FindAnyObjectByType<Canvas>();
            var navigation = Object.FindAnyObjectByType<NavigationController>();
            var gallery = Object.FindFirstObjectByType<PortraitGalleryView>(FindObjectsInactive.Include);
            if (canvas != null)
            {
                var scope = canvas.GetComponent<UiThemeScope>() ?? canvas.gameObject.AddComponent<UiThemeScope>();
                var scopeSo = new SerializedObject(scope);
                scopeSo.FindProperty("targetCamera").objectReferenceValue = Camera.main;
                scopeSo.FindProperty("navigation").objectReferenceValue = navigation;
                scopeSo.FindProperty("settingsPanel").objectReferenceValue = settings;
                scopeSo.FindProperty("portraitGallery").objectReferenceValue = gallery;
                scopeSo.ApplyModifiedPropertiesWithoutUndo();
                TagThemeBindings(canvas.transform);
            }

            EnsureThemedCardOnPrefab($"{PrefabFolder}/FilterButton.prefab", UiCardVariant.Filter);
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
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        static void AssignMathematicians(MathematicianRepository repository, List<MathematicianData> list) =>
            MathematicianRepositoryRefresh.AssignToRepository(repository, list);
    }
}
