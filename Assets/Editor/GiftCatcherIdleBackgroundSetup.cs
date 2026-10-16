using DDF.GiftCatcher;
using UnityEditor;
using UnityEngine;

public static class GiftCatcherIdleBackgroundSetup
{
    const string PrefabPath = "Assets/Prefabs/GiftCatcher.prefab";

    [InitializeOnLoadMethod]
    static void OnImport()
    {
        EditorApplication.delayCall += Wire;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall += Wire;
        };
    }

    [MenuItem("Gift Catcher/Configure Idle Screen")]
    static void Wire()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        { EditorApplication.delayCall += Wire; return; }
        if (EditorApplication.isPlayingOrWillChangePlaymode || !AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath)) return;
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            if (Configure(root.GetComponent<GiftCatcherGame>())) PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    public static bool Configure(GiftCatcherGame game)
    {
        bool idleChanged = GiftCatcherIdleScreenSetup.Configure(game);
        bool instructionsChanged = GiftCatcherInstructionsSetup.Configure(game);
        return idleChanged || instructionsChanged;
    }
}
