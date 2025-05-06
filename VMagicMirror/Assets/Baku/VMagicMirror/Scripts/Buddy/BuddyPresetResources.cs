using System;
using System.Collections.Generic;
using UnityEngine;

namespace Baku.VMagicMirror.Buddy
{
    public readonly struct BuddyDefaultSprites
    {
        public Texture2D DefaultTexture { get; }
        public Texture2D BlinkTexture { get; }
        public Texture2D MouthOpenTexture { get; }
        public Texture2D BlinkMouthOpenTexture { get; }
        
        public BuddyDefaultSprites(
            Texture2D defaultSprite,
            Texture2D blinkTexture,
            Texture2D mouthOpenSprite,
            Texture2D blinkMouthOpenTexture)
        {
            DefaultTexture = defaultSprite;
            BlinkTexture = blinkTexture;
            MouthOpenTexture = mouthOpenSprite;
            BlinkMouthOpenTexture = blinkMouthOpenTexture;
        }
    }
    
    /// <summary>
    /// アプリケーション本体に組み込むサブキャラのアセット情報を保持するクラス
    /// </summary>
    /// <remarks>
    /// サブキャラ非使用時にもメモリの乗る前提の作りになっているが、もっとLazyにロードする方向に寄せてもよい
    /// </remarks>
    [CreateAssetMenu(fileName = "BuddyPresetResources", menuName = "Baku/VMagicMirror/BuddyPresetResources")]
    public class BuddyPresetResources : ScriptableObject
    {
        [SerializeField] private BuddyPresetTexture2DAsset[] textures;
        [SerializeField] private BuddyPresetVrmAsset[] vrms;

        public BuddyPresetTexture2DAsset[] Textures => textures;
        public BuddyPresetVrmAsset[] Vrms => vrms;

        // NOTE: Lazyにテクスチャを生成する。一度生成したらアプリの生存期間中は使い回す
        private readonly Dictionary<string, Texture2D> _textures = new();
        
        public bool TryGetTexture(string key, out Texture2D result)
        {
            if (_textures.TryGetValue(key, out result))
            {
                return true;
            }
            
            foreach (var item in textures)
            {
                if (!item.Name.Equals(key, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                
                try
                {
                    // NOTE: cloneしたレポジトリでは TextAsset が missingになってるのが期待値
                    if (item.TextureBinary == null)
                    {
                        result = null;
                        return false;
                    }

                    var texture = new Texture2D(16, 16);
                    texture.LoadImage(item.TextureBinary);
                    texture.Apply();
                    _textures[key] = texture;
                    result = texture;
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    result = null;
                    return false;
                }
            }

            result = null;
            return false;
        }
        
        public bool TryGetVrm(string key, out byte[] result)
        {
            foreach (var vrm in vrms)
            {
                if (vrm.Name.Equals(key, StringComparison.OrdinalIgnoreCase))
                {
                    // NOTE: cloneしたレポジトリでは TextAsset が missingになってるのが期待値
                    if (vrm.VrmBinary == null)
                    {
                        result = null;
                        return false;
                    }

                    result = vrm.VrmBinary;
                    return true;
                }
            }

            result = null;
            return false;
        }

        // NOTE: 本来はこれに対してもキーを指定すべきだが、v4.0.0ではデフォルトスプライトを一種類しか提供しないので決め打ちする
        public BuddyDefaultSprites GetDefaultSprites()
        {
            if (TryGetTexture("A_default", out var defaultSprite) &&
                TryGetTexture("A_blink", out var blinkSprite) &&
                TryGetTexture("A_mouthOpen", out var mouthOpenSprite) &&
                TryGetTexture("A_blinkMouthOpen", out var blinkMouthOpenSprite))
            {
                return new BuddyDefaultSprites(
                    defaultSprite,
                    blinkSprite,
                    mouthOpenSprite,
                    blinkMouthOpenSprite);
            }
            else
            {
                throw new InvalidOperationException("Default sprites not found");
            }
        }
    }
    
    [Serializable]
    public class BuddyPresetTexture2DAsset
    {
        [SerializeField] private string name;
        // NOTE: 現行設計だとサブキャラがoffでもメモリに乗るので、その状態でのメモリ占有をちょっとケチっておく
        [SerializeField] private TextAsset textureBinary;
        
        public string Name => name;
        public byte[] TextureBinary => textureBinary.bytes;
    }

    [Serializable]
    public class BuddyPresetVrmAsset
    {
        [SerializeField] private string name;
        [SerializeField] private TextAsset vrmBinary;

        public string Name => name;
        // NOTE: 読み込みのフローをファイルからロードするケースと揃えたいので、Unity Editor上で特殊なimportをしない
        public byte[] VrmBinary => vrmBinary.bytes;
    }
}

