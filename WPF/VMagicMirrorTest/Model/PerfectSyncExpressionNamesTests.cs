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
        public void OlderSettingsLeaveCompatibilityOptionDisabled()
        {
            var setting = Newtonsoft.Json.JsonConvert.DeserializeObject<VMagicMirrorConfig.ExternalTrackerSetting>("{}");
            Assert.That(setting.PerfectSyncIgnoreCase, Is.False);
            setting.PerfectSyncIgnoreCase = true;
            var restored = Newtonsoft.Json.JsonConvert.DeserializeObject<VMagicMirrorConfig.ExternalTrackerSetting>(
                Newtonsoft.Json.JsonConvert.SerializeObject(setting));
            Assert.That(restored.PerfectSyncIgnoreCase, Is.True);
        }
    }
}
