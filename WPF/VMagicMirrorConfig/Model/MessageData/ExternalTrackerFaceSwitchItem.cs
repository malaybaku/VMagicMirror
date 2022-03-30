using Newtonsoft.Json.Linq;
using System;
using System.Collections.ObjectModel;
namespace Baku.VMagicMirrorConfig
{
    public static class FaceSwitchKeys
    {
        public static readonly string MouthSmile = "mouthSmile";
        public static readonly string EyeSquint = "eyeSquint";
        public static readonly string EyeWide = "eyeWide";
        public static readonly string BrowUp = "browUp";
        public static readonly string BrowDown = "browDown";
        public static readonly string TongueOut = "tongueOut";
        public static readonly string CheekPuff = "cheekPuff";

    }

    /// <summary>
    /// BlendShapeのうち<see cref="SourceName"/>の値が<see cref="ThresholdPercent"/>以上なら
    /// <see cref="ClipName"/>ブレンドシェイプを起動する、みたいな設定を保持するやつ。
    /// </summary>
    /// <remarks>
    /// <see cref="SourceName"/>はmouthSmileとかそういう値で、WPF側で決め打ちする。他は可変 
    /// </remarks>
    public class ExternalTrackerFaceSwitchItem
    {
        public ExternalTrackerFaceSwitchItem(string sourceName)
        {
            SourceName = sourceName;
        }

        public string SourceName { get; }

        /// <summary> じゅうぶんハッキリと表情が動いた、と判断できるしきい値 </summary>
        public int ThresholdPercent { get; set; } = 100;

        /// <summary> 条件に合致したとき動かすBlendShapeClipの名前 </summary>
        public string ClipName { get; set; } = "";

        /// <summary> BlendShapeを適用しているときもリップシンクを継続してよいかどうか </summary>
        /// <remarks>
        /// VRMのミニマム仕様的には全部falseにすべきで、特別にセットアップしたモデルでのみtrueにすることができる。
        /// </remarks>
        public bool KeepLipSync { get; set; } = false;

        /// <summary>
        /// 条件を満たしているときのみ表示するアクセサリーがある場合、その名前。何もしない場合は空文字。
        /// </summary>
        public string AccessoryName { get; set; } = "";
    }

    /// <summary> 表情スイッチアイテムの一覧です。 </summary>
    public class ExternalTrackerFaceSwitchSetting
    {
        private ExternalTrackerFaceSwitchSetting(ExternalTrackerFaceSwitchItem[] items)
        {
            Items = items;
        }

        public ExternalTrackerFaceSwitchItem[] Items { get; }

        /// <summary>
        /// 基本的なパラメータ設定で初期化されたアイテム一覧を生成します。
        /// 初回起動時や、設定をリセットするときに使います。
        /// </summary>
        /// <returns></returns>
        public static ExternalTrackerFaceSwitchSetting LoadDefault()
        {
            var items = new ExternalTrackerFaceSwitchItem[]
            {
                //笑顔
                new ExternalTrackerFaceSwitchItem(FaceSwitchKeys.MouthSmile)
                {
                    ThresholdPercent = 70,
                    ClipName = "Joy",
                },
                //細め (ジト目が作れるぞ！)
                new ExternalTrackerFaceSwitchItem(FaceSwitchKeys.EyeSquint)
                {
                    ThresholdPercent = 70,
                    ClipName = "",
                },
                //細め (ジト目が作れるぞ！)
                new ExternalTrackerFaceSwitchItem(FaceSwitchKeys.EyeWide)
                {
                    ThresholdPercent = 70,
                    ClipName = "",
                },
                //驚き
                new ExternalTrackerFaceSwitchItem(FaceSwitchKeys.BrowUp)
                {
                    ThresholdPercent = 70,
                    ClipName = "",
                },
                //悲しい or 怒り
                new ExternalTrackerFaceSwitchItem(FaceSwitchKeys.BrowDown)
                {
                    ThresholdPercent = 70,
                    ClipName = "Sorrow",
                },
                //頬を膨らます
                new ExternalTrackerFaceSwitchItem(FaceSwitchKeys.CheekPuff)
                {
                    ThresholdPercent = 70,
                    ClipName = "",
                },
                //舌出し
                new ExternalTrackerFaceSwitchItem(FaceSwitchKeys.TongueOut)
                {
                    ThresholdPercent = 70,
                    ClipName = "",
                }
            };
            return new ExternalTrackerFaceSwitchSetting(items);
        }

        /// <summary>
        /// JSON文字列からFaceSwitchの設定を読み込みます。
        /// このメソッドはパースやデシリアライズ時に例外スローする場合があることに注意して下さい。
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static ExternalTrackerFaceSwitchSetting FromJson(string json)
        {
            //NOTE: 初起動時とかは積極的にここを通るはず
            if (string.IsNullOrEmpty(json))
            {
                throw new ArgumentException(nameof(json));
            }

            var result = LoadDefault();
            var jobj = JObject.Parse(json);
            if (jobj["items"] is JArray items && items.Count == result.Items.Length)
            {
                //NOTE: 読み込むときにsourceを見ないが、コレは「どうせWPFでしか管理してないし…」というナメた態度です
                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i]["threshold"] is JValue threshold &&
                        items[i]["clipName"] is JValue clipName &&
                        items[i]["keepLipSync"] is JValue keepLipSync
                        )
                    {
                        result.Items[i].ThresholdPercent = (int)threshold;
                        result.Items[i].ClipName = (string?)clipName ?? "";
                        result.Items[i].KeepLipSync = (bool)keepLipSync;
                    }

                    if (items[i]["accessoryName"] is JValue accessoryName)
                    {
                        result.Items[i].AccessoryName = (string?)accessoryName ?? "";
                    }
                }
            }
            return result;
        }

        public string ToJson()
        {

            var jarr = new JArray();
            for (int i = 0; i < Items.Length; i++)
            {
                jarr.Add(new JObject()
                {
                    ["source"] = Items[i].SourceName,
                    ["threshold"] = Items[i].ThresholdPercent,
                    ["clipName"] = Items[i].ClipName,
                    ["keepLipSync"] = Items[i].KeepLipSync,
                    ["accessoryName"] = Items[i].AccessoryName,
                });
            }

            return new JObject()
            {
                ["items"] = jarr
            }.ToString(Newtonsoft.Json.Formatting.None);

        }
    }
}
