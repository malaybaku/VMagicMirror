namespace VMagicMirror.Buddy
{
    //TODO: IGUIをちゃんと復帰させる段階で #pragma warning disable の行を削除する
#pragma warning disable CS1591

    // NOTE: WebViewベースにするかUIで頑張るかでフォントとかガワの様子が変わってくる
    /// <exclude />
    public interface IGui
    {
        IGuiArea CreateGuiArea();
    }

    // TODO: 設計にも仕様がかなり引っ張られるのでもうちょい考える
    /// <exclude />
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
