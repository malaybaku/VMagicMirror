using System;
using System.Collections;
using UnityEngine;

namespace Baku.VMagicMirror.IK
{
    public abstract class HandIkGeneratorBase : IDisposable
    {
        public HandIkGeneratorBase(HandIkGeneratorDependency dependency)
        {
            Dependency = dependency;
        }

        protected HandIkGeneratorDependency Dependency { get; }

        //NOTE: ほんとはvirtual => null返却のほうがキレイな実装だが、一時的にabstractとする
        //TODO: 最後にvirtualでnull返却するように直す事！
        public abstract IHandIkState LeftHandState { get; }
        public abstract IHandIkState RightHandState { get; }

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

        public virtual void Dispose()
        {
        }

        protected Coroutine StartCoroutine(IEnumerator i) => Dependency.Component.StartCoroutine(i);
        protected void StopCoroutine(Coroutine coroutine) => Dependency.Component.StopCoroutine(coroutine);
    }
}
