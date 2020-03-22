---
layout: page
title: Motion
permalink: /en/docs/motion
lang_prefix: /en/
---

[Japanese](../../docs/motion)

# Motion

`Motion` tab can adjust character's proportion and motion related parameters.

{% include docimg.html file="/images/docs/motion_top.png" %}

#### Face
{: .doc-sec2 }

`LypSync`: Choose Microphone to use lip sync (viseme) feature. Available in Streaming tab.

`Track Face`: Choose WebCam to use face tracking feature. Available in Streaming tab.

`Enable image based hand tracking`: Turn on to use camera image based minimal hand tracking. Available in Streaming tab.

`Auto blink during face tracking`: Checked by default, and by turn off it, character blinks based on image processing.

`Enable forward / backward motion`: Check to see horizontal forward / backward motion.

`Disable Horizontal Flip`: Check to disable horizontal flip of motion. After changing this option press `Calibrate position` to calibrate.

`Calibrate Position`: Press to calibrate the position by current user position captured by web camera.

`Blink adjust by head motion`: Check to enable auto blink action when the avatar moves head quickly.

`Blink adjust by lip sync`: Check to enable auto blink when detect the end of speech.

`Eye Look Target`: Select where the character look to. Available in Streaming tab.

`Default Fun Blend Shape [%]`: Specifies the default fun expression rate. As the value increases the character will become always smile, but some character's facial expression will be unnatural when combined to blink or other face motions. In those cases, decrease the value.

`Eyebrow (Open to Customize)`: Advanced section and normally you do not need modify them. However this option will be helpful if you have original VRM and want to move the eyebrow, or in case the eyebrow motion is too big or too small. 

* This section requires the knowledge about `BlendShape` to control VRM facial expression. If you do not know well, please refer to [Virtual Cast Wiki](https://virtualcast.jp/wiki/doku.php?id=%E3%83%A2%E3%83%87%E3%83%AB%E4%BD%9C%E6%88%90:%E3%83%96%E3%83%AC%E3%83%B3%E3%83%89%E3%82%B7%E3%82%A7%E3%82%A4%E3%83%97%E8%A8%AD%E5%AE%9A) and see latter section's image. You will see the name like `mouth_a` or `mouth_b`. This is BlendShape. And you can specify the name of BlendShape to move eyebrow, by sliding each shape in Unity Editor.

#### Arm
{: .doc-sec2 }

`Enable Typing / Mouse Motion`: On by default. If turned off, the avatar stops to react to typing, mouse pointer move, or gamepad input. If you want to set the avatar always simply standing, turn off this checkbox.

`Waist width [cm]`: Set how much the character put his/her elbow outside.

`Strength to keep upper arm to body [%]`: If set larger value, the character's pose obeys more strictly to `Waits width [cm]` value.

Please see the following example of default value, close elbow, or open elbow.

<div class="row">
{% include docimg.html file="/images/docs/arm_side_default.png" customclass="col s12 m4 l4" imgclass="fit-doc-img" %}
{% include docimg.html file="/images/docs/arm_side_close.png" customclass="col s12 m4 l4" imgclass="fit-doc-img" %}
{% include docimg.html file="/images/docs/arm_side_open.png" customclass="col s12 m4 l4" imgclass="fit-doc-img" %}
</div>

`Presentation-like hand`: Check to move the charactrer's right hand as if he/she talks in presentation.

`Presentation-like motion scale [%]`: This property is not used currently, and will be removed in future update.

`Arm position radius min [cm]`: Set this parameter to avoid the situation the arms going into the body. Larger value makes hand position to be far from body.

#### Hand
{: .doc-sec2 }

Parameters about hand or finger length and typing motion.

`Wrist-Hand tips length [cm]`: Set the length from wrist to hand tip. This length is used to adjust the hand position when typing or moving mouse.

`Wrist-Palm length [cm]`: This property is not used currently, and will be removed in future update.

`Hand height adjust [cm]`: The vertical offset length from devices to the hand.

`(Press key)Hand height adjust [cm]`: The vertical offset after typing.

**Hint:** You can set large value to `(Press key) Hand height adjust [cm]` after setting up natural motion, to make comical big typing motion.

<div class="row">
{% include docimg.html file="/images/docs/large_typing_motion.png" customclass="col s12 m4 l4" imgclass="fit-doc-img" %}
</div>

#### Wait motion
{: .doc-sec2 }

Wait motion is breathing like motion. Normally you can use default setting, but if it seems unnatural then disable or adjust parameters.

`Enable`: Enable waiting motion.

`Scale [%]`: Set the motion scale.

`Period [sec]`: Set the period of wait motion by second.
