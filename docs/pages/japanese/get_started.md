---
layout: page
title: Get Started
permalink: /get_started
---

[English](./en/get_started)

# Get Started

このドキュメントでは、ダウンロードした`VMagicMirror`の基本的な使い方を紹介します。

また、一連のセットアップを行っていく様子をこちらの動画でも紹介しています。

<iframe class="youtube" width="560" height="315" data-src="https://www.youtube.com/embed/kYk-YHqPeMU" frameborder="0" allow="accelerometer; autoplay; encrypted-media; gyroscope; picture-in-picture" allowfullscreen></iframe>

### 1. 起動してキャラクターを表示する
{: .doc-sec1 }

`VMagicMirror.exe`を起動すると、GUIがある「コントロールパネル」と、キャラクターが映る「キャラクター表示ウィンドウ」が立ち上がります。

コントロールパネルかキャラクター表示ウィンドウの一方を閉じると、もう片方の画面も閉じて`VMagicMirror`が終了します。コントロールパネルが邪魔な場合は最小化しておきます。

キャラクターをロードするにはコントロールパネルの`ホーム`タブの`VRMロード`ボタンをクリックし、PC上の`.vrm`ファイルを選択します。

<div class="row">
{% include docimg.html file="./images/get_started/img00_015_started.png" customclass="col s12 m6" imgclass="fit-doc-img" %}
{% include docimg.html file="./images/get_started/img00_020_load_vrm.png" customclass="col s12 m6" imgclass="fit-doc-img" %}
</div>

キャラクター表示ウィンドウに表示される規約を確認して`OK`をクリックすると、キャラクターをロードします。

ロード後、同じキャラクターを次回以降も使いたい場合、`VRMロード`ボタンの下にある`次回の起動時にも同じVRMを読み込む`のチェックをオンにします。

<div class="row">
{% include docimg.html file="./images/get_started/img00_030_load_vrm_confirmation.png" customclass="col s12 m6" imgclass="fit-doc-img" %}
{% include docimg.html file="./images/get_started/img00_040_after_loaded.png" customclass="col s12 m6" imgclass="fit-doc-img" %}
</div>

**Hint:** この時点でキーボードやタッチパッドの位置、視点がキャラクターに合わない場合、ひとまず`キャラ体格で補正`ボタンをクリックしておきます。

<div class="row">
{% include docimg.html file="./images/get_started/img00_160_not_good_layout_example.png" customclass="col s12 m6" imgclass="fit-doc-img" %}
{% include docimg.html file="./images/get_started/img00_170_after_adjust.png" customclass="col s12 m6" imgclass="fit-doc-img" %}
</div>


### 2. 配信タブ: メイン機能の使い方
{: .doc-sec1 }

コントロールパネルの`配信`タブでは`VMagicMirror`のすべての主要機能にアクセスできます。

キャラクターをロードしたら色々な基本機能を試してみましょう。

{% include docimg.html file="./images/get_started/streaming_tab_overview.png" %}

#### 2.1. ウィンドウ
{: .doc-sec2 }

`ウィンドウ`で`背景を透過`のチェックをオンにすると、背景を透明にできます。`VMagicMirror`は通常、この状態で使用します。

背景が透明なとき、`(透過中)キャラ付近を掴んでドラッグ`のチェックがオンであればキャラクターを左クリック+ドラッグして移動できます。

移動後は`(透過中)キャラ付近を掴んでドラッグ`をオフにすることでキャラクターがクリックに反応しなくなり、背面のアプリケーションをクリックできるようになります。

<div class="row">
{% include docimg.html file="./images/get_started/img00_060_transparent_bg.png" customclass="col s12 m6 l4" imgclass="fit-doc-img" %}
{% include docimg.html file="./images/get_started/img00_070_transparent_bg_drag.png" customclass="col s12 m6 l4" imgclass="fit-doc-img" %}
{% include docimg.html file="./images/get_started/img00_090_transparent_bg_can_click.png" customclass="col s12 m6 l4" imgclass="fit-doc-img" %}
</div>


#### 2.2. 仮想カメラ出力
{: .doc-sec2 }

`仮想カメラ出力`はウェブ会議などで手軽にVMagicMirrorを使うための機能です。

