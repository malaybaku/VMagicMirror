---
layout: page
title: パーフェクトシンク
permalink: /tips/perfect_sync
---

# Tips: パーフェクトシンク
{: .no_toc }

パーフェクトシンクはVMagicMirror v1.3.0で追加された機能です。

<div class="toc-area" markdown="1">

#### 目次
{: .toc-header .no_toc }

* ToC
{:toc .table-of-contents }

</div>

#### パーフェクトシンクとは
{: .doc-sec2 }

パーフェクトシンクは[外部トラッキング機能](../docs/external_tracker)の発展的な機能で、高度に表情を操作できます。利用にはFace ID対応のiPhoneまたはiPadが必要であり、またVRMモデルにもパーフェクトシンク用のセットアップが必要です。

あらかじめ[外部トラッキング機能](../docs/external_tracker)の基本機能を試したのち、本ページを読むことを推奨しています。

パーフェクトシンクでは、iOSのARKitで取得できるユーザーの様々な表情を、アバターの個別のブレンドシェイプに反映する仕組みを提供します。これによって、VRMの規格で最低限サポートされるものより多様な表現が可能になります。

#### パーフェクトシンクをサポートする他のVRMツール
{: .doc-sec2 }

パーフェクトシンクはVMagicMirrorに固有の機能ではありません。

