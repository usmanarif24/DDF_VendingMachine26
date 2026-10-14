# Dubai Duty Free — Gift Catcher

A Unity touchscreen game for an exhibition vending machine, with Windows desktop testing and an Android deployment profile. Visitors drag the shopping bag to catch falling gifts. Catching **10 gifts** wins a physical prize; **dispensing is currently a placeholder** for the vending integration team.

The development APK has been built successfully and the game was confirmed working by the project owner on a Samsung Galaxy S21 FE. Physical dispensing has not been implemented or tested.

## Get started

1. Clone the repository and add the project folder to Unity Hub.
2. Open with **Unity 6000.6.5f1**. Install **Android Build Support**, including Unity's bundled SDK, NDK and OpenJDK, through Unity Hub.
3. Let Unity import the project and restore the packages listed in `Packages/manifest.json`.
4. Open **`Assets/Scenes/Main.unity`** and press **Play**.
5. The project is currently configured for **Windows x64** testing. Use **Gift Catcher → Configure Windows Testing** to switch to Windows, or select the existing **Android** profile in **File → Build Profiles** for phone builds.

The project uses URP, Unity UI, TextMesh Pro, and the new Input System. The main Canvas already has an EventSystem with `InputSystemUIInputModule`. Keep Unity `.meta` files alongside their assets to preserve scene and prefab references.

## Game and exhibition flow

| Rule | Current behavior |
| --- | --- |
| Win condition | 10 gifts; each gift counts as one, regardless of artwork |
| Round length | 60 seconds after the opening countdown |
| Lives | 3 |
| Rocks | Catching a rock removes one life |
| Missed gifts | No life penalty |
| Round end | Immediate win at 10 gifts, loss at zero lives, or timeout |
| Controls | Drag anywhere in the playfield; mouse dragging and arrow keys/A/D also work in the editor |
| Countdown | 3 seconds followed by a brief GO display |
| Instructions idle reset | Return to welcome after 30 seconds |
| Results reset | Return to welcome after 12 seconds |
| Focus loss/backgrounding | Active gameplay/countdown pauses and requires Resume |
| Display | 1080 × 1920 portrait reference; the full frame stays centered on taller displays |

The game targets 60 FPS and prevents screen sleep while running. These application settings do **not** establish Android kiosk mode or prevent visitors from leaving the app; device management is a separate deployment task.

## Project map

| Path | Purpose |
| --- | --- |
| `Assets/Scenes/Main.unity` | Build scene, Canvas, EventSystem and game prefab instance |
| `Assets/Prefabs/GiftCatcher.prefab` | Saved welcome, instructions, gameplay, result and pause screens; bag and 20 falling-item objects |
| `Assets/Scripts/GiftCatcher/GiftCatcherGame.cs` | Round state, timer, spawning, catches, lives, results and dispensing placeholder |
| `Assets/Scripts/GiftCatcher/GiftCatcherInput.cs` | Single-pointer touch/mouse dragging and movement bounds |
| `Assets/Scripts/GiftCatcher/GiftCatcherItem.cs` | References and per-item state for the authored object pool |
| `Assets/Media/Backgrounds`, `Items`, `UI` | Extracted PNG artwork and original SVG UI artwork |
| `Assets/Media/Source` | Original embedded WebP artwork |
| `Assets/Editor` | Scene authoring, configuration, APK build and validation tools; excluded from Android runtime |
| `Gift-Catcher-game.html` | Original standalone design/gameplay reference |
| `Assets/StreamingAssets/settings.ini` | Existing boilerplate configuration; no vending transport is wired to it |

All gameplay UI and falling-item objects are saved editor objects. Runtime code changes their state, textures, positions and visibility; it does not construct the UI or instantiate a new object for each falling gift. The HTML reference uses different rules (45 seconds and weighted points); **the Unity game rules above are authoritative**.

Select the **GiftCatcher** prefab instance in the scene to edit round rules and serialized references in the Inspector. Apply intended prefab changes when they should become the shared prefab defaults.

## Vending dispensing handover

The integration entry point is **`GiftCatcherGame.Finish(bool won)`** in `Assets/Scripts/GiftCatcher/GiftCatcherGame.cs`:

