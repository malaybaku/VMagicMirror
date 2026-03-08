# Decision Log

## 2026-03-08

### D-001: 対象課題の優先順位

- Decision: テスト性の改善を、保存形式などのマクロ課題より先に実施する
- Reason: 安全に変更可能な土台を先に作る必要があるため

### D-002: 実装順

- Decision: **WPF 先行**で着手する
- Reason: UI 非依存レイヤー分解が進めば、早期に自動テスト化しやすいため

### D-003: レビュー重視の編集粒度

- Decision: 1変更1目的で分割し、レビュー待ち中の別目的追記を避ける
- Reason: レビュー効率と手戻り抑制を優先するため

### D-004: 進捗管理の入口

- Decision: 本ディレクトリの [index.md](./index.md) を単一の入口にする
- Reason: 作業中の参照先を固定し、見落としを防ぐため

### D-005: WPF自動化の初期コマンド方針

- Decision: WPF は `.sln` ではなく `.csproj` 直指定を初期標準とする
- Reason: 実行時の切り分けが明確で、ビルド/テスト自動化に使いやすいため

### D-006: WPFテスト結果の出力先

- Decision: `dotnet test` は `--results-directory WPF/TestResults` を明示する
- Reason: 実行環境差分での書き込み先問題を避けるため

### D-007: Branch / PR 運用ルール

- Decision: 本相談での作業は常に `feature/refactor_with_ui_less_mode` をベースブランチとして扱う
- Decision: 作業ブランチは `feature/codex/` で始まる命名にする
- Decision: AI はローカルでベースブランチの read と作業ブランチへの write（`git commit`）を行う
- Decision: `git push` と `gh pr create` はメンテナーの許可後に実行する
- Decision: PR のマージ先は `feature/refactor_with_ui_less_mode` とする
- Reason: 権限最小化を維持しつつ、レビューと統合の流れを一定化するため

### D-008: 高複雑度作業の開始ルール

- Decision: 変更規模が大きくなる見込みの作業は、実装前にタスク分解ドキュメントを作成する
- Decision: WPF テスト性改善の実装開始前に `wpf-task-breakdown.md` を管理基準として用いる
- Reason: 巨大PRを避け、レビュー可能な単位で継続的に進めるため

### D-009: WPF最初の分離対象候補

- Decision: 初手の分離対象は `SaveLoadDataViewModel` を第一候補とする
- Reason: UI依存点が明確で、影響範囲を抑えて分離パターンを確立しやすいため

### D-010: WPFテスト性改善の到達イメージ

- Decision: 目標は「View非依存でViewModel/Modelの主要フローを実行可能」にすること
- Decision: ダイアログ・通知・UIスレッド切り替え・Window操作などのUI依存は境界経由で差し替え可能にする
- Decision: 差し替え先はコンソール入力でもテストスタブでもよく、WPFアプリ自体のコンソール化は必須要件としない
- Reason: テスト容易性を最優先しつつ、将来の実行ホスト拡張余地を確保するため

### D-011: PR作成後のブラウザ起動

- Decision: `gh pr create` 実行後は、作成したPR URLを既定ブラウザで開く
- Reason: レビュー依頼・内容確認の導線を毎回一定にするため
