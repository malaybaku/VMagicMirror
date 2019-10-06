using System.Collections.Generic;
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
            new string[] { "Escape", "_", "_", "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12",                                                       "PrintScreen", "Scroll", "Pause", },
        };

        //指番号。FingerAnimator.Fingersの値を書いてもいいが、字が増えすぎてもアレなので番号で入れてます
        //TOOD: ここのマップを可変にしてもいいかも(ぜんぶ人差し指にしたら一本指打法になるとか)
        private static readonly Dictionary<string, int> fingerMapper = new Dictionary<string, int>()
        {

            #region 1列目の左側

            ["LControlKey"] = 4,
            ["LWin"] = 4,
            ["Alt"] = 4,

            ["Space"] = 0,

            ["RControlKey"] = 9,

            #endregion

            #region 2列目の左側

            ["LShiftKey"] = 4,
            ["Z"] = 4,
            ["X"] = 3,
            ["C"] = 2,
            ["V"] = 1,
            ["B"] = 1,
            ["N"] = 6,
            ["M"] = 6,
            ["Oemcomma"] = 6,
            ["OemPeriod"] = 7,
            ["OemQuestion"] = 7,
            ["OemBackslash"] = 8,
            ["RShiftKey"] = 9,

            #endregion

            #region 3列目の左側

            ["CapsLock"] = 4,
            ["A"] = 4,
            ["S"] = 3,
            ["D"] = 2,
            ["F"] = 1,
            ["G"] = 1,
            ["H"] = 6,
            ["J"] = 6,
            ["K"] = 7,
            ["L"] = 8,
            ["Oemplus"] = 8,
            ["OemSemicolon"] = 8,
            ["OemCloseBrackets"] = 8,
            ["Enter"] = 9,

            #endregion

            #region 4列目の左側

            ["Tab"] = 4,
            ["Q"] = 4,
            ["W"] = 3,
            ["E"] = 2,
            ["R"] = 1, 
            ["T"] = 1,
            ["Y"] = 6,
            ["U"] = 6,
            ["I"] = 6,
            ["O"] = 7,
            ["P"] = 8,
            ["Oemtilde"] = 8,
            ["OemOpenBrackets"] = 8,

            #endregion

            #region 5列目の左側

            ["D1"] = 4,
            ["D2"] = 3,
            ["D3"] = 2,
            ["D4"] = 1,
            ["D5"] = 1,
            ["D6"] = 1,
            ["D7"] = 6,
            ["D8"] = 7,
            ["D9"] = 7,
            ["D0"] = 8,
            ["OemMinus"] = 8,
            ["OemQuotes"] = 8,
            ["OemPipe"] = 8,
            ["Back"] = 8,

            #endregion

            #region 6列目
            ["Escape"] = 4,

            ["F1"] = 3,
            ["F2"] = 3,
            ["F3"] = 2,
            ["F4"] = 2,
            ["F5"] = 1,
            ["F6"] = 1,
            ["F7"] = 6,
            ["F8"] = 6,
            ["F9"] = 7,
            ["F10"] = 7,
            ["F11"] = 8,
            ["F12"] = 8,

            ["PrintScreen"] = 7,
            ["Scroll"] = 7,
            ["Pause"] = 7,
            #endregion

            #region 矢印キーとかのあるエリア

            ["Left"] = 6,
            ["Down"] = 7,
            ["Right"] = 8,

            ["Up"] = 7,

            ["Delete"] = 6,
            ["End"] = 7,
            ["PageDown"] = 8,

            ["Insert"] = 6,
            ["Home"] = 7,
            ["PageUp"] = 8,

            #endregion

            #region テンキー

            ["NumPad0"] = 6,
            ["Decimal"] = 8,

            ["NumPad1"] = 6,
            ["NumPad2"] = 7,
            ["NumPad3"] = 8,

            ["NumPad4"] = 6,
            ["NumPad5"] = 7,
            ["NumPad6"] = 8,

            ["NumPad7"] = 6,
            ["NumPad8"] = 7,
            ["NumPad9"] = 8,

            ["NumLock"] = 6,
            ["Divide"] = 7,
            ["Multiply"] = 8,

            ["Subtract"] = 8,
            ["Add"] = 8,

            #endregion

        };

        //キーボードの各行において、左手で叩く事になっているいちばん右のキーの(keyCodeNamesの中での)列番号
        //スペーサー("_"扱いのキー)がある関係で物理キーボードの値とはズレてる事に注意
        private static readonly int[] _leftHandFingerIndexLimits = new[]
        {
            6,
            6,
            7,
            5,
            6,
            8,
        };

        //左手のみで打鍵するとき、_lefHandFingerIndexLimitsのインデックスまでの各行のキーをどの指で叩くか決めた一覧。
        //スペーサーになってる(本来存在しない)キーを叩く可能性があるのでfingerMapperとは別に定義する必要がある
        private static readonly int[][] _leftHandOnlyFingerMapper = new[]
        {
            new int[] { 4, 4, 3, 3, 2, 2, 0, },
            new int[] { 4, 3, 3, 2, 2, 1, 1, },
            new int[] { 4, 3, 3, 2, 2, 1, 1, 1, },
            new int[] { 4, 3, 3, 2, 1, 1, },
            new int[] { 4, 3, 3, 2, 2, 1, 1, },
            new int[] { 4, 4, 3, 3, 2, 2, 1, 1, 1, },
        };

        
        [SerializeField] private Transform keyPrefab = null;
        [SerializeField] private Vector3 initialPosition = Vector3.zero;
        [SerializeField] private Vector3 initialRotation = Vector3.zero;
        [SerializeField] private Vector3 initialScale = Vector3.one;

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

        private void Start()
        {
            if (keyPrefab != null)
            {
                InitializeKeys();
                CombineMeshes();
                transform.position = initialPosition;
                transform.rotation = Quaternion.Euler(initialRotation);
                transform.localScale = initialScale;
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
                    key.GetComponentInChildren<MeshRenderer>().material = HIDMaterialUtil.Instance.GetKeyMaterial();

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

        private void CombineMeshes()
        {
            int len = transform.childCount;
            var meshFilters = new MeshFilter[len];
            for (int i = 0; i < len; i++)
            {
                meshFilters[i] = transform.GetChild(i).GetComponentInChildren<MeshFilter>();
            }
            CombineInstance[] combine = new CombineInstance[meshFilters.Length];

            for (int i = 0; i < meshFilters.Length; i++)
            {
                combine[i].mesh = meshFilters[i].sharedMesh;
                combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
                meshFilters[i].gameObject.SetActive(false);
            }

            var meshFilter = GetComponent<MeshFilter>();
            meshFilter.mesh = new Mesh();
            meshFilter.mesh.CombineMeshes(combine);
        }

        public KeyTargetData GetKeyTargetData(string key, bool isLeftHandOnly = false)
            => isLeftHandOnly ? GetLeftHandTargetData(key) : GetTwoHandKeyTargetData(key);
        
        private KeyTargetData GetTwoHandKeyTargetData(string key)
        {
            var (row, col) = GetKeyIndex(key);
            var keyTransform = _keys[row][col];

            int fingerNumber = GetFingerNumberOfKey(key);
            return new KeyTargetData()
            {
                row = row,
                col = col,
                fingerNumber = fingerNumber,
                keyTransform = keyTransform,
                position = keyTransform.position,
                positionWithOffset = keyTransform.position + FingerNumberToOffset(fingerNumber),
            };
        }

        public KeyTargetData GetLeftHandTargetData(string key)
        {
            var rawData = GetTwoHandKeyTargetData(key);
            //元から左手で叩くキーはそのままでOK
            if (rawData.IsLeftHandPreffered)
            {
                return rawData;
            }

            //剰余により、右側にあるはずのキーを勝手に左側のキーに充て直す
            int limitIndex = _leftHandFingerIndexLimits[rawData.row];
            int modCol = rawData.col % (limitIndex + 1);
            var keyTransform = _keys[rawData.row][modCol];
            //割り当てたキーがスペーサー("_")の場合があるため、専用の方法で指を割り当てる。
            int fingerNumber = _leftHandOnlyFingerMapper[rawData.row][modCol];
            
            return new KeyTargetData()
            {
                row = rawData.row,
                col = modCol,
                fingerNumber = fingerNumber,
                keyTransform = keyTransform,
                position = keyTransform.position,
                positionWithOffset = keyTransform.position + FingerNumberToOffset(fingerNumber),
            };
        }

        private static int GetFingerNumberOfKey(string key)
        {
            string sanitized = SanitizeKey(key);
            //不明な場合は[0][0]なので左手小指にしておけばOK
            return fingerMapper.ContainsKey(sanitized) ?
                fingerMapper[sanitized] :
                FingerConsts.LeftLittle;
        }

        //row: 手前から数えた0始まりの行、例えばASDFキーがあるのは第2行
        //col: 左から数えた0始まりの列、例えばFキーは第4列
        private (int row, int col) GetKeyIndex(string key)
        {
            string sanitized = SanitizeKey(key);
            for (int i = 0; i < keyCodeNames.Length; i++)
            {
                int len = keyCodeNames[i].Length;
                for (int j = 0; j < keyCodeNames[i].Length; j++)
                {
                    if (keyCodeNames[i][j] == sanitized)
                    {
                        return (i, j);
                    }
                }
            }
            
            //ランダムとかでもいいが。
            return (0, 0);
        }
        
        private static Vector3 FingerNumberToOffset(int fingerNumber)
        {
            //NOTE: いったん面倒なので決め打ちする。指と指の間隔。
            const float fingerHorizontalLengthUnit = 0.015f;

            //何もしないと中指の位置で合わせに行くと考え、そこから隣の指を使うときに横へ調整
            float length = 0;
            switch (fingerNumber)
            {
                case FingerConsts.LeftThumb:
                    length = -2 * fingerHorizontalLengthUnit;
                    break;
                case FingerConsts.LeftIndex:
                    length = -2 * fingerHorizontalLengthUnit;
                    break;
                case FingerConsts.LeftMiddle:
                    length = 0 * fingerHorizontalLengthUnit;
                    break;
                case FingerConsts.LeftRing:
                    length = 1 * fingerHorizontalLengthUnit;
                    break;
                case FingerConsts.LeftLittle:
                    length = 2 * fingerHorizontalLengthUnit;
                    break;
                case FingerConsts.RightThumb:
                    length = 2 * fingerHorizontalLengthUnit;
                    break;
                case FingerConsts.RightIndex:
                    length = 2 * fingerHorizontalLengthUnit;
                    break;
                case FingerConsts.RightMiddle:
                    length = 0 * fingerHorizontalLengthUnit;
                    break;
                case FingerConsts.RightRing:
                    length = -1 * fingerHorizontalLengthUnit;
                    break;
                case FingerConsts.RightLittle:
                    length = -2 * fingerHorizontalLengthUnit;
                    break;
                default:
                    break;
            }

            return Vector3.right * length;
        }

        private static string SanitizeKey(string key)
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

        public struct KeyTargetData
        {
            public int row;
            public int col;
            public int fingerNumber;
            public Transform keyTransform;
            public Vector3 position;
            public Vector3 positionWithOffset;

            public bool IsLeftHandPreffered => fingerNumber < 5;
        }
    }
}
