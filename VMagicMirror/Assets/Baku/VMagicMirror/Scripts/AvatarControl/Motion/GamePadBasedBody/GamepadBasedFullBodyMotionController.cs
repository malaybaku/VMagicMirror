using System;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    public sealed class GamepadBasedFullBodyMotionController : IInitializable, IDisposable
    {
        private readonly CompositeDisposable _disposable = new CompositeDisposable();

        
        
        void IInitializable.Initialize()
        {
        }

        void IDisposable.Dispose() => _disposable.Dispose();
    }
}
