// #r ディレクティブを使うと読み込めなくなることの検証用サブキャラ
#load "..\_Reference\Globals.csx"

#r "System.Drawing.dll"
using System;
using System.Drawing;
using VMagicMirror.Buddy;

Api.Start += () =>
{
  var sprite = Api.Create2DSprite();
  var parent = Api.Transforms.GetTransform2D("mainImage");
  sprite.Transform.SetParent(parent);

  sprite.Show("default.png");

  var c = Color.FromArgb(255, 0, 0, 0);
};
