﻿using System;
using System.Windows;

namespace Baku.VMagicMirrorConfig.ViewModel
{
    public class BuddyTransform2DPropertyViewModel
    {
        public BuddyTransform2DPropertyViewModel(
            BuddySettingsSender settingSender,
            BuddyMetadata buddyMetadata,
            BuddyProperty buddyProperty
            )
        {
            _settingSender = settingSender;
            _buddyMetadata = buddyMetadata;
            _metadata = buddyProperty.Metadata;
            _value = buddyProperty.Value;

            var v = _value.Transform2DValue;
            PositionX = new RProperty<float>(v.Position.X, v => SetAndNotify(Value.WithPosition(Position.WithX(v))));
            PositionY = new RProperty<float>(v.Position.Y, v => SetAndNotify(Value.WithPosition(Position.WithY(v))));
            RotationX = new RProperty<float>(v.Rotation.X, v => SetAndNotify(Value.WithRotation(Rotation.WithX(v))));
            RotationY = new RProperty<float>(v.Rotation.Y, v => SetAndNotify(Value.WithRotation(Rotation.WithY(v))));
            RotationZ = new RProperty<float>(v.Rotation.Z, v => SetAndNotify(Value.WithRotation(Rotation.WithZ(v))));
            Scale = new RProperty<float>(v.Scale, v => SetAndNotify(Value.WithScale(v)));

            WeakEventManager<BuddyProperty, EventArgs>.AddHandler(
                buddyProperty, nameof(buddyProperty.Transform2DValueUpdated), OnTransform2DUpdated
                );
        }

        private readonly BuddySettingsSender _settingSender;
        private readonly BuddyMetadata _buddyMetadata;
        private readonly BuddyPropertyMetadata _metadata;
        private readonly BuddyPropertyValue _value;

        private BuddyTransform2D Value => _value.Transform2DValue;
        private BuddyVector2 Position => _value.Transform2DValue.Position;
        private BuddyVector3 Rotation => _value.Transform2DValue.Rotation;

        private bool _silent = false;

        private void SetAndNotify(BuddyTransform2D value)
        {
            _value.Transform2DValue = value;
            if (!_silent)
            {
                _settingSender.NotifyTransform2DProperty(_buddyMetadata, _metadata, _value.Transform2DValue);
            }
        }

        private void OnTransform2DUpdated(object? sender, EventArgs e) => SetValuesSilently();

        public RProperty<string> DisplayName { get; } = new RProperty<string>("");
        public RProperty<string> Description { get; } = new RProperty<string>("");

        public RProperty<float> PositionX { get; }
        public RProperty<float> PositionY { get; }

        public RProperty<float> RotationX { get; }
        public RProperty<float> RotationY { get; }
        public RProperty<float> RotationZ { get; }

        public RProperty<float> Scale { get; }

        public void ApplyLanguage(bool isJapanese)
        {
            DisplayName.Value = _metadata.DisplayName.Get(isJapanese);
            Description.Value = _metadata.Description.Get(isJapanese);
        }

        public void ResetToDefault()
        {
            // NOTE: 値が変更されてなくてもメッセージは1回飛ぶのを許容する。
            // 明示的に1回投げたあと、メッセージが複数回は飛ばないのを保証してUIを更新していく
            SetAndNotify(_metadata.DefaultTransform2DValue);
            SetValuesSilently();
        }

        /// <summary> BuddySettingsSenderは呼び出さないようにしたうえでModel側の現在値を適用する </summary>
        private void SetValuesSilently()
        {
            try
            {
                _silent = true;
                PositionX.Value = Position.X;
                PositionY.Value = Position.Y;
                RotationX.Value = Rotation.X;
                RotationY.Value = Rotation.Y;
                RotationZ.Value = Rotation.Z;
                Scale.Value = Value.Scale;
            }
            finally
            {
                _silent = false;
            }
        }
    }
}
