using System;
using UniRx;

namespace Baku.VMagicMirror.WordToMotion
{
    /// <summary>
    /// インデックス、あるいはリクエストのデータそのものの形でモーション開始を要求するやつ。
    /// プレビューの指示もこのクラスが行う。
    /// </summary>
    /// <remarks>
    /// 「プレビュー中はプレビュー以外のWtMは実行しない」というような時系列管理の責務は本クラスが持つ
    /// </remarks>
    public class WordToMotionRequester
    {
        private readonly WordToMotionRequestRepository _repository; 
        
        private bool _previewIsActive;
        private bool _hasPreviewRequest = false;
        private MotionRequest _previewRequest = default;
        
        private readonly Subject<MotionRequest> _runRequested = new Subject<MotionRequest>();
        public IObservable<MotionRequest> RunRequested => _runRequested;

        private readonly Subject<Unit> _stopRequested = new Subject<Unit>();
        public IObservable<Unit> StopRequested => _stopRequested;

        private readonly Subject<MotionRequest> _previewFacialRequested = new Subject<MotionRequest>();
        // プレビューモード中、表情は「いじらないでいい」 or 「全クリップ情報」のいずれかであることを、値を受け取るたびに通知したい
        public IObservable<MotionRequest> PreviewFacialRequested => _previewFacialRequested;

        private readonly Subject<MotionRequest> _previewMotionRequested = new Subject<MotionRequest>();
        // プレビューモード中、モーションは指示内容が変化した場合だけ通知したい
        public IObservable<MotionRequest> PreviewMotionRequested => _previewMotionRequested;

        public SourceType SourceType { get; set; } = SourceType.KeyboardTyping;
        
        public WordToMotionRequester(WordToMotionRequestRepository repository)
        {
            _repository = repository;
        }

        /// <summary>
        /// ゲームパッド入力などによって、インデックスベースでWtMを実行したい時に呼ぶ
        /// </summary>
        /// <param name="index"></param>
        /// <param name="sourceType"></param>
        public void Play(int index, SourceType sourceType)
        {
            if (!_previewIsActive && _repository.TryGet(index, out var request))
            {
                _runRequested.OnNext(request);
            }
        }

        /// <summary>
        /// インデックス指定ではなく、IPCで直接内容が指定されたWord to Motionの実行時に呼ぶ
        /// </summary>
        /// <param name="request"></param>
        public void Play(MotionRequest request)
        {
            if (!_previewIsActive)
            {
                _runRequested.OnNext(request);
            }
        }
        
        public void SetPreviewActive(bool active)
        {
            if (active == _previewIsActive)
            {
                return;
            }

            _previewIsActive = active;
            if (active)
            {
                _stopRequested.OnNext(Unit.Default);
            }
            else
            {
                _hasPreviewRequest = false;
            }
        }
        
        public void SetPreviewRequest(MotionRequest request)
        {
            _hasPreviewRequest = true;
        }
    }
}
