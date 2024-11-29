using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Baku.VMagicMirror
{
    [RequireComponent(typeof(RectTransform))]
    public class TextParticleBase : MonoBehaviour
    {
        // 画面中央から半径それぞれ(x, y)で指定された楕円にだいたい乗る形で文字を置きに行く。
        // ただし、楕円のうち真上/真下から30度以内の範囲は候補範囲ではない
        [Range(0f, 0.5f)] [SerializeField] private float avoidRangeX;
        [Range(0f, 0.5f)] [SerializeField] private float avoidRangeY;
        [SerializeField] private RawImage image;

        [SerializeField] private Vector2 angleRange = new(-60, 60);

        [SerializeField] private bool usePositionRandom;
        [Tooltip("AnchorPosition基準なので、非ゼロにする場合の数値感は 0.01 程度")]
        [SerializeField] private Vector2 positionRandomRange = new(0, 0);

        [SerializeField] private Vector2 localAngleRange = new(-20, 20);
        
        [SerializeField] private float totalDuration = 1f;
        [SerializeField] private float inDuration = 0.25f;
        [SerializeField] private float endDuration = 0.25f;
        [SerializeField] private Vector3 initialLocalScale = new(1.5f, 0.666f, 1f);
        [SerializeField] private Vector2 moveOffset = new(36, 18);

        [SerializeField] private bool useRotate;
        [SerializeField] private bool fixRotateDirection;
        [Range(-20f, 20f)]
        [SerializeField] private float rotateAngleDeg;
        [SerializeField] private float rotateXOffset;

        private RectTransform _rt;
        private RectTransform RectTransform => _rt ??= GetComponent<RectTransform>();

        public void SetTexture(Texture2D texture) => image.texture = texture;
        
        public Vector2 GenerateAnchorPosition()
        {
            var rand = Random.Range(0f, 1f);
            var angle = Mathf.Lerp(angleRange.x, angleRange.y, rand) * Mathf.PI / 180f;

            var result = new Vector2(
                0.5f + avoidRangeX * Mathf.Cos(angle),
                0.5f + avoidRangeY * Mathf.Sin(angle)
                );

            if (usePositionRandom)
            {
                result += new Vector2(
                    Random.Range(-positionRandomRange.x, positionRandomRange.x),
                    Random.Range(-positionRandomRange.y, positionRandomRange.y)
                );
            }

            return result;
        }

        public void SetAnchorPosition(Vector2 pos)
        {
            RectTransform.anchorMin = pos;
            RectTransform.anchorMax = pos;
        }

        public void RunAnimation()
        {
            RunAnimateAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }
        
        protected async UniTask RunAnimateAsync(CancellationToken cancellationToken)
        {
            RectTransform.localScale = initialLocalScale;
            RectTransform.anchoredPosition = Vector2.zero;
            var startLocalAngleEuler = new Vector3(0f, 0f, Random.Range(localAngleRange.x, localAngleRange.y));
            RectTransform.localRotation = Quaternion.Euler(startLocalAngleEuler);

            var offset = moveOffset;
            if (Random.Range(0f, 1f) > 0.5f)
            {
                offset.x = -offset.x;
            }

            var useRotateSign = 1f;
            if (useRotate && !fixRotateDirection)
            {
                useRotateSign = Random.Range(0f, 1f) > 0.5f ? 1 : -1;
            }

            if (useRotate)
            {
                offset.x -= rotateXOffset * useRotateSign;
                // 回転つきの場合、XだけEaseしたいので分ける
                RectTransform.DOAnchorPosX(offset.x, totalDuration).SetEase(Ease.InCubic);
                RectTransform.DOAnchorPosY(offset.y, totalDuration);
            }
            else
            {
                RectTransform.DOAnchorPos(offset, totalDuration);
            }
            
            image.DOFade(1.0f, inDuration);
            RectTransform.DOScaleX(1f, inDuration).SetEase(Ease.OutBounce);
            RectTransform.DOScaleY(1f, inDuration).SetEase(Ease.OutBounce);

            if (useRotate)
            {
                RectTransform
                    .DOLocalRotate(
                        startLocalAngleEuler + new Vector3(0, 0, rotateAngleDeg * useRotateSign),
                        totalDuration
                    )
                    .SetEase(Ease.OutCubic);
            }

            await UniTask.Delay(TimeSpan.FromSeconds(inDuration), cancellationToken: cancellationToken);

            
            
            await UniTask.Delay(
                TimeSpan.FromSeconds(totalDuration - inDuration - endDuration), cancellationToken: cancellationToken
                );
            
            image.DOFade(0f, endDuration);

            await UniTask.Delay(TimeSpan.FromSeconds(endDuration), cancellationToken: cancellationToken);
        }
    }
}
