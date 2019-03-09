using UnityEngine;

namespace Baku.VMagicMirror
{

    public class KeyboardProvider : MonoBehaviour
    {
        //System.Windows.Forms.Keysの値を見ながら、手元のキーボードを参考に。
        //"_"とあるのは基本的に叩かれないスペーサー
        //ここの表記が日本語キーボードを上下逆にしたようなジャグ配列になっていればOK
        //見た目で確認できるようにあえて横長に書いておく。
        private readonly static string[][] keyCodeNames = new string[][]
        {
            new string[] { "LControlKey", "LWin", "Alt", "_",  "_", "_", "Space", "_",  "_", "_", "_", "_",         "_",                             "_",       "RControlKey",  "Left",   "Down", "Right",        "NumPad0", "_",       "Decimal",  "_", },
            new string[] { "LShiftKey", "_", "Z", "X",   "C",  "V",  "B",  "N",     "M",  "Oemcomma", "OemPeriod", "OemQuestion", "OemBackslash",    "_",         "RShiftKey",  "_",      "Up",   "_",            "NumPad1", "NumPad2", "NumPad3",  "_" },
            new string[] { "CapsLock", "_",  "A",    "S",   "D",  "F",  "G",  "H",     "J",  "K",        "L",      "Oemplus",     "OemSemicolon", "OemCloseBrackets", "Enter",  "_",      "_",    "_",            "NumPad4", "NumPad5", "NumPad6",  "_"},
            new string[] { "Tab",         "Q",    "W",   "E",  "R",  "T",  "Y",     "U",  "I",        "O",         "P",           "Oemtilde", "OemOpenBrackets",     "_", "_",  "Delete", "End",  "PageDown",     "NumPad7", "NumPad8", "NumPad9",  "Add", },
            new string[] { "_",           "D1",   "D2",  "D3", "D4", "D5", "D6",    "D7", "D8",       "D9",        "D0",          "OemMinus", "OemQuotes", "OemPipe",  "Back",  "Insert", "Home", "PageUp",       "NumLock", "Divide",  "Multiply", "Subtract", },
            new string[] { "Escape", "_", "_", "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12",                                                       "Printscreen", "Scroll", "Pause", },
        };

        //スペーサーに注意しつつ、左手で打鍵できる限界のキーのひとつ右のインデックスを列順に指定する。
        private readonly static int[] LeftHandKeyThresholds = new int[]
        {
            7,
            7,
            8,
            7,
            8,
            8,
        };

        [SerializeField]
        Transform keyPrefab = null;

        [SerializeField]
        float[] radius = new float[]
        {
            0.2f,
            0.26f,
            0.32f,
            0.38f,
            0.44f,
            0.50f
        };

        [SerializeField]
        float[] angleOffset = new float[]
        {
            0.0f,
            2.5f,
            0.0f,
            2.0f,
            0.0f,
        };

        [SerializeField]
        float[] anglePerItem = new float[]
        {
            5.0f,
            5.0f,
            4.0f,
            4.0f,
            3.0f,
        };


        private Transform[][] _keys = null;

        void Start()
        {
            if (keyPrefab != null)
            {
                InitializeKeys();
            }
        }

        private void InitializeKeys()
        {
            _keys = new Transform[keyCodeNames.Length][];

            for (int i = 0; i < keyCodeNames.Length; i++)
            {
                _keys[i] = new Transform[keyCodeNames[i].Length];

                float angleAdjust = angleOffset[i] - keyCodeNames[i].Length * 0.5f * anglePerItem[i];
                for (int j = 0; j < keyCodeNames[i].Length; j++)
                {
                    float angle = (j * anglePerItem[i] + angleAdjust) * Mathf.Deg2Rad;
                    var key = Instantiate(keyPrefab, this.transform);

                    key.localPosition = radius[i] * new Vector3(
                        Mathf.Sin(angle),
                        0,
                        Mathf.Cos(angle)
                        );

                    var child = key.GetChild(0);
                    child.localRotation = Quaternion.Euler(
                        child.localRotation.eulerAngles.x, 
                        angle * Mathf.Rad2Deg, 
                        0
                        );

                    _keys[i][j] = key.transform;
                }
            }
        }

        public Vector3 GetPositionOfKey(string key)
        {
            return GetTransformOfKey(key).position;
        }

        public bool IsLeftHandPreffered(string key)
        {
            string sanitized = SanitizeKey(key);
            for (int i = 0; i < keyCodeNames.Length; i++)
            {
                int len = keyCodeNames[i].Length;
                for (int j = 0; j < keyCodeNames[i].Length; j++)
                {
                    if (keyCodeNames[i][j] == sanitized)
                    {
                        return (j < LeftHandKeyThresholds[i]);
                    }
                }
            }

            //不明な場合は[0][0]扱いになるので左手扱いが妥当
            return true;
        }

        private Transform GetTransformOfKey(string key)
        {
            string sanitized = SanitizeKey(key);
            for (int i = 0; i < keyCodeNames.Length; i++)
            {
                int len = keyCodeNames[i].Length;
                for (int j = 0; j < keyCodeNames[i].Length; j++)
                {
                    if (keyCodeNames[i][j] == sanitized)
                    {
                        return _keys[i][j];
                    }
                }
            }

            //ランダムとかでもいいが。
            return _keys[0][0];
        }

        private string SanitizeKey(string key)
        {
            //note: エイリアスのあるキー名を一方向に倒す。
            switch (key)
            {
                case "Return":
                    return "Enter";
                case "Capital":
                    return "CapsLock";
                case "Prior":
                    return "PageUp";
                case "Next":
                    return "PageDown";
                case "Snapshot":
                    return "PrintScreen";
                case "Oem1":
                    return "OemSemicolon";
                case "Oem2":
                    return "OemQuestion";
                case "Oem3":
                    return "Oemtilde";
                case "Oem4":
                    return "OemOpenBrackets";
                case "Oem5":
                    return "OemPipe";
                case "Oem6":
                    return "OemCloseBrackets";
                case "Oem7":
                    return "OemQuotes";
                case "Oem102":
                    return "OemBackslash";
                default:
                    return key;
            }
        }

    }
}
