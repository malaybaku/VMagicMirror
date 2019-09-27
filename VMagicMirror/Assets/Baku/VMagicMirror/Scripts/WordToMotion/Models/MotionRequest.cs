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
        /// <remarks>この設定を反映するには(Proxyではない本来の)ブレンドシェイプ解析が必要なことに注意</remarks>
        public bool PreferLipSync;

        //NOTE: 辞書にしないでこのまま使う手も無くはないです
        [SerializeField] private BlendShapeValues BlendShapeValues = null;

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
                    _blendShapeValues = new Dictionary<string, float>()
                    {
                        ["Joy"] = BlendShapeValues.Joy * 0.01f,
                        ["Angry"] = BlendShapeValues.Angry * 0.01f,
                        ["Sorrow"] = BlendShapeValues.Sorrow * 0.01f,
                        ["Fun"] = BlendShapeValues.Fun * 0.01f,
                        ["Neutral"] = BlendShapeValues.Neutral * 0.01f,

                        ["Blink"] = BlendShapeValues.Blink * 0.01f,
                        ["Blink_L"] = BlendShapeValues.Blink_L * 0.01f,
                        ["Blink_R"] = BlendShapeValues.Blink_R * 0.01f,

                        ["A"] = BlendShapeValues.A * 0.01f,
                        ["I"] = BlendShapeValues.I * 0.01f,
                        ["U"] = BlendShapeValues.U * 0.01f,
                        ["E"] = BlendShapeValues.E * 0.01f,
                        ["O"] = BlendShapeValues.O * 0.01f,

                        ["LookUp"] = BlendShapeValues.LookUp * 0.01f,
                        ["LookDown"] = BlendShapeValues.LookDown * 0.01f,
                        ["LookLeft"] = BlendShapeValues.LookLeft * 0.01f,
                        ["LookRight"] = BlendShapeValues.LookRight * 0.01f,
                    };
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
    }
}
