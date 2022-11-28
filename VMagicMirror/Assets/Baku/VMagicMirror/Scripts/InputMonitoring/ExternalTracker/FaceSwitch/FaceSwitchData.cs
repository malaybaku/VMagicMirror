using System;

namespace Baku.VMagicMirror.ExternalTracker
{
    /// <summary>
    /// WPFから飛んでくる、FaceSwitchの設定一覧
    /// </summary>
    [Serializable]
    public class FaceSwitchSettings
    {
        public FaceSwitchItem[] items;

        public static FaceSwitchSettings LoadDefault()
        {
            return new FaceSwitchSettings()
            {
                items = new[]
                {
                    new FaceSwitchItem()
                    {
                        source = FaceSwitchKeys.MouthSmile,
                        threshold = 70,
                        clipName = "Happy",
                        keepLipSync = false,
                    },
                    new FaceSwitchItem()
                    {
                        source = FaceSwitchKeys.BrowDown,
                        threshold = 70,
                        clipName = "Sad",
                        keepLipSync = false,
                    }
                },
            };
        }
    }

    /// <summary>
    /// NOTE: sourceがthreshold以上ならclipNameを適用し、リップシンクをそのままにするかはkeepLipSyncで判断、という内容
    /// </summary>
    [Serializable]
    public class FaceSwitchItem
    {
        public string source;
        public int threshold;
        public string clipName;

        [NonSerialized] 
        private string _filteredClipName = null;
        public string ClipName
        {
            get
            {
                if (_filteredClipName == null)
                {
                    _filteredClipName = BlendShapeCompatUtil.GetVrm10ClipName(clipName);
                }
                return _filteredClipName;
            }
        }
        public bool keepLipSync;
        public string accessoryName;
    }
    
    public static class FaceSwitchKeys
    {
        public const string MouthSmile = "mouthSmile";
        public const string EyeSquint = "eyeSquint";
        public const string EyeWide = "eyeWide";
        public const string BrowUp = "browUp";
        public const string BrowDown = "browDown";
        public const string CheekPuff = "cheekPuff";
        public const string TongueOut = "tongueOut";
    }
}
