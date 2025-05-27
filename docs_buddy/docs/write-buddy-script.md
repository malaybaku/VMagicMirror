---
uid: doc-write-buddy-script
---

# スクリプトを作成してサブキャラを動かす

このページではサブキャラを動作させる `C#` スクリプトである `main.csx` の記述について説明します。

本ページは、動作するサブキャラが手元にある前提での説明を行います。必要に応じて、 [Getting Started](xref:doc-getting-started) で紹介している [BuddySample](../static/BuddySample.zip) を改変用に展開しておくなどの準備を行います。

## スクリプトの例

`main.csx` スクリプトの最小限の記述例は以下の通りです。

このスクリプトでは、エンドユーザーが表示位置を調整できるような固定画像を1枚表示しています。

```csharp
// この #load ディレクティブはVS Code上での編集の補助のために定義している行であるため、そのままにして下さい。
#load "..\_Reference\Globals.csx"
using System;
// VMagicMirror専用のAPIにアクセスするためのusingステートメントです。
using VMagicMirror.Buddy;

// 画像を表示するためのインスタンスを生成します。
var sprite = Api.Create2DSprite();

// manifest.jsonで定義した名称を指定することで、
// エンドユーザーが位置・スケールを編集できるような Transform2D のインスタンスを取得します。
var parent = Api.Transforms.GetTransform2D("mainImage");

// Startイベントはサブキャラの起動時に一度だけ呼ばれます。
Api.Start += () =>
{
    // 画像の位置がユーザーの編集と連動できるように、親子関係を登録します。
    sprite.Transform.SetParent(parent);

    // 画像のサイズを定義します。画像は、アバターウィンドウ全体のサイズがおよそ 1280x720 であるとしたときのサイズで指定します。
    sprite.Size = new Vector2(128f, 128f);

    // 画像を指定して表示します。 "default.png" は、このスクリプトがあるフォルダに存在する必要があります。
    sprite.Show("default.png");
};

// Updateイベントは毎フレーム呼ばれます。これはサンプルスクリプトであるため、何もしないイベントハンドラを登録しています。
Api.Update += (deltaTime) =>
{

};
```

また、`main.csx` があるフォルダの `manifest.json` 上で、 `type` が `transform2D` であるようなプロパティを定義しておく必要があります。

```
{
  ...,
  "property": [
    {
      "name": "mainImage",
      "displayName":
       {
         "ja": "メイン画像",
         "en": "Main Image"
       },
      "type": "transform2D"
    },
    ...
  ]
}
   
```

> [!NOTE]
> `BuddySample
` を解凍したフォルダの `manifest.json` では上記のプロパティが定義してあります。


## スクリプトの実行の流れ

VMagicMirrorでは、ユーザーがサブキャラを有効化した直後に `main.csx` スクリプト全体を1回だけ実行し、その後サブキャラが無効化されるまではスクリプトの内部状態が保持されます。

サブキャラが無効化された時点でスクリプトの状態はリセットされます。また、その後のサブキャラを再起動した場合、スクリプトは再度コンパイルされます。

とくに、スクリプトの実行時に最初に呼ばれる [IRootApi.Start](xref:VMagicMirror.Buddy.IRootApi.Start) や毎フレーム実行される [IRootApi.Update](xref:VMagicMirror.Buddy.IRootApi.Update) などが C#のeventとして定義されています。これらのイベントを購読することで、サブキャラの実行後も定期的にスクリプト上の処理を実行することができます。

また、VMagicMirrorではメインアバターに対してリアクションするサブキャラを実装しやすいように、 `Start` や `Update` よりもピンポイントな状況で発火するイベントを用意しています。下記はそのようなイベントの一例です。

- アバターのまばたき: `Api.AvatarFacial.OnBlink` として取得可能な [IAvatarFacial.OnBlinked](xref:VMagicMirror.Buddy.IAvatarFacial.OnBlinked) イベント
- アバターがゲームパッドを握った状態でゲームパッドのボタン押下動作をした: `Api.AvatarMotionEvent.OnGamepadButtonDown` として取得可能な [IAvatarMotionEvent.OnGamepadButtonDown](xref:VMagicMirror.Buddy.IAvatarMotionEvent.OnGamepadButtonDown) イベント

```csharp
Api.AvatarMotionEvent.OnGamepadButtonDown += _ => { };
Api.AvatarFacial.OnBlinked += () => { };
```

## VS CodeでAPIを補完表示しながらスクリプトを作成する方法

VS Codeでスクリプトを編集する場合、次のセットアップを行っておくとAPIの関数を補完しながら編集ができます。

#### セットアップ手順