例えば[Vear](https://apps.apple.com/jp/app/id1490697369)や[Luppet](https://luppet.appspot.com/)もパーフェクトシンクに対応しています。

どれか一つのツールでパーフェクトシンクが動くようセットアップされたモデルは、基本的に他のツールでも利用できます。


#### パーフェクトシンクをすぐ試すには
{: .doc-sec2 }

※すぐモデルのセットアップに進みたい場合、このセクションをスキップして構いません。

パーフェクトシンクを簡単に試す方法を2つ紹介します。

1つ目の方法は、セットアップ済みのモデルを使うことです。

[千駄ヶ谷 渋（iPhone用BlendShapeあり）](https://hub.vroid.com/characters/7307666808713466197/models/1090122510476995853)をダウンロードしたのち、VMagicMirrorでロードします。その後、`外部トラッキング(Ex.Tracker)`タブで外部トラッキングを有効にしてiOSアプリと接続します。さらに、`パーフェクトシンク`チェックをオンにします。

これで完了です！眉や口を好きに動かしたり、舌を出したりしてみて下さい。

<div class="row">
{% include docimg.html file="./images/tips/perfect_sync_setup_model_example.png" customclass="col l6 m6 s12" imgclass="fit-doc-img" %}
</div>

あるいは、上記のモデル以外で[Vear](https://apps.apple.com/jp/app/id1490697369)のパーフェクトシンクに対応したモデルをロードしても構いません。このページの下部で触れますが、Vearのパーフェクトシンクに対応したモデルは基本的にVMagicMirrorのパーフェクトシンクでも動作します。
　

2つ目の方法はVRoidモデルを[HANA_Tool](https://booth.pm/ja/items/2437978)と組み合わせることです。もしお使いのVRMがVRoid Studioモデルであり、かつブレンドシェイプのカスタマイズを特に行っていない状態であれば、モデリング作業を行わずにパーフェクトシンクに対応できます。

`HANA_Tool`の使用方法は下記ページや、その他のウェブページでご確認下さい。

参考: [クリックで実装！パーフェクトシンク　BY HANA Tool](https://hinzka.hatenablog.com/entry/2020/10/12/014540)

<div class="note-area" markdown="1">

**NOTE** 

VMagicMirror v1.6.2およびそれ以前のバージョンでは`VRoid用のデフォルト設定を使う`という機能を提供していました。

しかし、この機能はVRoid Studioのバージョンによっては表情が大きく崩れてしまう問題があったため、v1.7.0で廃止しています。

v1.6.2以下のバージョンについても、`VRoid用のデフォルト設定を使う`は現時点で非推奨の機能です。

</div>


#### セットアップStep1. モデルのブレンドシェイプ整備
{: .doc-sec2 }

ここから下では、VRMをパーフェクトシンクに対応させる手順を示します。

お使いのVRMをパーフェクトシンクに対応させるには、iOS ARKitの仕様にあわせた最大52個のブレンドシェイプを用意します。このステップではBlender等でのモデル編集が必要です。

ブレンドシェイプの種類や、各ブレンドシェイプが取るべき形状は、こちらの記事で確認できます。

[iPhoneトラッキング向けBlendShapeリスト](https://hinzka.hatenablog.com/entry/2020/06/15/072929)

実際の作例を見たい場合、前の章でも触れた触れた[千駄ヶ谷 渋（iPhone用BlendShapeあり）](https://hub.vroid.com/characters/7307666808713466197/models/1090122510476995853)などをご覧ください。

また、「こんなに細かいものは必要ない」と感じるブレンドシェイプについては作成をスキップしても構いません。ただし、まばたきを自然にするために、`EyeSquintRight`, `EyeSquintLeft`,`EyeBlinkLeft`, `EyeBlinkRight`の4つは必ず用意して下さい。


#### セットアップStep2. Unity上でのBlendShapeClip作成
{: .doc-sec2 }

前の章で準備したモデルをUnityで一度インポートします。そして、作成したブレンドシェイプに対応する52個の`BlendShapeClip`を作成します。クリップ名は、iOSの顔トラッキング仕様に沿って、以下の名前にして下さい。

<div class="doc-ul" markdown="1">

- `BrowInnerUp`
- `BrowDownLeft`
- `BrowDownRight`
- `BrowOuterUpLeft`
- `BrowOuterUpRight`
- `EyeLookUpLeft`
- `EyeLookUpRight`
- `EyeLookDownLeft`
- `EyeLookDownRight`
- `EyeLookInLeft`
- `EyeLookInRight`
- `EyeLookOutLeft`
- `EyeLookOutRight`
- `EyeBlinkLeft`
- `EyeBlinkRight`
- `EyeSquintRight`
- `EyeSquintLeft`
- `EyeWideLeft`
- `EyeWideRight`
- `CheekPuff`
- `CheekSquintLeft`
- `CheekSquintRight`
- `NoseSneerLeft`
- `NoseSneerRight`
- `JawOpen`
- `JawForward`
- `JawLeft`
- `JawRight`
- `MouthFunnel`
- `MouthPucker`
- `MouthLeft`
- `MouthRight`
- `MouthRollUpper`
- `MouthRollLower`
- `MouthShrugUpper`
- `MouthShrugLower`
- `MouthClose`
- `MouthSmileLeft`
- `MouthSmileRight`
- `MouthFrownLeft`
- `MouthFrownRight`
- `MouthDimpleLeft`
- `MouthDimpleRight`
- `MouthUpperUpLeft`
- `MouthUpperUpRight`
- `MouthLowerDownLeft`
- `MouthLowerDownRight`
- `MouthPressLeft`
- `MouthPressRight`
- `MouthStretchLeft`
- `MouthStretchRight`
- `TongueOut`

</div>

それぞれの`BlendShapeClip`では原則として、Step 1で用意したモデル側のブレンドシェイプ1つを、`weight`が`100`になるようセットアップします。

<div class="row">
{% include docimg.html file="./images/tips/perfect_sync_clip_setting_example.png" customclass="col l6 m6 s12" imgclass="fit-doc-img" %}
</div>

Step 1でブレンドシェイプを作成しなかったものについては、BlendShapeClipを空の状態にします。つまり、何の表情も動かさないクリップにします。

<div class="row">
{% include docimg.html file="./images/tips/perfect_sync_empty_clip_example.png" customclass="col l6 m6 s12" imgclass="fit-doc-img" %}
</div>

以上ののちVRMを再度エクスポートすることで、パーフェクトシンクに対応したVRMモデルが得られます。


#### リファレンス
{: .doc-sec2 }

その他、パーフェクトシンク用モデルのセットアップ方法をより詳しく知るには以下の記事などを参照ください。

[iPhoneトラッキング向けBlendShapeリスト](https://hinzka.hatenablog.com/entry/2020/06/15/072929)

[VRoidでかんたん！パーフェクトシンク（1/3）VRoidモデルのFBXエクスポート](https://hinzka.hatenablog.com/entry/2020/08/15/145040)

[パーフェクトシンクであそぼう！](https://hinzka.hatenablog.com/entry/2020/08/15/145040)

[パーフェクトシンクの顔をお着換えモデルに移植しよう](https://hinzka.hatenablog.com/entry/2020/08/17/001851)

#### 補足. Vearとの互換性が高い背景について
{: .doc-sec2 }

VMagicMirrorでパーフェクトシンクを使うためのVRMのセットアップ条件は、[Vear](https://apps.apple.com/jp/app/id1490697369)のパーフェクトシンクの要件とほぼ共通です。これは2つの背景に基づきます。

第一の背景として、パーフェクトシンクの仕様ベースとなる各リソースを公開している[Hinzkaさん](https://twitter.com/hinzka)のモデルやブログ記事のリファレンスに沿った結果、要件が共通しています。

第二の背景として、Vearの現行要件に沿ったモデルを再利用できるようにし、VRMのセットアップ作業をなるべく減らせるようにするため、モデルの要件を共通させています。

ただし、あくまで指針として要件を近づけているだけであり、完全な互換性を保証するものではないことにご注意下さい。

