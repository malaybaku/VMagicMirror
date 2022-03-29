---
layout: page
title: Get Started
permalink: /get_started
---

[English](./en/get_started)

# Get Started
{: .no_toc }

このページでは、`VMagicMirror`の基本的な使い方を紹介します。

セットアップの様子は以下の動画でも紹介していますが、やや古いバージョンを使用していることに注意してください。

<iframe class="youtube" width="560" height="315" data-src="https://www.youtube.com/embed/kYk-YHqPeMU" frameborder="0" allow="accelerometer; autoplay; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>

<div class="toc-area" markdown="1">

#### 目次
{: .toc-header .no_toc }

* ToC
{:toc .table-of-contents }

</div>

### 1. 起動してキャラクターを表示する
{: .doc-sec1 }

ダウンロードしたファイルの起動方法はバージョンによってやや異なります。

使用されるバージョンによって、`1-1. v1.9.0以降のバージョン`または`v1.8.2以前のバージョンの場合`のいずれかの手順を確認してください。


#### 1-1. v1.9.0以降のバージョンの場合
{: .doc-sec2 }

v1.9.0以降ではインストーラー形式で`VMagicMirror`を配布しています。
zipを解凍して内部のインストーラファイルをダブルクリックで実行し、指示に従ってインストールします。

インストール後、スタートメニューやデスクトップショートカット等から`VMagicMirror`を実行します。

<div class="note-area" markdown="1">

**NOTE**

インストーラの実行がブロックされる場合、zipファイルまたは解凍後のインストーラ(exe)のセキュリティ設定を確認します。

ファイルを右クリックして`プロパティ`を選び、`セキュリティ`の項目があるか確認します。

もし項目があれば`許可する`をチェックして`OK`で変更を適用します。その後、再びインストーラーを実行します。

<div class="row">
{% include docimg.html file="./images/get_started/img00_004_remove_block_of_installer.jpg" customclass="col l6 m6 s12" imgclass="fit-doc-img" %}
</div>

</div>


#### 1-2. v1.8.2以前のバージョンの場合
{: .doc-sec2 }

zipファイルを解凍し、適当なフォルダに配置したのちフォルダ内の`VMagicMirror.exe`を実行します。

<div class="note-area" markdown="1">

**NOTE**

`VMagicMirror.exe`が正常に起動しない場合、zipの解凍方法を確認して下さい。

zipファイルを右クリックして`プロパティ`を選び、`セキュリティ`の項目があるか確認します。

もし項目があれば`許可する`をチェックして`OK`で変更を適用します。その後、再びzipを解凍してください。

<div class="row">
{% include docimg.html file="./images/get_started/img00_005_before_unzip.jpg" customclass="col l6 m6 s12" imgclass="fit-doc-img" %}
</div>

