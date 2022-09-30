using Baku.VMagicMirror.MotionExporter;

namespace Baku.VMagicMirror
{
    public class CustomMotionItem
    {
        public CustomMotionItem(string motionName, bool[] usedFlags, DeserializedMotionClip motion)
        {
            MotionName = motionName;
            MotionLowerName = motionName.ToLower();
            UsedFlags = usedFlags;
            Motion = motion;
        }
            
        /// <summary> カスタムモーションを一意に指定するのに使う文字列 </summary>
        public string MotionLowerName { get; }
            
        /// <summary> WPF側に渡す文字列で、実態はファイル名から拡張子を抜いたもの </summary>
        public string MotionName { get; }
            
        /// <summary> マッスルごとに、そのマッスルがアニメーション対象かどうかを示したフラグ </summary>
        public bool[] UsedFlags { get; }
            
        /// <summary> 実際に再生するモーション </summary>
        public DeserializedMotionClip Motion { get; }
    }    
}
