using Deform;
using UnityEngine;

namespace Baku.VMagicMirror
{
    public class DeformableCounter : MonoBehaviour
    {
        private DeformableManager _deformableManager = null;
        private bool _hasDeformableManager = false;
        private int _deformableCount = 0;

        private DeformableManager Manager
        {
            get
            {
                if (!_hasDeformableManager)
                {
                    _deformableManager = DeformableManager.GetDefaultManager(true);
                    if (_deformableManager != null)
                    {
                        _deformableManager.update = _deformableCount > 0;
                        _hasDeformableManager = true;
                    }
                }
                return _deformableManager;
            }
        }

        private void Start()
        {
            //起動直後は0になってるはずだからupdateを切っておきたい、という処理
            if (Manager != null)
            {
                Manager.update = _deformableCount > 0;
            }
        }
        
        /// <summary>
        /// Deformを使いたいクラスで、使い始めのタイミングで呼び出します。
        /// Deformを使い終わったらDecrement()を呼び出して下さい。
        /// </summary>
        public void Increment()
        {
            _deformableCount++;
            if (_deformableCount == 1 && Manager != null)
            {
                Manager.update = true;
            }
        }

        /// <summary>
        /// Incrementを呼び出して開始したDeformを終了します。
        /// </summary>
        public void Decrement()
        {
            _deformableCount--;
            if (_deformableCount == 0 && Manager != null)
            {
                Manager.update = false;
            }
        }
    }
}
