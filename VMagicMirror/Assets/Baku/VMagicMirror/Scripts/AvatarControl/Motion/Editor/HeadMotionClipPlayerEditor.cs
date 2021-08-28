using UnityEditor;
using UnityEngine;

namespace Baku.VMagicMirror
{
    [CustomEditor(typeof(HeadMotionClipPlayer))]
    public class HeadMotionClipPlayerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (!(target is HeadMotionClipPlayer player))
            {
                return;
            }

            if (GUILayout.Button("Stop"))
            {
                player.Stop();
            }
            else if (GUILayout.Button("Nodding"))
            {
                player.PlayNoddingMotion();
            }
            else if (GUILayout.Button("Shaking"))
            {
                player.PlayShakingMotion();
            }
        }
    }
}
