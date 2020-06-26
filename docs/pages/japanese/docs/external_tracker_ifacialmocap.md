---
layout: page
title: iFacialMocapとの連携
permalink: /docs/external_tracker_ifacialmocap
---

[English](../en/docs/external_tracker_ifacialmocap)

# iFacialMocapとの連携

[外部トラッキングアプリとの連携](./external_tracker)のうち、特にiFacialMocapとVMagicMirrorを連携する方法にかんするページです。


#### iFacialMocapとは
{: .doc-sec2 }

iFacialMocapはiOS向けに有償で配布されている表情キャプチャアプリです。

利用にはFace ID対応の端末が必要です。Face IDの対応機種は以下ページで確認できます。

[Face ID に対応している iPhone と iPad のモデル](https://support.apple.com/ja-jp/HT209183)

iFacialMocapはApp Storeで購入、インストールできます。

[iFacialMocap](https://apps.apple.com/jp/app/ifacialmocap/id1489470545)


#### VMagicMirrorと接続する
{: .doc-sec2 }

iFacialMocapを起動し、画面上部にiOS端末自身のIPアドレスが表示されることを確認します。

<div class="row">
{% include docimg.html file="./images/docs/ex_tracker_ifm_ip_address.png" customclass="col l6 m6 s12" imgclass="fit-doc-img" %}
</div>

iFacialMocapを開いたまま、安定する場所にiOS端末を設置します。

PC画面に戻り、`Ex Tracker`タブ > `アプリとの連携`で、`iFacialMocap`を選択します。

iOS端末のアプリ上に表示されたIPアドレスを入力し、`Connect`ボタンをクリックすると接続します。

<div class="row">
{% include docimg.html file="./images/docs/ex_tracker_ifm_control_panel_setup.png" customclass="col l6 m6 s12" imgclass="fit-doc-img" %}
</div>

**重要: ここでキャラクターが反応しない場合、本ページ下部のトラブルシューティングでQ1およびQ2を確認してください。**

接続後にアバターが横を向いてしまう場合や首が動かない場合は、ユーザーが真正面を向いた状態で`現在位置で顔をキャリブレーション`をクリックし、アバターの顔の向きをリセットします。

<div class="row">
{% include docimg.html file="./images/docs/ex_tracker_20_calibration_before.png" customclass="col l4 m6 s12" imgclass="fit-doc-img" %}
{% include docimg.html file="./images/docs/ex_tracker_30_calibration_after.png" customclass="col l4 m6 s12" imgclass="fit-doc-img" %}
</div>


#### トラブルシューティング
{: .doc-sec2 }

##### Q1. はじめて接続操作を行ったが、キャラクターが反応しない

A. この問題はWindowsファイアウォールによって、PCとiOSデバイス間の通信がブロックされると発生します。

このばあい、一度VMagicMirrorおよびiFacialMocapを終了し、再び接続を試みてください。

ここで、VMagicMirrorを起動し直すとファイアウォールの設定ダイアログが表示されます。

<div class="row">
{% include docimg.html file="./images/docs/ex_tracker_firewall_dialog.png" customclass="col l6 m6 s12" imgclass="fit-doc-img" %}
</div>

`アクセスを許可する`をクリックし、ふたたび接続を試みることで、接続できます。


##### Q2. Q1に記載されたファイアウォール設定が表示されず、接続に失敗する

A. この場合もWindowsファイアウォールの設定によって、VMagicMirrorとiOSの通信が不許可となっている可能性があります。

Windowsのコントロールパネルで`ファイアウォール`を検索して`Windows Defender ファイアウォール`項目を開き、`詳細設定`を選びます。

表示されたセキュリティ管理ウィンドウで`受信の規則`から`vmagicmirror.exe`を探し、`プロパティ`でウィンドウを開きます。

`接続を許可する`を選択して`OK`でウィンドウを閉じ、`vmagicmirror.exe`の左側が緑色のチェックマークになれば設定完了です。

<div class="row">
{% include docimg.html file="./images/tips/firewall_open_settings.png" customclass="col l4 m4 s6" imgclass="fit-doc-img" %}
{% include docimg.html file="./images/tips/firewall_open_property.png" customclass="col l4 m4 s6" imgclass="fit-doc-img" %}
{% include docimg.html file="./images/tips/firewall_allow_connection.png" customclass="col l4 m4 s6" imgclass="fit-doc-img" %}
</div>

**NOTE:** `受信の規則`に複数の`vmagicmirror.exe`があった場合、すべてに対して同じ操作を行ってください。


##### Q3. Window用に配布されているiFacialMocapのソフトは必要？

A. 不要です。VMagicMirror自体がiOS端末と直接通信するためです。

もしWindows用のiFacialMocapソフトをPCへインストール済みの場合、VMagicMirrorの使用中には立ち上げないよう注意してください。


##### Q4. 2回目以降の使用時に注意することは？

A. 前回の使用時と異なる位置にiPhoneやiPadを置いた場合、顔の方向が間違って表示されます。この場合はキャリブレーションをやり直してください。


##### Q5. iOS端末の調子が悪い

A. `iFacialMocap`のアプリを完全に終了したのち、このページの`VMagicMirrorと接続する`の手順に沿って再び接続してください。

