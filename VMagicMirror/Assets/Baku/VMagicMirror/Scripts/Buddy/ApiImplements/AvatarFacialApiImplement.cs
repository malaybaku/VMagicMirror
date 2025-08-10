using System;
using System.Collections.Generic;
using Baku.VMagicMirror.ExternalTracker;
using R3;
using UniVRM10;

namespace Baku.VMagicMirror.Buddy
{
    public class AvatarFacialApiImplement
    {
        private readonly BuddySettingsRepository _buddySettingsRepository;
        private readonly IVRMLoadable _vrmLoadable;
        private readonly ExpressionAccumulator _expressionAccumulator;
        private readonly BlinkDetector _blinkDetector;
        private readonly VoiceOnOffParser _voiceOnOffParser;
        
        // NOTE: 現時点では設定がカラ(「何もしない」)になってるFaceSwitchは条件を満たしても検出できない。
        // - つまり、漫符みたいなサブキャラはちょっと作りづらい
        // - 「アバターの表情が実際変わるかどうか」という観点で見れば検出できないのも納得感はある
        private readonly FaceSwitchExtractor _faceSwitchExtractor;
        private readonly FaceControlConfiguration _faceControlConfig;
        private readonly UserOperationBlendShapeResultRepository _userOperationBlendShapeResultRepository;

        private readonly HashSet<object> _microphoneRecordRequireBuddies = new();
        
        public AvatarFacialApiImplement(
            BuddySettingsRepository buddySettingsRepository,
            ExpressionAccumulator expressionAccumulator,
            FaceSwitchExtractor faceSwitchExtractor,
            FaceControlConfiguration faceControlConfig,
            UserOperationBlendShapeResultRepository userOperationBlendShapeResultRepository,
            BlinkDetector blinkDetector,
            VoiceOnOffParser voiceOnOffParser)
        {
            _buddySettingsRepository = buddySettingsRepository;
            _expressionAccumulator = expressionAccumulator;
            _faceSwitchExtractor = faceSwitchExtractor;
            _faceControlConfig = faceControlConfig;
            _userOperationBlendShapeResultRepository = userOperationBlendShapeResultRepository;
            _blinkDetector = blinkDetector;
            _voiceOnOffParser = voiceOnOffParser;
        }

        private bool InteractionApiEnabled => _buddySettingsRepository.InteractionApiEnabled.Value;
        
        private readonly ReactiveProperty<bool> _requireMicrophoneRecording = new();
        public IReadOnlyReactiveProperty<bool> RequireMicrophoneRecording => _requireMicrophoneRecording;

        public bool UsePerfectSync => _faceControlConfig.PerfectSyncActive;

        public IReadOnlyReactiveProperty<bool> IsTalking => _voiceOnOffParser.IsTalking;
        
        // TODO: 「BuddyがBlinkedを購読するまではBlinkDetectorを止めておく」みたいなガードが出来たら嬉しい
        // Blinkに関してはパフォーマンス影響が小さそうだが、他所でも応用が効きそうなので何かは考えてほしい
        public Observable<Unit> Blinked => _blinkDetector
            .Blinked()
            .Where(_ => InteractionApiEnabled);

        //IsTalkingとか (まだないが) LipSyncの取得APIを呼ぶときに下記のRegisterを呼ぶことで、マイクの録音が止まってたら開始を要求できる
        public void RegisterApiInstanceAsMicrophoneRequire(object obj)
        {
            _microphoneRecordRequireBuddies.Add(obj);
            _requireMicrophoneRecording.Value = true;
        }

        public void UnregisterApiInstanceAsMicrophoneRequire(object obj)
        {
            _microphoneRecordRequireBuddies.Remove(obj);
            _requireMicrophoneRecording.Value = _microphoneRecordRequireBuddies.Count > 0;
        }

        // NOTE: 下記の関数はモデルのロード前にfalse/0が戻るので、IsLoadedはチェックしないでよい
        public bool HasKey(string key, bool isCustomKey)
        {
            if (!InteractionApiEnabled)
            {
                return false;
            }

            var expressionKey = CreateExpressionKey(key, isCustomKey);
            return _expressionAccumulator.HasKey(expressionKey);
        }

        public float GetBlendShapeValue(string key, bool isCustomKey)
        {
            if (!InteractionApiEnabled)
            {
                return 0f;
            }

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
            if (!InteractionApiEnabled)
            {
                return null;
            }

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
        public FaceSwitchAction GetActiveFaceSwitch()
        {
            if (!InteractionApiEnabled)
            {
                return FaceSwitchAction.None;
            }

            // NOTE: Extractorの挙動がちょっとおもしろい(フレーム内でoff/onが頻繁に切り替わる事もあるような実装になってる)ので、
            // false-negative でNoneを取得する懸念がある。
            // ただし、フレーム内の十分遅いタイミングではメインアバターに適用してる値が取れるので問題ない…はず。
            return _faceSwitchExtractor.ActiveItem.Action;
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
