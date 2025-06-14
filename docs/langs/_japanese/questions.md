---
layout: page
title: FAQ
---

# FAQ
{: .no_toc }

<div class="toc-area" markdown="1">

#### 目次
{: .toc-header .no_toc }

* ToC
{:toc .table-of-contents }

</div>

#### 初回の起動でコントロールパネルが表示されない
{: .doc-sec2 }

インストールに失敗している可能性があります。

この場合、[ダウンロードページのトラブルシューティング](../download#troubleshoot_first_startup)をご覧下さい。

トラブルシューティングの方法でも対処できなかった場合、BOOTHメッセージかTwitter DMでご連絡下さい。

特にFull Editionについては購入状況の検証が必要なため、必ずBOOTHメッセージからお問い合わせ下さい。


#### 起動直後にVMagicMirrorが停止する
{: .doc-sec2 }

設定ファイルが破損している可能性があります。

次の手順で設定を初期化します。

<div class="doc-ul" markdown="1">

1. コントロールパネルの`ホーム`タブで、右側にある`リセット`ボタンを押します。確認ダイアログが出るので`OK`を押して設定をリセットします。
    + これで復帰した場合、[2: 基本的な使い方](../get_started)の手順でセットアップします。
2. 上記の方法で直らない場合、いったんVMagicMirrorを終了します。
3. 設定のオートセーブファイルを削除します。この操作はバージョンによって操作が異なります。
    + (v2.0.7以降) マイドキュメント以下の`VMagicMirror_Files\Saves`フォルダを開き、`_autosave`ファイル、および`_preferences`ファイルを削除します。
    + (v2.0.6以前) マイドキュメント以下の`VMagicMirror_Files\Saves`フォルダを開き、`_autosave`ファイルを削除します。
4. その後、`VMagicMirror`を再び起動します。

</div>

#### CPU負荷が高すぎる
{: .doc-sec2 }

`配信`タブおよび設定ウィンドウについて、以下の設定をカスタマイズします。

<div class="doc-ul" markdown="1">

* `配信`タブ
    * 効果大: 顔トラッキングを無効化します。
    * 効果中: リップシンクを無効化します。
    * 効果中: 影、および風を無効化します。
    * 効果小: 使っていないデバイスを非表示にします。
* 設定ウィンドウ
    * 効果中: `エフェクト`タブで、画質を低めにします。
    * 効果小: `レイアウト`タブで、`ゲームパッドのキャプチャ`、および`MIDIコントローラをVMagicMirrorで使用`チェックをオフにします。
        - ただし、この方法でオフにしたデバイスは使用できません。
    * 効果小: `エフェクト`タブで、`Bloom`の`強さ`を0にします。

</div>

この設定後もCPU負荷が高い場合、モデルデータの構造が原因の可能性があるため、重すぎない公式モデル(ニコニ立体ちゃんなど)をロードしてパフォーマンスを比較してください。



#### 視線トラッキングがうまく動かない
{: .doc-sec2 }

プレイしているソフトによっては仕様(※下部参照)となります。ご了承ください。

場合によっては見栄えが改善するカスタマイズとして、マウス追尾を無効化し、代わりに常に正面を見るようにした方が自然になるかもしれません。

<div class="doc-ul" markdown="1">

* コントロールパネルの`配信`タブで`顔・表情`メニュー下の`視線の動き`を`固定`に変更します。

</div>

※以下はこのトラブルの発生原因です。

一部のFPS等のゲームでは、ゲームの実行中つねにマウスをウィンドウ中央に寄せるプログラムが動作します。

このため物理的にマウスを動かしてもポインター位置が変わらず、アバターの首が動かない、という挙動になります。

有名なソフトでこの挙動がおきる例として、VRChatのデスクトップモードなどが該当します。


#### まばたきをトラッキングしない
{: .doc-sec2 }

設定ウィンドウの`モーション`タブで`顔・表情`のうち`顔トラッキング中も自動でまばたき`がオフになっていることを確認します。

メガネをかけている場合、外して試してみます。

また、カメラが顔全体の表情をはっきりと捉えられるよう、以下のことを確認してください。

<div class="doc-ul" markdown="1">

* カメラから丁度よい距離にいること
* 髪が顔にかかりすぎていないこと
* 部屋がじゅうぶん明るいこと
* 首元がすっきりした服を着ていること
* 口元が隠れすぎていないこと (マイクで一部が隠れる程度はOKです)

</div>


#### アバターをロードしたが、どこにも表示されない
{: .doc-sec2 }

ディスプレイの解像度や配置を変更するとアバターが表示されなくなることがあります。

以下の手順で、表示位置をリセットします。

<div class="doc-ul" markdown="1">

* コントロールパネル(※`ホーム`や`配信`タブがある方のウィンドウ)をつかみ、スクリーンの左上あたりに移動します。
* 設定ウィンドウを開いて`ウィンドウ`タブを表示し、`アバター位置のリセット`ボタンを押します。
    + この時点でアバターが見えたら、[Get Started](../get_started)に沿ってアバターの位置や視点を調整します。
* まだアバターが見えない場合、設定ウィンドウの`ウィンドウ`タブで`背景を透過`のチェックをオフにして、設定ウィンドウの横にアバターウィンドウが表示されるか確認します。
* 設定ウィンドウの`レイアウト`タブで`カメラ`の項目にある`位置をリセット`ボタンを押して、視点をリセットします。
    * アバターが表示されるのを確認したら、[Get Started](../get_started)に沿ってアバターの位置や視点を調整します。

</div>

以上の手順で改善しない場合、`起動直後にVMagicMirrorが停止する`の方法をお試し下さい。


#### 「VRMをロード」でアバターを選んでも何も動かない
{: .doc-sec2 }

セキュリティソフトが起動しているとコントロールパネル、アバター表示ウィンドウの通信が失敗することがあります。

セキュリティソフトがインストールされ、動作している場合、無効にして動作するか確認してください。

事例として、COMODO Internet Securityの使用時にこの現象が起きることが報告されています。


#### 影が綺麗に映らない
{: .doc-sec2 }

<div class="doc-ul" markdown="1">

* `Unlit`系シェーダーを使っているアバターの場合、映らないことがあります。
* VRoidStudio製のモデルなどで、服のテクスチャを部分的に透過している場合、本来は透明な部分が半透明で描画されてしまう事があります。

</div>

上記以外のケースでは、起動時設定でクオリティを上げると解決する場合があります(ただしCPU負荷が上昇します)。設定ウィンドウの`エフェクト`タブで画質設定を確認して下さい。

#### ゲームパッドを抜いたらVMagicMirrorがクラッシュした
{: .doc-sec2 }

ゲームプレイ後にゲームパッド(ゲームコントローラー)のケーブルを抜くとVMagicMirrorがクラッシュすることがあります。

この症状が起きたあと、VMagicMirrorを再起動しても繰り返しクラッシュしてしまう事があります。その場合、PCを再起動してください。

#### 顔トラッキングで首が回らない
{: .doc-sec2 }

この現象はVMagicMirrorの含むフォルダが日本語など、全角文字を含むパスのフォルダ以下に配置されていると発生します。

VMagicMirrorを一度終了したのち、VMagicMirrorのフォルダを丸ごと全角文字を含まないフォルダへ移動して、再度お試し下さい。


#### Windows 11で背景が透明にならない
{: .doc-sec2 }

Windows 11で背景が透明にならない場合、次節の`背景を透明にしようとしたが、黒背景になってしまう`の方法を確認して下さい。

OSをクリーンインストールに準ずる方法でインストールした場合や、NVIDIAのグラフィックボードのドライバの更新操作によっては、グラフィックの設定がリセットされている可能性があります。

またWindows 11に関してはマシン環境依存の問題も考えられるため、次節の方法を試みても背景が透過できない場合、製作者にご連絡下さい。


#### 背景を透明にしようとしたが、黒背景になってしまう
{: .doc-sec2 }

この問題はNVIDIAコントロールパネルの設定によって発生することがあります。

VMagicMirrorを終了し、デスクトップ上で右クリックしてNVIDIAコントロールパネルを起動します。

`3D設定` > `3D設定の管理`> `アンチエイリアシング - FXAA`の項目を探し、設定をオフにします。

変更後、ふたたびVMagicMirrorを起動します。


#### ペンタブレットの動作をトラッキングしない事がある
{: .doc-sec2 }

ペンタブレット動作をトラッキングできないケースのいくつかは仕様です。

VMagicMirrorはマウス動作を監視することでペン入力を捕捉しています。そのため、マウス動作が検出できない場合はペン入力を検出できません。

トラッキングが出来ないケースの例は以下の通りです。

<div class="doc-ul" markdown="1">

* Wacom等のペンタブレットに対応した、一部のイラストレーションソフトの使用中
* Power Pointなどの、Windows Ink機能での書き込みが可能なアプリケーションに対するスタイラスペンでの書き込み中

</div>

#### アンインストールの方法を確認したい
{: .doc-sec2 }

バージョンごとにアンインストール方法が異なります。

<div class="doc-ul" markdown="1">

- v1.9.0以降の場合: Windowsの設定で`アプリと機能`からアンインストール対象としてVMagicMirrorを検索し、削除します。
- v1.8.2以前の場合: VmagicMirrorをzipから展開したフォルダを、フォルダごと削除します。

</div>

v1.9.0以降のバージョンを使用しており、VMagicMirrorの設定ファイルも削除したい場合は、マイドキュメントフォルダの`VMagicMirror_Files`フォルダも削除します。


#### iFacialMocapとの連携がうまく行かない
{: .doc-sec2 }

バージョンアップによってこの問題が起きた場合、PC再起動で直る場合があります。

そのほかのトラブルシューティングについてはiFacialMocap連携に関するページの[トラブルシューティング](../docs/external_tracker_ifacialmocap#troubleshoot)を参照下さい。


#### マイクを選択したのにリップシンクが動作しない
{: .doc-sec2 }

以下を順に確認します。

<div class="doc-ul" markdown="1">

- Windowsを再起動し、症状が改善するか確認します。
- [外部トラッキング機能](../docs/external_tracker)が無効になっている、あるいは有効ではあるが`リップシンクも外部トラッキングの値を使用`がオフであることを確認します。
- アンチウイルスソフトがインストールされている場合、VMagicMirrorを対象外にします。例えばKasperskyの製品などでは、アンチウイルスの一環としてマイク入力を制限することがあります。
- VRMのモデルに依存した問題かどうかを確認します。VRoid Studioのサンプルモデルなど、安定していると思われるモデルでリップシンクが動作するか確認して下さい。
- マイクが複数ある環境の場合、ほかのマイクでリップシンクが動作するかを確認します。
- マイクが複数ある環境の場合、いったんマイクを外し、1つずつ接続して動作を確認します。
- 以上でも改善しない場合、本ページ上部の「起動直後にVMagicMirrorが停止する」のセクションにある設定リセットをお試し下さい。

</div>


#### VRM 1.0の対応状況が知りたい
{: .doc-sec2 }

VMagicMirrorはv3.0.0でVRM 1.0に対応しました。

VRM 1.0のアバターを使う場合、v3.0.0以降のバージョンをご使用下さい。

なお、v3.0.0以降でも引き続き従来の(VRM 0.x系の)アバターを読み込めます。


#### その他の質問・要望がある
{: .doc-sec2 }

その他の質問や要望は以下いずれかの方法でご連絡ください。

<div class="doc-ul" markdown="1">

- [製作者Twitter](https://twitter.com/baku_dreameater)へのDM
- [BOOTHショップ](https://baku-dreameater.booth.pm/)の問い合わせ
- [GitHubの新規issue](https://github.com/malaybaku/VMagicMirror/issues/new)

</div>

ご連絡にあたっては以下に留意してください。

<div class="doc-ul" markdown="1">

- GitHubの新規issueは公開情報となります。
- トラブルの説明で画像情報の共有が必要な場合、BOOTHショップメッセージでは画像共有が難しいため、ほかの方法を使用してください。

</div>
