# WPF Testability Plan (Primary)

WPF を先行して、UI 非依存レイヤーの分離と自動テストを整備する。

## Current Facts

- Solution: `WPF/VMagicMirrorConfig.sln`
- App project: `WPF/VMagicMirrorConfig/VMagicMirrorConfig.csproj`
- Test project: `WPF/VMagicMirrorTest/VMagicMirrorTest.csproj`
- Test framework: NUnit (`Microsoft.NET.Test.Sdk` + `NUnit` + `NUnit3TestAdapter`)

## Standard Commands (Confirmed on 2026-03-08)

- Build:
  - `dotnet build WPF/VMagicMirrorConfig/VMagicMirrorConfig.csproj -c Debug -p:Platform=x86`
- Test:
  - `dotnet test WPF/VMagicMirrorTest/VMagicMirrorTest.csproj -c Debug -p:Platform=x86 --results-directory WPF/TestResults`

`VMagicMirrorConfig.csproj` が `RuntimeIdentifier=win-x86` を持つため、まず `x86` を標準とする。

## Execution Prerequisites

- .NET SDK 8/9 系がインストールされていること
- `api.nuget.org:443` へアクセス可能であること（`dotnet restore` 実行に必要）
- 初回は `dotnet restore WPF/VMagicMirrorTest/VMagicMirrorTest.csproj -p:Platform=x86` を許可
- オフライン運用時はローカルNuGetキャッシュ/ミラーを別途合意する

## Validation Record (2026-03-08)

- Build: 成功（0 warning / 0 error）
- Test: 成功（32 passed / 0 failed）

## First Milestones

- UI 非依存でテスト可能なクラス群を特定する
- 依存方向を `View -> Application(Service) -> Domain` に寄せる
- 最初の小さな抽出（1機能）で回帰テストを追加する

## Review Granularity Rule

- 抽出だけのPR
- 呼び出し置換だけのPR
- テスト追加だけのPR

を分離し、各PRのレビュー負荷を最小化する。
