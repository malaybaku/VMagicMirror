using System;
using UnityEngine;

namespace Baku.VMagicMirror.Buddy
{
    //NOTE:
    // - ここで定義するのはMessageFactory経由でWPFから受けるデータ定義
    // - デシリアライズには使うが、それ以降は別のrecordライクな型に詰め替えて使う
    [Serializable]
    public class BuddySettingsMessage
    {
        public string BuddyId;
        public BuddySettingsPropertyMessage[] Properties;
    }

    [Serializable]
    public class BuddySettingsPropertyMessage
    {
        //NOTE: メッセージがBuddySettingsMessage.Propertiesの要素の場合、ここは自明な値として空白のままになる
        public string BuddyId;

        public string Name;
        public string Type;

        public bool BoolValue;
        public int IntValue;
        public float FloatValue;
        public string StringValue;
        public BuddyVector2 Vector2Value;
        public BuddyVector3 Vector3Value;
    }
    
    [Serializable]
    public struct BuddyVector2
    {
        public float X;
        public float Y;

        public Vector2 ToVector2() => new(X, Y);
    }

    [Serializable]
    public struct BuddyVector3
    {
        public float X;
        public float Y;
        public float Z;

        public Vector3 ToVector3() => new(X, Y, Z);
        public Quaternion ToQuaternion() => Quaternion.Euler(X, Y, Z);
    }
}
