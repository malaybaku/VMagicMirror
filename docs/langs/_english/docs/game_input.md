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

Game Input feature provides another option to move avatar moves rather like in-game character than the game player.

<div class="row">
{% include docimg.html file="./images/docs/game_input.jpg" customclass="col s12 m6 l6" imgclass="fit-doc-img" %}
</div>


<div class="note-area" markdown="1">

**NOTE**

v3.2.0 has known issue about Game Locomotion with custom motion.

In Game Locomotion mode, there are some cases that avatar keeps standing pose and pose will not change. In this case, see `Streaming` tab > `Motion` > `Body Motion Style` and choose `Default` or `Standing Only`, and then re-choose `Game Locomotion` to recover the avatar's pose.

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

- `Locomotion Style`: Change how to move to each direction.
    - `First Person` keeps body forward. 
    - `Third Person` makes avatar face to the direction where to move.
    - `Side-Scrolling like` is similar to `Third Person` but this option makes avatar only 2 direction, left or right.
- `Run by Default`: When turned on, always run instead of walking. When this option is enabled, `Run` action button makes avatar walk.

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
- `Additional Key Assign`: Assign any key to the actions, by select key input field right to the action and press key. Reset button can clear the key setting assigned to the action.

</div>


#### Use Custom Motion in Game Input
{: .doc-sec2 }

v3.5.0 and later version supports custom motion based on [VRM Animation](../../tips/use_vrma) (.vrma) file. See detail at [VRM Animation](../../tips/use_vrma) page.

This feature supports one shot motion like punch or jump, which acts some single action and return to default pose.

Note that v3.5.0 and current version does not support to replace move motion, or playing some motion with loop.


#### Load and Save Game Input Setting
{: .doc-sec2 }

By default, game input setting is saved when VMagicMirror quits.

If you have several games and want to switch input settings quickly, then use `Export` to export the setting as `.vmm_gi` file, and load it by `Import` button.

<div class="note-area" markdown="1">

**NOTE**

Game Input setting is saved at different file from main setting file. Load or save main setting file does not affect game input config.

</div>
