using System;
using R3;
using UnityEngine;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// フレームレートの実測値を定期的に通知するクラス。
    /// FPSの変更に弱いコードで使い、そうでないコードでは (補間処理とかをやっていても) あまり積極的には使わないことを想定している
    /// </summary>
    public class CurrentFramerateChecker : PresenterBase
    {
        // NOTE: アプリケーションとしては典型的には 30 or 60 らへんを想定してはいる
        private readonly ReactiveProperty<float> _currentFramerate = new(60f);

        /// <summary>
        /// フレームレートを取得する。ただし、実測値が30未満の場合は30を返す
        /// NOTE: 30未満を30扱いするのは、この値が基本的にBiQuadFilterで使うサンプリングレートになる想定のため
        /// </summary>
        public ReadOnlyReactiveProperty<float> CurrentFramerate => _currentFramerate;
        
        public override void Initialize()
        {
            Observable.Interval(TimeSpan.FromSeconds(1))
                .Subscribe(_ =>
                {
                    _currentFramerate.Value = Mathf.Max(1f / Time.smoothDeltaTime, 30);
                })
                .AddTo(this);
        }
    }
}
