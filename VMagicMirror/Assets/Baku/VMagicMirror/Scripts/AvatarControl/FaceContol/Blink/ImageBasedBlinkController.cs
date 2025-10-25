using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// <see cref="FaceTracker"/>の入力からまばたき値を出力するやつ
    /// </summary>
    public class ImageBasedBlinkController : MonoBehaviour
    {
        private readonly RecordBlinkSource _blinkSource = new RecordBlinkSource();
        public IBlinkSource BlinkSource => _blinkSource;
        
        //[Inject] 
        private FaceTracker _faceTracker = null;

        [SerializeField] private float eyeOpenKeepDiffPer16ms = 0.04f;
        [SerializeField] private float eyeOpenFromCloseDiffPer16ms = 0.1f;
        [Tooltip("Blink値がこの値を下から上にまたいだ場合、直ちにまばたき状態にする")]
        [SerializeField] private float blinkJumpThreshold = 0.35f;

        private bool _isLeftEyeOpening = false;
        private bool _isRightEyeOpening = false;

        private void Update()
        {
            //画像処理の精度が無限に良ければコレで済むが、そうもいかないのでフィルターとかをかませる
            //_blinkSource.Left = _faceTracker.EyeOpen.LeftEyeBlink;
            //_blinkSource.Right = _faceTracker.EyeOpen.RightEyeBlink;

            //コンセプト:
            // - 目開け付近(blink 0 ~ 0.3とか)で動くうちはそれに低速で追従
            // - それ以上に遷移する場合、まばたき状態に倒してから調整する。
            // つまり、基本的に半目状態は認めない。

            //deltaTime == 16msの場合向けにパラメタが決まってるので、そこで抑えておけばOK。
            //また_faceTracker.EyeOpenは最初から[0, 1]の範囲の値を返してるので、
            //openDiffとかcloseDiffのせいで値が01を超えてもよい
            float openKeepDiff = eyeOpenKeepDiffPer16ms * Time.deltaTime * 60f;
            float openFromCloseDiff = eyeOpenFromCloseDiffPer16ms * Time.deltaTime * 60f;

            float left = _faceTracker.CurrentAnalyzer.Result.LeftBlink;
            
            // 目の動きは3パターン考えると分かりやすい(書いてある通りだけど)
            if (left > blinkJumpThreshold)
            {
                //とりあえず値が一定以上 -> ただちに閉じる。
                _blinkSource.Left = 1.0f;
                _isLeftEyeOpening = false;
            }
            else if (_blinkSource.Left > blinkJumpThreshold)
            {
                // 目を閉じてたのを徐々に開く: 見た目分かりやすいのでMax側にも幅を作っているが、
                // 条件的にmax側は実際には使われない
                _blinkSource.Left = Mathf.Clamp(
                    left, _blinkSource.Left - openFromCloseDiff, _blinkSource.Right + openFromCloseDiff
                );
                _isLeftEyeOpening = true;
            }
            else if (_isLeftEyeOpening)
            {
                // 閉じ目を開く動作の途中: 開く方向には高速で動かすことができる
                float nextLeft = Mathf.Clamp(
                    left, _blinkSource.Left - openFromCloseDiff, _blinkSource.Right + openKeepDiff
                );

                //目が十分開いた or 目を閉じる方向に動いた = 目開き動作が終わったとみなし、より低速の動きに切り替える
                if (nextLeft < 0.1f || nextLeft > _blinkSource.Left)
                {
                    _isLeftEyeOpening = false;
                }
                
                _blinkSource.Left = nextLeft;
            }
            else
            {
                // 目が最初から開いてたのが引き続き開いている
                _blinkSource.Left = Mathf.Clamp(
                    left, _blinkSource.Left - openKeepDiff, _blinkSource.Left + openKeepDiff
                );
            }

            float right = _faceTracker.CurrentAnalyzer.Result.RightBlink;
            if (right > blinkJumpThreshold)
            {
                //とりあえず値が一定以上 -> ただちに閉じる。
                _blinkSource.Right = 1.0f;
            }
            else if (_blinkSource.Right > blinkJumpThreshold)
            {
                // 目を閉じてたのを徐々に開く: 見た目分かりやすいのでMax側にも幅を作っているが、
                // 条件的にmax側は実際には使われない
                _blinkSource.Right = Mathf.Clamp(
                    right, _blinkSource.Right - openFromCloseDiff, _blinkSource.Right + openFromCloseDiff
                );
            }
            else if (_isRightEyeOpening)
            {
                float nextRight = Mathf.Clamp(
                    right, _blinkSource.Right - openFromCloseDiff, _blinkSource.Right + openKeepDiff
                );

                if (nextRight < 0.1f || nextRight > _blinkSource.Right)
                {
                    _isRightEyeOpening = false;
                }
                
                _blinkSource.Right = nextRight;
            }
            else
            {
                _blinkSource.Right = Mathf.Clamp(
                    right, _blinkSource.Right - openKeepDiff, _blinkSource.Right + openKeepDiff
                );
            }
        }
    }
}
