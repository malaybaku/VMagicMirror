﻿using System;
using System.Text;
using UniRx;

namespace Baku.VMagicMirror
{
    public class WordAnalyzer 
    {
        //検出対象となる単語一覧
        private string[] _wordSet = new string[0];
        //検出対象ワードの最長の文字数
        private int _longestWordLength = 0;

        private Subject<string> _wordDetected = new Subject<string>();
        /// <summary>単語を検出すると発火します。</summary>
        public IObservable<string> WordDetected => _wordDetected;

        //キューの方がいいかも
        private StringBuilder _sb = new StringBuilder(64);

        /// <summary>文字を入力します。</summary>
        /// <param name="c"></param>
        public void Add(char c)
        {
            _sb.Append(c);

            //単語一覧から探す
            string s = _sb.ToString();
            for(int i = 0; i < _wordSet.Length; i++)
            {
                if (s.IndexOf(_wordSet[i]) >= 0)
                {
                    _wordDetected.OnNext(_wordSet[i]);
                    //ふつう末尾で一致しているハズだから、全消しでも無害
                    Clear();
                }
            }

            if (_sb.Length > _longestWordLength)
            {
                //通常は第二引数は1になるはず(1文字ずつ追加しているから)
                _sb.Remove(0, _sb.Length - _longestWordLength);
            }
        }

        /// <summary>ユーザーが入力中の単語を空の状態に戻します。</summary>
        /// <remarks>一定時間以上入力がないときに呼び出す想定</remarks>
        public void Clear()
        {
            _sb.Clear();
        }

        /// <summary>
        /// 検出対象となるワード一覧を設定します。
        /// </summary>
        /// <param name="words">検出すべき単語一覧。</param>
        /// <remarks>wordsは順番に意味がある(複数ヒット時はインデックスが手前のワード優先)ので注意</remarks>
        public void LoadWordSet(string[] words)
        {
            _wordSet = new string[words.Length];
            Array.Copy(words, _wordSet, words.Length);

            _longestWordLength = 0;
            for(int i = 0; i < _wordSet.Length; i++)
            {
                if (_wordSet[i].Length > _longestWordLength)
                {
                    _longestWordLength = _wordSet[i].Length;
                }
            }
        }

        /// <summary>検出対象となるワード一覧を空に戻します。</summary>
        public void ResetWordSet()
        {
            _wordSet = new string[0];
            _longestWordLength = 0;
        }

    }
}
