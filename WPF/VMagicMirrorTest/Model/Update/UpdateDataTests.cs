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

            Assert.That(note.DateString, Is.EqualTo("2021/10/24"));
            Assert.That(note.JapaneseNote, Is.EqualTo("- 追加: hoge.\n- 修正: fuga."));
            Assert.That(note.EnglishNote, Is.EqualTo("- Add: Foo\n- Fix: Bar"));
        }

        [Test]
        public void Test_リリースノート正常系_画像URLつき()
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

Image: https://github.com/user-attachments/assets/xxxx-xxxx-xxxx
");

            Assert.That(note.DateString, Is.EqualTo("2021/10/24"));
            Assert.That(note.JapaneseNote, Is.EqualTo("- 追加: hoge.\n- 修正: fuga."));
            Assert.That(note.EnglishNote, Is.EqualTo("- Add: Foo\n- Fix: Bar"));
            Assert.That(note.ImageUrl!.AbsoluteUri, Is.EqualTo(new System.Uri("https://github.com/user-attachments/assets/xxxx-xxxx-xxxx")));
        }

        [TestCase("")]
        [TestCase(null)]
        [Test]
        public void Test_リリースノート異常系_空文字とかnull(string rawNote)
        {
            var note = ReleaseNote.FromRawString(rawNote);
            Assert.That(note.DateString, Is.EqualTo(""));
            Assert.That(note.JapaneseNote, Is.EqualTo(""));
            Assert.That(note.EnglishNote, Is.EqualTo(""));
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
            Assert.That(note.DateString, Is.EqualTo(""));
            Assert.That(note.JapaneseNote, Is.EqualTo("- 追加: hoge.\n- 修正: fuga."));
            Assert.That(note.EnglishNote, Is.EqualTo("- Add: Foo\n- Fix: Bar"));
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
            Assert.That(note.DateString, Is.EqualTo(""));
            Assert.That(note.JapaneseNote, Is.EqualTo(rawReleaseNote));
            Assert.That(note.EnglishNote, Is.EqualTo(rawReleaseNote));
        }

        [Test]
        public void Test_リリースノート異常系_NoteなしでImageを定義するとEnglish部分に画像情報が混入する()
        {
            var note = ReleaseNote.FromRawString(@"2021/10/24

Japanese:

- 追加: hoge.
- 修正: fuga.

English:

- Add: Foo
- Fix: Bar

Image: https://github.com/user-attachments/assets/xxxx-xxxx-xxxx
");

            Assert.That(note.EnglishNote.Contains("Image:"), Is.True);
            Assert.That(note.ImageUrl!.AbsoluteUri, Is.EqualTo(new System.Uri("https://github.com/user-attachments/assets/xxxx-xxxx-xxxx")));
        }
    }
}