<div class="row">
{% include docimg.html file="./images/get_started/img00_095_virtual_cam_out.png" customclass="col s12 m6" imgclass="fit-doc-img" %}
</div>

この機能はとくに興味がなければスキップして構いません。詳細は[Tips: 仮想カメラを使う](./tips/virtual_camera)をご覧下さい。


#### 2.3. 顔・表情
{: .doc-sec2 }

`顔・表情`メニューは、顔の動きに関連する主要な機能です。

<div class="row">
{% include docimg.html file="./images/get_started/img00_100_streaming_face.png" customclass="col s12 m6" imgclass="fit-doc-img" %}
</div>

`リップシンク`: PCに接続されたマイクを選択して、音声にあった口の動きを反映します。

`顔をトラッキング`: ウェブカメラを選択することで、首の動作を反映します。

`顔とセットで手もトラッキング`: ウェブカメラによる簡易的なハンドトラッキングを有効にします。

顔をトラッキングさせてもモデルの首が回らない場合、[FAQ](./questions)の「顔トラッキングで首が回らない」を参照下さい。

ハンドトラッキングには制限事項や注意点があります。詳細は[ハンドトラッキングの使用について](./tips/using_hand_tracking)を参照下さい。

ウェブカメラが正面にない場合やウェブカメラを移動させた場合は、普段の姿勢でディスプレイを見ながら`姿勢・表情を補正`ボタンをクリックしてキャラクターの位置を戻します。

**Hint:** キャラクターがうつむきがちになる場合は、少し下を向いて`姿勢・表情を補正`ボタンをクリックすると、キャラが上を向きやすくなります。反対に、キャラクターが上を向きすぎる場合は、上を剥いて`姿勢・表情を補正`ボタンをクリックすることで、キャラが下を向きやすくなります。

`視線の動き`はキャラクターの目の動かし方を選択します。通常は`マウス`選択にすることで、キャラクターがマウスポインターの方向を見つめます。


#### 2.4. Word To Motion
{: .doc-sec2 }

`Word To Motion`はいくつかの方法でキャラクターの表情をコントロールできる機能です。

<div class="row">
{% include docimg.html file="./images/get_started/img00_105_word_to_motion.png" customclass="col s12 m6" imgclass="fit-doc-img" %}
</div>

デフォルト設定の場合、キーボードで"joy"とタイピングするとキャラクターの表情が変化します。それ以外でも、`デバイスの割り当て`で`ゲームパッド`を選んでA,B,X,Yボタンを押したり、`キーボード (数字の0-8)`を選んで数字キーの1,2,3,4を押したりしても表情が変化します。

詳しくは[詳細設定](./docs)の[Word To Motion](./docs/expressions)を参照下さい。

とくに`デバイスの割り当て`で`ゲームパッド`や`MIDIコントローラ`を選ぶことにより、こっそりと表情を切り替えられます。


#### 2.5. スクリーンショット
{: .doc-sec2 }

`撮影`ボタンであるカメラアイコンのボタンを押すと、3秒間のカウントダウンののちスクリーンショットを撮影します。

スクリーンショットの保存先は`VMagicMirror.exe`があるフォルダ以下の`Screenshots`フォルダです。(スクリーンショットを1枚も撮った事が無い場合、フォルダが存在しないことがあります)

スクリーンショットは透過画像で、影の表示/非表示も反映されるため、影ごと他の画像と合成できます。

<div class="row">
{% include docimg.html file="./images/get_started/img00_180_screenshot.png" customclass="col s12 m6" imgclass="fit-doc-img" %}
{% include docimg.html file="./images/get_started/img00_190_screenshot_shadow.png" customclass="col s12 m6" imgclass="fit-doc-img" %}
</div>


#### 2.6. 表示
{: .doc-sec2 }

`表示`では`VMagicMirror`の対応デバイスやエフェクトのオン・オフを切り替えます。

とくにキーボードを表示して`タイピング時のエフェクト`で`None`以外を選択した場合、タイピング時にエフェクトが表示されます。

<div class="row">
{% include docimg.html file="./images/get_started/img00_125_view_typing_effect_example.png" customclass="col s12 m6" imgclass="fit-doc-img" %}
</div>

