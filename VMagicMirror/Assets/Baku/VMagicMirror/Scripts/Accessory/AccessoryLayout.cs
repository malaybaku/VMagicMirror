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
    }
}
