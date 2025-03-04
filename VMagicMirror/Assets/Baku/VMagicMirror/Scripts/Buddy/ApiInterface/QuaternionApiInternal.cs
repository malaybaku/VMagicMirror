namespace Baku.VMagicMirror.Buddy.Api.Interface
{
    // Quaternionのうち、メンバーのシグネチャ自体がUnityEngineにかかるようなものを分離した定義。
    // このファイルだけ分離しているのはdocfxのパースのエラー対策であり、Runtime的な意味は特にない。
    public partial struct Quaternion
    {
        private UnityEngine.Quaternion ToIQ() => new(x, y, z, w);
        private static UnityEngine.Vector3 ToIV3(Vector3 v) => new(v.x, v.y, v.z);
        private static Vector3 ToVector3(UnityEngine.Vector3 v) => new(v.x, v.y, v.z);
        private static Quaternion ToQuaternion(UnityEngine.Quaternion iq) => new(iq.x, iq.y, iq.z, iq.w);
    }
}

