using System.Collections.Generic;
using mattatz.TransformControl;
using UnityEngine;

namespace Baku.VMagicMirror
{
    public class AccessoryItem : MonoBehaviour
    {
        //画像データは長いほうの辺が5-20cmくらいに見えるような使い方がメインじゃないかな、と考えて値を決めてる
        private const float ImageLengthDefault = 0.12f;
        //ユーザーが編集したスケールが1.0, 
        private const float BillboardScaleFactor = 0.1f;
        //カメラのnear clipが0.05なので、それより大きく、かつ十分小さめの値にする
        private const float BillboardZ = 0.1f;

        private static readonly AccessoryAttachTarget DefaultTarget = AccessoryAttachTarget.Head;
        private static readonly Vector3 DefaultPosition = new Vector3(0f, 0.05f, 0.17f);
        private static readonly Vector3 DefaultRotationEuler = Vector3.zero;
        private static readonly Vector3 DefaultScale = Vector3.one;
        
        [SerializeField] private Renderer imageRenderer;
        [SerializeField] private Transform modelParent;
        [SerializeField] private TransformControl transformControl;
        
        public AccessoryItemLayout ItemLayout { get; private set; }
        public string FileId => _file?.FileId ?? "";

        private AccessoryFile _file = null;
        private AccessoryFileDisposer _disposer = null;
        private Camera _cam = null;

        private Animator _animator = null;
        private readonly Dictionary<AccessoryAttachTarget, Transform> _attachBones =
            new Dictionary<AccessoryAttachTarget, Transform>();


        /// <summary>
        /// Unity上でTransformControlによってレイアウトが編集されており、その変更WPFに送信されていない場合にtrueになるフラグ
        /// </summary>
        public bool HasLayoutChange { get; private set; }

        public void ConfirmLayoutChange() => HasLayoutChange = false;

        /// <summary>
        /// ビルボード動作時に参照するカメラ、およびファイルを指定してアイテムをロードします。
        /// </summary>
        /// <param name="cam"></param>
        /// <param name="file"></param>
        public void Initialize(Camera cam, AccessoryFile file)
        {
            _cam = cam;
            _file = file;
            switch (file.Type)
            {
                case AccessoryType.Png:
                    InitializeImage(file);
                    break;
                case AccessoryType.Glb:
                    var glbContext = AccessoryFileReader.LoadGlb(file.FilePath, file.Bytes);
                    var glbObj = glbContext.Object;
                    glbObj.transform.SetParent(modelParent);
                    _disposer = glbContext.Disposer;
                    break;
                case AccessoryType.Gltf:
                    var gltfContext = AccessoryFileReader.LoadGltf(file.FilePath, file.Bytes);
                    var gltfObj = gltfContext.Object; 
                    gltfObj.transform.SetParent(modelParent);
                    _disposer = gltfContext.Disposer;
                    break;
                default:
                    LogOutput.Instance.Write($"WARN: Tried to load unknown data, id={_file.FileId}");
                    break;
            }
            SetVisibility(false);
        }

        //ファイル等から動的ロードしたものも含めて、アクセサリのリソースを解放し、ゲームオブジェクトを破棄します。
        public void Dispose()
        {
            _disposer?.Dispose();
            Destroy(gameObject);
        }

        private void Start()
        {
            transformControl.DragEnded += UpdateLayout;
        }
        
        private void LateUpdate()
        {
            UpdateIfBillboard();
        }

        private void OnDestroy()
        {
            if (transformControl != null)
            {
                transformControl.DragEnded -= UpdateLayout;
            }
        }

        private void InitializeImage(AccessoryFile file)
        {
            var context = AccessoryFileReader.LoadPngImage(file.Bytes);
            var tex = context.Object;
            _disposer = context.Disposer;
            
            imageRenderer.material.mainTexture = tex;
            if (tex.width < tex.height)
            {
                var aspect = tex.width * 1.0f / tex.height;
                imageRenderer.transform.localScale =
                    new Vector3(ImageLengthDefault * aspect, ImageLengthDefault, 1f);
            }
            else
            {
                var aspect = tex.height * 1.0f / tex.width;
                imageRenderer.transform.localScale = 
                    new Vector3(ImageLengthDefault, ImageLengthDefault * aspect, 1f);
            }
        }

