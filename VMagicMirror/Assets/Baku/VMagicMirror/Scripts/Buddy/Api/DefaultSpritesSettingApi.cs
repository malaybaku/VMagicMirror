using VMagicMirror.Buddy;
using UnityEngine;

namespace Baku.VMagicMirror.Buddy.Api
{
    // NOTE: SpriteEffectと比べて値域管理は頑張らないかわり、Instance側でナンセンスな値(負のintervalとか)は無視する想定
    public class DefaultSpritesSettingApi : IDefaultSpritesSetting
    {
        private readonly BuddyDefaultSpritesSettingInstance _instance;
        public DefaultSpritesSettingApi(BuddyDefaultSpritesSettingInstance instance)
        {
            _instance = instance;
        }

        public float BlinkIntervalMin
        { 
            get => _instance.BlinkIntervalMin;
            set => _instance.BlinkIntervalMin = Mathf.Max(value, 0f);
        }
        
        public float BlinkIntervalMax
        { 
            get => _instance.BlinkIntervalMax;
            set => _instance.BlinkIntervalMax = Mathf.Max(value, 0f);
        }
        
        public bool SyncBlinkBlendShapeToMainAvatar
        {
            get => _instance.SyncBlinkBlendShapeToMainAvatar;
            set => _instance.SyncBlinkBlendShapeToMainAvatar = value;
        }

        public bool SyncMouthBlendShapeToMainAvatar
        {
            get => _instance.SyncMouthBlendShapeToMainAvatar;
            set => _instance.SyncMouthBlendShapeToMainAvatar = value;
        }
    }
}