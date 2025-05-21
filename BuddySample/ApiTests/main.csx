// NOTE: この #load ステートメントは実行時に無視される
#load "..\_Reference\Globals.csx"
using System;
using System.IO;
using System.Threading.Tasks;
using VMagicMirror.Buddy;

// NOTE:
// - Sprite2D / Sprite3D / Vrm / VrmAnimation / Glb 等はそれ単体でチェックしたいことが多いので、テスト用サブキャラを分けている
// - このサブキャラテストを実行するには下記のアセットがmain.csxと同じフォルダに存在することが必要
//   - 画像: default.png, Sprite2Dのテストではないため、「表示できてますね」のチェック程度にしか使わない
//   - 音声: long.mp3, 音声の中断と「最後まで再生」の双方がチェックできたほうがいいので、5秒程度の長さのものが検証に適する

var sprite2d = Api.Create2DSprite();

// NOTE: Instanceが生成できること以外はここではチェックしない。また、VRMとかVrmAnimationはそもそも生成も試さないが、これは意図的
var sprite3d = Api.Create3DSprite();


// UpdateのdeltaTimeの妥当性チェックのためにUpdate上で使う
var totalTime = 0.0f;

Api.Start += () =>
{
  Api.Log("Api.Start: started");

  Api.InvokeDelay(() => {
    Api.Log("Api.InvokeDelay 1.5 sec");
  }, 1.5f);

  Api.InvokeInterval(() => {
    Api.Log("Api.InvokeInterval: 5.0 sec, but first delay is 2.5 sec");
  }, 10.0f, 2.5f);

  Api.Log($"OutputFeature = {Api.AvatarOutputFeatureEnabled}");
  Api.Log($"Buddy Dir={Api.BuddyDirectory}");
  Api.Log($"Cache Dir={Api.CacheDirectory}, Dir Exists={Directory.Exists(Api.CacheDirectory)}");

  var parent = Api.Transforms.GetTransform2D("mainImage");
  sprite2d.Transform.SetParent(parent);
  sprite2d.Size = new Vector2(256, 256);
  sprite2d.Show("default.png");

  Api.Log("Api.Start: ended");
};

Api.Update += (deltaTime) => 
{
  CheckTotalTime(deltaTime);
};

Api.Audio.AudioStarted += info => Api.Log($"Api.Audio.AudioStarted, length={info.Length}");
// NOTE: このテストではReasonの全パターンのテストまではしない (Stopもめっちゃ厳格には見ない)
Api.Audio.AudioStopped += info => Api.Log($"Api.Audio.AudioStopped, reason={info.Reason}");

Api.AvatarLoadEvent.Loaded += () => Api.Log("Api.Avatar.Loaded");
Api.AvatarLoadEvent.Unloaded += () => Api.Log("Api.Avatar.Unloaded");

Api.AvatarFacial.OnBlinked += () => Api.Log("Api.AvatarFacial.OnBlinked");

Api.AvatarMotionEvent.OnArcadeStickButtonDown += (button) => Api.Log($"Api.AvatarMotionEvent.OnArcadeStickButtonDown, button={button}");
Api.AvatarMotionEvent.OnGamepadButtonDown += (button) => Api.Log($"Api.AvatarMotionEvent.OnGamepadButtonDown, button={button}");
Api.AvatarMotionEvent.OnKeyboardKeyDown += (key) => Api.Log($"Api.AvatarMotionEvent.OnKeyboardKeyDown, key={key}");
Api.AvatarMotionEvent.OnPenTabletMouseButtonDown += () => Api.Log($"Api.AvatarMotionEvent.OnPenTabletMouseButtonDown");
Api.AvatarMotionEvent.OnTouchPadMouseButtonDown += () => Api.Log($"Api.AvatarMotionEvent.OnTouchPadMouseButtonDown");

Api.Input.GamepadButtonDown += (button) => Api.Log($"Api.Input.GamepadButtonDown, button={button}");
Api.Input.GamepadButtonUp += (button) => Api.Log($"Api.Input.GamepadButtonUp, button={button}");

// NOTE: 判定できるのは「Enterかそれ以外か」まで。
Api.Input.KeyboardKeyDown += (key) => Api.Log($"Api.Input.KeyboardKeyDown, key={key}");
Api.Input.KeyboardKeyUp += (key) => Api.Log($"Api.Input.KeyboardKeyUp, key={key}");

Api.Property.ActionRequested += (actionName) =>
{
  Api.Log($"Api.Property.ActionRequested: {actionName}");
  switch (actionName)
  {
    case "checkFacial":
      CheckFacial();
      break;
    case "getAllPropertyValues":
      GetAllPropertyValues();
      break;
    case "testLogWarn":
      TestLogWarn();
      break;
    case "testLogError":
      TestLogError();
      break;
    case "testThrowException":
      TestThrowException();
      break;
    case "playLongSound":
      PlayLongSound();
      break;
    case "stopLongSound":
      StopLongSound();
      break;
    case "getPose":
      GetPose();
      break;
    case "getDeviceLayouts":
      GetDeviceLayouts();
      break;
    case "getScreen":
      GetScreen();
      break;
    case "testRunTask":
      TestRunTask();
      break;
    case "testInput":
      TestInput();
      break;
    default:
      break;
  }
  Api.Log($"Api.Property.ActionRequested: {actionName} ended");
};

