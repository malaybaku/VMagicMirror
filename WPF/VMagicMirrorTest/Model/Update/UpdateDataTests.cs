using NUnit.Framework;

namespace Baku.VMagicMirrorConfig.Test
{
    public class UpdateDataTests
    {
        [TestCase("v1.2.3")]
        [TestCase("1.2.3", Description = "冒頭のvは無くてもいい")]
        [Test]
        public void Test_バージョン値パース_正常系(string raw)
        {
            var success = VmmAppVersion.TryParse(raw, out var result);
            Assert.IsTrue(success);
            Assert.AreEqual(1, result.Major);
            Assert.AreEqual(2, result.Minor);
            Assert.AreEqual(3, result.Build);
        }

        [TestCase("4.6.9.3")]
        [TestCase("4.6.9.abc", Description = "4桁目以降は数値じゃなくても通る")]
        [Test]
        public void Test_バージョン値パース_正常系_4桁表記すると手前の3桁を使う(string raw)
        {
            var success = VmmAppVersion.TryParse(raw, out var result);
            Assert.IsTrue(success);
            Assert.AreEqual(4, result.Major);
            Assert.AreEqual(6, result.Minor);
            Assert.AreEqual(9, result.Build);
        }

        [TestCase("")]
        [TestCase(null)]
        [Test]
        public void Test_バージョン値パース_異常系_空文字とnull(string raw)
        {
            var success = VmmAppVersion.TryParse(raw, out var result);
            Assert.IsFalse(success);
            Assert.AreEqual(0, result.Major);
            Assert.AreEqual(0, result.Minor);
            Assert.AreEqual(0, result.Build);
        }

        [TestCase("a1.2.3", Description = "prefixはv以外ダメ")]
        [TestCase("1.2.3a", Description = "suffixがあるのはダメ")]
        [TestCase("1.xxx.2", Description = "途中に変な値があるとダメ")]
        [Test]
        public void Test_バージョン値パース_異常系_書式が悪い(string raw)
        {
            var success = VmmAppVersion.TryParse(raw, out var result);
            Assert.IsFalse(success);
            Assert.AreEqual(0, result.Major);
            Assert.AreEqual(0, result.Minor);
            Assert.AreEqual(0, result.Build);
        }

        [Test]
        public void Test_バージョン値の比較()
        {
            //メジャーバージョンで決まる
            var a = new VmmAppVersion(2, 3, 4);
            var b = new VmmAppVersion(1, 5, 11);
            Assert.IsTrue(a.IsNewerThan(b));
            Assert.IsFalse(b.IsNewerThan(a));

            //マイナーバージョンで決まる
            b = new VmmAppVersion(2, 1, 5);
            Assert.IsTrue(a.IsNewerThan(b));
            Assert.IsFalse(b.IsNewerThan(a));

            //ビルドバージョンで決まる
            b = new VmmAppVersion(2, 3, 2);
            Assert.IsTrue(a.IsNewerThan(b));
            Assert.IsFalse(b.IsNewerThan(a));

            //等しい: どっちもNewerではない
            b = new VmmAppVersion(2, 3, 4);
            Assert.IsFalse(a.IsNewerThan(b));
            Assert.IsFalse(b.IsNewerThan(a));
        }

        [Test]
        public void Test_バージョン値のValid基準()
        {
            Assert.IsTrue(new VmmAppVersion(0, 0, 1).IsValid);
            Assert.IsTrue(new VmmAppVersion(0, 1, 0).IsValid);
            Assert.IsTrue(new VmmAppVersion(1, 0, 0).IsValid);
            //全部0だとNG
            Assert.IsFalse(new VmmAppVersion(0, 0, 0).IsValid);
        }

        [Test]
        public void Test_リリースノート正常系()
        {
            var note = ReleaseNote.FromRawString(
@"2021/10/24

Japanese:

- 追加: hoge.
- 修正: fuga.

English:

- Add: Foo
- Fix: Bar

Note:

- This is note area which should be ignored in parse process.
");

            Assert.AreEqual("2021/10/24", note.DateString);
            Assert.AreEqual("- 追加: hoge.\n- 修正: fuga.", note.JapaneseNote);
            Assert.AreEqual("- Add: Foo\n- Fix: Bar", note.EnglishNote);
        }

        [TestCase("")]
        [TestCase(null)]
        [Test]
        public void Test_リリースノート異常系_空文字とかnull(string rawNote)
        {
            var note = ReleaseNote.FromRawString(rawNote);
            Assert.AreEqual("", note.DateString);
            Assert.AreEqual("", note.JapaneseNote);
            Assert.AreEqual("", note.EnglishNote);
        }

        [TestCase(@"
Japanese:

- 追加: hoge.
- 修正: fuga.

English:

- Add: Foo
- Fix: Bar

Note:

- This is note area which should be ignored in parse process.
")]
        [TestCase(@"2048/May/11

Japanese:

- 追加: hoge.
- 修正: fuga.

English:

- Add: Foo
- Fix: Bar

Note:

- This is note area which should be ignored in parse process.
")]
        [Test]
        public void Test_リリースノート異常系_日付がないか書式異常の場合は空(string rawNote)
        {
            var note = ReleaseNote.FromRawString(rawNote);
            Assert.AreEqual("", note.DateString);
            Assert.AreEqual("- 追加: hoge.\n- 修正: fuga.", note.JapaneseNote);
            Assert.AreEqual("- Add: Foo\n- Fix: Bar", note.EnglishNote);
        }

        [TestCase(@"2021/10/24

Japanese

- 追加: hoge.
- 修正: fuga.

English

- Add: Foo
- Fix: Bar

Note:

- This is note area which should be ignored in parse process.
")]
        [TestCase(@"2021/10/24

Japanese

- 追加: hoge.
- 修正: fuga.

English:

- Add: Foo
- Fix: Bar

Note:

- This is note area which should be ignored in parse process.
")]
        [Test]
        public void Test_リリースノート異常系_区切りが汚いとJPもENも全文入る(string rawReleaseNote)
        {
            var note = ReleaseNote.FromRawString(rawReleaseNote);

            //文中に日付がある場合は全文のほうに入ってればいいので、DateString側が空になってるのが正、というのがポイント
            Assert.AreEqual("", note.DateString);
            Assert.AreEqual(rawReleaseNote, note.JapaneseNote);
            Assert.AreEqual(rawReleaseNote, note.EnglishNote);
        }
    }
}