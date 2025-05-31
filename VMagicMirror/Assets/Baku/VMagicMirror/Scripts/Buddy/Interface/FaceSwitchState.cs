namespace VMagicMirror.Buddy
{
    /// <summary>
    /// <see cref="IAvatarFacial.GetActiveFaceSwitch"/> の結果として取得できるような、
    /// Face Switch機能で検出したユーザーの表情です。
    /// </summary>
    public enum FaceSwitchState
    {
        /// <summary> 表情を特に検出していない </summary>
        None,
        /// <summary> 口元を笑顔にした </summary>
        MouthSmile,
        /// <summary> 目を細めた </summary>
        EyeSquint,
        /// <summary> 目を大きく見開いた </summary>
        EyeWide,
        /// <summary> 眉毛を大きく上げた </summary>
        BrowUp,
        /// <summary> 眉毛を大きく下げた </summary>
        BrowDown,
        /// <summary> 頬をふくらませた </summary>
        CheekPuff,
        /// <summary> 舌を出した </summary>
        TongueOut,
    }
}
