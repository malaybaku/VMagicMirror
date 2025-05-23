// Sprite2Dのテストをするためのサブキャラ

// NOTE: この #load ステートメントは実行時に無視される
#load "..\_Reference\Globals.csx"
using System;
using VMagicMirror.Buddy;

// NOTE: 2Dスプライトのテストでは以下のテストができる(べきである)
// - DefaultSpritesの表示 (4枚絵) + メインアバターとの同期処理
// - 1枚ずつの画像表示
// - 左右反転などの、(Vector2.oneの定数倍ではないような)典型的なスケーリング
// - APIとして定義されてるトランジションやエフェクトの実行
// - 画像の子要素として画像をくっつけるやつ


var sprite2d = Api.Create2DSprite();
var parent = Api.Transforms.GetTransform2D("mainImage");

var subSprite = Api.Create2DSprite();

var totalTime = 0f;

Api.Start += () =>
{
  sprite2d.Transform.SetParent(parent);
  sprite2d.Size = Vector2.one * 128;
  sprite2d.SetupDefaultSprites("default.png", "blink.png", "mouthOpen.png", "blink_MouthOpen.png");
  sprite2d.ShowDefaultSprites();

  subSprite.Size = Vector2.one * 64;
  subSprite.Transform.SetParent(sprite2d.Transform);
};

Api.Update += (deltaTime) =>
{
  var flip = Api.Property.GetBool("flip");
  sprite2d.Transform.LocalScale = new Vector2(flip ? -1 : 1, 1);

  var showSubSprite = Api.Property.GetBool("showSubSprite");
  if (showSubSprite)
  {
    subSprite.Show("subSprite.png");
    subSprite.Transform.LocalPosition = Api.Property.GetVector2("subSpritePosition");
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
    case "setDefaultSprites":
      SetDefaultImage();
      break;
    case "doTransition":
      DoTransition();
      break;
    case "applyFloatingEffect":
      ApplyFloatingEffect();
      break;
    case "applyPuniEffect":
      ApplyPuniEffect();
      break;
    case "applyVibrateEffect":
      ApplyVibrateEffect();
      break;
    case "doJump":
      DoJump();
      break;
    default:
      break;
  }
};

void SetDefaultImage()
{
  // NOTE: 値が0の場合は Sprite2DTransitionStyle.None 扱いされて遷移しない
  var transitionStyle = (Sprite2DTransitionStyle)Api.Property.GetInt("transitionStyle");
  var transitionDuration = Api.Property.GetFloat("transitionDuration");
  sprite2d.ShowDefaultSprites(transitionStyle, transitionDuration);
} 
void DoTransition()
{
  var imageFileName = Api.Property.GetString("transitionImage");
  // NOTE: 値が0の場合は Sprite2DTransitionStyle.None 扱いされて遷移しない
  var transitionStyle = (Sprite2DTransitionStyle)Api.Property.GetInt("transitionStyle");
  var transitionDuration = Api.Property.GetFloat("transitionDuration");

  sprite2d.Show(imageFileName, transitionStyle, transitionDuration);
}


// NOTE: Floating/Bounceについてはon/offそのものも指定する (Apply != 有効化)
void ApplyFloatingEffect()
{
  sprite2d.Effects.Floating.IsActive = Api.Property.GetBool("useFloatingEffect");
  sprite2d.Effects.Floating.Duration = Api.Property.GetFloat("floatingEffectDuration");
  sprite2d.Effects.Floating.Intensity = Api.Property.GetFloat("floatingEffectIntensity");
}

void ApplyPuniEffect()
{
  sprite2d.Effects.Puni.IsActive = Api.Property.GetBool("usePuniEffect");
  sprite2d.Effects.Puni.Duration = Api.Property.GetFloat("puniEffectDuration");
  sprite2d.Effects.Puni.Intensity = Api.Property.GetFloat("puniEffectIntensity");
}

void ApplyVibrateEffect()
{
  sprite2d.Effects.Vibrate.IsActive = Api.Property.GetBool("useVibrate");

  var propertiesX = Api.Property.GetVector3("vibratePropertiesX");
  sprite2d.Effects.Vibrate.IntensityX = propertiesX.x;
  sprite2d.Effects.Vibrate.FrequencyX = propertiesX.y;
  sprite2d.Effects.Vibrate.PhaseOffsetX = propertiesX.z;

  var propertiesY = Api.Property.GetVector3("vibratePropertiesY");
  sprite2d.Effects.Vibrate.IntensityY = propertiesY.x;
  sprite2d.Effects.Vibrate.FrequencyY = propertiesY.y;
  sprite2d.Effects.Vibrate.PhaseOffsetY = propertiesY.z;
}

void DoJump()
{
  var duration = Api.Property.GetFloat("jumpDuration");
  var height = Api.Property.GetFloat("jumpHeight");
  var jumpCount = Api.Property.GetInt("jumpCount");
  sprite2d.Effects.Jump.Jump(duration, height, jumpCount);
}
