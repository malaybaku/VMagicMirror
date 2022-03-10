namespace Baku.VMagicMirrorConfig
{
    public enum AccessoryAttachTarget : int
    {
        Head = 0,
        Neck = 1,
        RightHand = 2,
        LeftHand = 3,
        Chest = 4,
        Waist = 5,
        World = 6,
    }

    public class AccessorySetting : SettingEntityBase
    {
        /// <summary>
        /// アイテム一覧をJSONシリアライズしたもの
        /// </summary>
        public string SerializedSetting { get; set; } = "";
    }

    //NOTE: コレはAccessoryItemsと違い、通信でのみ使う
    public class AccessoryResetTargetItems
    {
        public string[] FileIds { get; set; } = new string[0];
    }

    public class AccessoryItems
    {
        public AccessoryItemSetting[] Items { get; set; } = new AccessoryItemSetting[0];
    }

    //TODO: 配列でデータ保持したい + シリアライズの瞬間だけjsonにしたい
    public class AccessoryItemSetting
    {
        public const char FolderIdSuffixChar = '>';

        //NOTE: 他のプロパティとは異なりキーのように用いられる。ユーザーは編集できない
        public string FileId { get; set; } = "";

        public string Name { get; set; } = "";
        public bool IsVisible { get; set; }
        //TODO: コレは整数値でシリアライズしてもらえるかが要監視
        public AccessoryAttachTarget AttachTarget { get; set; }

        public Vector3 Position { get; set; }
        //EulerAngleが入る。
        //Unity側で編集するとキレイでない値が入る事もあるが、それは許可する。
        //ビルボードモードになるとZ軸回転のみが反映され、XYは無視される
        public Vector3 Rotation { get; set; }
        //NOTE: いちおうVector3にするが、UIで3軸別々に編集することは直近では期待してない

        public Vector3 Scale { get; set; } = Vector3.One();

        //NOTE: 画像系のオブジェクトでのみ使える、カメラ前面にビルボードライクに表示されるモード。
        //TODO: この状態ではscaleの意味がちょっと変わるかもしれんので要注意…
        public bool UseBillboardMode { get; set; }

        // NOTE: 連番画像でのみ意味がある。Unity側は5 ~ 30の値の範囲が飛んでくることを期待している
        public int FramePerSecond { get; set; } = 15;
    }
}
