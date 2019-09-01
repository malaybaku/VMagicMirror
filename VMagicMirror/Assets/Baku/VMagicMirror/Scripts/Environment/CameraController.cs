using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace Baku.VMagicMirror
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField]
        private ReceivedMessageHandler handler = null;

        [SerializeField]
        private Camera _cam = null;

        [SerializeField]
        private CameraTransformController _transformController = null;

        private Vector3 _defaultCameraPosition = Vector3.zero;
        private Vector3 _defaultCameraRotationEuler = Vector3.zero;

        private Vector3 _customCameraPosition = Vector3.zero;
        private Vector3 _customCameraRotationEuler = Vector3.zero;

        public bool IsInFreeCameraMode { get; private set; } = false;

        public Vector3 BaseCameraPosition => _customCameraPosition;

        void Start()
        {
            _defaultCameraPosition = _cam.transform.position;
            _defaultCameraRotationEuler = _cam.transform.rotation.eulerAngles;

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
                    default:
                        break;
                }
            });
            handler.QueryRequested += (_, e) =>
            {
                switch (e.Query.Command)
                {
                    case MessageQueryNames.CurrentCameraPosition:
                        var angles = _cam.transform.rotation.eulerAngles;
                        e.Query.Result = JsonUtility.ToJson(new SerializedCameraPosition(new float[]
                        {
                            _cam.transform.position.x,
                            _cam.transform.position.y,
                            _cam.transform.position.z,
                            angles.x,
                            angles.y,
                            angles.z,
                        }));
                        break;
                    default:
                        break;
                }
            };
        }

        private void SetCameraBackgroundColor(float a, float r, float g, float b)
        {
            _cam.backgroundColor = new Color(r, g, b, a);
        }

        private void EnableFreeCameraMode(bool v)
        {
            IsInFreeCameraMode = v;
            _transformController.enabled = v;
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
                        .Select(w => float.Parse(w))
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
                _cam.transform.position = _customCameraPosition;
                _cam.transform.rotation = Quaternion.Euler(_customCameraRotationEuler);
            }
            else
            {
                _cam.transform.position = _defaultCameraPosition;
                _cam.transform.rotation = Quaternion.Euler(_defaultCameraRotationEuler);
            }
        }

        private void SetCameraFov(int fovDeg)
        {
            _cam.fieldOfView = fovDeg;
        }


    }

    [Serializable]
    public class SerializedCameraPosition
    {
        public List<float> values = new List<float>();

        public SerializedCameraPosition(float[] v)
        {
            if (v != null)
            {
                for(int i = 0; i < v.Length; i++)
                {
                    values.Add(v[i]);
                }
            }
        }
    }
}

