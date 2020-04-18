---
layout: page
title: 仮想カメラを使う
permalink: /tips/virtual_camera/
---

[English](../en/tips/virtual_camera)

# Tips: 仮想カメラを使う

仮想カメラはv0.9.9以降でサポートされている機能で、VMagicMirrorの画面をウェブカメラの映像として扱える機能です。

機能は限られていますが、ウェブ会議で手軽にVMagicMirrorを使うことができます。

※カメラ解像度の調整や背景の差し替えなど、高度な用途については他ソフトとの組み合わせを検討してください。一例として、[VMagicMirror製作者のブログ記事](https://www.baku-dreameater.net/entry/2020/02/22/165157)ではOBSを用いた方法を紹介しています。


#### セットアップ: 初使用時にやること
{: .doc-sec2 }

初めて仮想カメラ機能を使うときは、準備として仮想カメラのインストールを行います。

`配信`タブの`仮想カメラ`から、`※初めて使う場合`を選択します。

{% include docimg.html file="/images/tips/virtual_camera_first_setup.png" %}

インストール操作の説明ダイアログが表示されます。`フォルダを開く`でフォルダを開き、`Install.bat`をダブルクリックで実行します。

{% include docimg.html file="/images/tips/virtual_camera_run_bat.png" %}

以下のようなダイアログが表示されれば成功です。

{% include docimg.html file="/images/tips/virtual_camera_success_dialog.png" %}

`OK`でダイアログを閉じたのち、インストール操作のダイアログも閉じればセットアップは完了です。


#### 仮想カメラを会議ソフト等で使う
{: .doc-sec2 }

`配信`タブで`仮想カメラ出力を有効化`をオンにしたのち、対象のソフトで`Unity Video Capture`を選択します。

以下はZoomでカメラを選択している例です。

{% include docimg.html file="/images/tips/virtual_camera_camera_select_example.png" %}

カメラ一覧に`Unity Video Capture`が表示されない場合、一度対象ソフト(上記の場合ならZoom)を閉じて再び起動して下さい。

何度繰り返してもうまく行かない場合、VMagicMirrorやWindows自体の再起動をしたり、他のアプリケーションで動くかどうかをご確認下さい。

※ウェブカメラを扱うソフトの一部は仮想カメラをサポートしていません。

また、もし画像が映らない場合、VMagicMirror上の設定で解像度が幅`640`、高さ`480`になっているか確認し、異なる値であれば`640`x`480`に設定します。カメラ映像を使っているアプリケーション側でもカメラ解像度が明示的に選べる場合、`640x480`を選択して下さい。

画像が映っているものの引き伸ばされて見える場合は`リサイズ`ボタンを押してVMagicMirrorのウィンドウサイズを修正します。





#### 仮想カメラの解像度制限について

VMagicMirrorの仮想カメラは基本となる解像度が幅`640`、高さ`480`で固定となっています。

これ以外の解像度を設定した場合、ソフトによってはカメラ入力を取得できなくなります。その場合、解像度を`640`x`480`に戻して下さい。

