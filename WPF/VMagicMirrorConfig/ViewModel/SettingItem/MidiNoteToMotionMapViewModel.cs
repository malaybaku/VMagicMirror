using System.Collections.ObjectModel;

namespace Baku.VMagicMirrorConfig.ViewModel
{
    public class MidiNoteToMotionMapViewModel : ViewModelBase
    {
        public MidiNoteToMotionMapViewModel(MidiNoteToMotionMap model)
        {
            Items = new ReadOnlyObservableCollection<MidiNoteToMotionItemViewModel>(_items);
            Model = model;
            Load(model);
        }

        public MidiNoteToMotionMap Model { get; private set; }

        private readonly ObservableCollection<MidiNoteToMotionItemViewModel> _items
            = new ObservableCollection<MidiNoteToMotionItemViewModel>();
        public ReadOnlyObservableCollection<MidiNoteToMotionItemViewModel> Items { get; }

        public void Load(MidiNoteToMotionMap model)
        {
            Model = model;
            _items.Clear();
            foreach (var i in model.Items)
            {
                _items.Add(new MidiNoteToMotionItemViewModel()
                {
                    ItemIndex = i.ItemIndex,
                    NoteNumber = i.NoteNumber,
                });
            }
        }

        public MidiNoteToMotionMap Save()
        {
            var result = new MidiNoteToMotionMap();
            foreach (var i in Items)
            {
                result.Items.Add(new MidiNoteToMotionItem()
                {
                    ItemIndex = i.ItemIndex,
                    NoteNumber = i.NoteNumber,
                });
            }
            return result;
        }

    }


    public class MidiNoteToMotionItemViewModel : ViewModelBase
    {
        private int _itemIndex = 0;
        public int ItemIndex
        {
            get => _itemIndex;
            set => SetValue(ref _itemIndex, value);
        }

        private int _noteNumber = -1;
        public int NoteNumber
        {
            get => _noteNumber;
            set
            {
                bool validNow = HasValidNoteNumber;

                if (SetValue(ref _noteNumber, value) &&
                    validNow != HasValidNoteNumber
                    )
                {
                    RaisePropertyChanged(nameof(HasValidNoteNumber));
                }
            }
        }

        public bool HasValidNoteNumber => _noteNumber >= 0;
    }
}
