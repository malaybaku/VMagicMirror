using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary>プレゼンテーション動作時に右手があるべき場所を求めるやつ</summary>
    public class PresentationHandIKGenerator : MonoBehaviour
    {
        private const float PresentationArmRollFixedAngle = 25.0f;

        //シグモイドの最大値: 1.0fより小さくすることで腕がピンと伸びるのを防ぐ
        private const float SigmoidMax = 0.95f;
        //シグモイド関数を横方向に圧縮する
        private const float SigmoidGain = 1.0f;
        //シグモイド関数を右方向にずらす
        private const float SigmoidSlide = 1.0f;
        //普通のシグモイド関数
        private static float Sigmoid(float x)
        {
            return SigmoidMax / (1.0f + Mathf.Exp(-(x - SigmoidSlide) * SigmoidGain));
        }

        private readonly IKDataRecord _rightHand = new IKDataRecord();
        public IIKGenerator RightHand => _rightHand;

        private readonly IKDataRecord _rightIndex = new IKDataRecord();
        public IIKGenerator RightIndex => _rightIndex;

        //VRMの肩から手首までの長さ
        private float _lengthFromShoulderToWrist = 0.4f;

        #region settings 

        /// <summary>手首から指先までの距離[m]</summary>
        public float HandToTipLength { get; set; } = 0.1f;

        //TODO: このスケール値を使わずに腕を動かしたい
        /// <summary>腕モーションのスケーリング値。モニターが大きいときに用いる</summary>
        public float PresentationArmMotionScale { get; set; } = 0.5f;

        /// <summary>腕がめり込まないための、胴体を円柱とみなした時の半径に相当する値[m]</summary>
        public float PresentationArmRadiusMin { get; set; } = 0.2f;

        
        //ここから下の設定はUnityの中で勝手にやる

        [SerializeField]
        private Camera _cam = null;

        //Time.deltaTimeを掛けた値をLerpに適用する
        [SerializeField]
        private float _speedFactor = 12f;

        [Inject]
        private IVRMLoadable _vrmLoadable = null;
        
        //プレゼンの腕位置をいい感じに計算するために用いる。
        //TODO: スケール値を廃止するときには肩～右指先までの長さをきっちり使うことになるかも
        private Transform _head = null;
        private Transform _rightShoulder = null;

        //note: publicにしている必然性はそんなに無さそうな
        /// <summary>キャラのロード後にIK位置が更新されたかどうか</summary>
        public bool WaitFirstUpdate { get; private set; } = true;

        #endregion


        private Vector3 _targetPosition = Vector3.zero;
        private Vector3 _rightIndexTargetPosition = Vector3.zero;
        
        
        public void Initialize(Animator animator)
        {
            _head = animator.GetBoneTransform(HumanBodyBones.Head);
            //NOTE: 肩ボーンはオプションなことに注意する
            _rightShoulder = animator.GetBoneTransform(HumanBodyBones.RightShoulder) ??
                             animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
        }

        public void Dispose()
        {
            _head = null;
            _rightShoulder = null;
        }

        public void MoveMouse(Vector3 mousePosition)
        {
            if (_rightShoulder == null)
            {
                return;
            }

            Vector3 rightShoulderPosition = _rightShoulder.position;

            float camToHeadDistance = Vector3.Distance(_cam.transform.position, _head.position);
            Vector3 mousePositionWithDepth = new Vector3(
                mousePosition.x,
                mousePosition.y,
                camToHeadDistance
                );
            
            //指先がここに合って欲しい、という位置
            var idealFingerTargetPosition = _cam.ScreenToWorldPoint(mousePositionWithDepth);

            var diff = idealFingerTargetPosition - rightShoulderPosition;
            float lengthRatio = diff.magnitude / _lengthFromShoulderToWrist;

            //向きは揃えつつ、シグモイドでいい感じにサチって頂く
            var targetPosition =
                rightShoulderPosition +
                diff.normalized * (_lengthFromShoulderToWrist * Sigmoid(lengthRatio));

            //要るか分かんないが、指先位置は腕を伸ばすのと同じ方向に持って行く
            var fingerTargetPosition =
                targetPosition + diff.normalized * HandToTipLength;

            //右腕を強引に左側に引っ張らないためのガード
            if (targetPosition.x <= 0)
            {
                targetPosition = new Vector3(0.01f, targetPosition.y, targetPosition.z);
            }

            //NOTE: 手を肩よりも後ろに回すのはあまり自然じゃないのでガード
            if (targetPosition.z < 0)
            {
                targetPosition = new Vector3(targetPosition.x, targetPosition.y, 0);
            }

            //手が体に近すぎるとめり込むのをガード
            var horizontalVec = new Vector2(targetPosition.x, targetPosition.z);
            if (horizontalVec.magnitude < PresentationArmRadiusMin)
            {
                if (horizontalVec.magnitude < Mathf.Epsilon)
                {
                    horizontalVec = new Vector2(1, 0);
                }
                horizontalVec = PresentationArmRadiusMin * horizontalVec.normalized;

                targetPosition = new Vector3(
                    horizontalVec.x,
                    targetPosition.y,
                    horizontalVec.y
                    );
            }

            _targetPosition = targetPosition;
            //手の長さぶんだけ、人差し指が伸びるように仕向ける(ちゃんと動くか分かんないが)
            _rightIndexTargetPosition = fingerTargetPosition;
        }

        private void Start()
        {
            _vrmLoadable.VrmLoaded += info =>
            {
                //NOTE: Shoulderが必須ボーンでは無い事に注意
                var bones = new List<Transform>()
                    {
                        info.animator.GetBoneTransform(HumanBodyBones.RightShoulder),
                        info.animator.GetBoneTransform(HumanBodyBones.RightUpperArm),
                        info.animator.GetBoneTransform(HumanBodyBones.RightLowerArm),
                        info.animator.GetBoneTransform(HumanBodyBones.RightHand),
                    }
                    .Where(t => t != null)
                    .ToArray();

                float sum = 0;
                for (int i = 0; i < bones.Length - 1; i++)
                {
                    sum += Vector3.Distance(bones[i].position, bones[i + 1].position);
                }
                _lengthFromShoulderToWrist = sum;
            };
        }
        
        private void Update()
        {
            if (_head == null || _rightShoulder == null)
            {
                return;
            }

            float lerpFactor = WaitFirstUpdate ? 1.0f : (_speedFactor * Time.deltaTime);

            _rightHand.Position = Vector3.Lerp(
                _rightHand.Position,
                _targetPosition,
                lerpFactor
                );

            //右人差し指はPositionだけで十分: RotationWeightを使わない想定のため
            _rightIndex.Position = Vector3.Lerp(
                _rightIndex.Position,
                _rightIndexTargetPosition,
                _speedFactor * Time.deltaTime
                );

            //NOTE: 追加で回しているのは手の甲を内側にひねる成分(プレゼン的な動作として見栄えがよい…はず…)
            _rightHand.Rotation = Quaternion.FromToRotation(
                Vector3.right,
                (_rightHand.Position - _rightShoulder.position).normalized
                ) * Quaternion.AngleAxis(PresentationArmRollFixedAngle, Vector3.right);


            WaitFirstUpdate = false;
        }
    }
}
