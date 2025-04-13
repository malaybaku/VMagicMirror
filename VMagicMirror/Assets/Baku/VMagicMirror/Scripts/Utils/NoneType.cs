using System;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// UniRxのUnit型と同じでvoid相当を表す型。
    /// 戻り値がvoidになる関数を式ライクに扱いたい場合に任意で導入して使う。
    /// </summary>
    public readonly struct None : IEquatable<None>
    {
        public static None Value { get; } = new();
        
        public static bool operator ==(None first, None second) => true;
        public static bool operator !=(None first, None second) => false;
        public bool Equals(None other) => true;
        public override bool Equals(object obj) => obj is None;
        public override int GetHashCode() => 0;
        public override string ToString() => "";
    }
}
