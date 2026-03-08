# Task Board

更新日: 2026-03-08

## Todo

- [ ] WPF: 最初の「UI非依存ユニット」の抽出方針を決定
- [ ] Unity: WPFプロセス依存箇所の棚卸し
- [ ] Unity: ヘッドレスで確認可能な境界設計案を作成

## Todo Outlook

- WPF: 最初の「UI非依存ユニット」の抽出方針を決定
  目的: `SaveLoadData/SettingIo` で作った分離パターンを次候補に展開する基準を決める
  着手条件: 候補VMごとの UI依存点一覧があること
  完了条件: 「次の1機能」「分離順」「テスト観点」を1ページに明記

- Unity: WPFプロセス依存箇所の棚卸し
  目的: Unity単体で検証不能な経路を先に可視化する
  着手条件: 主要フローの入口クラス一覧があること
  完了条件: 依存箇所を `必須/代替可/不要` に分類した一覧を作成

- Unity: ヘッドレスで確認可能な境界設計案を作成
  目的: WPF起動なしで最小限の自動テストを回せる構造を定義する
  着手条件: 依存棚卸しの結果があること
  完了条件: 境界インターフェース案と最初の適用対象を決定

## Doing

- [ ] WPF: HomeViewModelの分離対象をさらに小さく分割（Reset/Export/Import/Modal表示の分離）

## Review

- [ ] なし

## Done

- [x] 2026-03-08 初版ドキュメント作成（index/task-board/wpf/unity/decision-log）
- [x] 2026-03-08 WPF build/test コマンド草案と実行前提（NuGet到達性）を記録
- [x] 2026-03-08 WPF build/test コマンド実行確認（build成功、test 32件成功）
- [x] 2026-03-08 WPFテスト性改善タスクを Phase 単位に分解
- [x] 2026-03-08 WPF ViewModel のUI直接依存ホットスポットを棚卸し
- [x] 2026-03-08 SaveLoadDataUseCase のユニットテスト追加（6件、合計38件成功）
- [x] 2026-03-08 SettingIoViewModel の確認ダイアログ境界を抽象化しテスト追加（合計41件成功）
- [x] 2026-03-08 次候補比較（Home vs VMCP）を実施し、VMCPを小PR対象として選定

## Update Rule

- `Todo -> Doing -> Review -> Done` の順に移動する
- Done には日付と短い成果を書く
- Done の箇条書きが増えすぎる場合は、マイルストーン単位で1行に集約する
- 方針変更が出たら [decision-log.md](./decision-log.md) を先に更新する
