---
layout: page
title: キーボードやタッチパッドの見た目を変更する
---

# キーボードやタッチパッドの見た目を変更する

ユーザーインターフェースはありませんが、`VMagicMirror`ではキーボードのキーや、タッチパッドの画像を好きなものに変更できます。現在は`png`形式の画像のみをサポートしています。

画像を変更するには、`VMagicMirror.exe`を起動する前に以下のフォルダを開きます。対象フォルダはVMagicMirrorのバージョンによって異なることに注意してください。

<div class="doc-ul" markdown="1">

- v1.9.0以降: `(マイドキュメント)\VMagicMirror_Files\Textures`
- v1.8.2またはそれ以前: `(VMagicMirror.exeのあるフォルダ)/VMagicMirror_Data/StreamingAssets`

</div>


このフォルダへ差し替えたい画像を追加します。ファイル名は次のようにしてください。

* キーボードのキー画像: `key.png`
* タッチパッド: `pad.png`
* ゲームパッド: `gamepad_body.png`
* MIDIコントローラのノート: `midi_note.png`
* MIDIコントローラのノブ: `midi_knob.png`
* ペンタブレット使用時のペン: `pen.png`
* ペンタブレット使用時のペンタブレット: `pen_tablet.png`
* アーケードスティック: `arcade_stick.png`
* 車のハンドル: `car_handle.png`
* (v3.9.0以降) マンガ風エフェクトのキー押下: `manga_keydown.png`
* (v3.9.0以降) マンガ風エフェクトのENTERキー押下: `manga_enter_keydown.png`
* (v3.9.0以降) マンガ風エフェクトのマウスクリック: `manga_click.png`
* (v3.9.0以降) マンガ風エフェクトのゲームパッドボタン押下: `manga_gamepad_button.png`
* (v3.9.0以降) マンガ風エフェクトのスティック操作: `manga_gameoad_stick.png`

デフォルトのままでよいものについては、ファイルが無い状態のままにします。

ゲームパッドとペンについては以下のUVテンプレートを参考にして下さい。車のハンドルについてはv3.8.0現在UVテンプレートは未提供であり、単色画像による差し替えを推奨しています。

それ以外については画像がほぼそのまま使用されるため、UVテンプレートはありません。下記の1枚目の画像では例として、キーボードとタッチパッドの画像が差し替わっています。

ファイル名が `manga_` から始まるエフェクト用の画像は、横長でアスペクト比が4:3の画像として用意します。

画像ファイルの配置後に`VMagicMirror.exe`を実行することで画像が適用されます。

<div class="note-area" markdown="1">

**NOTE**

ペンのUVテンプレートはv2.0.5以降で有効です。v2.0.4以前のものとはUVテンプレートが異なることに注意して下さい。

</div>

<div class="row">

{% include docimg.html file="/images/tips/change_texture.png" customclass="col s4 m4 l4" imgclass="fit-doc-img" %}

{% include docimg.html file="/images/tips_model/gamepad_template.png" customclass="col s4 m4 l4" imgclass="fit-doc-img" %}

{% include docimg.html file="/images/tips_model/pen_template.png" customclass="col s4 m4 l4" imgclass="fit-doc-img" %}

</div>

もとに戻したいときは、配置した画像ファイルを削除し、VMagicMirrorを再び起動します。
