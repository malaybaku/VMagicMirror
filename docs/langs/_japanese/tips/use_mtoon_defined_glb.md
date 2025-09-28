---
layout: page
title: アクセサリ機能でGLBファイルにMToonのマテリアルを適用して使う
---

# Tips: アクセサリ機能でGLBファイルにMToonのマテリアルを適用して使う

このページでは [アクセサリー](../../docs/accessory) 機能の応用的な使い方として、`.glb` ファイルのデータにMToonシェーダーを適用して読み込む方法を説明します。

本機能はVMagicMirror v4.2.1およびそれ以降のバージョンで利用できます。

<div class="note-area" markdown="1">

**NOTE**

本機能の公開時点ではMToonシェーダーの情報を持つ `.glb` ファイルそのものは流通していません。

そのため、後述するように本機能を使うにはUnity上でのセットアップが必要です。

</div>


#### 1. GLB出力用のUnityプロジェクトを準備する
{: .doc-sec2 }

Unity 2022.3またはUnity 6以降のバージョンのUnityプロジェクトを新規作成します。

プロジェクトの新規作成時にはBuilt-in Render Pipelineを選択します。

<div class="note-area" markdown="1">

**NOTE**

VMagicMirrorはv4.2.1時点でBuilt-in Render Pipelineを使用しています。

Unity Editor上での見え方とVMagicMirrorに読み込んだときの見た目の一致度を上げるために、特に理由がない限りはBuilt-in Render Pipelineのプロジェクトを使用して下さい。

</div>

その後、プロジェクトに下記を導入します。

- [UniVRM v0.121.0](https://github.com/vrm-c/UniVRM/releases/tag/v0.121.0)
- [UnityMToonGltfExtension v0.1.0](https://github.com/malaybaku/UnityMToonGltfExtension/releases/tag/v0.1.0)

プロジェクトを開いているUnity Editor上で `Window > Package Manager` からパッケージマネージャーウィンドウを開きます。

ウィンドウ左上の `+` から `Add Package from git URL...` を選び、下記の各URLを順に指定してインストールします。

<div class="doc-ul" markdown="1">

- `https://github.com/vrm-c/UniVRM.git?path=/Assets/VRMShaders#v0.121.0`
- `https://github.com/vrm-c/UniVRM.git?path=/Assets/UniGLTF#v0.121.0`
- `https://github.com/vrm-c/UniVRM.git?path=/Assets/VRM#v0.121.0`
- `https://github.com/vrm-c/UniVRM.git?path=/Assets/VRM10#v0.121.0`
- `https://github.com/malaybaku/UnityMToonGltfExtension.git?path=/Package#v0.1.0`

</div>

#### 2. GLBファイルを出力する
{: .doc-sec2 }

上記のプロジェクト上で、3Dデータのprefabを準備して、MToonシェーダーを用いたマテリアルを適用し、パラメータを調整します。このprefabのベースとなるモデルは `.fbx` をインポートしたものなど、 `.glb` 以外の形式でも問題ありません。

prefabをセットアップ後、メニューバーの `MToonGltf -> Export MToon glTF...` からエクスポート用ウィンドウを開きます。エクスポートしたいprefabを選択して `.glb` ファイルをエクスポートします。

エクスポート用ウィンドウでマテリアルに関する警告が表示されることがありますが、警告は無視して構いません。

なお、出力結果の `.glb` を同じプロジェクトでインポートすると、GLBファイルにMToonの情報が正しく格納されたかどうかチェックできます。
このチェックを行うにはメニューバーの `MToonGltf -> Use MToon glTF Importer` をオンにしてから、プロジェクト上に `.glb` ファイルを Drag & Dropします。

<div class="note-area" markdown="1">

**NOTE**

`UnityMToonGltfExtension` の挙動の変更によって上記以外のチェック手順が必要になる可能性があります。必要に応じて [UnityMToonGltfExtension](https://github.com/malaybaku/UnityMToonGltfExtension) を確認して下さい。

</div>


#### 3. 出力したファイルをアクセサリーとして使用する
{: .doc-sec2 }

[アクセサリー](../../docs/accessory) で利用できる通常の `.glb` ファイルと同様に、出力したファイルは `(マイドキュメント)\VMagicMirror_Files\Accessory` フォルダに配置します。

配置後にVMagicMirrorを実行するとアクセサリーが認識され、MToonマテリアルの情報つきでロードされるようになります。
