# VMagicMirror Maintenance Index

このディレクトリは、Unity/WPF のテスト性改善を進めるための内部管理ドキュメントです。

## Scope

- 対象: `WPF/` と `VMagicMirror/` のテスト性向上
- 優先順: **WPF 先行**
- 目的: AI Coding で安全に保守速度を上げるための作業分解と進捗可視化

## Rules

- 変更は 1 目的 1 単位で分割する（レビュー容易性を優先）
- 大きな変更は `interface追加 -> 実装移行 -> テスト追加` の順で分割する
- レビュー待ち中は、同一ファイルに別目的の変更を重ねない
- 進捗更新は必ず [task-board.md](./task-board.md) に記録する
- 重要な合意事項は [decision-log.md](./decision-log.md) に記録する

## Workstream

- WPF: [wpf-testing.md](./wpf-testing.md)
- WPF breakdown: [wpf-task-breakdown.md](./wpf-task-breakdown.md)
- WPF discovery note: [wpf-discovery-2026-03-08.md](./wpf-discovery-2026-03-08.md)
- WPF next candidate note: [wpf-next-candidate-2026-03-08.md](./wpf-next-candidate-2026-03-08.md)
- Unity: [unity-testing.md](./unity-testing.md)
- Task board: [task-board.md](./task-board.md)
- Decisions: [decision-log.md](./decision-log.md)

## PR / Branch Policy (Draft)

- AI はローカルで編集・コミットまでを担当
- Push / PR 作成はメンテナーが実施（最小権限）
- PR の粒度は「レビューで 1 回で意図が追える量」に制限
- ブランチ命名: `testability/<area>/<short-topic>`

## Automation Prerequisites (To Be Agreed)

- WPF の標準ビルドコマンド
- WPF の標準テストコマンド
- Unity 側の自動確認手段（MCP 利用有無、または CLI バッチ実行）
- CI に入れる最小ゲート（少なくとも WPF build/test）

### Confirmed Defaults (WPF)

- WPF build: `dotnet build WPF/VMagicMirrorConfig/VMagicMirrorConfig.csproj -c Debug -p:Platform=x86`
- WPF test: `dotnet test WPF/VMagicMirrorTest/VMagicMirrorTest.csproj -c Debug -p:Platform=x86 --results-directory WPF/TestResults`
- 前提: NuGet への到達性（`https://api.nuget.org/v3/index.json`）
