// NOTE: この #load ステートメントは実行時に無視される
#load "..\_Reference\Globals.csx"
using System;
using System.Threading;
using System.Threading.Tasks;
using VMagicMirror.Buddy;
using VMagicMirror.Buddy.IO;

// デフォルトサブキャラの挙動
// - デフォルト立ち絵が適用され、アプリ本体の実装ベースでまばたき/LipSync同期を行う
// - 退屈ポーズとして blink 相当のスプライトで寝てるポーズをする
//   - このポーズはキー入力等の検知、または一定時間の経過で解除される
// - 発声へのリアクションとして小ジャンプする
// - キー打鍵やゲームパッドのボタン押しへのリアクションとして小ジャンプする

// 実装の建付け
// - ScriptStatusとして、どの振る舞いからも見れてほしいグローバル変数相当のデータを引き回す
// - 個々のアクションはclassで定義される
//   - イベントハンドラは個別のclassが各々の必要に応じて購読する (ので、二重に購読することもある)
// - アクションは「排他で発生する」か「None状態のときに散発的に適用する」かのいずれかの方式で行う

enum MyBuddyActions
{
    // なし
    None,
    // メインアバターの表情に合わせている状態
    FacialSync,
    // 入力がないので寝てる状態
    Sleep,
    // 素早いタイピングなりボタン入力なりでハイテンションっぽくなる状態
    RepeatInput,
}

// どのアクションからも参照されるような、基本的なスクリプトの状態
class ScriptStatus(IRootApi rootApi, ISprite2D sprite)
{
    public IRootApi RootApi { get; } = rootApi;
    public ISprite2D Sprite { get; } = sprite;

    public MyBuddyActions CurrentAction { get; private set; } = MyBuddyActions.None;

    // NOTE: MyBuddyActions.Noneを指定する場合、明示的に元のスプライトに戻す処理を行ってる場合には第二引数をfalseにする。
    // それ以外の大半のケースでは第二引数は指定しないでよい
    public void SetActionState(MyBuddyActions state, bool setDefaultSpriteWhenNone = true)
    {
        if (CurrentAction == state)
        {
            return;
        }

        if (setDefaultSpriteWhenNone && state == MyBuddyActions.None)
        {
            Sprite.ShowDefaultSprites(Sprite2DTransitionStyle.LeftFlip, 0.3f);
        }
        CurrentAction = state;
    }
}

var status = new ScriptStatus(Api, Api.Create2DSprite());
var sleeper = new Sleeper(status);
var facialSynchronizer = new FacialSynchronizer(status);
var inputBasedJumper = new InputBasedJumper(status);
var repeatInput = new RepeatInput(status);
var talkReactionNod = new TalkReactionNod(status);

// コールバックの登録用に event Action<T> が公開されている
Api.Start += () =>
{
    // 位置を編集できるスプライトを生成する
    var parent = Api.Transforms.GetTransform2D("mainImage");
    var sprite = status.Sprite;

    sprite.Transform.SetParent(parent);
    sprite.Size = new Vector2(256, 256);

    // 「まばたき、口の開閉」の4枚を組み合わせた挙動を基本とする (※このAPIを使わないでもよい)
    sprite.SetupDefaultSpritesByPreset();
    // プリセットじゃない場合、4枚の画像を用意して下記のように呼ぶ。
    // 口は動かさない場合、2枚だけ用意して sprite.SetupDefaultSprites("default.png", "blink.png", "default.png", "blink.png") のように指定してもOK
    // sprite.SetupDefaultSprites("default.png", "blink.png", "mouthOpen.png", "blink_mouthOpen.png");

    // 通常の立ち絵を適用しておく
    status.Sprite.ShowDefaultSprites();

    facialSynchronizer.Start();
    inputBasedJumper.Start();
    repeatInput.Start();
    sleeper.Start();
    talkReactionNod.Start();
};

Api.Update += (deltaTime) =>
{
    // flipだけめちゃくちゃ基礎的な処理なのでここでやっておく
    var flipEnabled = Api.Property.GetBool("flip");
    status.Sprite.Transform.LocalScale = flipEnabled ? new Vector2(-1, 1) : new Vector2(1, 1);

    sleeper.Update(deltaTime);
    facialSynchronizer.Update(deltaTime);
    inputBasedJumper.Update(deltaTime);
    repeatInput.Update(deltaTime);
    talkReactionNod.Update(deltaTime);
};

