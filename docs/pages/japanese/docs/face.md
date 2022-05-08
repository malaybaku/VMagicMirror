---
layout: page
title: Face
permalink: /docs/face
---

[English](../en/docs/face)

# 顔・表情

`顔・表情`タブではモーションのうち、とくに顔に関連する調整ができます。

{% include docimg.html file="/images/docs/face_top.png" %}

<div class="note-area" markdown="1">

**NOTE**

このタブはv1.6.1で追加されました。それ以前のバージョンでは、`モーション`タブで顔に関するモーションが設定できます。

</div>


#### 基本の設定
{: .doc-sec2 }

`リップシンク`: リップシンク機能に用いるマイクを設定します。コントロールパネルのタブにもある機能です。

`マイク感度[dB]`: マイク入力が小さすぎる場合はプラスの値を指定することで、リップシンクが正しく動作するようになります。`音量をチェック`を使って適正な感度か確認して下さい。

`音量をチェック`: オンにするとマイク音量バーが表示されます。喋っているあいだ、ほぼ緑色で、ときどき赤色になるのが適正な音量です。

`顔をトラッキング`: ウェブカメラによる顔トラッキングに用いるカメラを設定します。コントロールパネルの配信タブにもある機能です。

`高解像度モード`: CPU負荷が上がる代わり、ややトラッキングの性能が向上します。v1.7.0以降で正式版として使用できます。

`顔トラッキング中も自動でまばたき`: デフォルトではオンになっています。オフにすると、画像処理ベースで目の開閉を制御するようになります。

`顔トラッキング中の前後移動を有効化`: オンにすると、キャラクターが前後に動くようになります。顔トラッキングが安定している場合、このチェックをオンにするとキャラクターの動きが更にリッチになります。

`左右反転をオフにする`: チェックボックをオンにすると左右の反転がオフになります。このオプションを切り替えた場合は`姿勢・表情を補正`ボタンを押すようにして下さい。

`姿勢・表情を補正`: 押すことで現在のカメラに映っている姿勢でキャリブレーションします。コントロールパネルの配信タブにもある機能です。

`カメラ非使用時、音声ベースでアバターを動かす`: webカメラもiOS連携も使用していないとき、このチェックをオンにすると、マイクでの音声入力に合わせてアバターがそれらしく動きます。カメラのない環境や、PC負荷を抑えたいケースで使用します。


#### 目・視線
{: .doc-sec2 }

`顔の動きや声でまばたき補正`: 自動まばたきが有効なときにこのチェックをオンにすると、首をすばやく動かしたときや、発話の区切り目にあわせて高確率でまばたき行う、自然な補正が適用されます。

`視線の動き`: 視線をどう動かすか選択します。コントロールパネルの配信タブにもある機能です。`マウス`を選択すると、キャラクターがマウスポインターの方向を見つめます。`固定`ではマウスを無視し、基本的に正面を向きます。`ユーザー`選択は`固定`に似ていますが、`フリーカメラモード`でキャラクターの向きを正面向き以外にしたときも、なるべくモニターの正面方向(つまりあなた自身)を見つめるように動きます。

`目の動きの大きさ[%]`: マウス追尾などで視線を動かすときの、眼球運動のスケールを設定します。VRoidモデルではデフォルト値(100%)を推奨しています。目ボーンがあるにもかかわらずモデルの目が動かない場合、大きめの値に調整してください。


#### ブレンドシェイプ
{: .doc-sec2 }

`表情切り替えを補間しない`: このオプションはv2.0.4以降で有効です。チェックをオンにすると、Word to Motion機能またはFace Switch機能で表情が切り替わるときの補間が無効になり、表情が直ちに適用されるようになります。

`Funブレンドのデフォルト値[%]`: ふだんの表情に`Fun`ブレンドシェイプを適用することで、やや笑顔の状態にするパラメータです。大きくするほど普段から笑顔になりますが、キャラクターによってはまばたきやリップシンクの動作と組み合わせたとき不自然になります。その場合は小さな値にします。

`Neutralブレンドシェイプ`: 通常は設定不要ですが、モデルのデフォルト表情を`Neutral`などのブレンドシェイプで調整している場合、そのブレンドシェイプクリップを指定します。ここで指定したクリップと同時にまばたき(`BLINK_L`/`BLINK_R`)やリップシンクも動作することに注意して下さい。

`体型調整ブレンドシェイプ`: 通常は設定不要ですが、モデルの体格や輪郭をブレンドシェイプで調整している場合、そのブレンドシェイプクリップを指定します。`Neutralブレンドシェイプ`と異なり、このブレンドシェイプは他の表情が適用された場合も常に適用されることに注意してください。