        private void SetVisibility(bool visible)
        {
            if (_file == null || !visible)
            {
                imageRenderer.gameObject.SetActive(false);
                modelParent.gameObject.SetActive(false);
                transformControl.mode = TransformControl.TransformMode.None;
                return;
            }

            switch (_file.Type)
            {
                case AccessoryType.Png:
                    imageRenderer.gameObject.SetActive(true);
                    modelParent.gameObject.SetActive(false);
                    break;
                case AccessoryType.Glb:
                case AccessoryType.Gltf:
                    imageRenderer.gameObject.SetActive(false);
                    modelParent.gameObject.SetActive(true);
                    break;
                default:
                    imageRenderer.gameObject.SetActive(false);
                    modelParent.gameObject.SetActive(false);
                    break;
            }
        }

        /// <summary>
        /// レイアウト情報を指定することで、アクセサリを所定の位置に移動します。
        /// WPF側の起動直後を含めて、WPF側の操作でレイアウトが変化したときに呼び出します。
        /// </summary>
        /// <param name="layout"></param>
        public void SetLayout(AccessoryItemLayout layout)
        {
            HasLayoutChange = false;
            ItemLayout = layout;
            if (_animator == null)
            {
                return;
            }

            SetVisibility(ItemLayout.IsVisible);
            if (!ItemLayout.IsVisible)
            {
                return;
            }

            transformControl.rotateOnlyZ = ItemLayout.UseBillboardMode;

            if (!_attachBones.TryGetValue(ItemLayout.AttachTarget, out var bone))
            {
                return;
            }
            
            if (ItemLayout.UseBillboardMode)
            {
                transform.SetParent(null);
                UpdateIfBillboard();
            }
            else
            {
                var t = transform;
                t.SetParent(bone);
                t.localPosition = ItemLayout.Position;
                t.localRotation= Quaternion.Euler(ItemLayout.Rotation);
                //NOTE: parentにスケールが効いてる可能性は考慮しない
                t.localScale = ItemLayout.Scale;
            }
        }

        public void ResetLayout()
        {
            if (ItemLayout == null)
            {
                return;
            }
            
            //※Animatorが無い場合も数字を更新することに注意。
            //特に、初めて読み込まれるアイテムは必ずモデルがロードされる前に読まれるので、
            //「モデルが未ロードだからリセットしません」というのはNG。
            
            //リセット時だけはvisibilityとかtargetも更新する
            ItemLayout.IsVisible = true;
            ItemLayout.UseBillboardMode = false;
            ItemLayout.AttachTarget = AccessoryAttachTarget.Head;
            ItemLayout.Position = DefaultPosition;
            ItemLayout.Rotation = DefaultRotationEuler;
            ItemLayout.Scale = DefaultScale;
            //この時点で明示的にレイアウトは更新し、WPFがリセット後のレイアウトを再送信しないでも良くする。
            SetLayout(ItemLayout);
            HasLayoutChange = true;
        }

        /// <summary>
        /// ロードされたVRMを指定してアイテムをアタッチできるようにします。
        /// </summary>
        /// <param name="info"></param>
        public void SetModel(VrmLoadedInfo info)
        {
            _animator = info.animator;

            if (_file == null)
            {
                return;
            }

            _attachBones[AccessoryAttachTarget.Head] = _animator.GetBoneTransform(HumanBodyBones.Head);
            _attachBones[AccessoryAttachTarget.Neck] = 
                _animator.GetBoneTransform(HumanBodyBones.Neck) ?? _animator.GetBoneTransform(HumanBodyBones.Head);
            _attachBones[AccessoryAttachTarget.Chest] = _animator.GetBoneTransform(HumanBodyBones.Chest);
            _attachBones[AccessoryAttachTarget.Waist] = _animator.GetBoneTransform(HumanBodyBones.Hips);
            _attachBones[AccessoryAttachTarget.LeftHand] = _animator.GetBoneTransform(HumanBodyBones.LeftHand);
            _attachBones[AccessoryAttachTarget.RightHand] = _animator.GetBoneTransform(HumanBodyBones.RightHand);

            //アイテムレイアウトのロード→モデルロード、という順序で操作したときのための措置
            if (ItemLayout != null)
            {
                SetLayout(ItemLayout);
            }
            SetVisibility(ItemLayout?.IsVisible == true);
        }

