---
layout: page
title: Face
lang: en
---

# Face

`Face` tab support motion setting especially about face.

{% include docimg.html file="/images/docs/face_top.png" %}

<div class="note-area" markdown="1">

**NOTE**

This tab is added in v1.6.1. If you use older version, you can see most of these options in `Motion` tab `Face` menu.

</div>


#### Basic Settings
{: .doc-sec2 }

`LypSync`: Choose Microphone to use lip sync (viseme) feature. Available in Streaming tab.

`Sensitivity [dB]`: Specify plus value when the microphone input is too small, to obtain good result for lipsync.

`Show Volume`: Turn on to see the input volume. Adjust `Sensitivity` such that, the volume bar is green and sometimes red during talking.

`Track Face`: Choose WebCam to use face tracking feature. Available in Streaming tab.

`High Power Mode`: Get more robust face tracking result, while getting higher CPU load. Available from v1.7.0.

`Auto blink during face tracking`: Checked by default, and by turn off it, character blinks based on image processing.

`Enable forward / backward motion`: Check to see horizontal forward / backward motion.

`Disable Horizontal Flip`: Check to disable horizontal flip of motion. After changing this option press `Calibrate position` to calibrate.

`Calibrate Position`: Press to calibrate the position by current user position captured by web camera.

`Voice based motion when webcam not used`: When image based head tracking is not used, avatar moves automatically by voice input.


#### Eye
{: .doc-sec2 }

`Blink adjust by head motion and lip sync`: Check to enable auto blink action, when the avatar moves head quickly or detect the end of speech by microphone.

`Eye Look Target`: Select where the character look to. Available in Streaming tab. Select `Mouse` to makes the character look at the orientation mouse pointer exists. Select `Fixed` to fix head motion except face tracking. `User` is similar to `Fixed`, but different when the character body does not face straight to the monitor, by `Free Camera Mode`. In this case `User` makes the character looks head to straight to monitor (in other word, keeps to look you).

`Eye Motion Scale[%]`: Set how eye (eye bone) moves by mouse gaze, or by ExTracker. Recommend default (100%) for VRoid model. If avatar eyes motion is too small, try larger value.

#### BlendShape
{: .doc-sec2 }

`Disable BlendShape Interpolation`: This option is available from v2.0.4. When turn on, then facial interpolation process for Word to Motion and Face Switch are disabled, and facial expression always switches immediately.

`Default Fun Blend Shape [%]`: Specifies the default fun expression rate. As the value increases the character will become always smile, but some character's facial expression will be unnatural when combined to blink or other face motions. In those cases, decrease the value.

`Default Face BlendShape`: In most case you should leave it empty. If your model needs some default expression to be applied, then select the BlendShape clip. Notice that the selected blendshape and `BLINK_L`, `BLINK_R`, lipsync, and perfect sync will run at the same time.

`Offset BlendShape`: In most case you should leave it empty. If your model has some BlendShapeClip which must be applied always to adjust body or face shape, then select it. 
