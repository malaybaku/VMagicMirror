using NUnit.Framework;

namespace Baku.VMagicMirrorConfig.Test
{
    public class ReleaseNoteTests
    {
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

        // 下記をそれぞれ見ている
        // - httpsではないケース
        // - ホストがgithub.comではないケース
        // - パスが /user-attachments/assets/ から始まってないケース
        [TestCase(
@"2021/10/24

Japanese:

- 追加: hoge.
- 修正: fuga.

English:

- Add: Foo
- Fix: Bar

Note:

- This is note area which should be ignored in parse process.

Image: http://example.com/user-attachments/assets/xxxx-xxxx-xxxx
")]
        [TestCase(
@"2021/10/24

Japanese:

- 追加: hoge.
- 修正: fuga.

English:

- Add: Foo
- Fix: Bar

Note:

- This is note area which should be ignored in parse process.

Image: https://example.com/user-attachments/assets/xxxx-xxxx-xxxx
")]
        [TestCase(
@"2021/10/24

Japanese:

- 追加: hoge.
- 修正: fuga.

English:

- Add: Foo
- Fix: Bar

Note:

- This is note area which should be ignored in parse process.

Image: https://github.com/invalid-path-example/xxxx-xxxx-xxxx
")]
        public void Test_リリースノート異常系_ImageUrlにヘンな値が入ってる場合は無視する(string rawReleaseNote)
        {
            var note = ReleaseNote.FromRawString(rawReleaseNote);
            // Dateを見るのはリリースノート全体としてのパース成功判定のため
            Assert.That(note.DateString, Is.EqualTo("2021/10/24"));
            Assert.That(note.ImageUrl, Is.Null);
        }
    }
}