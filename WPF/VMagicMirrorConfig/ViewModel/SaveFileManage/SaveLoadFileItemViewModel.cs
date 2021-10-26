using System;

namespace Baku.VMagicMirrorConfig
{
    public class SaveLoadFileItemViewModel
    {
        internal SaveLoadFileItemViewModel(bool isLoadMode, bool isCurrent, SettingFileOverview model, SaveLoadDataViewModel parent)
        {
            IsLoadMode = isLoadMode;
            IsCurrent = isCurrent;
            Index = model.Index;
            IsExist = model.Exist;
            ModelName = model.ModelName;
            LastUpdatedDate = model.LastUpdateTime;

            SelectThisCommand = new ActionCommand(async () =>
            {
                if (isLoadMode)
                {
                    await parent.ExecuteLoad(Index);
                }
                else
                {
                    await parent.ExecuteSave(Index);
                }
            });
        }

        //ロードは存在するファイルじゃないとダメ、セーブはオートセーブ以外ならOK。
        public bool CanChooseThisItem =>
            (IsLoadMode && IsExist) ||
            (!IsLoadMode && Index != 0);

        public bool IsLoadMode { get; }
        /// <summary>今ロードしてる設定がこのファイルに由来しているかどうかを取得します。</summary>
        public bool IsCurrent { get; }


        public int Index { get; }
        public string IndexString => Index == 0 ? "No.0 (Auto Save)" : $"No.{Index}";

        /// <summary>
        /// そもそもファイルあるんだっけ、というのを取得します。
        /// </summary>
        public bool IsExist { get; }

        /// <summary>
        /// モデルがロードされている場合、そのモデル名。ロードされてない場合は空文字列
        /// </summary>
        public string ModelName { get; }

        //NOTE: VRoid Hubでモデル名とサブ名称が両方あるものについてはサブ名を分けよう、という話
        //Unityからくる文字列でサブ名称がついてるものの例:
        //  VRoid Hub: ModelName\tアクセサリーつき

        /// <summary>
        /// 'VRM File: 'または'VRoid Hub: 'で始まるモデル名のメイン表記の文字列を取得します。
        /// </summary>
        public string ModelNameWithPrefix =>
            string.IsNullOrEmpty(ModelName) ? "VRM File : - " :
            ModelName.Split('\t')[0];

        /// <summary>
        /// モデルにサブ名称があればそれを取得し、なければ空文字列
        /// </summary>
        public string ModelNameSubTitle
        {
            get
            {
                if (string.IsNullOrEmpty(ModelName))
                {
                    return "";
                }
                var split = ModelName.Split('\t');
                return split.Length > 1 ? split[1] : "";
            }
        }

        public DateTime LastUpdatedDate { get; }
        public string LastUpdatedDateOrDash => IsExist ? $"Date: {LastUpdatedDate:yyyy/MM/dd HH:mm}" : "Date: -";

        public ActionCommand SelectThisCommand { get; }

    }
}
