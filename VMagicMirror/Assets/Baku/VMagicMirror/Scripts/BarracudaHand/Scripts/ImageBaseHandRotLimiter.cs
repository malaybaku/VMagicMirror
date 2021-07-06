using UnityEngine;

namespace Baku.VMagicMirror
{
    public class ImageBaseHandRotLimiter
    {
        public ImageBaseHandRotLimiter(ImageBaseHandLimitSetting setting)
        {
            _setting = setting;
            _leftOffset =
                Quaternion.AngleAxis(-90f, Vector3.right) *
                Quaternion.Euler(_setting.leftHandRotationOffset);
            _rightOffset =
                Quaternion.AngleAxis(-90f, Vector3.right) *
                Quaternion.Euler(_setting.rightHandRotationOffset);

            _leftOffsetInverse = Quaternion.Inverse(_leftOffset);
            _rightOffsetInverse = Quaternion.Inverse(_rightOffset);
        }

        private readonly ImageBaseHandLimitSetting _setting;
        private readonly Quaternion _leftOffset;
        private readonly Quaternion _rightOffset;

        private readonly Quaternion _leftOffsetInverse;
        private readonly Quaternion _rightOffsetInverse;

        public Quaternion CalculateRightHandRotation(Quaternion rot)
            => CalculateClampedRotation(rot, false);

        public Quaternion CalculateLeftHandRotation(Quaternion rot)
            => CalculateClampedRotation(rot, true);

        private Quaternion CalculateClampedRotation(Quaternion rot, bool isLeftHand)
        {
            //NOTE: 普通のQuaternion.eulerAnglesではダメなので注意
            var euler = GetEulerAngles(rot, isLeftHand);
            
            var minAngles = isLeftHand
                ? new Vector3(_setting.eulerMinAngles.x, -_setting.eulerMaxAngles.y, -_setting.eulerMaxAngles.z)
                : _setting.eulerMinAngles;
            var maxAngles = isLeftHand
                ? new Vector3(_setting.eulerMaxAngles.x, -_setting.eulerMinAngles.y, -_setting.eulerMinAngles.z)
                : _setting.eulerMaxAngles;

            return 
                Quaternion.Euler(MathUtil.ClampVector3(euler, minAngles, maxAngles)) *
                (isLeftHand ? _leftOffset : _rightOffset);
        }
        
        // 手首のワールド姿勢について、それを手のひらが正面向きに立った姿勢を基準にしたオイラー表現に変換します。
        private Vector3 GetEulerAngles(Quaternion rot, bool isLeftHand)
        {
            //基準姿勢が通常のVRMの姿勢じゃないためキャンセル
            var inverse = isLeftHand ? _leftOffsetInverse : _rightOffsetInverse;
            var diffRot = rot * inverse;
            
            var forward = diffRot * Vector3.forward;
            var right = diffRot * Vector3.right;

            //ヨー: 正面方向ベクトルがどっち向いたかをチェック
            var yaw = Mathf.Atan2(forward.x, forward.z);
            //ピッチ: forwardが上がったか下がったか見る。手の場合はふつう下に曲がり、その回転はYZ方向なので正
            var pitch = Mathf.Asin(-forward.y);
            //ロール: rightベクトルの上下で見る。
            //Atan2で書くと更にロバストになるが、多分そこまでしないでもいいはず
            var roll = Mathf.Asin(right.y);

            return new Vector3(pitch, yaw, roll) * Mathf.Rad2Deg;
        }
    }
}
