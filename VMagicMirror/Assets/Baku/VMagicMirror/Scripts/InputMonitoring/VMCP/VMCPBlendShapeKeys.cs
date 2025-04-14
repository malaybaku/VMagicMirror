using System.Diagnostics.CodeAnalysis;

namespace Baku.VMagicMirror.VMCP
{
    public static class VMCPBlendShapePerfectSyncKeys
    {
        public static bool TrySet(string rawKey, float value, RecordFaceTrackBlendShapes dest, out string camelCaseKey)
        {
            if (string.IsNullOrEmpty(rawKey) || rawKey.Length < 7)
            {
                camelCaseKey = "";
                return false;
            }

            // NOTE: PascalCaseも含めてswitchさせたらこれを省けるが、それはそれでめんどいので無し
            var key = char.ToLower(rawKey[0]) + rawKey[1..];

            switch (key)
            {
                // 目
                case CamelCaseKeys.eyeBlinkLeft: dest.Eye.LeftBlink = value; break;
                case CamelCaseKeys.eyeLookUpLeft: dest.Eye.LeftLookUp = value; break;
                case CamelCaseKeys.eyeLookDownLeft: dest.Eye.LeftLookDown = value; break;
                case CamelCaseKeys.eyeLookInLeft: dest.Eye.LeftLookIn = value; break;
                case CamelCaseKeys.eyeLookOutLeft: dest.Eye.LeftLookOut = value; break;
                case CamelCaseKeys.eyeWideLeft: dest.Eye.LeftWide = value; break;
                case CamelCaseKeys.eyeSquintLeft: dest.Eye.LeftSquint = value; break;

                case CamelCaseKeys.eyeBlinkRight: dest.Eye.RightBlink = value; break;
                case CamelCaseKeys.eyeLookUpRight: dest.Eye.RightLookUp = value; break;
                case CamelCaseKeys.eyeLookDownRight: dest.Eye.RightLookDown = value; break;
                case CamelCaseKeys.eyeLookInRight: dest.Eye.RightLookIn = value; break;
                case CamelCaseKeys.eyeLookOutRight: dest.Eye.RightLookOut = value; break;
                case CamelCaseKeys.eyeWideRight: dest.Eye.RightWide = value; break;
                case CamelCaseKeys.eyeSquintRight: dest.Eye.RightSquint = value; break;

                //口(多い)
                case CamelCaseKeys.mouthLeft: dest.Mouth.Left = value; break;
                case CamelCaseKeys.mouthSmileLeft: dest.Mouth.LeftSmile = value; break;
                case CamelCaseKeys.mouthFrownLeft: dest.Mouth.LeftFrown = value; break;
                case CamelCaseKeys.mouthPressLeft: dest.Mouth.LeftPress = value; break;
                case CamelCaseKeys.mouthUpperUpLeft: dest.Mouth.LeftUpperUp = value; break;
                case CamelCaseKeys.mouthLowerDownLeft: dest.Mouth.LeftLowerDown = value; break;
                case CamelCaseKeys.mouthStretchLeft: dest.Mouth.LeftStretch = value; break;
                case CamelCaseKeys.mouthDimpleLeft: dest.Mouth.LeftDimple = value; break;

                case CamelCaseKeys.mouthRight: dest.Mouth.Right = value; break;
                case CamelCaseKeys.mouthSmileRight: dest.Mouth.RightSmile = value; break;
                case CamelCaseKeys.mouthFrownRight: dest.Mouth.RightFrown = value; break;
                case CamelCaseKeys.mouthPressRight: dest.Mouth.RightPress = value; break;
                case CamelCaseKeys.mouthUpperUpRight: dest.Mouth.RightUpperUp = value; break;
                case CamelCaseKeys.mouthLowerDownRight: dest.Mouth.RightLowerDown = value; break;
                case CamelCaseKeys.mouthStretchRight: dest.Mouth.RightStretch = value; break;
                case CamelCaseKeys.mouthDimpleRight: dest.Mouth.RightDimple = value; break;

                case CamelCaseKeys.mouthClose: dest.Mouth.Close = value; break;
                case CamelCaseKeys.mouthFunnel: dest.Mouth.Funnel = value; break;
                case CamelCaseKeys.mouthPucker: dest.Mouth.Pucker = value; break;
                case CamelCaseKeys.mouthShrugUpper: dest.Mouth.ShrugUpper = value; break;
                case CamelCaseKeys.mouthShrugLower: dest.Mouth.ShrugLower = value; break;
                case CamelCaseKeys.mouthRollUpper: dest.Mouth.RollUpper = value; break;
                case CamelCaseKeys.mouthRollLower: dest.Mouth.RollLower = value; break;

                //あご
                case CamelCaseKeys.jawOpen: dest.Jaw.Open = value; break;
                case CamelCaseKeys.jawForward: dest.Jaw.Forward = value; break;
                case CamelCaseKeys.jawLeft: dest.Jaw.Left = value; break;
                case CamelCaseKeys.jawRight: dest.Jaw.Right = value; break;

                //鼻
                case CamelCaseKeys.noseSneerLeft: dest.Nose.LeftSneer = value; break;
                case CamelCaseKeys.noseSneerRight: dest.Nose.RightSneer = value; break;

                //ほお
                case CamelCaseKeys.cheekPuff: dest.Cheek.Puff = value; break;
                case CamelCaseKeys.cheekSquintLeft: dest.Cheek.LeftSquint = value; break;
                case CamelCaseKeys.cheekSquintRight: dest.Cheek.RightSquint = value; break;

                // 舌
                case CamelCaseKeys.tongueOut: dest.Tongue.TongueOut = value; break;

                //まゆげ
                case CamelCaseKeys.browDownLeft: dest.Brow.LeftDown = value; break;
                case CamelCaseKeys.browOuterUpLeft: dest.Brow.LeftOuterUp = value; break;
                case CamelCaseKeys.browDownRight: dest.Brow.RightDown = value; break;
                case CamelCaseKeys.browOuterUpRight: dest.Brow.RightOuterUp = value; break;
                case CamelCaseKeys.browInnerUp: dest.Brow.InnerUp = value; break;

                
                default:
                    camelCaseKey = "";
                    return false;
            }

            // ここに到達 = defaultのとこを通ってないので有効なキー
            camelCaseKey = key;
            return true;
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static class CamelCaseKeys
        {
            //目
            public const string eyeBlinkLeft = nameof(eyeBlinkLeft);
            public const string eyeLookUpLeft = nameof(eyeLookUpLeft);
            public const string eyeLookDownLeft = nameof(eyeLookDownLeft);
            public const string eyeLookInLeft = nameof(eyeLookInLeft);
            public const string eyeLookOutLeft = nameof(eyeLookOutLeft);
            public const string eyeWideLeft = nameof(eyeWideLeft);
            public const string eyeSquintLeft = nameof(eyeSquintLeft);

            public const string eyeBlinkRight = nameof(eyeBlinkRight);
            public const string eyeLookUpRight = nameof(eyeLookUpRight);
            public const string eyeLookDownRight = nameof(eyeLookDownRight);
            public const string eyeLookInRight = nameof(eyeLookInRight);
            public const string eyeLookOutRight = nameof(eyeLookOutRight);
            public const string eyeWideRight = nameof(eyeWideRight);
            public const string eyeSquintRight = nameof(eyeSquintRight);

            //口(多い)
            public const string mouthLeft = nameof(mouthLeft);
            public const string mouthSmileLeft = nameof(mouthSmileLeft);
            public const string mouthFrownLeft = nameof(mouthFrownLeft);
            public const string mouthPressLeft = nameof(mouthPressLeft);
            public const string mouthUpperUpLeft = nameof(mouthUpperUpLeft);
            public const string mouthLowerDownLeft = nameof(mouthLowerDownLeft);
            public const string mouthStretchLeft = nameof(mouthStretchLeft);
            public const string mouthDimpleLeft = nameof(mouthDimpleLeft);

            public const string mouthRight = nameof(mouthRight);
            public const string mouthSmileRight = nameof(mouthSmileRight);
            public const string mouthFrownRight = nameof(mouthFrownRight);
            public const string mouthPressRight = nameof(mouthPressRight);
            public const string mouthUpperUpRight = nameof(mouthUpperUpRight);
            public const string mouthLowerDownRight = nameof(mouthLowerDownRight);
            public const string mouthStretchRight = nameof(mouthStretchRight);
            public const string mouthDimpleRight = nameof(mouthDimpleRight);

            public const string mouthClose = nameof(mouthClose);
            public const string mouthFunnel = nameof(mouthFunnel);
            public const string mouthPucker = nameof(mouthPucker);
            public const string mouthShrugUpper = nameof(mouthShrugUpper);
            public const string mouthShrugLower = nameof(mouthShrugLower);
            public const string mouthRollUpper = nameof(mouthRollUpper);
            public const string mouthRollLower = nameof(mouthRollLower);

            //あご
            public const string jawOpen = nameof(jawOpen);
            public const string jawForward = nameof(jawForward);
            public const string jawLeft = nameof(jawLeft);
            public const string jawRight = nameof(jawRight);

            //鼻
            public const string noseSneerLeft = nameof(noseSneerLeft);
            public const string noseSneerRight = nameof(noseSneerRight);

            //ほお
            public const string cheekPuff = nameof(cheekPuff);
            public const string cheekSquintLeft = nameof(cheekSquintLeft);
            public const string cheekSquintRight = nameof(cheekSquintRight);

            //舌
            public const string tongueOut = nameof(tongueOut);

            //まゆげ
            public const string browDownLeft = nameof(browDownLeft);
            public const string browOuterUpLeft = nameof(browOuterUpLeft);
            public const string browDownRight = nameof(browDownRight);
            public const string browOuterUpRight = nameof(browOuterUpRight);
            public const string browInnerUp = nameof(browInnerUp);
        }
    }
}