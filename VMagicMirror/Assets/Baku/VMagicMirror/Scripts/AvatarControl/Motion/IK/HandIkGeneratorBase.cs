using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Baku.VMagicMirror
{
    public abstract class HandIkGeneratorBase
    {
        public HandIkGeneratorBase(MonoBehaviour coroutineResponder)
        {
            _coroutineResponder = coroutineResponder;
        }
        
        /// <summary> HandIKIntegratorのStart内部で呼ばれます。 </summary>
        public virtual void Start()
        {
        }

        /// <summary> HandIKIntegratorのUpdate内部で呼ばれます。 </summary>
        public virtual void Update()
        {
        }

        /// <summary> HandIKIntegratorのLateUpdate内部で呼ばれます。 </summary>
        public virtual void LateUpdate()
        {
        }


        private readonly MonoBehaviour _coroutineResponder;

        protected Coroutine StartCoroutine(IEnumerator i) => _coroutineResponder.StartCoroutine(i);
        protected void StopCoroutine(Coroutine coroutine) => _coroutineResponder.StopCoroutine(coroutine);
    }
}
