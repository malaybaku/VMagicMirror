---
layout: page
title: キーボードやタッチパッドの見た目を変更する
permalink: /tips/change_textures
---

[English](../../en/tips/change_textures)

# キーボードやタッチパッドの見た目を変更する

ユーザーインターフェースはありませんが、`VMagicMirror`ではキーボードのキーや、タッチパッドの画像を好きなものに変更できます。現在は`png`形式の画像のみをサポートしています。

画像を変更するには、`VMagicMirror.exe`を起動する前に以下のフォルダを開きます。対象フォルダはVMagicMirrorのバージョンによって異なることに注意してください。

<div class="doc-ul" markdown="1">

- v1.9.0以降: `(マイドキュメント)\VMagicMirror_Files\Textures`
- v1.8.2またはそれ以前: `(VMagicMirror.exeのあるフォルダ)/VMagicMirror_Data/StreamingAssets`

</div>


このフォルダへ差し替えたい画像を追加します。ファイル名は次のようにしてください。

* キーボードのキー画像を差し替える場合: `key.png`
* タッチパッドの画像を差し替える場合: `pad.png`
* ゲームパッドの本体部分を差し替える場合: `gamepad_body.png`
* ゲームパッドのスティックやボタン部分を差し替える場合: `gamepad_button.png` (*v1.8.1およびそれ以前のバージョンのみ)
* MIDIコントローラのノート部分を差し替える場合: `midi_note.png`
* MIDIコントローラのノブ部分を差し替える場合: `midi_knob.png`
* ペンタブレット使用時のペン部分を差し替える場合: `pen.png`
* ペンタブレット使用時のペンタブレット部分を差し替える場合: `pen_tablet.png`
* アーケードスティック部分を差し替える場合: `arcade_stick.png`

<div class="note-area" markdown="1">

**NOTE**

MIDIコントローラ系のテクスチャ切り替えはv1.6.2以降で動作します。

</div>

デフォルトのままでよいものについては、ファイルが無い状態のままにします。

ゲームパッドに関しては、VMagicMirrorのバージョンによって必要な画像が異なります。

v1.8.2以降のバージョンでは、単一の`gamepad_body.png`ファイルでテクスチャを定義します。UVテンプレートは以下の画像の通りです。

v1.8.1およびそれ以前のバージョンでは単色の画像が必要であり、ゲームパッド本体とボタン部分の色をそれぞれ`gamepad_body.png`、`gamepad_button.png`で指定します。

画像ファイルの配置後に`VMagicMirror.exe`を実行すると、画像が適用されます。

<div class="row">

{% include docimg.html file="/images/tips/change_texture.png" customclass="col s6 m6 l4" imgclass="fit-doc-img" %}

{% include docimg.html file="/images/tips_model/gamepad_template.png" customclass="col s6 m6 l4" imgclass="fit-doc-img" %}

</div>

もとに戻したいときは、配置した画像ファイルを削除し、VMagicMirrorを再び起動します。
