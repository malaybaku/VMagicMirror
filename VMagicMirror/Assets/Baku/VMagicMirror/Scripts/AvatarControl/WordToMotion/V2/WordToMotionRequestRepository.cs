using System;
using UniRx;

namespace Baku.VMagicMirror.WordToMotion
{
    public class WordToMotionRequestRepository
    {
        private ReactiveProperty<MotionRequest[]> _requests =
            new ReactiveProperty<MotionRequest[]>(Array.Empty<MotionRequest>());
        public IReadOnlyReactiveProperty<MotionRequest[]> Requests => _requests;

        public bool TryGet(int i, out MotionRequest result)
        {
            if (i < 0 || i >= _requests.Value.Length)
            {
                result = default;
                return false;
            }

            result = _requests.Value[i];
            return true;
        }
        
        public void Update(MotionRequest[] requests)
        {
            _requests.Value = requests ?? Array.Empty<MotionRequest>();
        }
    }
}
