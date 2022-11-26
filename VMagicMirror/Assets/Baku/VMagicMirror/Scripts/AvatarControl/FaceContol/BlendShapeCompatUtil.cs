using System.Collections.Generic;

namespace Baku.VMagicMirror
{
    public static class BlendShapeCompatUtil
    {
        private static readonly Dictionary<string, string> _oldNameToNewName = new Dictionary<string, string>()
        {
            ["Joy"] = "Happy",
            ["Sorrow"] = "Sad",
            ["Fun"] = "Relaxed",
            ["Blink_L"] = "BlinkLeft",
            ["Blink_R"] = "BlinkRight",
        };

        public static string GetVrm10ClipName(string vrm0ClipName)
        {
            return 
                (_oldNameToNewName.TryGetValue(vrm0ClipName, out var newName) ? newName : vrm0ClipName)
                ?? "";
        }
    }
}
