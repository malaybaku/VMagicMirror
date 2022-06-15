namespace Baku.VMagicMirrorConfig.ViewModel.StreamingTabViewModels
{
    /// <summary>
    /// カスタムとは無関係にアクションが実行できる、お手軽バージョンのWtM機能を提供するViewModel
    /// </summary>
    public class InstantWtmViewModel : ViewModelBase
    {
        private static readonly MotionRequest NodRequest = new MotionRequest()
        {
            MotionType = MotionRequest.MotionTypeBuiltInClip,
            BuiltInAnimationClipName = "Nod",
        };

        private static readonly MotionRequest ShakeRequest = new MotionRequest()
        {
            MotionType = MotionRequest.MotionTypeBuiltInClip,
            BuiltInAnimationClipName = "Shake",
        };

        private static readonly MotionRequest ClapRequest = new MotionRequest()
        {
            MotionType = MotionRequest.MotionTypeBuiltInClip,
            BuiltInAnimationClipName = "Clap",
        };

        public InstantWtmViewModel() : this(ModelResolver.Instance.Resolve<WordToMotionSettingModel>())
        {
        }

        internal InstantWtmViewModel(WordToMotionSettingModel model)
        {
            _model = model;

            if (IsInDesignMode)
            {
                ClapCommand = ActionCommand.Empty;
                NodCommand = ActionCommand.Empty;
                ShakeCommand = ActionCommand.Empty; 
            }
            else
            {
                //NOTE: BlendShapeClipは使わないので空データでよい
                ClapCommand = new ActionCommand(() => Play(ClapRequest));
                NodCommand = new ActionCommand(() => Play(NodRequest));
                ShakeCommand = new ActionCommand(() => Play(ShakeRequest));
            }
        }

        private readonly WordToMotionSettingModel _model;

        public ActionCommand ClapCommand { get; }
        public ActionCommand NodCommand { get; }
        public ActionCommand ShakeCommand { get; }

        private void Play(MotionRequest request) => _model.Play(request);
    }
}