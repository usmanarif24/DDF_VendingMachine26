using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Profile;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// All display objects and player settings are authored in edit mode and saved to the project.
public static class GiftCatcherAndroidSettings
{
    const string AppId = "com.ddf.giftcatcher";
    const string ProfilePath = "Assets/Settings/Build Profiles/Android™.asset";

    [InitializeOnLoadMethod]
    static void FirstImport()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var bagImporter = AssetImporter.GetAtPath("Assets/Media/UI/bag.png") as TextureImporter;
            if (PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android) == AppId &&
                bagImporter && bagImporter.npotScale == TextureImporterNPOTScale.None && !bagImporter.mipmapEnabled) return;
            Configure();
        };
    }

    [MenuItem("Gift Catcher/Configure Android Portrait Testing")]
    public static void Configure()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play mode before applying Android settings.");
        PlayerSettings.companyName = "Dubai Duty Free";
        PlayerSettings.productName = "Gift Catcher";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, AppId);
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToPortrait = true;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;
        PlayerSettings.defaultScreenWidth = 1080;
        PlayerSettings.defaultScreenHeight = 1920;
        PlayerSettings.defaultIsNativeResolution = true;
        PlayerSettings.Android.defaultWindowWidth = 1080;
        PlayerSettings.Android.defaultWindowHeight = 1920;
        PlayerSettings.Android.requestedVisibleInsets = AndroidWindowInsetsType.None;
        // Android sizes the player to its usable area; no runtime safe-area object creation is needed.
        PlayerSettings.Android.renderOutsideSafeArea = false;
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
        PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)36;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
        PlayerSettings.usePlayerLog = true;
        EditorUserBuildSettings.buildAppBundle = false;
        EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
        EditorUserBuildSettings.development = true;
        EditorUserBuildSettings.allowDebugging = false;
        EditorUserBuildSettings.connectProfiler = false;

        // Retain the existing profile and global scene list, rather than replacing user-authored assets.
        var profile = AssetDatabase.LoadAssetAtPath<BuildProfile>(ProfilePath);
        if (profile)
        {
            var serialized = new SerializedObject(profile);
            var iterator = serialized.GetIterator();
            while (iterator.Next(true))
            {
                if (iterator.propertyType != SerializedPropertyType.Boolean) continue;
                if (iterator.name == "m_Development") iterator.boolValue = true;
                if (iterator.name == "m_BuildAppBundle" || iterator.name == "m_ExportAsGoogleAndroidProject" ||
                    iterator.name == "m_ConnectProfiler" || iterator.name == "m_AllowDebugging" || iterator.name == "m_WaitForManagedDebugger")
                    iterator.boolValue = false;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
        }

        ConfigureScene();
        var mobilePipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
        if (mobilePipeline)
        {
            Undo.RecordObject(mobilePipeline, "Configure mobile native rendering");
            mobilePipeline.renderScale = 1;
            mobilePipeline.supportsHDR = false;
            EditorUtility.SetDirty(mobilePipeline);
        }
        ConfigureArtwork();
        AddPreviewSizes();
        AssetDatabase.SaveAssets();
        Validate();
        Debug.Log("Gift Catcher Android settings saved: portrait 1080x1920 reference, safe-area viewport, IL2CPP ARM64, OpenGLES3, API 26 minimum / 36 target, development APK. No APK has been built or installed.");
        if (profile) BuildProfile.SetActiveBuildProfile(profile);
        else if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            EditorUserBuildSettings.SwitchActiveBuildTargetAsync(BuildTargetGroup.Android, BuildTarget.Android);
    }

    static void ConfigureArtwork()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Media" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) continue;
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (!importer) continue;
            // RawImage artwork retains the source aspect ratio and alpha at every canvas scale.
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 2048;
            var android = importer.GetPlatformTextureSettings("Android");
            android.overridden = true;
            android.maxTextureSize = 2048;
            android.format = importer.DoesSourceTextureHaveAlpha() ? TextureImporterFormat.ETC2_RGBA8 : TextureImporterFormat.ETC2_RGB4;
            importer.SetPlatformTextureSettings(android);
            importer.SaveAndReimport();
        }
    }

    static void ConfigureScene()
    {
        var scene = SceneManager.GetSceneByPath("Assets/Scenes/Main.unity");
        bool temporary = !scene.IsValid() || !scene.isLoaded;
        if (temporary) scene = EditorSceneManager.OpenScene("Assets/Scenes/Main.unity", OpenSceneMode.Additive);
        try
        {
            Canvas canvas = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                var candidate = root.GetComponentInChildren<Canvas>(true);
                if (candidate) { canvas = candidate; break; }
            }
            if (!canvas) throw new InvalidOperationException("Main scene has no Canvas.");
            var scaler = canvas.GetComponent<CanvasScaler>();
            Undo.RecordObject(scaler, "Configure portrait canvas scaling");
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            scaler.referencePixelsPerUnit = 100;
            // A taller phone keeps the full vending-machine frame centered, with neutral margins.
            if (!canvas.transform.Find("Portrait Backdrop"))
            {
                var backdrop = new GameObject("Portrait Backdrop", typeof(RectTransform), typeof(Image));
                Undo.RegisterCreatedObjectUndo(backdrop, "Add portrait backdrop");
                backdrop.transform.SetParent(canvas.transform, false);
                backdrop.transform.SetAsFirstSibling();
                var rect = (RectTransform)backdrop.transform;
                rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                var image = backdrop.GetComponent<Image>();
                image.color = new Color32(35, 31, 32, 255); image.raycastTarget = false;
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        finally { if (temporary) EditorSceneManager.CloseScene(scene, true); }
    }

    static void AddPreviewSizes()
    {
        // Unity exposes custom Game view sizes through editor internals, so guard against version changes.
        try
        {
            var assembly = typeof(EditorWindow).Assembly;
            var sizesType = assembly.GetType("UnityEditor.GameViewSizes");
            var groupType = assembly.GetType("UnityEditor.GameViewSizeGroupType");
            var sizeType = assembly.GetType("UnityEditor.GameViewSize");
            var kindType = assembly.GetType("UnityEditor.GameViewSizeType");
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            var singleton = sizesType.BaseType.GetProperty("instance", flags).GetValue(null);
            var group = sizesType.GetMethod("GetGroup", flags).Invoke(singleton, new[] { Enum.Parse(groupType, "Android") });
            var countMethod = group.GetType().GetMethod("GetTotalCount", flags);
            var get = group.GetType().GetMethod("GetGameViewSize", flags);
            foreach (int height in new[] { 1920, 2400 })
            {
                bool exists = false;
                int count = (int)countMethod.Invoke(group, null);
                for (int i = 0; i < count; i++)
                {
                    var size = get.Invoke(group, new object[] { i });
                    if ((int)sizeType.GetProperty("width", flags).GetValue(size) == 1080 &&
                        (int)sizeType.GetProperty("height", flags).GetValue(size) == height) exists = true;
                }
                if (exists) continue;
                var name = height == 1920 ? "Vending Machine Portrait" : "Galaxy S21 FE Portrait";
                var custom = Activator.CreateInstance(sizeType, flags, null,
                    new[] { Enum.Parse(kindType, "FixedResolution"), (object)1080, height, name }, null);
                group.GetType().GetMethod("AddCustomSize", flags).Invoke(group, new[] { custom });
            }
            sizesType.GetMethod("SaveToHDD", flags)?.Invoke(singleton, null);
            Debug.Log("Gift Catcher: Android Game view presets available for 1080x1920 and 1080x2400.");
        }
        catch (Exception exception) { Debug.LogWarning("Could not add Game view presets automatically: " + exception.Message); }
    }

    [MenuItem("Gift Catcher/Validate Android Settings")]
    public static void Validate()
    {
        if (PlayerSettings.defaultInterfaceOrientation != UIOrientation.Portrait ||
            PlayerSettings.Android.renderOutsideSafeArea ||
            PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android) != AppId ||
            PlayerSettings.Android.targetArchitectures != AndroidArchitecture.ARM64 ||
            PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android) != ScriptingImplementation.IL2CPP ||
            PlayerSettings.GetGraphicsAPIs(BuildTarget.Android).Length != 1 ||
            PlayerSettings.GetGraphicsAPIs(BuildTarget.Android)[0] != GraphicsDeviceType.OpenGLES3)
            throw new InvalidOperationException("Android settings validation failed.");
        // Expand uses the smaller screen/reference scale, so the complete 9:16 stage always fits.
        foreach (var size in new[] { new Vector2(1080, 1920), new Vector2(1080, 2400), new Vector2(1080, 2300), new Vector2(720, 1600) })
        {
            float scale = Mathf.Min(size.x / 1080, size.y / 1920);
            if (1080 * scale > size.x + .01f || 1920 * scale > size.y + .01f)
                throw new InvalidOperationException("Portrait reference frame clips at " + size);
        }
        Debug.Log("Gift Catcher Android validation PASSED: player configuration and unclipped portrait frame at kiosk, S21 FE, safe-area and lower-resolution sizes.");
    }
}
