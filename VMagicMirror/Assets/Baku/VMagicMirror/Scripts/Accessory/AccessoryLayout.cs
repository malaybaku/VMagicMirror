using System;
using UnityEngine;

namespace Baku.VMagicMirror
{
    //WPFとjsonを投げ合うときに使うデータ。名前とかを軽率に変えないよう注意
    
    //値の定義順も含めてWPFと一致させる
    public enum AccessoryAttachTarget
    {
        Head = 0,
        Neck = 1,
        RightHand = 2,
        LeftHand = 3,
        Chest = 4,
        Waist = 5,
        World = 6,
    }

    //値の定義順も含めてWPFと一致させる
    public enum AccessoryImageResolutionLimit
    {
        None = 0,
        Max1024 = 1,
        Max512 = 2,
        Max256 = 3,
        Max128 = 4,
    }

    [Serializable]
    public class AccessoryResetTargetItems
    {
        public string[] FileIds;
    }
    
    [Serializable]
    public class AccessoryLayouts
    {
        public AccessoryItemLayout[] Items;
    }

    [Serializable]
    public class AccessoryItemLayout
    {
        //NOTE: 他のプロパティとは異なりキーのように用いられる。ユーザーは編集できない
        public string FileId = "";
        //NOTE: Nameは投げつけてもWPF側で見ないが、WPFから飛んでくるので一応書いてある
        public string Name = "";
        public bool IsVisible;
        public AccessoryAttachTarget AttachTarget;
        public Vector3 Position;
        public Vector3 Rotation;
        public Vector3 Scale = Vector3.one;
        public bool UseBillboardMode;
        //連番画像でのみ意味のある値
        public int FramePerSecond;
        //画像または連番画像でのみ意味のある値 (※glTFのテクスチャに適用する手もあるが、面倒なので一旦パス)
        public AccessoryImageResolutionLimit ResolutionLimit;

        public int GetResolutionLimitSize() => ResolutionLimit switch
        {
            AccessoryImageResolutionLimit.Max1024 => 1024,
            AccessoryImageResolutionLimit.Max512 => 512,
            AccessoryImageResolutionLimit.Max256 => 256,
            AccessoryImageResolutionLimit.Max128 => 128,
            //無限大のことを指す
            _ => 16384,
        };
    }
}
