# WPF Next Candidate Note (2026-03-08)

## Compared Targets

- `HomeViewModel` (256 lines)
- `VMCPSettingViewModel` (53 lines)

## Comparison Result

- `HomeViewModel` は `OpenFileDialog/SaveFileDialog`、`MessageBoxWrapper`、`Application.Current`、
  `SettingWindow/MetroDialog` など依存点が多く、1PRでの分離範囲が膨らみやすい。
- `VMCPSettingViewModel` は確認ダイアログ依存が主で、`SettingIoViewModel` と同型の分離パターンを
  低リスクで適用できる。

## Decision

- 次の小PRは `VMCPSettingViewModel` を対象にする。
- `HomeViewModel` は分離対象をさらに小さく分割してから着手する。
