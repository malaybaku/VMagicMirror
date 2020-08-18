---
layout: page
title: MacでVMagicMirrorを使う
permalink: /tips/perfect_sync
---

[English](../en/tips/perfect_sync)

# Tips: 外部トラッキングでパーフェクトシンクを使う

パーフェクトシンクはVMagicMirror v1.3.0で追加された機能です。

#### パーフェクトシンクとは
{: .doc-sec2 }

パーフェクトシンクは[外部トラッキング機能](../docs/external_tracker)の発展的な機能で、高度に表情を操作できます。利用にはFace ID対応のiPhoneまたはiPadが必要です。

あらかじめ[外部トラッキング機能](../docs/external_tracker)の基本機能を試してから、本ページをお読み下さい。

パーフェクトシンクでは、iOSのARKitで顔トラッキングを行って取得した52個のブレンドシェイプの全てをアバターに反映できる仕組みを提供します。これにより、VRMの規格で最低限サポートされるものより多様な表現が可能となります。

　

#### パーフェクトシンクをすぐ試すには
{: .doc-sec2 }

※とにかくセットアップに進みたい場合、このセクションをスキップして構いません。

　

パーフェクトシンクをすぐ試す方法を2つ紹介します。

1つ目の方法は、セットアップ済みのモデルを使うことです。

[千駄ヶ谷 渋（iPhone用BlendShapeあり）](https://hub.vroid.com/characters/7307666808713466197/models/1090122510476995853)をダウンロードしたのち、VMagicMirrorでロードします。その後、`外部トラッキング(Ex.Tracker)`タブで外部トラッキングを有効にしてiOSアプリと接続します。さらに、`パーフェクトシンク`チェックをオンにします。

(TODO: screenshot)

これで完了です！眉や口を好きに動かしたり、舌を出したりしてみて下さい。

(TODO: screenshot)

あるいは、上記のモデル以外で[Vear](https://apps.apple.com/jp/app/id1490697369)のパーフェクトシンクに対応したモデルをロードしても構いません。このページの下部で触れますが、Vearのパーフェクトシンクに対応したモデルは基本的にVMagicMirrorのパーフェクトシンクでも動作します。

　

2つ目の方法はVRoidモデルを使用することです。もしお使いのVRMがVRoid Studioモデルであり、かつブレンドシェイプのカスタマイズを特に行っていない状態であれば利用できます。

お使いのVRMをロードしたのち、`外部トラッキング(Ex.Tracker)`タブで外部トラッキングを有効にしてiOSアプリと接続します。さらに、`パーフェクトシンク`チェック、および`VRoid用のデフォルト設定を使う`チェックをオンにします。

これで完了です！

(TODO: ここにもスクショ)

ただし、2つ目の方法はあくまで簡易的な手段です。例えば、この方法では舌を出したり、眉を片方ずつ動かすことはできません。

　

#### セットアップStep1. モデルのブレンドシェイプ整備
{: .doc-sec2 }

ここから下では、VRMをパーフェクトシンクに対応させる手順を示します。

お使いのVRMをパーフェクトシンクに対応させるには、iOSの顔トラッキングにあわせた最大52個のブレンドシェイプを用意します。つまり、このステップではBlender等でのモデル編集が必要です。

ブレンドシェイプの種類や、各ブレンドシェイプが取るべき形状については、こちらの記事で確認してください。

https://hinzka.hatenablog.com/entry/2020/06/15/072929

実際の作例が知りたい場合、上でも触れた[千駄ヶ谷 渋（iPhone用BlendShapeあり）](https://hub.vroid.com/characters/7307666808713466197/models/1090122510476995853)などをご覧ください。

また、上記の記事に沿って作成するブレンドシェイプのうち、「こんなに細かいものは必要ない！」と感じるブレンドシェイプは作成をスキップしても構いません。

　

#### セットアップStep2. Unity上でのBlendShapeClip作成
{: .doc-sec2 }

「セットアップStep1. モデルのブレンドシェイプ整備」で準備したモデルをUnityで一度インポートします。そして、作成したブレンドシェイプに対応する52個の`BlendShapeClip`を作成します。クリップ名は、iOSの顔トラッキング仕様に沿い、以下の名前にして下さい。

`BrowInnerUp`
`BrowDownLeft`
`BrowDownRight`
`BrowOuterUpLeft`
`BrowOuterUpRight`

`EyeLookUpLeft`
`EyeLookUpRight`
`EyeLookDownLeft`
`EyeLookDownRight`
`EyeLookInLeft`
`EyeLookInRight`
`EyeLookOutLeft`
`EyeLookOutRight`

`EyeBlinkLeft`
`EyeBlinkRight`
`EyeSquintRight`
`EyeSquintLeft`
`EyeWideLeft`
`EyeWideRight`

`CheekPuff`
`CheekSquintLeft`
`CheekSquintRight`

`NoseSneerLeft`
`NoseSneerRight`

`JawOpen`
`JawForward`
`JawLeft`
`JawRight`

`MouthFunnel`
`MouthPucker`
`MouthLeft`
`MouthRight`
`MouthRollUpper`
`MouthRollLower`
`MouthShrugUpper`
`MouthShrugLower`
`MouthClose`

`MouthSmileLeft`
`MouthSmileRight`
`MouthFrownLeft`
`MouthFrownRight`
`MouthDimpleLeft`
`MouthDimpleRight`
`MouthUpperUpLeft`
`MouthUpperUpRight`
`MouthLowerDownLeft`
`MouthLowerDownRight`
`MouthPressLeft`
`MouthPressRight`
`MouthStretchLeft`
`MouthStretchRight`

`TongueOut`

それぞれの`BlendShapeClip`では原則として、Step 1で用意したモデル側のブレンドシェイプを`weight`が`100`になるようにセットアップします。

Step 1でブレンドシェイプを作成しなかったものについては、BlendShapeClipを作成直後の状態のままにして、何の表情も動かさないようなクリップにします。

(TODO: screenshot)

#### 補足. Vearとの互換性が高い背景について
{: .doc-sec2 }

VMagicMirrorでパーフェクトシンクを使うためのVRMのセットアップ条件は、[Vear](https://apps.apple.com/jp/app/id1490697369)のパーフェクトシンクの要件とほぼ共通です。これは2つの背景に基づきます。

第一の背景として、パーフェクトシンクの仕様根拠となる各リソースを公開している[Hinzkaさん](https://twitter.com/hinzka)のモデルやブログ記事のリファレンスに沿った結果、要件が似ています。

第二の背景として、Vearの現行要件に沿ったモデルを再利用できるようにし、VRMのセットアップ作業をなるべく減らせるようにするため、モデルの要件を共通させています。

ただし、あくまで指針として要件を近づけているだけであり、完全な互換性を保証するものではないことにご注意下さい。

