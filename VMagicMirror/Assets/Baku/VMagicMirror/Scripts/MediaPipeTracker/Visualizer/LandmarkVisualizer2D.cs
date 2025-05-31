using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Baku.VMagicMirror.MediaPipeTracker
{
    public class LandmarkVisualizer2D : MonoBehaviour
    {
        [SerializeField] private RectTransform gizmoPrefab;
        [SerializeField] private RawImage targetScreen;
        // _points のうち、min/maxで囲われている範囲だけを表示する。どっちか片方がマイナスの場合、この値は無視し、pointsのすべてを表示する
        [SerializeField] private int pointsVisualizePreferredIndexMin = -1;
        [SerializeField] private int pointsVisualizePreferredIndexMax = -1;

        private readonly List<RectTransform> _gizmos = new();
        
        private readonly object _pointsLock = new();
        private readonly List<Vector2> _points = new();
        
        /// <summary>
        /// NormalizedLandmark相当の座標系の値を指定する。座標系の補正とかはクラス内で行う
        /// </summary>
        /// <param name="points"></param>
        public void SetPositions(IEnumerable<Vector2> points)
        {
            lock (_pointsLock)
            {
                _points.Clear();
                _points.AddRange(points);
            }
        }

        public void Clear()
        {
            lock (_pointsLock)
            {
                _points.Clear();
            }
        }

        private void Update()
        {
            lock (_pointsLock)
            {
                if (CheckCanVisualizeRangedPoints())
                {
                    RenderPoints(pointsVisualizePreferredIndexMin, pointsVisualizePreferredIndexMax);
                }
                else
                {
                    RenderPoints(0, _points.Count);
                }
            }
        }

        // minはinclusive, maxはexclusive
        private void RenderPoints(int minIndex, int maxIndex)
        {
            var count = maxIndex - minIndex;
            if (_gizmos.Count != count)
            {
                RefreshGizmo(count);
            }

            for (var i = 0; i < count; i++)
            {
                var rt = _gizmos[i];
                var p = _points[i + minIndex];
                // pointsは左上が[0,0]で右下が[1,1]になるような座標系なので座標系を直す
                rt.anchorMin = rt.anchorMax = new Vector2(
                    p.x,
                    1f - p.y
                );
                rt.anchoredPosition = Vector2.zero;
            }
        }
        
        private bool CheckCanVisualizeRangedPoints()
        {
            if (pointsVisualizePreferredIndexMin < 0 ||
                pointsVisualizePreferredIndexMax < 0 || 
                pointsVisualizePreferredIndexMin >= pointsVisualizePreferredIndexMax ||
                _points.Count == 0)
            {
                //単に設定を使う気がないケース || 自明に設定が不正なケース
                return false;
            }
            
            // 設定を使う気がありそうだが、範囲設定が不正なケース
            if (pointsVisualizePreferredIndexMin >= _points.Count ||
                pointsVisualizePreferredIndexMax >= _points.Count)
            {
                Debug.LogWarning($"requested index min or max is larger than points count {_points.Count}");
                return false;
            }

            return true;
        }
        
        // gizmoの数が指定した個数になっていなければリフレッシュする
        private void RefreshGizmo(int count)
        {
            foreach (var g in _gizmos)
            {
                Destroy(g.gameObject);
            }
            _gizmos.Clear();

            for (var i = 0; i < count; i++)
            {
                var gizmo = Instantiate(gizmoPrefab, targetScreen.rectTransform);
                // 色つけるほどでもない気がするので、GameObjectの名前をいじる
                gizmo.gameObject.name = $"Points_[{i}]";
                _gizmos.Add(gizmo);
            }
        }
    }
}
