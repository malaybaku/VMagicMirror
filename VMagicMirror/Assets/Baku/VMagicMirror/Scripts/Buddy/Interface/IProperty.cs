using System;

namespace VMagicMirror.Buddy
{
    /// <summary>
    /// マニフェストで定義されたプロパティ値を取得できるAPIです。
    /// </summary>
    /// <remarks>
    /// サブキャラの設定として、表示位置や仕草のスピードなど、エンドユーザーが編集や調整を行うプロパティを公開し、このAPIでその調整後の値を取得できます。
    /// manifest.json にプロパティを定義する方法については <see href="xref:doc-buddy-manifest">サブキャラの基本設定を定義する</see> を参照して下さい。
    /// </remarks>
    public interface IProperty
    {
        /// <summary>
        /// action値として定義したプロパティのボタン入力が行われたときに発火します。
        /// 引数はマニフェストで定義した name の値になります。
        /// </summary>
        event Action<string> ActionRequested;
        
        /// <summary>
        /// bool値として定義したプロパティの現在値を取得します。
        /// </summary>
        /// <param name="key">プロパティ名</param>
        /// <returns>プロパティの現在値</returns>
        /// <remarks>
        /// 指定したキーがマニフェスト上で定義されていない場合、この関数は <c>false</c> を返します。
        /// </remarks>
        bool GetBool(string key);

        /// <summary>
        /// int値として定義したプロパティの現在値を取得します。
        /// </summary>
        /// <param name="key">プロパティ名</param>
        /// <returns>プロパティの現在値</returns>
        /// <remarks>
        /// 指定したキーがマニフェスト上で定義されていない場合、この関数は <c>0</c> を返します。
        /// </remarks>
        int GetInt(string key);
        
        /// <summary>
        /// bool値として定義したプロパティの現在値を取得します。
        /// </summary>
        /// <param name="key">プロパティ名</param>
        /// <returns>プロパティの現在値</returns>
        /// <remarks>
        /// 指定したキーがマニフェスト上で定義されていない場合、この関数は <c>0</c> を返します。
        /// </remarks>
        float GetFloat(string key);

        /// <summary>
        /// string値として定義したプロパティの現在値を取得します。
        /// </summary>
        /// <param name="key">プロパティ名</param>
        /// <returns>プロパティの現在値</returns>
        /// <remarks>
        /// 指定したキーがマニフェスト上で定義されていない場合、この関数は空文字列を返します。
        /// </remarks>
        string GetString(string key);

        /// <summary>
        /// <see cref="Vector2"/> の値として定義したプロパティの現在値を取得します。
        /// </summary>
        /// <param name="key">プロパティ名</param>
        /// <returns>プロパティの現在値</returns>
        /// <remarks>
        /// 指定したキーがマニフェスト上で定義されていない場合、この関数は <see cref="Vector2.zero"/> を返します。
        /// </remarks>
        Vector2 GetVector2(string key);

        /// <summary>
        /// <see cref="Vector3"/> の値として定義したプロパティの現在値を取得します。
        /// </summary>
        /// <param name="key">プロパティ名</param>
        /// <returns>プロパティの現在値</returns>
        /// <remarks>
        /// 指定したキーがマニフェスト上で定義されていない場合、この関数は <see cref="Vector3.zero"/> を返します。
        /// </remarks>
        Vector3 GetVector3(string key);

        /// <summary>
        /// <see cref="Quaternion"/> の値として定義したプロパティの現在値を取得します。
        /// </summary>
        /// <param name="key">プロパティ名</param>
        /// <returns>プロパティの現在値</returns>
        /// <remarks>
        /// 指定したキーがマニフェスト上で定義されていない場合、この関数は <see cref="Quaternion.identity"/> を返します。
        /// </remarks>
        Quaternion GetQuaternion(string key);
    }
}
