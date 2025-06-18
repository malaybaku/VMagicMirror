using System.IO;
using UnityEditor;
using UnityEngine;

namespace Baku.VMagicMirror.MotionExporter
{
    [CustomEditor(typeof(MotionExporter))]
    public class MotionExporterEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Export"))
            {
                if (!(target is MotionExporter exporter))
                {
                    Debug.LogError("Failed to get Motion Exporter. Could not save.");
                    return;
                }

                if (exporter.exportTarget == null)
                {
                    Debug.LogError("Export Target is not set. Please set a valid Animation Clip.");
                    return;
                }
                
                var motion = MotionExporterImpl.GetSerializedMotion(exporter.exportTarget);
                SaveSerializedClip(motion, exporter.exportTarget.name);
            }
        }

        private void SaveSerializedClip(SerializedMotion motion, string clipName)
        {
            if (motion == null)
            {
                Debug.LogError("Motion was not exported, please check settings.");
                return;
            }

            string json = JsonUtility.ToJson(motion);
            var filePath = Path.Combine(
                Application.streamingAssetsPath,
                clipName + ".vmm_motion"
            );
            Directory.CreateDirectory(Application.streamingAssetsPath);
            File.WriteAllText(filePath, json);
            Debug.Log("Motion file was saved to: " + filePath);
        }
    }
    
}
