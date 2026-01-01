using NUnit.Framework;

namespace Baku.VMagicMirrorConfig.Test
{
    public class VmmAppVersionTests
    {
        [TestCase("v1.2.3")]
        [TestCase("1.2.3", Description = "冒頭のvは無くてもいい")]
        [Test]
        public void Test_バージョン値パース_正常系(string raw)
        {
            var success = VmmAppVersion.TryParse(raw, out var result);
            Assert.That(success, Is.True);
            Assert.That(result.Major, Is.EqualTo(1));
            Assert.That(result.Minor, Is.EqualTo(2));
            Assert.That(result.Build, Is.EqualTo(3));
        }

        [TestCase("4.6.9.3")]
        [TestCase("4.6.9.abc", Description = "4桁目以降は数値じゃなくても通る")]
        [Test]
        public void Test_バージョン値パース_正常系_4桁表記すると手前の3桁を使う(string raw)
        {
            var success = VmmAppVersion.TryParse(raw, out var result);
            Assert.That(success, Is.True);
            Assert.That(result.Major, Is.EqualTo(4));
            Assert.That(result.Minor, Is.EqualTo(6));
            Assert.That(result.Build, Is.EqualTo(9));
        }

        [TestCase("")]
        [TestCase(null)]
        [Test]
        public void Test_バージョン値パース_異常系_空文字とnull(string raw)
        {
            var success = VmmAppVersion.TryParse(raw, out var result);
            Assert.That(success, Is.False);
            Assert.That(result.Major, Is.EqualTo(0));
            Assert.That(result.Minor, Is.EqualTo(0));
            Assert.That(result.Build, Is.EqualTo(0));
        }

        [TestCase("a1.2.3", Description = "prefixはv以外ダメ")]
        [TestCase("1.2.3a", Description = "suffixがあるのはダメ")]
        [TestCase("1.xxx.2", Description = "途中に変な値があるとダメ")]
        [Test]
        public void Test_バージョン値パース_異常系_書式が悪い(string raw)
        {
            var success = VmmAppVersion.TryParse(raw, out var result);
            Assert.That(success, Is.False);
            Assert.That(result.Major, Is.EqualTo(0));
            Assert.That(result.Minor, Is.EqualTo(0));
            Assert.That(result.Build, Is.EqualTo(0));
        }

        [Test]
        public void Test_バージョン値の比較()
        {
            //メジャーバージョンで決まる
            var a = new VmmAppVersion(2, 3, 4);
            var b = new VmmAppVersion(1, 5, 11);
            Assert.That(a.IsNewerThan(b), Is.True);
            Assert.That(b.IsNewerThan(a), Is.False);

            //マイナーバージョンで決まる
            b = new VmmAppVersion(2, 1, 5);
            Assert.That(a.IsNewerThan(b), Is.True);
            Assert.That(b.IsNewerThan(a), Is.False);

            //ビルドバージョンで決まる
            b = new VmmAppVersion(2, 3, 2);
            Assert.That(a.IsNewerThan(b), Is.True);
            Assert.That(b.IsNewerThan(a), Is.False);

            //等しい: どっちもNewerではない
            b = new VmmAppVersion(2, 3, 4);
            Assert.That(a.IsNewerThan(b), Is.False);
            Assert.That(b.IsNewerThan(a), Is.False);
        }

        [Test]
        public void Test_バージョン値のValid基準()
        {
            Assert.That(new VmmAppVersion(0, 0, 1).IsValid, Is.True);
            Assert.That(new VmmAppVersion(0, 1, 0).IsValid, Is.True);
            Assert.That(new VmmAppVersion(1, 0, 0).IsValid, Is.True);
            //全部0だとNG
            Assert.That(new VmmAppVersion(0, 0, 0).IsValid, Is.False);
        }
    }
}