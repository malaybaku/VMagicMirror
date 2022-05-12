---
layout: page
title: Using Hand Tracking
lang: en
---

# Tips: (v1.7.0b and older ver) Using Hand Tracking

<div class="note-area" markdown="1">

**NOTE**

This page is about hand tracking supported in v1.7.0b and older version.

If you have v1.8.0 or later, please see the latest [Hand Tracking](../docs/hand_tracking) page instead.

</div>

The hand tracking system in VMagicMirror runs with low CPU cost, but there are some limitations.

There also exists some preferred environments for the stability.

#### Limitations
{: .doc-sec2 }

This system does not aim to capture the precise hand position.

The finger state check is rough and limited to detect "rock, paper, scissors" pattern.

The orientation to move hand is up-down or left-right. Forward and backward motion is not detected.


#### Recommended Environments
{: .doc-sec2 }

Light up the room so your face and hands are captured clearly from the web camera.

Please put away the objects behind you whose colors are similar to your skin color.

For the clothing, please avoid to expose your shoulders and elbows. 

Also the clothing with different color from your skin color is preferred.


#### Cf: How the Hand Tracking Works
{: .doc-sec2 }

In VMagicMirror, the hand tracking system is realized with following steps, on the base of face tracking technology. You do not have to understand the process perfectly, but it would help to understand the background of limitations.

Step1: Detect face area with deep neural network system.

Step2: Get the mean color value at the center of face area, to estimate user's skin color.

Step3: In the left and right area of the face, try to pick up the similar color area (Blob).

Step4: If the blob area is enough large, then treat them as hand area.

Step5: Check the shape of the blob to count stretched fingers.

Step6: From the Step4 and Step5, the system get the hand area size and finger stretching count, so it can estimate the hand shape.

The following tweet shows the image.

<blockquote class="twitter-tweet"><p lang="ja" dir="ltr"><a href="https://twitter.com/hashtag/VMagicMirror?src=hash&amp;ref_src=twsrc%5Etfw">#VMagicMirror</a><br>ひじょーーーーーーに今更なんですが、画像ベースのハンドトラッキング機能を作成中です。<br><br>ウェブカメラのみを使って顔検出とセットで動く機能で、CPU負荷が低いのがポイントです。<br><br>手の向きとかグーチョキパーの反映くらいまで出来るようになったらリリースしたいな～という感じです <a href="https://t.co/QWOhDRbDYG">pic.twitter.com/QWOhDRbDYG</a></p>&mdash; 獏星(ばくすたー) / Megumi Baxter (@baku_dreameater) <a href="https://twitter.com/baku_dreameater/status/1237380280127643650?ref_src=twsrc%5Etfw">March 10, 2020</a></blockquote> <script async src="https://platform.twitter.com/widgets.js" charset="utf-8"></script>

The black painted area is detected face area. This black paint is a pre-process to prevents the incorrect hand detection from neck or other skin colored areas around the face.

Pink circles on the hand means the area between fingers detected. This part shows that, the detection is done though it is not so highly precised.
