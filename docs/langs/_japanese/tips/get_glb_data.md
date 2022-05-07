---
layout: page
title: GLBデータの入手方法
permalink: /tips/get_glb_data
---

# Tips: GLBデータの入手方法

このページでは、[アクセサリー機能](../docs/accessory)で利用可能なGLB、またはGLTFデータの入手方法を紹介します。

GLBおよびGLTFはオープンソースの3Dモデル規格であるため、多くの3Dモデル用ツールでサポートされています。

ここでは代表的な方法の例を3つ想定し、最初の2つを詳しく紹介します。

<div class="doc-ul" markdown="1">

1. Unityで既存の3DモデルをGLBとしてエクスポートする
2. [SketchFab](https://sketchfab.com)でモデルをGLTFとして取得する
3. BlenderなどのDCCツールで、エクスポート形式の一つとしてGLBを選択する

</div>

DCCツールからのエクスポート方法については各ソフトの使用方法をご確認ください。

<div class="note-area" markdown="1">

**NOTE**

本ページの方法で3Dモデルを変換して取り扱うにあたり、アバターや画像の著作権と同様に著作権を重視してください。

とくに、変換処理によって元製作者の意図とは異なるモデルの見た目になったと考えられる場合、いっそうモデルの使用可否を入念に考慮してください。

</div>


#### 方法1: 既存のモデルをUnity上でglbとしてエクスポートする
{: .doc-sec2 }

Unityで新規プロジェクトを作成するか、あるいは既存プロジェクトを立ち上げて、[UniVRM](https://github.com/vrm-c/UniVRM)をインポートします。

その後、エクスポート対象となるモデルをシーン上のワールド原点に配置します。

もしモデルのデフォルト方向や姿勢を変更したい場合、あるいはモデルの`Scale`が`1`ではない場合は、まず空のゲームオブジェクトを新たにワールド原点に配置します。その後、モデルを子要素として、子要素の位置や回転、スケール調整によってモデルを所定の位置に配置します。

セットアップ後、モデルまたは新規に配置したゲームオブジェクトを選択し、`UniGLTF > Export(glb)`を実行してGLBファイルを保存します。

<div class="row">
{% include docimg.html file="./images/tips/accessory_glb_export_unity.png" customclass="col s12 m6 l6" imgclass="fit-doc-img" %}
</div>

<div class="note-area" markdown="1">

**NOTE**

保存されたデータの見た目を確認するには、同じUnityプロジェクト上で適当なフォルダ以下にglbファイルをドラッグ&ドロップし、再生成されたprefabをシーン上に配置します。

エクスポートするモデルのマテリアルがStandard以外のシェーダーを使っている場合、マテリアルやテクスチャのエクスポートに失敗することがあります。この場合、モデルのマテリアルをStandardシェーダーベースでセットアップし直すことを検討してください。

なお、この例ではUnityの有償アセットである[Anime Rooms](https://assetstore.unity.com/packages/3d/props/interior/anime-rooms-75722)を使用しています。

</div>


#### 方法2: SketchFabでモデルをgltfとして取得する
{: .doc-sec2 }

[SketchFab](https://sketchfab.com)は3Dモデルの公開サイトで、一部のモデルが有料または無料でダウンロード可能です。

ダウンロード可能なモデルに対し、`gltf`変換のオプションを選択してダウンロードすることでモデルが取得できます。

<div class="row">
{% include docimg.html file="./images/tips/accessory_gltf_from_sketchfab.png" customclass="col s12 m6 l6" imgclass="fit-doc-img" %}
</div>

<div class="note-area" markdown="1">

**NOTE**

Sketchfabではダウンロード時にライセンスが表示されるため、ライセンスに従ってご使用ください。

また、この方法で取得したGLTFデータに問題がある場合はOriginal Formatでモデルをダウンロードし、それをUnity等でインポートしてマテリアルなどを編集してからGLBに再度エクスポートすることを検討してください。

</div>
