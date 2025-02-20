using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using NLua;
using UniRx;
using UnityEngine;
using UnityEngine.Scripting;

namespace Baku.VMagicMirror.Buddy.Api
{
    /// <summary>
    /// VMMのScriptでトップレベルから呼ぶ関数をここに入れる
    /// </summary>
    [Preserve]
    public class RootApi
    {
        private readonly CancellationTokenSource _cts = new();

        //TODO: Layoutと同じくSpriteにもInstanceのレポジトリとUpdaterを作りたい
        private readonly List<Sprite2DApi> _sprites = new();
        public IReadOnlyList<Sprite2DApi> Sprites => _sprites;

        private readonly Subject<Sprite2DApi> _spriteCreated = new();
        public IObservable<Sprite2DApi> SpriteCreated => _spriteCreated;
        
        private readonly string _baseDir;

        public RootApi(string baseDir, string buddyId, ApiImplementBundle apiImplementBundle)
        {
            _baseDir = baseDir;
            Property = apiImplementBundle.BuddyPropertyRepository.Get(buddyId);
            AvatarPose = new AvatarPoseApi(apiImplementBundle.AvatarPoseApi);
            AvatarFacial = new AvatarFacialApi(apiImplementBundle.AvatarFacialApi);
            Audio = new AudioApi(baseDir, apiImplementBundle.AudioApi);
            DeviceLayout = new DeviceLayoutApi(apiImplementBundle.DeviceLayoutApi);
            Screen = new ScreenApi(apiImplementBundle.ScreenApi);
        }

        internal void Dispose()
        {
            foreach (var sprite in _sprites)
            {
                sprite.Dispose();
            }
            _sprites.Clear();

            AvatarFacial.Dispose();

            _cts.Cancel();
            _cts.Dispose();
        }

        [Preserve] public Action StartCS { get; set; }
        [Preserve] public Action<float> UpdateCS { get; set; }

        //TODO: FeatureLockについては、ここで記述されるプロパティ単位で
        //「丸ごとOK or 丸ごと塞がってる」となるのが分かりやすさ的には望ましい

        //NOTE: プロパティ形式で取得できるAPIは、スクリプトが最初に呼ばれる前に非nullで初期化されるのが期待値
        [Preserve] public PropertyApi Property { get; } = null;
        [Preserve] public TransformsApi Transforms { get; internal set; } = null;
        [Preserve] public DeviceLayoutApi DeviceLayout { get; }
        
        // NOTE: このへん `api.Avatar.MotionEvent` みたく書けたほうが字面がいいから修正しそう
        [Preserve] public AvatarLoadEventApi AvatarLoadEvent { get; } = new();
        [Preserve] public AvatarPoseApi AvatarPose { get; }
        [Preserve] public AvatarMotionEventApi AvatarMotionEvent { get; } = new();
        [Preserve] public AvatarFacialApi AvatarFacial { get; }
        [Preserve] public AudioApi Audio { get; }
        [Preserve] public ScreenApi Screen { get; }
        
        
        [Preserve]
        public void Log(string value)
        {
            if (Application.isEditor)
            {
                Debug.Log(value);
            }
            else
            {
                LogOutput.Instance.Write(value);
            }
        }

        [Preserve]
        public float Random() => UnityEngine.Random.value;

        [Preserve]
        public void InvokeDelay(LuaFunction func, float delaySeconds)
        {
            UniTask.Void(async () =>
            {
                await UniTask.Delay(TimeSpan.FromSeconds(delaySeconds),
                    cancellationToken: _cts.Token,
                    delayTiming: PlayerLoopTiming.LastPostLateUpdate
                    );
                ApiUtils.Try(() => func.Call());
            });
        }

        [Preserve]
        public void InvokeInterval(LuaFunction func, float intervalSeconds)
            => InvokeInterval(func, intervalSeconds, 0f);

        [Preserve]
        public void InvokeInterval(LuaFunction func, float intervalSeconds, float firstDelay)
        {
            UniTask.Void(async () =>
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(firstDelay),
                    cancellationToken: _cts.Token,
                    delayTiming: PlayerLoopTiming.LastPostLateUpdate
                    );
                while (!_cts.IsCancellationRequested)
                {
                    ApiUtils.Try(() => func.Call());
                    await UniTask.Delay(
                        TimeSpan.FromSeconds(intervalSeconds),
                        cancellationToken: _cts.Token,
                        delayTiming: PlayerLoopTiming.LastPostLateUpdate
                        );
                }
            });
        }

        // 名前もうちょい短くしたい…？
        [Preserve]
        public bool ValidateFilePath(string path)
        {
            var fullPath = Path.Combine(_baseDir, path);
            return
                ApiUtils.IsChildDirectory(SpecialFiles.BuddyRootDirectory, fullPath) &&
                File.Exists(path);
        }
        
        [Preserve]
        public Sprite2DApi Create2DSprite()
        {
            var result = new Sprite2DApi(_baseDir);
            _sprites.Add(result);
            _spriteCreated.OnNext(result);
            return result;
        }
        
        [Preserve]
        public Vector2 Vector2(float x, float y) => new(x, y);
    }
}