// 立ち絵のオーバーライド処理は他とあまり紐づいてないのでここで実施
// ※リリース版でも立ち絵オーバーライドを許可する場合はもうちょいgood designしないといけない
// (Show関数を全体的に分岐させる必要があるため)
Api.Property.ActionRequested += (action) =>
{
    switch (action)
    {
        case "applyImageFile":
            ApplyImageFile();
            break;
    }
};

void ApplyImageFile()
{
    var defaultImagePath = Api.Property.GetString("defaultImageFile");
    var blinkImagePath = Api.Property.GetString("blinkImageFile");
    if (File.Exists(defaultImagePath) && File.Exists(blinkImagePath))
    {
        // NOTE: ホントは口パクと合わせて4枚の異なる画像を差し込むが、ここでは簡単のためdefaultとblinkだけで済ます
        status.Sprite.SetupDefaultSprites(defaultImagePath, blinkImagePath, defaultImagePath, blinkImagePath);
    }
    else
    {
        status.Sprite.SetupDefaultSpritesByPreset();
    }
}

// 短時間で連続してタイピングすると表情が切り替わって動くようなやつ
// 目安: 2秒間に10回くらいのガガガっと入力したらこのクラスが指定する状態に入る
class RepeatInput
{
    enum RepeatInputState
    {
        None,
        // 連続入力によって表示が変化した状態
        RepeatInput,
        // 〆で表情を切り替えて戻る状態
        ReturnToDefault,
    }

    const float RepeatInputDecreaseRate = 3f;
    const float RepeatInputThreshold = 7f;
    const float JumpDuration = 0.5f;
    const float JumpHeight = 15f;
    const float MinFaceChangeTime = 1.0f;
    const float MaxFaceChangeTime = 15.0f;
    // 一旦モードに入ったあと、この秒数だけ入力がないと「ドヤ顔 or ウィンクして元に戻る」のステートに入る
    const float ReturnToDefaultNoneInputTime = 1.0f;

    public RepeatInput(ScriptStatus status)
    {
        _status = status;
    }

    readonly ScriptStatus _status;
    IRootApi Api => _status.RootApi;

    RepeatInputState _state = RepeatInputState.None;

    // 打鍵で1増えて、時間経過で決まったペースで減っていくような特性を持つスコア
    float _repeatInputScore = 0f;
    float _noneInputTime = 0f;
    float _activeTime = 0f;

    public void Start()
    {
        Api.Input.KeyboardKeyDown += _ =>
        {
            _repeatInputScore += 1f;
            _noneInputTime = 0f;
        };
        Api.Input.GamepadButtonDown += _ =>
        {
            _repeatInputScore += 1f;
            _noneInputTime = 0f;
        };
    }

    public void Update(float deltaTime)
    {
        if (!Api.Property.GetBool("enableRepeatInput"))
        {
            if (_status.CurrentAction == MyBuddyActions.RepeatInput)
            {
                _status.SetActionState(MyBuddyActions.None);
            }
            Reset();
            return;
        }

        if (_status.CurrentAction != MyBuddyActions.None &&
            _status.CurrentAction != MyBuddyActions.RepeatInput)
        {
            Reset();
            return;
        }

        switch (_state)
        {
            case RepeatInputState.None:
                UpdateNoneState(deltaTime);
                break;
            case RepeatInputState.RepeatInput:
                UpdateActiveState(deltaTime);
                break;
            case RepeatInputState.ReturnToDefault:
                // 何もしない: 戻る処理はTaskかInvokeDelayで実行するため
                break;

        }
    }

    // 入力ペースが十分早かった場合、ステートが進む
    void UpdateNoneState(float deltaTime)
    {
        _repeatInputScore -= RepeatInputDecreaseRate * deltaTime;
        if (_repeatInputScore < 0f)
        {
            _repeatInputScore = 0f;
        }

        if (_repeatInputScore > RepeatInputThreshold)
        {
            _state = RepeatInputState.RepeatInput;
            _noneInputTime = 0f;
            _activeTime = 0f;

            _status.SetActionState(MyBuddyActions.RepeatInput);
            _status.Sprite.ShowPreset("A_happy", Sprite2DTransitionStyle.LeftFlip, 0.3f);
            // NOTE: 再長時間までひたすらタイピングしているケースのためのジャンプ回数を指定している。
            // 大体の場合は途中でストップする
            _status.Sprite.Effects.Jump.Jump(MaxFaceChangeTime, JumpHeight, (int)(MaxFaceChangeTime / JumpDuration));
        }
    }

