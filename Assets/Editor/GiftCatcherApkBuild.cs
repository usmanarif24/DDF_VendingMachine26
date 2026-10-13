using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class GiftCatcherApkBuild
{
    const string Request = "Temp/GiftCatcherApkBuild.request";

    [InitializeOnLoadMethod]
    static void OnImport() { EditorApplication.delayCall += ProcessRequest; }

    static void ProcessRequest()
    {
        if (!File.Exists(Request)) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || BuildPipeline.isBuildingPlayer ||
            EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += ProcessRequest;
            return;
        }
        File.Delete(Request);
        Build();
    }

    [MenuItem("Gift Catcher/Build Android APK")]
    public static void Build()
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
        {
            Debug.LogError("Switch the active build profile to Android before building the APK.");
            return;
        }
        GiftCatcherAndroidSettings.Validate();
        EditorUserBuildSettings.buildAppBundle = false;
        EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
        Directory.CreateDirectory("Builds/Android");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/Main.unity" },
            locationPathName = "Builds/Android/GiftCatcher-dev.apk",
            target = BuildTarget.Android,
            options = BuildOptions.Development
        });
        if (report.summary.result == BuildResult.Succeeded)
            Debug.Log("Gift Catcher APK BUILD SUCCEEDED: " + Path.GetFullPath("Builds/Android/GiftCatcher-dev.apk") + " (" + report.summary.totalSize + " bytes). Copy to the phone to install; no device deployment was attempted.");
        else
            Debug.LogError("Gift Catcher APK BUILD FAILED: " + report.summary.result + ", " + report.summary.totalErrors + " errors. See the build report and Editor log.");
    }
}
