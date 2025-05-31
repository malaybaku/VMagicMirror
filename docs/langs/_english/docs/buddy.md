---
layout: page
title: Buddy
lang: en
---

# Buddy

`Buddy` is a feature available in VMagicMirror v4.0.0 and later.

This feature shows buddy character in addition to 3D avatar displeyed with basic usage.

In this page, the 3D model normally displayed is referred to as `main avatar`, so that buddy and main avatar is clearly distinguished.

<div class="row">
{% include docimg.html file="/images/docs/buddy_top.png" customclass="col s12 m6 l6" imgclass="fit-doc-img" %}
</div>

<div class="note-area" markdown="1">

**NOTE**

VMagicMirror v4.0.0 supports image file based buddy.

Though v4.0.0 can load and show 3D data, the API is limited and they might changes largely in future updates.

</div>

#### Features
{: .doc-sec2 }

Buddy has following features.

<div class="doc-ul" markdown="1">

- Show the character with image file(`.png`), to put them on the front of the avatar window, or in the 3D space same as main avatar.
- The buddy behavior can be made with user-defined `C#` script.
- The script API helps to make reactions for main avatar's action, or raw user actions.

</div>

<div class="note-area" markdown="1">

**NOTE**

Supprt for 3D data (VRM, glb) based buddy is under development.

</div>


#### Limitations
{: .doc-sec2 }

In Standard Edition, the script APIs to make reaction for main avatar and user, which is called as `Interaction API`, is off by default. During the option is enabled, special post-process effect is applied.

This will stop some buddy's behaviors, and following are examples.

<div class="doc-ul" markdown="1">

- Mimic main avatar's blink and facial expressions
- React to user's microphone input
- React to user's keyboard and gamepad inputs

</div>

Please get Full Edition to use the feature without limitation. Please see detail in [Download](../../download) page and [(BOOTH)VMagicMirror Full Edition](https://baku-dreameater.booth.pm/items/3064040).


#### Start to use Buddy
{: .doc-sec2 }

Open `Buddy` tab and enable the buddy to start.

In v4.0.0 and later version, there are also built-in buddies.

You can edit buddy's placement in avatar window via `Free Layout` option.

Note that, there is a case that 2D image buddy gets too far from the center of window, and cannot change their layout by `Free Layout` feature. In such case, you can open buddy's setting to edit buddy's position like position `x`, `y` directly.

The coordinate for the buddy displayed in the foreground has size approximately `1280 x 720`, with left bottom point is `(x, y) = (0, 0)`.

Following options have effects to all buddies.

<div class="doc-ul" markdown="1">

- `Use Interaction API`: Turn on the option to enable APIs, by which buddies can make reactions to main avatar and user. The option is on by default in Full Edition, while off by default in Standard Edition.
- `Open Folder`: Open the folder where to place user-defined buddy.
- `Reload All`: Reload all buddies settings again. Use this function when you have placed new buddy by `Use Distributed Buddy` and want to load them without restarting VMagicMirror.
- `Developer Mode`: Turn on when you are developing your own buddy. Please see [Developer Doc](https://malaybaku.github.io/VMagicMirrorBuddyDoc/) for detail.

</div>

Other settings are defined for each buddies. Please check buddy's help for detail.


#### Use Distributed Buddy
{: .doc-sec2 }

You can introduce new buddy. Here is an example steps for the case, that buddy data is distributed as `.zip` data.

<div class="doc-ul" markdown="1">

- Check the zip file's property and update security settings (Zone Identifier) if needed.
- Unzip the data.
- In file explorer, open `(My Document)\VMagicMirror_Files\Buddy\` folder.
- Place the unzipped folder.

</div>

Folder structures must be like following. Be careful that unzip files might form double folder.

<div class="doc-ul" markdown="1">

- `(My Document)\VMagicMirror_Files\Buddy\{Folder_Name}\`
  - `main.csx`
  - `manifest.json`
  - ...

</div>

<div class="row">
{% include docimg.html file="/images/docs/buddy_folder_structure.png" customclass="col s12 m6 l6" imgclass="fit-doc-img" %}
</div>

Also, the folder name should be kept same after the buddy introduced. When the folder name changed, then the buddy's setting will be reset to default. 


#### Create own Buddy
{: .doc-sec2 }

If you want to create your own buddy or distribute them, please see the documentation page about VMagicMirror Buddy APIs.

[Developer Doc](https://malaybaku.github.io/VMagicMirrorBuddyDoc/)

*Note that the doc is only in Japanese.

The page shows simple template buddy data and the list of the script APIs available.

<div class="note-area" markdown="1">

**NOTE**

In v4.0.0, it is expected that creating your own buddy will require advanced programming skills.

I am planning to create and distribute templates that allow you to use the scripts as they are, on the documentation page mentioned above and on BOOTH.

</div>


