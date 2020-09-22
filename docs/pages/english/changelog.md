---
layout: page
title: Change Log
permalink: /en/changelog
lang_prefix: /en/
---

[Japanese](../changelog)

# Change Log

#### v1.4.0

2020/09/26

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

#### v1.3.0

2020/08/27

* Add: Support for Perfect Sync by External Tracker. See [Tips Page](./tips/perfect_sync) for the detail.
* Add: Voice based random motion, for no tracking system environment.
* Change: Improved head tracking for web camera.
* Add: Option to change eye motion scale, on setting window `Motion` > `Face` > `Eye Motion Scale`.
* Fix: Avatar looked vertically stretched when transparent mode.
* Fix: Avatar moves strangely when webcam and fixed eye motion selected.
* Fix: Other small issues.

#### v1.2.0

2020/07/30

* Fix: Fix issue related to External Tracker especially for first use.
* Change: Unity version updated to 2019.4, which leads the improve performance.
* Change: Internal fix, for performance improvement.
* Change: Change colorspace from Gamma to Linear. This improves the appearance especially for the models which expect linear colorspace. For this change the light default color also changes to white (`#FFFFFF`). You can edit the color and intensity of the light from setting window `Effect` tab > `Light`.

#### v1.1.0

2020/06/26

* Add: External tracking app (iFacialMocap) collaboration.
* Add: Option to continue lipsync during Word to Motion face expression is applied.
* Add: Keyboard typing randomize mode (setting window `Motion` > `Arm`).
* Add: Load background image feature, when the file `background.jpg` or `background.png` is put on the folder where `VMagicMirror.exe` exists.
* Fix: Fix issue the free layout edit result often fails to be saved.


#### v1.0.0

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

2020/04/19

* Fix: Fix the issue that when virtual camera output is enabled, character window seems to be stopped in about 5 seconds, after losing window focus.

#### v0.9.9

2020/04/19

* Add: Virtual Camera Output
* Change: Improvement for shoulder motion.
* Change: Show warning when installed folder path includes multi byte character, to notify face tracking might fail.
* Fix: Fix the issue that some PC environment fails to load face texture.

Shoulder motion improvement is applied by default, but you can turn off it for the case that it does not suit for your avatar. Please check at Setting Window > `Motion` > `Arm` > `Modify shoulder motion`.

#### v0.9.8

2020/03/24

* Add: Hand tracking with web camera. Requires camera to use this feature.
    - Check `Streaming` tab > `Image based hand tracking` in `Face` menu.
* Change: Now arms continues to react mouse / keyboard inputs, even when choose `keyboard (num 0-8) ` for `Device Assign` in Word to Motion selection.
    - If you want to stop keyboard and mouse reaction, turn off Setting Window > `Motion` > `Arms` > `Motion` > `Enable Typing / Mouse Motion`.
* Change: Made minor performance improvement.
* Fix: Fix the issue the character's wrists are unnaturally banded on application start, if typing / mouse motion is disabled.

#### v0.9.7a

2020/02/22

* Add: Toggle UI to turn on /off the feature in v0.9.7, "Fix: Fix issue the right hand almost fixed when playing some first person view games, which uses mouse move to viewpoint control."
    - In setting window turn on `Motion` > `Arm` > `Enable mouse motion to FPS assumed mode` to enable the feature.
    - This feature is off by default, because it maybe disturb the pen tablet based motion.

#### v0.9.7

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

