using UnityEngine;

namespace Baku.VMagicMirror.VMCP
{
    public enum VMCPMessageType
    {
        //NOTE: VMCPで想定されててもVMagicMirror的に知らんやつはUnknownになる
        Unknown,
        DefineRoot,
        ForwardKinematics,
        TrackerPose,
        BlendShapeValue,
        BlendShapeApply,
    }

    public enum VMCPTrackerPoseType
    {
        Unknown,
        Head,
        LeftHand,
        RightHand,
        Spine,
        Hips,
    }
    
    //NOTE: もし今後OSCを他の用途で使う場合、関数名をもうちょっと限定的なコンテキストに直すべき
    public static class OSCMessageExtensions
    {
        public static VMCPMessageType GetMessageType(this uOSC.Message message)
        {
            return message.address switch
            {
                "/VMC/Ext/Root/Pos" => VMCPMessageType.DefineRoot,
                "/VMC/Ext/Bone/Pos" => VMCPMessageType.ForwardKinematics,
                "/VMC/Ext/Tra/Pos" => VMCPMessageType.TrackerPose,
                "/VMC/Ext/Blend/Val" => VMCPMessageType.BlendShapeValue,
                "/VMC/Ext/Blend/Apply" => VMCPMessageType.BlendShapeApply,
                _ => VMCPMessageType.Unknown,
            };
        }

        public static string GetForwardKinematicBoneName(this uOSC.Message message)
        {
            var values = message.values;
            //pos + rotの値が揃ってないなら見る価値ないので、要素数は1ではなく8で見る
            if (values.Length < 8 || !(values[0] is string name))
            {
                return "";
            }
            return name;
        }
        
        public static VMCPTrackerPoseType GetTrackerPoseType(this uOSC.Message message, out string nameResult)
        {
            var values = message.values;
            //pos + rotの値が揃ってないなら見る価値ないので、要素数は1ではなく8で見る
            if (values.Length < 8 || !(values[0] is string name))
            {
                nameResult = "";
                return VMCPTrackerPoseType.Unknown;
            }

            nameResult = name;
            return name switch
            {
                "Head" => VMCPTrackerPoseType.Head,
                "LeftHand" => VMCPTrackerPoseType.LeftHand,
                "RightHand" => VMCPTrackerPoseType.RightHand,
                "Spine" => VMCPTrackerPoseType.Spine,
                "Hips" => VMCPTrackerPoseType.Hips,
                _ => VMCPTrackerPoseType.Unknown,
            };
        }

        public static (Vector3 pos, Quaternion rot) GetPose(this uOSC.Message message)
        {
            var values = message.values;
            
            if (!(values.Length > 7 && 
                  values[1] is float posX &&
                  values[2] is float posY &&
                  values[3] is float posZ &&
                  values[4] is float rotX &&
                  values[5] is float rotY &&
                  values[6] is float rotZ &&
                  values[7] is float rotW))
            {
                return (Vector3.zero, Quaternion.identity);
            }
            
            return (
                new Vector3(posX, posY, posZ),
                new Quaternion(rotX, rotY, rotZ, rotW)
            );
        }

        public static bool TryGetBlendShapeValue(this uOSC.Message message, out string key, out float value)
        {
            var values = message.values;
            if (values.Length > 1 && values[0] is string keyResult && values[1] is float valueResult)
            {
                key = keyResult;
                value = valueResult;
                return true;
            }
            else
            {
                key = "";
                value = 0f;
                return false;
            }            
        }
    }
}
