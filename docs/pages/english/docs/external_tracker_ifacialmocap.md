---
layout: page
title: Connect to iFacialMocap
permalink: /en/docs/external_tracker_ifacialmocap
lang_prefix: /en/
---

[Japanese](../../docs/external_tracker_ifacialmocap)

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
{% include docimg.html file="./images/docs/ex_tracker_ifm_ip_address.png" customclass="col l6 m6 s12" imgclass="fit-doc-img" %}
</div>

Leave the iOS device with iFacialMocap is opened, and put it on the stable place.

Go to PC and `Ex Tracker` tab > `Connect to App` > select `iFacialMocap`.

Then input the IP address shown in iOS device, and click `Connect` to complete connection.

<div class="row">
{% include docimg.html file="./images/docs/ex_tracker_ifm_control_panel_setup.png" customclass="col l6 m6 s12" imgclass="fit-doc-img" %}
</div>

**IMPORTANT:** If you cannot check avatar motion, please check below `TroubleShooting`, especially Q1 and Q2.

If your avatar looks wrong orientatoin or face motion does not start, execute `Cralibrate Face Pose` to calibrate.

<div class="row">
{% include docimg.html file="./images/docs/ex_tracker_20_calibration_before.png" customclass="col l4 m6 s12" imgclass="fit-doc-img" %}
{% include docimg.html file="./images/docs/ex_tracker_30_calibration_after.png" customclass="col l4 m6 s12" imgclass="fit-doc-img" %}
</div>

### Troubleshooting

#### Q1. Failed to connect for the first setup
{: .doc-sec2 }

A. This issue happens by Windows firewall setting, which block LAN communication between PC and iOS device.

Please quit VMagicMirror and iFacialMocap to retry setup.

In the next boot of VMagicMirror, you will see the firewall permission dialog.

<div class="row">
{% include docimg.html file="./images/docs/ex_tracker_firewall_dialog.png" customclass="col l4 m4 s12" imgclass="fit-doc-img" %}
</div>

`Allow Access` and retry setup to establish connection.


#### Q2. After Q1 setup still fails to connect
{: .doc-sec2 }

A. This case also will be related to Windows firewall setting.

Open Firewall settings in Windows control panel and open security management window.

In the `Receive Rule`, find `vmagicmirror.exe` and open `Properties`.

Check `Allow connection` and `OK` to close. After setup, confirm the left side mark of `vmagicmirror.exe` becomes green check.

<div class="row">
{% include docimg.html file="./images/tips/firewall_open_settings.png" customclass="col l4 m4 s6" imgclass="fit-doc-img" %}
{% include docimg.html file="./images/tips/firewall_open_property.png" customclass="col l4 m4 s6" imgclass="fit-doc-img" %}
{% include docimg.html file="./images/tips/firewall_allow_connection.png" customclass="col l4 m4 s6" imgclass="fit-doc-img" %}
</div>

**NOTE:** If you find multiple `vmagicmirror.exe` items in the setting, please setup for all.

#### Q3. Head moves, but face expression does not change
{: .doc-sec2 }

A. iFacialMocap setting might ignore face blendshape data.

Please see iFacialMocap and check `Lower` and `Upper` options are turned on.

<div class="row">
{% include docimg.html file="./images/docs/ex_tracker_ifm_part_setting.png customclass="col l4 m4 s6" imgclass="fit-doc-img" %}
</div>

If the mouth motion still have issues, please also check [External Tracker App](../external_tracker) and see `Use LipSync with External Tracking App` section.

If the above processes does not solve the problem, please try other VRM model, to see the model has any setup issues.


#### Q4. Do I also have to download iFacialMocap Window software?
{: .doc-sec2 }

A. No, because VMagicMirror directly connects to iOS device.

Please avoid to start iFacialMocap windows software during using VMagicMirror, as it will lead to data receive communication issue.


#### Q5. Is there something to be careful in 2nd or later use?
{: .doc-sec2 }

A. If you have put the iOS device other place than previous time, avatar maybe looks left or right. In this case please go through calibration process again.


#### Q6. iOS device works incorrectly, how to recover?
{: .doc-sec2 }

A. Quit `iFacialMocap` app, and try `Connect to VMagicMirror` process again.
