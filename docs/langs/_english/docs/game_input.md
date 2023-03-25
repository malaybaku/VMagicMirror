---
layout: page
title: Game Input
lang: en
---

# Game Input

This page is about Game Input feature, available in v3.1.0 and later version.

#### What is Game Input Feature?
{: .doc-sec2 }

Game Input feature is an option of how to move you avatar during playing game.

By default, the avatar grips gamepad or arcade stick objects and controls it with you.

Game Input feature provides another option that, avatar moves rather like in-game character than the player.

<div class="note-area" markdown="1">

**NOTE**

This feature is in beta phase. Several improvements (e.g. additional action) are planned in future version.

</div>

#### How to Use
{: .doc-sec2 }

In `Streaming` tab > `Motion` > `Body Motion Style`, select `Game Input` to enable the feature.

The feature supports following input by default.
If your avatar does not move according to the input, check the next `Note` section.

Gamepad:

<div class="doc-ul" markdown="1">

- Left stick: Move
- Right stick: Look around
- Right trigger: Gun trigger action
- `A` button: Jump

</div>

Keyboard:

<div class="doc-ul" markdown="1">

- WASD, or arrow keys: Move
- Space: Jump
- Mouse move: Look around

</div>

#### Note
{: .doc-sec2 }

<div class="doc-ul" markdown="1">

- If the avatar does not move during playing the game, check the avatar still does not move when the game is turned off.
    - Some games prohibit capturing device input during the game is active, to prevent cheat tools.
- If gamepad input seems not recognized, check setting window > `Device` tab > `Gamepad` > `Enable gamepad input capture`. If you use Dual Shock 4, then turn on `Use Direct Input`.
- When Game Input is active, some motion in Word to Motion might not work correctly.

</div>

#### Settings
{: .doc-sec2 }

<div class="doc-ul" markdown="1">

Common: 

- `Always Run`: When turned on, always run instead of walking. If you want to switch between walk and run by some inputs, turn off this option.

Gamepad:

- `Enable Gamepad`: Turn on to use gamepad as game input. Turn off only when you have unused connected gamepad and its stick is recognized unexpectedly.
- Stick / DPad: You can assign `Move` or `Look Around` for these input.
- Button: Assign action like `Run`, `Punch`, etc.

Keyboard / Mouse:
- `Enable Keyboard / Mouse`: Turn on to use keyboard / mouse for game input.
- `Use WASD to move`: Turn on to use WASD key as move input. 
- `Use ARROW to move`: Turn on to use UP/DOWN/LEFT/RIGHT keys as move input.
- `Use SHIFT to Run`: Turn on to use shift key for run.
- `Use SPACE to Jump`: Turn on to use space key to jump.
- `Use mouse move to look around`: Turn on to use mouse move input as look around.
- `Left Click`: Assign an action to mouse left click. Available actions are same as gamepad button.
- `Right Click`: Assign an action to mouse right click.
- `Middle Click`: Assign an action to mouse middle click.

</div>
