using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;

namespace Baku.VMagicMirror.WordToMotion
{
    public class WordToMotionPresenter : PresenterBase
    {
        private readonly IMessageReceiver _receiver;
        private readonly WordToMotionRequestRepository _repository;
        private readonly CustomMotionRepository _customMotionRepository;
        private readonly WordToMotionRequester _requester;
        private WordToMotionRunner _runner;
        private readonly IRequestSource[] _sources;

        public WordToMotionPresenter(
            IMessageReceiver receiver,
            WordToMotionRequestRepository repository,
            CustomMotionRepository customMotionRepository,
            WordToMotionRequester requester,
            WordToMotionRunner runner,
            IEnumerable<IRequestSource> sources
            )
        {
            _receiver = receiver;
            _repository = repository;
            _customMotionRepository = customMotionRepository;
            _requester = requester;
            _runner = runner;
            _sources = sources.ToArray();
        }

        public override void Initialize()
        {
            _receiver.AssignCommandHandler(
                VmmCommands.ReloadMotionRequests,
                message => ReloadMotionRequests(message.Content)
            );
            _receiver.AssignCommandHandler(
                VmmCommands.PlayWordToMotionItem,
                message => PlayWordToMotionItem(message.Content)
            );
            
            _receiver.AssignCommandHandler(
                VmmCommands.EnableWordToMotionPreview,
                message => _requester.SetPreviewActive(message.ToBoolean())
            );
            
            _receiver.AssignCommandHandler(
                VmmCommands.SendWordToMotionPreviewInfo,
                message => ReceiveWordToMotionPreviewInfo(message.Content)
            );
            
            _receiver.AssignCommandHandler(
                VmmCommands.SetDeviceTypeToStartWordToMotion,
                message => SetWordToMotionInputType(message.ToInt())
            );

            //未実装なので一旦なしで
            // _receiver.AssignCommandHandler(
            //     VmmCommands.RequestCustomMotionDoctor,
            //     _ => { }
            // );
            
            _receiver.AssignQueryHandler(
                VmmQueries.GetAvailableCustomMotionClipNames,
                q =>
                {
                    q.Result = string.Join("\t", _customMotionRepository.LoadAvailableCustomMotionNames());
                    Debug.Log("Get Available CustomMotion Clip Names, result = " + q.Result);
                });

            //リクエストは等価に扱う && 頻度は制御する、という話
            _sources
                .Select(source => source.RunMotionRequested.Select(index => (index, source.SourceType)))
                .Merge()
                .ThrottleFirst(TimeSpan.FromSeconds(0.3f))
                .Subscribe(value => _requester.Play(value.index, value.SourceType))
                .AddTo(this);

            _requester.PreviewIsActive
                .Subscribe(previewIsActive =>
                {
                    if (previewIsActive)
                    {
                        _runner.EnablePreview();
                    }
                    else
                    {
                        _runner.StopPreview();
                    }
                })
                .AddTo(this);
            
            _requester.RunRequested
                .Subscribe(_runner.Run)
                .AddTo(this);
            _requester.StopRequested
                .Subscribe(_ => _runner.Stop())
                .AddTo(this);

            _requester.PreviewRequested
                .Subscribe(_runner.RunAsPreview)
                .AddTo(this);
        }
        
        private void SetWordToMotionInputType(int deviceType)
        {
            var type = (SourceType) deviceType;
            foreach (var source in _sources)
            {
                source.SetActive(type == source.SourceType);
            }
        }

        private void ReloadMotionRequests(string json)
        {
            try
            {
                _repository.Update(JsonUtility.FromJson<MotionRequestCollection>(json).Requests);
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);
            }
        }

        private void PlayWordToMotionItem(string json)
        {
            try
            {
                _requester.Play(JsonUtility.FromJson<MotionRequest>(json));
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);
            }
        }

        private void ReceiveWordToMotionPreviewInfo(string json)
        {
            try
            {
                _requester.SetPreviewRequest(JsonUtility.FromJson<MotionRequest>(json));
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);
            }
        }
    }
}
