using System.Linq;

namespace Baku.VMagicMirror.ExternalTracker
{
    /// <summary>
    /// FaceSwitchの設定と現在の顔トラッキング情報から、FaceSwitchの出力値を決めてくれる地味に役立つクラス
    /// </summary>
    public class FaceSwitchExtractor
    {


        /// <summary> FaceSwitch的にはこの値だと嬉しいな～というブレンドシェイプ名 </summary>
        public string ClipName { get; private set; } = "";

        /// <summary> FaceSwitch的にリップシンクを続行する/しないの判定値 </summary>
        public bool KeepLipSync { get; private set; } = false;

        private string[] _avatarBlendShapeNames = new string[0];
        /// <summary> 現在ロードされているアバターの全ブレンドシェイプ名 </summary>
        public string[] AvatarBlendShapeNames
        {
            get => _avatarBlendShapeNames;
            set
            {
                _avatarBlendShapeNames = value;
                RefreshItemsToCheck();
            }
        } 
        
        //NOTE: WPF側のデフォルト表示と同じ構成の初期状態を入れておく
        private FaceSwitchSettings _setting = FaceSwitchSettings.LoadDefault();
        /// <summary> 設定ファイルから読み込まれて送信された設定 </summary>
        public FaceSwitchSettings Setting
        {
            get => _setting;
            set
            {
                _setting = value;
                RefreshItemsToCheck();
            }
        }


        //ロードされたアバターと設定を突き合わせた結果得られる、確認すべき条件セットの一覧
        private FaceSwitchItem[] _itemsToCheck = new FaceSwitchItem[0];

        private void RefreshItemsToCheck()
        {
            if (Setting == null || AvatarBlendShapeNames == null)
            {
                _itemsToCheck = new FaceSwitchItem[0];
                return;
            }

            //拾うモノ: 設定に乗っており、かつブレンドシェイプが実際に今のアバターに存在するもの。
            _itemsToCheck = Setting.items
                .Where(i => AvatarBlendShapeNames.Contains(i.clipName))
                .ToArray(); 
        }
        
        /// <summary>
        /// 顔情報を指定することで、適用すべきブレンドシェイプ名を更新します。
        /// </summary>
        /// <param name="source"></param>
        public void Update(IFaceTrackSource source)
        {
            // float smile = 0.5f * (source.Mouth.LeftSmile + source.Mouth.RightSmile);

            for (int i = 0; i < _itemsToCheck.Length; i++)
            {
                if (ExtractSpecifiedBlendShape(source, _itemsToCheck[i].source) > _itemsToCheck[i].threshold * 0.01f)
                {
                    ClipName = _itemsToCheck[i].clipName;
                    KeepLipSync = _itemsToCheck[i].keepLipSync;
                    return;
                }
            }
            
            //一つも該当しない場合
            ClipName = "";
            KeepLipSync = false;
        }

        //NOTE: このキーはWPF側が決め打ちしてるやつです
        private static float ExtractSpecifiedBlendShape(IFaceTrackSource source, string key)
        {
            switch (key)
            {
            case FaceSwitchKeys.MouthSmile:
                return 0.5f * (source.Mouth.LeftSmile + source.Mouth.RightSmile);
            case FaceSwitchKeys.EyeSquint:
                return 0.5f * (source.Eye.LeftSquint + source.Eye.RightSquint);
            case FaceSwitchKeys.EyeWide:
                return 0.5f * (source.Eye.LeftWide + source.Eye.RightWide);
            case FaceSwitchKeys.BrowUp:
                return 0.333f * (source.Brow.InnerUp + source.Brow.LeftOuterUp + source.Brow.RightOuterUp);
            case FaceSwitchKeys.BrowDown:
                return 0.5f * (source.Brow.LeftDown + source.Brow.RightDown);
            case FaceSwitchKeys.CheekPuff:
                return source.Cheek.Puff;
            case FaceSwitchKeys.TongueOut:
                return source.Tongue.TongueOut;
            default:
                return 0;
            }
        }
    }
}
