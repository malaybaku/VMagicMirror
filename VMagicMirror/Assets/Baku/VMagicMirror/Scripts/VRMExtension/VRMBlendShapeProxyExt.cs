using System.Reflection;
using UnityEngine;
using VRM;

namespace Baku.VMagicMirror.VRMExtension
{
    public static class VRMBlendShapeProxyExtension
    {
        /// <summary>
        /// 指定したBlendShapeProxyで、現在のBlendShapeAvatar.Clipsを使用して内部のmergerインスタンスを再初期化します。
        /// </summary>
        /// <param name="proxy"></param>
        /// <remarks>
        /// なぜこんなコトをするかというと、VRoid用パーフェクトシンク(※そこまでガツガツ動くわけではないが)のための
        /// クリップをAvatar.Clipsに仕込んだとき、明示的にmergerを作り直さないとうまく動かないため。
        /// </remarks>
        public static void ReloadBlendShape(this VRMBlendShapeProxy proxy)
        {
            var type = typeof(VRMBlendShapeProxy);
            var mergerField = type.GetField(
                "m_merger", 
                BindingFlags.Instance | BindingFlags.NonPublic);
            
            var merger = mergerField?.GetValue(proxy) as BlendShapeMerger;
            Debug.Log($"Trying to get m_merger member, success ? = {mergerField != null}, {merger != null}");
            merger?.RestoreMaterialInitialValues(proxy.BlendShapeAvatar.Clips);
            if (proxy.BlendShapeAvatar != null)
            {
                mergerField?.SetValue(
                    proxy,
                    new BlendShapeMerger(proxy.BlendShapeAvatar.Clips, proxy.transform)
                    );
            }
        }
    }
}
