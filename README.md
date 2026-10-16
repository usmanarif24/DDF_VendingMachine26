# Dubai Duty Free — Gift Catcher

A Unity touchscreen game for an exhibition vending machine, with Windows desktop testing and an Android deployment profile. Visitors drag the shopping bag to catch falling gifts. Catching **10 gifts** wins a physical prize; **Windows Arduino dispensing is implemented** and requires hardware commissioning.

The development APK has been built successfully and the game was confirmed working by the project owner on a Samsung Galaxy S21 FE. Physical dispensing has not yet been tested against the machine.

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
| `Assets/Scripts/GiftCatcher/GiftCatcherGame.cs` | Round state, timer, spawning, catches, lives, results and Arduino dispensing |
| `Assets/Scripts/GiftCatcher/GiftCatcherInput.cs` | Single-pointer touch/mouse dragging and movement bounds |
| `Assets/Scripts/GiftCatcher/GiftCatcherItem.cs` | References and per-item state for the authored object pool |
| `Assets/Media/Backgrounds`, `Items`, `UI` | Extracted PNG artwork and original SVG UI artwork |
| `Assets/Media/Source` | Original embedded WebP artwork |
| `Assets/Editor` | Scene authoring, configuration, APK build and validation tools; excluded from Android runtime |
| `Gift-Catcher-game.html` | Original standalone design/gameplay reference |
| `Assets/Scripts/GiftCatcher/OperatorSettings.cs` | PlayerPrefs configuration, validation and one-time legacy INI migration |
| `Assets/Scripts/GiftCatcher/GiftCatcherOperatorPanel.cs` | In-app settings, refill, recovery and Save and Restart |
| `Assets/StreamingAssets/settings.ini` | Legacy settings imported once on installations without saved preferences |

All gameplay UI and falling-item objects are saved editor objects. Runtime code changes their state, textures, positions and visibility; it does not construct the UI or instantiate a new object for each falling gift. The HTML reference uses different rules (45 seconds and weighted points); **the Unity game rules above are authoritative**.

The seven supplied PNGs from `GRAPHICS FOR GIFT CATCHER.zip` are now used in the game: the DDF yellow open bag and six falling merchandise types — teddy, perfume, chocolate, headphones, neck pillow and water bottle. The old gift box, watch, sunglasses and suitcase are excluded from spawning. Rocks remain the obstacle. Bag/teddy screen placements preserve the supplied proportions, redundant bag text overlays are hidden, and the catch edge follows the new bag opening. Gift-bottom offsets reflect the supplied artwork's alpha bounds. Original PNG pixels and existing asset GUIDs are preserved; the two new gifts use the same import settings as the existing artwork.

The welcome/idle screen uses `Assets/Media/Backgrounds/idle-depth-room.png`: the approved clean still of the teddy seated inside a red-and-pearl alcove. The logo and prompt are not baked into this image. The separate teddy overlay and the previous ribbon VideoPlayer are removed from the active idle presentation. Other game screens retain their existing background.

The official black DDF wordmark and **TOUCH TO START** are native Unity UI above the still. `GiftCatcherIdleScreen` uses DOTween for a staggered fade/slide entrance, subtle logo drift and gentle prompt opacity/scale pulses. Tweens pause behind the operator panel, are killed when leaving idle, and restore the authored layout before the next visit. The old welcome cards, headings, bag, gift box and rule copy remain hidden. The transparent start button fills the screen, with the operator button accessible above it. `GiftCatcherIdleBackgroundSetup` / `GiftCatcherIdleScreenSetup` maintain the saved prefab layout; no runtime UI is constructed.

Instructions use the matching pearl, wine-red and champagne-gold palette, the official DDF wordmark and three concise icon/text rows: catch gifts, avoid rocks and win your surprise. The bag and teddy are the supplied merchandise artwork. The old brand header and bordered cards are hidden, with a single **LET’S PLAY** button below the steps. Win target, time and lives continue to follow operator settings. `GiftCatcherInstructionsSetup` authors this saved layout alongside the idle setup.

