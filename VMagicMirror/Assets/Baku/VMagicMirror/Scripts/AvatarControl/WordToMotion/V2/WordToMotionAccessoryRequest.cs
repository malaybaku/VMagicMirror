using R3;

namespace Baku.VMagicMirror.WordToMotion
{
    public class WordToMotionAccessoryRequest
    {
        private readonly ReactiveProperty<string> _accessoryRequest = new ReactiveProperty<string>("");
        public ReadOnlyReactiveProperty<string> AccessoryRequest => _accessoryRequest;

        public void SetAccessoryRequest(string fileId) => _accessoryRequest.Value = fileId;
        public void Reset() => SetAccessoryRequest("");
    }
}
