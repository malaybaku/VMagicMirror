---
layout: page
title: Web会議にVMagicMirrorで出席する
---

# Tips: Web会議にVMagicMirrorで出席する

このページでは、ZoomなどのWeb会議ソフトでVMagicMirrorを使う方法を紹介します。

`OBS Studio`が標準機能として仮想カメラをサポートしているため、これを使いた手順を説明します。


#### 準備: OBS Studioのインストールとセットアップ
{: .doc-sec2 }

[OBS Studio](https://obsproject.com/ja/download)のバージョン26.0以降をインストールします。
特に理由がない限り、最新バージョンを用いて下さい。

あらかじめVMagicMirrorを立ち上げておきます。

その後、OBS Studioを起動します。初めて起動するときは初期設定を行います。この設定はあとから変更可能なため、適当に選択して構いません。

初期設定ののち、`シーン`下部にあるプラスボタンから、新規シーンを適当な名称(`vmm_meeting`など)で作成します。

次に、`ソース`の下部にあるプラスボタンを押して、種類から`ゲームキャプチャ`を選びます。新規ソースを適当な名称(`vmm`など)で作成します。

<div class="row">
{% include docimg.html file="./images/tips/virtual_cam_obs_new_src.png" customclass="col l4 m4 s12" imgclass="fit-doc-img" %}
</div>

すると`ゲームキャプチャ`の初期設定を行うダイアログが表示されます。

ここで、`モード`から`特定のウィンドウをキャプチャ`を選択します。

`ウィンドウ`の右側をタップし、`[VMagicMirror.exe]: VMagicMirror`を選択します。

`透過を許可`のチェックをオンにします。

`OK`をクリックし、設定を保存します。

<div class="row">
{% include docimg.html file="./images/tips/virtual_cam_obs_property_setup.png" customclass="col l4 m4 s12" imgclass="fit-doc-img" %}
{% include docimg.html file="./images/tips/virtual_cam_obs_property_setup_finish.png" customclass="col l4 m4 s12" imgclass="fit-doc-img" %}
</div>

以上で準備は完了です。正しく設定できていれば、VMagicMirrorのウィンドウがOBSのプレビュー画面に表示されます。

VMagicMirrorが画面全体に写っていない場合、`OBS Studio`のプレビュー画面上でウィンドウをドラッグして引き伸ばすか、あるいはVMagicMirror自体のウィンドウを拡大してサイズを調整します。


#### 会議に出る手順
{: .doc-sec2 }

Web会議に出席する前に、VMagicMirrorと`OBS Studio`を起動します。

プレビュー画面にVMagicMirrorが表示されていることを確認します。

VMagicMirrorが画面全体に写っていない場合、`OBS Studio`のプレビュー画面上でウィンドウをドラッグして引き伸ばすか、あるいはVMagicMirror自体のウィンドウを拡大してサイズを調整します。

`OBS Studio`の画面右にある`仮想カメラ開始`ボタンを押し、OBSの出力がwebカメラとして認識されるようにします。

<div class="row">
{% include docimg.html file="./images/tips/virtual_cam_obs_new_src.png" customclass="col l4 m4 s12" imgclass="fit-doc-img" %}
</div>

その後、Web会議を開始します。

Web会議の種類によらず、Webカメラの選択機能があるはずなので、それを探してカメラ一覧から`OBS Virtual Camera`を選択します。

正しく選択できていれば、Web会議上にVMagicMirrorの画面が表示されます。

会議の終了後は、`OBS Studio`の画面右で`仮想カメラ停止`ボタンを押し、カメラ出力を停止します。


#### バーチャル背景などを使いたい場合
{: .doc-sec2 }

Zoomのバーチャル背景などを使いたい場合は、グリーンバックを明瞭にすることが推奨されます。

この場合、VMagicMirrorの`配信`タブで`表示`から、`キーボード`と`影`をオフにして下さい。