    // 入力が途切れる、または一定時間が経過すると表情を切り替えたのち、一定時間で戻る
    void UpdateActiveState(float deltaTime)
    {
        _activeTime += deltaTime;
        _noneInputTime += deltaTime;
        if (_activeTime > MaxFaceChangeTime ||
            (_activeTime > MinFaceChangeTime && _noneInputTime > ReturnToDefaultNoneInputTime))
        {
            _status.Sprite.Effects.Jump.Stop();
            var preset = Api.Random() < 0.5f ? "A_wink" : "A_smug_face";
            _status.Sprite.ShowPreset(preset, Sprite2DTransitionStyle.LeftFlip, 0.3f);
            _state = RepeatInputState.ReturnToDefault;
            Api.RunOnMainThread(() => ResetToDefaultStateAsync());
        }
    }

    async Task ResetToDefaultStateAsync()
    {
        await Task.Delay(1500);
        _status.Sprite.ShowDefaultSprites(Sprite2DTransitionStyle.LeftFlip, 0.3f);
        await Task.Delay(500);
        _status.SetActionState(MyBuddyActions.None, false);
        Reset();
    }

    void Reset()
    {
        _state = RepeatInputState.None;
        _activeTime = 0f;
        _repeatInputScore = 0f;
        _noneInputTime = 1f;
    }
}

// 一定時間特定のインプットがない (key/gamepadボタン押しがない) と寝る。
// 寝たあとは時間経過、またはインプット検知によって通常の表情に戻る
class Sleeper
{
    // NOTE: ある程度短くしないと寝まくる感じになるので注意、(特にアバター出力がオフの場合)
    const float NoneInputTimeMin = 60f;
    const float NoneInputTimeMax = 120f;

    const float SleepBahaviorPeriod = 10f;
    // NOTE: アバター出力が取れないときにあんまり長時間寝かせてもいけないので、最長でもあんまり寝ないようにはしておく
    const float SleepMaxTime = 20f;
    const float SleepMinTime = 5f;

    const float MaxTiltAngle = 10f;

    enum SleepState
    {
        None,
        Sleeping,
        ReturnToDefault,
    }

    float _noneInputTime = 0f;
    float _sleepTime = 0f;
    SleepState _sleepState = SleepState.None;
    float _noneInputTimeThreshold = NoneInputTimeMin;


    public Sleeper(ScriptStatus status)
    {
        _status = status;
    }

    readonly ScriptStatus _status;
    IRootApi Api => _status.RootApi;

    public void Start()
    {
        // NOTE: この2つとは別で「寝てるときに話しかけると起きる」も仕込んである
        Api.Input.KeyboardKeyDown += _ => ReceiveInput();
        Api.Input.GamepadButtonDown += _ => ReceiveInput();

        // びっくりして起きるとき、横方向にのみVibrateさせる
        _status.Sprite.Effects.Vibrate.IntensityX = 3f;
        _status.Sprite.Effects.Vibrate.FrequencyX = 15f;
        _status.Sprite.Effects.Vibrate.IntensityY = 0f;
    }

    public void Update(float deltaTime)
    {
        if (!Api.Property.GetBool("enableSleep"))
        {
            if (_status.CurrentAction == MyBuddyActions.Sleep)
            {
                _status.SetActionState(MyBuddyActions.None);
            }
            ResetSleepStatus();
            return;
        }

        if (_status.CurrentAction != MyBuddyActions.None &&
            _status.CurrentAction != MyBuddyActions.Sleep)
        {
            ResetSleepStatus();
            return;
        }

        switch (_sleepState)
        {
            case SleepState.None:
                UpdateNoneInputTime(deltaTime);
                break;
            case SleepState.Sleeping:
                UpdateSleepTime(deltaTime);
                break;
            case SleepState.ReturnToDefault:
                // 何もしない: 戻る処理はTaskで実行するため
                break;
        }
    }

    // 「寝ない」方向のインプットを受けると呼ばれる関数
    void ReceiveInput()
    {
        switch (_sleepState)
        {
            case SleepState.None:
                // 入力が発生 == 入力待ち時間のカウントをリセット
                _noneInputTime = 0f;
                break;
            case SleepState.Sleeping:
                if (_sleepTime > SleepMinTime)
                {
                    // 寝てる状態からのインプットは、Surprisedを経由して戻る
                    _sleepState = SleepState.ReturnToDefault;
                    Api.RunOnMainThread(() => SurpriseAndResetAsync());
                }
                break;
        }
    }

