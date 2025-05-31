// #load 先で禁止されてるnamespaceのusingをすると起動できないことの検証用サブキャラ
#load "..\_Reference\Globals.csx"
#load ".\SubScript.csx"
using System;
using VMagicMirror.Buddy;

Api.Start += () =>
{
  var sprite = Api.Create2DSprite();
  var parent = Api.Transforms.GetTransform2D("mainImage");
  sprite.Transform.SetParent(parent);

  sprite.Show("default.png");
  Foo();
};
