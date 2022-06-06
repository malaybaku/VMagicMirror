using System.Collections;
using UnityEngine;

namespace Baku.VMagicMirror.IK
{
    public class ClapMotionElbowController
    {
        private readonly ElbowMotionModifier _elbow;
        private readonly MonoBehaviour _coroutineSource;

        private Coroutine _coroutine;
        
        public ClapMotionElbowController(
            ElbowMotionModifier elbowMotionModifier, MonoBehaviour coroutineSource)
        {
            _elbow = elbowMotionModifier;
            _coroutineSource = coroutineSource;
        }

        //※初期値はすごい適当に決めてます
        public float ElbowOffsetX { get; set; } = 0.2f;

        //指定した秒数をかけてヒジをちょっと(拍手用に)開く
        public void ApplyElbowModify(float duration)
        {
            StartCoroutine(ApplyElbowModifyRoutine(duration));
        }

        //指定した秒数でヒジを元の状態に戻す
        public void ResetElbowModify(float duration)
        {
            StartCoroutine(ResetElbowModifyRoutine(duration));
        }

        private IEnumerator ApplyElbowModifyRoutine(float duration)
        {
            var time = 0f;
            while (time < duration)
            {
                var rate = Mathf.SmoothStep(0, 1, time / duration);
                var offset = rate * ElbowOffsetX;
                _elbow.LeftElbowPositionOffset = Vector3.left * offset;
                _elbow.RightElbowPositionOffset = Vector3.right * offset;
                time += Time.deltaTime;
                yield return null;
            }
            _elbow.LeftElbowPositionOffset = Vector3.left * ElbowOffsetX;
            _elbow.RightElbowPositionOffset = Vector3.right * ElbowOffsetX;
        }
        
        private IEnumerator ResetElbowModifyRoutine(float duration)
        {
            var time = 0f;
            while (time < duration)
            {
                var rate = Mathf.SmoothStep(1, 0, time / duration);
                var offset = rate * ElbowOffsetX;
                _elbow.LeftElbowPositionOffset = Vector3.left * offset;
                _elbow.RightElbowPositionOffset = Vector3.right * offset;
                time += Time.deltaTime;
                yield return null;
            }
            
            _elbow.LeftElbowPositionOffset = Vector3.zero;
            _elbow.RightElbowPositionOffset = Vector3.zero;
        }
        
        private void StartCoroutine(IEnumerator coroutine)
        {
            if (_coroutine != null)
            {
                _coroutineSource.StopCoroutine(_coroutine);
            }
            _coroutine = _coroutineSource.StartCoroutine(coroutine);
        }
    }
}
