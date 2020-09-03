using System;
using UnityEngine;

namespace Baku.VMagicMirror
{
    [Serializable]
    public class CalibrationData
    {
        //NOTE: イメージ的には「ユーザー正面向き時のカメラから見たユーザーの位置」っぽいものが入る…はず
        public bool hasOpenCvPose;
        public Vector3 openCvFacePos;
        public Vector3 openCvFaceRotEuler;
        
        public float faceSize;
        public Vector2 faceCenter;

        public void SetDefaultValues()
        {
            hasOpenCvPose = false;
            openCvFacePos = Vector3.zero;
            openCvFaceRotEuler = Vector3.zero;
            
            faceSize = 0.1f;
            faceCenter = Vector2.zero;
        }
    }
}