    // 入力がきっかけの場合、驚いてから戻る
    async Task SurpriseAndResetAsync()
    {
        _status.Sprite.Transform.LocalRotation = Quaternion.identity;
        _status.Sprite.Effects.Vibrate.IsActive = true;
        _status.Sprite.ShowPreset("A_surprised", Sprite2DTransitionStyle.LeftFlip, 0.25f);
        await Task.Delay(600);
        _status.Sprite.Effects.Vibrate.IsActive = false;
        await Task.Delay(1000);
        _status.Sprite.ShowDefaultSprites(Sprite2DTransitionStyle.LeftFlip, 0.3f);
        await Task.Delay(500);

        _status.SetActionState(MyBuddyActions.None, false);
        _sleepState = SleepState.None;
        _sleepTime = 0f;
        _noneInputTime = 0f;
    }

    // 入力なしで一定時間が経過した場合、Surprisedを経由せず通常のスプライトに戻る
    async Task ResetAsync()
    {
        _status.Sprite.Transform.LocalRotation = Quaternion.identity;
        _status.Sprite.ShowDefaultSprites(Sprite2DTransitionStyle.LeftFlip, 0.5f);
        await Task.Delay(600);

        _status.SetActionState(MyBuddyActions.None, false);
        _sleepState = SleepState.None;
        _sleepTime = 0f;
        _noneInputTime = 0f;
    }

    void UpdateNoneInputTime(float deltaTime)
    {
        _noneInputTime += deltaTime;

        // 話しかけられている場合は寝ない
        if (Api.AvatarFacial.IsTalking)
        {
            _noneInputTime = 0f;
        }

        if (_noneInputTime < _noneInputTimeThreshold)
        {
            return;
        }

        _noneInputTimeThreshold = Api.Random() * (NoneInputTimeMax - NoneInputTimeMin) + NoneInputTimeMin;

        // アクションの占有を開始しつつ、睡眠ポーズを起動
        _sleepState = SleepState.Sleeping;
        _status.Sprite.ShowPreset("A_blink");
        _status.SetActionState(MyBuddyActions.Sleep);
    }

    void UpdateSleepTime(float deltaTime)
    {
        _sleepTime += deltaTime;
        var rate = (_sleepTime % SleepBahaviorPeriod) / SleepBahaviorPeriod;
        var rotationRate = 0.5f * (1 - (float)Math.Cos(rate * Math.PI * 2));

        // flipしてるかどうかで符号が変わる。デフォルトでは右向き想定なことに注意
        var flip = Api.Property.GetBool("flip");
        var rotationAngle = rotationRate * MaxTiltAngle * (flip ? 1 : -1);
        _status.Sprite.Transform.LocalRotation = Quaternion.Euler(0, 0, rotationAngle);

        if (_sleepTime > SleepMinTime && Api.AvatarFacial.IsTalking)
        {
            // 寝始めたあとで声をかけると起きる
            _sleepState = SleepState.ReturnToDefault;
            Api.RunOnMainThread(() => SurpriseAndResetAsync());
        }

        if (_sleepTime > SleepMaxTime)
        {
            _sleepState = SleepState.ReturnToDefault;
            Api.RunOnMainThread(() => ResetAsync());
        }
    }

    void ResetSleepStatus()
    {
        if (_sleepState != SleepState.None)
        {
            // NOTE: ここでのLocalRotationのリセットは、あくまで「寝てた状態から戻す」ためのもの
            // そのため、LocalRotationをリセットすることで他のアクションに影響が出ることはない
            // NOTE: 本スクリプトでは他の振る舞いでLocalRotationを操作しないので、割とテキトーに戻しても大丈夫
            _status.Sprite.Transform.LocalRotation = Quaternion.identity;
            _sleepState = SleepState.None;
        }

        _noneInputTime = 0f;
        _sleepTime = 0f;
    }
}


// TODO: ディレイ、確率での表情切り替え、表情の最短持続時間…といったケアがあるほうが好ましいかも。
// - メインアバターの表情に対して同期を試みる
// - 標準のフェイシャルの範囲で動かす
class FacialSynchronizer
{
    public FacialSynchronizer(ScriptStatus status)
    {
        _status = status;
    }

    static readonly TimeSpan _faceSyncDelay = TimeSpan.FromSeconds(0.25);
    static readonly TimeSpan _minFaceSyncTime = TimeSpan.FromSeconds(0.6);

