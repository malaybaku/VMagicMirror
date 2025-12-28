using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private Camera cam = null;
        [SerializeField] private CameraTransformController transformController = null;

        private Vector3 _defaultCameraPosition = Vector3.zero;
        private Vector3 _defaultCameraRotationEuler = Vector3.zero;

        // NOTE: 下記4つの値はWPFから渡ってきた物を使う
        private Vector3 _customCameraPosition = Vector3.zero;
        private Vector3 _customCameraRotationEuler = Vector3.zero;
        private bool _hasReferenceHeadPosition;
        private Vector3 _referenceHeadPosition;

        // _prevを使うのはアバター切り替えの前後でカメラ位置をケアするため
        private bool _hasPrevModelHeadPosition;
        private Vector3 _prevModelHeadPosition;
        
        private bool _hasModel;
        private Vector3 _headPosition;
        
        [Inject]
        public void Initialize(IVRMLoadable vrmLoadable, IMessageReceiver receiver)
        {
            vrmLoadable.VrmLoaded += info =>
            {
                _headPosition = info.animator.GetBoneTransform(HumanBodyBones.Head).position;
                _hasModel = true;
                AdjustCameraPoseOnVrmLoaded();
                ApplyReferenceHeadPositionIfAvailable();

                _prevModelHeadPosition = _headPosition;
                _hasPrevModelHeadPosition = true;
            };

            vrmLoadable.VrmDisposing += () =>
            {
                _hasModel = false;
                _headPosition = Vector3.zero;
            };

            receiver.AssignCommandHandler(
                VmmCommands.EnableFreeCameraMode,
                message => EnableFreeCameraMode(message.ToBoolean())
            );

            receiver.AssignCommandHandler(
                VmmCommands.SetCustomCameraPosition, 
                message => 
                    SetCustomCameraPosition(message.GetStringValue())
                    );

            receiver.AssignCommandHandler(
                VmmCommands.QuickLoadViewPoint, 
                message => 
                    SetCustomCameraPosition(message.GetStringValue())
                    );

            receiver.AssignCommandHandler(
                VmmCommands.ResetCameraPosition, 
                message => ResetCameraPosition()
                );

            receiver.AssignQueryHandler(
                VmmCommands.CurrentCameraPosition, 
                query =>
                {
                    var t = cam.transform;
                    var angles = t.rotation.eulerAngles;
                    var pos = t.position;
                    
                    var headPosition =
                        _hasModel ? _headPosition :
                        _hasReferenceHeadPosition ? _referenceHeadPosition :
                        Vector3.zero;

                    query.Result = JsonUtility.ToJson(new SerializedCameraPosition(
                        new[] { pos.x, pos.y, pos.z, angles.x, angles.y, angles.z },
                        _hasModel || _hasReferenceHeadPosition,
                        headPosition
                        ));
                });
        }

        private void Start()
        {
            var camTransform = cam.transform;
            _defaultCameraPosition = camTransform.position;
            _defaultCameraRotationEuler = camTransform.rotation.eulerAngles;
        }

        private void EnableFreeCameraMode(bool v)
            => transformController.enabled = v;

        private void ResetCameraPosition()
        {
            _customCameraPosition = _defaultCameraPosition;
            _customCameraRotationEuler = _defaultCameraRotationEuler;
            _hasReferenceHeadPosition = false;
            _referenceHeadPosition = Vector3.zero;

            UpdateCameraPose();
        }


        private void SetCustomCameraPosition(string content)
        {
            try
            {
                var data = JsonUtility.FromJson<SerializedCameraPosition>(content);

                // カメラ位置自体が完全にゼロな場合、無効値として無視
                var values = data.values.ToArray();
                if (values.All(v => Mathf.Abs(v) < Mathf.Epsilon))
                {
                    return;
                }

                _customCameraPosition = new Vector3(values[0], values[1], values[2]);
                _customCameraRotationEuler = new Vector3(values[3], values[4], values[5]);
                _hasReferenceHeadPosition = data.hasHeadPosition;
                _referenceHeadPosition = data.headPosition;

                UpdateCameraPose();
                ApplyReferenceHeadPositionIfAvailable();
            }
            catch(Exception ex)
            {
                LogOutput.Instance.Write(ex);
            }
        }

        private void UpdateCameraPose()
        {
            // NOTE: WPF側がきちんと設定を持ってないと6DoF=0,0,0,0,0,0を指定したようになるため、そのときに入力を無視する
            if (!(_customCameraPosition.magnitude > Mathf.Epsilon ||
                _customCameraRotationEuler.magnitude > Mathf.Epsilon
                ))
            {
                cam.transform.SetPositionAndRotation(
                    _defaultCameraPosition,
                    Quaternion.Euler(_defaultCameraRotationEuler)
                );
                return;
            }

            if (_hasModel && _hasReferenceHeadPosition)
            {
                // セーブデータと現在のアバター双方のHeadボーンの位置が分かっている場合、
                // セーブデータの頭位置に対して身長差を加味した値を実際の値とする。
                // どちらか片方だけでも不明な場合、補正はしない
                var headDiff = _headPosition - _referenceHeadPosition;
                cam.transform.SetPositionAndRotation(
                    _customCameraPosition + headDiff,
                    Quaternion.Euler(_customCameraRotationEuler)
                );
            }
            else
            {
                cam.transform.SetPositionAndRotation(
                    _customCameraPosition,
                    Quaternion.Euler(_customCameraRotationEuler)
                );
            }
        }

        private void AdjustCameraPoseOnVrmLoaded()
        {
            if (_hasPrevModelHeadPosition)
            {
                // アプリ起動後、2体目以降のアバターロードで通過
                var diff = _headPosition - _prevModelHeadPosition;
                cam.transform.position += diff;
                return;
            }

            if (HasValidCustomCameraPose() && _hasReferenceHeadPosition)
            {
                // アプリ起動後の1体目のロードより前に ReferenceHeadPosition がついたカメラ姿勢が設定済みだと通過
                var diff = _headPosition - _referenceHeadPosition;
                cam.transform.position += diff;
                return;
            }

            // 下記いずれかでここに到達する。これらのケースでは調整のしようがないので、何もしないでOK
            // - そもそもWPF側からカメラ姿勢が送られてない状態で、1体目のアバターをロード
            // - 保存済みのカメラ姿勢にリファレンスとなるアバターのHead位置がない状態で、1体目のアバターをロード
        }
        
        private void ApplyReferenceHeadPositionIfAvailable()
        {
            if (_hasModel && HasValidCustomCameraPose() && !_hasReferenceHeadPosition)
            {
                _hasReferenceHeadPosition = true;
                _referenceHeadPosition = _headPosition;
            }
        }

        private bool HasValidCustomCameraPose()
            => _customCameraPosition.magnitude > Mathf.Epsilon || _customCameraRotationEuler.magnitude > Mathf.Epsilon;
    }

    [Serializable]
    public class SerializedCameraPosition
    {
        public List<float> values = new();

        // v4.2.1から追加:
        // valuesで定まるカメラ位置を使っているアバターの頭ボーン位置 (※ロード直後のTポーズ時) をペアで保存する
        // インストール後や旧バージョンではこの値は保存されていないが、
        // VMMが起動してアバターを読み込むと (フリーカメラを使わないでも) ポーリングによって値が送られる
        public bool hasHeadPosition;
        public Vector3 headPosition;

        public SerializedCameraPosition(float[] values, bool hasHeadPosition, Vector3 headPosition)
        {
            this.values = new List<float>(values);
            this.hasHeadPosition = hasHeadPosition;
            this.headPosition = headPosition;
        }
    }
}