        /// <summary>
        /// VRMがアンロードされたとき呼び出すことで、参照を外します。
        /// </summary>
        public void UnsetModel()
        {
            transform.SetParent(null);
            _animator = null;
            _attachBones.Clear();
            transformControl.mode = TransformControl.TransformMode.None;
            SetVisibility(false);
        }

        /// <summary>
        /// TransformControlによるアップデートをしたいとき、毎フレーム呼び出す
        /// </summary>
        /// <param name="request"></param>
        public void ControlItemTransform(TransformControlRequest request)
        {
            if (_animator == null)
            {
                return;
            }

            transformControl.global = request.WorldCoordinate;
            transformControl.mode = ItemLayout.IsVisible ? request.Mode : TransformControl.TransformMode.None;

            if (!ItemLayout.IsVisible)
            {
                return;
            }

            transformControl.Control();

            //スケールについては1軸だけいじったとき、残りの2軸を追従させる
            var scale = transform.localScale;
            if (Mathf.Abs(scale.x - scale.y) > Mathf.Epsilon ||
                Mathf.Abs(scale.y - scale.z) > Mathf.Epsilon ||
                Mathf.Abs(scale.z - scale.x) > Mathf.Epsilon
            )
            {
                //3つの値から1つだけ仲間はずれになっている物があるはずなので、それを探す
                var b1 = Mathf.Abs(scale.x - scale.y) > Mathf.Epsilon;
                var b2 = Mathf.Abs(scale.z - scale.x) > Mathf.Epsilon;

                var nextScale = scale.x;
                if (!b1)
                {
                    nextScale = scale.z;
                }
                else if (!b2)
                {
                    nextScale = scale.y;
                }
                //上記以外はxだけズレてる or 全軸バラバラのケースなため、x軸を使う
                transform.localScale = Vector3.one * nextScale;
            }
        }

        /// <summary>
        /// フリーレイアウトモードを終了するとき呼び出すことで、TransformControlを非表示にします。
        /// </summary>
        public void EndControlItemTransform()
        {
            transformControl.mode = TransformControl.TransformMode.None;
        }
        
