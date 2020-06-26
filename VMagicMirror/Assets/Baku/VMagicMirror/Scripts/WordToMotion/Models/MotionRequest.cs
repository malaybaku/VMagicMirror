using System;
using System.Collections.Generic;
using UnityEngine;

namespace Baku.VMagicMirror
{
    //NOTE: WPF側のMotionRequestとプロパティ名を統一してます。片方だけいじらないように！

    /// <summary>単一のBVHまたはビルトインモーション、および表情制御のリクエスト情報を表す。</summary>
    [Serializable]
    public class MotionRequest
    {
        public const int MotionTypeNone = 0;
        public const int MotionTypeBuiltInClip = 1;
        public const int MotionTypeBvhFile = 2;

        public int MotionType;
        public string Word;
        public string BuiltInAnimationClipName;
        public string ExternalBvhFilePath;
        public float DurationWhenOnlyBlendShape;
        
        //TODO: ここにHoldBlendShapeとかHoldPoseとかを追加するかも

        /// <summary>
        /// ブレンドシェイプを適用すべきか否か
        /// </summary>
        public bool UseBlendShape;

        /// <summary>
        /// アニメーションの終了後にブレンドシェイプをそのままの状態にすべきか否か
        /// </summary>
        public bool HoldBlendShape;

        /// <summary>
        /// リップシンクに関係あるブレンドシェイプについて、リップシンクのブレンドシェイプ指定を優先するか否か
        /// </summary>
        public bool PreferLipSync;

        //NOTE: 辞書にしないでこのまま使う手も無くはないです
        
        public BlendShapeValues BlendShapeValues = null;

        public List<MotionRequestBlendShapeItem> ExtraBlendShapeValues = null;

        [NonSerialized]
        private Dictionary<string, float> _blendShapeValues = null;
        /// <summary>
        /// ブレンドシェイプ一覧
        /// </summary>
        public Dictionary<string, float> BlendShapeValuesDic
        {
            get
            {
                if (_blendShapeValues == null)
                {
                    _blendShapeValues = BlendShapeValues.ToDic();
                    foreach (var pair in ExtraBlendShapeValues)
                    {
                        _blendShapeValues[pair.Name] = pair.Value * 0.01f;
                    }
                }
                return _blendShapeValues;
            }
        }

        /// <summary>デフォルトの簡単な設定からなる動作リクエストを生成します。</summary>
        /// <returns></returns>
        public static MotionRequest FromJson(string content)
        {
            //TODO: ちょっとここ安定性低いからtry catchしないとダメじゃないかな！
            return JsonUtility.FromJson<MotionRequest>(content);
        }
    }

    [Serializable]
    public class MotionRequestBlendShapeItem
    {
        public string Name;
        public int Value;
    }

    /// <summary>
    /// NOTE: コレクションクラスを作ってるのはJSONのルートをオブジェクトにするため
    /// </summary>
    [Serializable]
    public class MotionRequestCollection
    {
        public MotionRequest[] Requests;

        public static MotionRequestCollection FromJson(string json)
            => JsonUtility.FromJson<MotionRequestCollection>(json);
    }

    [Serializable]
    public class BlendShapeValues
    {
        public int Joy;
        public int Neutral;
        public int Angry;
        public int Sorrow;
        public int Fun;
        public int Blink;
        public int Blink_L;
        public int Blink_R;
        public int A;
        public int I;
        public int U;
        public int E;
        public int O;
        public int LookUp;
        public int LookDown;
        public int LookLeft;
        public int LookRight;

        public Dictionary<string, float> ToDic() => new Dictionary<string, float>()
        {
            [nameof(Joy)] = Joy * 0.01f,
            [nameof(Neutral)] = Neutral * 0.01f,
            [nameof(Angry)] = Angry * 0.01f,
            [nameof(Sorrow)] = Sorrow * 0.01f,
            [nameof(Fun)] = Fun * 0.01f,
            [nameof(Blink)] = Blink * 0.01f,
            [nameof(Blink_L)] = Blink_L * 0.01f,
            [nameof(Blink_R)] = Blink_R * 0.01f,
            [nameof(A)] = A * 0.01f,
            [nameof(I)] = I * 0.01f,
            [nameof(U)] = U * 0.01f,
            [nameof(E)] = E * 0.01f,
            [nameof(O)] = O * 0.01f,
            [nameof(LookUp)] = LookUp * 0.01f,
            [nameof(LookDown)] = LookDown * 0.01f,
            [nameof(LookLeft)] = LookLeft * 0.01f,
            [nameof(LookRight)] = LookRight * 0.01f,
        };
    }
}
