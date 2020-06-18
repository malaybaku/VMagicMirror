---
layout: page
title: Connect to iFacialMocap
permalink: /en/tips/external_tracker_ifacialmocap
lang_prefix: /en/
---

[English](../../tips/external_tracker_ifacialmocap)

# Connect to iFacialMocap

Show how to setup iFacialMocap for [Using External Tracker App](./external_tracker).


#### What is iFacialMocap?
{: .doc-sec2 }

iFacialMocap is a paid application for face tracking in iOS.

This required Face ID supported devices. See the following page to get what devices are supported.

[Face ID に対応している iPhone と iPad のモデル](https://support.apple.com/en-us/HT209183)

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
{% include docimg.html file="./images/tips/ex_tracker_ifm_control_panel_setup.png" customclass="col l6 m6 s12" imgclass="fit-doc-img" %}
</div>

If your avatar looks wrong orientatoin please execute `Cralibrate Face Pose` to calibrate face position.

<div class="row">
{% include docimg.html file="./images/tips/ex_tracker_20_calibration_before.png" customclass="col l4 m6 s12" imgclass="fit-doc-img" %}
{% include docimg.html file="./images/tips/ex_tracker_30_calibration_after.png" customclass="col l4 m6 s12" imgclass="fit-doc-img" %}
</div>


#### Troubleshooting
{: .doc-sec2 }

##### Q1. セットアップが正しいはずなのに接続に失敗する

A. Windowsファイアウォールの設定によって、VMagicMirrorとiOSの通信が不許可となっている可能性があります。

Windowsのコントロールパネルで`ファイアウォール`を検索して`Windows Defender ファイアウォール`項目を開き、`詳細設定`を選びます。

表示されたセキュリティ管理ウィンドウで`受信の規則`から`vmagicmirror.exe`を探し、`プロパティ`でウィンドウを開きます。

`接続を許可する`を選択して`OK`でウィンドウを閉じ、`vmagicmirror.exe`の左側が緑色のチェックマークになれば設定完了です。

<div class="row">
{% include docimg.html file="./images/tips/firewall_open_settings.png" customclass="col l4 m4 s6" imgclass="fit-doc-img" %}
{% include docimg.html file="./images/tips/firewall_open_property.png" customclass="col l4 m4 s6" imgclass="fit-doc-img" %}
{% include docimg.html file="./images/tips/firewall_allow_connection.png" customclass="col l4 m4 s6" imgclass="fit-doc-img" %}
</div>

**NOTE:** `受信の規則`に複数の`vmagicmirror.exe`があった場合、すべてに対して同じ操作を行ってください。


##### Q2. Window用に配布されているiFacialMocapのソフトは必要？

A. 不要です。VMagicMirror自体がiOS端末と直接通信するためです。

もしWindows用のiFacialMocapソフトをPCへインストール済みの場合、VMagicMirrorの使用中には立ち上げないよう注意してください。


##### Q3. 2回目以降の使用時に注意することは？

A. 前回の使用時とことなる位置にiPhoneやiPadを置いた場合、キャリブレーションをやり直す必要があります。


##### Q3. iOS端末の調子が悪い

A. `iFacialMocap`のアプリを完全に終了したのち、`VMagicMirrorと接続する`の手順に沿って再度接続してください。

