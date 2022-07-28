using System;

namespace Baku.VMagicMirrorConfig
{
    public class PreferenceData
    {
        //public string PreferredLanguageName { get; set; } = "";
        //Automationもこっちに保存してもよいと思う

        public HotKeySetting? HotKeySetting { get; set; }

        public PreferenceData Validate()
        {
            if (HotKeySetting == null)
            {
                HotKeySetting = new HotKeySetting();
            }

            return this;
        }

        public static PreferenceData LoadDefault()
        {
            return new ()
            {
                HotKeySetting = new HotKeySetting()
            };
        }
    }
}
