using Newtonsoft.Json;
using System;

namespace Baku.VMagicMirrorConfig
{
    // 目のブレンドシェイプ値を受け取るためのクラスで、アプリケーションと同じライフサイクルを持つ
    public class EyeBlendShapePreviewReceiver
    {
        public EyeBlendShapePreviewReceiver() : this(
            ModelResolver.Instance.Resolve<IMessageReceiver>()
            )
        {
        }

        internal EyeBlendShapePreviewReceiver(IMessageReceiver receiver)
        {
            receiver.ReceivedCommand += OnReceivedCommand;
        }

        public event EventHandler<EyeBlendShapeValuesEventArgs>? EyeBlendShapeValuesReceived;

        private void OnReceivedCommand(CommandReceivedData e)
        {
            if (e.Command != ReceiveMessageNames.EyeBlendShapeValues)
            {
                return;
            }

            try
            {
                var values = JsonConvert.DeserializeObject<RawEyeBlendShapeValues>(e.Args);
                if (values == null)
                {
                    return;
                }
                var args = new EyeBlendShapeValuesEventArgs(
                    new EyeBlendShapeValues(values.LeftBlink, values.RightBlink)
                );
                EyeBlendShapeValuesReceived?.Invoke(this, args);
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);
            }
        }

        // NOTE: こっちはIPCでのみ使うので入れ子になっている
        public class RawEyeBlendShapeValues
        {
            public float LeftBlink { get; set; }
            public float RightBlink { get; set; }
        }
    }

    public class EyeBlendShapeValuesEventArgs : EventArgs
    {
        public EyeBlendShapeValuesEventArgs(EyeBlendShapeValues values)
        {
            Values = values;
        }
        public EyeBlendShapeValues Values { get; }
    }

    public class EyeBlendShapeValues
    {
        public EyeBlendShapeValues(float leftBlink, float rightBlink)
        {
            LeftBlinkPercent = Math.Clamp((int)Math.Round(leftBlink * 100f), 0, 100);
            RightBlinkPercent = Math.Clamp((int)Math.Round(rightBlink * 100f), 0, 100);
        }
        public int LeftBlinkPercent { get; }
        public int RightBlinkPercent { get; }
    }
}
