---
layout: page
title: Face Tracking
lang: en
---

# Face Tracking
{: .no_toc }

In v4.0.0 and later, VMagicMirror provides face tracking option from `Face Tracker` tab in control panel.

(todo: タブのスクリーンショット)

<div class="toc-area" markdown="1">

#### Content
{: .toc-header .no_toc }

* ToC
{:toc .table-of-contents }

</div>

### Supported Tracking Options
{: .doc-sec1 }

Following tracking options are available.

<div class="doc-ul" markdown="1">

- `Web Camera (Lite)`: Low PC loaded face tracking with web camera.
- `Web Camera (High Power)`: High PC loaded face tracking with web camera.
- `Ex. Tracker`: Face tracking with iOS app (iFacialMocap). If you have supported device, the option will perform best while keeping low PC load.

</div>

You can also disable web camera with web camera tracking option selected. In this case, you will get very low PC load.

<div class="note-area" markdown="1">

**NOTE**

There are also available tracking way to use VMC Protocol data receive, though the setup steps might be more complicated. Please see detail at  [VMC Protocol](./vmc_protocol).

</div>

### Setup for each Tracking
{: .doc-sec1 }

This section shows how each face tracking will work and what tracking settings are available

Note that, avatar's facial settings is also in [Face](./face/) tab in setting window.


#### Web Camera (Lite)
{: .doc-sec2 }

`Web Camera (Lite)` is an option to use face tracking with web camera, while avoiding high PC load. The option is useful if your PC environment has another content like games.

(todo: screen shot)

`Web Camera (Lite)` does not use blink or lipsync data by web camera input. Head motion and microphone input will generate blink and lipsync behaviors.

In this mode, only simple options are available.

<div class="doc-ul" markdown="1">

- `Use Web Camera`: Turn on to use web camera for tracking. Turn off to reduce PC load.
- `Disable horizontal flip`: Turn on to disable horizontal flip when applying tracking result.

</div>


#### Web Camera (High Power)
{: .doc-sec2 }

`Web Camera (High Power)` mode performs high quality face tracking, while using more PC load than `Web Camera (Lite)` mode. This mode is recommended if your PC resources can run high loaded multiple apps, or VMagicMirror is the only performance-critical app to run on the PC.

(todo: screenshot)

This tracking gets blink and mouth facial data based on web camera input.

Following options are available.

<div class="doc-ul" markdown="1">

- `Use Web Camera`: Turn on to use web camera for tracking. Turn off to reduce PC load.
- `Disable horizontal flip`: Turn on to disable horizontal flip when applying tracking result.
- `Enable Forward/Backward Move`: Turn on to allow move avatar forward and backward(*).
- `Apply Lip Sync based on Web Camera`: Turn on to apply mouth motion based on web camera input. When turned off, microphone input based lipsync value is applied instead.
- `Use Perfect Sync`: Apply the facial data as Perfect Sync blendshape values. Please see detail at [Perfect Sync](../tips/perfect_sync).

</div>

*`Enable Forward/Backward Move` option does almost nothing if the avatar touches virtual keyboard, gamepad etc. To utilize this option, confirm that `Standing Only` option is selected at `Streaming` tab  > `Motion` > `Body Motion Style`.

Also, this tracking mode support `Blink Tracking Settings` window.

(todo: screenshot)

This settings window handles how to handle `Blink Value` which is a part of face tracking result in `Web Camera (High Power)`.

`Blink Value` is a value which get small when your eyes are open, and get large when eyes are closed. The value range is between 0 and 100, though the actual range vary depends o indivisual differences, webcam placement, etc.

<div class="doc-ul" markdown="1">

- `Blink Value when Eye Opened (%)`: Specify Blink Value when eyes are open. Typically 0~30 is appropriate value.
- `Blink Value when Eye Closed (%)`: Specify Blink Value when eyes are closed. Typically 40~70 is appropriate value.
- `Use Mean Blink value`: Turn on to treat mean Blink Value, `(Left + Right) / 2`, for both eye. Use this option especially when you want to move avatar's both eye same. Note that the option disables tracking wink facial correctly.
- `Use Adjusted Value when Perfect Sync is Enabled`: Turn on to apply value adjust calculation when Perfect Sync option is active. Enable the option when the avatar's blinking does not move enough.
- `Show Blink Value Preview`: Turn on to see actual Blink Value for both eyes.
  - `Reset Preview` buttons resets min/max value result. 

</div>

By using `Show Blink Value Preview` option, you can adjust other options based on blink value's range.

<div class="doc-ul" markdown="1">

- `Blink Value when Eye Opened (%)`: Specify Blink Value larger than min value in preview.
- `Blink Value when Eye Closed (%)`: Specify Blink Value smaller than max value in preview.
- If both values get close, then `half-open` state become rare and blink open/close motion will be clear.

</div>

For example, following screenshot shows `Min`=5~10, and `Max`=65~80.

(todo: screenshot)

In such case, there are some setup patterns.

<div class="doc-ul" markdown="1">

- To apply several tracking result including half-opened eyes:
    - `Blink Value when Eye Opened (%)`: 20
    - `Blink Value when Eye Closed (%)`: 50
- To apply blink motion clearly:
    - `Blink Value when Eye Opened (%)`: 35
    - `Blink Value when Eye Closed (%)`: 40

</div>



#### Ex.Tracker
{: .doc-sec2 }

External Tracker is a feature to collaborate with iOS app [iFacialMocap](https://apps.apple.com/jp/app/ifacialmocap/id1489470545).

If you have supported device, the option will perform very good tracking result while keeping low PC load.

See detail at  [Exteral Tracker App](./external_tracker) and [Connect to iFacialMocap](./external_tracker_ifacialmocap). 


### Face Switch
{: .doc-sec1 }

Face Switch is a feature to switch avatar's face by user expression.

<div class="row">
{% include docimg.html file="./images/docs/ex_tracker_50_face_switch.png" customclass="col l6 m6 s12" imgclass="fit-doc-img" %}
</div>

Face switch has parameters to setup.

<div class="doc-ul" markdown="1">

- `Threshold`: Select from 10% to 90%, to specify when the face switch is triggered. Higher value means you have to more clear expression.
- `BlendShape`: Choose the BlendShape to apply, or select `(Do Nothing)`(*) as empty selection.
- `Keep LipSync`: You can check it for the BlendShape with only eye motion, so the LipSync still work.

*`(Do Nothing)` indication might incorrect appearance as Japanese expression "`(何もしない)`". In this case you can choose `(何もしない)`.

Note that some face expressions are difficult to be recognized.

Also note that, this feature is not an extension of face tracking, but also considerable as shortcut key assignment via your face.

This means irrelevant assignment will be still useful.

For example, you can assign a special face expression for tongue out condition, even if the expression does not include tongue out motion at all.

<div class="note-area" markdown="1">

**NOTE**

`Word to Motion` feature has higher priority. When face switch and `Word to Motion` input runs simultaneously, then `Word to Motion` output is applied.

</div>

Also v2.0.2 and later version support accessory visibility control during Face Switch is applied.

Check `Show Accessory Option` to show accessory visibility selection.

See [Accessory](../accessory) page for the detail of accessory feature.
