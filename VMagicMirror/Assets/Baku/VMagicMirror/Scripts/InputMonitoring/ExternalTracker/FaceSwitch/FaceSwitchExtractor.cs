using System;
using System.Linq;

namespace Baku.VMagicMirror.ExternalTracker
{
    /// <summary>
    /// FaceSwitchで現在指定しているアイテムを表す要素で、キー的に扱えるようなもの
    /// </summary>
    public readonly struct ActiveFaceSwitchItem
    {
        public ActiveFaceSwitchItem(string clipName, bool keepLipSync, string accessoryName)
        {
            ClipName = clipName;
            KeepLipSync = keepLipSync;
            AccessoryName = accessoryName;
        }
        
        public string ClipName { get; }
        public bool KeepLipSync { get; }
        public string AccessoryName { get; }

        public bool IsEmpty =>
            string.IsNullOrEmpty(ClipName) && string.IsNullOrEmpty(AccessoryName);
        
        public static readonly ActiveFaceSwitchItem Empty = new("", false, "");

        public bool Equals(ActiveFaceSwitchItem other) =>
            ClipName == other.ClipName &&
            KeepLipSync == other.KeepLipSync && 
            AccessoryName == other.AccessoryName;
    }
    
    /// <summary>
    /// FaceSwitchの設定と現在の顔トラッキング情報から、FaceSwitchの出力値を決めてくれる地味に役立つクラス
    /// </summary>
    public class FaceSwitchExtractor
    {
        public ActiveFaceSwitchItem ActiveItem { get; private set; } = ActiveFaceSwitchItem.Empty;

        private string[] _avatarBlendShapeNames = Array.Empty<string>();
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
        private FaceSwitchItem[] _itemsToCheck = Array.Empty<FaceSwitchItem>();

        private void RefreshItemsToCheck()
        {
            if (Setting == null || AvatarBlendShapeNames == null)
            {
                _itemsToCheck = Array.Empty<FaceSwitchItem>();
                return;
            }

            //ブレンドシェイプかアクセサリーの適用内容が記載されているものだけ拾う。無効なものを残すとパフォーマンスが落ちるので無視。
            _itemsToCheck = Setting.items
                .Where(i => 
                    AvatarBlendShapeNames.Contains(i.ClipName) || !string.IsNullOrEmpty(i.accessoryName))
                .ToArray(); 
        }
        
        /// <summary>
        /// 顔情報を指定することで、適用すべきブレンドシェイプ名を更新します。
        /// </summary>
        /// <param name="source"></param>
        public void Update(IFaceTrackBlendShapes source)
        {
            for (int i = 0; i < _itemsToCheck.Length; i++)
            {
                if (ExtractSpecifiedBlendShape(source, _itemsToCheck[i].source) > _itemsToCheck[i].threshold * 0.01f)
                {
                    ActiveItem = new ActiveFaceSwitchItem(
                        _itemsToCheck[i].ClipName,
                        _itemsToCheck[i].keepLipSync,
                        _itemsToCheck[i].accessoryName
                    );
                    return;
                }
            }
            
            //一つも該当しない場合
            ActiveItem = ActiveFaceSwitchItem.Empty;
        }

        //NOTE: このキーはWPF側が決め打ちしてるやつです
        private static float ExtractSpecifiedBlendShape(IFaceTrackBlendShapes source, string key)
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
