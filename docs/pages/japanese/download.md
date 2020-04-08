---
layout: page
title: Download
permalink: /download
---

[English](./en/download)

# ダウンロード

[BOOTH](https://booth.pm/ja/items/1272298)からダウンロード可能です。

<a target="_blank" href="https://baku-dreameater.booth.pm/items/1272298/">
  <img src="https://asset.booth.pm/static-images/banner/468x60_02.png">
</a>

ダウンロードおよび利用にあたっては[License](./license)もご確認下さい。

無償で公開していますが、ブーストつきで購入していただけると作者が喜びます。

また、ソースコードは[GitHub](https://github.com/malaybaku/VMagicMirror)で公開しています。

### 動作環境
{: .doc-sec1 }

Windows 10で動作します。

リップシンクでは通常のマイクのほか、仮想マイク入力も選択できます。このため、ボイスチェンジャー出力をもとにリップシンクすることも可能です。

ウェブカメラは視野角が狭めで、顔全体が映るものを使用してください。VMagicMirrorではおよそ320x240の解像度に圧縮してウェブカメラの映像を用いるため、解像度が低いカメラを使っても問題ありません。

CPUやグラフィックボードは最低要件としてはほぼ何でも動作しますが、リップシンク、影の表示、顔トラッキングではCPU負荷が高くなります。

v0.9.8時点で以下2件の不具合報告を受けています。

(不具合1) AMDの古いCPUが使われている環境では、アプリケーションが起動しない場合があります。

(不具合2) 古いGPUが使われている環境では、モデルの顔部分のテクスチャが表示されないことがあります。


開発者は以下の環境で動作チェックしていますが、もしお手元のPCでVMagicMirrorの動作が不自然に重すぎる場合、PC環境を記載のうえ開発者までご連絡下さい。

**環境1: デスクトップPC**

CPU: Intel Core i7-6700K

GPU: GeForce GTX 1080

ウェブカメラ: C922 Pro Stream Webcam

マイク入力:

1. VoiceMeeter Bananaの出力
2. VT-4 WET (変声済みのVT-4の出力)
3. C922 Pro Stream Webcam


**環境2: ノートPC(Surface Book 2)**

ウェブカメラ: PC本体フロントエンドカメラ

マイク入力: PC本体マイク
