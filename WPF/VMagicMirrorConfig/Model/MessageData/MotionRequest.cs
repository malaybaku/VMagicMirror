using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Baku.VMagicMirrorConfig
{
    //NOTE: Unity側のMotionRequestとプロパティ名を統一してます。片方だけいじらないように！

    /// <summary>ビルトインモーションまたはカスタムモーション、および表情制御のリクエスト情報を表す。</summary>
    public class MotionRequest
    {
        public const int MotionTypeNone = 0;
        public const int MotionTypeBuiltInClip = 1;
        public const int MotionTypeCustom = 2;

        public int MotionType { get; set; }

        public string Word { get; set; } = "";

        public string BuiltInAnimationClipName { get; set; } = "";

        public string CustomMotionClipName { get; set; } = "";

        public string AccessoryName { get; set; } = "";

        public float DurationWhenOnlyBlendShape { get; set; } = 3.0f;

        /// <summary>
        /// NOTE: ブレンドシェイプは「1個も適用しない」か「(リップシンクだけ例外だけど基本)全部適用する」のいずれかになる点に留意
        /// </summary>
        public bool UseBlendShape { get; set; }

        /// <summary>
        /// ブレンドシェイプをアニメーション終了後もそのままの値にするかどうか
        /// </summary>
        public bool HoldBlendShape { get; set; }

        /// <summary>
        /// ブレンドシェイプアニメーションを口のリップシンクアニメーションで上書きしてもよいかどうか
        /// </summary>
        public bool PreferLipSync { get; set; }

        /// <summary>
        /// VRM規格で決まっている最小限のブレンドシェイプ一覧
        /// </summary>
        public Dictionary<string, int> BlendShapeValues { get; set; } = new Dictionary<string, int>();

        /// <summary>
        /// VRM規格では決まっていないが、ユーザーが読み込んだVRMに含まれていたブレンドシェイプの一覧
        /// </summary>
        public List<BlendShapePairItem> ExtraBlendShapeValues { get; set; } = new List<BlendShapePairItem>();

        /// <summary>この要素をJSONにシリアライズしたものを取得します。</summary>
        /// <returns></returns>
        public string ToJson()
        {
            var serializer = new JsonSerializer();
            var sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            {
                serializer.Serialize(writer, this);
            }
            return sb.ToString();
        }

        /// <summary>デフォルトの簡単な設定からなる動作リクエストを生成します。</summary>
        /// <returns></returns>
        public static MotionRequest GetDefault()
        {
            var result = new MotionRequest()
            {
                MotionType = MotionTypeNone,
                Word = "name",
                UseBlendShape = true,
                HoldBlendShape = false,
                DurationWhenOnlyBlendShape = 3.0f,
            };
            result.BlendShapeValues["Joy"] = 100;

            return result;
        }

        public static MotionRequest[] GetDefaultMotionRequestSet()
        {
            var result = new MotionRequest[]
            {
                new MotionRequest()
                {
                    MotionType = MotionTypeNone,
                    Word = "reset",
                    UseBlendShape = true,
                    HoldBlendShape = false,
                    DurationWhenOnlyBlendShape = 0.1f,
                },
                new MotionRequest()
                {
                    MotionType = MotionTypeNone,
                    Word = "joy",
                    UseBlendShape = true,
                    HoldBlendShape = false,
                    DurationWhenOnlyBlendShape = 3.0f,
                },
                new MotionRequest()
                {
                    MotionType = MotionTypeNone,
                    Word = "angry",
                    UseBlendShape = true,
                    HoldBlendShape = false,
                    DurationWhenOnlyBlendShape = 3.0f,
                },
                new MotionRequest()
                {
                    MotionType = MotionTypeNone,
                    Word = "sorrow",
                    UseBlendShape = true,
                    HoldBlendShape = false,
                    DurationWhenOnlyBlendShape = 3.0f,
                },
                new MotionRequest()
                {
                    MotionType = MotionTypeNone,
                    Word = "fun",
                    UseBlendShape = true,
                    HoldBlendShape = false,
                    DurationWhenOnlyBlendShape = 3.0f,
                },
                new MotionRequest()
                {
                    MotionType = MotionTypeBuiltInClip,
                    Word = "wave",
                    BuiltInAnimationClipName = "Wave",
                    UseBlendShape = false,
                    HoldBlendShape = false,
                    DurationWhenOnlyBlendShape = 3.0f,
                },
                new MotionRequest()
                {
                    MotionType = MotionTypeBuiltInClip,
                    Word = "good",
                    BuiltInAnimationClipName = "Good",
                    UseBlendShape = false,
                    HoldBlendShape = false,
                    DurationWhenOnlyBlendShape = 3.0f,
                },
                new MotionRequest()
                {
                    MotionType = MotionTypeBuiltInClip,
                    Word = "nodding",
                    BuiltInAnimationClipName = "Nod",
                    UseBlendShape = false,
                    HoldBlendShape = false,
                    DurationWhenOnlyBlendShape = 3.0f,
                },
                new MotionRequest()
                {
                    MotionType = MotionTypeBuiltInClip,
                    Word = "shaking",
                    BuiltInAnimationClipName = "Shake",
                    UseBlendShape = false,
                    HoldBlendShape = false,
                    DurationWhenOnlyBlendShape = 3.0f,
                },
                new MotionRequest()
                {
                    MotionType = MotionTypeBuiltInClip,
                    Word = "clap",
                    BuiltInAnimationClipName = "Clap",
                    UseBlendShape = false,
                    HoldBlendShape = false,
                    DurationWhenOnlyBlendShape = 3.0f,
                },
            };
            result[1].BlendShapeValues["Joy"] = 100;
            result[2].BlendShapeValues["Angry"] = 100;
            result[3].BlendShapeValues["Sorrow"] = 100;
            result[4].BlendShapeValues["Fun"] = 100;
            return result;
        }
    }

    /// <summary>
    /// NOTE: コレクションクラスを作ってるのはJSONのルートをオブジェクトにするため
    /// </summary>
    public class MotionRequestCollection
    {
        public MotionRequestCollection(MotionRequest[] requests)
        {
            Requests = requests;
        }

        public MotionRequest[] Requests { get; }

        public string ToJson()
        {
            var serializer = new JsonSerializer();
            var sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            {
                serializer.Serialize(writer, this);
            }
            return sb.ToString();
        }

        public static MotionRequestCollection LoadDefault()
            => new MotionRequestCollection(MotionRequest.GetDefaultMotionRequestSet());

        public static MotionRequestCollection FromJson(TextReader reader)
        {
            var serializer = new JsonSerializer();
            using (var jsonReader = new JsonTextReader(reader))
            {
                return
                    serializer.Deserialize<MotionRequestCollection>(jsonReader) ??
                    throw new InvalidOperationException();
            }
        }
    }

    public class BlendShapePairItem
    {
        public string? Name { get; set; }
        public int Value { get; set; }
    }
}
