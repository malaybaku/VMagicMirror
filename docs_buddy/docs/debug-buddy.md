---
uid: doc-debug-buddy
---

# 開発者モードを使ってサブキャラをデバッグする

## 開発者モード

VMagicMirrorの「サブキャラ」タブ上部から「開発者モード」をオンにできます。

開発者モードをオンにすると以下の機能が利用できます。これらのGUI機能については [メインドキュメントのBuddyに関するページ](https://malaybaku.github.io/VMagicMirror/docs/buddy/) も部分的に紹介しています。

- ログの出力レベルを変更できます。
    - 開発者モードをオンにすると、重大なエラーに加えて軽微なエラーや警告のログも出力の対象となります。
    - 重大なエラーログのみを閲覧したい場合はログ詳細度を `Fatal` や `Error` にします。より多くのログを確認する場合、 `Info` や `Vervose` を選択します。
- GUI上に直近のログは10件まで表示されます。
    - ただし、[Api.Update](xref:VMagicMirror.Buddy.IRootApi.Update) イベントに対してログ出力を行っているなど、高頻度にログを出力している場合、一部のログはGUIに表示されず欠落することがあります。この場合、前セクションで紹介したログファイルを直接開いてログを確認します。
- 個別のサブキャラについて、 `manifest.json` を編集した結果を適用したい場合に、 `再読み込み` ボタンによって再読み込みを実行できます。
    - 開発者モードがオフの場合、`manifest.json` の変更を反映するにはVMagicMirrorアプリケーション自体の再起動が必要になることに注意して下さい。

## ログ出力APIとログファイルの閲覧方法

サブキャラのスクリプトでは、以下の関数を用いてログ出力を行えます。

- [Api.Log](xref:VMagicMirror.Buddy.IRootApi.Log(System.String))
- [Api.LogWarning](xref:VMagicMirror.Buddy.IRootApi.LogWarning(System.String))
- [Api.LogError](xref:VMagicMirror.Buddy.IRootApi.Log(System.String)) 

```csharp
using System;
using VMagicMirror.Buddy;

Api.Start += () => 
{
    Api.Log("通常のログ");
    Api.LogWarning("警告ログ");
    Api.LogError("エラーログ");
}

```

この方法でログを出力するとき、前セクションで触れている開発者モードをオンにして、ログ詳細度を `Info` などのログが多く出力される設定に変更します。

出力したログはGUIに加えて、 `(MyDocuments)\VMagicMirror_Files\Logs\Buddy\(サブキャラのフォルダ名).txt` のファイルパスにテキストとして出力されます。 `サブキャラのフォルダ名` は `main.csx` を配置しているフォルダの名称が使われます。


## ログファイルが削除/再生成されるタイミングについて

上記の方法で取得できるログファイルについて、ログを含む `(MyDocuments)\VMagicMirror_Files\Logs\Buddy\` フォルダはVMagicMirrorを起動するタイミングでフォルダが一度削除され、新規のアプリケーション実行と対応するログファイルで上書きされます。

ログファイルのバックアップを保存する場合、VMagicMirrorを終了後に、ログファイルを適当な別のフォルダにコピーしてください。
