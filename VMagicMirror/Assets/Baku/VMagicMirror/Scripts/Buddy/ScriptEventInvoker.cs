using Baku.VMagicMirror.Buddy.Api;
using NLua;
using UniRx;

namespace Baku.VMagicMirror.Buddy
{
    /// <summary>
    /// スクリプトのコールバック関数をいい感じに呼ぶクラス。
    /// <see cref="ScriptCaller"/>と同じライフサイクルで動くのが期待値
    /// </summary>
    public class ScriptEventInvoker : PresenterBase
    {
        private readonly RootApi _api;
        private readonly AvatarMotionEventApiImplement _inputEventApiImplement;
        
        public ScriptEventInvoker(RootApi api, AvatarMotionEventApiImplement inputEventApiImplement)
        {
            _api = api;
            _inputEventApiImplement = inputEventApiImplement;
        }
        
        //NOTE: インスタンス生成のコンテキストがいわゆる「普通の」Zenjectに乗っからない想定。
        // Initialize/Disposeは明示的に呼ばれるのがこのクラスでは期待値になる
        public override void Initialize()
        {
            _inputEventApiImplement.KeyboardKeyDown
                .Subscribe(key => Invoke(_api.AvatarMotionEvent.OnKeyboardKeyDown, new object[] { key }))
                .AddTo(this);
        }

        private static void Invoke(LuaFunction func, object[] args)
            => ApiUtils.Try(() => func.Call(args));
    }
}
