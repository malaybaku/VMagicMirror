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
        public BuddyTransform2D Transform2DValue;
        public BuddyTransform3D Transform3DValue;
    }
    
    [Serializable]
    public struct BuddyVector2
    {
        public float X;
        public float Y;

        public Vector2 ToVector2() => new(X, Y);
        public static BuddyVector2 FromVector2(Vector2 v) => new() { X = v.x, Y = v.y };
    }

    [Serializable]
    public struct BuddyVector3
    {
        public float X;
        public float Y;
        public float Z;

        public Vector3 ToVector3() => new(X, Y, Z);
        public Quaternion ToQuaternion() => Quaternion.Euler(X, Y, Z);
        public static BuddyVector3 FromVector3(Vector3 v) => new() { X = v.x, Y = v.y, Z = v.z };
    }

    //NOTE: 下記2つはデータ上はPropertyの一種として飛んでくるが、Unityの内部ではLayoutという(Propertyと別の)枠組みで扱っている
    [Serializable]
    public struct BuddyTransform2D
    {
        public BuddyVector2 Position;
        public BuddyVector3 Rotation;
        public float Scale;
    }

    [Serializable]
    public struct BuddyTransform3D
    {
        public BuddyVector3 Position;
        public BuddyVector3 Rotation;
        public float Scale;
        // NOTE: アタッチ先ボーンがない場合は -1 が入ってるのが期待値ではある。
        // が、もしかしたら処理の都合でbool (HasParentBone)を後付けするかも
        public int ParentBone;
    }
}
