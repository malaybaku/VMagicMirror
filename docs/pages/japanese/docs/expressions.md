---
layout: page
title: Expressions
permalink: /docs/expressions
---

[English](../en/docs/expressions)

# 表情のコントロール

ここではVMagicMirrorがサポートする表情コントロール方法である`Word to Motion`機能を紹介します。

#### 概要
{: .doc-sec2 }

`Word to Motion`は`VMagicMirror`で表情をコントロールする機能です。

単語をタイピングすることでキャラクターの表情を切り替えたり、簡易的なモーションを再生できたりする機能です。

もともとは単語タイピングにのみ反応する機能でしたが、現在は以下4つの方法から一つを選んで表情やモーションを再生できます。

`デバイスの割り当て`で、この機能を使う方法を設定します。

1. キーボードの単語タイピング
2. ゲームパッドのボタン操作
3. キーボードのテンキー
4. MIDIコントローラのキー押下

<div class="row">
{% include docimg.html file="/images/docs/word_to_motion_top.png" customclass="col s12 m6 l6" imgclass="fit-doc-img" %}
{% include docimg.html file="/images/docs/word_to_motion_by_gamepad.png" customclass="col s12 m6 l6" imgclass="fit-doc-img" %}
</div>

上の例では`デバイスの割り当て`で`ゲームパッド`を選択し、`Y`ボタンを押した結果、`fun`の表情を実行しています。

アイテムの並び順は`↑`ボタンや`↓`ボタンで変更できます。また、アイテムの右にある`X`ボタンを押すと作成したアイテムを削除できます。

アイテムをカスタマイズするには以下の4ステップを行います。

1. `+`ボタンを押してアイテムを追加します。既に存在するアイテムを編集する場合、このステップは不要です。
2. 設定ボタンを押し、カスタムウィンドウを開きます。
3. カスタムウィンドウ上でモーションや表情を設定します。
4. `OK`を押して変更を反映します。

<div class="row">
{% include docimg.html file="/images/docs/word_to_motion_custom_flow.png" customclass="col s12 m6 l6" imgclass="fit-doc-img" %}
{% include docimg.html file="/images/docs/word_to_motion_custom_window.png" customclass="col s12 m6 l6" imgclass="fit-doc-img" %}
</div>

カスタムウィンドウで出来る主な操作は以下の4つです。

1. このアイテムを起動するワードを指定します。
2. モーションを選びます。
    * v1.5.0やそれ以前のバージョンでは3種類のビルトインモーションのみをサポートしています。
    * v1.6.0およびそれ以降では自作モーションを使用できます。詳細は[カスタムモーションのTips](./tips/use_custom_motion)を参照下さい。
3. 表情の基本設定です。
    * `表情の動作を有効化`をオンにすることで、このアイテムで表情を動かせるようになります。
    * `(全身モーションが「なし」の場合) 表情の変化時間 [sec]`では、表情をキープする時間を設定できます。
    * `リップシンクを続行`をオンにすると、表情の適用中もリップシンクが動作します。目から上だけが動くような表情と組み合わせて活用できます。
    * `アニメーション終了後も表情を維持`をオンにすると、表情を切り替えたままにできます。
4. ブレンドシェイプの設定です。基本的には1つのブレンドシェイプのみを大きな値にし、他はゼロにします。


また、v0.9.6以降ではMIDIコントローラでも表情が切り替えられます。MIDIコントローラのキーと表情を関連づけるためには3つのステップが必要です。こちらの例を参照下さい。


<blockquote class="twitter-tweet"><p lang="ja" dir="ltr"><a href="https://twitter.com/hashtag/VMagicMirror?src=hash&amp;ref_src=twsrc%5Etfw">#VMagicMirror</a><br>MIDIコンを叩くと表情が変わるやつの進捗です。<br><br>・コントロールパネル側で、MIDIコンと実行するアイテムのマッピング設定を開く<br>・MIDIコンのキーを叩いてセットアップ<br>・セットアップ完了したら再びMIDIコンのキーを叩く<br><br>の3手順で動きます <a href="https://t.co/RDbsszWLpi">pic.twitter.com/RDbsszWLpi</a></p>&mdash; 獏星(ばくすたー) / Megumi Baxter (@baku_dreameater) <a href="https://twitter.com/baku_dreameater/status/1211990346525077504?ref_src=twsrc%5Etfw">December 31, 2019</a></blockquote> <script async src="https://platform.twitter.com/widgets.js" charset="utf-8"></script>

1. `デバイスの割り当て`を`MIDIコントローラ`にした状態で`キー割り当て`右の編集ボタンをクリックします。
2. `MIDIのノート割り当て`ウィンドウが現れるので、この状態で使用したいMIDIキーを9個、順番に押します。押したキーの番号は`変更後のノート`に記録されます。
3. 設定が完了したら`OK`をクリックしてウィンドウを閉じます。

ウィンドウを閉じたのち、設定したMIDIキーを押してみて、表情が変わることを確認します。


#### Hint
{: docs-sec2 }

`デバイスの割り当て`で`キーボード (単語入力)`を選んでいるとき、タイピング中は制御キー(Shift, Ctrl, Altなど)を押さないよう注意してください。またタイピングが極端に遅い場合、単語として認識されない事があります。

`デバイスの割り当て`で`ゲームパッド`を選んだ場合、ゲームコントローラを握って動かす動作は無効化されます。

`デバイスの割り当て`で`キーボード (数字の0-8)を選んだ場合、キーボードやマウスを動かす動作は無効化されます。

カスタムウィンドウで`アニメーション終了後も表情を維持`をオンにしたアイテムを使って表情を切り替える場合、いちど実行すると基本の表情に戻らなくなるため、表情リセット機能が必要になるはずです。

以下のような設定のアイテムを作ることで表情をリセットできます。このアイテムは既定の設定では`reset`という名称で含まれています。

{% include docimg.html file="/images/docs/word_to_motion_reset_tips.png" %}
