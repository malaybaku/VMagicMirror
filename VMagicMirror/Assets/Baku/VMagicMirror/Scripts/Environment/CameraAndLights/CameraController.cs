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

        // NOTE: 下記4つの値は基本WPFから渡された値をリスペクトして使うが、
        // Unity側では以下2ケースにおいて、現在のアバターのHeadボーン位置を _headPositionFor~ に有効値としてsetする。
        // つまり、頭の基準位置が不明なカメラ姿勢を受け取った場合、現在のアバターの頭位置を基準位置にする。
        // これにより、初回のロード後に
        //
        // - ケース1:
        //   - _customCameraPosition, _customCameraRotationEuler いずれかが非ゼロ、かつ _hasHeadPositionFor~ がfalse
        //   - その状態でアバターをロードした
        // - ケース2:
        //   - アバターがロード済みである
        //   - その状態で、 _hasHeadPosition が false であるようなカメラ姿勢をWPFから受け取った
        private Vector3 _customCameraPosition = Vector3.zero;
        private Vector3 _customCameraRotationEuler = Vector3.zero;
        private bool _hasReferenceHeadPosition;
        private Vector3 _referenceHeadPosition;

        private bool _hasModel;
        private Vector3 _headPosition;
        
        [Inject]
        public void Initialize(IVRMLoadable vrmLoadable, IMessageReceiver receiver)
        {
            vrmLoadable.VrmLoaded += info =>
            {
                _headPosition = info.animator.GetBoneTransform(HumanBodyBones.Head).position;
                _hasModel = true;
                UpdateCameraPose();
                ApplyReferenceHeadPositionIfAvailable();
            };

            vrmLoadable.VrmDisposing += () =>
            {
                _hasModel = false;
                _headPosition = Vector3.zero;
            };
            
            receiver.AssignCommandHandler(
                VmmCommands.Chromakey, 
                message =>
                {
                    var argb = message.ToColorFloats();
                    SetCameraBackgroundColor(argb[0], argb[1], argb[2], argb[3]);
                });

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

        public void SetCameraBackgroundColor(float a, float r, float g, float b) 
            => cam.backgroundColor = new Color(r, g, b, a);

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
                // どちらか片方だけでも不明な場合、
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

        private void ApplyReferenceHeadPositionIfAvailable()
        {
            if (_hasModel &&
                (_customCameraPosition.magnitude > Mathf.Epsilon ||
                _customCameraRotationEuler.magnitude > Mathf.Epsilon) &&
                !_hasReferenceHeadPosition
                )
            {
                _hasReferenceHeadPosition = true;
                _referenceHeadPosition = _headPosition;
            }
        }
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

