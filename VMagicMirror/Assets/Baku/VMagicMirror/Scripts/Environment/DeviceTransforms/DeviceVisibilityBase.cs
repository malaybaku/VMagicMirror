﻿using Deform;
using DG.Tweening;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    [RequireComponent(typeof(Renderer))]
    [RequireComponent(typeof(MagnetDeformer))]
    public abstract class DeviceVisibilityBase : MonoBehaviour
    {
        [Inject] private DeformableCounter _deformableCounter = null;

        private MagnetDeformer _deformer = null;
        private Renderer _renderer = null;
        public bool IsVisible { get; private set; } = true;

        /// <summary>
        /// <see cref="OnStart"/>以降で参照可能な、メインのレンダラーを取得します。
        /// </summary>
        protected Renderer MainRenderer => _renderer;

        private bool _componentInitialized;

        /// <summary>
        /// Start関数の時点で実行されます。
        /// </summary>
        protected virtual void OnStart()
        {
        }
        
        private void Start()
        {
            InitializeComponents();
            OnStart();
        }

        public void Setup(DeformableCounter deformableCounter)
        {
            //NOTE: このクラスを使ってる全箇所がSetupを呼ぶようになったら _deformableCounterのfield injectionをやめてよい
            _deformableCounter = deformableCounter;
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            if (_componentInitialized)
            {
                return;
            }

            _componentInitialized = true;
            _deformer = GetComponent<MagnetDeformer>();
            _renderer = GetComponentInChildren<Renderer>();
        }
        
        /// <summary>
        /// メインのRendererの有効/無効を書き換えたときに呼び出します。サブのメッシュのvisibilityを変えたりするのに使えます。
        /// </summary>
        /// <param name="enable"></param>
        protected virtual void OnRendererEnableUpdated(bool enable)
        {
        }

        /// <summary>
        /// Deformerの値をDOTweenで更新しているとき呼ばれます。0-1の範囲の値で、0は表示、1は非表示です。
        /// </summary>
        /// <param name="v"></param>
        protected virtual void OnSetMagnetDeformerValue(float v)
        {
        }

        public void SetVisibility(bool visible)
        {
            IsVisible = visible;
            DOTween
                .To(
                    () => _deformer.Factor, 
                    v =>
                    {
                        _deformer.Factor = v;
                        OnSetMagnetDeformerValue(v);
                    }, 
                    visible ? 0.0f : 1.0f, 
                    0.5f)
                .SetEase(Ease.OutCubic)
                .OnStart(() =>
                {
                    _deformableCounter.Increment();
                    if (visible)
                    {
                        _renderer.enabled = true;
                        OnRendererEnableUpdated(true);
                    }
                })
                .OnComplete(() =>
                {
                    _deformableCounter.Decrement();
                    _renderer.enabled = IsVisible;
                    OnRendererEnableUpdated(IsVisible);
                });
        }
    }
}