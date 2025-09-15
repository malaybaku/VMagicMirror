using UnityEngine;

namespace Baku.VMagicMirror
{
    public static class FingerRotationAxisGetter
    {
        public static Vector3 GetFingerBendRotationAxis(int fingerNumber, Transform[][] fingers)
        {
            return fingerNumber < 5
                ? GetLeftHandFingerBendRotationAxis(fingerNumber, fingers)
                : GetRightHandFingerBendRotationAxis(fingerNumber, fingers);
        }
        
        private static Vector3 GetLeftHandFingerBendRotationAxis(int fingerNumber, Transform[][] fingers)
        {
            if (fingerNumber == FingerConsts.LeftThumb)
            {
                return GetLeftThumbRotationAxis(fingers);
            }

            // 親指以外では「Tポーズのときに指がすでにちょっと開いてるかどうか」だけ踏まえて微調整する。
            // 回転軸はほぼ +z/-z 軸に沿っているが、モデルの状態によってはx軸成分も入る
            if (fingers[fingerNumber] == null || fingers[fingerNumber].Length < 3)
            {
                return Vector3.forward;
            }
            
            // VRM的に「キレイなTポーズ」では、このベクトルが -x 方向になるが、モデルによって多少ブレる
            var diff = fingers[fingerNumber][0].position - fingers[fingerNumber][2].position;
            diff.y = 0f;
            if (IsSmall(diff))
            {
                return Vector3.forward;
            }

            // NOTE: diffからy成分を除去してdiffとupが直交なのを保証しているため、必ずCrossの計算結果が単位ベクトルになる
            return Vector3.Cross(-diff.normalized, Vector3.up);
        }
        
        private static Vector3 GetRightHandFingerBendRotationAxis(int fingerNumber, Transform[][] fingers)
        {
            if (fingerNumber == FingerConsts.RightThumb)
            {
                return GetRightThumbRotationAxis(fingers);
            }

            if (fingers[fingerNumber] == null || fingers[fingerNumber].Length < 3)
            {
                return Vector3.back;
            }

            var diff = fingers[fingerNumber][0].position - fingers[fingerNumber][2].position;
            diff.y = 0f;
            if (IsSmall(diff))
            {
                return Vector3.back;
            }

            return Vector3.Cross(-diff.normalized, Vector3.up);
        }
            
        private static Vector3 GetLeftThumbRotationAxis(Transform[][] fingers)
        {
            // 計算アプローチ
            // - 「手首 - 親指第2関節 - 人差し指の付け根」 で平面を張っていることにする
            // - 「人指 - 親指」の正規化したベクトルを回転軸にする
            // - 必要なボーンが足りない場合、諦めてVector3.upとかVector3.downを返す
            
            if (fingers[0] == null || fingers[0].Length < 3 || fingers[1] == null || fingers[1].Length < 2)
            {
                return Vector3.down;
            }
            
            // NOTE: thumbは distal ~ proximalのどれをリファレンスにするかがちょっと悩ましい (一長一短ある)
            var thumbPos = fingers[0][1].position;
            var indexProximal = fingers[1][2].position;

            var diff = indexProximal - thumbPos;
            if (IsSmall(diff))
            {
                return Vector3.down;
            }
            
            // NOTE: 左手ではマイナスをつけないと正の角度に対しておかしくなるので注意
            return -(diff.normalized);
        }
        
        private static Vector3 GetRightThumbRotationAxis(Transform[][] fingers)
        {
            if (fingers[5] == null || fingers[5].Length < 3 || fingers[6] == null || fingers[6].Length < 2)
            {
                return Vector3.up;
            }
            
            var thumbPos = fingers[5][1].position;
            var indexProximal = fingers[6][2].position;
            
            var diff = indexProximal - thumbPos;
            if (IsSmall(diff))
            {
                return Vector3.up;
            }
            return diff.normalized;
        }

        // ベクトルが小さすぎて計算が怪しいのを判定するやつ
        private static bool IsSmall(Vector3 v) => v.sqrMagnitude < (0.001f * 0.001f);
    }
}