    readonly ScriptStatus _status;
    IRootApi Api => _status.RootApi;

    string _facialName = "";

    // NOTE: 表情を一度変えたときにしばらくtrueになるフラグで、このフラグが立ってる間は表情をキープするのが期待値
    bool _faceChangeBlocked = false;

    public void Start()
    {
        // do nothing: イベント購読もなし
    }

    public void Update(float deltaTime)
    {
        var enabled = Api.Property.GetBool("enableFacialSync");
        if (!enabled)
        {
            if (_status.CurrentAction == MyBuddyActions.FacialSync)
            {
                _status.SetActionState(MyBuddyActions.None);
            }
            _facialName = "";
            return;
        }

        if (_status.CurrentAction != MyBuddyActions.None &&
            _status.CurrentAction != MyBuddyActions.FacialSync)
        {
            _facialName = "";
            return;
        }

        if (_faceChangeBlocked) 
        {
            return;
        }

        var facial = Api.AvatarFacial.CurrentFacial;
        var nextFacialName =
          (facial == BlendShapePresetNames.Happy) ? "A_happy" :
          (facial == BlendShapePresetNames.Angry) ? "A_angry" :
          (facial == BlendShapePresetNames.Sad) ? "A_sad" :
          (facial == BlendShapePresetNames.Relaxed) ? "A_relaxed" :
          (facial == BlendShapePresetNames.Surprised) ? "A_surprised" :
          "";

        if (_facialName == nextFacialName)
        {
            return;
        }

        _facialName = nextFacialName;
        if (string.IsNullOrEmpty(nextFacialName))
        {
            // 同期してた表情からデフォルト表情に戻ると、ここを通過して終了する
            if (_status.CurrentAction == MyBuddyActions.FacialSync)
            {
                _faceChangeBlocked = true;
                Api.RunOnMainThread(() => ResetFacialAsync());
            }
            return;
        }

        // 状態の排他 + しばらく別の表情に切り替わるのは禁止する。別の表情になっても良くするためのガード解除はTask上で行う
        if (_status.CurrentAction == MyBuddyActions.None)
        {
            _status.SetActionState(MyBuddyActions.FacialSync);
        }

        _faceChangeBlocked = true;
        Api.RunOnMainThread(() => ChangeFacialAsync(nextFacialName));
    }

    async Task ChangeFacialAsync(string facialName)
    {
        await Task.Delay(_faceSyncDelay);
        _status.Sprite.ShowPreset(facialName, Sprite2DTransitionStyle.LeftFlip, 0.3f);
        await Task.Delay(_minFaceSyncTime);
        _faceChangeBlocked = false;
    }

    async Task ResetFacialAsync()
    {
        await Task.Delay(_faceSyncDelay);
        _status.Sprite.ShowDefaultSprites(Sprite2DTransitionStyle.LeftFlip, 0.3f);
        await Task.Delay(500);
        _status.SetActionState(MyBuddyActions.None, false);
        _faceChangeBlocked = false;
    }
}

// - キーかゲームパッドの入力によってジャンプする。
// - 他のアクションが効いてるうちはジャンプしない
// - 入力によるジャンプにはStateは設けていない: NoneだったらNoneステートのままJumpを実行し、そのまま他のステートに入ってもよい
class InputBasedJumper
{
    public InputBasedJumper(ScriptStatus status)
    {
        _status = status;
    }

    readonly ScriptStatus _status;
    IRootApi Api => _status.RootApi;

    bool _jumpRequested;
    int _jumpCount = 1;

    // 1回ジャンプしたらしばらくジャンプ禁止するための待ち時間
    float _jumpCountDown = 0f;

    // 発話を検出してる時間の長さ
    float _talkingTime = 0f;

    public void Start()
    {
        Api.Input.GamepadButtonDown += _ => JumpByInput();
        Api.Input.KeyboardKeyDown += _ => JumpByInput();
    }

    public void Update(float deltaTime)
    {
        var enabled = Api.Property.GetBool("enableJumpReaction");
        if (!enabled)
        {
            ResetParameters();
        }

        if (_jumpCountDown > 0f)
        {
            _jumpCountDown -= deltaTime;
            _jumpRequested = false;
            _talkingTime = 0f;
            return;
        }

        UpdateTalkBasedJump(deltaTime);

        var jumpRequested = _jumpRequested;
        _jumpRequested = false;
        if (!jumpRequested || _status.CurrentAction != MyBuddyActions.None)
        {
            return;
        }

        DoJump();
    }

