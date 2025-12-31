using NUnit.Framework;
using System.Windows.Input;

namespace Baku.VMagicMirror.Test.ApiBehavior
{
    public class KeyInteropTests
    {
        [Test]
        public void VirtualKeyFromKey_アルファベットは普通に通る()
        {
            //NOTE: 65 = Windows.Forms.Keys.A
            const int Start = 65;
            for (var i = 0; i < 26; i++)
            {
                var k = KeyInterop.VirtualKeyFromKey(Key.A + i);
                Assert.That(Start + i, Is.EqualTo(k));
            }
        }

        [TestCase(Key.LWin, 91)]
        [TestCase(Key.RWin, 92)]
        [TestCase(Key.LeftShift, 160)]
        [TestCase(Key.RightShift, 161)]
        [TestCase(Key.LeftCtrl, 162)]
        [TestCase(Key.RightCtrl, 163)]
        [TestCase(Key.LeftAlt, 164)]
        [TestCase(Key.RightAlt, 165)]
        [Test]
        public void VirtualKeyFromKey_特殊なキーが想定通りにマップされる(Key key, int num)
        {
            var k = KeyInterop.VirtualKeyFromKey(key);
            Assert.That(k, Is.EqualTo(num));
        }
    }
}
