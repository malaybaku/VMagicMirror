namespace Baku.VMagicMirror.Buddy.Api.Interface
{
    /// <summary>
    /// ユーザーが調整したプロパティを取得できるAPI。
    /// サブキャラの表示位置やスピード、ユーザー名の変更などに対応する
    /// </summary>
    public interface IPropertyApi
    {
        bool GetBool(string key);
        int GetInt(string key);
        float GetFloat(string key);
        string GetString(string key);
        Vector2 GetVector2(string key);
        Vector3 GetVector3(string key);
        Quaternion GetQuaternion(string key);
    }
}
