# Issue #1281 Face Tracking Refactor Tasks

## Scope

Webカメラによる顔トラッキング設定について、従来の「軽量 / 高品質」区分を廃止し、ユーザーに見える設定を機能単位へ整理する。

主な対象は WPF の `FaceTrackerPanel` と設定保存 / IPC 送信、および Unity 側の MediaPipe / FaceControl / LookAt まわり。

## Confirmed Design Decisions

- WPF の `MotionSetting.EnableWebCamHighPowerMode` と `MotionSettingModel.EnableWebCamHighPowerMode` は、保存データ互換性のため名前を据え置く。
- 上記プロパティの実体は、新仕様の「表情をカメラでトラッキング」として読み替えて使う。
- 名前と実体がずれるため、Entity / Model 付近にコードコメントを必ず追加する。
- `EnableWebCameraHighPowerModeLipSync` は、少なくとも Model 層まで旧名称を据え置く。
- `EnableWebCameraHighPowerModeLipSync` の UI 表示は「カメラで検出したリップシンクを適用」に寄せる。
- `AutoBlinkDuringFaceTracking` は廃止し、WPF の Entity / SettingModel から削除する。
- カメラベースのまばたき適用は、新規 bool 設定「カメラで検出したまばたきを適用」に帰着させる。
- 「動きをすばやく反映」は新規 bool 設定として保存し、デフォルト ON にする。
- `マウスポインタのほうを見るかどうか` は新規 int 設定として保存し、旧 LookAt 3 bool 設定は持ち越さない。
- LookAt 新設定のデフォルトは「カメラがオフのときは見る」。
- ComboBox の表示順は「つねに見る」「カメラがオフのときは見る」「つねに見ない」とする。
- 内部 enum / int 値は実装都合で決めてよい。
- 新 UI の「見る」はマウスポインタを見る意味に統一する。
- 旧「ユーザーを見る」相当の挙動は今回の改修で廃止する。
- WPF 側では各チェックボックスの値をそのまま IPC 送信する。
- `表情をカメラでトラッキング && 個別オプション` のような実効状態の AND 判定は Unity 側で行う。
- `VmmCommands` の enum 名は保存データ互換性と関係しないため、実態に合う名前へ変更する。
- `VmmCommands` の定義位置は既存位置維持にこだわらなくてよい。WPF と Unity はペアでビルドする前提。
- 読み込み専用の旧キー移行実装は不要。

## WPF Tasks

- [x] `MotionSetting` から `AutoBlinkDuringFaceTracking` を削除する。
- [x] `MotionSettingModel` から `AutoBlinkDuringFaceTracking` を削除する。
- [x] `MotionSetting` に「カメラで検出したまばたきを適用」用の bool 設定を追加する。デフォルトは ON。
- [x] `MotionSettingModel` に上記 bool の `RProperty<bool>` と IPC 送信を追加する。
- [x] `MotionSetting` に「動きをすばやく反映」用の bool 設定を追加する。デフォルトは ON。
- [x] `MotionSettingModel` に上記 bool の `RProperty<bool>` と IPC 送信を追加する。
- [x] `MotionSetting` に「マウスポインタのほうを見るかどうか」用の int 設定を追加する。デフォルトは「カメラがオフのときは見る」。
- [x] `MotionSettingModel` に上記 int の `RProperty<int>` と IPC 送信を追加する。
- [ ] `EnableWebCamHighPowerMode` が「表情をカメラでトラッキング」を表す互換名であることをコメントする。
- [x] 旧 LookAt 3 bool 設定の UI 利用箇所を、FaceTrackerPanel では新 int 設定ベースへ置き換える。
- [x] 旧 LookAt 3 bool 設定を他の画面でどう扱うか確認し、必要なら FaceTrackerPanel 側のみに新仕様を限定するか、全体を新仕様へ寄せる。
- [x] `FaceTrackerViewModel` にマイク一覧と `LipSyncMicrophoneDeviceName` を追加する。
- [x] マイク選択の空文字同期は、既存の `StreamingTabViewModels.FaceViewModel` と同じ方針にする。
- [x] `FaceTrackerViewModel` の軽量 / 高品質選択状態を廃止し、Webカメラ / 外部トラッキングの切り替えへ整理する。
- [x] `FaceTrackerViewModel` の FaceSwitch 注意表示条件を新仕様へ変更する。
- [x] 「表情をカメラでトラッキング」が OFF のときは、表情トラッキング依存機能が使えない旨の注意を表示する。
- [x] 「表情をカメラでトラッキング」が ON のときは、頬 / 舌が使えない旨の注意を表示する。
- [x] `FaceTrackerPanel.xaml` の `Webカメラ (軽量)` / `Webカメラ (高品質)` UI を統合する。
- [x] `FaceTrackerPanel.xaml` の「webカメラを用いて、高品質な...」相当の説明を削除する。
- [x] `FaceTrackerPanel.xaml` にリップシンク用マイク一覧を追加する。
- [x] `FaceTrackerPanel.xaml` に「表情をカメラでトラッキング」チェックボックスを追加する。
- [x] 表情トラッキング配下の UI を indent し、親チェックボックス OFF 時に disabled にする。
- [x] `カメラで検出したまばたきを適用` チェックボックスを追加する。
- [x] `まばたきのトラッキング設定を開く` ボタンを、まばたきチェックボックスの右に移動する。
- [x] `カメラで検出したリップシンクを適用` チェックボックスを表情トラッキング配下へ移動する。
- [x] `パーフェクトシンクを使用` とヘルプ導線を表情トラッキング配下へ移動する。
- [x] `詳細` Expander を追加する。
- [x] `詳細` Expander に `動きをすばやく反映` を追加する。
- [x] `詳細` Expander に `手前・奥への移動を有効化` を移動する。
- [x] `詳細` Expander にマウスポインタ注視 ComboBox を追加する。
- [ ] `詳細` Expander の見栄えとサイズ感について、ユーザーに目視チェックを依頼する。
- [x] `現在位置で顔をキャリブレーション` ボタンは Webカメラ設定内に維持する。
- [x] 日英リソースを新 UI 文言へ更新する。