また、zipの解凍先はユーザーが使用する通常のフォルダ(`C:\`やマイドキュメントのフォルダなど)を使用します。

`Program Files`などの特殊なフォルダは避けて下さい。

</div>


#### 1-3. 起動後にモデルを表示する
{: .doc-sec2 }

`VMagicMirror`を起動するとGUIがある「コントロールパネル」と、キャラクターが映る「キャラクターウィンドウ」が立ち上がります。

コントロールパネルかキャラクターウィンドウの一方を閉じると、もう片方の画面も閉じて`VMagicMirror`が終了します。コントロールパネルが邪魔な場合、最小化しておきます。

<div class="row">
{% include docimg.html file="./images/get_started/img00_015_started.jpg" customclass="col s6 m4 l4" imgclass="fit-doc-img" %}
</div>


モデルはPC上のVRMファイルからロードするか、またはVRoid Hubからロードできます。

PC上のVRMファイルをロードする場合、`ホーム`タブの`PC上のファイルをロード`ボタンをクリックします。

`.vrm`ファイルを選択してキャラクターウィンドウに現れる規約を確認します。`OK`をクリックするとキャラクターがロードされます。

<div class="row">
{% include docimg.html file="./images/get_started/img00_020_load_vrm.jpg" customclass="col s12 m6 l4" imgclass="fit-doc-img" %}
{% include docimg.html file="./images/get_started/img00_030_load_vrm_confirmation.jpg" customclass="col s12 m6 l4" imgclass="fit-doc-img" %}
</div>

VRoid Hubのモデルをロードする場合、`VRoid Hubからロード`ボタンをクリックします。

Webブラウザが開くため、指示に従ってアプリ連携を完了し、認可コードを表示します。表示された認可コードをVMagicMirrorの入力欄にペーストし、ログインします。2回目以降は自動でログインします。

その後、使いたいモデルを選び、規約を確認してロードします。利用できるのは自分のモデル、「いいね」したモデル、公式にピックアップされたモデルの3種類です。

<div class="row">
{% include docimg.html file="./images/get_started/img00_032_connect_vroid_hub.jpg" customclass="col s12 m6 l4" imgclass="fit-doc-img" %}
{% include docimg.html file="./images/get_started/img00_034_vroid_hub_characters.jpg" customclass="col s12 m6 l4" imgclass="fit-doc-img" %}
{% include docimg.html file="./images/get_started/img00_037_vroid_hub_confirmation.jpg" customclass="col s12 m6 l4" imgclass="fit-doc-img" %}
</div>

<div class="note-area" markdown="1">

**NOTE**

他者がアップロードしたモデルは「いいね」をしても使用できるとは限りません。

詳しくは[VRoid Hubのアバター利用について](./tips/use_vroid_hub)をご覧下さい。

</div>

ロードしたキャラクターを次回以降も使いたい場合、`ホーム`タブの`次回の起動時にも同じVRMを読み込む`のチェックをオンにします。

<div class="row">
{% include docimg.html file="./images/get_started/img00_040_after_loaded.jpg" customclass="col s12 m6" imgclass="fit-doc-img" %}
</div>

また、この時点でキーボードやタッチパッドの位置、視点がキャラクターに合わない場合、`キャラ体格で補正`ボタンをクリックします。

<div class="row">
{% include docimg.html file="./images/get_started/img00_160_not_good_layout_example.jpg" customclass="col s12 m6" imgclass="fit-doc-img" %}
{% include docimg.html file="./images/get_started/img00_170_after_adjust.jpg" customclass="col s12 m6" imgclass="fit-doc-img" %}
</div>

視点やキーボードの位置などは、後半で紹介する手順でさらに細かく調整できます。


### 2. 配信タブ: メイン機能の使い方
{: .doc-sec1 }

`配信`タブでは`VMagicMirror`のすべての主要機能にアクセスできます。

キャラクターをロードしたら色々な基本機能を試してみましょう。

{% include docimg.html file="./images/get_started/img00_050_streaming_tab.jpg" %}

#### 2.1. ウィンドウ
{: .doc-sec2 }

`ウィンドウ`で`背景を透過`をチェックすると、背景を透明にできます。

背景が透明であり、かつ`(透過中)キャラ付近を掴んでドラッグ`のチェックがオンであれば、キャラクターを`左クリック+ドラッグ`で移動できます。

移動後に`(透過中)キャラ付近を掴んでドラッグ`をオフにすると、キャラクターがクリックに反応しなくなり、背面のアプリケーションをクリックできます。

<div class="row">
{% include docimg.html file="./images/get_started/img00_060_transparent_bg.jpg" customclass="col s12 m6 l4" imgclass="fit-doc-img" %}
{% include docimg.html file="./images/get_started/img00_070_transparent_bg_drag.jpg" customclass="col s12 m6 l4" imgclass="fit-doc-img" %}
{% include docimg.html file="./images/get_started/img00_090_transparent_bg_can_click.jpg" customclass="col s12 m6 l4" imgclass="fit-doc-img" %}
</div>

また`背景を透過`がオフであれば、`背景画像`の`ロード`から背景画像を設定したり、`クリア`で元の単色背景に戻したりできます。


#### 2.2. 顔・表情
{: .doc-sec2 }

`顔・表情`メニューは、顔の動きに関連する主要な機能です。

<div class="row">
{% include docimg.html file="./images/get_started/img00_100_streaming_face.jpg" customclass="col s12 m6" imgclass="fit-doc-img" %}
</div>

<div class="doc-ul" markdown="1">

- `リップシンク`: PCに接続されたマイクを選択して、音声にあった口の動きを反映します。
- `マイク感度[dB]`: マイク入力が小さい場合にプラスの値を指定することで、リップシンクが正しく動作するようになります。`音量をチェック`を使って適正な値を確認して下さい。
- `音量をチェック`: オンにするとマイク音量バーが表示されます。喋っている間はほぼ緑色で、ときどき赤色になるのが適正な音量です。
- `顔をトラッキング`: ウェブカメラを選択することで、首の動作を反映します。
- `高解像度モード`: CPU負荷が上昇する代わり、ややトラッキングの性能が向上します。v1.7.0以降で正式版として使用できます。

</div>

顔をトラッキングさせてもモデルの首が回らない場合、[FAQ](./questions)の「顔トラッキングで首が回らない」を参照下さい。

ウェブカメラが正面にない場合やウェブカメラを移動させた場合は、普段の姿勢でディスプレイを見ながら`姿勢・表情を補正`ボタンをクリックしてキャラクターの位置を戻します。

<div class="note-area" markdown="1">

**HINT** 

キャラクターがうつむきがちな場合、少し下を向いて`姿勢・表情を補正`をクリックするとキャラが上を向きやすくなります。

反対に、キャラクターが上を向きすぎる場合は上を向いて`姿勢・表情を補正`ボタンをクリックするとキャラが下を向きやすくなります。

</div>

`視線の動き`ではキャラクターの目の動かし方を選択します。通常は`マウス`選択にすることで、キャラクターがマウスポインターの方向を見つめます。

<div class="note-area" markdown="1">

**NOTE** 

本機能とは別に、iOSアプリによる表情トラッキングが可能です。

詳細は[外部トラッキング](./docs/external_tracker)を参照ください。

</div>


#### 2.3. モーション
{: .doc-sec2 }

アバターの動き方を設定します。

キーボードやマウスの操作時、およびゲームパッド操作時の動き方を選択できます。

また`つねに手下げモード`チェックをオンにすると、キーボード入力等への反応を停止させ、手がつねに下がった姿勢にできます。`つねに手下げモード`が有効なとき、体の動きがやや大きくなります。

<div class="row">
{% include docimg.html file="./images/get_started/img00_210_motion_modes.jpg" customclass="col s12 m4 l4" imgclass="fit-doc-img" %}
{% include docimg.html file="./images/get_started/img00_220_motion_modes_hand_down.jpg" customclass="col s12 m4 l4" imgclass="fit-doc-img" %}
</div>


#### 2.4. 表示
{: .doc-sec2 }

`表示`ではキャラクター以外のデバイス表示やエフェクトのオン・オフを切り替えます。

とくにキーボード等を表示しているとき、`タイピング時のエフェクト`で`None`以外を選ぶとタイピング時にエフェクトが表示されます。

<div class="row">
{% include docimg.html file="./images/get_started/img00_125_view_typing_effect_example.jpg" customclass="col s12 m6" imgclass="fit-doc-img" %}
</div>

<div class="note-area" markdown="1">

**Hint**

キャラクターの影の見栄えが悪い場合、[FAQ](./questions)の「影が綺麗に映らない」の項目を確認してください。

それでも見栄えが改善しない場合、この設定で影の表示をオフにします。

</div>


#### 2.5. カメラ
{: .doc-sec2 }

カメラ機能では、キャラクターをうつす視点を操作できます。

本機能を使うときは通常`ウィンドウ`メニューの`背景を透過`をオフにします。その後、`フリーカメラモード`チェックをオンにすると、キャラクターウィンドウ上で視点を動かせます。

<div class="doc-ul" markdown="1">

- `中ホイール`: カメラを前後に移動します。
- `右クリック + ドラッグ`: 視線を上下左右に回転します。
    - v2.0.1以降では、`Altキー + 左クリック + ドラッグ`でも視線を回転できます。
- `中クリック + ドラッグ`: カメラを上下左右に平行移動します。
    - v2.0.1以降では、`Shiftキー + 左クリック + ドラッグ`でもカメラを平行移動できます。

</div>

キャラクターを見失った場合や始めからやり直す場合、`位置をリセット`ボタンで初期状態に戻します。

<div class="row">
{% include docimg.html file="./images/get_started/img00_130_free_camera_mode.jpg" customclass="col s12 m6" imgclass="fit-doc-img" %}
</div>

また、`クイックセーブ`ボタンで現在の視点を保存したり、`クイックロード`の対応するボタンでロードしたりできます。

<div class="note-area" markdown="1">

**Hint**

`背景を透過`がオンのままでも、`フリーカメラモード`チェックをオンにして視点を調整できます。

<div class="doc-ul" markdown="1">

1. `フリーカメラモード`チェックをオンにする
2. `(透過中)キャラ付近を掴んでドラッグ`をオンにする
3. キャラクターを左クリックし、各操作で視点を調整
4. 調整後、`(透過中)キャラ付近を掴んでドラッグ`と`フリーカメラモード`をオフにする

</div>

ただし、この方法ではキャラクターが表示ウィンドウの外に見切れやすいです。

キャラクターを見失った場合は`位置をリセット`ボタンでリセットして下さい。

あるいは、`背景を透過`をオフにしてウィンドウの表示を確認して下さい。

</div>

#### 2.6. デバイスのレイアウト
{: .doc-sec2 }

`フリーレイアウトモード`をオンにすると、キーボード、タッチパッド、ゲームコントローラなどの位置を調整できます。

{% include docimg.html file="./images/get_started/img00_200_free_layout.jpg" %}

チェックをオンにすると自動的に`背景を透過`チェックがオフになります。

このモードではキャラクターウィンドウの左上に設定が出現します。

<div class="doc-ul" markdown="1">

- `Control Mode`: デバイスの位置、回転、スケールのどれを調整するかを選択します。
- `Coordinate`: デバイスに沿った座標で動かすか、ワールド座標を用いるかを選択します。通常は`Local`のまま操作します。
- `Gamepad Scale`: ゲームパッドのモデル部分の大きさを調整します。ゲームパッドが手から突き抜けてしまう場合、値を小さくします。

</div>

レイアウトが極端に崩れてしまった場合、`リセット`で標準的なレイアウトに戻します。

#### 2.7. Word To Motion
{: .doc-sec2 }

`Word To Motion`はキャラクターの表情をコントロールできる機能です。

<div class="row">
{% include docimg.html file="./images/get_started/img00_105_word_to_motion.jpg" customclass="col s12 m6" imgclass="fit-doc-img" %}
</div>

デフォルト設定の場合、キーボードで"joy"とタイピングするとキャラクターの表情が変化します。

それ以外でも、`デバイスの割り当て`で`ゲームパッド`を選んでA,B,X,Yボタンを押したり、`キーボード (数字の0-8)`を選んで数字キーの1,2,3,4を押したりしても表情が変化します。

詳しくは[Docs](./docs)の[Word To Motion](./docs/expressions)を参照下さい。

とくに`デバイスの割り当て`で`ゲームパッド`や`MIDIコントローラ`を選ぶことで、キャラクターの動作に反映させずに表情を切り替えられます。


### 3. もっと細かく調整したい場合は
{: .doc-sec1 }

VMagicMirrorのより細かい機能については、以下のページで紹介しています。

<div class="doc-ul" markdown="1">

- [Docs](./docs): このページで紹介しなかった詳細設定やiOSとの連携について、UIの項目別に紹介します。全ての機能を網羅的にチェックできます。
- [Tips](./tips): VMagicMirrorの用途に応じた使い方を何種類か紹介しています。

</div>
