using System;
using System.Collections.Generic;
using UnityEngine;

namespace Baku.VMagicMirror
{
    //NOTE: WPF側のMotionRequestとプロパティ名を統一してます。片方だけいじらないように！
    //TODO: これは通信特化なデータ定義なので、別途アプリケーション内で引き回しやすい(ほぼ等価な)データ型を作ってもよい

    /// <summary>ビルトインモーションまたはカスタムモーション、および表情制御のリクエスト情報を表す。</summary>
    [Serializable]
    public class MotionRequest
    {
        public const int MotionTypeNone = 0;
        public const int MotionTypeBuiltInClip = 1;
        public const int MotionTypeCustom = 2;

        public int MotionType;
        public string Word;
        public string BuiltInAnimationClipName;
        public string CustomMotionClipName;
        public float DurationWhenOnlyBlendShape;
        
        //TODO: ここにHoldPoseとかを追加するかも

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

        /// <summary>
        /// モーション実行中のみアクセサリを表示したい場合、そのアクセサリーのFileId
        /// </summary>
        public string AccessoryName;

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

    //NOTE: VRM 0.x時代との互換性についてはWPF側が吸収する想定なことに注意。
    //Unity側はVRM 0.xのBlendShapeClipの呼称には関知しない
    [Serializable]
    public class BlendShapeValues
    {
        public int Happy;
        public int Neutral;
        public int Angry;
        public int Sad;
        public int Relaxed;
        public int Surprised;
        public int Blink;
        public int BlinkLeft;
        public int BlinkRight;
        public int Aa;
        public int Ih;
        public int Ou;
        public int Ee;
        public int Oh;
        public int LookUp;
        public int LookDown;
        public int LookLeft;
        public int LookRight;

        public Dictionary<string, float> ToDic() => new Dictionary<string, float>()
        {
            [nameof(Neutral)] = Neutral * 0.01f,
            [nameof(Happy)] = Happy * 0.01f,
            [nameof(Angry)] = Angry * 0.01f,
            [nameof(Sad)] = Sad * 0.01f,
            [nameof(Relaxed)] = Relaxed * 0.01f,
            [nameof(Surprised)] = Surprised * 0.01f,
            [nameof(Blink)] = Blink * 0.01f,
            [nameof(BlinkLeft)] = BlinkLeft * 0.01f,
            [nameof(BlinkRight)] = BlinkRight * 0.01f,
            [nameof(Aa)] = Aa * 0.01f,
            [nameof(Ih)] = Ih * 0.01f,
            [nameof(Ou)] = Ou * 0.01f,
            [nameof(Ee)] = Ee * 0.01f,
            [nameof(Oh)] = Oh * 0.01f,
            [nameof(LookUp)] = LookUp * 0.01f,
            [nameof(LookDown)] = LookDown * 0.01f,
            [nameof(LookLeft)] = LookLeft * 0.01f,
            [nameof(LookRight)] = LookRight * 0.01f,
        };
    }
}
