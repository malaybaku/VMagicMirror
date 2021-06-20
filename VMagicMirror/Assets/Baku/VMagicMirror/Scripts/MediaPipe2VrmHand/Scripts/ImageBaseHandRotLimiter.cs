using UnityEngine;

namespace Baku.VMagicMirror
{
    public class ImageBaseHandRotLimiter
    {
        public ImageBaseHandRotLimiter(ImageBaseHandLimitSetting setting)
        {
            _setting = setting;
        }

        private readonly ImageBaseHandLimitSetting _setting;

        public Quaternion CalculateRightHandRotation(Quaternion rot)
        {
            var offsetRot = Quaternion.Euler(_setting.rightHandRotationOffset);

            int closestIndex = -1;
            float closestDiff = 0f;

            for (int i = 0; i < _setting.items.Length; i++)
            {
                var refRot = Quaternion.Euler(_setting.items[i].rotationEuler) * offsetRot;
                
                (rot * Quaternion.Inverse(refRot)).ToAngleAxis(out var angleDiff, out _);

                angleDiff = Mathf.Repeat(angleDiff + 180f, 360f) - 180f;
                //まともな姿勢に十分近い事が分かった: このままの値で通す
                if (Mathf.Abs(angleDiff) < _setting.items[i].angle)
                {
                    return rot;
                }
                else
                {
                    //どのくらい回転させればマトモな角度になるかをチェック
                    var diff = Mathf.Abs(angleDiff) - _setting.items[i].angle;
                    
                    if (closestIndex < 0 || diff < closestDiff)
                    {
                        closestIndex = i;
                        closestDiff = diff;
                    }
                }
            }
            
            //どの基準姿勢からも離れている: なるべく少ない回転量で許容できる姿勢まで持っていく
            var resultBase = Quaternion.Euler(_setting.items[closestIndex].rotationEuler) * offsetRot;
                
            (rot * Quaternion.Inverse(resultBase)).ToAngleAxis(out var adjustAngle, out var adjustAxis);
            adjustAngle = Mathf.Repeat(adjustAngle + 180f, 360f) - 180f;
            adjustAngle = Mathf.Sign(adjustAngle) * _setting.items[closestIndex].angle;
            
            return Quaternion.AngleAxis(adjustAngle, adjustAxis) * resultBase;
        }

        public Quaternion CalculateLeftHandRotation(Quaternion rot)
        {
            var offsetRot = Quaternion.Euler(_setting.leftHandRotationOffset);
            return rot;
        }

    }
}
