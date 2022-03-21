namespace Baku.VMagicMirror.ViewModelsConfig
{
    public class BlendShapeItemViewModel : ViewModelBase
    {
        /// <summary>
        /// クリップ名、親要素、値、およびこれが規格で定まった
        /// 最低限のブレンドシェイプか否かを指定してインスタンスを初期化します。
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="isUsedWithThisAvatar"></param>
        public BlendShapeItemViewModel(WordToMotionItemViewModel parent, string name, int value, bool isUsedWithThisAvatar)
        {
            BlendShapeName = name;
            ValuePercentage = value;
            IsUsedWithThisAvatar = isUsedWithThisAvatar;
            ForgetThisClipCommand = new ActionCommand(() => parent.RequestForgetClip(this));
        }

        /// <summary>
        /// VRM規格で定まっているブレンドシェイプについて、クリップ名と親要素を指定してインスタンスを初期化します。
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public BlendShapeItemViewModel(WordToMotionItemViewModel parent, string name, int value) :
            this(parent, name, value, true)
        {
        }

        public string BlendShapeName { get; }

        private int _valuePercentage = 0;
        public int ValuePercentage
        {
            get => _valuePercentage;
            set => SetValue(ref _valuePercentage, value);
        }

        private bool _isUsedWithThisAvatar = true;
        /// <summary>
        /// このクリップが、現在ロードされているアバターに含まれるかどうかを取得、設定します。
        /// 基本ブレンドシェイプクリップについては常にtrueになるよう、親クラス側が制御します。
        /// また基本以外のブレンドシェイプクリップについては、
        /// アバターが一度もロードされていない場合はtrueでよいものとします。
        /// </summary>
        public bool IsUsedWithThisAvatar
        {
            get => _isUsedWithThisAvatar;
            set => SetValue(ref _isUsedWithThisAvatar, value);
        }

        public ActionCommand ForgetThisClipCommand { get; }
    }
}
