---
layout: page
title: VRMにMToon以外のシェーダーを適用して読み込む
---

# Tips: VRMにMToon以外のシェーダーを適用して読み込む

このページでは、VRMモデルのマテリアルを差し替えた `.mouv` (Material Override Unity VRM) ファイルを作成し、VMagicMirrorで表示する方法を説明します。

VRChatアバターをVRMに変換した場合など、VRMモデルと、適用したい非MToonのマテリアルのアセットが手元にある方向けの手順です。

VRM 1.0 / 0.x双方のモデルが利用できますが、安定性の観点からVRM 1.0のモデルを使用することを推奨しています。

#### できること・制限
{: .doc-sec2 }

VRMをベースに、マテリアルだけを好きなものに差し替えて表示できます。差し替えない部分には元のVRMのマテリアルを使えます。

<div class="doc-ul" markdown="1">

- VMagicMirror v5.2.0以上が必要です。
- 差し替えるマテリアルには、**URP (Universal Render Pipeline) に対応するシェーダー**が必要です。任意のシェーダーや設定での表示を保証するものではありません。
- この方法でマテリアルを適用した場合、VRMの MaterialBindingによる表情（マテリアルの色やUVを変える表情など）の動作は保証されません。
- 書き出しには元のVRMのデータを使用します。Unity上でメッシュ、ボーン、コンポーネントなどを変更・追加しても、それらの変更を持ち込む用途には使えません。
- `.mouv` は通常の `.vrm` とは異なる形式です。一般的なVRM対応アプリでそのまま使えるものではありません。

</div>

<div class="note-area" markdown="1">

**NOTE**

`.mouv` ファイルにはコンパイル済みシェーダーの情報が含まれます。`.mouv` ファイルを再配布する場合は、使用するシェーダーの利用条件で、この形式での再配布が許可されていることを確認してください。

</div>

#### 1. テンプレートプロジェクトを開く
{: .doc-sec2 }

