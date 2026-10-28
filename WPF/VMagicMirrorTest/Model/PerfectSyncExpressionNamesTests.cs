using System.Globalization;
using System.Linq;
using Baku.VMagicMirror;
using NUnit.Framework;

namespace Baku.VMagicMirror.Test
{
    public class PerfectSyncExpressionNamesTests
    {
        [Test]
        public void ExactMatchWinsRegardlessOfOrder()
        {
            var aliases = PerfectSyncExpressionNames.ResolveAliases(
                new[] { "mouthFunnel", "MouthFunnel", "MOUTHFUNNEL" }, new[] { "MouthFunnel" });
            Assert.That(aliases, Is.Empty);
        }

        [Test]
        public void OnlySupportedMissingNamesAreSupplementedAndOriginalsAreUnchanged()
        {
            var originals = new[] { "mouthFunnel", "MySmile", "EyeBlinkLeft" };
            var aliases = PerfectSyncExpressionNames.ResolveAliases(
                originals, new[] { "MouthFunnel", "EyeBlinkLeft", "JawOpen" }).ToArray();
            Assert.That(aliases, Is.EqualTo(new[] { ("MouthFunnel", "mouthFunnel") }));
            Assert.That(originals, Is.EqualTo(new[] { "mouthFunnel", "MySmile", "EyeBlinkLeft" }));
        }

        [Test]
        public void AmbiguousNamesUseFirstOriginalDefinition()
        {
            var aliases = PerfectSyncExpressionNames.ResolveAliases(
                new[] { "mouthFunnel", "MOUTHFUNNEL" }, new[] { "MouthFunnel" });
            Assert.That(aliases.Single().SourceName, Is.EqualTo("mouthFunnel"));
        }

        [Test]
        public void CaseMatchingDoesNotDependOnCurrentCulture()
        {
            var previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
                var aliases = PerfectSyncExpressionNames.ResolveAliases(
                    new[] { "browinnerup" }, new[] { "BrowInnerUp" });
                Assert.That(aliases.Single().SourceName, Is.EqualTo("browinnerup"));
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
            }
        }

        [Test]
        public void OlderSettingsDefaultToCaseInsensitiveAndExplicitChoiceIsSaved()
        {
            var setting = Newtonsoft.Json.JsonConvert.DeserializeObject<VMagicMirrorConfig.MotionSetting>("{}");
            Assert.That(setting.PerfectSyncExpressionCaseSensitive, Is.False);
            setting.PerfectSyncExpressionCaseSensitive = true;
            var restored = Newtonsoft.Json.JsonConvert.DeserializeObject<VMagicMirrorConfig.MotionSetting>(
                Newtonsoft.Json.JsonConvert.SerializeObject(setting));
            Assert.That(restored.PerfectSyncExpressionCaseSensitive, Is.True);
            restored.ResetToDefault();
            Assert.That(restored.PerfectSyncExpressionCaseSensitive, Is.False);
        }

        [TestCase(false, true)]
        [TestCase(true, false)]
        public void CheckboxAndStoredCaseSensitivityAreInvertedInBothDirections(bool stored, bool check)
        {
            var converter = new VMagicMirrorConfig.View.BooleanReverseConverter();
            Assert.That(converter.Convert(stored, typeof(bool), null, CultureInfo.InvariantCulture), Is.EqualTo(check));
            Assert.That(converter.ConvertBack(check, typeof(bool), null, CultureInfo.InvariantCulture), Is.EqualTo(stored));
        }
    }
}
