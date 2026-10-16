using System.IO;
using DDF.GiftCatcher;
using TMPro;
using UnityEditor;

public static class GiftCatcherVendingSetup
{
    [MenuItem("Gift Catcher/Update Legacy Vending Migration Defaults")]
    public static void Wire()
    {
        if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode) return;
        const string defaults = "Assets/Settings/VendingDefaults.txt";
        string text = File.ReadAllText("Assets/StreamingAssets/settings.ini");
        if (!File.Exists(defaults) || File.ReadAllText(defaults) != text)
        { File.WriteAllText(defaults, text); AssetDatabase.ImportAsset(defaults); }
        var root = PrefabUtility.LoadPrefabContents("Assets/Prefabs/GiftCatcher.prefab");
        try
        {
            var game = root.GetComponent<GiftCatcherGame>();
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.TextAsset>(defaults);
            var label = root.transform.Find("Result/Demo Label").GetComponent<TMP_Text>();
            if (game.vendingDefaults == asset && game.vendingStatusLabel == label) return;
            game.vendingDefaults = asset; game.vendingStatusLabel = label;
            PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/GiftCatcher.prefab");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
}
