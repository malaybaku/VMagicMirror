using System;
using R3;

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
        
        private readonly ReactiveProperty<bool> _previewIsActive = new ReactiveProperty<bool>(false);
        public ReadOnlyReactiveProperty<bool> PreviewIsActive => _previewIsActive;

        private readonly Subject<MotionRequest> _runRequested = new Subject<MotionRequest>();
        public Observable<MotionRequest> RunRequested => _runRequested;

        private readonly Subject<MotionRequest> _previewRequested = new Subject<MotionRequest>();
        // プレビューモード中、表情は「いじらないでいい」 or 「全クリップ情報」のいずれかであることを、値を受け取るたびに通知したい
        public Observable<MotionRequest> PreviewRequested => _previewRequested;

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
            if (!_previewIsActive.Value && _repository.TryGet(index, out var request))
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
            if (!_previewIsActive.Value)
            {
                _runRequested.OnNext(request);
            }
        }
        
        public void SetPreviewActive(bool active)
        {
            if (active == _previewIsActive.Value)
            {
                return;
            }

            _previewIsActive.Value = active;
        }
        
        public void SetPreviewRequest(MotionRequest request)
        {
            _previewRequested.OnNext(request);
        }
    }
}
