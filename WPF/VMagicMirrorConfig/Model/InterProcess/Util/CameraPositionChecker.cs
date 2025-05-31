﻿using System;
using System.Threading;
using System.Threading.Tasks;

namespace Baku.VMagicMirrorConfig
{
    /// <summary> Unity側のカメラの位置をポーリングベースで確認するやつ </summary>
    /// <remarks> 1インスタンスはStartとStopを1回ずつ呼び出したら使い終わり、という設計です </remarks>
    class CameraPositionChecker
    {
        public CameraPositionChecker(IMessageSender sender, LayoutSettingModel layoutSetting)
        {
            _sender = sender;
            _layoutSetting = layoutSetting;
            _cts = new CancellationTokenSource();
        }

        private readonly IMessageSender _sender;
        private readonly LayoutSettingModel _layoutSetting;
        private readonly CancellationTokenSource _cts;

        public void Start(int intervalMillisec) => Task.Run(async () =>
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(intervalMillisec, _cts.Token);
                    if (_cts.Token.IsCancellationRequested)
                    {
                        return;
                    }
                    string data = await _sender.QueryMessageAsync(MessageFactory.CurrentCameraPosition());
                    _layoutSetting.CameraPosition.SilentSet(data);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    LogOutput.Instance.Write(ex);
                }
            }
        });

        public void Stop() => _cts.Cancel();
    }
}
