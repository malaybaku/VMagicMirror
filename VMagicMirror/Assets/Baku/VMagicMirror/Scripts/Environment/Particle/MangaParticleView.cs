using UnityEngine;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// マンガ風エフェクトを何かするやつ
    /// </summary>
    public class MangaParticleView : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;
        // 配列になってるパーティクルは順繰りに使われる
        [SerializeField] private TextParticleBase[] normalKeyDowns;
        [SerializeField] private TextParticleBase enterKeyDown;
        [SerializeField] private TextParticleBase mouseButtonDown;
        [SerializeField] private TextParticleBase mouseMove;
        [SerializeField] private TextParticleBase[] gamepadButtonDowns;
        [SerializeField] private TextParticleBase[] gamepadStickMoves;

        private int _normalKeyDownIndex = 0;
        private int _gamepadButtonDownIndex = 0;
        private int _gamepadStickMoveIndex = 0;
        
        
        /// <summary>
        /// マンガ風エフェクト全体の有効/無効を指定する
        /// </summary>
        /// <param name="active"></param>
        public void SetActive(bool active) => canvas.gameObject.SetActive(active);

        public void RunNormalKeyDownEffect()
        {
            var target = normalKeyDowns[_normalKeyDownIndex];
            target.SetAnchorPosition(target.GenerateAnchorPosition());
            target.RunAnimation();
            _normalKeyDownIndex = (_normalKeyDownIndex + 1) % normalKeyDowns.Length;
        }

        public void RunEnterKeyDownEffect()
        {
            enterKeyDown.SetAnchorPosition(enterKeyDown.GenerateAnchorPosition());
            enterKeyDown.RunAnimation();
        }

        public void RunMouseKeyDownEffect()
        {
            mouseButtonDown.SetAnchorPosition(mouseButtonDown.GenerateAnchorPosition());
            mouseButtonDown.RunAnimation();
        }

        public void RunGamepadButtonDownEffect()
        {
            var target = gamepadButtonDowns[_gamepadButtonDownIndex];
            target.SetAnchorPosition(target.GenerateAnchorPosition());
            target.RunAnimation();
            _gamepadButtonDownIndex = (_gamepadButtonDownIndex + 1) % gamepadButtonDowns.Length;
        }

        public void RunGamepadStickMoveEffect()
        {
            var target = gamepadStickMoves[_gamepadStickMoveIndex];
            target.SetAnchorPosition(target.GenerateAnchorPosition());
            target.RunAnimation();
            _gamepadStickMoveIndex = (_gamepadStickMoveIndex + 1) % gamepadStickMoves.Length;
        }

        public void SetKeyDownTexture(Texture2D texture)
        {
            foreach (var target in normalKeyDowns)
            {
                target.SetTexture(texture);
            }
        }
        public void SetEnterKeyDownTexture(Texture2D texture) => enterKeyDown.SetTexture(texture);

        public void SetMouseClickTexture(Texture2D texture) => mouseButtonDown.SetTexture(texture);
        public void SetMouseMoveTexture(Texture2D texture) => mouseMove.SetTexture(texture);
        
        public void SetGamepadButtonDownTexture(Texture2D texture)
        {
            foreach (var target in gamepadButtonDowns)
            {
                target.SetTexture(texture);
            }
        }

        public void SetGamepadStickTexture(Texture2D texture)
        {
            foreach (var target in gamepadStickMoves)
            {
                target.SetTexture(texture);
            }
        }
    }
}
