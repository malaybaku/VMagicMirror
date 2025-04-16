---
layout: page
title: カスタムモーションをVMagicMirrorで使用する
---

# Tips: カスタムモーションをVMagicMirrorで使用する

この機能はv1.6.0で追加されました。

次の手順によって、ビルトインモーション以外のモーションをVMagicMirrorで使用できます。

1. Unity Editor上での操作により、使いたいモーションをVMagicMirror向けのファイルとしてエクスポートします。
2. エクスポートしたモーションを特定のフォルダに配置します。
3. Word to Motion機能から、追加したモーションを選択します。


<div class="note-area" markdown="1">

**NOTE**

VRMの標準定義ファイルであるVRM AnimationファイルはVMagicMirro v3.4.0からサポートしていますが、準備の方法や制限事項が異なります。

詳しくは[VRM AnimationをVMagicMirrorで使用する](../use_vrma)を参照ください。

</div>

#### 必要な環境や知識
{: .doc-sec2 }

<div class="doc-ul" markdown="1">

- Unity Editorでの作業が必要です。
- スクリプトに関する知識は不要です。
- できればUnityのAnimation Clipに関する基礎知識があることが望ましいです。

</div>

#### エクスポート可能なモーション形式と制限事項
{: .doc-sec2 }

可能な形式:

<div class="doc-ul" markdown="1">

- UnityでHumanoid Animationとして認識できるAnimation Clipはエクスポート可能です。
    - とくにUnity 2019.4.14f1またはそれ以降の2019.4系バージョンで認識できる必要があります。
    - 例えば、Unity上で直接製作した人体用モーション以外でも、fbxに含まれる人体用モーションも多くはエクスポート可能です。

</div>

制限事項:

<div class="doc-ul" markdown="1">

- アバターのルート姿勢、および下半身のモーションは適用されません。
    - VMagicMirrorは上半身の動きのみを想定しているため、このような制限がかかっています。
- IKによる手の動作はエクスポートされるデータには含まれますが、再生されません。
    - 今後のアップデートで手のIKアニメーション再生にも対応予定です。
- ループアニメーションには未対応です。

</div>

#### 1. モーションのエクスポート
{: .doc-sec2 }

Unity 2019.4.14f1をインストールした環境で、新規でプロジェクトをまたは既存のプロジェクトを開きます。

[VMagicMirror_MotioExporterのReleases](https://github.com/malaybaku/VMagicMirror_MotionExporter/releases)ページから、最新バージョンの`.unitypackage`ファイルをダウンロードします。

プロジェクトにunitypackageをインポートし、`Assets/Baku/VMagicMirror_MotionExporter/Scenes/MotionExporter`シーンを開きます。

シーン上の`Exporter`オブジェクトの`Motion Exporte`コンポーネントを開き、`Export Target`にエクスポートしたい`AnimationClip`を指定します。

<div class="row">
{% include docimg.html file="/images/tips/custom_motion_export_example.png" customclass="col l6 m6 s12" imgclass="fit-doc-img" %}
</div>

`Export`ボタンを押すと、`StreamingAssets`フォルダにファイルが出力されます。

Note:
もしファイルが確認できない場合、Unityではなくファイルエクスプローラで直接確認したり、一度Unityを立ち上げ直して下さい。

ファイル名はデフォルトで`(AnimationClipの名前).vmm_motion`のような形式になりますが、拡張子がそのままであれば、ファイル名を変更しても構いません。

<div class="note-area" markdown="1">

**Tips**

エクスポートされたモーションを同じプロジェクト上で読み込んで再生することで、エクスポート結果が正しいかどうかや、上半身のみのモーションで見栄えに問題がないか検証できます。

1. [UniVRM](https://github.com/vrm-c/UniVRM)をインポートします。
2. 適当なVRMモデルをプロジェクトにインポートし、`MotionExporter`シーン上に配置します。
3. シーン中の`MotionTestPlayer`オブジェクトを選択し、`MotionTestPlay`コンポーネントを次のように設定します。
    - `FileName`: `StreamingAssets`以下にある、エクスポート済みのモーションファイル名
    - `Target`: 2でシーンに配置したモデル
    - `OnlyUpperBody`: オン (※デフォルトでオンになっています)
4. シーンを実行します。

<div class="row">
{% include docimg.html file="/images/tips/custom_motion_verify_example.png" customclass="col l6 m6 s12" imgclass="fit-doc-img" %}
</div>

</div>

#### 2. エクスポートしたモーションの配置
{: .doc-sec2 }

上記の手順でエクスポートしたモーション(`.vmm_motion`ファイル)をVMagicMirror用のフォルダに配置します。

配置場所はVMagicMirrorのバージョンによって異なります。

<div class="doc-ul" markdown="1">

- v1.9.0以降: `(マイドキュメント)\VMagicMirror_Files\Motions`
- v1.8.2またはそれ以前: `(VMagicMirror.exeのフォルダ)/Motions`

</div>

もしフォルダがない場合、フォルダを新規作成して下さい。

<div class="row">
{% include docimg.html file="/images/tips/custom_motion_placement.png" customclass="col l6 m6 s12" imgclass="fit-doc-img" %}
</div>


#### 3. Word to Motion機能での選択
{: .doc-sec2 }

VMagicMirrorを起動し、[表情の設定](../../docs/expressions)にある手順で編集ウィンドウを開きます。

左側のモーション選択部分で`カスタムモーション`をチェックし、選択一覧からモーションを選択します。

<div class="row">
{% include docimg.html file="/images/tips/custom_motion_setup.png" customclass="col l6 m6 s12" imgclass="fit-doc-img" %}
</div>

選択後はビルトインモーションと同様の手順で使用できます。

<div class="note-area" markdown="1">

**NOTE**

ここでモーションが表示されない場合、エクスポートしたデータの形式が不正な可能性があります。

`1. モーションのエクスポート`の末尾に記載しているTipsに従って、Unity上でモーションを再生できるかどうか再度お試し下さい。

</div>