void CheckTotalTime(float deltaTime)
{
  totalTime += deltaTime;
  if (totalTime > 5.0f)
  {
    Api.Log($"Api.Update: {deltaTime:0.00} sec, total={totalTime:0.00} sec, lang={Api.Language}, rand={Api.Random():0.000}");
    totalTime = 0.0f;
  }
}

void CheckFacial()
{
  Api.Log(
    $"facial, current='{Api.AvatarFacial.CurrentFacial}', face switch='{Api.AvatarFacial.GetActiveFaceSwitch()}'"
    );

  var smileLeft = Api.AvatarFacial.GetCurrentValue(PerfectSyncBlendShapeNames.MouthSmileLeft, true);
  var smileRight = Api.AvatarFacial.GetCurrentValue(PerfectSyncBlendShapeNames.MouthSmileRight, true);
  var smile = (smileLeft + smileRight) / 2.0f;
  Api.Log($"facial, isTalking={Api.AvatarFacial.IsTalking}, mouthSmile={smile:0.00}");
} 

void GetAllPropertyValues()
{
  Api.Log($"bool, flip={Api.Property.GetBool("flip")}");;
  Api.Log($"int, someCount (int)  ={Api.Property.GetInt("someCount")}");;
  Api.Log($"int, myOptions (enum) ={Api.Property.GetInt("myOptions")}");;
  Api.Log($"int, someCount (range)={Api.Property.GetInt("someCountWithLimit")}");;

  Api.Log($"float, someFloat={Api.Property.GetFloat("duration")}");;
  Api.Log($"float, someFloat (range)={Api.Property.GetFloat("rangedDuration")}");;

  Api.Log($"string, userName={Api.Property.GetString("userName")}");;
  Api.Log($"string, someFilePath(path)={Api.Property.GetString("someFilePath")}");;

  var v2 = Api.Property.GetVector2("v2sample");
  Api.Log($"Vector2, v2sample={v2.x:0.00}, {v2.y:0.00}");

  var v3 = Api.Property.GetVector3("v3sample");
  Api.Log($"Vector3, v3sample={v3.x:0.00}, {v3.y:0.00}, {v3.z:0.00}");

  var rot = Api.Property.GetQuaternion("myRot").eulerAngles;
  Api.Log($"Quaternion, myRot={rot.x:0.00}, {rot.y:0.00}, {rot.z:0.00}");

  // NOTE: この辺はついで。
  var t2d = Api.Transforms.GetTransform2D("mainImage");
  Api.Log($"Transform2D:mainImage, pos=({t2d.Position.x:0.00}, {t2d.Position.y:0.00}), localPos=({t2d.LocalPosition.x:0.00}, {t2d.LocalPosition.y:0.00})");
  var t2dRot = t2d.Rotation.eulerAngles;
  Api.Log($"Transform2D:mainImage, rot=({t2dRot.x:0.0}, {t2dRot.y:0.0}, {t2dRot.z:0.0}), scale=({t2d.LocalScale:0.00})");

  var t3d = Api.Transforms.GetTransform3D("anchor3d");
  Api.Log($"Transform3D:anchor3d, pos=({t3d.Position.x:0.00}, {t3d.Position.y:0.00}, {t3d.Position.z:0.00}), localPos=({t3d.LocalPosition.x:0.00}, {t3d.LocalPosition.y:0.00}, {t3d.LocalPosition.z:0.00})");
  var t3dRot = t3d.Rotation.eulerAngles;
  Api.Log($"Transform3D:anchor3d, rot=({t3dRot.x:0.0}, {t3dRot.y:0.0}, {t3dRot.z:0.0}), scale=({t3d.LocalScale.x:0.00}, {t3d.LocalScale.y:0.00}, {t3d.LocalScale.z:0.00})");
}

void TestLogWarn() => Api.LogWarning("警告ログのテストです。開発者モードの場合、行数までは表示されるのが期待値です");
void TestLogError() => Api.LogError("エラーログのテストです。開発者モードの場合、行数までは表示されるのが期待値です。また、開発者モードじゃなくてもエラーが表示されるのが期待値です。");
void TestThrowException() => throw new Exception("例外をスローするテストです。開発者モードの場合、行数までは表示されるのが期待値です。また、開発者モードじゃなくてもエラーが表示されるのが期待値です。");

void PlayLongSound() => Api.Audio.Play("long.mp3", key: "longSound");
void StopLongSound() => Api.Audio.Stop("longSound");

