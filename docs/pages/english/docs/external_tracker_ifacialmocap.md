---
layout: page
title: Connect to iFacialMocap
permalink: /en/docs/external_tracker_ifacialmocap
lang_prefix: /en/
---

[English](../../docs/external_tracker_ifacialmocap)

# Connect to iFacialMocap

Show how to setup iFacialMocap for [External Tracker App](./external_tracker).


#### What is iFacialMocap?
{: .doc-sec2 }

iFacialMocap is a paid application for face tracking in iOS.

This required Face ID supported devices. See the following page to get what devices are supported.

[iPhone and iPad models that support Face ID](https://support.apple.com/en-us/HT209183)

iFacialMocap is available on App Store.

[iFacialMocap](https://apps.apple.com/jp/app/ifacialmocap/id1489470545)


#### Connect to VMagicMirror
{: .doc-sec2 }

Start iFacialMocap and see the IP address at the top.

<div class="row">
{% include docimg.html file="./images/tips/ex_tracker_ifm_ip_address.png" customclass="col l6 m6 s12" imgclass="fit-doc-img" %}
</div>

Leave the iOS device with iFacialMocap is opened, and put it on the stable place.

Go to PC and `Ex Tracker` tab > `Connect to App` > select `iFacialMocap`.

Then input the IP address shown in iOS device, and click `Connect` to complete connection.

<div class="row">
{% include docimg.html file="./images/docs/ex_tracker_ifm_control_panel_setup.png" customclass="col l6 m6 s12" imgclass="fit-doc-img" %}
</div>

If your avatar looks wrong orientatoin please execute `Cralibrate Face Pose` to calibrate face position.

<div class="row">
{% include docimg.html file="./images/docs/ex_tracker_20_calibration_before.png" customclass="col l4 m6 s12" imgclass="fit-doc-img" %}
{% include docimg.html file="./images/docs/ex_tracker_30_calibration_after.png" customclass="col l4 m6 s12" imgclass="fit-doc-img" %}
</div>


#### Troubleshooting
{: .doc-sec2 }

##### Q1. Fails to connect

A. Windows Firewall might block the connection between the PC and iOS device.

Open Firewall settings in Windows control panel and open security management window.

In the `Receive Rule`, find `vmagicmirror.exe` and open `Properties`.

Check `Allow connection` and `OK` to close. After setup, confirm the left side mark of `vmagicmirror.exe` becomes green check.

<div class="row">
{% include docimg.html file="./images/tips/firewall_open_settings.png" customclass="col l4 m4 s6" imgclass="fit-doc-img" %}
{% include docimg.html file="./images/tips/firewall_open_property.png" customclass="col l4 m4 s6" imgclass="fit-doc-img" %}
{% include docimg.html file="./images/tips/firewall_allow_connection.png" customclass="col l4 m4 s6" imgclass="fit-doc-img" %}
</div>

**NOTE:** If you find multiple `vmagicmirror.exe` items in the setting, please setup for all.


##### Q2. Do I also have to download iFacialMocap Window software?

A. No, because VMagicMirror directly connects to iOS device.

Please avoid to start iFacialMocap windows software during using VMagicMirror, as it will lead to data receive communication issue.


##### Q3. Is there something to be careful in 2nd or later use?

A. If you have put the iOS device other place than previous time, avatar maybe looks left or right. In this case please go through calibration process again.


##### Q3. iOS device works incorrectly, how to recover?

A. Quit `iFacialMocap` app, and try `Connect to VMagicMirror` process again.
