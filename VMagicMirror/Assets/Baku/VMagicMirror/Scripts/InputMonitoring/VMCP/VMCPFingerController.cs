using System;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.VMCP
{
    //NOTE: 概念的にはVMCPHandIkGeneratorのサブルーチンだが、処理順をいい感じにしたいのでクラスを分けてます
    public class VMCPFingerController : MonoBehaviour
    {
        private bool _hasAction;
        private Action _lateUpdateAction;

        private BodyMotionModeController _bodyMotionModeController;

        [Inject]
        public void Initialize(BodyMotionModeController bodyMotionModeController)
        {
            _bodyMotionModeController = bodyMotionModeController;
        }
        
        private void LateUpdate()
        {
            //TODO: Word to Motionのモーション実行中も指の状態適用をスキップすべきではある
            //ハンドトラッキング中にWtMの手モーションするか？と思ったためいったん保留している
            if (_hasAction && _bodyMotionModeController.MotionMode.Value != BodyMotionMode.GameInputLocomotion)
            {
                _lateUpdateAction();
            }
        }

        public void SetLateUpdateCallback(Action action)
        {
            _lateUpdateAction = action;
            _hasAction = _lateUpdateAction != null;
        }
    }
}
