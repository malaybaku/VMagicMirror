---
layout: page
title: Effects
---

# エフェクト

`エフェクト`タブでは画質や光、影、Bloom、風の設定を調節できます。

<div class="row">
{% include docimg.html file="/images/docs/effects_top_1.png" customclass="col s12 m6 l6" imgclass="fit-doc-img" %}
{% include docimg.html file="/images/docs/effects_top_2.png" customclass="col s12 m6 l6" imgclass="fit-doc-img" %}
</div>

#### 1. 項目ごとの設定
{: .doc-sec2 }

`画質`: 画質を設定します。

`アンチエイリアス`: アンチエイリアス(Multisample Anti-Alias)の設定です。高品質にするほど描画の負荷も高くなります。デフォルトでは無効になっています。

`ボーンのみ低FPS化`: オンにするとアバターの動きが低FPS化します。このオプションを有効にしても処理負荷は変化しません。

`ライト`: ライトの色、明るさ、方向を設定します。また、`デスクトップの色調でライトを補正`ではPC画面の色合いがリアルタイムにアバターに適用されます。

`影`: 影の有効/無効や影の濃さ、色を設定します。

`Bloom`: アバターに淡い光のようなエフェクトを与えるBloom効果の色、効果の強さ、しきい値を設定します。無効にするときは`強さ[%]`の値を0にします。

`縁取り`: v3.6.0以降で利用可能で、フチの太さ、色、画質が選択できます。縁取り効果は`背景を透過`がオンになっているときのみ適用されます。また、影、Bloom、アクセサリー類がほぼ不透明な場合、これらの要素に対しても効果が適用されることに注意してください。

`風`: アバターにあたる風の強さ、細かさ、向きを設定します。

<div class="note-area" markdown="1">

**NOTE**

`デスクトップの色調でライトを補正`を使用するとモニター上に黄色いフレーム枠が表示されます。これは内部的に使用している画面キャプチャAPIの仕様による表示です。

</div>


#### 2. Hint
{: .doc-sec2 }

アバターに照明を当てるライトと影の向きは独立しています。通常は照明をアバターの斜め上から当て、影はほぼ正面から当てることで、それらしい見た目になります。

影の向きと奥行きを用いてアバターとデスクトップ画面の距離感を表現できます。以下の画像ではデフォルト設定に加えて、アバターと画面が離れているような見え方に調整した例を並べています。

<div class="row">
{% include docimg.html file="/images/docs/shadow_default.png" customclass="col s12 m4 l4" imgclass="fit-doc-img" %}
{% include docimg.html file="/images/docs/shadow_look_far.png" customclass="col s12 m4 l4" imgclass="fit-doc-img" %}
</div>

ライトおよび影は、VRMで`Unlit`系シェーダーを使っていると機能しないことに注意してください。

`風`は`VRMSpringBone`を使う機能のため、揺れものをセットアップしてある場合のみ動作します。また`風`はアバターに設定された全ての`VRMSpringBone`が影響を受けるため、「髪だけを揺らしてスカートは揺らさない」といった設定はサポートしていません。