```csharp
if (won) Debug.Log("dispensing gift");
```

This executes when the visitor catches the tenth gift. The round transitions to the result screen before the log is emitted. Normal gameplay stops updating in the result state, so additional frames do not emit another request. Losing rounds do not emit the log. **There is no hardware call, connection status, acknowledgement, round ID, retry handling, or dispense completion tracking yet.**

The winning UI currently displays “Dispensing your gift…” and collection instructions, with a demo label. This is placeholder copy and does not confirm a physical gift has been delivered.

The vending team should:

1. Replace the log with a call into a dedicated vending controller/service after agreeing the vendor protocol and Android permissions. The transport may be serial, USB, network, or a vendor SDK; no transport is assumed by this project.
2. Issue one request per winning round and add a unique round/request ID. Use the vendor's deduplication support or equivalent bookkeeping so a reconnect, retry, or app restart cannot dispense twice for the same win.
3. Keep communications asynchronous. Marshal UI changes back onto Unity's main thread; do not block the game thread while waiting for the machine.
4. Add pending, confirmed-success, timeout, and error states to the authored result screen. Show collection instructions after the appropriate machine acknowledgement. Provide a staff-help message for errors and avoid unattended duplicate retries.
5. **Gate the 12-second result auto-return while a dispense is pending.** The current `Update` result branch counts down immediately after a win. Define when the countdown should start following success/failure, and keep a pending request associated with its original round even if the app backgrounds.
6. Define machine-ready/stock checks, fault recovery, collection detection if supported, and the next-visitor reset policy. Prevent a new prize attempt while the machine is unavailable or an earlier dispense is unresolved.
7. Test actual hardware for one dispense per win, no dispense on loss/timeout, disconnection, empty stock, delayed acknowledgement, errors, duplicate responses, background/resume, and application restart.

The current frontend rule validation checks the console placeholder. Update `GiftCatcherValidation` to use a fake vending service when replacing that placeholder; the existing test expects exactly one `dispensing gift` log for a win.

## Windows configuration and building

The active target is **Windows x64**, using the built-in Windows build profile. Android's profile and player settings are retained for phone and vending-machine deployment.

| Setting | Value |
| --- | --- |
| Scripting / architecture | Mono / Windows x64 |
| Graphics | Direct3D 11 |
| Display | Fullscreen Window at the monitor's native resolution |
| Layout | 1080 × 1920 reference; Scale With Screen Size → Expand |
| Controls | Mouse/touch dragging; arrow keys or A/D |
| Build type | Development build |

On a portrait 1080 × 1920 monitor, the authored frame fills the screen. On a landscape desktop monitor, the game remains centered with dark side margins. Windows does not rotate the monitor; set its display orientation in Windows if testing a physical portrait screen. The canvas scaling changes neither the saved object layout nor the artwork proportions.

Use **Gift Catcher → Validate and Build Windows** while Windows is active. This runs the round-rule checks and creates:

```text
Builds/Windows/GiftCatcher.exe
```

Keep the executable alongside its generated data folder, `UnityPlayer.dll`, and other build files. Copy the **entire `Builds/Windows` folder** when sharing a Windows build. For a smaller desktop test window, launch with `-screen-fullscreen 0 -screen-width 540 -screen-height 960`; the UI still uses the 1080 × 1920 reference.

Switching targets is an editor/workspace setting, stored in Unity's local project state rather than as a portable Git branch setting. Teammates can select Windows in Build Profiles or run **Configure Windows Testing** after cloning. No editor bootstrap forces a correctly configured project back to Android.

## Android configuration and building

| Setting | Value |
| --- | --- |
| Application ID | `com.ddf.giftcatcher` |
| App name / version | Gift Catcher / 0.1.0 (version code 1) |
| Orientation | Portrait; landscape and upside-down autorotation disabled |
| Canvas | Scale With Screen Size, 1080 × 1920 reference, Expand |
| Phone safe area | Rendering outside the safe area disabled |
| System bars | Hidden at launch via requested visible insets |
| Scripting / architecture | IL2CPP / ARM64 |
| Graphics | OpenGL ES 3 |
| Minimum / target SDK | API 26 / API 36 |
| Build format | Development APK; app bundle and Gradle project export disabled |
| Mobile rendering | Native render scale; HDR disabled |
| Raster artwork | Original proportions, no mipmaps, ETC2 Android compression |

