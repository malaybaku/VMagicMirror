using UnityEngine;

namespace Baku.VMagicMirror
{
    public class EyebrowBlendShapeSet
    {
        static class FixedBlendShapeNames
        {
            public const string VRoid_Ver0_6_3_Up = "Face.M_F00_000_00_Fcl_BRW_Surprised";
            public const string VRoid_Ver0_6_3_Down = "Face.M_F00_000_00_Fcl_BRW_Angry";
        }

        //TODO: ブレンドシェイプの名前を可変にする: 
        // 下記はあくまでVRoid Studioの(v0.6.3の)出力で用いられてる名前であって仕様ではないため

        //NOTE: VRMの仕様上、ふつうは左右の眉が共有モーフになってるはずと考えられるので、既定値はオフ
        public bool UseSeparatedTarget { get; set; } = false;

        public string LeftUpKey { get; set; } = FixedBlendShapeNames.VRoid_Ver0_6_3_Up;
        public string LeftDownKey { get; set; } = FixedBlendShapeNames.VRoid_Ver0_6_3_Down;
        public string RightUpKey { get; set; } = "";
        public string RightDownKey { get; set; } = "";

        public float UpScale { get; set; } = 1.0f;
        public float DownScale { get; set; } = 1.0f;

        private BlendShapeTarget _leftUp;
        private BlendShapeTarget _leftDown;
        private BlendShapeTarget _rightUp;
        private BlendShapeTarget _rightDown;

        //TODO: こっちのオーバーロードはそのうち消してね
        public void RefreshTarget(BlendShapeStore blendShapeStore)
        {
            Reset();

            var items = blendShapeStore.GetBlendShapeStoreItems();
            int goalCount = UseSeparatedTarget ? 4 : 2;
            int foundCount = 0;

            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];
                if (item.name == LeftUpKey && !_leftUp.isValid)
                {
                    _leftUp = new BlendShapeTarget(item);
                    foundCount++;
                }
                else if (item.name == LeftDownKey && !_leftDown.isValid)
                {
                    _leftDown = new BlendShapeTarget(item);
                    foundCount++;
                }
                else if (UseSeparatedTarget && item.name == RightUpKey && !_rightUp.isValid)
                {
                    _rightUp = new BlendShapeTarget(item);
                    foundCount++;
                }
                else if (UseSeparatedTarget && item.name == RightDownKey && !_rightDown.isValid)
                {
                    _rightDown = new BlendShapeTarget(item);
                    foundCount++;
                }

                if (foundCount >= goalCount)
                {
                    return;
                }
            }

        }

        //NOTE: こっちが最終的に生き残るよ
        public void RefreshTarget(VRMBlendShapeStore blendShapeStore)
        {
            Reset();

            var items = blendShapeStore.GetBlendShapeStoreItems();
            int goalCount = UseSeparatedTarget ? 4 : 2;
            int foundCount = 0;

            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];
                if (item.name == LeftUpKey && !_leftUp.isValid)
                {
                    _leftUp = new BlendShapeTarget(item);
                    foundCount++;
                }
                else if (item.name == LeftDownKey && !_leftDown.isValid)
                {
                    _leftDown = new BlendShapeTarget(item);
                    foundCount++;
                }
                else if (UseSeparatedTarget && item.name == RightUpKey && !_rightUp.isValid)
                {
                    _rightUp = new BlendShapeTarget(item);
                    foundCount++;
                }
                else if (UseSeparatedTarget && item.name == RightDownKey && !_rightDown.isValid)
                {
                    _rightDown = new BlendShapeTarget(item);
                    foundCount++;
                }

                if (foundCount >= goalCount)
                {
                    return;
                }
            }

        }
        
        public void Reset()
        {
            _leftUp.SetWeight(0);
            _leftDown.SetWeight(0);
            _rightUp.SetWeight(0);
            _rightDown.SetWeight(0);

            _leftUp = new BlendShapeTarget();
            _leftDown = new BlendShapeTarget();
            _rightUp = new BlendShapeTarget();
            _rightDown = new BlendShapeTarget();
        }

        /// <summary>
        /// Update blendshape by [-1, 1] value (-1: down, 1: up)
        /// </summary>
        /// <param name="leftEyeBrow"></param>
        /// <param name="rightEyeBrow"></param>
        public void UpdateEyebrowBlendShape(float leftEyeBrow, float rightEyeBrow)
        {
            if (UseSeparatedTarget)
            {
                float leftUpRate = Mathf.Clamp(leftEyeBrow * UpScale, 0, 1);
                float leftDownRate = Mathf.Clamp(-leftEyeBrow * DownScale, 0, 1);

                float rightUpRate = Mathf.Clamp(rightEyeBrow * UpScale, 0, 1);
                float rightDownRate = Mathf.Clamp(-rightEyeBrow, 0, 1) * DownScale;

                //100を掛けるのはBlendShapeがそういう仕様なんです
                _leftUp.SetWeight(leftUpRate * 100);
                _leftDown.SetWeight(leftDownRate * 100);
                _rightUp.SetWeight(rightUpRate * 100);
                _rightDown.SetWeight(rightDownRate * 100);
            }
            else
            {
                float meanEyeBrow = (leftEyeBrow + rightEyeBrow) * 0.5f;

                float upRate = Mathf.Clamp(meanEyeBrow * UpScale, 0, 1);
                float downRate = Mathf.Clamp(-meanEyeBrow * DownScale, 0, 1);

                _leftUp.SetWeight(upRate * 100);
                _leftDown.SetWeight(downRate * 100);
                //共通モーフの場合はLeft側に両眉ぶんのBlendShapeが入るので大丈夫
            }
        }

        struct BlendShapeTarget
        {
            public BlendShapeTarget(BlendShapeStoreItem source) : this()
            {
                isValid = true;
                renderer = source.renderer;
                index = source.index;
            }

            public bool isValid;
            public SkinnedMeshRenderer renderer;
            public int index;

            public void SetWeight(float value)
            {
                if (isValid && renderer != null)
                {
                    renderer.SetBlendShapeWeight(index, value);
                }
            }
        }
    }
}
