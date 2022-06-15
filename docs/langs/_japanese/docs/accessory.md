---
layout: page
title: アクセサリー
---

# アクセサリー

このページでは、`VMagicMirror` v2.0.0以降で実装されているアクセサリー機能を紹介します。

<div class="row">
{% include docimg.html file="./images/docs/accessory_header.png" customclass="col s12 m6 l6" imgclass="fit-doc-img" %}
</div>

※画像中のアクセサリのうち、3Dモデルのライセンスについては本ページ最下部をご覧下さい。

#### アクセサリー機能とは
{: .doc-sec2 }

アクセサリー機能では画像または3Dモデルを読み込み、アバターの身体に装着できます。

<div class="doc-ul" markdown="1">

- 画像はpng形式のみに対応しています。
- 3Dモデルはglb、またはglTFファイルに対応しています。
- v2.0.1以降ではアニメーションするアクセサリとして、連番のpng画像も読み込めます。

</div>

3Dモデルの対応形式であるglb/glTFは、アバターのファイル形式(VRM)の基盤となっている形式です。

この形式で3Dモデルを入手する方法については[GLBデータの入手方法](../../tips/get_glb_data)を参照下さい。


#### 使用方法
{: .doc-sec2 }

png、glb、またはglTFデータを`(マイドキュメント)\VMagicMirror_Files\Accessory`フォルダ内に配置します。
glTFはモデルを含むフォルダを丸ごと配置し、フォルダ名をアクセサリの内容に即した名前にします。

<div class="note-area" markdown="1">

**NOTE**

一度読み込んだファイル名/フォルダ名を後から変更した場合、アイテムの配置がリセットされます。

</div>

もしVMagicMirrorの起動後にアクセサリ用のファイルを追加/削除した場合、`再読み込み`ボタンを押すことでファイルの状態が反映されます。

<div class="row">
{% include docimg.html file="./images/docs/accessory_folder_structure.png" customclass="col s6 m4 l4" imgclass="fit-doc-img" %}
{% include docimg.html file="./images/docs/accessory_item_edit.png" customclass="col s6 m4 l4" imgclass="fit-doc-img" %}
</div>


アクセサリが読み込まれるとコントロールパネルの`アクセサリ`タブにアクセサリ一覧が表示されます。各アクセサリのチェックボックスで表示/非表示を切り替えられます。

各アイテムの編集項目は次の通りです。

<div class="doc-ul" markdown="1">

- `表示名`: アイテムを折りたたんだときに表示される名称を設定します。
- `配置先`: アイテムをアバターの身体のどの部分に固定するかを指定します。
- `2Dのまま最前面に表示`: アイテムを常に最前面に表示します。(後述)
- `Position`: アイテムの位置を指定します。`フリーレイアウト`での直接編集もできます。
- `Rotation`: アイテムの回転を指定します。`フリーレイアウト`での直接編集もできます。
- `Scale`: アイテムのスケールを指定します。`フリーレイアウト`での直接編集もできます。
- `FPS`: 連番png画像でのみ表示されます。アニメーションのFPSを5-30の範囲で指定します。

</div>

`2Dのまま最前面に表示`は画像のアクセサリ専用の機能で、オンにすると画像をつねに最前面に表示できます。

最前面表示ではアクセサリー位置の決定方法がやや特殊で、`2Dのまま最前面に表示`がオフだった場合の表示位置を基準にして位置が確定します。

<div class="note-area" markdown="1">

**NOTE**

この調整作業はアクセサリー機能を使う上で直感的に操作しづらい部分です。一例として、次の手順をお試し下さい。

<div class="doc-ul" markdown="1">

- `2Dのまま最前面に表示`を一度オフにしたあと、`フリーレイアウト`でアクセサリーを所定の配置先(目や口など)に移動する
- その後、`2Dのまま最前面に表示`をオンにし、見た目が想定通りになるか確認する
- 位置を調整したい場合、テキストで直接調整するか、または`2Dのまま最前面に表示`を再度オフにして位置を調整する

</div>

</div>

#### 本ページの画像で使用している3Dモデルのライセンスについて
{: .doc-sec2 }

[Low poly Christmas deer horns accessory](https://sketchfab.com/3d-models/low-poly-christmas-deer-horns-accessory-5e5d4500345445cfa5dc7848ebd278ba) by 3D Bear is licensed under [Creative Commons Attribution](http://creativecommons.org/licenses/by/4.0/).