Select the **GiftCatcher** prefab instance in the scene to edit round rules and serialized references in the Inspector. Apply intended prefab changes when they should become the shared prefab defaults.

## Arduino vending integration

### In-app operator panel

Tap **OPERATOR** at the top right of the game, or press **F10**, to open the operator panel. It is available on the welcome, instructions, gameplay, paused and fault/result screens. Opening it freezes game timers and bag input; Close/F10 discards unsaved edits and returns to the previous screen. Access is disabled while a dispense is pending. This is a staff convenience panel without a PIN or authentication.

The panel manages win target, lives, round duration, result/instructions return delays, hardware/demo mode, Space-key debug dispensing, COM port, active trays, initial tray cycles, inventory revision, startup/reply timing, DTR and RTS. Current remaining cycles and machine faults appear at the top. Use the scrollable settings area to reach all controls.

**Test Dispense** runs one dispense directly from the operator panel, using the next motor/turn and the active saved settings. It does not require playing a round or enabling Space-key debug mode. Successful hardware completion consumes and saves one turn using the same inventory/journal flow as a win; demo mode advances only demo stock. The panel remains open and displays the outcome and updated progress. Close, Save and Restart, Machine Refilled and further test presses are disabled while it waits for completion. Empty stock and unresolved faults block testing. Apply unsaved edits with Save and Restart before testing.

If physical vending is enabled but the configured COM port is missing at startup/Save and Restart, the app automatically uses **simulation** for that session. The panel identifies the missing port, Test Dispense and game wins advance saved demo turns, and no serial connection is opened or hardware stock consumed. Machine Refilled resets the active simulated stock too. The saved physical-vending preference and COM port remain unchanged. Connect the machine and use Save and Restart to detect it and resume the separate hardware inventory. A port that is present but fails to open, a disconnect during a request, or an unresolved journal still requires recovery; these failures are not treated as successful dispenses.

**Save and Restart** validates and saves settings to **PlayerPrefs**, closes the idle serial service and reloads Main inside the running app. It discards the current round; it does not quit the application or launch another process. Configuration is stored as one versioned JSON value under `DDF.GiftCatcher.OperatorSettings.v1`, followed by `PlayerPrefs.Save()`. All subsequent launches load these preferences. Closing the panel without saving preserves the previous settings.

On the first launch without this preference, the previous persistent INI, packaged INI, or bundled legacy defaults are imported once, in that order. Existing COM port and stock configuration are preserved. INI files are no longer read or written after migration; use the operator panel for all changes. Inventory counts and the unresolved-request journal remain separate durable files, so changing preferences does not erase dispensing history or bypass recovery. Editor and standalone PlayerPrefs belong to their respective application environments.

Set the active tray count and each tray's **turns when full**. After physically filling the coils, press **Machine Refilled**. This saves all panel settings, resets stock and restarts the game immediately. With four trays of three turns, dispensing follows motor 1 turns 1–3, then motor 2 turns 1–3, motor 3 turns 1–3, motor 4 turns 1–3, then shows Machine Empty. A zero-capacity tray is skipped. The internal inventory revision is managed by this button and is no longer a staff-editable field.

Normal **Save and Restart**, closing the app and reopening it preserve the saved progress. Changing the configured full capacity only affects stock on the next Machine Refilled action; it does not add turns to the current load. After changing the number of active trays, use Machine Refilled to start the new layout. The panel shows the next motor/turn and completed/remaining turns for each tray. Full capacities are saved alongside remaining counts, so changing refill settings does not change the displayed progress of the current load. Existing three-line inventory files are upgraded without resetting remaining counts. Hardware and demo stock remain separate.

