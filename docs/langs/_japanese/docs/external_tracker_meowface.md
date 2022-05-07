---
layout: page
title: MeowFaceとの連携
permalink: /docs/external_tracker_meowface
---

# MeowFaceとの連携
{: .no_toc }

[外部トラッキングアプリとの連携](./external_tracker)のうち、特にMeowFaceとVMagicMirrorを連携する方法にかんするページです。


### MeowFaceとは
{: .doc-sec1 }

MeowFaceはAndroid向けに無料で公開されているアプリで、iOSの顔トラッキング仕様を部分的に再現することを目的としています。

特に[iFacialMocap](./external_tracker_ifacialmocap)とは通信データの内容について互換性があります。

このため、VMagicMirrorではiFacialMocapに接続するのと同様の手順でMeowFaceに接続できます。

MeowFaceの端末スペック要件は本ドキュメントの整備時点で確認できていませんが、実際にインストールすることで試せます。

アプリはGoogle Playから取得可能です。

[MeowFace](https://play.google.com/store/apps/details?id=com.suvidriel.meowface)


### MeowFaceで出来ること/出来ないこと
{: .doc-sec1 }

MeowFaceはiOS端末の挙動を模擬するアプリであるため、出来ること、出来ないことがあります。

<div class="doc-ul" markdown="1">

できること:

- 首の回転に関するトラッキング
- まばたき、眉の動きなどのトラッキング
- [パーフェクトシンク](../tips/perfect_sync)の利用

できないこと:

- 頭部全体の移動のトラッキング
- 一部の表情パラメータのトラッキング

</div>

また、後述するように手作業での表情パラメータ調整が必要になるケースがあります。


### VMagicMirrorと接続する
{: .doc-sec1 }

MeowFaceを起動し、画面上部にAndroid端末自身のIPアドレスが表示されることを確認します。

この状態で、MeowFaceのカメラ部分に自分の顔が映るようにAndroid端末を設置します。

PC画面に戻り、`Ex Tracker`タブ > `アプリとの連携`で、`iFacialMocap`を選択します。

MeowFaceに表示されたIPアドレスを入力し、`Connect`ボタンをクリックすると接続します。

<div class="note-area" markdown="1">

**注意**

ここでキャラクターが反応しない場合、[iFacialMocapのトラブルシューティング](./external_tracker_ifacialmocap#troubleshoot)を確認して下さい。

MeowFaceとiFacialMocapはいずれもスマホ/PC間で通信を行うため、接続エラーの原因はほぼ共通です。

また、セキュリティソフトがAndroid端末との通信をブロックしている可能性もあるため、PC、iOS端末の双方で設定を確認して下さい。

</div>

接続後にアバターが横を向いてしまう場合や首が動かない場合は、ユーザーが真正面を向いた状態で`現在位置で顔をキャリブレーション`をクリックし、アバターの顔の向きをリセットします。


### 表情パラメータの調整
{: .doc-sec1 }

接続後にアバターの表情が思い通りに動かない場合、MeowFaceの画面下部にある表情パラメータの一覧をスクロールし、必要な表情について`Weight`や`Max`を調整します。

たとえば、まばたきのパラメータは`eyeBlink_L`と`eyeBlink_R`という名称です。

自分が目を閉じてもアバターの目が閉じきらない場合、これらのパラメータの`Weight`を大きい値に設定してみて下さい。


<div class="note-area" markdown="1">

**注意**

MeowFaceはiFacialMocapに比べて新しいアプリであり、今後のアップデートで挙動が変わる可能性があります。調整にあたってはアプリの最新情報をご確認下さい。

</div>
