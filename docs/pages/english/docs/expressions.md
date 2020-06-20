---
layout: page
title: Expressions
permalink: /en/docs/expressions
lang_prefix: /en/
---

[Japanese](../../docs/expressions)

# Expressions

This page is about how to control face expression (and a few motions) in VMagicMirror.

This feature is called `Word to Motion`.

#### Overview
{: .doc-sec2 }

`Word to Motion` is the function to control avatar's facial and motion expressions.

Originally this feature supports receiving word input (e.g. "joy") to emotional expression, but now it also supports several input styles, as you would expect.

1. Keyboard, word input base
2. Gamepad, button input
3. Keyboard, number key
4. MIDI Controller

<div class="row">
{% include docimg.html file="/images/docs/word_to_motion_top.png" customclass="col s12 m6 l6" imgclass="fit-doc-img" %}
{% include docimg.html file="/images/docs/word_to_motion_by_gamepad.png" customclass="col s12 m6 l6" imgclass="fit-doc-img" %}
</div>

The example above shows `Device Assignment` selected to `Gamepad` and press `Y` button to launch `fun` expression.

You can sort items by up/down buttons at the left of each items, and can delete them by right `x` button.

To add and custom item, take the following steps.

1. Press `+` button to add a new item. If you custom existing item, skip it.
2. Press `Setting` button to open the setting.
3. In the custom window you can setup motion and face blend shape.
4. Press `OK` to confirm your custom.

<div class="row">
{% include docimg.html file="/images/docs/word_to_motion_custom_flow.png" customclass="col s12 m6 l6" imgclass="fit-doc-img" %}
{% include docimg.html file="/images/docs/word_to_motion_custom_window.png" customclass="col s12 m6 l6" imgclass="fit-doc-img" %}
</div>

In custom window you can take 4 actions.

1. Set the word to start this item. If you do not use `Keyboard (word)` mode, then this work just for the name label.
2. Select motion type for this item. 
    * **NOTE:** In v0.9.6 only 2 built-in motions are supported, so in almost case you will select `None`.
3. Face expression setting.
    * Turn on `Enable Face Expression` check to play some face expression in this item.
    * `Duration when Body Motion is None [sec]` defines how long the character keep the face.
    * `Keep LipSync active` enables lipsync during the face expression is applied, for the blendshape without mouth motion.
    * If `Keep face after motion` is on, then character face does not change after the duration.
4. Set BlendShapes. 
    * **NOTE:** Basically, set only one blend shape to non-zero, and set other values to zero.

From v0.9.6 you can also use MIDI controller for input device. There are 3 steps to use MIDI controller to launch expressions.

Following tweet show how to setup.

<blockquote class="twitter-tweet"><p lang="ja" dir="ltr"><a href="https://twitter.com/hashtag/VMagicMirror?src=hash&amp;ref_src=twsrc%5Etfw">#VMagicMirror</a><br>MIDIコンを叩くと表情が変わるやつの進捗です。<br><br>・コントロールパネル側で、MIDIコンと実行するアイテムのマッピング設定を開く<br>・MIDIコンのキーを叩いてセットアップ<br>・セットアップ完了したら再びMIDIコンのキーを叩く<br><br>の3手順で動きます <a href="https://t.co/RDbsszWLpi">pic.twitter.com/RDbsszWLpi</a></p>&mdash; 獏星(ばくすたー) / Megumi Baxter (@baku_dreameater) <a href="https://twitter.com/baku_dreameater/status/1211990346525077504?ref_src=twsrc%5Etfw">December 31, 2019</a></blockquote> <script async src="https://platform.twitter.com/widgets.js" charset="utf-8"></script>

1. Select `Device Assign` to `MIDI Controller` and click edit button at the right of `Keys`.
2. You will see `MIDI note assign` window, so press MIDI keys to use, to input MIDI note number for each items. `Note to Change` column is updated by your MIDI input.
3. After setting click `OK` to complete setting.

After closing the window, try pressing the same keys to check the input works.

#### Hint
{: .doc-sec2 }

`Keyboard (word)` in `Device Assignment`: Please do NOT type special keys like Control/Shift/Alt during input word. When the typing is too slow, the word input is not recognized.

`Gamepad` in `Device Assignment`: When you select this option, the gamepad gripping motion is disabled.

`Keyboard (num 0-8)` in `Device Assignment`: When you select this option, the typing motion and touch pad motion is ignored.

`Keep face after motion` in custom window: If you use this option the face expression does not reset automatically, so you should have reset item. In default setting `reset` item works as you want.

{% include docimg.html file="/images/docs/word_to_motion_reset_tips.png" %}
