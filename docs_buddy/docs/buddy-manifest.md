---
uid: doc-buddy-manifest
---

# manifest.jsonでサブキャラの基本設定を定義する

サブキャラ機能は動作を記述する `main.csx` スクリプトと基本設定を定義する `manifest.json` ファイルを最小限の構成要素としています。

本ページでは `manifest.json` の定義方法の例を紹介します。

## 例: 2D画像を表示するmanifest.json

同時に最大1つまでの2D画像を表示するようなサブキャラでは、次のような `manifest.json` を作成します。
この例は [Getting Started](xref:doc-getting-started) で紹介している [BuddySample.zip](../static/BuddySample.zip) に含まれるものとほぼ同様です。

> [!NOTE]
> 下記で `//` から始まる行は実際のJSONには含まれないコメントです。

```
{
  // com.company.name のフォーマットで、サブキャラを一意に特定できるような名称を指定します。
  "id": "com.developerName.buddySample",
  // GUI上に表示されるサブキャラの名称を日本語、および英語で指定しておきます。
  "displayName": {
    "ja": "サブキャラのサンプル",
    "en": "Buddy Example"
  },
  // 制作者の名前を指定します。
  "creator": "CreatorName",
  // バージョン値を指定します。
  "version": "0.1.0",
  // 制作者に関するURLを指定します。
  "creatorUrl": "https://example.com",
  // GUI上に表示されるサブキャラの設定項目の一覧を定義します。
  "property": [
    // この例では type が transform2Dであるようなデータ、つまり2次元平面上での位置やサイズが編集できるオブジェクトを定義しています。
    {
      "name": "mainImage",
      // 個別のプロパティをGUI上で表示する際の表示名も日本語、および英語で指定します。
      "displayName":
      {
        "ja": "メイン画像",
        "en": "Main Image"
      },
      // プロパティのデータの種類を指定します。 
      "type": "transform2D"
    }
  ]
}

```

## 実際に使用されるデータについて

VMagicMirror v4.0.0の時点では `displayName` および `property` が有効なデータとして使用されています。

これ以外のプロパティは、将来的にサブキャラの更新や適宜webページへの動線を追加することを目的として定義しています。

VMagicMirror v4.0.0の時点では使用していませんが、実体に応じた値を定義することを推奨しています。


## プロパティの定義とスクリプトでの取得

プロパティ一覧にデータを追加で定義することで、エンドユーザーが値を編集できるUIが追加されます。

```
  "property": [
    {
      "name": "mainImage",
      ...
    },
    {
      "name": "flip",
      "displayName":
       {
         "ja": "画像を左右反転",
         "en": "Flip Horizontal"
       },
      "type": "bool"
    }
  ]
```

プロパティの定義順はそのままGUI上での表示順として反映されます。

とくに、 `type` には様々な型のデータが指定でき、それぞれのデータに対応したGUIが提示できます。指定した `type` に応じて、`main.csx` スクリプトから対応するメソッドを呼び出すことで、ユーザーがGUI上で指定した値を取得できます。

- `bool` : [Api.Property.GetBool](xref:VMagicMirror.Buddy.IProperty.GetBool(System.String))
- `int` : [Api.Property.GetInt](xref:VMagicMirror.Buddy.IProperty.GetInt(System.String))
- `float` : [Api.Property.GetFloat](xref:VMagicMirror.Buddy.IProperty.GetFloat(System.String))
- `string` : [Api.Property.GetString](xref:VMagicMirror.Buddy.IProperty.GetString(System.String))
- `vector2` : [Api.Property.GetVector2](xref:VMagicMirror.Buddy.IProperty.GetVector2(System.String))
- `vector3` : [Api.Property.GetVector3](xref:VMagicMirror.Buddy.IProperty.GetVector3(System.String))
- `quaternion` : [Api.Property.GetQuaternion](xref:VMagicMirror.Buddy.IProperty.GetQuaternion(System.String))
- `transform2D` : [Api.Transforms.GetTransform2D](xref:VMagicMirror.Buddy.IManifestTransforms.GetTransform2D(System.String))
- `transform3D` : [Api.Transforms.GetTransform3D](xref:VMagicMirror.Buddy.IManifestTransforms.GetTransform3D(System.String))

なお、`type` には `action` という特殊な値も指定できます。 `action` については次のセクションで紹介します。

## actionプロパティ

プロパティに `type` が `action` であるようなプロパティを定義すると、プロパティに対応する編集可能なデータはとくに追加されない変わり、GUI上にボタンが提示されます。

```
  "property": [
    {
      ...
    },
    {
      "name": "jumpAction",
      "displayName":
       {
         "ja": "ジャンプ",
         "en": "Jump"
       },
      "type": "action"
    }
  ]
```

上記の例では `ジャンプ` という名称のボタンが表示されます。このボタンを押下したことは [Api.Property.ActionRequested](xref:VMagicMirror.Buddy.IProperty.ActionRequested) イベントとしてスクリプトで検出できて、引数には `name` の名称が渡されます。

たとえば、ボタンを押すことでジャンプを行うようなリアクションが以下のように実装できます。

```csharp
var sprite = Api.Create2DSprite();

...

Api.Property.ActionRequested += actionName => 
{
  if (actionName == "jumpAction)
  {
    sprite.Effects.Jump.Jump(0.5f, 24f, 1);
  }
}
```


## アプリケーションの実行中にmanifest.jsonを変更して適用する方法

`manifest.json` は原則としてVMagicMirror自体のアプリケーション起動時に1回だけ読み込まれます。

[開発者モード](xref:doc-debug-buddy) を有効にすると、個別のサブキャラに対して `manifest.json` を含めたサブキャラの再読み込みを実行するUIが追加で表示されます。

もしアプリケーションを実行しながらプロパティ一覧の変更をチェックしたい場合、 [開発者モード](xref:doc-debug-buddy) の説明も参考にしながら、開発者モードで作業を行って下さい。



## リファレンスについて

とくにプロパティ定義について、本ページでは網羅的な説明は行っていません。

`manifest.json` で定義できるデータの一覧については [manifest.jsonのリファレンス](xref:ref-manifest-json) を参照して下さい。

