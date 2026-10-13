using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Game view zoom is an editor preference; this file is excluded from the Android player.
[InitializeOnLoad]
public static class GiftCatcherGameView
{
    const string Menu = "Gift Catcher/Lock Game View to 1x";
    const string Key = "DDF.GiftCatcher.LockGameViewZoom";
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static readonly Type ViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
    static double nextCheck;
    static bool warned;
    static bool confirmed;

    static GiftCatcherGameView()
    {
        EditorApplication.update += Apply;
        EditorApplication.delayCall += Apply;
    }

    [MenuItem(Menu)]
    static void Toggle() { EditorPrefs.SetBool(Key, !EditorPrefs.GetBool(Key, true)); nextCheck = 0; Apply(); }
    [MenuItem(Menu, true)]
    static bool Check() { UnityEditor.Menu.SetChecked(Menu, EditorPrefs.GetBool(Key, true)); return true; }

    static void Apply()
    {
        if (!EditorPrefs.GetBool(Key, true) || ViewType == null || EditorApplication.timeSinceStartup < nextCheck) return;
        nextCheck = EditorApplication.timeSinceStartup + .25;
        try
        {
            foreach (EditorWindow view in Resources.FindObjectsOfTypeAll(ViewType))
            {
                var zoom = ViewType.GetField("m_ZoomArea", Flags)?.GetValue(view);
                if (zoom == null) continue;
                var scale = zoom.GetType().GetProperty("scale", Flags);
                if (scale == null || !(scale.GetValue(zoom) is Vector2 current)) continue;
                if (Mathf.Approximately(current.x, 1) && Mathf.Approximately(current.y, 1)) continue;
                // Reset the editor's zoom transform, keeping it centered on the authored portrait frame.
                var transform = zoom.GetType().GetMethod("SetTransform", Flags, null, new[] { typeof(Vector2), typeof(Vector2) }, null);
                if (transform == null) continue;
                transform.Invoke(zoom, new object[] { Vector2.zero, Vector2.one });
                ViewType.GetMethod("EnforceZoomAreaConstraints", Flags)?.Invoke(view, null);
                view.Repaint();
                if (!confirmed && scale.GetValue(zoom) is Vector2 applied && Mathf.Approximately(applied.y, 1))
                {
                    confirmed = true;
                    Debug.Log("Gift Catcher: Game view zoom verified at 1x. Toggle the lock under the Gift Catcher menu.");
                }
            }
        }
        catch (Exception exception)
        {
            if (warned) return;
            warned = true;
            Debug.LogWarning("Game view zoom lock is unavailable in this editor: " + exception.Message);
        }
    }
}