## IPC Tasks

- [x] WPF / Unity 共通の `VmmCommands` から `AutoBlinkDuringFaceTracking` を削除する。
- [ ] `EnableWebCamHighPowerMode` コマンドを、実態に合う名前へ変更する。
- [x] 「カメラで検出したまばたきを適用」用の IPC コマンドを追加する。
- [x] 「動きをすばやく反映」用の IPC コマンドを追加する。
- [x] 「マウスポインタのほうを見るかどうか」用の IPC コマンドを追加する。
- [ ] `EnableWebCameraHighPowerModeLipSync` コマンド名を実態に寄せるか、既存名維持にするかを実装時に判断する。
- [ ] WPF の `MessageFactory` に新コマンド送信用メソッドを追加する。
- [ ] Unity 側の受信箇所を新コマンド名へ更新する。

## Unity Tasks

- [x] `FaceControlManager` から `AutoBlinkDuringFaceTracking` 依存をなくす。
- [ ] `FaceControlConfigurationReceiver` で、新しい「表情をカメラでトラッキング」コマンドを受ける。
- [ ] `FaceControlConfiguration` の `WebCamHighPower` コメントや意味が新仕様と矛盾しないように整理する。
- [ ] MediaPipe FaceLandmarker の BlendShape 出力 ON/OFF を「表情をカメラでトラッキング」に基づかせる。
- [ ] BlendShape 出力 ON/OFF 変更時に必要なら MediaPipe タスクを restart する。
- [ ] Unity 側で `表情トラッキング && カメラまばたき適用` の実効状態を判定する。
- [ ] `FaceControlManager` の Blink 選択を新仕様へ変更する。
- [ ] 表情トラッキング ON かつまばたき適用 OFF の場合、Blink は自動まばたきを使う。
- [ ] 表情トラッキング ON かつリップシンク適用 OFF の場合でも、MediaPipe の BlendShape 出力自体は ON のままにする。
- [ ] MediaPipe リップシンク適用の実効状態を Unity 側で判定する。
- [ ] パーフェクトシンク適用の実効状態を Unity 側で判定する。
- [ ] `MediaPipeFaceSwitchSetter` の動作条件を「表情をカメラでトラッキング」基準へ変更する。
- [ ] `MediaPipeFaceAttitudeController` の速い / 遅いフィルタ分岐を「動きをすばやく反映」設定へ置き換える。
- [ ] `MediaPipeTrackerBodyOffset` の速い / 遅いフィルタ分岐を「動きをすばやく反映」設定へ置き換える。
- [ ] `HeadIkIntegrator` の Webカメラ中 LookAt 無効化ロジックを、新しいマウスポインタ注視設定に合わせる。
- [ ] 「カメラがオフのときは見る」は、Webカメラ顔トラッキングが実効 ON の間はマウスポインタを見ないようにする。
- [ ] 「つねに見る」は、Webカメラ顔トラッキング中でもマウスポインタを見るようにする。
- [ ] 「つねに見ない」は、常にマウスポインタ LookAt を無効にする。
- [ ] 旧「ユーザーを見る」相当の MainCamera LookAt を今回の FaceTrackerPanel 新仕様から外す。

## Verification Tasks

- [x] WPF プロジェクトをビルドする。
- [ ] WPF テストプロジェクトを実行できる範囲で確認する。
- [ ] `rg` で `AutoBlinkDuringFaceTracking` の残存参照を確認する。
- [ ] `rg` で `EnableWebCamHighPowerMode` の残存参照を確認し、保存互換名として残す箇所とIPC旧名として残ってしまった箇所を区別する。
- [x] `rg` で旧 UI 文言 `Webカメラ (軽量)` / `Webカメラ (高品質)` / `Web Camera (Lite)` / `Web Camera (High Power)` の残存を確認する。
- [x] 差分を生成して確認する。

## Notes For This Task

- Unity のコンパイルチェックはしないでよい。
- Codex は差分を生成して確認するが、commit までは行わない。
- commit はユーザーが差分を読んだうえで行う。
- 文字列リソースの追加 / 変更は日本語と英語のみメンテナンスし、中国語のローカライズはしない。
- 中国語文言はユーザーが妥当性を判断できないため、必要な場合は英語へ fallback させる方針とする。
- 機能廃止などで絶対に使われなくなる中国語リソースキーは削除してよい。
- 旧 LookAt 3 bool 設定の UI は `FaceTrackerPanel` からは外したが、詳細設定画面側には既存設定として残している。
