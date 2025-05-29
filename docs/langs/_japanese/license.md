---
layout: page
title: License
---

# License

本ページ下部の`License Note`がVMagicMirror(以下、「本ソフト」)に関するライセンス文です。

上部の`Preamble`および`バイナリ配布物へのリバースエンジニアリングにかんする注意`は補足説明であり、ライセンス文の一部ではありません。

### Preamble
{: .doc-sec1 }

1. 本ソフトは個人利用、商用利用のいずれでもお使いいただけます。
2. 本ソフトを動画作品や配信のなかで用いる場合、クレジット表記は必須ではありません。
3. 本ソフトはソフトウェア単体としては暴力表現、性的表現のいずれも含みません。
4. 本ソフトをデスクトップマスコットなど、他人に公開しない用途で使う場合、アバターとしての利用に該当しません。
5. 本ソフトを配信、動画で使用した場合、本ソフトの作者が考え得る用途では、アバターとしての利用に該当します。
6. 本ソフトとは別に、各アバターの利用規約に従って下さい。


### バイナリ配布物にかんする注意
{: .doc-sec1 }

本項目はライセンス本体ではありませんが、配慮をお願いしたいことを2点挙げています。

#### 注意1. サードパーティについて
{: .doc-sec2 }

本ソフトのリバースエンジニアリングは完全に禁止されているわけではありませんが、特にBOOTHでバイナリ配布される本ソフトの構成物として、GitHubレポジトリにソースコードがなく、かつオープンソースではない以下のリソースが含まれます。

<div class="doc-ul" markdown="1">

- Unity Asset Storeで購入可能な有償アセット
- VRoid SDK
- 個別に制作依頼してアプリケーションに直接組み込まれている、サブキャラの画像、およびVRM (v4.0.0以降)

</div>

上記リソース群に対するリバースエンジニアリングは各頒布元のライセンスに違反する可能性があることにご留意ください。

また、上記リソースが含まれるため、BOOTHやFanboxで共有されるバイナリは再頒布できません(断言まではしかねますが、再頒布行為を行った方がサードパーティの規約に違反する可能性が極めて高いです)。


#### 注意2. フルエディションについて
{: .doc-sec2 }

v1.8.0以降では従来の無料配布される基本エディションに加え、有料でのみ入手可能なフルエディションを別途配布しています。フルエディションの詳細は[ダウンロードの案内ページ](../download)をご覧下さい。

フルエディションの配布に関連し、以下に留意下さい。ただしMITライセンスに優先するものではないので、あくまで「お願い」にとどまります。

<div class="doc-ul" markdown="1">

- フルエディションのソースコードはGitHubで公開されているため、ソースコードの理解を目的としたリバースエンジニアリングは推奨しません。
- GitHubのソースコードからフルエディション相当のビルドを行うことを一切制限するものではありません。そのようなビルドはこのページのライセンスよりもGitHubレポジトリ上のMITライセンスに従います。
- フルエディションはシェアウェア的なコンセプトも含めて有料配布するものであるため、二次配布は行わないで下さい。また、基本エディションに対して機能制限の解除を目的としたリバースエンジニアリングはご遠慮下さい。

</div>


#### 注意3. デフォルトサブキャラについて
{: .doc-sec2 }

BOOTH等でバイナリとして配布される本ソフトでは、追加のインストールなく使用できるサブキャラ(「リスナー」)が含まれています。

同サブキャラの画像、およびVRMの著作者は下記の通りです。

© 2025 denjiro99
赤崎でんじろー (https://x.com/denjiro99)

とくにBOOTH等でバイナリとして配布されている本ソフトに対し、リバースエンジニアリング等によって当該の画像あるいはVRMのみを抽出して再頒布することは禁止します。


#### 注意4. 返金要望のお断りについて

<div class="doc-ul" markdown="1">

- ライセンスで示す免責条項に基づき、いかなる理由でも返金は行っていません。
- 「フルエディションのみ起動に失敗する」などの重大な問題がある場合、ソフトウェア自体の問題ではなくzipファイルのダウンロード時エラーなど、別の原因で発生している可能性が非常に高いです。この場合は[FAQ](../questions)を確認のうえ、必要な場合はお問い合わせください。

</div>

### License Note
{: .doc-sec1 }

MIT License

Copyright (c) 獏星(ばくすたー)@baku_dreameater

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
