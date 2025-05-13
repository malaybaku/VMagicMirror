using Microsoft.Win32;
using System;
using System.Collections.Generic;

namespace Baku.VMagicMirrorConfig.ViewModel
{
    // NOTE: VMの型をPropertyと同じだけ分けたほうが、余計なプロパティがViewに露出しないぶんキレイになる
    // ただし、そこまで頑張らんでも…みたいな話もあるため、現時点では1クラスに押し込んでいる
    public class BuddyPropertyViewModel
    {
        public BuddyPropertyViewModel(
            BuddySettingsSender settingSender,
            BuddyMetadata buddyMetadata, 
            BuddyProperty buddyProperty
            ) 
        {
            _settingSender = settingSender;
            _buddyMetadata = buddyMetadata;
            _metadata = buddyProperty.Metadata;
            _value = buddyProperty.Value;

            switch (_metadata.ValueType)
            {
                case BuddyPropertyType.Bool:
                    BoolValue = new RProperty<bool>(_value.BoolValue, v =>
                    {
                        _value.BoolValue = v;
                        _settingSender.NotifyBoolProperty(_buddyMetadata, _metadata, v);
                    });
                    break;
                case BuddyPropertyType.Int:
                    IntValue = new RProperty<int>(_value.IntValue, v =>
                    {
                        _value.IntValue = v;
                        _settingSender.NotifyIntProperty(_buddyMetadata, _metadata, v);
                    });
                    break;
                case BuddyPropertyType.Float:
                    FloatValue = new RProperty<float>(_value.FloatValue, v =>
                    {
                        _value.FloatValue = v;
                        _settingSender.NotifyFloatProperty(_buddyMetadata, _metadata, v);
                    });
                    break;
                case BuddyPropertyType.String:
                case BuddyPropertyType.FilePathString:
                    StringValue = new RProperty<string>(_value.StringValue, v =>
                    {
                        _value.StringValue = v;
                        _settingSender.NotifyStringProperty(_buddyMetadata, _metadata, v);
                    });
                    break;
                case BuddyPropertyType.Vector2:
                    Vector2Value = new RProperty<BuddyVector2>(_value.Vector2Value, v =>
                    {
                        _value.Vector2Value = v;
                        _settingSender.NotifyVector2Property(_buddyMetadata, _metadata, v);
                    });
                    VectorX = new RProperty<float>(
                        _value.Vector2Value.X,
                        v => Vector2Value.Value = _value.Vector2Value.WithX(v)
                        );
                    VectorY = new RProperty<float>(
                        _value.Vector2Value.Y,
                        v => Vector2Value.Value = _value.Vector2Value.WithY(v)
                        );
                    break;
                case BuddyPropertyType.Vector3:
                case BuddyPropertyType.Quaternion:
                    // NOTE: QuaternionとVector3はViewModel上での見せ方は完全に一致する
                    Vector3Value = new RProperty<BuddyVector3>(_value.Vector3Value, v =>
                    {
                        _value.Vector3Value = v;
                        _settingSender.NotifyVector3Property(_buddyMetadata, _metadata, v);
                    });
                    VectorX = new RProperty<float>(
                        _value.Vector3Value.X,
                        v => Vector3Value.Value = _value.Vector3Value.WithX(v)
                        );
                    VectorY = new RProperty<float>(
                        _value.Vector3Value.Y,
                        v => Vector3Value.Value = _value.Vector3Value.WithY(v)
                        );
                    VectorZ = new RProperty<float>(
                        _value.Vector3Value.Z,
                        v => Vector3Value.Value = _value.Vector3Value.WithZ(v)
                        );
                    break;
                case BuddyPropertyType.Transform2D:
                    Transform2DValue = new BuddyTransform2DPropertyViewModel(_settingSender, _buddyMetadata, buddyProperty);
                    break;
                case BuddyPropertyType.Transform3D:
                    Transform3DValue = new BuddyTransform3DPropertyViewModel(_settingSender, _buddyMetadata, buddyProperty);
                    break;
                case BuddyPropertyType.Action:
                    // Actionでは値の準備はしないでOKで、その代わりInvokeActionCommandが呼ばれるようになる
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        private readonly BuddySettingsSender _settingSender;
        private readonly BuddyMetadata _buddyMetadata;
        private readonly BuddyPropertyMetadata _metadata;
        private readonly BuddyPropertyValue _value;

        public BuddyPropertyType VisualType => _metadata.VisualType;

        // NOTE: この2つはローカライズの対象であり、ApplyLanguage() が呼ばれると実際にローカライズに基づいた値が入る
        public RProperty<string> DisplayName { get; } = new("");
        public RProperty<string> Description { get; } = new("");
        
        // 下記のRPropertyのうち、_metadata.ValueTypeに基づいてどれか一つだけが実際に使われる
        public RProperty<bool> BoolValue { get; } = new RProperty<bool>(false);
        public RProperty<int> IntValue { get; } = new RProperty<int>(0);
        public RProperty<float> FloatValue { get; } = new RProperty<float>(0);
        public RProperty<string> StringValue { get; } = new RProperty<string>("");
        public RProperty<BuddyVector2> Vector2Value { get; } = new RProperty<BuddyVector2>(new BuddyVector2());
        public RProperty<BuddyVector3> Vector3Value { get; } = new RProperty<BuddyVector3>(new BuddyVector3());

        // ViewからはBuddyVector2やVector3に対してではなく、下記のベクトルの要素単位でアクセスする
        public RProperty<float> VectorX { get; } = new RProperty<float>(0f);
        public RProperty<float> VectorY { get; } = new RProperty<float>(0f);
        public RProperty<float> VectorZ { get; } = new RProperty<float>(0f);
        //NOTE: プロパティの実態がTransform2DやTransform3Dの場合以外はnull
        public BuddyTransform2DPropertyViewModel? Transform2DValue { get; }
        public BuddyTransform3DPropertyViewModel? Transform3DValue { get; }

        private ActionCommand? _invokeActionCommand;
        public ActionCommand InvokeActionCommand => _invokeActionCommand ??= new ActionCommand(InvokeAction);
        private void InvokeAction() => _settingSender.InvokeBuddyAction(_buddyMetadata, _metadata);

        private ActionCommand? _setFilePathStringByDialogCommand;
        public ActionCommand SetFilePathStringByDialogCommand => _setFilePathStringByDialogCommand ??= new ActionCommand(SetFilePathStringByDialog);
        private void SetFilePathStringByDialog()
        {
            var isJapanese = LanguageSelector.Instance.LanguageName == LanguageSelector.LangNameJapanese;
            // NOTE: タイトルやファイルのFilter設定をカスタムするように拡張してもいいかも (フィルターを書き損じると困るが…)
            var dialog = new OpenFileDialog()
            {
                Title = $"Select File: {DisplayName.Value}",
                Filter = "All files (*.*)|*.*",
                Multiselect = false,
            };

            if (dialog.ShowDialog() == true)
            {
                return;
            }

            // NOTE: CurrentDirectoryの場所を特に保証してないのでフルパスに寄せとく
            var fullPath = System.IO.Path.GetFullPath(dialog.FileName);
            if (System.IO.File.Exists(fullPath))
            StringValue.Value = fullPath;
        }


        public int IntRangeMin => _metadata.IntRangeMin;
        public int IntRangeMax => _metadata.IntRangeMax;
        public float FloatRangeMin => _metadata.FloatRangeMin;
        public float FloatRangeMax => _metadata.FloatRangeMax;
        public IReadOnlyList<string> EnumOptions => _metadata.EnumOptions;

        public void ApplyLanguage(bool isJapanese)
        {
            DisplayName.Value = _metadata.DisplayName.Get(isJapanese);
            Description.Value = _metadata.Description.Get(isJapanese);

            Transform2DValue?.ApplyLanguage(isJapanese);
            Transform3DValue?.ApplyLanguage(isJapanese);
        }

        public void ResetToDefault()
        {
            switch (_metadata.ValueType)
            {
                case BuddyPropertyType.Bool:
                    BoolValue.Value = _metadata.DefaultBoolValue;
                    return;
                case BuddyPropertyType.Int:
                    IntValue.Value = _metadata.DefaultIntValue;
                    return;
                case BuddyPropertyType.Float:
                    FloatValue.Value = _metadata.DefaultFloatValue;
                    return;
                case BuddyPropertyType.String:
                    StringValue.Value = _metadata.DefaultStringValue;
                    return;
                case BuddyPropertyType.Vector2:
                    // NOTE: 直接Vector2Valueだけ直してもX,Yの値が更新されないので、成分単位で処理する(V3も同様)
                    // このやり方だとメッセージが2回飛んでしまうが、防止するほどではないはず…ということで、あまり気にしてない
                    VectorX.Value = _metadata.DefaultVector2Value.X;
                    VectorY.Value = _metadata.DefaultVector2Value.Y;
                    return;
                case BuddyPropertyType.Vector3:
                case BuddyPropertyType.Quaternion:
                    VectorX.Value = _metadata.DefaultVector3Value.X;
                    VectorY.Value = _metadata.DefaultVector3Value.Y;
                    VectorZ.Value = _metadata.DefaultVector3Value.Z;
                    break;
                case BuddyPropertyType.Transform2D:
                    if (Transform2DValue == null)
                    {
                        // NOTE: コーディングエラーでのみ発生
                        throw new InvalidOperationException("Transform2D reset requested, but property is not specified");
                    }
                    Transform2DValue.ResetToDefault();
                    break;
                case BuddyPropertyType.Transform3D:
                    if (Transform3DValue == null)
                    {
                        // NOTE: コーディングエラーでのみ発生
                        throw new InvalidOperationException("Transform3D reset requested, but property is not specified");
                    }
                    Transform3DValue.ResetToDefault();
                    break;
                default:
                    throw new NotSupportedException();
            }

        }
    }
}
