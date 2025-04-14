using UnityEngine;

namespace Baku.VMagicMirror.MediaPipeTracker
{
    // true / falseを連続で一定回数以上叩き込むと初めてフラグが切り替わるようなステート
    public class CounterBoolState
    {
        private readonly int _onCount;
        private readonly int _offCount;
        private bool _targetValue;
        private int _currentCount;

        private int Count => _targetValue ? _onCount : _offCount;

        /// <summary>
        /// <see cref="Set"/> の呼び出し回数を踏まえた現在値を取得する
        /// </summary>
        public bool Value { get; private set; }

        // Valueとしてならす前の、単に直近で 
        /// <summary>
        /// <see cref="Value"/> に整形するより前の、単に直近で <see cref="Set"/> または <see cref="Reset"/> で渡された値を取得する
        /// </summary>
        public bool LatestSetValue { get; private set; }
        
        public CounterBoolState(int onCount, int offCount, bool initialState = false)
        {
            _onCount = onCount;
            _offCount = offCount;
            _targetValue = initialState;
            _currentCount = initialState ? _onCount : _offCount;
            Value = initialState;
            LatestSetValue = initialState;
        }

        public void Reset(bool initialState)
        {
            _targetValue = initialState;
            _currentCount = initialState ? _onCount : _offCount;
            Value = initialState;
            LatestSetValue = initialState;
        }
        
        public void Set(bool value)
        {
            LatestSetValue = value;
            if (value == _targetValue)
            {
                _currentCount = Mathf.Min(_currentCount + 1, Count);
            }
            else
            {
                _currentCount = 1;
                _targetValue = value;
            }

            if (_currentCount >= Count)
            {
                Value = _targetValue;
            }
        }
    }
}