VMagicMirrorでは、以下に示す方法で補完(Intellisense)を適用しながらサブキャラのスクリプトを編集できます。

サブキャラフォルダのセットアップ: [BuddySample](../static/BuddySample.zip) に含まれるファイルを解凍し、`omnisharp.json`および `.vscode` フォルダを、下記のようなファイル構造になるよう配置します。

- `main.csx`
- `manifest.json`
- `omnisharp.json`
- `.vscode`
    - `launch.json`
    - `manifest.json`

VS Codeのセットアップ: 以下を行います。

- VS Codeに `C#` 拡張機能をインストール
- PC上に .NET SDK をインストール
- Powershell等で `dotnet tool install -g dotnet-script` を実行

スクリプトの確認: `main.csx`の冒頭に下記の行が無ければ、行を追記します。

```csharp
#load "..\_References\Globals.csx"
```

上記のセットアップを行ったのち、VS Codeを再起動して「フォルダを開く」で `main.csx` を含むサブキャラのフォルダを開くことで、補完が適用された状態になります。


#### 補完が適用される仕組みについて

上記のセットアップの大部分は、VMagicMirrorに限定しない一般的なC# Script (`.csx`)の補完環境のセットアップです。

このセットアップおよび `main.csx` の冒頭で指定したロード処理、つまり

```csharp
#load "..\_References\Globals.csx"
```

によって、 `(My Documents)\VMagicMirror_Files\Buddy\_Reference\` フォルダに含まれる以下のリソースが参照され、補完が適用されます。

- `Globals.csx`
- `VMagicMirror.Buddy.dll`
- `VMagicMirror.Buddy.xml`

> [!TIP]
> もし `_Reference` フォルダがまだ存在しない場合、 v4.0.0以降のバージョンのVMagicMirrorを一度起動すると生成されます。

なお、 `Globals.csx` をロードする `#load` の行はアプリケーションの実行時には無視されます。


## サブキャラのスクリプト実行に関する注意点

上記のほか、サブキャラのスクリプトAPIは以下のような特徴を持ちます。個別の説明は [API](xref:VMagicMirror.Buddy) 以下の各ドキュメントを参照して下さい。

#### オブジェクトの親子関係

サブキャラを2D/3Dのオブジェクトとして表示するときは、基本的に下記の親子構造を作成します。

画像を平面的に表示する場合:

- 親: `manifest.json` で `transform2D` 型のプロパティを定義することでスクリプトから取得できる [IReadOnlyTransform2D](xref:VMagicMirror.Buddy.IReadOnlyTransform2D) のインスタンス
- 子: [Api.Create2DSprite](xref:VMagicMirror.Buddy.IRootApi.Create2DSprite) で作成したスプライト画像インスタンスの [Transform](xref:VMagicMirror.Buddy.ISprite2D.Transform) プロパティとして得られる [ITransform2D](xref:VMagicMirror.Buddy.ITransform2D) のインスタンス

画像を空間的に表示する場合:

- 親: `manifest.json` で `transform3D` 型のプロパティを定義することでスクリプトから取得できる [IReadOnlyTransform3D](xref:VMagicMirror.Buddy.IReadOnlyTransform3D) のインスタンス
- 子: [Api.Create3DSprite](xref:VMagicMirror.Buddy.IRootApi.Create3DSprite) で作成したスプライト画像インスタンスの [Transform](xref:VMagicMirror.Buddy.ISprite3D.Transform) プロパティとして得られる [ITransform3D](xref:VMagicMirror.Buddy.ITransform3D) のインスタンス

`transform2D` や `transform3D` として定義した Transform の姿勢はエンドユーザーがGUIでの数値編集、またはフリーレイアウトモードによるギズモ操作を通じて編集されます。

これらのオブジェクトの子要素として視覚要素のある2D/3Dオブジェクトを設定することで、基本姿勢が編集できるようなサブキャラがセットアップできます。

つまり、下記のようなスクリプトはサブキャラの実装としては典型的な構成になります。

```csharp
var sprite = Api.Create2DSprite();
var parent = Api.Transforms.GetTransform2D("myTransform");
sprite.Transform.SetParent(parent);
```

親オブジェクトを設定することはスクリプトを実行するうえでは必須ではありませんが、エンドユーザーがサブキャラの表示位置を制御できるようにする場合、なるべく上記の構成に沿って下さい。

#### スクリプトの実行制限

VMagicMirrorのサブキャラ用スクリプトは下記の制限を受けます。

