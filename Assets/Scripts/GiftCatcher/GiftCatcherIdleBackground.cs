using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace DDF.GiftCatcher
{
    // Plays only while the authored Welcome screen is active. The poster also works in edit mode.
    [RequireComponent(typeof(RawImage), typeof(VideoPlayer))]
    public sealed class GiftCatcherIdleBackground : MonoBehaviour
    {
        public RawImage display;
        public VideoPlayer player;
        public Texture2D poster;

        void OnEnable()
        {
            display.texture = poster;
            player.playOnAwake = false;
            player.isLooping = true;
            player.audioOutputMode = VideoAudioOutputMode.None;
            player.renderMode = VideoRenderMode.APIOnly;
            player.waitForFirstFrame = true;
            player.skipOnDrop = true;
            player.source = VideoSource.Url;
            player.url = Path.Combine(Application.streamingAssetsPath, "Idle_Pearl_Ribbons_1080x1920.mp4");
            player.prepareCompleted += Ready;
            player.errorReceived += Failed;
            player.Prepare();
        }

        void Ready(VideoPlayer source) { if (isActiveAndEnabled) source.Play(); }

        void Update()
        {
            if (player.isPrepared && player.texture != null) display.texture = player.texture;
        }

        void Failed(VideoPlayer source, string error)
        {
            source.Stop();
            display.texture = poster;
            Debug.LogWarning("Idle ribbon video unavailable; showing its still background. " + error);
        }

        void OnDisable()
        {
            if (!player) return;
            player.prepareCompleted -= Ready;
            player.errorReceived -= Failed;
            player.Stop();
            if (display) display.texture = poster;
        }
    }
}
