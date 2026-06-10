using System;
using mattatz.TransformControl;
using UnityEngine;

namespace Baku.VMagicMirror
{
    public sealed class RuntimeTransformControlFactory
    {
        public void DisableExisting(Transform target)
        {
            var control = target.GetComponent<TransformControl>();
            if (control == null)
            {
                return;
            }

            control.mode = TransformControl.TransformMode.None;
            control.enabled = false;
        }

        public RuntimeTransformControlHandle Create(Transform target)
        {
            var control = target.GetComponent<TransformControl>();
            var created = control == null;
            if (created)
            {
                control = target.gameObject.AddComponent<TransformControl>();
            }

            control.Initialize();
            control.enabled = true;
            control.mode = TransformControl.TransformMode.None;
            control.global = false;
            control.useDistance = true;
            control.distance = 12f;
            return new RuntimeTransformControlHandle(control, created);
        }
    }

    public sealed class RuntimeTransformControlHandle : IDisposable
    {
        private readonly bool _destroyOnDispose;

        public RuntimeTransformControlHandle(TransformControl control, bool destroyOnDispose)
        {
            Control = control;
            _destroyOnDispose = destroyOnDispose;
        }

        public TransformControl Control { get; private set; }

        public void Dispose()
        {
            if (Control == null)
            {
                return;
            }

            Control.mode = TransformControl.TransformMode.None;
            Control.enabled = false;
            if (_destroyOnDispose)
            {
                UnityEngine.Object.Destroy(Control);
            }

            Control = null;
        }
    }
}