Each acknowledged motor cycle is one logical coil turn. Progress is flushed to disk after each acknowledged completion, before the pending request is cleared. Refill generations are persisted immediately, even before the first dispense. If the app stops mid-command before completion can be confirmed, the unresolved-request journal blocks further vending until staff reconcile that dispense. The firmware cannot determine a partial physical turn or confirm delivery, so the app does not automatically resend an uncertain command. Machine Refilled requires the recovery acknowledgement when such a journal exists.

When an unresolved dispense journal exists, the panel offers an acknowledgement that staff inspected the motor/tray and reconciled the last prize. Selecting it clears that journal only when saving. Leaving it unchecked preserves the lock across the scene restart. No motor command is sent by opening, saving, refilling or clearing recovery. Real dispensing remains Windows-only.

The operator UI is saved in the prefab. Settings rows are anchored to the top of its scroll content. `GiftCatcherOperatorSetup` authors a missing panel or repairs the first revision's row anchors on normal script import; **Gift Catcher → Add Operator Panel** can also perform this repair. The setup preserves existing controls. Runtime opening also repairs older anchors and scrolls to the first setting. Gameplay and vending validations run only through their menu/build commands, not automatically on script import.

Unity now implements the command/reply protocol in the supplied `VendingMachine_Targetted_1-4.ino`. The Arduino file is not changed, uploaded or copied into this repository.

**Hardware transport is Windows COM serial only.** Android builds can still run in demo mode; real Android dispensing requires a native USB serial bridge implementing `IVendingConnection`. The desktop transport does not work on Android.

### Configure the machine

Open the in-app operator panel, edit the following fields, then select **Save and Restart**. The following legacy INI names describe their corresponding panel settings; editing an INI does not change saved PlayerPrefs.

```ini
[vending]
enabled = 0
port = COM3
num_motors = 4
motor_1_turns = 3
motor_2_turns = 3
motor_3_turns = 3
motor_4_turns = 3
inventory_revision = 1
startup_delay_ms = 2000
response_timeout_ms = 6000
dtr = 1
rts = 0
```

- `enabled=0`: demo mode. No serial connection is opened, and the result explicitly says no physical gift was dispensed. Set `enabled=1` for hardware after choosing the actual Arduino port.
- `port`: the Arduino COM port from Windows Device Manager; COM3 is a placeholder. Close Arduino Serial Monitor and other programs using that port.
- `num_motors`: active springs/trays (1–4, matching the unchanged firmware).
- `motor_N_turns`: available acknowledged cycles for that tray. One win or debug request consumes one cycle; motor 1 with 3 turns is used for three requests before switching to motor 2. Zero-count trays are skipped. These counts do not instruct three cycles on one win or measure physical revolutions.
- `inventory_revision`: internal stock generation, advanced by Machine Refilled. Normal settings saves preserve it. An unresolved request still requires reconciliation.
- `startup_delay_ms`: wait after opening the port, allowing Arduino boards that reset on open to finish setup (0–10000 ms).
- `response_timeout_ms`: total acknowledgement deadline after the startup wait (4000–60000 ms). The firmware runs a motor for up to 3 seconds, so the default is 6000 ms.
- `dtr` / `rts`: serial control-line state (0 or 1), configurable for the connected board.

Baud rate **9600**, **8N1**, no serial handshake, and newline terminators are fixed to match the firmware.

### Win and acknowledgement flow

`GiftCatcherGame.Finish(true)` starts a `VendingDispenser` request. It logs `dispensing gift`, opens the configured port on a background thread, waits for board startup, then sends `start\n`. Only after receiving `Motor control enabled.` does it send the next nonempty tray number followed by a newline.

The result screen remains pending until `Starting Motor N` and a matching terminal message arrive:

- `Motor N stopped due to LOW signal after 0.15s delay.`
- `Motor N stopped after 3 seconds.`

Replies for another motor, stale terminal messages before a start acknowledgement, and repeated enable replies do not cause another motor command. After acknowledged cycle completion Unity sends `stop\n` to disable new commands and closes the port. It shows **“Dispense cycle complete. Please check the collection tray.”** The normal 12-second result countdown starts only after completion; the visitor cannot return home while a request is pending.