エクスポートには [MaterialOverrideVrm](https://github.com/malaybaku/MaterialOverrideVrm) のテンプレートプロジェクトを使います。必要なパッケージとURPの設定が用意されています。モデルや差し替え用マテリアルは含まれていません。

1. [v0.2.0のリリースページ](https://github.com/malaybaku/MaterialOverrideVrm/releases/tag/v0.2.0)から、`MaterialOverrideVrm-Unity6-v0.2.0.zip` をダウンロードして展開します。
2. Unity Hubで Unity 6000.3.13f1 (Unity 6.3 LTS) をインストールします。
3. Unity Hubのプロジェクト追加から、展開したフォルダ内の `Assets`、`Packages`、`ProjectSettings` が入ったプロジェクトフォルダを選択し、上記のUnityバージョンで開きます。パッケージの取得とインポートの完了を待ちます。
4. Projectウィンドウから `Assets/Scenes/Export.unity` を開きます。

<div class="note-area" markdown="1">

**NOTE**

Unity 2022を使う場合は、同じリリースページにある `MaterialOverrideVrm-Unity2022-v0.2.0.zip` をUnity 2022.3.22f1 で開いても作業できます。

</div>

<div class="note-area" markdown="1">

**NOTE: 既存プロジェクトに導入する場合**

既存プロジェクトにUPMで導入することもできます。作業前にプロジェクトをバックアップしてください。

`Packages/manifest.json` の `dependencies` に、次のUniVRMの依存関係を追加します。

```json
"com.vrmc.gltf": "https://github.com/vrm-c/UniVRM.git?path=/Packages/UniGLTF#v0.131.4",
"com.vrmc.vrm": "https://github.com/vrm-c/UniVRM.git?path=/Packages/VRM10#v0.131.4"
```

続いて `Window > Package Manager` の `+` から `Add package from git URL...` を選び、次のURLでパッケージを導入します。

```text
https://github.com/malaybaku/MaterialOverrideVrm.git?path=/Packages/com.baxter.material-override-vrm#v0.2.0
```

パッケージを入れるだけではURPに切り替わりません。 Unity 6では `Packages/manifest.json` の `dependencies` に `"com.unity.render-pipelines.universal": "17.3.0"` も指定してください（Unity 2022ではURP 14.0.10を使用します）。

`Tools > Material Overridable VRM > URP Export Setup` を開き、ダイアログに従ってURP設定を適用してください。既存のマテリアルは自動変換されないため、差し替え用マテリアルもURP対応のものを準備します。

</div>

#### 2. VRMと差し替え用マテリアルを準備する
{: .doc-sec2 }

1. 元のVRMファイルをProjectウィンドウの `Assets` 以下へ追加し、インポートの完了を待ちます。
2. インポートしたモデルのPrefabをHierarchyへドラッグ＆ドロップしてシーンに配置し、位置を原点 `(0, 0, 0)` にします。
3. 使用するURP対応シェーダー、マテリアル、テクスチャをプロジェクトへインポートします。マテリアルのシェーダーや各パラメータを調整します。
4. Hierarchyでモデルのルート（一番親のGameObject）を選択し、Inspectorの `Add Component` から `MaterialOverrideSource` を追加します。
5. `Source Vrm` に元のVRMが設定されていることを確認します。自動設定されない場合は、Projectウィンドウから元のVRMを指定します。
6. `Bindings` を展開します。`Face`、`Body` などのRendererごとに、`Material 0` などのスロットが並びます。`Current` で元のマテリアルを確認し、差し替えたいスロットの `Override` に用意したマテリアルをドラッグ＆ドロップします。

`Current` は読み取り専用です。`Override` を `None` のままにしたスロットは元のVRMのマテリアルを使用します。同じマテリアルを複数のスロットに指定しても構いません。Rendererのマテリアル欄を直接変更するのではなく、この `Override` で差し替え先を指定してください。

設定したシーンを保存します。

<div class="row">
{% include docimg.html file="/images/tips/mouv_export_inspector.png" customclass="col l12 m12 s12" imgclass="fit-doc-img" %}
</div>

<div class="note-area" markdown="1">

**NOTE**

差し替え用マテリアルから元のVRM内のテクスチャを直接参照すると、エクスポートできない場合があります。その場合は必要なテクスチャを独立したPNGなどのアセットとして保存し、マテリアルから参照してください。

</div>

#### 3. プレビューで見た目を確認する
{: .doc-sec2 }

1. Unity Editorの再生ボタンで Play Mode に入ります。
2. Hierarchyでモデルのルートを選択し、Inspectorの `MaterialOverrideSource` にある `Preview Material Override (Play Mode)` を押します。
3. SceneビューやGameビューで、意図した部分にマテリアルが適用されていること、色やテクスチャなどの見た目に問題がないことを確認します。
4. 確認後、再生ボタンをもう一度押して Play Modeを終了します。

<div class="row">
{% include docimg.html file="/images/tips/mouv_playmode_preview.png" customclass="col l12 m12 s12" imgclass="fit-doc-img" %}
</div>

プレビュー中はBindingsを変更できません。修正する場合はPlay Modeを終了してから設定を変更し、もう一度プレビューしてください。Play Mode中の設定変更は終了時に失われるため、マテリアルやBindingsの調整はEdit Modeで行ってください。

プレビューを終了すると表示は元のマテリアルに戻りますが、Edit Modeで指定した `Override` はエクスポートに使用されます。

#### 4. .mouvファイルをエクスポートする
{: .doc-sec2 }

1. Play Modeを終了した状態で、ビルドターゲットを Windows にします。Unity 6では `File > Build Profiles`、Unity 2022では `File > Build Settings` で確認・切り替えできます。
2. モデルのルートを選択し、`MaterialOverrideSource` の `Export...` を押します。
3. 保存先とファイル名を指定して、`.mouv` ファイルを出力します。

<div class="row">
{% include docimg.html file="/images/tips/mouv_export_button.png" customclass="col l12 m12 s12" imgclass="fit-doc-img" %}
</div>

#### 5. VMagicMirrorで読み込む
{: .doc-sec2 }

VMagicMirror v5.2.0以上では、ファイルからアバターを読み込むときに `.vrm` に加えて `.mouv` も選択できます。出力した `.mouv` ファイルを選ぶと、差し替えたマテリアルを適用したモデルが表示されます。
