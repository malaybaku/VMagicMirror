// Sprite3Dのテストをするためのサブキャラ

// NOTE: この #load ステートメントは実行時に無視される
#load "..\_Reference\Globals.csx"
using System;
using System.IO;
using VMagicMirror.Buddy;

// NOTE: 3Dスプライトは現時点で2Dより仕様がめっちゃ少ないのでテストも簡易的。特徴は下記くらい
// - 親子階層が作れること
// - スプライトのサイズの取り扱い (= 基本的に長辺1mのスプライトになること)

var sprite3d = Api.Create3DSprite();
var subSprite = Api.Create3DSprite();
var parent = Api.Transforms.GetTransform3D("mainImage");

Api.Start += () =>
{
  sprite3d.Transform.SetParent(parent);
  // NOTE: ないファイルを指定した場合、エラーログは出るが、この行で停止はしない(次に進む)
  sprite3d.Show("default.png");
  subSprite.Transform.SetParent(sprite3d.Transform);
};

Api.Update += (deltaTime) =>
{
  var flip = Api.Property.GetBool("flip");
  sprite3d.Transform.LocalScale = new Vector3((flip ? -1 : 1), 1, 1);

  subSprite.Transform.LocalPosition = Api.Property.GetVector3("subSpritePosition");
  subSprite.Transform.LocalRotation = Api.Property.GetQuaternion("subSpriteRotation");
  subSprite.Transform.LocalScale = Vector3.one * Api.Property.GetFloat("subSpriteScale");

  var showSubSprite = Api.Property.GetBool("showSubSprite");
  if (showSubSprite)
  {
    subSprite.Show("subSprite.png");
  }
  else
  {
    subSprite.Hide();
  }
};

Api.Property.ActionRequested += (action) =>
{
  switch (action)
  {
    case "setDefaultSprite":
      sprite3d.Show("default.png");
      break;
    case "doTransition":
      var imageFileName = Api.Property.GetString("transitionImage");
      sprite3d.Show(imageFileName);
      break;
  }
};
