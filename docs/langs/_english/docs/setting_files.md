---
layout: page
title: Setting Files
permalink: /en/docs/setting_files
lang: en
---

[Japanese](../../docs/setting_files)

# Setting Files

This page is about setting file features.

Basic feature is in control panel `Home` tab, and advanced feature is available from setting window `File` tab.

<div class="row">
{% include docimg.html file="/images/docs/setting_files_top_home.png" customclass="col s12 m6 l6" imgclass="fit-doc-img" %}
{% include docimg.html file="/images/docs/setting_files_top_tab.png" customclass="col s12 m6 l6" imgclass="fit-doc-img" %}
</div>

### When Setting File Management is Needed
{: .doc-sec2 }

First, you do not have to use this feature in many of the cases, because settings are automatically saved.

You should use this feature when you are working with several models, for example when:

<div class="doc-ul" markdown="1">

- There are same character but different clothes models, and you want to switch them quickly
- There are characters with different proportion or face settings, and you want to switch them quickly

</div>


### Basic Operation
{: .doc-sec2 }

Basic feature is available in control panel `Home` tab.

Click `Save` or `Load` to save current setting, or load saved data.

<div class="row">
{% include docimg.html file="/images/docs/setting_files_save.png" customclass="col s12 m6 l6" imgclass="fit-doc-img" %}
{% include docimg.html file="/images/docs/setting_files_load.png" customclass="col s12 m6 l6" imgclass="fit-doc-img" %}
</div>

In `Load` you also can select auto save data.

<div class="note-area" markdown="1">

**NOTE**

Auto save is special data, which is automatically saved data when VMagicMirror quits.

When VMagicMirror starts, auto save data is loaded as the default data.

</div>

`Load` has options `Load Character` and `Load Non-Character`.

When check only `Load Character`, only the character is applied in saved data. It is default option, and is useful to switch save character with different clothes.

When check `Load Non-Character`, other settings like face tracking and layout etc. will be loaded.

When both option is unchecked, the data will not be loaded.

<div class="note-area" markdown="1">

**NOTE**

`Load` feature skips license check for local VRM file, as you have already checked it.

Please reload the same model to show license if you are unclear about license detail.

VRoid Hub model always requires license confirmation, because the license may be changed in server side.

</div>

`Export` and `Import` is extra features to save and load setting as a file in any folder.

`Export` does not save character info, so `Import` only load non-character settings.


### Advanced Feature: Automation
{: .doc-sec2 }

<div class="note-area" markdown="1">

**NOTE**

This feature requires programming or network knowledge background.

You must use only models with license controlled model, as local VRM file.

VRoid Hub models are not supported in this feature.

</div>

With automation feature and UDP messaging, you can load setting file withouy GUI operation.

Automation can be enabled in setting window `File` tab, > `Automation` > `Enable Automation` button.

Confirm the dialog content, and you will also need to allow firewall in first time.

If you do not allow network process in firewall setting, you will have to custom it in Window control panel.

Detail is similar to [iFacialMocap connection Troubleshoot](./external_tracker_ifacialmocap#troubleshoot) Q1 and Q2.


After enable the automation, send JSON text message to load data.

Following is an example of JSON message to send.

```
{
    "command": "load_setting_file",
    "args": 
    {
        "index": 1,
        "load_character": true,
        "load_non_character": false
    }
}
```

You can change 3 parameters in `args`.

<div class="doc-ul" markdown="1">

- `index`: Data index to load. specify an integer value between 1 and 15.
- `load_character`: Equivalent to `Load Character` check in basic load GUI. Set `true` or `false`.
- `load_non_character`: Equivalent to `Load Non-Character` check in basic load GUI. Set `true` or `false`.

</div>