**Firmware limits:** `stop` only disables new motor commands; it does not stop an already-running motor. The firmware itself stops the motor using its LOW-input condition after at least 2 seconds plus 150 ms, or its 3-second timer. Neither serial stop message confirms that a gift physically reached the tray. No physical-delivery guarantee, physical stock detection, emergency-stop command, or request-ID acknowledgement exists in this firmware. Unity tracks a software count of acknowledged cycles; it cannot verify stock or the exact mechanical rotation angle.

### Space-key debug testing

Set **Space debug dispensing** to **1** in the operator panel, select Save and Restart, and focus the Unity Game view. Each **Space press** runs one request through the same result screen and tray-selection logic without needing to win the game. Holding Space does not repeatedly dispense. Presses while the operator panel is open, a request is pending or the machine is faulted are ignored.

With `[vending] enabled=1`, Space sends a real command and consumes one acknowledged inventory cycle. With `enabled=0`, it only simulates completion and advances a **separate demo inventory**. Set `debug=0` before exhibition deployment.

Remaining counts are saved in `vending-inventory.txt` (hardware) or `vending-inventory-demo.txt` (demo) under `Application.persistentDataPath`. Inventory is updated atomically before clearing the pending-request journal, and a duplicate completion ID is not counted twice. A failed or unconfirmed cycle does not decrement the confirmed count and remains subject to staff recovery. Once every tray is empty, the app blocks further dispensing and asks for a refill.

### Failure and recovery

Unity sends a motor command at most once per winning request and never automatically retries it. Before sending that command it saves a round ID and motor in **`vending-pending.txt`** under `Application.persistentDataPath`. The record is removed only after the matching motor-stop reply. Disconnects, missing replies or shutdown can leave the record in place, preventing a new dispense after restarting the application.

Connection/configuration failures or uncertain results show a staff-help screen, block new rounds and stop automatic return. For recovery, exit the app, inspect the board/motor/tray and resolve the fault. If a pending file remains, have a staff member reconcile whether the prize was dispensed, then remove that file before restarting. Do not remove it simply to retry: the firmware cannot deduplicate commands. A crash between saving the record and sending the command intentionally favors staff review over a possible duplicate vend. The journal is local to one app installation; do not operate multiple app instances against the same board.

Unity API/UI changes are processed on the main thread; serial waits do not block frame updates. Gameplay still pauses on focus loss; an active dispense continues to await its Arduino reply while the app runs in the background. Existing UI objects are reused; no runtime screens are constructed.

### Code and testing

- `VendingDispenser.cs`: configuration, exact reply parser, asynchronous serial transaction, recovery journal and `IVendingConnection` transport seam.
- `GiftCatcherGame.cs`: win dispatch, pending/success/demo/failure result UI and reset gating.
- `GiftCatcherVendingSetup.cs`: wires the bundled configuration and existing result status label into the saved prefab.
- **Gift Catcher → Validate Arduino Integration (No Hardware)**: fake-serial checks for all four motors, reply ordering, duplicate suppression, completion, disconnect, acknowledgement timeout, restart lock and disabled-mode isolation; also reruns game rules with hardware forcibly disabled.

Windows uses Unity's **.NET Framework** API compatibility profile for `System.IO.Ports`. Physical hardware has not been tested by these automated checks. Before exhibition deployment test each selected motor, actual sensor timing, disconnect/timeout, app restart with an unresolved request, result gating and manual staff recovery. Android transport, physical inventory sensing and kiosk/device management remain separate integration work.

## Windows configuration and building

The active target is **Windows x64**, using the built-in Windows build profile. Android's profile and player settings are retained for phone and vending-machine deployment.

| Setting | Value |
| --- | --- |
| Scripting / architecture | Mono / Windows x64; .NET Framework API compatibility |
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
| Validate Arduino Integration (No Hardware) | Fake serial protocol, recovery and result-screen checks; never opens the real port |
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
