---
layout: page
title: Hand Tracking
permalink: /en/docs/using_hand_tracking
lang_prefix: /en/
---

[Japanese](../../docs/hand_tracking)

# Hand Tracking

This page is about webcam based hand tracking feature, supported in VMagicMirror v1.8.0 and later.

<div class="row">
{% include docimg.html file="./images/docs/hand_tracking.png" customclass="col s12 m6 l6" imgclass="fit-doc-img" %}
</div>

<div class="note-area" markdown="1">

**NOTE**

This page is for v1.8.0 or later version.

If you have older version, please see  [(v1.7.0b and older ver) Using Hand Tracking](../docs/hand_tracking) instead.

</div>

#### Feature / Expected Usage
{: .doc-sec2 }

By hand tracking feature, you can

<div class="doc-ul" markdown="1">

- Track your hand by webcam, during your hands are next to the face.
- Finger tracking is also available.

</div>

Expected usage is like following.

<div class="doc-ul" markdown="1">

- Wave your hands by tracking at the start and end of a streaming.
- Do Paper-lock-scissors

</div>


#### Limitations
{: .doc-sec2 }

In Standard Edition distributed by free, some visual effects are applied during hand tracking is enabled.

Please get Full Edition to use it without this effect. Please see detail in [Download](../download) page.

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
- Precision will decrease when the hand does not face to webcam.
- Cross hands is not recognized.
- Hand detection area is small, in compare to tracking specialized device (like Leap Motion).

</div>


#### Usage
{: .doc-sec2 }

In `Hand Tracking` tab, check `Enable Hand Tracking` and select webcam.

<div class="note-area">

**NOTE**

If you use webcam to face tracking, then single webcam is used for both face and hand tracking. This means that you cannot select different cameras.

Also you can use [External Tracker](./external_tracker) with this hand tracking feature.

</div>

You can turn on `Disable Horizontal Flip` to disable flip, so that avatar will raise right hand when you raise the right hand.

During `Show Detection Status` is on, the area below shows detection results. Especially for the first time to try hand tracking, I recommend to turn on this check to see tracking stability and the area size of tracking.

<div class="row">
{% include docimg.html file="./images/docs/hand_tracking_debug_area.png" customclass="col s12 m6 l6" imgclass="fit-doc-img" %}
</div>


#### Tips to stabilize the tracking
{: .doc-sec2 }

<div class="doc-ul" markdown="1">

- Light up the room so your face and hands are captured clearly from the web camera.
- Please put away the objects behind you whose colors are similar to your skin color.
- For the clothing, please avoid to expose your shoulders and elbows. 
- Be aware of the distance between your hands and camera. Too far is NG of course, and detection also fails when the hand is too close to camera.

</div>
