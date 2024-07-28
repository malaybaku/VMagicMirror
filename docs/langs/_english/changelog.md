---
layout: page
title: Change Log
lang: en
---

# Change Log

<div class="doc-ul" markdown="1">

#### v3.8.2
{: .doc-sec2 }

2024/07/28

* Fixed: Apply additional fix to the issue that key up event is ignored in specific cases (Game Input feature etc.).


#### v3.8.1
{: .doc-sec2 }

2024/06/30

* Fixed: Add the workardound of issue that key up event is ignored for some mode (Game Input feature etc.) .


#### v3.8.0
{: .doc-sec2 }

2024/05/31

* Add: Option for car steering motion, in `Streaming Tab` > `Motion` > `Gamepad Motion` menu. `View` > `Gamepad` checkbox controls car steering object's visibility.


#### v3.7.0
{: .doc-sec2 }

2024/03/31

* Add: Support to run VRM Animation as loop motion. See detail at [Use VRM Animation](../tips/use_vrma).
* Fixed: In v3.6.0, Bloom effect did not work correctly.
* Fixed: Some of valid vrma files were treated as invalid data and failed to load.


#### v3.6.0
{: .doc-sec2 }

2024/02/25

* Add: Outline effect in Streaming tab, to shows outline on the avatar when transparent window is on. Detailed setting is on Settings Windows > "Effect" tab > "Outline" menu.
* Changed: Hot Key can launch up to 40 items of Word to Motion.
* Changed: Desktop color based light adjust feature is no more in Streaming Tab. The feature itself is available in Setting Window > "Effect" tab > "Light" menu.


#### v3.5.1
{: .doc-sec2 }

2024/01/30

* Fix: In Game Input feature, mouse click could not launch VRM animation motion v3.5.0.
* Fix: Game Input action selection state could be invalid when deleting .vrma file in motion folder in v3.5.0.


#### v3.5.0
{: .doc-sec2 }

2023/12/31

* Add: Support VRM Animation(.vrma) motion in [Game Input](../docs/game_input) feature. See detail at [Game Input](../docs/game_input), or [Use VRM Animation](../tips/use_vrma) pages.
* Fix: When loading VRM1.0 model from PC file, the license indication about excessively violence expression was not correct.


#### v3.4.0
{: .doc-sec2 }

2023/10/30

* Add: Experimental support for VRM Animation(.vrma) in Word to Motion feature. See detail at: [Use VRM Animation](../tips/use_vrma)
* Change: Internal UniVRM version update to v0.114.0. This update is for VRM Animation support.
* Fix: v3.3.1 Full Edition's issue that custom motions / accessories were not loaded correctly. This issue was because of Unity version update in previous update, and thus it is version-specific issue.


#### v3.3.1
{: .doc-sec2 }

2023/09/30

* Add: Option in VMC Protocol, to apply raw received bone poses without adjustment.
* Fix: In specific use case VMC Protocol based hand poses were not applied.
* Change: Unity version is changed to 2022.3.7f1.


#### v3.3.0
{: .doc-sec2 }

2023/08/31

* Add: VMC Protocol data receive support. See detail at: [VMC Protocol](../docs/vmc_protocol)


#### v3.2.3
{: .doc-sec2 }

2023/07/30

* Change: Make microphone-based head motion to stop when certain time elapsed after voice input is missing.
* Fix: In arcade stick mode, there were cases that particle appears from incorrect timing and positions. 


#### v3.2.2
{: .doc-sec2 }

2023/06/28

* Change: Update VRoid SDK version to v0.2.0. This update makes VRM1.0 model available from VRoid Hub, and support easier login steps to VRoid Hub.


#### v3.2.1
{: .doc-sec2 }

2023/05/28

* Add: Support Spout output, at setting window > `Window` tab > `Spout`.
* Add: Support Anti-Alias (MSAA, Multisample Anti-Aliasing), from setting window > `Effects` tab > `Image Quality`.


#### v3.2.0
{: .doc-sec2 }

2023/04/29

