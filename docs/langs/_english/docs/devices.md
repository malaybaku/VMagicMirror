---
layout: page
title: Devices
permalink: /en/docs/devices
lang: en
---

[Japanese](../../docs/devices)

# Devices

`Devices` tab support connection settings for gamepad and MIDI controller.

In v1.5.0 and older version, this feature was included in `Layout` tab.

<div class="row">
{% include docimg.html file="/images/docs/devices_top.png" customclass="col s12 m6 l6" imgclass="fit-doc-img" %}
</div>

#### 1. Gamepad
{: .doc-sec2 }

`Enable Input Capture`: On by default, but if you do not use game controller and you want to save CPU usage, then turn off.

`Use DirectInput (check for DUAL SHOCK 4)`: Turn on to use DUAL SHOCK 4. If your controller is not recognized by default, please try to turn on.

`Visible`: Show or hide game controller. Available in `Streaming` tab of control panel.

`Lean by Stick`: Set by which input avatar leans.

`Reverse Direction to lean`: Set which axis lean should negate.

#### 2. MIDI
{: .doc-sec2 }

`Use MIDI Controller for VMagicMirror`: Turn on to reflect MIDI input based motion or to controll face expression.

Turn off when you are using DAW software with VMagicMirror, or using on PC with many connected MIDI controllers.

**NOTE**: In v1.5.0 and older version, `Use MIDI Controller on VMagicMirror` is turned on by default. In v1.6.0 and later, this check is off by default.