        //Unity上でTransformControlによって改変したPosition/Rotation/Scaleがある場合に呼び出すことで、layoutを更新します。
        private void UpdateLayout(TransformControl.TransformMode mode)
        {
            if (ItemLayout == null || 
                _animator == null || 
                !_attachBones.TryGetValue(ItemLayout.AttachTarget, out var bone))
            {
                return;
            }

            HasLayoutChange = true;
            switch (mode)
            {
                case TransformControl.TransformMode.Translate:
                    if (!ItemLayout.UseBillboardMode)
                    {
                        ItemLayout.Position = transform.localPosition;
                        break;
                    }
                    
                    //やること: カメラとアイテムの中心でレイを作り、zオフセットを尊重しつつボーン基準のXY平面にぶつける
                    var ct = _cam.transform;
                    var cPos = ct.position;
                    var ray = new Ray(cPos, transform.position - cPos);

                    var boneForward = bone.forward;
                    var plane = new Plane(boneForward, bone.position + boneForward * ItemLayout.Position.z);

                    //手にアタッチするケースではボーンのXY平面が動きすぎて難しいので、
                    //諦めてワールドのXY平面方向でやる
                    if (ItemLayout.AttachTarget == AccessoryAttachTarget.LeftHand ||
                        ItemLayout.AttachTarget == AccessoryAttachTarget.RightHand)
                    {
                        plane = new Plane(Vector3.forward, bone.position);
                    }

                    if (!plane.Raycast(ray, out var enter))
                    {
                        return;
                    }

                    var dst = ray.origin + ray.direction * enter;
                    ItemLayout.Position = bone.InverseTransformPoint(dst);
                    break;
                
                case TransformControl.TransformMode.Rotate:

                    if (!ItemLayout.UseBillboardMode)
                    {
                        ItemLayout.Rotation = transform.localRotation.eulerAngles;
                        break;
                    }

                    //NOTE: ビルボードモードではXY軸には回せないようになるので、Z軸回転のみ考慮する
                    //カメラに対する、このオブジェクトのローカル回転
                    var diff = Quaternion.Inverse(_cam.transform.rotation) * transform.rotation;
                    //XY軸方向の編集が禁止されていると仮定するとZ軸成分だけ残ってるはず
                    diff.ToAngleAxis(out var angle, out _);
                    angle = MathUtil.ClampAngle(angle);

                    var roll = Mathf.Asin(bone.right.y) * Mathf.Rad2Deg;
                    if (ItemLayout.AttachTarget == AccessoryAttachTarget.LeftHand ||
                        ItemLayout.AttachTarget == AccessoryAttachTarget.RightHand)
                    {
                        roll = 0f;
                    }

                    var rot = ItemLayout.Rotation;
                    rot.z = angle - roll;
                    ItemLayout.Rotation = rot;
                    break;
                case TransformControl.TransformMode.Scale:
                    var rawScale = transform.localScale.x;
                    ItemLayout.Scale = Vector3.one * (
                        ItemLayout.UseBillboardMode ? rawScale / BillboardScaleFactor : rawScale
                        );
                    break;
                default:
                    //何もしない: ここは通らないはず
                    break;
            }
        }
        
        private void UpdateIfBillboard()
        {
            if (_animator == null || ItemLayout == null || !ItemLayout.UseBillboardMode)
            {
                return;
            }

            if (transformControl.IsDragging)
            {
                //NOTE: ここをガードすると一時的にrotationとかz軸方向の移動がヘンになる事があるが、それは許容する
                return;
            }

            var bone = _attachBones[ItemLayout.AttachTarget];
            if (bone == null)
            {
                return;
            }

            transform.localScale = ItemLayout.Scale * BillboardScaleFactor;
            
            //ビルボードの位置決め: ビルボードじゃなかった場合と画像の中心位置が揃うように合わせつつ、
            //画面のほぼ最前面に持ってくる
            var camTransform = _cam.transform;
            var camPos = camTransform.position;
            var direction = (bone.TransformPoint(ItemLayout.Position) - camPos).normalized;

            var dot = Vector3.Dot(camTransform.forward, direction);
            if (dot < 0.01)
            {
                //カメラのほぼ横～後ろに対象ボーンが回り込んでるケース: 見えないのが正しいはずなので、強引に隠す
                transform.SetPositionAndRotation(
                    camTransform.TransformPoint(Vector3.back),
                    camTransform.rotation
                );
                return;
            }

            transform.position = camPos + direction * (BillboardZ / dot);
            
            //回転は手とそれ以外で反映する軸を変える。
            //というか、手は参照すべき軸が非自明で難しいため、深追いしない
            if (ItemLayout.AttachTarget == AccessoryAttachTarget.LeftHand ||
                ItemLayout.AttachTarget == AccessoryAttachTarget.RightHand)
            {
                transform.rotation = camTransform.rotation * Quaternion.Euler(0, 0, ItemLayout.Rotation.z);
                return;
            }

            //大まかに正面付近を向いているのを正面付近から撮っている、と仮定して合わせるとこんなもん
            var boneRoll = -Mathf.Asin(bone.right.y) * Mathf.Rad2Deg;
            transform.rotation = 
                Quaternion.AngleAxis(boneRoll, camTransform.forward) *
                camTransform.rotation *
                Quaternion.Euler(0, 0, ItemLayout.Rotation.z);
        }
    }
}