* Add: In Game Locomotion mode, support keyboard key assigned to action like Crouch, Punch, etc.
* Add: In Game Locomotion mode, add 3rd person view move styles.
* Add: Hot Key action supports to switch body motion style from default / hand down / game input.
* Change: In Game Locomotion mode "Always Run" option has changed to "Run by Default". During this option is on, run action button makes avatar walk instead.
* Fix: Bug in Game Locomotion mode that custom motion makes avatar move too quick on start and end.
* Fix: Bug in app's startup, that loading view disappears too early when automatic VRM load option is enabled.

Known Issue:

* In Game Locomotion mode, there are some cases that avatar keeps standing pose and pose will not change. In this case, see `Streaming` tab > `Motion` > `Body Motion Style` and choose `Default` or `Standing Only`, and then re-choose `Game Locomotion` to recover the avatar's pose.


#### v3.1.1
{: .doc-sec2 }

2023/03/30

* Fix: VRoid Hub collaboration to work correctly.
* Fix: "See More..." button in game input key assign window work correctly.


#### v3.1.0
{: .doc-sec2 }

2023/03/30

* Add: [Game Input](../docs/game_input) mode, in that avatar moves like in-game character. This mode can be enabled at Streaming Tab > Motion > Body Motion Style and choose "Game Input".
* Add: Option to launch VMagicMirror with control panel window minimized. The option is in Home tab > Other Preferenfces.
* Fix: Eye bone odd behavior in VRM 1.0 model, when bone coordinate does not align to world XYZ axis.


#### v3.0.2
{: .doc-sec2 }

2023/01/28

