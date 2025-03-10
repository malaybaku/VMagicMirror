namespace VMagicMirror.Buddy
{
    // NOTE: WebViewベースにするかUIで頑張るかでフォントとかガワの様子が変わってくる
    public interface IGui
    {
        IGuiArea CreateGuiArea();
    }

    // NOTE:
    // - これはクラスなので、
    // - 左右=[0, 1], 下上=[0, 1]で画面サイズ比ベースの指定になる(ので、Sizeをキレイに指定するのはちょっと難しい)
    public interface IGuiArea
    {
        ITransform2D Transform { get; }
        Vector2 Position { get; set; }
        Vector2 Size { get; set; }
        Vector2 Pivot { get; set; }

        void SetActive(bool active);
        void ShowText(string content, bool immediate);
    }
}
