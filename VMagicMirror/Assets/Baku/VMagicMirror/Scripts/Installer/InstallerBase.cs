using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.Installer
{
    /// <summary>
    /// カテゴリごとにInstallerを定義できると良さげなので用意した基底クラスです。実は不要？
    /// </summary>
    public abstract class InstallerBase : MonoBehaviour
    {
        public abstract void Install(DiContainer container);
    }
}
