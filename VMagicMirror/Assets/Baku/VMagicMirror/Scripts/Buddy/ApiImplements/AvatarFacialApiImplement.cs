using System;
using Baku.VMagicMirror.ExternalTracker;
using UniRx;
using UniVRM10;

namespace Baku.VMagicMirror.Buddy
{
    // TODO: このクラスに「まばたきした」のイベントとか「FaceSwitchの状態」とかの情報も入れてしまいたい
    // (たぶん実装は全然違うので、その辺のステータス監視するクラスは別で生やすのが良いが)
    public class AvatarFacialApiImplement
    {
        private readonly IVRMLoadable _vrmLoadable;
        private readonly ExpressionAccumulator _expressionAccumulator;
        private readonly BlinkDetector _blinkDetector;
        private readonly VoiceOnOffParser _voiceOnOffParser;
        
        // NOTE: 重要な注意点として、現時点では設定がカラ(「何もしない」)になってるFaceSwitchは条件を満たしても検出できない。
        // - つまり、漫符みたいなサブキャラを作るのがちょっとムズい
        // - 「アバターの表情が実際変わるかどうか」という観点で見れば検出できないのも納得感はある
        private readonly ExternalTrackerDataSource _externalTrackerDataSource;
        private readonly FaceSwitchExtractor _faceSwitchExtractor;
        private readonly FaceControlConfiguration _faceControlConfig;
        private readonly UserOperationBlendShapeResultRepository _userOperationBlendShapeResultRepository;

        public AvatarFacialApiImplement(
            IVRMLoadable vrmLoadable,
            ExpressionAccumulator expressionAccumulator,
            ExternalTrackerDataSource externalTrackerDataSource,
            FaceSwitchExtractor faceSwitchExtractor,
            FaceControlConfiguration faceControlConfig,
            UserOperationBlendShapeResultRepository userOperationBlendShapeResultRepository,
            BlinkDetector blinkDetector,
            VoiceOnOffParser voiceOnOffParser)
        {
            _vrmLoadable = vrmLoadable;
            _expressionAccumulator = expressionAccumulator;
            _externalTrackerDataSource = externalTrackerDataSource;
            _faceSwitchExtractor = faceSwitchExtractor;
            _faceControlConfig = faceControlConfig;
            _userOperationBlendShapeResultRepository = userOperationBlendShapeResultRepository;
            _blinkDetector = blinkDetector;
            _voiceOnOffParser = voiceOnOffParser;

            _vrmLoadable.VrmLoaded += OnVrmLoaded;
            _vrmLoadable.VrmDisposing += OnVrmUnloaded;
        }

        public bool IsLoaded { get; private set; }

        public bool UsePerfectSync => _faceControlConfig.UsePerfectSync;

        public IReadOnlyReactiveProperty<bool> IsTalking => _voiceOnOffParser.IsTalking;
        
        // TODO: 「BuddyがBlinkedを購読するまではBlinkDetectorを止めておく」みたいなガードが出来たら嬉しい
        // Blinkに関してはパフォーマンス影響が小さそうだが、他所でも応用が効きそうなので何かは考えてほしい
        public IObservable<Unit> Blinked => _blinkDetector.Blinked();

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

        /// <summary>
        /// Word to Motion / Face Switchによって適用された表情のブレンドシェイプがある場合、または
        /// VMC Protocolで「喜怒哀楽 + 驚き」のいずれの表情が適用されている場合、
        /// その中でも値がもっとも大きいものを取得する。
        /// 上記以外の、リップシンク、まばたき、パーフェクトシンクで動いているブレンドシェイプについては、
        /// 値が大きくても本メソッドの戻り値にはならない。
        ///
        /// この値はアバターとサブキャラの表情を連動させる目的で使うのが想定されている。
        /// </summary>
        /// <returns></returns>
        public ExpressionKey? GetUserOperationActiveBlendShape()
        {
            if (_userOperationBlendShapeResultRepository.HasActiveKey)
            {
                return _userOperationBlendShapeResultRepository.ActiveKey;
            }
            else
            {
                return null;
            }
        }
        
        //TODO: これは削除してGetUserOperationActiveBlendShapeだけにする…というのもアリかもしれない
        // Buddyの実装してみてから考えるのがヨサソウ。

        /// <summary>
        /// FaceSwitch機能の検出状態を取得する。
        /// 戻り値は既定のstringのいくつか、またはFaceSwitchが適用中でなければ空文字列になる。
        /// 
        /// Word to Motion機能で表情を適用している間は、
        /// この値が "" 以外の値を返すが実際には表情は適用されていない…という状況も起こり得る。
        /// </summary>
        /// <returns></returns>
        public string GetActiveFaceSwitch()
        {
            if (!_externalTrackerDataSource.Connected)
            {
                return "";
            }

            return ConvertFaceSwitchActionToName(_faceSwitchExtractor.ActiveItem.Action);
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

        private static string ConvertFaceSwitchActionToName(FaceSwitchAction action)
        {
            // NOTE: None以外は実質ToString()してるのと変わらないが、
            // enumのrenameでAPIが壊れないようにしたいので(一見冗長でも)リテラルに変換してる
            return action switch
            {
                FaceSwitchAction.MouthSmile => "MouthSmile",
                FaceSwitchAction.EyeSquint => "EyeSquint",
                FaceSwitchAction.EyeWide => "EyeWide",
                FaceSwitchAction.BrowUp => "BrowUp",
                FaceSwitchAction.BrowDown => "BrowDown",
                FaceSwitchAction.CheekPuff => "CheekPuff",
                FaceSwitchAction.TongueOut => "TongueOut",
                _ => "",
            };
        }
    }
}
