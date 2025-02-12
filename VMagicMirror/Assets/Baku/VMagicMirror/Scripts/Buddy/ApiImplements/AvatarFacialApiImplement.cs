using UniVRM10;

namespace Baku.VMagicMirror.Buddy
{
    // TODO: このクラスに「まばたきした」のイベントとか「FaceSwitchの状態」とかの情報も入れてしまいたい
    // (たぶん実装は全然違うので、その辺のステータス監視するクラスは別で生やすのが良いが)
    public class AvatarFacialApiImplement
    {
        private readonly IVRMLoadable _vrmLoadable;
        private readonly ExpressionAccumulator _expressionAccumulator;

        public AvatarFacialApiImplement(IVRMLoadable vrmLoadable, ExpressionAccumulator expressionAccumulator)
        {
            _vrmLoadable = vrmLoadable;
            _expressionAccumulator = expressionAccumulator;

            _vrmLoadable.VrmLoaded += OnVrmLoaded;
            _vrmLoadable.VrmDisposing += OnVrmUnloaded;
        }

        public bool IsLoaded { get; private set; }

        // NOTE: 下記の関数はモデルのロード前にfalse/0が戻るので、IsLoadedはチェックしないでよい
        public bool HasKey(string key, bool isCustomKey)
        {
            var expressionKey = CreateExpressionKey(key, isCustomKey);
            return _expressionAccumulator.HasKey(expressionKey);
        }

        public float GetBlendShapeValue(string key, bool isCustomKey)
        {
            var expressionKey = CreateExpressionKey(key, isCustomKey);
            return _expressionAccumulator.GetValue(expressionKey);
        }

        private void OnVrmLoaded(VrmLoadedInfo info)
        {
            IsLoaded = true;
        }

        private void OnVrmUnloaded()
        {
            IsLoaded = false;
        }

        private static ExpressionKey CreateExpressionKey(string key, bool isCustomKey)
        {
            if (isCustomKey)
            {
                return ExpressionKey.CreateCustom(key);
            }

            return key.ToLower() switch
            {
                nameof(ExpressionPreset.happy) => ExpressionKey.Happy,
                nameof(ExpressionPreset.angry) => ExpressionKey.Angry,
                nameof(ExpressionPreset.sad) => ExpressionKey.Sad,
                nameof(ExpressionPreset.relaxed) => ExpressionKey.Relaxed,
                nameof(ExpressionPreset.surprised) => ExpressionKey.Surprised,
                nameof(ExpressionPreset.aa) => ExpressionKey.Aa,
                nameof(ExpressionPreset.ih) => ExpressionKey.Ih,
                nameof(ExpressionPreset.ou) => ExpressionKey.Ou,
                nameof(ExpressionPreset.ee) => ExpressionKey.Ee,
                nameof(ExpressionPreset.oh) => ExpressionKey.Oh,
                nameof(ExpressionPreset.blink) => ExpressionKey.Blink,
                nameof(ExpressionPreset.blinkLeft) => ExpressionKey.BlinkLeft,
                nameof(ExpressionPreset.blinkRight) => ExpressionKey.BlinkRight,
                nameof(ExpressionPreset.lookUp) => ExpressionKey.LookUp,
                nameof(ExpressionPreset.lookDown) => ExpressionKey.LookDown,
                nameof(ExpressionPreset.lookLeft) => ExpressionKey.LookLeft,
                nameof(ExpressionPreset.lookRight) => ExpressionKey.LookRight,
                nameof(ExpressionPreset.neutral) => ExpressionKey.Neutral,
                // NOTE: 不明な名称の場合もエラーにするほどではない…という想定
                _ => ExpressionKey.Neutral,
            };
        }
    }
}
