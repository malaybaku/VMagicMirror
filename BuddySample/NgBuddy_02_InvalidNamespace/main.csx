// 禁止されてるnamespaceのusingをすると起動できないことの検証用サブキャラ
#load "..\_Reference\Globals.csx"
using System;
using System.IO;
using VMagicMirror.Buddy;

Api.Start += () =>
{
  var sprite = Api.Create2DSprite();
  var parent = Api.Transforms.GetTransform2D("mainImage");
  sprite.Transform.SetParent(parent);

  sprite.Show("default.png");
  
  // 仮に実行できたらtrueにはなる
  var e = File.Exists(Path.Combine(Api.BuddyDirectory, "default.png"));
};
