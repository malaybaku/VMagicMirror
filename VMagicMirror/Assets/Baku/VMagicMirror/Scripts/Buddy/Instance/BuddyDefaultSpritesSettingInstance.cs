using UnityEngine;

namespace Baku.VMagicMirror
{
    // NOTE: Settings自体は2D/3DのSpriteで流用できるように分けられている。
    // 表示する実体はTexture2DだったりSpriteだったりするので、そこは別のクラスが管理する
    public class BuddyDefaultSpritesSettingInstance
    {
        public float BlinkIntervalMin { get; set; } = 10;
        public float BlinkIntervalMax { get; set; } = 20;
        public bool SyncBlinkBlendShapeToMainAvatar { get; set; } = true;
        public bool SyncMouthBlendShapeToMainAvatar { get; set; } = true;
        
        public Vector2 LocalPositionOffsetOnBlink { get; set; } = new Vector2(0, -3);
    }
}
