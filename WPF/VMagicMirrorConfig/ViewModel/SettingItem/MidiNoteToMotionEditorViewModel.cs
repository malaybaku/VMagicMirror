namespace Baku.VMagicMirrorConfig.ViewModel
{

    public class MidiNoteToMotionEditorViewModel : ViewModelBase
    {
        internal MidiNoteToMotionEditorViewModel(
            MidiNoteToMotionMapViewModel current,
            MidiNoteReceiver? midiNoteReceiver
            )
        {
            Current = current;
            After = new MidiNoteToMotionMapViewModel(current.Model.CreateCopy());
            ResetToCurrentSettingCommand
                = new ActionCommand(ResetToCurrentSetting);

            MidiNoteReceiver = midiNoteReceiver;
        }

        //NOTE: これはちょっとトリッキーで、View側で使います
        internal MidiNoteReceiver? MidiNoteReceiver { get; }

        public MidiNoteToMotionMapViewModel Current { get; }
        public MidiNoteToMotionMapViewModel After { get; }
        public MidiNoteToMotionMap Result => After.Save();

        public ActionCommand ResetToCurrentSettingCommand { get; }
        private void ResetToCurrentSetting()
        {
            After.Load(Current.Model.CreateCopy());
        }
    }
}
