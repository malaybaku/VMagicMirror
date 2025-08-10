using System;
using UniGLTF.Extensions.VRMC_vrm;
using R3;
using UnityEngine;
using UniVRM10;
using UniVRM10.Migration;

namespace Baku.VMagicMirror
{
    /// <summary> VRMのメタ情報表示リクエストのブローカー </summary>
    public class VrmLoadProcessBroker
    {
        private readonly Subject<(Vrm0Meta, Texture2D)> _showVrm0MetaRequested = new Subject<(Vrm0Meta, Texture2D)>();
        public IObservable<(Vrm0Meta meta, Texture2D thumbnail)> ShowVrm0MetaRequested => _showVrm0MetaRequested;

        private readonly Subject<(Meta, Texture2D)> _showVrm1MetaRequested = new Subject<(Meta, Texture2D)>();
        public IObservable<(Meta meta, Texture2D thumbnail)> ShowVrm1MetaRequested => _showVrm1MetaRequested;

        private readonly Subject<(string modelId, Vrm10Instance instance, bool isVrm10)> _vroidModelLoaded
            = new Subject<(string modelId, Vrm10Instance instance, bool isVrm10)>();
        public IObservable<(string modelId, Vrm10Instance instance, bool isVrm10)> VRoidModelLoaded =>
            _vroidModelLoaded;

        private readonly Subject<Unit> _hideRequested = new Subject<Unit>();
        public IObservable<Unit> HideRequested => _hideRequested;
        
        public void RequestShowVrm0Meta(Vrm0Meta meta, Texture2D thumbnail) => _showVrm0MetaRequested.OnNext((meta, thumbnail));
        public void RequestShowVrm1Meta(Meta meta, Texture2D thumbnail) => _showVrm1MetaRequested.OnNext((meta, thumbnail));

        //NOTE: この関数はロード要求ではなくて「ロードしました」という報告っぽい感じ
        public void NotifyVRoidModelLoaded(string modelId, Vrm10Instance instance, bool isVrm10) 
            => _vroidModelLoaded.OnNext((modelId, instance, isVrm10));

        public void RequestHide() => _hideRequested.OnNext(Unit.Default);
    }
}
