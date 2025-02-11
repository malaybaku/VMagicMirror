using System;
using System.IO;
using System.Text;
using Baku.VMagicMirror.Buddy.Api;
using NLua;
using UniRx;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.Buddy
{
    // フォルダを指定されてスクリプトを実行するためのクラス。
    // このクラスは勝手に動き出さずに、ScriptLoaderからの指定に基づいて動作を開始/終了する。
    // このクラスはアプリ終了時以外にもスクリプトのリロード要求で破棄されることがあり、この方法で破棄される場合はロードしたリソースを解放する。
    public class ScriptCaller
    {
        internal RootApi Api { get; }
        private readonly Lua _lua;
        private readonly ScriptEventInvoker _scriptEventInvoker;
        private readonly CompositeDisposable _disposable = new();

        //「定義されてれば呼ぶ」系のコールバックメソッド。今のとこupdateしかないが他も増やしてよい + 増えたら管理方法を考えた方が良さそう
        private LuaFunction _updateFunction;

        private readonly BuddySpriteCanvas _spriteCanvas;
        // NOTE: 呼び出し元クラスは、Initialize()が呼ばれる時点でこのパスにlua拡張子のファイルが存在することを保証している
        public string EntryScriptPath { get; }
        public string EntryScriptDirectory { get; }
        public string BuddyId { get; }
        
        [Inject]
        public ScriptCaller(
            string entryScriptPath,
            BuddySpriteCanvas spriteCanvas,
            IFactory<RootApi, ScriptEventInvoker> scriptEventInvokerFactory
            )
        {
            EntryScriptPath = entryScriptPath;
            EntryScriptDirectory = Path.GetDirectoryName(entryScriptPath);
            BuddyId = Path.GetFileName(EntryScriptDirectory);
            _spriteCanvas = spriteCanvas;
            Api = new RootApi(EntryScriptDirectory);
            _lua = new Lua();
            
            // readonlyにできると嬉しいのでここでやっているが、問題があればInitialize()の中とかで初期化してもよい
            _scriptEventInvoker = scriptEventInvokerFactory.Create(Api);
        }
        
        public void Initialize()
        {
            _lua.State.Encoding = Encoding.UTF8;
            _lua["api"] = Api;
            _lua[nameof(Sprite2DTransitionStyle)] = Sprite2DTransitionStyleValues.Instance;

            // APIの生成時にSpriteのインスタンスまで入ってる状態にしておく (ヒエラルキーの構築時に最初からインスタンスがあるほうが都合がよいため)
            Api.SpriteCreated
                .Subscribe(sprite => sprite.Instance = _spriteCanvas.CreateSpriteInstance())
                .AddTo(_disposable);
            
            if (!File.Exists(EntryScriptPath))
            {
                LogOutput.Instance.Write($"Error, script does not exist at: {EntryScriptPath}");
                return;
            }

            try
            {
                _lua.DoString(File.ReadAllText(EntryScriptPath));
                _updateFunction = _lua["update"] as LuaFunction;
                
                if (_lua["start"] is LuaFunction function)
                {
                    function.Call();
                }
            }
            catch (Exception e)
            {
                LogOutput.Instance.Write("Failed to load script at:" + EntryScriptPath);
                LogOutput.Instance.Write(e);
                Debug.LogException(e);
            }

            _scriptEventInvoker.Initialize();
        }

        public void Dispose()
        {
            Api.Dispose();
            _lua?.Dispose();
            _scriptEventInvoker.Dispose();
            _disposable.Dispose();
        }

        public void Tick()
        {
            try
            {
                _updateFunction?.Call(Time.deltaTime);
            }
            catch (Exception e)
            {
                LogOutput.Instance.Write("Error in lua script update:");
                LogOutput.Instance.Write(e);
                Debug.LogException(e);
            }
            
            foreach (var sprite in Api.Sprites)
            {
                UpdateSprite(sprite);
            }
        }

        public void SetPropertyApi(PropertyApi api) => Api.Property = api;
        public void SetTransformsApi(TransformsApi api) => Api.Transforms = api;

        // TODO: Scriptのメイン処理ではない(3DイラストとかVRMまでここに書いてたら手に負えない)のでコードの置き場所は考える
        private void UpdateSprite(Sprite2DApi sprite)
        {
            var pos = sprite.Position;
            if (sprite.Effects.Floating.IsActive)
            {
                pos = GetAndUpdateFloatingPosition(pos, sprite.Effects.Floating);
            }
            // TODO: 不要になる or 必要だけどanchorじゃない方法で適用する…となりそうで、ややこしいので一旦ストップ
            //sprite.Instance.SetPosition(pos);

            var size = sprite.Size;
            if (sprite.Effects.BounceDeform.IsActive)
            {
                size = GetAndUpdateBounceDeformedSize(size, sprite.Effects.BounceDeform);
            }
            sprite.Instance.SetSize(size);
            var isTransitionDone =
                sprite.Instance.DoTransition(Time.deltaTime, sprite.CurrentTexture, sprite.CurrentTransitionStyle);
            // sprite.Instance.SetTexture(sprite.CurrentTexture);
            if (isTransitionDone)
            {
                sprite.CurrentTransitionStyle = Sprite2DTransitionStyle.None;
            }
        }

        private Vector2 GetAndUpdateBounceDeformedSize(Vector2 size, BounceDeformSpriteEffect effect)
        {
            var t = effect.ElapsedTime + Time.deltaTime;
            if (t > effect.Duration)
            {
                if (effect.Loop)
                {
                    effect.ElapsedTime = t - effect.Duration;
                }
                else
                {
                    effect.IsActive = false;
                    return size;
                }
            }

            var rate = t / effect.Duration;
            // bounceRate > 0 のとき、横に平べったくなる。マイナスの場合は縦に伸びる
            var bounceRate = Mathf.Sin(rate * Mathf.PI * 2f);
            
            // - Intensityは「伸びる側の伸び率」を規定する
            // - 縮むほうはSizeの積が一定になるように決定される(=伸びたぶんの逆数で効かす)
            // TODO: bounceRateの正負切り替わりの瞬間がキモいかもしれないので様子を見ましょう
            if (bounceRate > 0)
            {
                var x = 1 + bounceRate * effect.Intensity;
                var y = 1 / x;
                return new Vector2(size.x * x, size.y * y);
            }
            else
            {
                var y = 1 + (-bounceRate) * effect.Intensity;
                var x = 1 / y;
                return new Vector2(size.x * x, size.y * y);
            }
        }

        private Vector2 GetAndUpdateFloatingPosition(Vector2 pos, FloatingSpriteEffect effect)
        {
            var t = effect.ElapsedTime + Time.deltaTime;
            if (t > effect.Duration)
            {
                effect.ElapsedTime = t - effect.Duration;
            }

            var rate = t / effect.Duration;
            var yRate = 0.5f * (1 - Mathf.Cos(rate * Mathf.PI * 2f));
            return pos + new Vector2(0, yRate * effect.Intensity);
        }
    }
}
