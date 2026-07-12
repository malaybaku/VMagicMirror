# MediaPipe 単一タスクモード実装 TODO

## 作業の前提

- 作業ブランチは最新の `develop` から新規作成した直後である。
- 旧 PR #1222 のブランチ全体や、その rebase 済みブランチを土台にはしない。
- 既存の通常動作を既定値として維持し、問題が起きる環境向けに任意の回避オプションを追加する。
- この文書にある「コミット」は作業の分割案を表す。まずは実装と検証を進め、指示があるまで実際の `git commit` は行わない。
- 作業ツリーに既存のユーザー変更がある場合は、それを変更・破棄・巻き戻ししない。

## 解決する課題

[Issue #1221](https://github.com/malaybaku/VMagicMirror/issues/1221) の目的は、Web カメラで顔と手を同時にトラッキングするとき、MediaPipe のタスクを複数同時実行せず、HolisticLandmarker 1つだけで処理できるモードを追加することである。

主目的は単純な高速化ではない。FaceLandmarker と Hand/HolisticLandmarker の2タスクを同時実行するとクラッシュするユーザー環境があるため、その回避策を用意することが目的である。Holistic 単独方式が従来方式より必ず軽いという前提は置かない。

## 開発経緯と仕様判断

### PR #1222 が実装したもの

[PR #1222](https://github.com/malaybaku/VMagicMirror/pull/1222) では、顔＋手、顔＋手＋肘を HolisticLandmarker 1つで処理する実装が作られた。

HolisticLandmarker は FaceLandmarker のような顔姿勢行列を返さないため、468個の顔ランドマークから頭部姿勢を推定する `HolisticFacePoseEstimator` も追加された。この推定結果は、FaceLandmarker が返す姿勢行列と目視・数値の両面で概ね一致するところまで確認されている。

### PR #1222 が close された理由

- HolisticLandmarker の顔 BlendShape、とくに blink の値が FaceLandmarker より大きく暴れ、実用上の品質を満たさなかった。
- Face＋Hand の2タスク方式より Holistic 単独方式が必ず軽い、という確証もなかった。
- 問題は独自の頭部姿勢推定ではなく、HolisticLandmarker 自体の BlendShape 品質にある。

関連する技術的な説明は次の記事にも残っている。

- [MediaPipeでHolisticLandmarkerの出力から頭部姿勢を推定するコードをAIに書いてもらった話](https://note.com/baku_dreameater/n/ne3dbb8cc21b5)

その後、複数タスクを動かすとクラッシュする環境が実際に報告されたため #1221 が再オープンされた。したがって、#1222 の全面置換は採用せず、Holistic 単独方式をオプトインのクラッシュ回避モードとして追加する。

### PR #1282 との関係

[PR #1282](https://github.com/malaybaku/VMagicMirror/pull/1282) では「Web カメラ軽量／高品質」というモード区分が廃止され、次の設定が独立した。

- 表情をカメラでトラッキング
- カメラで検出したまばたきを適用
- カメラで検出したリップシンクを適用
- Perfect Sync
- 動きをすばやく反映
- マウスポインタへの注視方法

元の [Issue #1281](https://github.com/malaybaku/VMagicMirror/issues/1281) には #1221 の後続仕様も明記されている。

- 「常に MediaPipe タスクを最大1つにする」をハンドトラッキング側のオプションとして追加する。
- このオプションとハンドトラッキングがともに有効な間は、「表情をカメラでトラッキング」を強制的にオフにする。
- 表情トラッキングが利用できない理由を UI に表示する。

したがって、Holistic BlendShape を平滑化・改善して利用することは今回のスコープに含めない。Holistic 使用時は BlendShape の出力自体を無効化する。

## 前提ライブラリ

HolisticLandmarker で顔を見失った際のクラッシュ修正が含まれる MediaPipeUnityPlugin v0.16.3 以上を使用する。

- [MediaPipeUnityPlugin v0.16.3](https://github.com/homuler/MediaPipeUnityPlugin/releases/tag/v0.16.3)
- v0.16.3 のリリースノートには、HolisticLandmarker で顔をロストした際のクラッシュ修正が明記されている。
- この前提は #1223 で `develop` に導入済みであり、リポジトリの README も v0.16.3 以上を指定している。
- 今回あらためて MediaPipeUnityPlugin を更新する必要はない。

## Phase 1: #1222 から再利用する基盤だけを取り込む

### 方針

旧 PR のコミットを通常の cherry-pick で履歴に積まず、必要な差分だけを `--no-commit` で作業ツリーへ取り込む。その後、既知の不具合と不採用仕様を除去してから実装を続ける。

次の3コミットを再利用元とする。

- `ede04586` — `HolisticFacePoseEstimator` と asmdef 一式
- `c9c26789` — `HolisticTask`、Holistic 用顔結果処理、キャリブレーション対応
- `c1b5715b` — `SetTaskActive` の同値更新を無視するガード

`c1b7515` という表記が出てきた場合は、履歴上の `c1b5715b` を指すものとして扱う。

差分の取り込み例:

```powershell
git cherry-pick --no-commit ede04586
git cherry-pick --no-commit c9c26789
git cherry-pick --no-commit c1b5715b
```

この操作ではコミットを作成しない。競合が起きた場合は、最新 `develop` の既存動作を優先しつつ、下記の必要ファイルだけを取り込む。

### 必ず行う修正

- [ ] `c9c26789` に含まれる顔姿勢推定の反転バグを直す。

取り込み直後は、概念的に次の誤った条件になっている。

```csharp
... || HolisticFacePoseEstimator.TryEstimate(...)
```

成功時にエラー扱いしないよう、次の条件にする。

```csharp
... || !HolisticFacePoseEstimator.TryEstimate(...)
```

- [ ] `HolisticFacePoseEstimator` を現在の完成形に合わせて `static class` にする。
- [ ] `HolisticLandmarkerResultExtensions` を独立ファイルへ分離する。
  - `b8e9d630` のうち、この分離だけを参考にする。
  - `HandTaskV2.cs` 末尾にある extension 定義を新規ファイルへ移す。
  - Unity の `.meta` も含める。
- [ ] `b8e9d630` が既存の `HandAndFaceLandmarkTask` と `HandAndFaceLandmarkTaskV2` に追加した `Obsolete` は取り込まない。
  - これらは単一タスクモードがオフのときの正式な実装経路として残す。
- [ ] Holistic の BlendShape 対応を削除または恒久的に無効化する。
  - `HolisticLandmarkerOptions.outputFaceBlendshapes` は `false` にする。
  - `HolisticTask.SetBlendShapeOutputActive`、BlendShape 用フィールド、BlendShape による再起動処理は不要。
  - Holistic 結果を処理するときは `expectBlendShapeOutput: false` とするか、Holistic 用ハンドラーから該当引数自体を削除する。
- [ ] Holistic の task mode と active 状態を安全に切り替えられる API に整える。
  - `SetTaskActive(bool isActive, HolisticTaskMode mode)` のように、起動と mode 指定を一体化してよい。
  - active のまま mode が変わったときだけ再起動する。
  - 同じ active 値と同じ mode の再指定では再起動しない。
- [ ] 停止後または再起動前の古い result callback が来る可能性を考慮する。
  - 停止済みタスクの結果を姿勢へ反映しない。
  - mode 切り替え時に旧タスクの結果が新 mode として処理されないか、実機でストレステストする。

### Phase 1 で取り込まないもの

次のコミットは #1222 の「全面的に Holistic へ置換する」という古い仕様に属するため、取り込まない。

- `699d6f1e` 以降の Controller/Installer 置換
- 旧タスク登録の削除
- 旧タスクへの `Obsolete` 追加
- Holistic BlendShape を実用するためのコード
- 旧タスクの未使用化に伴う設定やフラグの削除

### Phase 1 完了時の状態

- [ ] `HolisticFacePoseEstimator` が存在する。
- [ ] `HolisticTask` が存在し、顔＋手、手＋肘、顔＋手＋肘を処理できる。
- [ ] Holistic の顔姿勢とキャリブレーションを処理できる。
- [ ] Holistic BlendShape は出力しない。
- [ ] `MediaPipeTrackerTaskController` の選択ロジックはまだ `develop` と同じ。
- [ ] `MediaPipeTrackerSystemInstaller` の既存タスク登録はまだ `develop` と同じ。
- [ ] 追加した `HolisticTask` はまだ通常動作へ接続されていない。
- [ ] したがって、この時点のユーザー向け挙動は `develop` と同一である。

## Phase 2: 設定の永続化と IPC を追加する

### 新しい設定

`AlwaysUseSingleMediaPipeTask` 相当の bool 設定を追加する。実際の名前は既存の命名規則に合わせてよい。

- 既定値: `false`
- 保存対象: はい
- 既存設定ファイルに値がない場合: `false`
- 設定リセット時: `false`
- 意味: ハンドトラッキングを含む MediaPipe 処理で、同時稼働する Landmarker を最大1つに制限する。

### WPF 側の変更先

- [ ] `WPF/VMagicMirrorConfig/Model/Entity/MotionSetting.cs`
  - bool の永続化プロパティを追加する。
  - `ResetFaceBasicSetting` など、ハンドトラッキング設定を初期化する既存リセット経路にも追加する。
- [ ] `WPF/VMagicMirrorConfig/Model/SettingModel/MotionSettingModel.cs`
  - `RProperty<bool>` を追加する。
  - 値の変更時に IPC を送る。
  - Load/Save/Reset で値が反映されることを確認する。
- [ ] `WPF/VMagicMirrorConfig/Model/InterProcess/Core/MessageFactory.cs`
  - bool メッセージ生成関数を追加する。
- [ ] WPF/Unity 共通の `VmmCommands`
  - 新しいコマンドを追加する。
- [ ] Unity 側 `MediaPipeTrackerTaskController`
  - 新コマンドを `ReactiveProperty<bool>` へ bind する。

## Phase 3: 従来方式と Holistic 方式をオプションで切り替える

### 必須のタスク選択

| 顔 | 手 | 肘 | 単一タスク OFF | 単一タスク ON |
|---:|---:|---:|---|---|
| ON | OFF | - | `FaceLandmarkTask` | `FaceLandmarkTask` |
| OFF | ON | OFF | `HandTask` | `HandTask` |
| OFF | ON | ON | `HandTaskV2` | `HolisticTask.HandAndElbow` |
| ON | ON | OFF | `HandAndFaceLandmarkTask` | `HolisticTask.FaceAndHand` |
| ON | ON | ON | `HandAndFaceLandmarkTaskV2` | `HolisticTask.FaceHandAndElbow` |

補足:

- 単一タスク設定がオフの場合は、最新 `develop` と同じタスク経路とトラッキング品質を維持する。
- 顔＋手で単一タスク設定がオフの場合、Holistic に置き換えてはいけない。
- 外部トラッカーが顔を担当している場合、Web カメラ側の顔タスクは無効なので「顔 OFF」として扱う。
- 顔だけの場合は元から FaceLandmarker 1つなので、単一タスク設定がオンでも従来の FaceLandmarker を使用する。

### Unity 側の構成

- [ ] `MediaPipeTrackerSystemInstaller.cs` に `HolisticTask` を追加登録する。
- [ ] `FaceLandmarkTask`、`HandTask`、`HandTaskV2`、`HandAndFaceLandmarkTask`、`HandAndFaceLandmarkTaskV2` の既存登録を残す。
- [ ] `MediaPipeTrackerTaskController` へ単一タスク設定を含む選択ロジックを追加する。
- [ ] 顔・手・肘・外部トラッカー・単一タスク設定から、実行対象を1つの enum/record などへ集約する。
- [ ] 選択結果を `DistinctUntilChanged` 相当で監視し、同じ選択では処理し直さない。
- [ ] 選択が変わった場合は、対象外のタスクを停止してから新しい対象を開始する。
- [ ] 設定切り替えの途中でも、旧タスクと新タスクが同時稼働する時間を作らない。
- [ ] Controller の `Dispose` ですべてのタスクを停止する。
- [ ] WebCamTexture の active 状態は、顔または手の論理トラッキングが必要かどうかに基づいて維持する。

個別タスクごとに多数の `Subscribe` を作り、暗黙に相互排他にするよりも、まず単一の `TrackingTaskSelection` を計算してから適用する構成を推奨する。今回の目的自体が複数タスク同時稼働によるクラッシュ回避なので、排他性をコード上で明確にする。

## Phase 4: 表情トラッキングを安全に制約する

### 必須仕様

単一タスク設定とハンドトラッキングがともに有効な間は、カメラによる表情トラッキングを使用しない。

実効値は概念的に次とする。

```text
effectiveExpressionTracking =
    requestedExpressionTracking &&
    !(alwaysSingleMediaPipeTask && handTracking)
```

- [ ] WPF 側で単一タスク設定またはハンドトラッキングがオンになった際、両方がオンなら `EnableWebCamHighPowerMode`、すなわち現在の「表情をカメラでトラッキング」を `false` にする。
- [ ] 強制オフされた値は保存値にも反映する。
- [ ] 制約解除後に以前の `true` を自動復元しない。チェックボックスを再び操作可能にするだけにする。
- [ ] Unity 側にも同じ条件の防御を入れ、IPC の到着順や外部 IPC クライアントに依存しない。
- [ ] HolisticLandmarker の `outputFaceBlendshapes` は常に `false` とし、出力後に無視する実装にはしない。
- [ ] 単一タスク設定がオンでも手がオフなら、FaceLandmarkTask 1つで顔を処理するため、カメラによる表情トラッキングを利用可能にする。

表情トラッキングが無効な間も、次は通常どおり利用可能であること。

- マイクによるリップシンク
- 自動まばたき
- Holistic による頭部姿勢
- Holistic による手・指・肘

## Phase 5: UI を追加する

### ハンドトラッキング画面

- [ ] `WPF/VMagicMirrorConfig/ViewModel/HandTracking/HandTrackingViewModel.cs`
  - 新しい単一タスク設定を公開する。
- [ ] `WPF/VMagicMirrorConfig/View/ControlPanel/HandTrackingPanel.xaml`
  - 「MediaPipe のタスクを常に1つだけ使用する」相当のチェックボックスを追加する。
  - ハンドトラッキング有効化・肘トラッキング設定の近くに配置する。
  - クラッシュ回避用の互換オプションであり、必ずしも高速化する設定ではないことが分かる説明にする。

### 顔トラッキング画面

- [ ] `WPF/VMagicMirrorConfig/ViewModel/ControlPanel/FaceTrackerViewModel.cs`
  - 単一タスク設定とハンドトラッキングから、表情トラッキングを操作可能かどうかを公開する。
  - いずれかの値が変わった際に強制オフ条件を再評価する。
- [ ] `WPF/VMagicMirrorConfig/View/ControlPanel/FaceTrackerPanel.xaml`
  - 制約中は「表情をカメラでトラッキング」のチェックボックスを無効化する。
  - 次の趣旨の理由文を表示する。

```text
MediaPipeを1タスクだけ使用する設定とハンドトラッキングが有効なため、
カメラによる表情トラッキングは利用できません。
```

- [ ] 表情トラッキング配下のまばたき、カメラリップシンク、Perfect Sync、キャリブレーション関連 UI も既存の `EnableWebCamExpressionTracking` に従って無効になることを確認する。

### ローカライズ

- [ ] `WPF/VMagicMirrorConfig/Resources/Japanese.xaml`
- [ ] `WPF/VMagicMirrorConfig/Resources/English.xaml`
- [ ] プロジェクトの現行方針上、追加が必要な他言語リソースがあれば対応する。

最低限、次の文言を追加する。

- 単一タスク設定のラベル
- 単一タスク設定の説明
- 表情トラッキングを利用できない理由

## Phase 6: 検証

### ビルドと静的確認

- [ ] WPF をビルドする。

```powershell
dotnet build WPF/VMagicMirrorConfig/VMagicMirrorConfig.csproj -c Debug -p:Platform=x86
```

- [ ] WPF テストを実行する。

```powershell
dotnet test WPF/VMagicMirrorTest/VMagicMirrorTest.csproj -c Debug -p:Platform=x86 --results-directory WPF/TestResults
```

- [ ] Unity のコンパイルを確認する。
- [ ] `uloop compile` が Roslyn Scripting の `System.Runtime.Loader.AssemblyLoadContext` 読み込みエラーで失敗する場合、それは既知のローカル環境問題として切り分け、Unity Editor 側の Console でも確認する。
- [ ] `git diff --check` を実行する。
- [ ] 意図しない設定ファイルやユーザー変更が差分に入っていないことを確認する。

### 設定の組み合わせ

- [ ] 単一タスク設定 OFF で、最新 `develop` と同じ顔・手・肘・表情品質になる。
- [ ] 単一タスク設定 ON、手 OFF では FaceLandmarkTask が1つだけ動き、カメラ表情トラッキングも利用できる。
- [ ] 単一タスク設定 ON、顔＋手では HolisticLandmarker 1つだけが動く。
- [ ] 単一タスク設定 ON、顔＋手＋肘では HolisticLandmarker 1つだけが動く。
- [ ] 単一タスク設定 ON、外部トラッカー＋手では手用タスクが1つだけ動く。
- [ ] 単一タスク設定 ON、外部トラッカー＋手＋肘では HolisticLandmarker 1つだけが動く。
- [ ] 単一タスク設定 ON＋手 ON で、表情トラッキングが即時にオフになり、UI も操作不可になる。
- [ ] 手を OFF または単一タスク設定を OFF にすると、表情トラッキングのチェックボックスが再び操作可能になるが、自動的には ON へ戻らない。
- [ ] Holistic 使用時にカメラ由来のまばたき、画像リップシンク、Perfect Sync が動かない。
- [ ] 同条件でマイクリップシンクと自動まばたきは動く。

### トラッキング品質と安定性

- [ ] Holistic の頭部姿勢が自然に動く。
- [ ] Holistic の手首、指、肘が動く。
- [ ] 左右反転設定が顔と手に正しく適用される。
- [ ] 顔キャリブレーションが Holistic の顔姿勢でも動く。
- [ ] 手のプレビューと blink/状態プレビューが不正な値を残さない。
- [ ] 顔をフレーム外へ外してもクラッシュせず、頭部姿勢がクリアされる。
- [ ] 片手または両手をロストしたとき、対応する手・肘姿勢がクリアされる。
- [ ] カメラ稼働中に顔、手、肘、外部トラッカー、単一タスク設定を連続して切り替えても例外が出ない。
- [ ] 切り替え中に2つの Landmarker が同時稼働しない。
- [ ] 古い result callback が、新しく選択された mode の結果として反映されない。
- [ ] WebCamTexture の取得停止・再開が破綻しない。

`HolisticFacePoseEstimator` は固定の垂直 FOV 63度を仮定しているため、次も確認する。

- [ ] 正面だけでなく、上下左右に頭を回した場合。
- [ ] カメラへ近づいた場合と離れた場合。
- [ ] アスペクト比または解像度の異なるカメラ入力。
- [ ] 従来 FaceLandmarker の頭部姿勢と大きく乖離しないこと。

### 永続化

- [ ] 新設定が保存され、再起動後に復元される。
- [ ] 新設定を持たない既存設定ファイルでは `false` になる。
- [ ] 設定リセットで `false` へ戻る。
- [ ] 強制オフされた表情トラッキング設定と実動作が一致する。

## スコープ外

- Holistic BlendShape の平滑化または品質改善
- Holistic BlendShape を用いたまばたき、リップシンク、Perfect Sync
- Holistic 単独方式が必ず高速・低負荷であることの保証
- 従来の複数タスク方式の削除
- 単一タスク設定を既定でオンにする変更
- MediaPipeUnityPlugin の追加更新

## 推奨する論理的な変更単位

実際にコミットする段階になった場合は、レビューしやすいよう次の単位を推奨する。現時点ではコミットしない。

1. `Add optional Holistic tracking task foundation`
   - Holistic 基盤のみ。ユーザー向け動作は変えない。
2. `Add always-single MediaPipe task setting and IPC`
   - 永続化設定と IPC。
3. `Select Holistic task when single-task mode is enabled`
   - Controller/Installer と排他的なタスク選択。
4. `Disable webcam expression tracking in single-task mode`
   - Unity の防御と WPF の強制オフ。
5. `Add single-task mode UI and localization`
   - HandTrackingPanel、FaceTrackerPanel、日英文言。
6. 必要に応じたテスト・不具合修正コミット。

## 完了条件

- 単一タスク設定が既定でオフであり、オフ時の挙動が `develop` から退行していない。
- 単一タスク設定とハンドトラッキングがオンのとき、MediaPipe Landmarker が最大1つだけ稼働する。
- 同条件では Holistic BlendShape を生成・利用せず、UI 上もカメラ表情トラッキングが使用不可であることが明示される。
- 頭部姿勢、手、肘、キャリブレーション、ロスト処理、設定切り替えが安定して動く。
- WPF のビルドとテスト、および Unity のコンパイル・実機確認が完了している。
- ユーザーの既存変更や無関係なファイルを変更していない。
