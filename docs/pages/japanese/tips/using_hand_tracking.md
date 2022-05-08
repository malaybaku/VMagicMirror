---
layout: page
title: ハンドトラッキングの使用について
permalink: /tips/using_hand_tracking
---

[English](../en/tips/using_hand_tracking)

# Tips: (v1.7.0b以前)ハンドトラッキングの使用について

<div class="note-area" markdown="1">

**NOTE**

このページはv1.7.0bおよびそれ以前のバージョンで利用可能なハンドトラッキングに関して説明しています。

v1.8.0以降を使用している場合、最新の[ハンドトラッキング](../docs/hand_tracking)のページをご覧下さい。

</div>

`VMagicMirror`で実装されている画像ハンドトラッキングは軽量に動作するかわり、いくつかの制限や注意点があります。

快適に使用するには次の制限、および注意点をご覧ください。

#### 制限
{: .doc-sec2 }

手の正確な位置を取得するものではありません。

指を一本ずつ正確に立てたり下げたりすることはできません。簡易的なグー、チョキ、パーの検出のみが利用可能です。

手が動かせるエリアは上下左右に限定されており、前後には動かせません。


#### 注意点
{: .doc-sec2 }

照明について、手および顔がウェブカメラに明るく映る環境で利用して下さい。

背景について、肌色に近いものがウェブカメラに写り込まないようにしてください。

服装について、手と顔以外の肌の露出が少ない格好で使って下さい。長袖のシャツを着ていることが最も理想的ですが、それ以外でも胸元、肩、ひじが隠れているとより安定します。

服装について、露出が少ないことに加え、肌の色に近い服は避けてください。


#### 補足: VMagicMirrorのハンドトラッキングの仕組み
{: .doc-sec2 }

VMagicMirrorのハンドトラッキングは顔トラッキング技術をベースにして、以下のステップで実現されています。ハンドトラッキングを利用するうえで仕組みの理解は必須ではありませんが、注意点や制限をより正確に理解したい場合はあわせて参照下さい。

Step1: 従来からある顔トラッキングの仕組みで、顔のエリアを検出します。

Step2: 顔エリアの中心部分の色を平均することで、ユーザーの肌の色を推定します。

Step3: 顔から水平方向にはなれたエリアで、ユーザーの肌の色に近い色の領域(Blob)を検出します。

Step4: 検出した領域(Blob)がある程度大きければ、手の領域として認識します。

Step5: Blobの形状を計算することで、指がはっきりと立っている本数を推定します。

Step6: Step4, Step 5で取得した手の大きさや映り方、指が立っているのが確認できた本数などから、手の形をグー、チョキ、パーのいずれかであると推定します。

以下は開発中の様子を示したツイートです。

<blockquote class="twitter-tweet"><p lang="ja" dir="ltr"><a href="https://twitter.com/hashtag/VMagicMirror?src=hash&amp;ref_src=twsrc%5Etfw">#VMagicMirror</a><br>ひじょーーーーーーに今更なんですが、画像ベースのハンドトラッキング機能を作成中です。<br><br>ウェブカメラのみを使って顔検出とセットで動く機能で、CPU負荷が低いのがポイントです。<br><br>手の向きとかグーチョキパーの反映くらいまで出来るようになったらリリースしたいな～という感じです <a href="https://t.co/QWOhDRbDYG">pic.twitter.com/QWOhDRbDYG</a></p>&mdash; 獏星(ばくすたー) / Megumi Baxter (@baku_dreameater) <a href="https://twitter.com/baku_dreameater/status/1237380280127643650?ref_src=twsrc%5Etfw">March 10, 2020</a></blockquote> <script async src="https://platform.twitter.com/widgets.js" charset="utf-8"></script>

動画のなかでも左側の実写カメラの映像を参照してください。

黒く塗られた領域は顔の領域です。顔付近を手と誤って認識しないように、顔の上下では手を検出していません。

ピンク色の丸は表示されるのは指の間を検出しているものです。低精度ながら検出ができている様子をご覧下さい。