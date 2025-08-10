using System;
using ModestTree;
using R3;

namespace Baku.VMagicMirror.WordToMotion
{
    public class WordToMotionRequestRepository
    {
        private readonly ReactiveProperty<MotionRequest[]> _requests =
            new ReactiveProperty<MotionRequest[]>(Array.Empty<MotionRequest>());
        public ReadOnlyReactiveProperty<MotionRequest[]> Requests => _requests;

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

        public int FindIndex(string word)
        {
            return Array.FindIndex(_requests.Value, v => v.Word == word);
        }
    }
}
