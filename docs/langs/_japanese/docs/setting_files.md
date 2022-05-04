---
layout: page
title: Setting Files
permalink: /docs/setting_files
---

[English](../../en/docs/setting_files)

# 設定ファイルの管理

このページではコントロールパネル上の`ホーム`タブにある設定ファイルの管理機能、および設定ウィンドウの`ファイル`タブから使用できる高度な機能を紹介します。

<div class="row">
{% include docimg.html file="/images/docs/setting_files_top_home.png" customclass="col s12 m6 l6" imgclass="fit-doc-img" %}
{% include docimg.html file="/images/docs/setting_files_top_tab.png" customclass="col s12 m6 l6" imgclass="fit-doc-img" %}
</div>

### はじめに: 設定ファイルの管理が必要となるケース
{: .doc-sec2 }

前提として、VMagicMirrorではカスタマイズした設定は自動的にセーブされています。

そのため、多くの場合は手動で設定を管理する必要はありません。

ここで紹介する機能が必要になるのは次のようなケースです。

<div class="doc-ul" markdown="1">

- 同一キャラクターの着せ替えモデルを素早くスイッチしたい
- 体格・表情がまったく異なるキャラクターのセットアップを切り替えたい

</div>


### 基本操作
{: .doc-sec2 }

基本機能はコントロールパネルの`ホーム`タブから使用します。

現在の設定を保存したり、保存した設定をロードしたりするには、`セーブ`または`ロード`ボタンをクリックします。

<div class="row">
{% include docimg.html file="/images/docs/setting_files_save.png" customclass="col s12 m6 l6" imgclass="fit-doc-img" %}
{% include docimg.html file="/images/docs/setting_files_load.png" customclass="col s12 m6 l6" imgclass="fit-doc-img" %}
</div>

`ロード`では手動セーブしたデータに加え、オートセーブも選択対象にできます。

<div class="note-area" markdown="1">

**NOTE**

オートセーブは特別なセーブ先で、VMagicMirrorを終了する際の設定が自動的に保存されています。
また、VMagicMirrorを起動した直後は自動でオートセーブの内容が適用されます。

</div>

とくに`ロード`について、`キャラをロード`と`キャラ以外をロード`のチェックがあることに注意して下さい。

`キャラをロード`のみチェックをオンにした場合、設定は現在の値のまま、キャラクターのみを切り替えます。同じキャラクターの着せ替えを切り替えたい場合に使用します。

`キャラ以外をロード`をチェックした場合、キャラクター以外の設定としてレイアウトやトラッキング設定などがロードされます。

両方のチェックをオフにした場合、ファイルの内容はロードされません。

<div class="note-area" markdown="1">

**NOTE**

この方法でキャラクターを切り替えるとき、ローカルファイルからロードするキャラクターのライセンスは再度チェックせずにロードされます。

一方、VRoid Hubのモデルはサーバー上でライセンスが変更されている可能性があるため、毎回ライセンスチェックを行います。

</div>

また、通常使用する必要はありませんが、`エクスポート`により、設定ファイルを任意のファイルに保存できます。`エクスポート`で出力したファイルは`インポート`から読み込めます。

ただし、`エクスポート`したファイルはキャラクター情報を含みません。そのため、`インポート`ではキャラクター以外の設定のみが反映されます。


### 高度な機能: オートメーション
{: .doc-sec2 }

<div class="note-area" markdown="1">

**NOTE**

このセクションはプログラミングやネットワークに関する予備知識を想定したセクションです。

この機能でモデルを切り替える場合、ライセンスが十分管理された、ローカルファイルのVRMを使用して下さい。

VRoid Hubのモデルは本機能では使用できません。

</div>

オートメーション機能を有効にすると、VMagicMirrorに対してUDPでメッセージを送信することで、GUIを経由せずに自動で設定ファイルを切り替えられます。


オートメーション機能を有効にするには、設定ウィンドウの`ファイル`タブで`オートメーションを有効化`をクリックします。

確認ダイアログの確認後、初回の場合はファイアウォールの設定確認ダイアログが表示されるため、設定を許可して下さい。

もしファイアウォール設定で通信を許可しなかった場合、Windowsのコントロールパネルからファイアウォール設定を変更する必要があります。

ファイアウォールを設定する方法については[iFacialMocapのトラブルシューティング](./external_tracker_ifacialmocap#troubleshoot)のQ1, Q2がほぼ同様の内容のため、参考にして下さい。

上記の設定後、指定したUDPポートにJSONテキストを送信することで、事前にセーブした設定をロードします。

送信するJSONメッセージの例を次に示します。

```
{
    "command": "load_setting_file",
    "args": 
    {
        "index": 1,
        "load_character": true,
        "load_non_character": false
    }
}
```

上記JSONのうち、変更可能なパラメータは`args`以下の3つです。

<div class="doc-ul" markdown="1">

- `index`: ロードするデータの番号です。1以上、15以下の値を指定します。
- `load_character`: 手動でロードする場合の`キャラ情報をロード`のチェックに相当します。値を`true`または`false`で指定します。
- `load_non_character`: 手動でロードする場合の`キャラ情報以外をロード`のチェックに相当します。値を`true`または`false`で指定します。

</div>