**Hint:** 影の見栄えが悪い場合、[FAQ](./questions)の"影が綺麗に映らない"の項目を確認してください。それでも見栄えが改善しない場合、影の表示をオフにします。


#### 2.7. カメラ
{: .doc-sec2 }

カメラ機能では、キャラクターをうつす視点を操作できます。

本機能を使うときは基本的に`ウィンドウ`メニューの`背景を透過`をオフにします。その後、`フリーカメラモード`チェックをオンにすると、キャラクター表示ウィンドウ上で直接視点を動かせます。

`右クリック + ドラッグ`: 視線を上下左右に回転します。

`中クリック + ドラッグ`: カメラを上下左右に平行移動します。

`中ホイール`: カメラを前後に移動します。

調整が終わったら`背景を透過`をオンに、`フリーカメラモード`をオフに戻します。

キャラクターを見失った場合や始めからやり直したい場合、`位置をリセット`ボタンで初期状態に戻せます。

<div class="row">
{% include docimg.html file="./images/get_started/img00_130_free_camera_mode.png" customclass="col s12 m6" imgclass="fit-doc-img" %}
{% include docimg.html file="./images/get_started/img00_140_after_free_camera_mode.png" customclass="col s12 m6" imgclass="fit-doc-img" %}
</div>

また、動かした視点は`クイックセーブ`の`[1], [2], [3]`いずれかのボタンを押して保存したり、`クイックロード`の対応するボタンを押してロードしたりできます。


**Hint:** 操作に慣れてきたら、`背景を透過`をオンにしたまま`フリーカメラモード`チェックをオンにしても視点を調整できます。

1. `(透過中)キャラ付近を掴んでドラッグ`をオンにする
2. キャラクターを左クリックし、各操作で視点を調整
3. 調整が終わったら`(透過中)キャラ付近を掴んでドラッグ`と`フリーカメラモード`をオフにする

ただし、この操作方法では気づかないうちにキャラクターがキャラクター表示ウィンドウから見切れることがあります。キャラクターを見失ってしまい、直し方がわからなくなった場合は`位置をリセット`ボタンを押してやり直すか、`背景を透過`をオフにしてウィンドウの表示を確認します。


#### 2.8. デバイスのレイアウト
{: .doc-sec2 }

`フリーレイアウトモード`のチェックをオンにするとキーボード、タッチパッド、ゲームコントローラなどの位置を調整できます。

{% include docimg.html file="./images/get_started/img00_200_free_layout.png" %}

チェックをオンにした時点で自動的に`背景を透過`チェックがオフになります。`フリーレイアウト`は背景が透明なままだと使用できないことに注意して下さい。

フリーレイアウトモード中の、キャラクター表示ウィンドウ左上の設定は次のような意味です。

`Control Mode`: デバイスの位置、回転、スケールのどれを調整するかを選択します。

`Coordinate`: デバイスに沿った座標で動かすか、ワールド座標を用いるかを選択します。通常は`Local`のまま操作します。

`Gamepad Scale`: ゲームパッドのモデル部分の大きさを調整します。ゲームパッドが手から突き抜けてしまう場合、値を小さくします。

レイアウトが極端に崩れてしまった場合、`リセット`で標準的なレイアウトに戻します。

#### 2.9. モーション
{: .doc-sec2 }

`プレゼン風に右手を動かす`のチェックをオンにしてマウスを動かすと、キャラクターが右手でマウスポインタの方向を指し示します。

<div class="row">
{% include docimg.html file="./images/get_started/img00_150_presentation_mode.png" customclass="col s12 m6" imgclass="fit-doc-img" %}
</div>

このスタイルは解説動画やプレゼンテーションで`VMagicMirror`を使う際に便利です。

詳しくは[Tips: プレゼンテーションでVMagicMirrorを使う](./tips/presentation)もあわせてご覧下さい。


### 3. もっと細かく調整したい場合は
{: .doc-sec1 }

[Docs](./docs)や[Tips](./tips)にて、さらに詳細な機能をいくつか紹介しています。

より細かく調整したい方はあわせてご覧下さい。
