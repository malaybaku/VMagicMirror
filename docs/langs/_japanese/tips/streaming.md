---
layout: page
title: 配信にVMagicMirrorを使う
---

# Tips: 配信にVMagicMirrorを使う

VMagicMirrorは主要な用途として2つのケースを想定しています。

1. 配信サービス上での動画配信
2. デスクトップマスコット

ここではとくに、動画配信に向けたセットアップ情報をまとめています。

#### 実機画面キャプチャとクロマキー合成をえらぶ
{: .doc-sec2 }

VMagicMirrorの実行画面を配信ソフトでキャプチャするメジャーな方法は二通りです。

* スクリーンキャプチャ: 手元のスクリーン画面の全部、または一部をキャプチャします。
* ウィンドウキャプチャ: VMagicMirrorのウィンドウを非透過にして、非透過ウィンドウをキャプチャします。

スクリーンキャプチャの場合、ウィンドウに映っているそのままの状態が配信されます。
この方法は直感的であることに加え、影やタッチパッドといった、半透明のオブジェクトを正しく映せるメリットがあります。

ウィンドウキャプチャの場合、次のことに注意します。

影エフェクトは原則オフにします。`配信`タブの`表示`で、`影`のチェックをオフにします。

Bloomエフェクトがクロマキーと競合することがあります。必要に応じてオフにします。設定ウィンドウの`エフェクト`タブで、`Bloom`の`強さ`を0まで引き下げます。

もしアバターの配色が原色グリーンを含む場合、クロマキーを変更します。このとき、VMagicMirrorの背景色も変更します。設定ウィンドウの`ウィンドウ`タブで、`背景色`を緑以外の色へ変更します。

キーボードやタッチパッドの見栄えが悪い場合は非表示にするか、デバイスのテクスチャを不透明なものに差し替えます。不透明テクスチャに差し替える場合は、[キーボードやタッチパッドの見た目を変更する](../change_textures)を参照して下さい。

また、OBSに特有の設定として`ゲームキャプチャ`が挙げられます。ゲームキャプチャには透過ウィンドウの透過状態をそのままにしてコンポジットするオプションがあるため、背景を透過した状態のVMagicMirrorの表示と好相性です。PCの負荷上問題がなければ、OBSにおいてはウィンドウキャプチャの代わりにゲームキャプチャを使用することを検討して下さい。


#### CPU負荷をチェックする
{: .doc-sec2 }

配信PCではVMagicMirror以外にもOBSやゲームソフトを実行しているはずです。これらのソフトにより、PC全体ではCPU負荷が高くなります。

VMagicMirrorのCPU使用率を低下するには以下のように設定を変更します。ただし設定によって外観が損なわれるため、最小限の変更に留めてください。

`配信`タブ

1. 効果大: 顔トラッキングを無効化します。
2. 効果中: リップシンクを無効化します。
3. 効果中: 影、および風を無効化します。
4. 効果小: 使っていないデバイスを非表示にします。

設定ウィンドウ

1. 効果中: `エフェクト`タブで、画質を低めにします。
2. 効果小: `レイアウト`タブで、`ゲームパッドのキャプチャ`、および`MIDIコントローラをVMagicMirrorで使用`チェックをオフにします。
    - ただし、この方法でオフにしたデバイスは使用できません。
3. 効果小: `エフェクト`タブで、`画質`で低めの画質を選択します。
4. 効果小: `エフェクト`タブで、`Bloom`の`強さ`を0にします。


#### アバターの置き方を選択する
{: .doc-sec2 }

アバターの配置を検討します。

配置にあたっては、必ずしも手元や全身を見せなくともよいことに注意します。

一般的なVTuberの配信姿勢はバストアップや首から上のみになりがちです。

キーボードやタッチパッドのモーションを「腕が何となくそれらしく動いている」という挙動だと割り切れば、顔だけを映すレイアウトはじゅうぶん有効です。

1. 雑談配信の場合、キーボードやタッチパッドが隠れるようなカメラレイアウトを検討します。
2. ゲーム配信の場合、ゲームパッドを高い位置に移動させたり、手元が見えるカメラレイアウトを検討します。
3. バストアップのレイアウトにしながら手元も見せたい場合、タッチパッドやキーボードを高い位置に移動させます。


#### 表情のコントロール方法をえらぶ
{: .doc-sec2 }

アバターの表情はいくつかの方法で制御できます。

1. キーボードの単語入力
2. キーボードのテンキー入力
3. ゲームパッドのボタン
4. MIDIコントローラ入力

詳細は[表情のコントロール](../../docs/expressions)を確認してください。