* Add: Pose edit for hand down pose applied when "Always-Hands-Down" mode etc, in Setting Window > "Motion" tab > "Body" menu.
* Fix: Error dialog appear bug, when VRM does not have thumbnail data.
* Fix: Accessory with Word to Motion disappears soon, when "Keep Facial after motion" option is turned on.
* Fix: In v3.0.0~v3.0.1, LipSync BlendShape (e.g. "Aa", "Ou") was not available in word to motion feature.
* Fix: In v3.0.1, there were cases that glb data accessory's animation did not start correctly.
* Removed: Help page about facial tracking app "MeowFace", which is available on Google Play for Android. Please see why I have removed it from [this issue](https://github.com/malaybaku/VMagicMirror/issues/886).

Known issue:

* "Always-Hands-Down" gizmo appears on drifted position after changing avatar. In this case, move the gizmo makes avatara's hand align to the gizmo.


#### v3.0.1
{: .doc-sec2 }

2022/12/25

* Add: Numbered png accessory supports "Blink Effect" feature, which runs just after blinking. 
* Add: Resolution limit option to image file based accessory.
* Fix: Decrease light intensity, to make the look similar to v2.0.11 with VRM1.0 based shading.
* Fix: Bad arm pose issue happend in v3.0.0.
* Fix: Optimize facial expression related setting data, to prevent app crash.
* Fix: Reduced memory usage related to accessory image data.


#### v3.0.0
{: .doc-sec2 }

2022/11/30

* Add: Support for VRM 1.0 model. VRM 0.x models are still available in this version.

Known issues on VRM 1.0 support

* Hand position will be incorrect on first arm motion (keyboard typing, mouse pad control, etc.)
* With some avatars, upper arm and elbow appearance get very bad.

In the case of the latter issue, please try to open Setting Window > Motion > Arm, and set `Strength to keep upper arm to body[%]` value to zero.

If you find some critical issues about this version, please downgrade to v2.0.11.

#### v2.0.11
{: .doc-sec2 }

2022/10/18

* Fix: In v2.0.8 - v2.0.10, facial expression get chattered after running Word to Motion item, if "Neutral" blendshape is set in facial setting.
* Fix: In v2.0.10 "Keep facials after motion" in Word to Motion feature did not work correctly.


#### v2.0.10
{: .doc-sec2 }

2022/09/30

* Fix: Word to Motion was triggered even by the unselected device.

#### v2.0.9
{: .doc-sec2 }

2022/09/30

* Add: Loading UI, which is visible until app read config data.
* Change: Eye look at motion. In this version eye motion is similar to v2.0.4 or older version.
* Change: Foot IK control, to get legs downward.
* Change: Environment light color become not constant color, and determined based on light color setting.
* Fix: When running motion, it gets less avatar pose jump. For this fix Word to Motion system have been refactored in many part.
* Add: Show error message when tried to load VRM 1.0. The updated to support VRM 1.0 will be released in 2022.


#### v2.0.8
{: .doc-sec2 }

2022/08/31

* Add: BlendShape based eye motion supported.
* Add: (Experimental) Bone FPS reduction. Try from settings window > "Effects" tab > "Quality". Note that this option does NOT reduce CPU usage.
* Add: "Move eyes during facial expression applied" option in setting window > "Face" tab > "Eye".
* Change: Mic based lipsync reflects input volume.
* Change: Adjust eye motion, for the case not using iOS based tracking.
* Fix: Default Face BlendShape and Offset BlendShape's incorrect selection, and facial interpolation behaviors.
* Fix: 2D accessory's incorrect behavior when it is attached to World and Foreground Mode is enabled.


#### v2.0.7
{: .doc-sec2 }

2022/07/29

* Add: [Hot Key](../docs/hotkey) feature in settings window, to run actions by shortcut key inputs.


#### v2.0.6
{: .doc-sec2 }

2022/07/04

* Fix: Accessory edit was not saved correctly when it is fixed to "World" and edited from Free Layout mode.
* Fix: In v2.0.5, avatar load failed when the model does not have "Chest" bone.
* Fix: In v2.0.5, specific avatar leads black background when shadow effect is enabled.
* Change: To fix shadow related issue, Unity version is roll-backed to 2020.3.22f1.
* Change: In free camera mode, wheel based zoom only works when mouse pointer is inside the character window.


#### v2.0.5
{: .doc-sec2 }

2022/06/29

* Add: "Clap" built-in motion.
* Add: "Clap", "Nod Head", "Shake Head" motion button in Streaming Tab's "Word to Motion" area.
* Change: Eye bone control to respect avatar's "VRMLookAtBoneApplyer" curve setting. You can roll-back this behavior to older version's one, by turning off Setting Window > "Face" tab > "Eye" > "Apply eye bone curve defined in avatar" option.
* Change: Pen 3D model's UV map has changed. Notice that this change is not backward-compatible. Please see detail at [Change Device Textures](../tips/change_textures).
* Change: Internal update of Unity Editor to 2021.3.1f1, and UniVRM to 0.98.0.
* Fix: Other some bugs.


#### v2.0.4
{: .doc-sec2 }

2022/04/29

* Change: Applied interpolation to facial expression by Word to Motion and Face Switch. This feature can be rolled back at setting window "face" tab > "BlendShape" settings.
* Fix: Word to Motion could not display accessory in specific condition.
* Fix: In free camera mode, left click based camera movement did not work correctly.
* Fix: Other minor issues.

#### v2.0.3
{: .doc-sec2 }

2022/03/30

* Add: "Twist Body Motion" option for a kind of cute motion.
* Change: Support to detect newly connected webcam and microphones after the app started.
* Fix: Some other minor issues.
* Fix: Internal source code refactors.

#### v2.0.2
{: .doc-sec2 }

2022/02/28

* Add: Word to Motion and Face Switch features support option to show accessory during motion or facial expression is active.
* Change: Numbered png accessory plays from start when it is hidden and re-shown.
* Fix: Reduce the cases that avatar's transparent parts are rendered in front of image accessory.


#### v2.0.1
{: .doc-sec2 }

2022/01/28

* Add: Support numberd png animation for accessory feature.
* Add: `World` option in accessory to fix item in 3D space.
* Change: Improve look and usability for free layout mode gizmos.
* Change: Add IPv6 support for iFacialMocap connection.
* Change: In free camera mode, add left click based input patterns to support laptop users' usability without mouse.
* Fix: Issue that specific environments might lead crash.
* Fix: Limit full screen mode. Here `Screen Mode` means non-title bar screen mode (which was allowed in very-old version), and normal full screen is available.

#### v2.0.0
{: .doc-sec2 }

2021/12/25

* Add: [Accessory](../docs/accessory) feature which support image files and 3D models.
* Fix: When avatar is loaded from VRoid Hub and camera get close to the avatar, some meshes becomes invisible


#### v1.9.3
{: .doc-sec2 }

2021/12/06

* Fix: Bug fix of v1.9.2, in whicn texture replacement and custom motion does not work correctly.


#### v1.9.2
{: .doc-sec2 }

2021/11/29

* Add: Show notification dialog when app update is available.
* Change: Lipsync now supports stereo channel microphone with only one side input.
* Change: `Reset Settings` feature improved to reset current settings more appropriately.
* Fix: Bug fix that character window does not close on app quitting until mouse pointer moves.
* Fix: Background color change had lead character window shrink.
* Fix: The case color picker UI does not set correct color.
* Change (Internal): Update Unity version to 2020.3.22f1, and control panel is now based on .NET 6.


#### v1.9.1
{: .doc-sec2 }

2021/10/24

* Fix: Fixed issue that v1.9.0 fails to load specific models. To fix the issue, UniVRM version was reset to 0.66.0.

#### v1.9.0
{: .doc-sec2 }

2021/10/23

* Add: Experimental feature of adjust light intensity and color, according to desktop displayed content.
* Change: Now the app is distributed with installer (.exe). This also changes where settings are saved / custom files should be placed. the folder is  `VMagicMirror_Files` under `My Documents`. (Folder is created if it does not exist).
* Fix: Minor issues.
* Changed: Update UniVRM version from 0.66.0 to 0.81.0.

Note that `VMagicMirror_Files` folder specification affects to the way to [Change Keyboard and Touchpad Appearance](../tips/change_textures), and [Use Custom Motion](../tips/use_custom_motion).


#### v1.8.2
{: .doc-sec2 }

2021/08/28

* Add: New built-in head motion `Nod` and `Shake`. These motions are registered as 7th and 8th Word to Motion item by default.
* Add: MIDI device supports up to 16 keys assignment for Word to Motion feature.
* Add: Gamepad newly supports L/R/Select buttons for Word to Motion feature.
* Change: Gamepad 3D models is replaced. This changes the way how to [Change Device Textures](../tips/change_textures), in which gamepad texture now supports UV and you will need single image file.
* Change: If iFacialMocap connection setting exists, the app automatically tries once to connect at start. App will success to connect when iFacialMocap runs beforehand and other settings are correct.


#### v1.8.1
{: .doc-sec2 }

2021/07/31

* Add: Option to emphasize face expression for External Tracker feature.
* Fixed: Error that VRoid Hub model download fails, when the model is first to use.
* Fixed: Incorrect finger assignment for arcade stick mode gamepad input.
* Changed: Improved guide flow about hand tracking feature.


#### v1.8.0
{: .doc-sec2 }

2021/07/17

* Change: Distribution style has changed. From this version, conventional free edition is called `Standard Edition` and there is new paid edition `Full Edition`. Please see detail at [Download](../download) page.
* Change(Add): Re-design camera based hand tracking, so that more precise motion is possible. Standard edition has limitation for this feature, that special visual effect is applied during hand tracking is enabled.
* Add: `Disable Horizontal Flip` option is applied to External Tracker system tracking.
* Add: Chinese Simplified localization for the GUI (*Thanks shirunesuru for the proposal in [this issue](https://github.com/malaybaku/VMagicMirror/issues/571)!)
* Fix: When OS language is specific locale like German and when using iFacialMocap, avatar position might be incorrect.
* Fix: Fix to work arcade stick motion to be more similar to the real device.
* Fix: Device layout in character window become incorrect if the device layout was not changed at all in first run.


#### v1.7.0b
{: .doc-sec2 }

2021/05/09

* Fix: v1.7.0 and v1.7.0a will cause startup crashed if VMagicMirror installation folder include multi-byte characters.
* Change: When using multiple displays, mouse touch pad motion and pen tablet motion gets larger.


#### v1.7.0a
{: .doc-sec2 }

2021/05/07

* Fix: In v1.7.0, Right side of the keyboard was not animated when visibility changed.
* Fix: Pen tablet and MIDI controller was visible just after the app started, against their default visibility.
* Add: [Change Device Textures](../tips/change_textures) tips link, in setting window `Layout` tab, 

#### v1.7.0
{: .doc-sec2 }

2021/05/05

* Add: Pen tablet like motion mode, for mouse and key input.
* Add: Arcade stick like motion mode, for gamepad input.
* Add: `Half FPS` option in setting window `Effect` tab.
* Add: Improved webcam based tracking `High Power Mode` option and remove beta indication. High power mode does not detect forward/backward motion and blink detection, but has higher stability.
* Fix: Improved webcam based trackign smoothing algorithm.
* Fix: When using some MIDI controller note-off operation was misunderstood as note-on input.
* Change: Internally update Unity Editor version to 2019.4.23f1, and UniVRM to v0.66.0.
* Removed: Remove `Use VRoid Default Setting` in External Tracking, Perfect Sync menu. This removal is because of the feature unstability, and even for older version this feature is no more recommended.


#### v1.6.2
{: .doc-sec2 }

2021/03/31

* Add: Background image loading feature.
* Add: Setting save and load functions has become more useful, and support UDP message based setting load.
* Add: MIDI controller texture replacement support, with similar way as keyboard and touch pad.
* Fix: Fix issue that background transparency disabled when loading VRoid Hub model on app start.
* Fix: Eye mesh bad appearance when using Perfect Sync in specific models. This issue is not fixed completely, but many of cases are now okay.
* Changed: Internal large amount of code fix, in control panel (WPF).

#### v1.6.1
{: .doc-sec2 }

2021/01/31

* Add: Hands go down when there is no keyboard or mouse input for certain time. This feature is enabled by default, and if you want to disable see setting window > `Motion` > `Arms` menu.
* Add: `Face` tab in setting window, which supports face related motion setting except about iOS collaboration. Most of the options are moved from `Motion` tab `Face` menu, in previous version.
* Add: Support for default face expression clip like `Neutral`, in setting window `Face` tab > `BlendShape` menu.
* Change: Motion became slower when iFacialMocap based tracking is lost.

#### v1.6.0
{: .doc-sec2 }

2020/12/30

* Add: Support for custom motion. Please see detail at [Use Custom Motion](../tips/use_custom_motion)
* Change: GUI design changed.
* Change: During you keep down the keyboard key or mouse button, then the avatar also keep key down pose.
* Change: MIDI input is not captured by default. This is to prevent performance problem in the PC with many connected MIDI controllers. If you need to capture the input, check setting window > `Device` tab > `MIDI` menu.
* Fix: The problem in specific PC enviroment that, avatar's face mesh disappears when face expression applied.
* Fix: The problem when using iFacialMocap that avatar did not go back to base position when tracking is lost.
* Removed: Removed virtual camera feature. Now `OBS Studio` 26.0 and later has virtual camera by default, so please consider to use it.


#### v1.5.0a
{: .doc-sec2 }

2020/11/01

* Fix: the issue in v1.5.0 that, VRoid Hub login UI does not receive any keyboard input, and thus cannot login.


#### v1.5.0 
{: .doc-sec2 }

2020/10/31

* Add: (beta feature) `High Power Mode ` for webcam tracking. This mode leads higher CPU load, but the motion will be quick.
* Add: Mic sensitivity adjust.
* Add: Typing effect `Butterfly`.
* Fix: Issue that, specifig games disturb keyboard input motion.
* Change: When `Transparent Background` is turned off, the window TopMost (always foreground) is always disabled.
* Change: Improve the appearance during hand down mode.
* Change: Apply bigger eyes waiting motion.
* Change: The process to control EyeBrow blendshapes especially for VRoid model when blink. If you feel it not good, please setup the model's  `BLINK_L` and `BLINK_R` BlendShapeClip to include the eyebrow motion.
* Change: Change face tracking algorithm to v1.3.0 based one.
* Change: Removed virtual cam feature from `Streaming` tab. You can still use this feature in setting window `Window` tab, but is will also be removed in v1.6.0. Please consider to use `OBS Studio` as it supports virtual cam by standard from version 26.0.


#### v1.4.0
{: .doc-sec2 }

2020/09/24

* Add: `Always-Hand-Down` mode. During this mode is selected, the hands are always down, and body motion increases.
* Change: Improve gamepad based motion, especially when you are doing quick input.
* Change: When Perfect Sync and other face expression with `Keep LipSync` option is running at same time, then mouth, jaw, cheek and tongue blendshapes of Perfect Sync continue to be applied.
* Change: Warning message will appear near to the webcam selection UI, when External Tracker is on.
* Change: `MIDI` option is removed from `Streaming` tab > `View`. This setting is available at setting window > `Layout` tab > `Keyboard / MIDI`
* Change: Performance improvement for some part.
* Change: Webcam face tracking algorithm slightly changed.
* Fix: Hand position does not match to gamepad when stick based body lean feature is turned off.
* Fix: When VMagicMirror repeats restart with background transparent option, the character window size increases.
* Fix: Character window sometimes goes outside of the monitor area.
* Fix: Twisted wrist after some built-in motions.
* Change: Reduced binary size, for distribution.

#### v1.3.0a
{: .doc-sec2 }

2020/08/27

* Fix: Issue in v1.3.0 that, Perfect Sync could not move avatar's right brow down.

#### v1.3.0
{: .doc-sec2 }

2020/08/27

* Add: Support for Perfect Sync by External Tracker. See [Tips Page](../tips/perfect_sync) for the detail.
* Add: Voice based random motion, for no tracking system environment.
* Change: Improved head tracking for web camera.
* Add: Option to change eye motion scale, on setting window `Motion` > `Face` > `Eye Motion Scale`.
* Fix: Avatar looked vertically stretched when transparent mode.
* Fix: Avatar moves strangely when webcam and fixed eye motion selected.
* Fix: Other small issues.

#### v1.2.0
{: .doc-sec2 }

2020/07/30

* Fix: Fix issue related to External Tracker especially for first use.
* Change: Unity version updated to 2019.4, which leads the improve performance.
* Change: Internal fix, for performance improvement.
* Change: Change colorspace from Gamma to Linear. This improves the appearance especially for the models which expect linear colorspace. For this change the light default color also changes to white (`#FFFFFF`). You can edit the color and intensity of the light from setting window `Effect` tab > `Light`.

#### v1.1.0
{: .doc-sec2 }

2020/06/26

* Add: External tracking app (iFacialMocap) collaboration.
* Add: Option to continue lipsync during Word to Motion face expression is applied.
* Add: Keyboard typing randomize mode (setting window `Motion` > `Arm`).
* Add: Load background image feature, when the file `background.jpg` or `background.png` is put on the folder where `VMagicMirror.exe` exists.
* Fix: Fix issue the free layout edit result often fails to be saved.


#### v1.0.0
{: .doc-sec2 }

2020/05/22

* Add: Add license preamble about reverse engineering.
* Add: VRoid Hub collaboration
* Add: Return to default position feature, when face tracking is enabled but not detected.
* Change: Body motion improved.
* Change: The pose just after the model is loaded is now hand down pose. With this update, you can use VMagicMirror with just standing model viewer if you uncheck `Motion` > `Arm` > `Enable Typing / Mouse Motion`.
* Change: In setting window, `Motion` > `Arm` > `Presentation-like hand` > `Show Pointer Support` is unchecked now by default.
* Fix: Fix issue some face parts might disappear when shadow depth offset is small. This issue still happens when shadow is enabled, but not happen when shadow is off.
* Fix: Fix issue Standard shader might fail to load correctly

#### v0.9.9a
{: .doc-sec2 }

2020/04/19

* Fix: Fix the issue that when virtual camera output is enabled, character window seems to be stopped in about 5 seconds, after losing window focus.

#### v0.9.9
{: .doc-sec2 }

2020/04/19

* Add: Virtual Camera Output
* Change: Improvement for shoulder motion.
* Change: Show warning when installed folder path includes multi byte character, to notify face tracking might fail.
* Fix: Fix the issue that some PC environment fails to load face texture.

Shoulder motion improvement is applied by default, but you can turn off it for the case that it does not suit for your avatar. Please check at Setting Window > `Motion` > `Arm` > `Modify shoulder motion`.

#### v0.9.8
{: .doc-sec2 }

2020/03/24

* Add: Hand tracking with web camera. Requires camera to use this feature.
    - Check `Streaming` tab > `Image based hand tracking` in `Face` menu.
* Change: Now arms continues to react mouse / keyboard inputs, even when choose `keyboard (num 0-8) ` for `Device Assign` in Word to Motion selection.
    - If you want to stop keyboard and mouse reaction, turn off Setting Window > `Motion` > `Arms` > `Motion` > `Enable Typing / Mouse Motion`.
* Change: Made minor performance improvement.
* Fix: Fix the issue the character's wrists are unnaturally banded on application start, if typing / mouse motion is disabled.

#### v0.9.7a
{: .doc-sec2 }

2020/02/22

* Add: Toggle UI to turn on /off the feature in v0.9.7, "Fix: Fix issue the right hand almost fixed when playing some first person view games, which uses mouse move to viewpoint control."
    - In setting window turn on `Motion` > `Arm` > `Enable mouse motion to FPS assumed mode` to enable the feature.
    - This feature is off by default, because it maybe disturb the pen tablet based motion.

#### v0.9.7
{: .doc-sec2 }

2020/02/22

* Add: Support PS4 Controller (DUAL SHOCK 4). In setting window turn on `Layout` > `Gamepad` > `Use DirectInput` to enable the controller.
* Change: Change right hand move when mouse moved. With this change, the hand will slightly drift by the quick mouse move. This change is because of the fix.
* Fix: Fix issue the right hand almost fixed when playing some first person view games, which uses mouse move to viewpoint control.

#### - (not an .exe update)
{: .doc-sec2 }

2020/01/24

* Change: Update web page 

#### v0.9.6
{: .doc-sec2 }

2020/01/13

* Add: Word to Motion supports all Blend Shape clips assigned to character, including non-VRM standard blendshapes.
* Add: MIDI controller support for word to motion, and basic motion.
* Change: When face tracking is running, forward and backward motion is disabled by default. You can turn on it on setting window `Motion` tab, `Face` menu.
* Change: Device free layout now shows move / rotate / scale gizmo only for the visible devices.
* Change: This updates has some internal performance improvements.

#### v0.9.5
{: .doc-sec2 }

2019/12/14

* Add: Gamepad model 
* Add: Device layout edit mode
* Change: Typing motion and touch pad motion
* Change: OK/Cancel Dialog is changed to Material Design based style
* Fix: Performance issue fix in v0.9.4
* Fix: URL link not working issue

#### v0.9.4
{: .doc-sec2 }

2019/12/07

* Add: Camera quick save / load
* Fix: Keyboard invisible from downward issue
* Add: Interactive blink generation with head / eye look-at / viseme, when NOT using image based blink.
* Change: "Auto blink during face tracking" is on by default, which is in setting window `Motion` tab.
* Change: Head motion becomes slower. This change prevents unstable head motion.
* Change: Application framework is still .NET Core 3.0, but distribute as NOT-single binary. This change means the application style becomes more conservative than v0.9.3.

#### v0.9.3
{: .doc-sec2 }

2019/11/09

* Add: Wind effect. On/Off at `Streaming` panel, and detail setting is in setting window `Effects` tab
* Add: Option to disable face tracking right-left reverse (Setting window, `Motion`>`Face`)
* Add: MOD loading function
* Change: Improve neck motion to be natural when `Eye Look Target` is `Mouse` or `User`
* Change: In the setting window, change the tab name `Light` to `Effects`
* Change: Config application framework changed to .NET Core 3.0. From this version distributed app only contains large `VMagicMirrorConfig.exe` in the config app folder. Also first startup of the software takes time a bit.

#### v0.9.2
{: .doc-sec2 }

2019/10/26

* Add: Support to launch word to motion item from number key of keyboard
* Change: Improve motion during using gamepad
* Fix: Improve behavior to avoid strange motion of head and right hand just after started
* Remove: Gamepad visible / invisible selection (because v0.9.0 or later version does not show it)

#### v0.9.1
{: .doc-sec2 }

2019/10/14

* Add: "Word to Motion" feature support gamepad input
* Add: Add "Good" motion to "Wort to Motion" built-in motions
* Change: In presentation mode, all keyboard typing is treated as left hand motion
* Change: In presentation mode, automatic adjust for arm stretch factor
* Change: Improve motion to relax wrists twist
* Change: Fix internal behavior to reduce risk of treated as malware from security software

#### v0.9.0
{: .doc-sec2 }

2019/09/29

* Add: "Word to Motion" feature to move the character by typing
* Add: Screenshot
* Add: Simply standing mode, without typing and mouse reaction
* Change: Motion for the gamepad, so that it looks like the character grips a normal gamepad.
* Change: Eye motion (introduce eye jitter)
* Change: Internal source code updates.
* Fix: Fix bug that, in some Europe Windows OS, the character does not appear after load.
* Fix: Fix bug of unnatural eye motion, when the character does not have blink BlendShape.

#### v0.8.7
{: .doc-sec2 }

2019/08/04

* Add: Typing Effect
* Add: Auto blink when tracking face
* Change: Head motion to be 3DoF when tracking face

#### v0.8.6
{: .doc-sec2 }

2019/07/22

* Add: Pointer emphasize during presentation mode.
* Add: Whole character transparency in `Window` setting.
* Fix: Fix to enable loading specific VRM models those were unavailable in v0.8.4 and v0.8.5.


#### v0.8.5
{: .doc-sec2 }

2019/06/15

* Add: UI icons
* Fix: Avoid to show two or more setting windows
* Change: Improve to reduce CPU usage
* Change: Improve shadow looking
* Change: Increase the scale of waiting motion with faster motion

#### v0.8.4 
{: .doc-sec2 }

2019/06/05

* Fix: Shadow was strange with trasparent colored texture until v0.8.3.
* Change: Default light and shadow settings.
* Change (Internal): Update UniVRM version from v0.51.0 to v0.53.0.

#### v0.8.3 
{: .doc-sec2 }

2019/06/02

* Add: shadow
* Add: UI to adjust light and shadow direction ("light" tab in setting window)
* Change: decrease FOV of camera from 60deg to 40deg
* Change: free camera mode changes its rotational motion to track character. 
* Change: disable dragging when you click transparent area, during transparent window mode
* Fix: Stabilize the appearance of VRM load UI.

#### v0.8.2(a)
{: .doc-sec2 }

2019/05/21

* New: "Open Manual URL" link at the right top side on control panel Home tab.
* Changed: Internal optimization for the higher FPS during face tracking.

#### v0.8.2
{: .doc-sec2 }

2019/05/19

* Changelog is now in English!!
* Issue fix: in v0.8.1 character position sometimes gets incorrect. v0.8.2 fix this issue.
* Issue fix: some key typing like "g" and "h" makes hands collision until v0.8.1, and v0.8.2 fixes this issue partially.
* New feature : Camer FOV (field of view) adjust UI on setting window > "Layout" > "Camera"
* Changed : Internal improvement for the lighter of face tracking
* Changed : During the face tracking, close eye totally when almost closed
* Changed : Improve presentation mode motion so that right index finger and mouse pointer get closed
* Changed : Hide mouse pad always (as it looks not so good by overriding keyboard area..)

#### v0.8.1
{: .doc-sec2 }

2019/05/12

* "Adjust size by VRM" feature
* Allow resize character window, and remove resolution selection dialog
* Eyebrow motion parameter UI
    + For VRoid Studio based model and Alicia Solid, automate setup
* Automatic startup when Windows has started
* Load previous version setting (* works for v0.8.0 or later)
* Fix issue after lip sync is disabled, mouth remains open

#### v0.8.0
{: .doc-sec2 }

2019/05/04

* Face tracking
* New manual (this is what you see now!)
* Refresh whole GUI layout
* Eyebrow motion for VRoidStudio 0.6.3 or newer VRoidStudio output model

#### v0.1.6a (v0.16a)
{: .doc-sec2 }

2019/04/28

* Fix issue that v0.1.6 bends wrist angle too much

#### v0.1.6 (v0.16)
{: .doc-sec2 }

2019/04/27

* Free camera mode
* Prensentation mode to point mouse position by right hand
* Close character's elbow to the body feature

#### v0.1.5 (v0.15)
{: .doc-sec2 }

2019/04/20

* Selectable microphone input for lip sync
* When closing character window, control panel also closes
* Fix issue some character cannot move her/his finger and lip sync does not work

#### v0.1.4 (v0.14)
{: .doc-sec2 }

2019/03/29

* Controller input support

#### v0.1.3 (v0.13)
{: .doc-sec2 }

2019/03/22

* Lip Sync
* UI translation for JP/EN

#### v0.1.2 (v0.12)
{: .doc-sec2 }

2019/03/17

* Add function to memorize character window position
* Fix issue some character has strange wrist and elbow bending angle
* Fix issue the PrintScreen key is incorrect

#### v0.1.1 (v0.11)
{: .doc-sec2 }

2019/03/14

* UI to adjust hand - keyboard distance
* Adjust window positions to avoid control panel is hidden
* Disable full screen by default

#### v0.1.0 (v0.1)
{: .doc-sec2 }

2019/03/13

* Publish on BOOTH

</div>
