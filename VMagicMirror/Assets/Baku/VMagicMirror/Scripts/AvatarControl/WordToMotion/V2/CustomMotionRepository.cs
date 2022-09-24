using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Baku.VMagicMirror.MotionExporter;

namespace Baku.VMagicMirror.WordToMotion
{
    /// <summary>
    /// カスタムモーションの内容をローカルフォルダから取得して保持するやつ
    /// </summary>
    public class CustomMotionRepository
    {
        private readonly Dictionary<string, CustomMotionItem> _clips = new Dictionary<string, CustomMotionItem>();
        private bool _initialized;

        public CustomMotionItem GetItem(string motionName)
        {
            InitializeIfNeeded();
            return _clips[motionName.ToLower()];
        }

        public bool ContainsKey(string motionName)
        {
            InitializeIfNeeded();
            return _clips.ContainsKey(motionName.ToLower());
        }

        public string[] LoadAvailableCustomMotionNames()
        {
            InitializeIfNeeded();
            //順序を固定するのがポイント
            return _clips.Values
                .Select(v => v.MotionName)
                .OrderBy(x => x)
                .ToArray();
        }

        private void InitializeIfNeeded()
        {
            if (_initialized)
            {
                return;
            }
            _initialized = true;

            //エディタの場合はStreamingAssets以下で代用(無ければ無いでOK)
            var dirPath = SpecialFiles.MotionsDirectory;
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }            

            var importer = new MotionImporter();
            foreach(var filePath in Directory.GetFiles(dirPath).Where(p => Path.GetExtension(p) == ".vmm_motion"))
            {
                try
                {
                    var motion = importer.LoadSerializedMotion(File.ReadAllText(filePath));
                    if (motion == null)
                    {
                        continue;
                    }
                    var motionName = Path.GetFileNameWithoutExtension(filePath);
                    var flags = motion.LoadMuscleFlags();
                    SerializeMuscleNameMapper.MaskUsedMuscleFlags(flags, MuscleFlagMaskStyle.OnlyUpperBody);
                    _clips[motionName.ToLower()] = new CustomMotionItem(
                        motionName,
                        flags,
                        importer.Deserialize(motion)
                    );
                }
                catch (Exception ex)
                {
                    LogOutput.Instance.Write(ex);
                }
            }
        }
    }
}

