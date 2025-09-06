using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace Baku.VMagicMirrorConfig.ViewModel
{
    public class OffsetClipViewModel : ViewModelBase
    {
        private readonly MotionSettingModel _model;
        private readonly FaceMotionBlendShapeNameStore _blendShapeNameStore;
        private bool _isSilentMode;

        public OffsetClipViewModel() : this(null!, null!) { }

        internal OffsetClipViewModel(
            MotionSettingModel model,
            FaceMotionBlendShapeNameStore blendShapeNameStore
            )
        {
            _model = model;
            _blendShapeNameStore = blendShapeNameStore;
            Items = new(_items);

            if (!IsInDesignMode)
            {
                _model.FaceOffsetClip.AddWeakEventHandler(OnModelOffsetClipChanged);

                WeakEventManager<INotifyCollectionChanged, NotifyCollectionChangedEventArgs>.AddHandler(
                    _blendShapeNameStore.BlendShapeNames,
                    nameof(INotifyCollectionChanged.CollectionChanged), 
                    OnBlendShapeNamesChanged
                    );

                RefreshItems();
            }
        }

        private readonly ObservableCollection<OffsetClipItemViewModel> _items = new();
        public ReadOnlyObservableCollection<OffsetClipItemViewModel> Items { get; }

        // NOTE: タブ文字区切りになってる事に関してはView側でよしなにしてもらう
        public RProperty<string> OffsetClipName => _model.FaceOffsetClip;

        private void OnBlendShapeNamesChanged(object? sender, NotifyCollectionChangedEventArgs e) => RefreshItems();

        private void OnModelOffsetClipChanged(object? sender, PropertyChangedEventArgs e) => RefreshSelectedItems();

        private void OnItemSelectedChanged(OffsetClipItemViewModel item)
        {
            DoSilently(() =>
            {
                if (string.IsNullOrEmpty(item.BlendShapeName) && item.Selected.Value)
                {
                    // 「なし」をオンにすると、他のぜんぶの選択が解除される
                    foreach (var item in _items.Where(i => !string.IsNullOrEmpty(i.BlendShapeName)))
                    {
                        item.Selected.Value = false;
                    }
                }
                else if (!string.IsNullOrEmpty(item.BlendShapeName) && item.Selected.Value)
                {
                    //「なし」以外をオンにすると、「なし」の選択が解除される
                    var noneItem = _items.FirstOrDefault(i => string.IsNullOrEmpty(i.BlendShapeName));
                    if (noneItem != null)
                    {
                        noneItem.Selected.Value = false;
                    }
                }
            });

            // NOTE: 「なし」だけが選択されてる場合には空文字列になるのがポイント
            _model.FaceOffsetClip.Value = string.Join(
                "\t",
                _items.Where(i => i.Selected.Value).Select(i => i.BlendShapeName).OrderBy(v => v, StringComparer.InvariantCulture)
                );
        }


        // BlendShapeの内訳が変わったときに呼ぶことでItemsを全て作り直す
        private void RefreshItems()
        {
            DoSilently(() =>
            {
                _items.Clear();
                foreach (var blendShapeName in _blendShapeNameStore.BlendShapeNames)
                {
                    var displayName = string.IsNullOrEmpty(blendShapeName) ? LocalizedString.GetString("CommonUi_None") : blendShapeName;
                    var item = new OffsetClipItemViewModel(blendShapeName, displayName);
                    item.Selected.PropertyChanged += (_, __) => OnItemSelectedChanged(item);
                    _items.Add(item);
                }
            });
            RefreshSelectedItems();
        }

        // Model側の状態が変化したときに呼ぶことで、各要素のOn/Offの状態をModelに合わせる
        private void RefreshSelectedItems() => DoSilently(() =>
        {
            var selectedNames = _model.FaceOffsetClip.Value.Split('\t');
            foreach (var item in _items)
            {
                if (string.IsNullOrEmpty(item.BlendShapeName))
                {
                    // 「なし」の適用条件だけContainsでは評価できないことに注意
                    item.Selected.Value = selectedNames.Length == 0;
                }
                else
                {
                    item.Selected.Value = selectedNames.Contains(item.BlendShapeName);
                }
            }
        });

        private void DoSilently(Action action)
        {
            if (_isSilentMode)
            {
                return;
            }

            _isSilentMode = true;
            try
            {
                action();
            }
            finally
            {
                _isSilentMode = false;
            }
        }
    }

    public class OffsetClipItemViewModel : ViewModelBase
    {
        public OffsetClipItemViewModel(string blendShapeName, string displayName)
        {
            BlendShapeName = blendShapeName;
            DisplayName = displayName;
        }

        public string BlendShapeName { get; }
        public string DisplayName { get; private set; }
        public RProperty<bool> Selected { get; } = new(false);
    }
}
