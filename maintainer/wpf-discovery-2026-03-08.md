# WPF Discovery Notes (2026-03-08)

## Scope

- 対象: `WPF/VMagicMirrorConfig/ViewModel`
- 目的: WPF 直接依存を減らすため、優先的に分離する候補を決める

## Direct UI Dependency Hotspots

以下は `Application.Current` / `Dispatcher` / `OpenFileDialog` / `SaveFileDialog` /
`MessageBoxWrapper` / `Clipboard` / `Window生成` を含むファイル。

- `ViewModel/MainWindowViewModel.cs`
- `ViewModel/ControlPanel/HomeViewModel.cs`
- `ViewModel/ControlPanel/BuddySettingViewModel.cs`
- `ViewModel/ControlPanel/FaceTrackerViewModel.cs`
- `ViewModel/ControlPanel/HelpViewModel.cs`
- `ViewModel/ControlPanel/StreamingTabViewModels.cs`
- `ViewModel/Buddy/BuddyPropertyViewModel.cs`
- `ViewModel/KeyAssign/KeyAssignViewModel.cs`
- `ViewModel/SaveFileManage/SaveLoadDataViewModel.cs`
- `ViewModel/SettingWindowTab/LayoutSettingViewModel.cs`
- `ViewModel/SettingWindowTab/MotionSettingViewModel.cs`
- `ViewModel/SettingWindowTab/SettingIoViewModel.cs`
- `ViewModel/SettingWindowTab/VMCPSettingViewModel.cs`
- `ViewModel/SettingWindowTab/WordToMotionSettingViewModel.cs`
- `ViewModel/SettingItem/VMCPSourceItemViewModel.cs`

## Current Test Coverage Snapshot

- 既存テストは主に `ApiBehavior` と `Model/Update` に集中
- `ViewModel` の直接テストは未整備

確認済みテスト群:
- `VMagicMirrorTest/ApiBehavior/KeyInteropTests.cs`
- `VMagicMirrorTest/Model/Update/ReleaseNoteTests.cs`
- `VMagicMirrorTest/Model/Update/VmmAppVersionTests.cs`

## First Extraction Candidate

第一候補: `ViewModel/SaveFileManage/SaveLoadDataViewModel.cs`

理由:
- ダイアログ確認・Dispatcher呼び出しなど、UI依存が明確
- 影響範囲が比較的閉じており、最初の分離対象として扱いやすい
- 保存/読込は回帰リスクが高く、テスト追加効果が大きい

## Next Step

- `SaveLoadDataViewModel` について以下を分離対象として設計する
  - ユーザー確認ダイアログ境界
  - UIスレッド切り替え境界
  - Save/Load処理のオーケストレーション本体
