---
layout: page
title: Use VMagicMirror for Streaming
lang: en
---

# Tips: Use VMagicMirror for Streaming

VMagicMirror expects 2 main use cases of Streaming and Desktop Mascot.

This tips writes about the setup for streaming.

#### Choose screen capture or Window capture
{: .doc-sec2 }

For the popular streaming software like OBS, usually you have 2 ways to capture the avatar shown on VMagicMirror.

1. Screen capture: capture whole screen or part of the screen.
2. Window capture: disable avatar window transparency, and capture the window.

Screen capture uses as-is image. This way has the merit to be easy to understand what happens, and shadow and semi-transparent interfaces (like touch pad) are correctly shown.

When you use window capture please check following points.

Turn off shadow effects. In `Streaming` tab, hide `shadow`.

If the bloom effect seem to prevent chromakey composition, then turn off. In setting window `Effects` tab, change `Bloom`'s `Intensity` to 0.

If your avatar has green part, you might need to change chromakey color. In setting window `Window` tab, change background color.

Keyboard and touchpad objects are semi-transparent and thus maybe looks not good, when in composit image. In this case hide them, or replace the device textures (see [Change Device Textures](../change_textures)).

**NOTE:** OBS has one more useful choice of "Game Capture". Game capture supports to capture transparent window as is, and this feature will work very well for VMagicMirror with transparent background. If your PC has enough capacity to do so, please consider using game capture instead of normal window capture.

#### Check CPU Usage
{: .doc-sec2 }

When streaming PC uses many computational resources.

Check the following points to reduce CPU usage of `VMagicMirror`, but some of them might make the looking worse.

In `Streaming` tab on control panel,

1. (Large effect): Disable face tracking.
2. (Middle effect): Disable lipsync by microphone.
3. (Middle effect): Disable shadow, and wind.
4. (Small effect): Hide unused devices.

In setting window,

1. (Middle effect): `Effects` tab, set `Quality` to lower option.
2. (Small effect): `Layout` tab, disable gamepad input capture and MIDI input reading. 
3. (Small effect): `Effects` tab, set `Image Quality` to lower one.
4. (Small effect): `Effects` tab, set `Bloom`'s `Intensity` to 0.

#### Consider How to Place the Avatar
{: .doc-sec2 }

When placing the avatar please consider NOT to show the avatar hands or arms.

As usual VTuber shows only upper than shoulder, the hand motion might lead unnatural image.

Even in this case shoulder and upper-arm motion is reflected to the streaming, and this makes avatar more lively.

If the streaming is mainly for the talking, you can choose the layout the camera is hidden.

When you are planning the game play streaming, then move gamepad to the higher position, or just change camera layout to show your hands.

If you want to apply bust-up layout and also want to show the hand motion, then move up keyboard and touch pad.

#### Choose how to Switch the Face Expressions
{: .doc-sec2 }

You can use several way to switch the avatar's face expressions.

1. Keyboard, word input
2. Keyboard, number key
3. Gamepad
4. MIDI controller

Please see the detail in [Expressions](../../docs/expressions).
