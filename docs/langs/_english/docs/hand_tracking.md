---
layout: page
title: Hand Tracking
lang: en
---

# Hand Tracking

This page is about hand tracking feature with web camera.

<div class="row">
{% include docimg.html file="./images/docs/hand_tracking.png" customclass="col s12 m6 l6" imgclass="fit-doc-img" %}
</div>

#### Feature / Expected Usage
{: .doc-sec2 }

By hand tracking feature, you can:

<div class="doc-ul" markdown="1">

- Track your hand by web camera, during your hands are next to the face.
- Track finger motions.

</div>

Expected usage is like following.

<div class="doc-ul" markdown="1">

- Wave your hands by tracking at the start and end of a streaming.
- Do Paper-lock-scissors

</div>


#### Limitations
{: .doc-sec2 }

In Standard Edition distributed by free, some visual effects are applied during hand tracking is enabled.

Please get Full Edition to use it without this effect. Please see detail in [Download](../../download) page.

<div class="doc-ul" markdown="1">

- [(BOOTH)VMagicMirror Full Edition](https://baku-dreameater.booth.pm/items/3064040)

</div>

(Left: Standard Edition / Right: Full Edition)

<div class="row">
{% include docimg.html file="./images/docs/hand_tracking_edition_difference.png" customclass="col s12 m6 l6" imgclass="fit-doc-img" %}
</div>

This visual effect is the only difference. There are no difference in tracking feature.

Also please check following restrictions.

<div class="doc-ul" markdown="1">

- It does not track quick movement.
- Precision will decrease when the hand does not face to the camera.
- App v3.9.1 and older version does not recognize cross hands.
- Hand detection area is small, in compare to tracking specialized device (like Leap Motion).

</div>


#### Usage
{: .doc-sec2 }

In `Hand Tracking` tab, check `Enable Hand Tracking` and select webcam.

<div class="note-area" markdown="1">

**NOTE**

If you use webcam to face tracking, then single webcam is used for both face and hand tracking. This means that you cannot select different cameras.

Also you can use [External Tracker](../external_tracker) with this hand tracking feature.

There are several options available.

<div class="doc-ul" markdown="1">

- `Disable Horizontal Flip`: Turn on to disable flip, so that avatar will raise right hand when you raise the right hand.
- `Motion Scale(%)`: Specify scale to apply your motion to the avatar by percentage. Default value is 100.
- `Hand Horizontal Offset (cm)`: A positive value will make the hands spread out to the left and right.
- `Hand Vertical Offset (cm)`: A positive value will the hands rise higher.
- `Show Detection Status`: Turn on to show how hand is tracked in control panel window. At the first time using hand tracking feature, I recommend to use the option to see tracking stability and the area where the hand can be tracked.

</div>


<div class="row">
{% include docimg.html file="./images/docs/hand_tracking_debug_area.png" customclass="col s12 m6 l6" imgclass="fit-doc-img" %}
</div>


#### Tips to stabilize the tracking
{: .doc-sec2 }

<div class="doc-ul" markdown="1">

- Light up the room so your face and hands are captured clearly from the web camera.
- Large PC monitor becomes a light by itself. Detection will be stable when displayed content is basically white.
- Please put away the objects behind you whose colors are similar to your skin color.
- For the clothing, please avoid to expose your shoulders and elbows. 
- Be aware of the distance between your hands and camera. Too far is NG of course, and detection also fails when the hand is too close to camera.
- In v4.0.0 and later version, single hand tracking is more stable than tracking two hands.

</div>

Another good way to check how thing and lights has effect is to quit VMagicMirror and run Windows built-in `Camera` app.
Please be careful that both VMagicMirror and other apps will occupy the camera device to use, and other apps cannot access the camera at the same time.
