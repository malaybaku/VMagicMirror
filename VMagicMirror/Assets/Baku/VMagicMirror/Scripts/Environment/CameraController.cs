using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace Baku.VMagicMirror
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private ReceivedMessageHandler handler = null;
        [SerializeField] private Camera cam = null;
        [SerializeField] private CameraTransformController transformController = null;

        private Vector3 _defaultCameraPosition = Vector3.zero;
        private Vector3 _defaultCameraRotationEuler = Vector3.zero;

        private Vector3 _customCameraPosition = Vector3.zero;
        private Vector3 _customCameraRotationEuler = Vector3.zero;

        public bool IsInFreeCameraMode { get; private set; }

        public Vector3 BaseCameraPosition => _customCameraPosition;

        private void Start()
        {
            var camTransform = cam.transform;
            _defaultCameraPosition = camTransform.position;
            _defaultCameraRotationEuler = camTransform.rotation.eulerAngles;

            handler.Commands.Subscribe(message =>
            {
                switch(message.Command)
                {
                    case MessageCommandNames.Chromakey:
                        var argb = message.ToColorFloats();
                        SetCameraBackgroundColor(argb[0], argb[1], argb[2], argb[3]);
                        break;
                    case MessageCommandNames.EnableFreeCameraMode:
                        EnableFreeCameraMode(message.ToBoolean());
                        break;
                    case MessageCommandNames.SetCustomCameraPosition:
                        SetCustomCameraPosition(message.Content);
                        break;
                    case MessageCommandNames.ResetCameraPosition:
                        ResetCameraPosition();
                        break;
                    case MessageCommandNames.CameraFov:
                        SetCameraFov(message.ToInt());
                        break;
                }
            });
            handler.QueryRequested += query =>
            {
                if (query.Command == MessageQueryNames.CurrentCameraPosition)
                {
                    var t = cam.transform;
                    var angles = t.rotation.eulerAngles;
                    var pos = t.position;
                    query.Result = JsonUtility.ToJson(new SerializedCameraPosition(new[]
                    {
                        pos.x,
                        pos.y,
                        pos.z,
                        angles.x,
                        angles.y,
                        angles.z,
                    }));
                }
            };
        }

        private void SetCameraBackgroundColor(float a, float r, float g, float b)
        {
            cam.backgroundColor = new Color(r, g, b, a);
        }

        private void EnableFreeCameraMode(bool v)
        {
            IsInFreeCameraMode = v;
            transformController.enabled = v;
        }

        private void ResetCameraPosition()
        {
            _customCameraPosition = _defaultCameraPosition;
            _customCameraRotationEuler = _defaultCameraRotationEuler;
            UpdateCameraTransform(true);
        }


        private void SetCustomCameraPosition(string content)
        {
            //note: ここはv0.9.0以降ではシリアライズしたJson、それより前ではfloatのカンマ区切り配列。
            //Jsonシリアライズを導入したのは、OSのロケールによっては(EU圏とかで)floatの小数点が","になる問題をラクして避けるため。
            float[] values = new float[0];
            try
            {
                //シリアライズ: 普通はこれで通る
                values = JsonUtility.FromJson<SerializedCameraPosition>(content)
                    .values
                    .ToArray();
            }
            catch(Exception ex)
            {
                LogOutput.Instance.Write(ex);
            }

            if (values.Length != 6)
            {
                try
                {
                    //旧バージョンから設定をインポートしたときはこれでうまくいく
                    values = content.Split(',')
                        .Select(float.Parse)
                        .ToArray();
                }
                catch (Exception ex)
                {
                    LogOutput.Instance.Write(ex);
                }
            }

            if (values.Length != 6)
            {
                return;
            }

            //ぜんぶ0な場合、無効値として無視
            if (values.All(v => Mathf.Abs(v) < Mathf.Epsilon))
            {
                return;
            }

            _customCameraPosition = new Vector3(
                values[0], values[1], values[2]
                );

            _customCameraRotationEuler = new Vector3(
                values[3], values[4], values[5]
                );

            UpdateCameraTransform(false);
        }

        private void UpdateCameraTransform(bool forceUpdateInFreeCameraMode)
        {
            if (IsInFreeCameraMode && !forceUpdateInFreeCameraMode)
            {
                return;
            }

            //NOTE: WPF側がきちんと設定を持ってないと6DoF=0,0,0,0,0,0を指定したようになるため、そのときに入力を無視する
            if (_customCameraPosition.magnitude > Mathf.Epsilon ||
                _customCameraRotationEuler.magnitude > Mathf.Epsilon
                )
            {
                cam.transform.position = _customCameraPosition;
                cam.transform.rotation = Quaternion.Euler(_customCameraRotationEuler);
            }
            else
            {
                cam.transform.position = _defaultCameraPosition;
                cam.transform.rotation = Quaternion.Euler(_defaultCameraRotationEuler);
            }
        }

        private void SetCameraFov(int fovDeg)
        {
            cam.fieldOfView = fovDeg;
        }


    }

    [Serializable]
    public class SerializedCameraPosition
    {
        public List<float> values = new List<float>();

        public SerializedCameraPosition(float[] v)
        {
            if (v == null)
            {
                return;
            }
            
            for(int i = 0; i < v.Length; i++)
            {
                values.Add(v[i]);
            }
        }
    }
}

