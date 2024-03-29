---
layout: page
title: VRM AnimationをVMagicMirrorで使用する
---

# Tips: VRM AnimationをVMagicMirrorで使用する

この機能はv3.4.0およびそれ以降のVMagicMirrorで利用できます。

従来のカスタムモーション機能(「[カスタムモーションをVMagicMirrorで使用する](../use_custom_motion)」)に加え、VRMの標準定義となったVRM Animation形式のモーション(以下、VRMA)を適用できます。

<div class="note-area" markdown="1">

**NOTE**

この機能は後述の制限等も踏まえ、実験的に導入しています。VRMAの仕様が今後変わりうることにも注意して下さい。

より安定した機能としては、上述しているカスタムモーション機能をご使用ください。

</div>


#### 1. 適用できる機能と制限事項
{: .doc-sec2 }

v3.4.0時点では、VRMAを[表情/モーション](../../docs/expressions)のカスタムモーションとして使用できます。

v3.5.0およびそれ以降では、[ゲーム入力機能](../../docs/game_input)にもVRMAを使用できます。

下記の制限があることに注意してください。

<div class="doc-ul" markdown="1">

- 表情のアニメーション情報は、ファイルに含まれたとしても使用されません。
- 「表情/モーション」機能でVRMAを使う場合、`Hips`ボーンについては位置が移動しないような制限がかかります。そのため、しゃがみ/ジャンプを含むような動きについては、元のモーションと異なる見た目になります。
    - ゲーム入力機能ではこの制限はかかりません。
- ターンを行うような、正面向きから大きく動くモーションの挙動は保証しかねます。

</div>


#### 2. VRM Animation (.vrma) ファイルの入手方法
{: .doc-sec2 }

2023年12月時点で、VRMAファイル (`.vrma`) の入手方法の例は2通り挙げられます。

<div class="doc-ul" markdown="1">

- [AnimationClipToVrmaSample](https://github.com/malaybaku/AnimationClipToVrmaSample) のプロジェクトを使用すると、Unity上でHumanoid向けのモーションデータとして読み込まれたAnimationClipをVRMAファイルに変換できます。この方法を用いる場合、詳細な使い方はリンク先を参照ください。
- UniVRM v0.114.0以降のバージョンではBVHファイルを `.vrma` ファイルに変換できます。この方法では指のモーション情報が `.vrma` ファイルに保存されなくなる事に注意してください。

</div>


#### 3. ファイルの配置
{: .doc-sec2 }

VMagicMirrorを起動していない状態で、VRMAファイル(`.vrma`)を`(マイドキュメント)\VMagicMirror_Files\Motions`以下に配置します。

もし`Motions`フォルダがまだない場合、フォルダを新規作成して下さい。

※このフォルダは[カスタムモーションをVMagicMirrorで使用する](../use_custom_motion)と共通です。ファイルの拡張子が従来のカスタムモーションと異なるため、VMagicMirrorでは適切に区別して読み込まれます。

#### 4. ゲーム入力機能での選択
{: .doc-sec2 }

VMagicMirrorを起動し、[ゲーム入力](../../docs/game_input)機能の設定ウィンドウを開きます。

その後、入力方法に応じて以下のように設定します。

<div class="doc-ul" markdown="1">

- ゲームパッドのボタン、マウスクリック: ボタンで実行するモーションの一覧に、VRMAのファイル名が選択肢として表示されます。
- キーボード: VRMAの各ファイルに対し、そのモーションを実行したいキーを割り当てることができます。

</div>


#### 5. Word to Motion機能での選択
{: .doc-sec2 }

VMagicMirrorを起動し、[表情/モーション](../../docs/expressions)にある手順で編集ウィンドウを開きます。

左側のモーション選択部分で`カスタムモーション`をチェックし、選択一覧からモーションを選択します。

選択後はビルトインモーションと同様の手順で使用できます。

<div class="note-area" markdown="1">

**NOTE**

ここでモーションが表示されなかったり再生できない場合、VRMAのデータ形式が想定外になっている可能性があります。

- Animationファイルが表情のみのデータになっていないか確認してください。
- 可能な場合、UniVRMのサンプルプロジェクトで読み込めるかどうかの検証を行ってください。
- 第三者が制作したデータの場合、制作者への問い合わせをご検討ください。

</div>
