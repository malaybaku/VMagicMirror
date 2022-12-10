using System;
using System.Collections.Generic;
using mattatz.TransformControl;
using UnityEngine;
using UniVRM10;

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

        private static readonly Vector3 DefaultPosition = new Vector3(0f, 0.05f, 0.17f);
        private static readonly Vector3 DefaultRotationEuler = Vector3.zero;
        private static readonly Vector3 DefaultScale = Vector3.one;
        
        [SerializeField] private Renderer imageRenderer;
        [SerializeField] private Transform modelParent;
        [SerializeField] private TransformControl transformControl;
        
        public AccessoryItemLayout ItemLayout { get; private set; }
        public string FileId => _file?.FileId ?? "";

        private AccessoryFile _file = null;
        private IAccessoryFileActions _fileActions = null;
        private Camera _cam = null;

        private Animator _animator = null;
        private readonly Dictionary<AccessoryAttachTarget, Transform> _attachBones 
            = new Dictionary<AccessoryAttachTarget, Transform>();

        private bool _firstEnabledCalled;
        public Action<AccessoryItem> FirstEnabled;
        
        private bool _visibleByWordToMotion;
        public bool VisibleByWordToMotion
        {
            get => _visibleByWordToMotion;
            set
            {
                if (_visibleByWordToMotion != value)
                {
                    _visibleByWordToMotion = value;
                    SetVisibility(ShouldBeVisible);
                }
            }
        }

        private bool _visibleByFaceSwitch;
        public bool VisibleByFaceSwitch
        {
            get => _visibleByFaceSwitch;
            set
            {
                if (_visibleByFaceSwitch != value)
                {
                    _visibleByFaceSwitch = value;
                    SetVisibility(ShouldBeVisible);
                }
            }
        }

        public bool ShouldBeVisible
        {
            get
            {
                if (_file == null || _animator == null || ItemLayout == null)
                {
                    return false;
                }

                return 
                    ItemLayout.IsVisible ||
                    VisibleByWordToMotion ||
                    VisibleByFaceSwitch;
            }
        }
        
        /// <summary>
        /// Unity上でTransformControlによってレイアウトが編集されており、その変更WPFに送信されていない場合にtrueになるフラグ
        /// </summary>
        public bool HasLayoutChange { get; private set; }

        private bool ShouldAdjustBillboard => ShouldBeVisible && ItemLayout.UseBillboardMode;

        public void ConfirmLayoutChange() => HasLayoutChange = false;

        public void LoadContent()
        {
            try
            {
                _file.LoadBinary();
                switch (_file.Type)
                {
                    case AccessoryType.Png:
                        InitializeImage(_file);
                        break;
                    case AccessoryType.Glb:
                        var glbContext = AccessoryFileReader.LoadGlb(_file.FilePath, _file.Bytes);
                        var glbObj = glbContext.Object;
                        glbObj.transform.SetParent(modelParent);
                        _fileActions = glbContext.Actions;
                        break;
                    case AccessoryType.Gltf:
                        var gltfContext = AccessoryFileReader.LoadGltf(_file.FilePath, _file.Bytes);
                        var gltfObj = gltfContext.Object;
                        gltfObj.transform.SetParent(modelParent);
                        _fileActions = gltfContext.Actions;
                        break;
                    case AccessoryType.NumberedPng:
                        InitializeAnimatableImage(_file);
                        break;
                    default:
                        LogOutput.Instance.Write($"WARN: Tried to load unknown data, id={_file.FileId}");
                        break;
                }
            }
            finally
            {
                //NOTE: glb / glTFの場合、バイナリって破棄しても安全なんだっけ…？(安全そうには見えるが)
                _file.ReleaseBinary();
            }
        }
        
        /// <summary>
        /// ビルボード動作時に参照するカメラ、およびファイルを指定してアイテムをロードします。
        /// </summary>
        /// <param name="cam"></param>
        /// <param name="file"></param>
        public void Initialize(Camera cam, AccessoryFile file)
        {
            _cam = cam;
            _file = file;

            SetVisibility(false);
        }

        //ファイル等から動的ロードしたものも含めて、アクセサリのリソースを解放し、ゲームオブジェクトを破棄します。
        public void Dispose()
        {
            _fileActions?.Dispose();
            Destroy(gameObject);
        }

        private void Start()
        {
            transformControl.DragEnded += UpdateLayout;
        }

        private void Update()
        {
            if (ShouldBeVisible)
            {
                if (!_firstEnabledCalled)
                {
                    FirstEnabled?.Invoke(this);
                    _firstEnabledCalled = true;
                }

                _fileActions.Update(Time.deltaTime);
            }
        }
        
        private void LateUpdate()
        {
            UpdateIfBillboard();
            if (ShouldAdjustBillboard)
            {
                transformControl.RequestUpdateGizmo();
            }
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
            _fileActions = context.Actions;
            
            imageRenderer.material.mainTexture = tex;
            SetImageRendererAspect(tex);
        }

        private void InitializeAnimatableImage(AccessoryFile file)
        {
            var context = AccessoryFileReader.LoadNumberedPngImage(file.Binaries);
            context.Object.Renderer = imageRenderer;
            _fileActions = context.Actions;
            
            var tex = context.Object.FirstTexture;
            imageRenderer.material.mainTexture = tex;
            SetImageRendererAspect(tex);
        }

        private void SetImageRendererAspect(Texture2D tex)
        {
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
            _fileActions?.OnVisibilityChanged(visible);
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
                case AccessoryType.NumberedPng:
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
            _fileActions?.UpdateLayout(layout);
            if (_animator == null)
            {
                return;
            }

            //glb/gltfは本質的に3Dなんだから2Dモードは不要、と考えて弾く。
            //カメラのnear clipを突き抜けてヘンなことになるのを防ぐ狙いもある
            if (ItemLayout.UseBillboardMode && 
                (_file.Type != AccessoryType.Png && _file.Type != AccessoryType.NumberedPng))
            {
                ItemLayout.UseBillboardMode = false;
            }
            //ビルボードモードではLateUpdateでアイテムを動かすときがGizmoの更新タイミングになるので、手動更新にする
            transformControl.AutoUpdateGizmo = !ItemLayout.UseBillboardMode;
            transformControl.XyPlaneMode = ItemLayout.UseBillboardMode;
            
            SetVisibility(ShouldBeVisible);
            
            //アイテムとして不可視であっても位置をあわせる: 後から表情経由で表示されるかもしれないため。
            if (!_attachBones.TryGetValue(ItemLayout.AttachTarget, out var bone))
            {
                // ワールド固定の場合、ここを通過してSetParent(null)が呼ばれることでワールドに固定される
                if (ItemLayout.AttachTarget != AccessoryAttachTarget.World)
                {
                    return;
                }
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
        /// ロードされたVRMのAnimatorを指定し、アイテムをモデルの特定部位にアタッチできるようにします。
        /// </summary>
        /// <param name="animator"></param>
        public void SetAnimator(Animator animator)
        {
            _animator = animator;

            _attachBones[AccessoryAttachTarget.Head] = animator.GetBoneTransform(HumanBodyBones.Head);
            _attachBones[AccessoryAttachTarget.Neck] = 
                animator.GetBoneTransform(HumanBodyBones.Neck) ?? animator.GetBoneTransform(HumanBodyBones.Head);
            _attachBones[AccessoryAttachTarget.Chest] = animator.GetBoneTransform(HumanBodyBones.Chest);
            _attachBones[AccessoryAttachTarget.Waist] = animator.GetBoneTransform(HumanBodyBones.Hips);
            _attachBones[AccessoryAttachTarget.LeftHand] = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            _attachBones[AccessoryAttachTarget.RightHand] = animator.GetBoneTransform(HumanBodyBones.RightHand);

            if (_file == null)
            {
                return;
            }
            
            //アイテムレイアウトのロード→モデルロード、という順序で操作したときのための措置
            if (ItemLayout != null)
            {
                SetLayout(ItemLayout);
            }

            SetVisibility(ShouldBeVisible);
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
            if (_animator == null && ItemLayout == null)
            {
                return;
            }

            transformControl.global = request.WorldCoordinate;
            //NOTE: 表情やモーションに付随して一瞬表示されるような状態に対しては位置編集UIは出さない
            var visible = ItemLayout.IsVisible;
            transformControl.mode = visible ? request.Mode : TransformControl.TransformMode.None;

            if (!visible)
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
            Transform bone = null;
            if (ItemLayout == null || 
                _animator == null || 
                (ItemLayout.AttachTarget != AccessoryAttachTarget.World &&
                 !_attachBones.TryGetValue(ItemLayout.AttachTarget, out bone)
                ))
            {
                return;
            }

            var hasBone = bone != null;
            var boneForward = hasBone ? bone.forward : Vector3.forward;
            var bonePosition = hasBone ? bone.position : Vector3.zero;
            var boneRightY = hasBone ? bone.right.y : 0f;
            
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

                    var plane = new Plane(boneForward, bonePosition + boneForward * ItemLayout.Position.z);

                    //手にアタッチするケースではボーンのXY平面が動きすぎて難しいので、
                    //諦めてワールドのXY平面方向でやる
                    if (ItemLayout.AttachTarget == AccessoryAttachTarget.LeftHand ||
                        ItemLayout.AttachTarget == AccessoryAttachTarget.RightHand)
                    {
                        plane = new Plane(Vector3.forward, bonePosition);
                    }

                    if (!plane.Raycast(ray, out var enter))
                    {
                        return;
                    }

                    var dst = ray.origin + ray.direction * enter;
                    ItemLayout.Position = hasBone ? bone.InverseTransformPoint(dst) : dst;
                    break;
                
                case TransformControl.TransformMode.Rotate:

                    if (!ItemLayout.UseBillboardMode)
                    {
                        ItemLayout.Rotation = transform.localRotation.eulerAngles;
                        break;
                    }

                    //NOTE: ビルボードモードではXY軸には回せないようになるので、Z軸回転のみ考慮する

                    //カメラと正対する方向に対しての、このオブジェクトのローカル回転
                    var diff = Quaternion.Inverse(_cam.transform.rotation * Quaternion.Euler(0, 180, 0)) * transform.rotation;
                    diff.ToAngleAxis(out var angle, out var axis);
                    var camForward = _cam.transform.forward;
                    //XY軸方向の編集は禁止してるから、必ずcamera.forwardかその逆を軸にした回転となる。
                    //Vector3.Dotの結果はほぼ1.0かほぼ-1.0のどちらか
                    if (Vector3.Dot(camForward, axis) > 0f)
                    {
                        angle = -angle;
                    }
                    angle = MathUtil.ClampAngle(angle);

                    var roll = boneRightY * Mathf.Rad2Deg;
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
            if (!ShouldAdjustBillboard)
            {
                return;
            }

            if (transformControl.IsDragging)
            {
                //NOTE: ここをガードすると一時的にrotationとかz軸方向の移動がヘンになる事があるが、それは許容する
                return;
            }

            //マイナーケースで「2DアクセサリをWorld固定で最前面表示しようとしている」というのがあり、それをガードしている
            //これによって描画が崩れる可能性があるが、GUI側で警告を出すことでユーザーに直してもらう想定
            if (!_attachBones.TryGetValue(ItemLayout.AttachTarget, out var bone) || 
                bone == null)
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
                //NOTE: 180度ひっくり返すのは、カメラに対して正面向きにする必要があるため
                Quaternion.Euler(0, 180, ItemLayout.Rotation.z);
        }
    }
}
