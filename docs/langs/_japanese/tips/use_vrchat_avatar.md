---
layout: page
title: VRChatアバターをVMagicMirrorで使用する
---

# Tips: VRChatアバターをVMagicMirrorで使用する
{: .no_toc }

このページでは、VRChat向けにセットアップされているアバターをVMagicMirror向けに変換して使用する方法を説明します。

{% include docimg.html file="/images/tips/use_vrchat_avatar_example.png" customclass="col s12 m6 l6" imgclass="fit-doc-img" %}

使用アバター：[しなの](https://booth.pm/ja/items/6106863)（@ぽんでろ）

<div class="note-area" markdown="1">

- このTipsの機能を使うにはVMagicMirror v5.2.0以上が必要です。推奨バージョンは5.3.0以上です。
- このTipsで記載した機能はエディションによる制限はなく、無償版と有償版で同様に動作します。
- このTipsページではUnityやVRC SDK, VCC/ALCOMの基本的な知識があることを前提としています。

</div>

<div class="toc-area" markdown="1">

#### 目次
{: .toc-header .no_toc }

* ToC
{:toc .table-of-contents }

</div>

#### VMagicMirrorに持ち込めるデータ
{: .doc-sec2 }

VRChatアバターをVMagicMirrorで使用するには以下いずれかのファイルを作成します。

<div class="doc-ul" markdown="1">

- **VRM (`.vrm`)**: モデルの形状やボーン、表情、揺れものなどをまとめたアバター用のファイルです。
- **Material Override Unity VRM (`.mouv`)**: VRMのデータとマテリアル情報をあわせて保持するファイルです。VRMのモデル情報を基盤としたうえでファイルに保持したマテリアルを適用するため、MToon以外のシェーダーもサポートします。

</div>

`.mouv` はユーザー定義シェーダーの持ち込みサポートを目的とするファイルであり、現時点(2026/11)では実質的にVMagicMirror専用のファイルフォーマットです。

<div class="note-area" markdown="1">

**NOTE: `.mouv` ファイルの再配布について**

`.mouv` ファイルにはアバターのモデルやテクスチャに加えてコンパイル済みのシェーダーが含まれます。作成したファイルを第三者に渡したりダウンロードできる形で公開したりする場合、衣装などの追加素材も含め、それぞれの利用条件でこの形式での再配布が許可されていることを確認してください。

</div>


#### 必要な操作について
{: .doc-sec2 }

手元に `.vrm` ファイルがすでにあり、見た目に問題がない場合、そのファイルをVMagicMirrorで直接読み込んで使用できます。

出力済みのVRMがあり、そのファイルを変更せずにマテリアルだけを別途用意したものに差し替えたい場合、[VRMにMToon以外のシェーダーを適用して読み込む](../use_material_override_vrm) の手順に沿って `.mouv` ファイルを取得し、VMagicMirrorで読み込めます。この場合、本ページの途中説明はスキップし、 `VMagicMirrorで読み込む` 以降のセクションを参照してください。

まだ手元にVRMがない場合や、改めてVRChat用のUnityプロジェクトから出力を行いたい場合は、本ページで紹介する　[Material Override VRM for VRChat](https://github.com/malaybaku/MaterialOverrideVrmVRC) パッケージでアバターを `.mouv` ファイルとしてエクスポートします。


#### `.mouv` ファイルの制限について
{: .doc-sec2 }

`.mouv` ファイルの利用にあたっては以下の制限があります。

<div class="doc-ul" markdown="1">

- **シェーダー**: URP(Universal Render Pipeline)をサポートするシェーダーが利用できます。Built-in RPでのみ動作するシェーダーは使用できません。
- **揺れものやConstraint**: VRMに定義できる範囲での情報が反映されます。
- **表情**: 原則としてVRMの表情が定義したものだけを利用できます。ただし例外として、後述するパーフェクトシンクについてはVRMの表情定義をせずに利用する方法もあります。
- **アニメーションやギミック**: `.mouv` はアニメーションのデータを含まないため、アニメーションを前提とした動作は持ち込めません。
- **マテリアルを変える表情**: マテリアルの色やUVを変える方法で定義したVRMの表情は、マテリアルを差し替えた場合の動作が保証されません。

</div>

とくにシェーダーに関する制限は、VMagicMirrorのRender PipelineがURPを使用していることに由来します。現時点(2026/11)でVRChatのアセットシステムはBuilt-in RPを想定しているため、シェーダーの互換性が保証されていないことに十分注意してください。

その他の制限は、`.mouv` のマテリアルを除く情報がVRMのデータに基づくことに由来します。

なお、モーションの持ち込みについてはVRM Animationをサポートしています。詳細は [VRM AnimationをVMagicMirrorで使用する](./use_vrma) 等を参照してください。


#### アバターをエクスポートする
{: .doc-sec2 }

VRChat向けのUnityプロジェクトから `.mouv` を作成するには、[Material Override VRM for VRChat](https://github.com/malaybaku/MaterialOverrideVrmVRC) を使用します。

このパッケージは、NDMF VRM ExporterによるVRMへの変換と、変換前のマテリアルを組み合わせる処理をまとめて行います。大まかな流れは次のとおりです。

<div class="doc-ul" markdown="1">

1. [baxter's VPM Packages](https://malaybaku.github.io/baku-vpm/) をVCC / ALCOMに追加し、`Material Override VRM for VRChat` を導入します。
2. パッケージの案内に沿って依存パッケージ、およびVRMへの変換設定を準備します。
3. アバターをエクスポートし、`.mouv` ファイルを保存します。推奨手順の「まとめてエクスポート」では、`.vrm` ファイルもあわせて保存されます。

</div>

より具体的な操作については [Material Override VRM for VRChat](https://github.com/malaybaku/MaterialOverrideVrmVRC/blob/main/README_usage.md) を参照してください。また、このパッケージはVRM変換時に [NDMF VRM Exporter](https://github.com/hkrn/ndmf-vrm-exporter) を使用するため、表情や揺れもののセットアップに関してはNDMF VRM Exporterの説明も参照してください。


#### VMagicMirrorで読み込む
{: .doc-sec2 }

起動後、`ホーム` タブの `PC上のファイルをロード` から `.mouv` ファイルを選択するか、または `ホーム` タブに対して `.mouv` ファイルをドラッグ & ドロップすることで、 `.vrm` を使う場合と同様にロードされます。

<div class="note-area" markdown="1">

**NOTE**

`.mouv` ファイルは構成によってファイルサイズやシェーダー/マテリアル情報を多数含むため、VRMファイルよりロード時間が長くなる傾向があります。

</div>

もし `.mouv` ファイルの表示に問題がある場合、`.vrm` との見え方を比較する等の方法でセットアップ方法に問題がないかを確認します。必要に応じて [Material Override VRM for VRChat](https://github.com/malaybaku/MaterialOverrideVrmVRC/blob/main/README_usage.md) を再確認してください。

ロードが成功した場合、 `.mouv` はマテリアル情報を除いてVRMファイルと同様に扱われます。以降の操作方法についてはVMagicMirrorの各ドキュメントを参照ください。


#### パーフェクトシンクのセットアップについて
{: .doc-sec2 }

VMagicMirrorでは細かな顔の動きを反映するためパーフェクトシンクをサポートしており、webカメラまたはFace ID対応のiPhone / iPadによってパーフェクトシンクを利用できます。表情の準備と設定については [パーフェクトシンク](../perfect_sync) を、iPhone/iPadアプリとの接続については [外部トラッキングアプリとの連携](../../docs/external_tracker)を参照してください。

Unityプロジェクトからアバターを出力する場合、まずモデルに対してARKitの表情に対応付けられるブレンドシェイプを全部(52種類)、または一部適用します。VRCFTなど、ARKitと異なる表情セットも利用できます。例えば [Vtuber ARKit52 FacialTracking BlendShapeKey AddOn](https://charlotta.booth.pm/items/6961828) では、対応アバターのモデルに52種類のARKitブレンドシェイプを追加できます。具体的な対応アバター、導入方法、利用条件などは商品ページを確認してください。

その後、 [VRChat用パッケージの設定手順](https://github.com/malaybaku/MaterialOverrideVrmVRC/blob/main/README_usage.md#perfect-sync-expressions) に沿って用意したブレンドシェイプをVRMの表情(Expression)に割り当てたのち、アバターを再エクスポートすることで、パーフェクトシンクに対応したモデルを作成できます。

<div class="note-area" markdown="1">

**NOTE: ブレンドシェイプは存在するがVRMのExpressionを紐づけていない場合**

手元の `.vrm` や `.mouv` に使いたい表情のExpressionが定義されていなくても、メッシュそのものにARKitに準拠した名称のブレンドシェイプがあれば、それを用いてパーフェクトシンクを利用できます。VMagicMirrorの詳細設定で `顔・表情` > `パーフェクトシンク` > `未定義の表情をメッシュのブレンドシェイプで補う` をオンにすることで、ブレンドシェイプを直接取得してパーフェクトシンクを動作させられます。

ただし、VRMの出力時に [Avatar Optimizer](https://github.com/anatawa12/AvatarOptimizer) を用いてブレンドシェイプを削除・統合していると、ブレンドシェイプが無くなったりリネームされたりするため、上記のVMagicMirrorのオプションは機能しなくなります。Avatar Optimizerを使う場合、必ず前述した手順に沿ってExpressionをセットアップしてください。

</div>
