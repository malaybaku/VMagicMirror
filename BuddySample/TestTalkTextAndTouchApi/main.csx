// セリフAPI + タッチAPIをテストするやつ
#load "..\_Reference\Globals.csx"
using System;
using VMagicMirror.Buddy;

// できること:
// - WPF上のボタンに基づいて、セリフAPIの動作が見られる
// - メインのSprite2Dでポインターに関する全イベントが取れる
//   - とくにクリックではセリフが出てトランジションも発生する 
//
// デバッグにおける観点:
// - トランジションとかJumpよる変形/移動とPointerEnter/Leave/Clickらへんの相性
// - 背景透過しているときに、レイキャストの判定やら何やら踏まえて適切にPointerEnter/Leaveするかどうか
// - Pointer(Down/Up/Click)のそれぞれが期待する動きかどうか
const string normalImageName = "default.png";

var sprite2d = Api.Create2DSprite();
var parent = Api.Transforms.GetTransform2D("mainImage");

Api.Start += () =>
{
  sprite2d.Transform.SetParent(parent);
  sprite2d.Size = Vector2.one * 128;
  sprite2d.Show(normalImageName);
};

Api.Property.ActionRequested += (action) =>
{
  switch (action)
  {
    case "startDialog":
      StartDialog();
      break;
  }
};

sprite2d.TalkText.ItemDequeued += (info) => Api.Log($"ItemDequeued, key={info.Key}, text={info.Text}");
sprite2d.TalkText.ItemFinished += (info) => Api.Log($"ItemFinished, key={info.Key}, text={info.Text}");

sprite2d.PointerEnter += (data) => Api.Log("PointerEnter");
sprite2d.PointerLeave += (data) => Api.Log("PointerLeave");
sprite2d.PointerDown += (data) => Api.Log("PointerDown");
sprite2d.PointerUp += (data) => Api.Log("PointerUp");
sprite2d.PointerClick += (data) =>
{
  Api.Log("PointerClick");
  DoClickReaction();
};

void StartDialog()
{
  sprite2d.TalkText.ShowText("1つ目のテキストです。全てをデフォルト設定で呼び出します。");
  sprite2d.TalkText.ShowText("2つ目のテキストです。全文をただちに表示します。<size=64><color=\"red\">Rich Textも検証しておきます</color></size>。赤字、かつ大きな表示になってれば期待値です", -1f);
  sprite2d.TalkText.ShowText("3つ目のテキストです。keyを明示的に指定します。", key: "3rdTalkTextKey");

  // NOTE: scroll挙動が見やすいように非常に長くしておく
  var text3 = "4つ目のテキストです。speedが非常に早い長文になっています。スクロールが期待動作をしているか確認するために使えます。" +
    "スクロールは、少なくともユーザーが操作しない場合は自動で下に移動していくのが期待値です。" +
    "ユーザーが操作をした場合、もしプログラム的に自動で下スクロールしているのであればその挙動は次のセリフまでは停止するのが望ましいと考えられます。";
    sprite2d.TalkText.ShowText(text3 + text3 + text3, 
    speed: 20f, 
    waitAfterCompleted: 0f
  );
  // 明示的にwaitするテスト
  sprite2d.TalkText.Wait(10f, "waitAfter4thText");
  sprite2d.TalkText.ShowText("5つ目(ラスト)のテキストです。speedが非常に遅いため、クリックして全文表示したくなる可能性がかなり高いです", 1f);
}

void DoClickReaction()
{
    var actionType = Api.Property.GetInt("clickActionType");
    switch (actionType) 
    {
      case 0:
        // None: 何もしない
        break;
      case 1:
        // 画像トランジション: ポインターイベントの観察がしたいので低速にする
        sprite2d.Show(normalImageName, Sprite2DTransitionStyle.LeftFlip, 1.0f);
        break;
      case 2:
        // ジャンプ: これもイベント観察のためにゆっくり
        sprite2d.Effects.Jump.Jump(1.5f, 64f, 1);
        break;
      default:
        break;
    }
}