- `#load ..\_Reference\Globals.csx` による読み込み処理は特別な処理として無視されます。
- それ以外の `#load` ディレクティブでは、 `main.csx` のあるフォルダ、またはその親フォルダである `Buddy` フォルダに含まれるスクリプトが読み込めます。
    - `Buddy` フォルダに含まれないスクリプトを `#load` で読み込もうとした場合、そのスクリプトは空のスクリプトと見なされるか、またはファイルが見つからない旨のコンパイルエラーになります。
- `#r` ディレクティブは使用できません。使用した場合、コンパイルエラーになります。
- 下記の namespace を `using` ステートメントで参照したり、これらのnamespaceのクラスにアクセスしたりするスクリプトは実行前にエラーになります。
    - `System.AppDomain`
    - `System.Configuration`
    - `System.Diagnostics`
    - `System.Environment`
    - `System.IO`
    - `System.Net`
    - `System.Reflection`
    - `System.Resources`
    - `System.Runtime.InteropServices`

とくに `System.IO` については、ファイルの存在確認や読み込みなど一部のAPIに限定して、 [VMagicMirror.Buddy.IO](xref:VMagicMirror.Buddy.IO) 名前空間でファイル、ディレクトリ、パス関連のAPIの一部を提供しています。

なお、今後のVMagicMirrorのアップデートで本制限の緩和を検討しています。

ネットワーク通信を行いたい、ネイティブライブラリと連携したい等、本制限の緩和を必要とするサブキャラの作成を考えている場合、 [このGitHub issue](https://github.com/malaybaku/VMagicMirror/issues/1109) にリアクションを行うか、または具体的な想定ユースケースをコメントする等の方法でcontributeを検討して下さい。


#### インタラクションAPIの制限について

メインドキュメントの [サブキャラ](https://malaybaku.github.io/VMagicMirror/docs/buddy) のページや [ダウンロード](https://malaybaku.github.io/VMagicMirror/download/) のページに記載の通り、サブキャラにはエディションに応じた挙動の違いがあります。

現在のサブキャラでインタラクションAPIが利用可能かどうかは [Api.InteractionApiEnabled](xref:VMagicMirror.Buddy.IRootApi.InteractionApiEnabled) で取得できます。

このフラグが `false` の場合、下記のAPI群には引き続きアクセスできますが、値が常にゼロなどの既定値になったり、イベントが発火しない等の制限がかかることに注意してください。

- [IInput](xref:VMagicMirror.Buddy.IInput)
- [IAvatarFacial](xref:VMagicMirror.Buddy.IAvatarFacial)
- [IAvatarMotionEvent](xref:VMagicMirror.Buddy.IAvatarMotionEvent)
- [IAvatarPose](xref:VMagicMirror.Buddy.IAvatarPose)


#### 相対パスの取り扱い

[ISprite2D.Show](xref:VMagicMirror.Buddy.ISprite2D.Show(System.String)) 等のファイルパスを引数とするメソッドでは、ファイルパスが相対パスである場合、 `main.csx` のあるフォルダから見た相対パスとして解釈されます。また、これらのメソッドには絶対パスも指定できます。

つまり、下記の呼び出しはいずれも有効です。

```csharp
ISprite2D sprite = ...;

// main.csx のあるフォルダにある myImage.png を読み込む
sprite.Show("myImage.png");

// ユーザーが指定したファイルパス等に基づいて、絶対パスで指定された画像ファイルを読み込む
sprite.Show("C:\\example\\path\\to\\image\\myImage.png");
```

なお、 `main.csx` のあるディレクトリの絶対パスは [IRootApi.BuddyDirectory](xref:VMagicMirror.Buddy.IRootApi.BuddyDirectory) で取得できます。

#### スレッド/非同期処理

サブキャラのスレッド制御における前提として、VMagicMirrorはUnity Engineで動作しており、3Dモデルや画像(テクスチャ)等、ゲームエンジンの基盤となるオブジェクトはメインスレッドのみから制御できます。 [ISprite2D.Show](xref:VMagicMirror.Buddy.ISprite2D.Show(System.String)) 等はこのスレッドの制限を受けるため、メインスレッド外からAPIを呼び出すとエラーになることがあります。

[IRootApi.Update](xref:VMagicMirror.Buddy.IRootApi.Update) など、スクリプトの基本的なイベントは全てメインスレッドから実行されるため、これらのイベントハンドラ上ではとくにスレッドの問題を考慮せずにサブキャラを操作できます。

もし `Task` で非同期的な処理を記述しながらサブキャラを制御したい場合、 [IRootApi.RunOnMainThread](xref:VMagicMirror.Buddy.IRootApi.RunOnMainThread(System.Func{System.Threading.Tasks.Task})) の使用により、タスクをメインスレッド上で実行することを検討してください。

