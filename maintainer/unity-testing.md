# Unity Testability Plan

WPF 先行で進めるが、Unity 側の準備タスクも先に定義しておく。

## Main Issue

- WPF プロセスが起動していないと動作確認しにくい箇所がある
- その結果、Unity 単体でのテストが不足している

## Strategy Draft

- WPF 連携を抽象化し、Unity 側で差し替え可能な境界を作る
- PlayMode/Editor テストで検証可能な責務単位へ分解する
- 「連携本体」と「連携アダプタ」を分離する

## Automation Options (To Be Agreed)

- Option A: Unity MCP を使って PlayMode / EditMode の実行を自動化
- Option B: Unity CLI (`-batchmode`) でテスト実行を自動化

どちらを標準にするかは、手元運用コストと再現性で決める。

## Pre-Work Checklist

- Unity の標準バージョン固定（エディタ版）
- テスト実行コマンドの固定
- ログ保管先の固定
- 失敗時に保存する成果物（Editor.log など）の範囲決定
