using System;

namespace Baku.VMagicMirrorConfig
{
    //メッセージボックスで表示するテキストの言語別対応。
    //リソースに書くほどでもないのでベタに書く
    class MessageIndication
    {
        private MessageIndication(string title, string content)
        {
            Title = title;
            Content = content;
        }

        public string Title { get; }
        public string Content { get; }

        private static MessageIndication Load(string keySuffix) => new MessageIndication(
            LocalizedString.GetString("DialogTitle_" + keySuffix),
            LocalizedString.GetString("DialogMessage_" + keySuffix)
            );

        public static MessageIndication LoadVrmConfirmation() => Load("LoadLocalVrm");
        public static MessageIndication ResetSettingConfirmation() => Load("ResetAllSetting");
        public static MessageIndication ResetSingleCategoryConfirmation() => Load("ResetCategorySetting");
        public static MessageIndication ShowVRoidSdkUi() => Load("ShowVRoidSdkUi");
        public static MessageIndication ShowLoadingPreviousVRoid() => Load("LoadPreviousVRoidModel");
        public static MessageIndication ShowLoadingSavedVRoidModel() => Load("LoadSavedVRoidModel");

        /// <summary>
        /// NOTE: Contentのほうがフォーマット文字列なのでstring.Formatで現在のバージョン値を指定すること
        /// e.g. string.Format(res.Content, appVersion.ToString())
        /// </summary>
        /// <param name="languageName"></param>
        /// <returns></returns>
        public static MessageIndication AlreadyLatestVersion() => Load("AlreadyLatestVersion");

        /// <summary>
        /// NOTE: Contentのほうがフォーマット文字列なのでstring.Formatで消すアイテムの名前を指定して完成させること！
        /// string.Format(res.Content, "itemName")
        /// みたいな。
        /// </summary>
        /// <param name="languageName"></param>
        /// <returns></returns>
        public static MessageIndication ErrorLoadSetting() => Load("LoadSettingFileError");

        /// <summary>
        /// NOTE: Contentのほうがフォーマット文字列なのでstring.Formatで消すアイテムの名前を指定して完成させること！
        /// ex: string.Format(res.Content, "itemName")
        /// </summary>
        /// <param name="languageName"></param>
        /// <returns></returns>
        public static MessageIndication DeleteWordToMotionItem() => Load("DeleteWtmItem");

        /// <summary>
        /// NOTE: Contentがフォーマット文字列なため、削除予定のブレンドシェイプ名を指定して完成させること
        /// ex: string.Format(res.Content, "clipName")
        /// </summary>
        /// <param name="languageName"></param>
        /// <returns></returns>
        public static MessageIndication ForgetBlendShapeClip() => Load("ForgetBlendShapeInfo");

        /// <summary>
        /// 無効なIPアドレスを指定したときに怒る文言です。
        /// </summary>
        /// <param name="languageName"></param>
        /// <returns></returns>
        public static MessageIndication InvalidIpAddress() => Load("InvalidIpAddress");

        /// <summary>
        /// モデルでExTrackerのパーフェクトシンクに必要なブレンドシェイプクリップが未定義だったときのエラーのヘッダー部
        /// </summary>
        /// <param name="languageName"></param>
        /// <returns></returns>
        public static MessageIndication ExTrackerMissingBlendShapeNames() => Load("ExTrackerMissingBlendShape");

        /// <summary>
        /// webカメラのトラッキングを使うために外部トラッキングを切ろうとしてる人向けの確認ダイアログ
        /// </summary>
        /// <param name="languageName"></param>
        /// <returns></returns>
        public static MessageIndication ExTrackerCheckTurnOff() => Load("ExTrackerCheckTurnOff");

        /// <summary>
        /// 設定ファイルをのスロットに保存するときの確認。Content側は番号入れるぶんの{0}のプレースホルダーがあるので注意。
        /// </summary>
        /// <returns></returns>
        public static MessageIndication ConfirmSettingFileSave() => Load("ConfirmSettingFileSave");

        /// <summary>
        /// 設定ファイルをスロットからロードするときの確認。Content側は番号入れるぶんの{0}のプレースホルダーがあるので注意。
        /// </summary>
        /// <returns></returns>
        public static MessageIndication ConfirmSettingFileLoad() => Load("ConfirmSettingFileLoad");

        /// <summary>
        /// セーブ/ロード中に詳細設定ウィンドウを開いてたらガードするための文言
        /// </summary>
        /// <returns></returns>
        public static MessageIndication GuardSettingWindowDuringSaveLoad() => Load("GuardSettingWindowDuringSaveLoad");

        /// <summary>
        /// オートメーション機能を有効にしたい人向けの確認ダイアログ
        /// </summary>
        /// <returns></returns>
        public static MessageIndication EnableAutomation() => Load("EnableAutomation");

        /// <summary>
        /// オートメーション機能を無効にしたい人向けの確認ダイアログ
        /// </summary>
        /// <returns></returns>
        public static MessageIndication DisableAutomation() => Load("DisableAutomation");

        /// <summary>
        /// モデルのボーン設定がペンに対応していない、という情報を提供するダイアログ
        /// ※snackbarに本文だけ出せるような内容でもあります
        /// </summary>
        /// <returns></returns>
        public static MessageIndication WarnInfoAboutPenUnavaiable() => Load("PenUnavailable");

        /// <summary>
        /// ユーザーがVRM 1.0をロードさせようとした場合に表示する、
        /// VRM1.0が未対応であることを伝えるダイアログ
        /// </summary>
        /// <returns></returns>
        public static MessageIndication Vrm10NotSupported() => Load("Vrm10NotSupported");

        /// <summary>
        /// VMCPの設定をアイテム1つぶん削除するときの確認ダイアログ
        /// </summary>
        /// <returns></returns>
        public static MessageIndication ResetVmcpSourceItemConfirmation() => Load("ResetVmcpSourceItemConfirm");


    }
}
