using UnityEngine;

namespace Baku.VMagicMirror.Buddy.Api
{
    public class CameraApi
    {
        //TODO: setterも公開してしまいたい？(この方法でsetした姿勢はあんま保存したくないが、制御は許可したい)
        public Vector3 Position => Vector3.zero;
        public Quaternion Rotation => Quaternion.identity;
        public float FieldOfView => 40f;
    }
}

