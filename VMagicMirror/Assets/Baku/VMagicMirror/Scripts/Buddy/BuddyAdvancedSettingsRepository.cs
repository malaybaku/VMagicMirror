using System;
using System.IO;
using R3;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.Buddy
{
    public sealed class BuddyAdvancedSettingsRepository : IInitializable
    {
        [Serializable]
        public class Settings
        {
            [SerializeField] private bool enableAdvancedFeatures;
            
            public bool EnableRDirective => enableAdvancedFeatures;
            public bool SkipNamespaceLimitation => enableAdvancedFeatures;
        }

        private readonly ReactiveProperty<bool> _enableRDirective = new(false);
        public ReadOnlyReactiveProperty<bool> EnableRDirective => _enableRDirective;
        
        private readonly ReactiveProperty<bool> _skipNamespaceLimitation = new(false);
        public ReadOnlyReactiveProperty<bool> SkipNamespaceLimitation => _skipNamespaceLimitation;

        [Inject]
        public BuddyAdvancedSettingsRepository() { }
        
        void IInitializable.Initialize() => LoadSettings();

        // NOTE: 後からWPF経由でリロードしてもいい想定なのでpublicにしてる
        public void LoadSettings()
        {
            var file = SpecialFiles.BuddyAdvancedSettingsFilePath;
            if (!File.Exists(file))
            {
                _enableRDirective.Value = false;
                _skipNamespaceLimitation.Value = false;
                return;
            }

            try
            {
                var json = File.ReadAllText(file);
                var settings = JsonUtility.FromJson<Settings>(json);
                _enableRDirective.Value = settings.EnableRDirective;
                _skipNamespaceLimitation.Value = settings.SkipNamespaceLimitation;
            }
            catch (Exception e)
            {
                LogOutput.Instance.Write(e);
            }
        }
    }
}
