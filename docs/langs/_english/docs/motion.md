---
layout: page
title: Motion
permalink: /en/docs/motion
lang: en
---

# Motion

`Motion` tab can adjust character's proportion and motion related parameters.

{% include docimg.html file="/images/docs/motion_top.png" %}

<div class="note-area" markdown="1">

**NOTE**

If you use v1.6.0 or older version, then you see `Face` menu in this tab. See [Face](./face) page for the detail.

</div>


#### Upper Body
{: .doc-sec2 }

`Always-Hands-Down Mode`: Turn on to force both arms always on. This mode increases body translate motion.  `Streaming` tab supports same feature.

`Key/Mouse Motion`: Select how avatar moves by keyboard and mouse input. `Streaming` tab supports same feature.

`Gamepad Motion`: Select how avatar moves by gamepad input. `Streaming` tab supports same feature.

When you choose `Pen Tablet` in `Key/Mouse Motion`, the avatar will look to the tablet area during the input.

Also, when you choose `Arcade Stick` in `Gamepad Motion`, the avatar will react to ABXY/L1L2R1R2 buttons and left stick input, and will ignore other inputs like start button press.


#### Arm
{: .doc-sec2 }

`Enable Typing / Mouse Motion`: On by default. If turned off, the avatar stops to react to typing, mouse pointer move, or gamepad input. If you want to set the avatar always simply standing, turn off this checkbox.

`Random typing to hide key input`: When checked, the avatar keyboard input becomes random to hide what key you pressed actually.

`Modify shoulder motion`: Make shoulder motion richer. Enabled by default.

`Waist width [cm]`: Set how much the character put his/her elbow outside.

`Strength to keep upper arm to body [%]`: If set larger value, the character's pose obeys more strictly to `Waits width [cm]` value.

Please see the following example of default value, close elbow, or open elbow.

<div class="row">
{% include docimg.html file="/images/docs/arm_side_default.png" customclass="col s12 m4 l4" imgclass="fit-doc-img" %}
{% include docimg.html file="/images/docs/arm_side_close.png" customclass="col s12 m4 l4" imgclass="fit-doc-img" %}
{% include docimg.html file="/images/docs/arm_side_open.png" customclass="col s12 m4 l4" imgclass="fit-doc-img" %}
</div>

`Enable mouse motion to FPS assumed mode`: When this checkbox is turned on, the hand will still move when you are playing PC FPS game. This option also has disadvantage that pen tablet or screen touch operation maybe become unnatural.

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
