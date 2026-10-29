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
        public void MeshFallbackUsesOnlyMissingSupportedNamesAndKeepsTheirIndices()
        {
            var result = PerfectSyncExpressionNames.ResolveMeshShapes(
                new[] { "MySmile", "mouthFunnel", "JAWOPEN", "BrowInnerUp" },
                new[] { "BrowInnerUp" },
                new[] { "MouthFunnel", "JawOpen", "BrowInnerUp", "TongueOut" }).ToArray();
            Assert.That(result, Is.EqualTo(new[] { ("MouthFunnel", 1), ("JawOpen", 2) }));
        }

        [Test]
        public void ExistingExpressionsPreventMeshFallbackEvenWhenTheirBindingsAreEmpty()
        {
            // Presence alone controls priority: empty authored expressions are intentional too.
            Assert.That(PerfectSyncExpressionNames.ResolveMeshShapes(
                new[] { "mouthFunnel" }, new[] { "MouthFunnel" }, new[] { "MouthFunnel" }), Is.Empty);
        }

        [TestCase(false, 0)]
        [TestCase(true, 1)]
        public void ExpressionCaseSettingControlsWhetherAliasOrMeshWins(bool caseSensitive, int meshCount)
        {
            var originals = new[] { "mouthFunnel" };
            var supported = new[] { "MouthFunnel" };
            var aliases = caseSensitive ? new string[0] :
                PerfectSyncExpressionNames.ResolveAliases(originals, supported).Select(a => a.Name).ToArray();
            var result = PerfectSyncExpressionNames.ResolveMeshShapes(
                new[] { "MOUTHFUNNEL" }, originals.Concat(aliases), supported).ToArray();
            Assert.That(result.Length, Is.EqualTo(meshCount));
        }

        [Test]
        public void MeshMatchingPrefersExactNameThenFirstCaseInsensitiveMatch()
        {
            var supported = new[] { "MouthFunnel" };
            Assert.That(PerfectSyncExpressionNames.ResolveMeshShapes(
                new[] { "mouthFunnel", "MouthFunnel" }, new string[0], supported).Single().Index, Is.EqualTo(1));
            Assert.That(PerfectSyncExpressionNames.ResolveMeshShapes(
                new[] { "mouthFunnel", "MOUTHFUNNEL" }, new string[0], supported).Single().Index, Is.EqualTo(0));
        }

        [Test]
        public void SameShapeCanBeBoundInMultipleMeshes()
        {
            var supported = new[] { "MouthFunnel" };
            var firstMesh = PerfectSyncExpressionNames.ResolveMeshShapes(
                new[] { "MouthFunnel" }, new string[0], supported).ToArray();
            var secondMesh = PerfectSyncExpressionNames.ResolveMeshShapes(
                new[] { "Other", "mouthFunnel" }, new string[0], supported).ToArray();
            Assert.That(firstMesh, Is.EqualTo(new[] { ("MouthFunnel", 0) }));
            Assert.That(secondMesh, Is.EqualTo(new[] { ("MouthFunnel", 1) }));
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
            Assert.That(setting.PerfectSyncUseMeshBlendShapes, Is.False);
            setting.PerfectSyncExpressionCaseSensitive = true;
            setting.PerfectSyncUseMeshBlendShapes = true;
            var restored = Newtonsoft.Json.JsonConvert.DeserializeObject<VMagicMirrorConfig.MotionSetting>(
                Newtonsoft.Json.JsonConvert.SerializeObject(setting));
            Assert.That(restored.PerfectSyncExpressionCaseSensitive, Is.True);
            Assert.That(restored.PerfectSyncUseMeshBlendShapes, Is.True);
            restored.ResetToDefault();
            Assert.That(restored.PerfectSyncExpressionCaseSensitive, Is.False);
            Assert.That(restored.PerfectSyncUseMeshBlendShapes, Is.False);
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
