using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Baku.VMagicMirrorConfig.ViewModel
{
    // NOTE: ホントにDesignでしか使わないやつ。
    // Buddyのプロパティは種類が多い && UGC依存で表示が変わるので、エディタでの視認性を担保するモチベーションが高くて特別に作っている
    public class DesignBuddyItemViewModel
    {
        private readonly BuddyData _buddyData;

        private static BuddyData CreateDesignBuddyData()
        {
            var displayName = BuddyLocalizedText.Const("デザイン用の表示名");
            var displayNameVeryLong = BuddyLocalizedText.Const("デザイン用の表示名の非常に長いサンプルです。プロパティの表示が崩れる可能性があります。");
            var desc = BuddyLocalizedText.Const("デザイン用の説明文");

            var propertyMetadata = new BuddyPropertyMetadata[]
            {
                BuddyPropertyMetadata.Bool("boolSample", displayName, desc, false),
                BuddyPropertyMetadata.Bool("boolSample2", displayNameVeryLong, desc, false),
                BuddyPropertyMetadata.Int("intSample", displayName, desc, 42),
                BuddyPropertyMetadata.Int("intSample2", displayNameVeryLong, desc, 42),
                BuddyPropertyMetadata.RangeInt("rangeIntSample", displayName, desc, 50, 0, 100),
                BuddyPropertyMetadata.Float("floatSample", displayName, desc, 3.14f),
                BuddyPropertyMetadata.RangeFloat("rangeFloatSample", displayName, desc, 5f, -1f, 12f),
                BuddyPropertyMetadata.String("stringSample", displayName, desc, "Hello World!"),
                BuddyPropertyMetadata.FilePathString("filePathSample", displayName, desc, "C:\\Example\\Buddy\\MyBuddy\\sample.txt"),
                BuddyPropertyMetadata.Enum("enumSample", displayName, desc, 0, ["Enum1", "Enum2", "Enum3"]),
                BuddyPropertyMetadata.Vector2("vector2Sample", displayName, desc, new BuddyVector2(1, 2)),
                BuddyPropertyMetadata.Vector3("vector3Sample", displayName, desc, new BuddyVector3(1, 2, 3)),
                BuddyPropertyMetadata.Quaternion("vector4Sample", displayName, desc, new BuddyVector3(10, 20, 30)),
                BuddyPropertyMetadata.Transform2D("transform2DSample", displayName, desc, new BuddyTransform2D(new BuddyVector2(1, 2), new BuddyVector3(10, 20, 30), 1.0f)),
                BuddyPropertyMetadata.Transform3D("transform3DSample", displayName, desc, new BuddyTransform3D(
                    new BuddyVector3(1, 2, 3), new BuddyVector3(10, 20, 30), 1.0f, BuddyParentBone.RightUpperArm
                    )),
                BuddyPropertyMetadata.Action("actionSample", displayName, desc),
                BuddyPropertyMetadata.Action("actionSample2", displayNameVeryLong, desc),
            };

            var metadata = new BuddyMetadata(
                false,
                @"C:\Example\Buddy\MyBuddy\",
                "com.example-campany.buddy",
                BuddyLocalizedText.Const("デザイン用サブキャラ"),
                "Example Creator",
                "https://example.com/creator-url",
                "0.1.2",
                propertyMetadata
            );

            return new BuddyData(
                metadata,                
                propertyMetadata.Select(p => new BuddyProperty(p, p.CreateDefaultValue())).ToArray()
                );
        }

        public DesignBuddyItemViewModel()
        {
            // NOTE: 開発者モードUIは見ときたいので
            IsDeveloperMode = new RProperty<bool>(false);
            HasError = new RProperty<bool>(true);
            HasNonDeveloperError = new RProperty<bool>(true);
            CurrentFatalError = new RProperty<BuddyLogMessage?>(new BuddyLogMessage(
                "MyBuddy",
                "不明な重大エラーのテスト用テキストです。コンパイルエラーのテキストが入りうるので、それなりに長いテキストをサンプルに入れてます。",
                (int) BuddyLogLevel.Fatal
                ));

            var buddyData = CreateDesignBuddyData();
            _buddyData = buddyData;
            LogMessages = new ReadOnlyObservableCollection<BuddyLogMessage>(_logMessages);

            // NOTE: 本クラスがEditor専用型なので、null許容に対してウソをついておく(BuddyPropertyViewModel自体は実際使うクラスなので、nullは認めないままにしとく)
            BuddySettingsSender? nullSender = null;
            Properties = buddyData.Properties
                .Select(p => new BuddyPropertyViewModel(nullSender!, buddyData.Metadata, p))
                .ToArray();

            foreach(var property in Properties)
            {
                property.ApplyLanguage(true);
            }
        }

        // NOTE: ここから下はあまり頻繁にはいじらない想定 (しょっちゅう変えるものはctorまでに変えるように実装しておきたい)

        public RProperty<bool> IsActive => _buddyData.IsActive;

        public RProperty<bool> IsDeveloperMode { get; }
        public RProperty<bool> HasError { get; }
        public RProperty<bool> HasNonDeveloperError { get; }


        private readonly ActionCommand _dummyCommand = new(() => { });
        public ActionCommand ReloadCommand => _dummyCommand;
        public ActionCommand ResetSettingsCommand => _dummyCommand;

        public ActionCommand CopyLogMessageCommand => _dummyCommand;
        public ActionCommand ClearLogCommand => _dummyCommand;
        public ActionCommand OpenLogFileCommand => _dummyCommand;

        public IReadOnlyList<BuddyPropertyViewModel> Properties { get; }

        public string FolderName => _buddyData.Metadata.FolderName;
        public RProperty<string> DisplayName { get; } = new("表示名サンプル");

        // NOTE: ログは一旦スルーしとくが、決め打ちで何か入れてもよい
        private readonly ObservableCollection<BuddyLogMessage> _logMessages = [];
        public ReadOnlyObservableCollection<BuddyLogMessage> LogMessages { get; }

        /// <summary> 非開発者に表示する想定の重大エラー </summary>
        public RProperty<BuddyLogMessage?> CurrentFatalError { get; }
    }
}
