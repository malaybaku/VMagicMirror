using UnityEngine;

namespace Baku.VMagicMirror.GameInput
{
    public class GameInputBodyRootOrientationController
    {
        private const float TurnDuration = 0.2f;
        private const float SideViewTurnDuration = 0.2f;
        private const float MaxSpeed = 1400f;

        //NOTE: 入力判定側の措置としてこんなに小さい値は通してこないはずだが、念のため。
        private const float MagnitudeThreshold = 0.3f;

        //横スクロールでは左右どっちに向くときもわずかに手前を向かせておく(10degくらい)
        private static readonly Vector2 SideViewLeft = new Vector2(-1f, -0.2f).normalized;
        private static readonly Vector2 SideViewRight = new Vector2(1f,  -0.2f).normalized;

        private readonly Camera _camera;

        public GameInputLocomotionStyle LocomotionStyle { get; set; } = GameInputLocomotionStyle.FirstPerson;

        //NOTE: ワールド回転であって、カメラ位置を考慮した3人称でのアバターの向く方向になるような値。
        //Quaternion.Euler(0, angle, 0)の形式で生成される
        public Quaternion Rotation { get; private set; } = Quaternion.identity;
        private float _yawAngle;
        private float _yawAngleVelocity;

        private float _prevTargetYaw;
        private float _directionMagnitude;

        public GameInputBodyRootOrientationController(Camera camera)
        {
            _camera = camera;
        }


        //NOTE: ゲーム入力が有効な間は毎フレーム呼ぶ想定の関数。
        // - 1人称視点: directionを無視し、Rotationの値を徐々にidentity = 本来の正面向きに持っていく
        // - 3人称視点: 入力に応じた向きに向かせる
        // - 横スクロール: 左右どちらかに向かせるが、3人称とは向かせ方が異なる
        public void UpdateInput(Vector2 direction, float deltaTime)
        {
            switch (LocomotionStyle)
            {
                case GameInputLocomotionStyle.FirstPerson:
                    _yawAngle = Mathf.SmoothDamp(
                        _yawAngle, 0f, ref _yawAngleVelocity, TurnDuration, MaxSpeed, deltaTime
                    );
                    UpdateRotation();
                    return;
                case GameInputLocomotionStyle.ThirdPerson:
                    UpdateInputAsThirdPerson(direction, deltaTime);
                    return;
                case GameInputLocomotionStyle.SideView2D:
                    UpdateInputAsSideView2D(direction, deltaTime);
                    return;
            }
        }

        public void ResetImmediately()
        {
            Rotation = Quaternion.identity;
            _yawAngle = 0f;
            _yawAngleVelocity = 0f;
            _prevTargetYaw = 0f;
            _directionMagnitude = 0f;
        }

        private void UpdateInputAsThirdPerson(Vector2 direction, float deltaTime)
        {
            var yaw = _prevTargetYaw;
            
            if (direction.magnitude > MagnitudeThreshold)
            {
                yaw = GetYawAngleOfThirdPerson(direction);
                _prevTargetYaw = yaw;
            }
            _directionMagnitude = direction.magnitude;

            if (_directionMagnitude < MagnitudeThreshold)
            {
                return;
            }

            //NOTE: 180度付近の反転は常に時計周りで行う
            var deltaAngle = Mathf.DeltaAngle(_yawAngle + 10f, yaw) + 10f;
            var targetAngle = _yawAngle + deltaAngle;

            //NOTE: スティックを弱く倒した場合、回転が遅くなる
            _yawAngle = Mathf.SmoothDamp(
                _yawAngle,
                targetAngle, 
                ref _yawAngleVelocity, 
                TurnDuration * DirectionMagnitudeToDurationFactor(_directionMagnitude),
                MaxSpeed, deltaTime
            );

            //値域をつねに抑える。1人称に戻したときに暴れないようにするため
            _yawAngle = Mathf.DeltaAngle(0f, _yawAngle);

            UpdateRotation();
        }

        private void UpdateInputAsSideView2D(Vector2 direction, float deltaTime)
        {
            var yaw = _prevTargetYaw;
            var x = direction.x;
            if (Mathf.Abs(x) > MagnitudeThreshold)
            {
                //実入力ではない値に差し替えて「左」か「右」の2択に帰着させる
                var directionModified = x > 0 ? SideViewRight : SideViewLeft;
                yaw = GetYawAngleOfThirdPerson(directionModified);
                _prevTargetYaw = yaw;
                _directionMagnitude = Mathf.Abs(x);
            }

            if (_directionMagnitude < MagnitudeThreshold)
            {
                return;
            }

            //NOTE: 手前側に少し向き続ける状態になるので、単にDeltaAngleすると手前を経由する回転が生成される。はず。
            var deltaAngle = Mathf.DeltaAngle(_yawAngle, yaw);
            var targetAngle = _yawAngle + deltaAngle;

            _yawAngle = Mathf.SmoothDamp(
                _yawAngle, targetAngle, ref _yawAngleVelocity, SideViewTurnDuration,
                MaxSpeed, deltaTime
            );

            //値域をつねに抑えるのは通常の3人称と同じ
            _yawAngle = Mathf.DeltaAngle(0f, _yawAngle);
            UpdateRotation();
        }

        private void UpdateRotation() => Rotation = Quaternion.AngleAxis(_yawAngle, Vector3.up);

        private float GetYawAngleOfThirdPerson(Vector2 direction)
        {
            var directionOnCamera = _camera.transform.rotation * new Vector3(direction.x, 0f, direction.y);
            return Mathf.Atan2(directionOnCamera.x, directionOnCamera.z) * Mathf.Rad2Deg;
        }

        //スティックが浅く倒れていると向きの回転にかかる時間が長くなる…というファクター
        private static float DirectionMagnitudeToDurationFactor(float magnitude)
        {
            return 1.0f / Mathf.Clamp(magnitude, 0.3f, 1f);
        }
    }
}