void GetPose()
{
  var rootPos = Api.AvatarPose.GetRootPosition();
  var rootRot = Api.AvatarPose.GetRootRotation().eulerAngles;
  Api.Log($"AvatarPose, RootPos={rootPos.x:0.00}, {rootPos.y:0.00}, {rootPos.z:0.00}");
  Api.Log($"AvatarPose, RootRot={rootRot.x:0.00}, {rootRot.y:0.00}, {rootRot.z:0.00}");

  var hasLeftIndexBone = Api.AvatarPose.HasBone(HumanBodyBones.LeftIndexProximal);
  Api.Log($"AvatarPose, HasLeftIndexBone={hasLeftIndexBone}");

  var rightHandPos = Api.AvatarPose.GetBoneGlobalPosition(HumanBodyBones.RightHand);
  var rightHandLocalPos = Api.AvatarPose.GetBoneLocalPosition(HumanBodyBones.RightHand);
  Api.Log($"AvatarPose, RightHandPos={rightHandPos.x:0.00}, {rightHandPos.y:0.00}, {rightHandPos.z:0.00}");
  Api.Log($"AvatarPose, RightHandLocalPos={rightHandLocalPos.x:0.00}, {rightHandLocalPos.y:0.00}, {rightHandLocalPos.z:0.00}");

  var rightHandRot = Api.AvatarPose.GetBoneGlobalRotation(HumanBodyBones.RightHand).eulerAngles;
  var rightHandLocalRot = Api.AvatarPose.GetBoneLocalRotation(HumanBodyBones.RightHand).eulerAngles;
  Api.Log($"AvatarPose, RightHandRot={rightHandRot.x:0.00}, {rightHandRot.y:0.00}, {rightHandRot.z:0.00}");
  Api.Log($"AvatarPose, RightHandLocalRot={rightHandLocalRot.x:0.00}, {rightHandLocalRot.y:0.00}, {rightHandLocalRot.z:0.00}");
}

void GetDeviceLayouts()
{
  var cameraPos = Api.DeviceLayout.GetCameraPose().position;
  var fov = Api.DeviceLayout.GetCameraFov();
  Api.Log($"Layouts, camera fov={fov}, CameraPos={cameraPos.x:0.00}, {cameraPos.y:0.00}, {cameraPos.z:0.00}");

  var gamepadVisible = Api.DeviceLayout.GetGamepadVisible();
  var gamepadPos = Api.DeviceLayout.GetGamepadPose().position;
  Api.Log($"Layouts, gamepadVisible={gamepadVisible}, GamepadPos={gamepadPos.x:0.00}, {gamepadPos.y:0.00}, {gamepadPos.z:0.00}");

  // var arcadeStickVisible = Api.DeviceLayout.GetArcadeStickVisible();
  // var arcadeStickPos = Api.DeviceLayout.GetArcadeStickPose().position;

  var penTabletVisible = Api.DeviceLayout.GetPenTabletVisible();
  var penTabletPos = Api.DeviceLayout.GetPenTabletPose().position;
  Api.Log($"Layouts, penTabletVisible={penTabletVisible}, PenTabletPos={penTabletPos.x:0.00}, {penTabletPos.y:0.00}, {penTabletPos.z:0.00}");

  var touchPadVisible = Api.DeviceLayout.GetTouchpadVisible();
  var touchPadPos = Api.DeviceLayout.GetTouchpadPose().position;
  Api.Log($"Layouts, touchPadVisible={touchPadVisible}, TouchPadPos={touchPadPos.x:0.00}, {touchPadPos.y:0.00}, {touchPadPos.z:0.00}");

  var keyboardVisible = Api.DeviceLayout.GetKeyboardVisible();
  var keyboardPos = Api.DeviceLayout.GetKeyboardPose().position;
  Api.Log($"Layouts, keyboardVisible={keyboardVisible}, KeyboardPos={keyboardPos.x:0.00}, {keyboardPos.y:0.00}, {keyboardPos.z:0.00}");
}

void GetScreen()
{
  var w = Api.Screen.Width;
  var h = Api.Screen.Height;
  var t = Api.Screen.IsTransparent;
  Api.Log($"Screen, w={w}, h={h}, transparent={t}");
}

void TestRunTask()
{
  Api.RunOnMainThread(async () => {

    await Task.Delay(1000);
    // NOTE: この辺でメインスレッドが使えますよね、ということのチェックのためにボーンを拾いに行ってる
    var posX = Api.AvatarPose.GetBoneGlobalPosition(HumanBodyBones.RightHand).x;
    Api.Log($"Log from task, RightHand pos x = {posX:0.00}");
  });
}

void TestInput()
{
  var leftStick = Api.Input.GamepadLeftStick;
  var rightStick = Api.Input.GamepadRightStick;
  Api.Log($"TestInput, Gamepad LStick={leftStick.x:0.00}, {leftStick.y:0.00}");
  Api.Log($"TestInput, Gamepad RStick x={rightStick.x:0.00}, {rightStick.y:0.00}");

  for (var i = 0; i < (int)GamepadButton.Unknown; i++)
  {
    var button = (GamepadButton)i;
    var isDown = Api.Input.GetGamepadButton(button);
    Api.Log($"TestInput, GamepadButton[{button}] isDown={isDown}");
  }

  var mouseButton = Api.Input.MousePosition;
  Api.Log($"TestInput, MousePos(Normalized) x={mouseButton.x:0.00}, y={mouseButton.y:0.00}");
}
