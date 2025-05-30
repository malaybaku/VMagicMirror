# Buddy Doc

このフォルダのdocfx出力を https://github.com/malaybaku/VMagicMirrorBuddyDoc に配置することで、サブキャラ(Buddy)機能のドキュメンテーションページを公開します。

## Build

docfxが導入済みの環境で、下記によって `_site` 以下に静的ページが出力されます。

```
docfx ./docfx.json
```

## ビルド時の挙動について

ビルド時に、xml doc commentに基づくページを出力するため、レポジトリ内の `VMagicMirror/Assets/Baku/VMagicMirror/Scripts/Buddy/Interface` フォルダが参照されます。

このため、ソースコードの書かれ方によっては本フォルダに変更がなくとも `docfx` コマンドに対して警告やエラーが追加で発生する場合があります。例えば、上記のフォルダ内で 

```
using UnityEngine;
```

を行っていると基本的に静的ページのビルドはエラーになります。このエラーはファイルのpartial化などで防げる場合があります。具体例としては `QuaternionInternal.cs` 、およびこれを exclude している `docfx.json` 内の設定を参照してください。
