---
layout: page
title: Effects
permalink: /docs/effects
---

[English](../../en/docs/effects)

# エフェクト

`エフェクト`タブでは画質や光、影、Bloom、風の設定を調節できます。

<div class="row">
{% include docimg.html file="/images/docs/effects_top_1.png" customclass="col s12 m6 l6" imgclass="fit-doc-img" %}
{% include docimg.html file="/images/docs/effects_top_2.png" customclass="col s12 m6 l6" imgclass="fit-doc-img" %}
</div>

#### 1. 項目ごとの設定
{: .doc-sec2 }

`画質`: 画質を設定します。

`ライト`: ライトの色、明るさ、方向を設定します。

`影`: 影の有効/無効や影の濃さ、色を設定します。

`Bloom`: キャラクターに淡い光のようなエフェクトを与えるBloom効果の色、効果の強さ、しきい値を設定します。無効にするときは`強さ[%]`の値を0にします。

`風`: キャラクターにあたる風の強さ、細かさ、向きを設定します。

#### 2. Hint
{: .doc-sec2 }

キャラクターに照明を当てるライトと影の向きは独立しています。通常は照明をキャラクターの斜め上から当て、影はほぼ正面から当てることで、それらしい見た目になります。

影の向きと奥行きを用いてキャラクターとデスクトップ画面の距離感を表現できます。以下の画像ではデフォルト設定に加えて、キャラと画面が離れているような見え方に調整した例を並べています。

<div class="row">
{% include docimg.html file="/images/docs/shadow_default.png" customclass="col s12 m4 l4" imgclass="fit-doc-img" %}
{% include docimg.html file="/images/docs/shadow_look_far.png" customclass="col s12 m4 l4" imgclass="fit-doc-img" %}
</div>

ライトおよび影は、VRMで`Unlit`系シェーダーを使っていると機能しないことに注意してください。

`風`は`VRMSpringBone`を使う機能のため、揺れものをセットアップしてある場合のみ動作します。また`風`はキャラクターに設定された全ての`VRMSpringBone`が影響を受けるため、「髪だけを揺らしてスカートは揺らさない」といった設定はサポートしていません。