Android renders at the device's native usable resolution; **1080 × 1920 is the layout reference**, not a forced phone framebuffer resolution. Dark margins preserve the exhibition framing on taller screens.

First activate the saved **Android** build profile, or use **Gift Catcher → Configure Android Portrait Testing** to switch and reapply the phone-test settings. Once the platform switch finishes, use **Gift Catcher → Build Android APK** to create:

```text
Builds/Android/GiftCatcher-dev.apk
```

Alternatively, use **Build** in the Android Build Profile. Copy the APK to the phone and open it to install, allowing installation from the file-opening app when prompted. **Build and Run** requires an ADB-authorized device; a manual APK installation does not.

`Builds`, generated Unity caches and APK files are excluded from Git. Build locally from the checked-in scene, prefab, scripts and configuration. Development builds use the local Android debug signing setup; arrange a shared release keystore and production build settings separately for deployment. Different signing keys can prevent an existing installation from being updated.

### Editor tools

| Gift Catcher menu item | Purpose |
| --- | --- |
| Build First Iteration | Authors the initial scene/prefab if it is missing; does not rebuild an existing game hierarchy |
| Configure Android Portrait Testing | Reapplies the documented Android test settings, artwork import settings and preview presets |
| Configure Windows Testing | Activates Windows x64 and sets the desktop player/canvas settings; retains the Android profile |
| Validate and Build Windows | Runs the round-rule checks and builds the Windows development player |
| Build Android APK | Builds the development APK without attempting device deployment |
| Validate Round Rules | Exercises an isolated copy of the saved prefab without altering Main or entering Play mode |
| Validate Android Settings | Checks Android player settings and portrait-frame fit |
| Lock Game View to 1x | Toggles an editor-only preview zoom lock; disable to choose zoom freely |

The scene authoring/configuration tools include first-import bootstrap callbacks. With the existing prefab and configured app ID/artwork, normal imports do not recreate the game. The zoom lock defaults to enabled unless disabled in local editor preferences. Preview-size and zoom helpers use Unity editor internals, so recheck them after upgrading Unity.

## Validation and known build compatibility details

- Automated editor checks passed for initialization, drag limits, pause, missed gifts, ten-gift win, three-rock loss, 60-second timeout, one dispensing log per win, and exhibition resets.
- Portrait-fit/configuration checks passed for the 1080 × 1920 frame, taller phone sizes, a reduced safe-area viewport and a lower-resolution viewport.
- A development ARM64 APK built successfully and its signature was verified. The owner confirmed it runs on the S21 FE. Actual exhibition hardware, unattended operation and physical dispensing remain to be tested.
- On 14 October 2026, the project was switched back to Windows x64, the round-rule checks passed under the Windows editor target, and a development Windows player built successfully. An eight-second headless startup smoke check completed without logged runtime exceptions. Interactive Windows rendering and mouse/touch operation still need a visual playtest; the headless check does not verify graphics.
- `Assets/AVProVideo/Editor/Scripts/PostProcessBuild_Android.cs` contains two required compatibility fixes for the bundled AVPro version: Unity 6 does not receive the removed `android.enableDexingArtifactTransform` option, and the standalone `com.google.guava:listenablefuture` dependency is excluded when AVPro's full Guava JAR is present. Preserve or reevaluate these fixes when updating AVPro.
- The build reports 16 KB alignment warnings for bundled AVPro ARM64 native libraries. AVPro is not used by the current game screens. Before production deployment on devices requiring 16 KB compatibility, update the plugin or assess excluding its unused Android libraries, then rebuild and test the target machine.

For build failures, inspect Unity's Console and the project-relative **`Logs/Editor.log`**. The Android build helper reports `Gift Catcher APK BUILD SUCCEEDED` or `Gift Catcher APK BUILD FAILED` after completion. Windows batch validation/build output is in **`Logs/WindowsValidation.log`**, and the standalone startup smoke output is in **`Logs/WindowsPlayerSmoke.log`**. Logs are local generated files and are not committed.
