using System.Collections.Generic;
using System.Threading.Tasks;

namespace Baku.VMagicMirror.Buddy.Api.Interface
{
    public interface IVrmApi : IObject3DApi
    {
        Task LoadAsync(string path);

        // NOTE: LoadAsyncが終わったあとで呼び出すのが期待値
        void Show();

        // ポーズ
        void SetBoneRotation(HumanBodyBones bone, Quaternion localRotation);
        // NOTE: これはワールド姿勢
        void SetHipsPosition(Vector3 position);

        // NOTE: このへんは「サブキャラが通信を受ける」とか「オレオレフォーマットのファイルを使って何か再生する」といったケースで使いうる
        void SetBoneRotations(IReadOnlyDictionary<HumanBodyBones, Quaternion> localRotations);
        void SetMuscles(float?[] muscles);
        
        
        // 表情
        string[] GetCustomBlendShapeNames();
        bool HasBlendShape(string name);
        float GetBlendShape(string name);
        void SetBlendShape(string name, float value);

        // VRMA
        void RunVrma(string path, bool immediate);
        void StopVrma();
    }
}
