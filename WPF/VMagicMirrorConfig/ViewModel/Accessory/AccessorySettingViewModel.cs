using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Baku.VMagicMirrorConfig
{
    public class AccessorySettingViewModel : ViewModelBase
    {
        internal AccessorySettingViewModel(AccessorySettingSync model, LayoutSettingSync layoutModel)
        {
            Items = new ReadOnlyObservableCollection<AccessoryItemViewModel>(_items);
            _model = model;
            _model.Loaded += (_, __) => OnLoaded();
            _model.ItemRefreshed += () => OnLoaded();
            _layoutModel = layoutModel;

            ResetCommand = new ActionCommand(ResetToDefault);
            OpenAccessoryFolderCommand = new ActionCommand(OpenAccessoryFolder);
            ReloadFilesCommand = new ActionCommand(ReloadFiles);
            OpenAccessoryTipsUrlCommand = new ActionCommand(OpenAccessoryTipsUrl);
        }

        private readonly AccessorySettingSync _model;
        private readonly LayoutSettingSync _layoutModel;

        private readonly ObservableCollection<AccessoryItemViewModel> _items 
            = new ObservableCollection<AccessoryItemViewModel>();
        public ReadOnlyObservableCollection<AccessoryItemViewModel> Items { get; }

        public RProperty<bool> EnableDeviceFreeLayout => _layoutModel.EnableDeviceFreeLayout;

        public ActionCommand OpenAccessoryFolderCommand { get; }
        public ActionCommand ReloadFilesCommand { get; }
        public ActionCommand OpenAccessoryTipsUrlCommand { get; }
        public ActionCommand ResetCommand { get; }

        private void OnLoaded()
        {
            foreach(var item in _items)
            {
                item.Dispose();
            }
            _items.Clear();

            for(int i = 0; i < _model.Items.Items.Length; i++)
            {
                _items.Add(new AccessoryItemViewModel(_model, i));
            }
        }

        private void OpenAccessoryFolder()
        {
            if (Directory.Exists(SpecialFilePath.AccessoryFileDir))
            {
                Process.Start(new ProcessStartInfo(SpecialFilePath.AccessoryFileDir)
                {
                    UseShellExecute = true,
                });
            }
        }

        private void ReloadFiles() => _model.RefreshFiles();
        private void OpenAccessoryTipsUrl() => UrlNavigate.Open(LocalizedString.GetString("URL_docs_accessory"));
        private void ResetToDefault() => SettingResetUtils.ResetSingleCategoryAsync(_model.ResetToDefault);
    }

    //NOTE: ViewModelは設定ファイルを1回読み込むとか、明示的にリロードが要求されるとかの時点で捨てて再生成する。
    //再生成のタイミング = model側もアイテムリロードしてそうなタイミング、という感じ
    public class AccessoryItemViewModel : ViewModelBase
    {
        public static string[] AvailableAttachTargets { get; }
        static AccessoryItemViewModel()
        {
            //NOTE: 多言語化したいかどうか微妙なライン…ぶっちゃけ多言語化したくない…
            AvailableAttachTargets = Enum.GetValues<AccessoryAttachTarget>()
                .Select(e => e.ToString())
                .ToArray();
        }


        internal AccessoryItemViewModel(AccessorySettingSync model, int index)
        {
            _model = model;
            _item = model.Items.Items[index];
            _file = model.Files.FirstOrDefault(f => f.FileId == _item.FileId);
            _model.ItemUpdated += OnItemUpdated;
            ResetCommand = new ActionCommand(Reset);

            FileName = _item.FileId;
            Name = new RProperty<string>(_item.Name, v =>
            {
                _item.Name = v;
                UpdateItemFromUi();
            });
            IsVisible = new RProperty<bool>(_item.IsVisible, v =>
            {
                _item.IsVisible = v;
                UpdateItemFromUi();
            });
            UseBillboardMode = new RProperty<bool>(_item.UseBillboardMode, v =>
            {
                _item.UseBillboardMode = v;
                UpdateItemFromUi();
            });
            AttachTarget = new RProperty<int>((int)_item.AttachTarget, v =>
            {
                _item.AttachTarget = (AccessoryAttachTarget)v;
                UpdateItemFromUi();
            });

            PosX = new RProperty<float>(_item.Position.X, v =>
            {
                _item.Position = _item.Position.WithX(v);
                UpdateItemFromUi();
            });
            PosY = new RProperty<float>(_item.Position.Y, v =>
            {
                _item.Position = _item.Position.WithY(v);
                UpdateItemFromUi();
            });
            PosZ = new RProperty<float>(_item.Position.Z, v =>
            {
                _item.Position = _item.Position.WithZ(v);
                UpdateItemFromUi();
            });

            RotX = new RProperty<float>(_item.Rotation.X, v =>
            {
                _item.Rotation = _item.Rotation.WithX(v);
                UpdateItemFromUi();
            });
            RotY = new RProperty<float>(_item.Rotation.Y, v =>
            {
                _item.Rotation = _item.Rotation.WithY(v);
                UpdateItemFromUi();
            });
            RotZ = new RProperty<float>(_item.Rotation.Z, v =>
            {
                _item.Rotation = _item.Rotation.WithZ(v);
                UpdateItemFromUi();
            });

            Scale = new RProperty<float>(_item.Scale.X, v =>
            {
                _item.Scale = new Vector3(v, v, v);
                UpdateItemFromUi();
            });

            FramePerSecond = new RProperty<int>(_item.FramePerSecond, v =>
            {
                _item.FramePerSecond = v;
                UpdateItemFromUi();
            });
        }

        private readonly AccessorySettingSync _model;
        private readonly AccessoryItemSetting _item;
        private readonly AccessoryFile? _file;

        private bool _isUpdatingByReceivedData;

        public string FileName { get; }
        //3Dモデルはビルボードモード使う必要ない(万が一フラグが立っててもUnity側で無視させる)
        public bool CanSelectBillboardMode =>
            _file?.Type == AccessoryType.Png || _file?.Type == AccessoryType.NumberedPng;

        public bool CanEditFramePerSecond => _file?.Type == AccessoryType.NumberedPng;

        public RProperty<string> Name { get; }
        public RProperty<bool> IsVisible { get; }
        public RProperty<bool> UseBillboardMode { get; }
        public RProperty<int> AttachTarget { get; }

        public RProperty<float> PosX { get; }
        public RProperty<float> PosY { get; }
        public RProperty<float> PosZ { get; }

        public RProperty<float> RotX { get; }
        public RProperty<float> RotY { get; }
        public RProperty<float> RotZ { get; }

        public RProperty<float> Scale { get; }

        public RProperty<int> FramePerSecond { get; }

        public ActionCommand ResetCommand { get; }

        //NOTE: Unityのコンポーネントリセットと同じノリで良いはずのため、確認ダイアログは無し。
        //手触りがまずかったらダイアログを挟むか、またはUI側をContextMenu化で「確認してるよ」感を出す
        private void Reset() => _model.RequestReset(_item.FileId);

        private void OnItemUpdated(AccessoryItemSetting item)
        {
            if (item != _item)
            {
                return;
            }

            _isUpdatingByReceivedData = true;

            Name.Value = _item.Name;
            IsVisible.Value = _item.IsVisible;
            UseBillboardMode.Value = _item.UseBillboardMode;
            AttachTarget.Value = (int)_item.AttachTarget;
            PosX.Value = _item.Position.X;
            PosY.Value = _item.Position.Y;
            PosZ.Value = _item.Position.Z;
            RotX.Value = _item.Rotation.X;
            RotY.Value = _item.Rotation.Y;
            RotZ.Value = _item.Rotation.Z;
            Scale.Value = _item.Scale.X;
            FramePerSecond.Value = _item.FramePerSecond;

            _isUpdatingByReceivedData = false;
        }

        private void UpdateItemFromUi()
        {
            if (!_isUpdatingByReceivedData)
            {
                _model.UpdateItemFromUi(_item);
            }
        }

        public void Dispose()
        {
            _model.ItemUpdated -= OnItemUpdated;
        }
    }
}
