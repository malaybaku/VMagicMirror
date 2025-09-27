using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.MediaPipeTracker
{
    // NOTE
    // - このクラスではmirrorの反映、およびトラッキングロストしてるかどうかの判定までを行う
    //   - 特にトラッキングロストに関してステートフルである
    // - 姿勢の平滑化はこのクラスでは計算しない
    //   - この点ではステートレス

    /// <summary>
    /// マルチスレッドを考慮したうえでIK/FKの計算をするすごいやつだよ
    /// </summary>
    public class MediaPipeKinematicSetter : ITickable
    {        
        private readonly BodyScaleCalculator _bodyScaleCalculator;
        private readonly MediaPipeTrackerRuntimeSettingsRepository _settingsRepository;
        private readonly MediapipePoseSetterSettings _poseSetterSettings;
        private readonly object _poseLock = new();

        // NOTE: ポーズに関しては「トラッキング結果を保存する関数群」の部分で反転を掛けるので、内部計算はあまり反転しない
        private bool IsFaceMirrored => _settingsRepository.IsFaceMirrored.Value;
        private bool IsHandMirrored => _settingsRepository.IsHandMirrored.Value;
        private float HandTrackingMotionScale => _settingsRepository.HandTrackingMotionScale.Value;
        private float HandTrackingOffsetX => _settingsRepository.HandTrackingOffsetX.Value;
        private float HandTrackingOffsetY => _settingsRepository.HandTrackingOffsetY.Value;
        
        // NOTE:
        // - Poseの原点は「キャリブしたときに頭が映ってた位置」が期待値
        // - headPose.position は root位置の移動として反映する
        private readonly CounterBoolState _hasHeadPose = new(3, 5);
        private Pose _headPose = Pose.identity;
        // NOTE: このtimeは以下のように使う。 (left|right)Hand のほうも同じ
        // - _hasHeadPose がtrueになると積算されはじめる
        // - SetHeadPose か ClearHeadPose のいずれかでカウントをリセットする
        //   - つまり、トラッキングできてるかどうかによらず、結果が振ってさえいればリセットされ続ける
        // - 一定値を超えた場合、トラッキングロストと同様に扱って _hasHeadPose をリセットする
        private float _headResultLostTime = 0f;

        private readonly CounterBoolState _hasLeftHandPose = new(3, 15);
        private Vector2 _leftHandNormalizedPos;
        private Quaternion _leftHandRot = Quaternion.identity;
        private float _leftHandResultLostTime = 0f;
        // TODO: RateじゃなくてKinematicSetter上では角度っぽい値を持つようにしたい (「肩が真下からどのくらい開いてるかの角度」とかで良さそう)
        public float LeftElbowOpenRate { get; private set; }

        private readonly CounterBoolState _hasRightHandPose = new(3, 15);
        private Vector2 _rightHandNormalizedPos;
        private Quaternion _rightHandRot = Quaternion.identity;
        private float _rightHandResultLostTime = 0f;
        public float RightElbowOpenRate { get; private set; }
        
        [Inject]
        public MediaPipeKinematicSetter(
            MediapipePoseSetterSettings poseSettings,
            MediaPipeTrackerRuntimeSettingsRepository settingsRepository,
            BodyScaleCalculator bodyScaleCalculator,
            MediapipePoseSetterSettings poseSetterSettings
        )
        {
            _settingsRepository = settingsRepository;
            _bodyScaleCalculator = bodyScaleCalculator;
            _poseSetterSettings = poseSetterSettings;
        }

        #region トラッキング結果のI/O
        
        public bool IsHeadPoseLostEnoughLong()
        {
            lock (_poseLock)
            {
                return _headResultLostTime > _poseSetterSettings.TrackingLostPoseAndFacialResetWait;
            }
        }
        
        /// <summary>
        /// NOTE: 一度でもトラッキングに成功したあとで戻り値がfalseになった場合、resultにはトラッキングロスト直前の姿勢が入る。
        /// つまり、戻り値がfalseの場合でもresultを参照し続けた場合、トラッキングロス時の姿勢を適用できる
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public bool TryGetHeadPose(out Pose result)
        {
            lock (_poseLock)
            {
                result = _headPose;
                return _hasHeadPose.Value;
            }
        }
        
        public void SetHeadPose6Dof(Pose pose)
        {
            lock (_poseLock)
            {
                var pos = new Vector3(
                    pose.position.x * _poseSetterSettings.Face6DofHorizontalScale,
                    pose.position.y * _poseSetterSettings.Face6DofHorizontalScale,
                    pose.position.z * _poseSetterSettings.Face6DofDepthScale
                );
                if (IsFaceMirrored)
                {
                    pos = MathUtil.Mirror(pos);
                    pose = MathUtil.Mirror(pose);
                }

                if (!_settingsRepository.EnableBodyMoveZAxis)
                {
                    pos.z = 0f;
                }
                
                _headPose = new Pose(pos, pose.rotation);
                _hasHeadPose.Set(true);
                _headResultLostTime = 0f;
            }
        }
        
        public void ClearHeadPose()
        {
            lock (_poseLock)
            {
                _hasHeadPose.Set(false);
            }
        }

        // NOTE: maybeLostは、トラッキングロストが始まってるかもしれないときにtrueになる。トラッキングロスト動作の前で惰性を演出したい場合に用いる
        public bool TryGetLeftHandPose(out Pose result, out bool maybeLost)
        {
            lock (_poseLock)
            {
                // Valueがfalse = 平滑化の結果としてロスト扱い
                // LatestSetValueがfalse = 平滑化の結果には出てないけどロストしてるかも…という状態
                maybeLost = !_hasLeftHandPose.Value || !_hasLeftHandPose.LatestSetValue;

                if (_hasLeftHandPose.Value)
                {
                    result = GetHandPose(true);
                    return true;
                }
                
                result = Pose.identity;
                return false;
            }
        }
        
        /// <summary>
        /// NOTE: posは「画像座標で、x軸方向を基準として画像のアス比の影響だけ除去したもの」を指定する。
        /// つまり、画像座標 (x, y) に対して (x, y / aspect) 的な加工をした値を指定するのが期待値。
        ///
        /// また、rotは「手のひらが+z, 中指が+yに向いてる状態」を基準とした回転を指定する。
        /// VRMのワールド回転の値ではないことに注意する。
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="rot"></param>
        public void SetLeftHandPose(Vector2 pos, Quaternion rot)
        {
            lock (_poseLock)
            {
                if (IsHandMirrored)
                {
                    SetRightHandPoseInternal(MathUtil.Mirror(pos), MathUtil.Mirror(rot));
                }
                else
                {
                    SetLeftHandPoseInternal(pos, rot);
                }
            }
        }
        
        public void ClearLeftHandPose()
        {
            lock (_poseLock)
            {
                if (IsHandMirrored)
                {
                    _hasRightHandPose.Set(false);
                }
                else
                {
                    _hasLeftHandPose.Set(false);
                }
            }
        }

        public bool TryGetRightHandPose(out Pose result, out bool maybeLost)
        {
            lock (_poseLock)
            {
                maybeLost = !_hasRightHandPose.Value || !_hasRightHandPose.LatestSetValue;

                if (_hasRightHandPose.Value)
                {
                    result = GetHandPose(false);
                    return true;
                }
                
                result = Pose.identity;
                return false;
            }
        }

        public void SetRightHandPose(Vector2 pos, Quaternion rot)
        {
            lock (_poseLock)
            {
                if (IsHandMirrored)
                {
                    SetLeftHandPoseInternal(MathUtil.Mirror(pos), MathUtil.Mirror(rot));
                }
                else
                {
                    SetRightHandPoseInternal(pos, rot);
                }
            }
        }

        public void ClearRightHandPose()
        {
            lock (_poseLock)
            {
                if (IsHandMirrored)
                {
                    _hasLeftHandPose.Set(false);
                }
                else
                {
                    _hasRightHandPose.Set(false);
                }
            }
        }

        // NOTE: internal系のメソッドではミラーの検証は行わない(呼び出し元がそこをケアしているのが期待される)
        private void SetLeftHandPoseInternal(Vector2 pos, Quaternion rot)
        {
            _leftHandNormalizedPos = pos;
            _leftHandRot = rot * _poseSetterSettings.ConstHandRotOffset * MediapipeMathUtil.GetVrmForwardHandRotation(true);
            _hasLeftHandPose.Set(true);
            _leftHandResultLostTime = 0f;
        }

        private void SetRightHandPoseInternal(Vector2 pos, Quaternion rot)
        {
            _rightHandNormalizedPos = pos;
            _rightHandRot = rot * _poseSetterSettings.ConstHandRotOffset * MediapipeMathUtil.GetVrmForwardHandRotation(false);
            _hasRightHandPose.Set(true);
            _rightHandResultLostTime = 0f;
        }

        public void SetLeftElbowOpenRate(float rate)
        {
            if (IsHandMirrored)
            {
                RightElbowOpenRate = rate;
            }
            else
            {
                LeftElbowOpenRate = rate;
            }
        }

        public void SetRightElbowOpenRate(float rate)
        {
            if (IsHandMirrored)
            {
                LeftElbowOpenRate = rate;
            }
            else
            {
                RightElbowOpenRate = rate;
            }
        }
        
        #endregion

        void ITickable.Tick()
        {
            lock (_poseLock)
            {
                _headResultLostTime += Time.deltaTime;
                if (_hasHeadPose.Value &&
                    _headResultLostTime > _poseSetterSettings.TrackingLostTimeThreshold)
                {
                    _hasHeadPose.Reset(false);
                }
                
                _leftHandResultLostTime += Time.deltaTime;
                if (_hasLeftHandPose.Value &&
                    _leftHandResultLostTime > _poseSetterSettings.TrackingLostTimeThreshold)
                {
                    LeftElbowOpenRate = 0f;
                    _hasLeftHandPose.Reset(false);
                    _leftHandResultLostTime = 0f;
                }
                
                _rightHandResultLostTime += Time.deltaTime;
                if (_hasRightHandPose.Value &&
                    _rightHandResultLostTime > _poseSetterSettings.TrackingLostTimeThreshold)
                {
                    RightElbowOpenRate = 0f;
                    _hasRightHandPose.Reset(false);
                    _rightHandResultLostTime = 0f;
                }
            }
        }

        // NOTE: 大まかには位置決めのほうがメインになっている
        private Pose GetHandPose(bool isLeftHand)
        {
            var rawNormalizedPos = isLeftHand ? _leftHandNormalizedPos : _rightHandNormalizedPos;

            // NOTE: scaled~ のほうは画像座標ベースの計算に使う。 world~ のほうは実際にユーザーが手を動かした量の推定値になっている
            var scaledNormalizedPos = rawNormalizedPos * _poseSetterSettings.Hand2DofNormalizedHorizontalScale;
            var worldPosDiffXy =
                rawNormalizedPos * (_poseSetterSettings.Hand2DofWorldHorizontalScale * HandTrackingMotionScale) +
                new Vector2(
                    isLeftHand ? -HandTrackingOffsetX : HandTrackingOffsetX,
                    HandTrackingOffsetY
                );
            
            // ポイント
            // - 手が伸び切らない程度に前に出すのを基本とする
            // - 画面中心から離れるほど奥行きをキャンセルして真横方向にする
            var forwardRate = Mathf.Sqrt(1 - Mathf.Clamp01(scaledNormalizedPos.sqrMagnitude));
            var handForwardOffset =
                _bodyScaleCalculator.LeftArmLength * 0.7f * _poseSetterSettings.Hand2DofDepthScale * forwardRate;

            var handPositionOffset = new Vector3(
                worldPosDiffXy.x * _bodyScaleCalculator.BodyHeightFactor,
                worldPosDiffXy.y * _bodyScaleCalculator.BodyHeightFactor,
                handForwardOffset
            );
            var handPosition = _bodyScaleCalculator.RootToHead + handPositionOffset;
         
            // weightに基づいて、補正回転のうち一部だけ適用
            var additionalRotation = Quaternion.Slerp(
                Quaternion.identity,
                GetHandAdditionalRotation(scaledNormalizedPos, isLeftHand),
                _poseSetterSettings.HandRotationModifyWeight
            );

            var baseRotation = isLeftHand ? _leftHandRot : _rightHandRot;
            return new Pose(
                handPosition, 
                additionalRotation * baseRotation
                );
        }
        
        // 手の回転をおおよそ肩に対して球面的にするための補正回転を生成する
        private Quaternion GetHandAdditionalRotation(Vector2 normalizedPos, bool isLeftHand)
        {
            // NOTE: 左(右)手を画面の真ん中つまり顔や胸元に持っていく場合、手のひらが少し右(左)向きになるはず…という補正値
            var rotWhenHandIsCenter = Quaternion.Euler(0, isLeftHand ? 10 : -10, 0);

            // 手のひらが下向きになると見栄えが悪いので、基本的には正面～ちょっと上になるように仕向ける
            normalizedPos.y = Mathf.Min(normalizedPos.y + 0.2f, 1.1f);

            // 画面の中心から離れると球面的に外向きの回転がつく。この値はアバターの体型には依存せず、画面座標からの変換だけで決めてしまう
            var forwardRate = Mathf.Sqrt(1.00001f - Mathf.Clamp01(normalizedPos.sqrMagnitude));

            var direction = new Vector3(
                normalizedPos.x, normalizedPos.y, forwardRate * _poseSetterSettings.Hand2DofDepthScale
                );
            return Quaternion.LookRotation(direction) * rotWhenHandIsCenter;
        }
    }
}
