using Newtonsoft.Json.Linq;
using System;
using System.Collections.ObjectModel;
using System.Linq;

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

    /// <summary>
    /// ブレンドシェイプクリップ名を一覧保持するクラスです。
    /// </summary>
    public class ExternalTrackerBlendShapeNameStore
    {
        public ExternalTrackerBlendShapeNameStore()
        {
            BlendShapeNames = new ReadOnlyObservableCollection<string>(_blendShapeNames);
            var defaultNames = LoadDefaultNames();
            for (int i = 0; i < defaultNames.Length; i++)
            {
                _blendShapeNames.Add(defaultNames[i]);
            }
        }

        private readonly ObservableCollection<string> _blendShapeNames = new ObservableCollection<string>();
        /// <summary> UIに表示するのが妥当と考えられるブレンドシェイプクリップ名の一覧です。 </summary>
        public ReadOnlyObservableCollection<string> BlendShapeNames { get; }

        //Unityで読み込まれたキャラクターのブレンドシェイプ名の一覧です。
        //NOTE: この値は標準ブレンドシェイプ名を含んでいてもいなくてもOK。ただし現行動作では標準ブレンドシェイプ名は含まない。
        private string[] _avatarClipNames = new string[0];

        //設定ファイルから読み込んだ設定で使われていたブレンドシェイプ名の一覧。
        //NOTE: この値に標準ブレンドシェイプ名とそうでないのが混在することがあるが、それはOK
        private string[] _settingUsedNames = new string[0];

        /// <summary>
        /// ロードされたVRMの標準以外のブレンドシェイプ名を指定して、名前一覧を更新します。
        /// </summary>
        /// <param name="avatarBlendShapeNames"></param>
        public void Refresh(string[] avatarBlendShapeNames)
        {
            //なんとなく正格評価しておく(値コピーの方が安心なので…
            _avatarClipNames = avatarBlendShapeNames.ToArray();
            RefreshInternal();
        }

        /// <summary>
        /// ファイルからロードされたはずの設定を参照し、その中で使われているブレンドシェイプ名を参考にして名前一覧を更新します。
        /// </summary>
        /// <param name="currentSetting"></param>
        public void Refresh(ExternalTrackerFaceSwitchSetting currentSetting)
        {
            _settingUsedNames = currentSetting.Items
                .Select(i => i.ClipName)
                .ToArray();
            RefreshInternal();
        }

        private void RefreshInternal()
        {
            //理想の並び: デフォルトのやつ一覧、今ロードしたVRMにある名前一覧、(今ロードしたVRMにはないけど)設定で使ってる名前一覧
            var newNames = LoadDefaultNames().ToList();
            int defaultSetLength = newNames.Count;
            foreach (var nameInModel in _avatarClipNames)
            {
                if (!newNames.Contains(nameInModel))
                {
                    newNames.Add(nameInModel);
                }
            }

            foreach (var nameInSetting in _settingUsedNames)
            {
                if (!newNames.Contains(nameInSetting))
                {
                    newNames.Add(nameInSetting);
                }
            }

            var newNameArray = newNames.ToArray();

            //NOTE: ここポイントで、既存要素は消さないよう慎重に並べ替えます(消すとOC<T>の怒りを買ってUI側の要素選択に悪影響が出たりするので…)
            for (int i = defaultSetLength; i < newNameArray.Length; i++)
            {
                if (_blendShapeNames.Contains(newNameArray[i]))
                {
                    int currentIndex = _blendShapeNames.IndexOf(newNameArray[i]);
                    if (currentIndex != i)
                    {
                        //もう入ってる値だが、場所を入れ替えたいケース
                        _blendShapeNames.Move(currentIndex, i);
                    }
                }
                else
                {
                    //そもそも入ってないケース
                    _blendShapeNames.Insert(i, newNameArray[i]);
                }
            }

            //OC<T>側のほうが配列が長い場合、ハミ出た分は余計なやつなので消しちゃってOK
            while (_blendShapeNames.Count > newNameArray.Length)
            {
                _blendShapeNames.RemoveAt(newNameArray.Length);
            }
        }

        private string[] LoadDefaultNames()
        {
            return new string[]
            {
                //「なし」があるのが大事。これによって、条件に合致しても何のブレンドシェイプを起動しない！という事ができる。
                "",
                "Joy",
                "Angry",
                "Sorrow",
                "Fun",

                "A",
                "I",
                "U",
                "E",
                "O",

                "Neutral",
                "Blink",
                "Blink_L",
                "Blink_R",

                "LookUp",
                "LookDown",
                "LookLeft",
                "LookRight",
            };
        }
    }
}
