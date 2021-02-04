---
layout: page
title: キーボードやタッチパッドの見た目を変更する
permalink: /tips/change_textures
---

[English](../en/tips/change_textures)

# キーボードやタッチパッドの見た目を変更する

ユーザーインターフェースはありませんが、`VMagicMirror`ではキーボードのキーや、タッチパッドの画像を好きなものに変更できます。現在は`png`形式の画像のみをサポートしています。

画像を変更するには、`VMagicMirror.exe`を起動する前に以下のフォルダを開きます。

`(VMagicMirror.exeのあるフォルダ)/VMagicMirror_Data/StreamingAssets`

このフォルダへ差し替えたい画像を追加します。ファイル名は次のようにしてください。

* キーボードのキー画像を差し替える場合: `key.png`
* タッチパッドの画像を差し替える場合: `pad.png`
* ゲームパッドの本体部分を差し替える場合: `gamepad_body.png`
* ゲームパッドのスティックやボタン部分を差し替える場合: `gamepad_button.png`
* MIDIコントローラのノート部分を差し替える場合: `midi_note.png`
* MIDIコントローラのノブ部分を差し替える場合: `midi_knob.png`

<div class="note-area" markdown="1">

**NOTE**

MIDIコントローラ系のテクスチャ切り替えはv1.6.2以降で動作します。

</div>

デフォルトのままでよいものは、単にファイルが無いままであれば問題ありません。

また、ゲームパッドに関しては単色画像での差し替えのみを想定しているため、イラストは適用しないで下さい。

画像ファイルを配置したあとで`VMagicMirror.exe`を実行することで、配置した画像が読み込まれます。

{% include docimg.html file="/images/tips/change_texture.png" %}

もとに戻したいときは、配置した画像ファイルを削除してからVMagicMirrorを再び起動します。