    void ResetParameters()
    {
        _jumpRequested = false;
        _jumpCount = 1;
        _jumpCountDown = 0f;
        _talkingTime = 0f;
    }

    void JumpByInput()
    {
        if (Api.Random() < 0.8f)
        {
            _jumpCount = 1;
            _jumpRequested = true;
        }
    }

    void DoJump()
    {
        // ジャンプの仕方もランダムで、大まかに「高いジャンプは長時間かかる」みたいな傾向をもたせる
        var jumpHeightRand = Api.Random();
        var jumpDuration = (0.2f + 0.1f * jumpHeightRand) * _jumpCount;
        var jumpHeight = 20f + 15f * jumpHeightRand;

        //NOTE: ディレイはつけない。そもそもIsTalking = falseになるタイミングにちょっとディレイがあるため
        _status.Sprite.Effects.Jump.Jump(jumpDuration, jumpHeight, _jumpCount);
        _jumpCountDown = jumpDuration + 0.2f;
    }

    void UpdateTalkBasedJump(float deltaTime)
    {
        //DEBUG: うなずき動作のほうを検証したいので塞いでおく
        return;

        if (Api.AvatarFacial.IsTalking)
        {
            _talkingTime += deltaTime;
            return;
        }

        // 一瞬だけ喋ったのはノーカン
        if (_talkingTime < .5f)
        {
            _talkingTime = 0f;
            return;
        }

        _talkingTime = 0f;

        // 「ジャンプしない / 1回ジャンプ / 2回ジャンプ」の3パターンに分けてる
        var rand = Api.Random();
        if (rand < 0.8f)
        {
            _jumpCount = rand < 0.2f ? 2 : 1;
            _jumpRequested = true;
        }
    }
}

// - 発声に対してうなずき動作を行う
// - NOTE: Jumpを削除してコッチのリアクションだけにすることを検討中
class TalkReactionNod
{
    const float NodDuration = 0.5f;
    const float NodAngle = 5f;
    const float NodProbability = 0.65f;
    const float DoubleNodProbability = 0.3f;

    public TalkReactionNod(ScriptStatus status)
    {
        _status = status;
    }

    readonly ScriptStatus _status;
    IRootApi Api => _status.RootApi;

    // 発話を検出してる時間の長さ: 短時間すぎる発話はノーカン扱いにするために時間を計測しておく
    float _talkingTime = 0f;

    bool _isNodding = false;
    float _nodMotionTime = 0f;
    int _nodCount = 1;

    public void Start()
    {
        // do nothing
    }

    public void Update(float deltaTime)
    {
        // NOTE: うなずきは禁止する理由が思いつかないので、プロパティによるon/offはサポートしない
        if (_status.CurrentAction != MyBuddyActions.None)
        {
            if (!_isNodding)
            {
                _status.Sprite.Transform.LocalRotation = Quaternion.identity;
            }
            _isNodding = false;
            _nodMotionTime = 0f;
            _talkingTime = 0f;
            return;
        }

        if (!_isNodding)
        {
            CheckNodStart(deltaTime);
        }

        if (_isNodding)
        {
            UpdateNod(deltaTime);
        }
    }

    void CheckNodStart(float deltaTime)
    {
        if (Api.AvatarFacial.IsTalking)
        {
            _talkingTime += deltaTime;
            return;
        }

        // 喋りが一瞬だけの場合は無視
        if (_talkingTime < .5f)
        {
            _talkingTime = 0f;
            return;
        }

        _talkingTime = 0f;

        // 確率次第でうなづく。毎回ではない
        if (Api.Random() < NodProbability)
        {
            _nodMotionTime = 0f;
            _nodCount = Api.Random() < DoubleNodProbability ? 2 : 1;
            _isNodding = true;
        }
    }

    void UpdateNod(float deltaTime)
    {
        _nodMotionTime += deltaTime;
        if (_nodMotionTime >= NodDuration * _nodCount)
        {
            _status.Sprite.Transform.LocalRotation = Quaternion.identity;
            _isNodding = false;
            return;
        }

        // 2回うなづくケースがあることに注意
        var rate = (_nodMotionTime % NodDuration) / NodDuration;
        var flip = Api.Property.GetBool("flip");
        var rotationAngle = (float)Math.Sin(rate * Math.PI) * NodAngle * (flip ? 1 : -1);
        _status.Sprite.Transform.LocalRotation = Quaternion.Euler(0, 0, rotationAngle);
    }
}
