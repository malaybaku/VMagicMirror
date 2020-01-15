using System;
using System.IO;
using UnityEngine;

namespace Baku.VMagicMirror
{
    public class HIDMaterialUtil
    {
        private HIDMaterialUtil() { }
        private static HIDMaterialUtil _instance;
        public static HIDMaterialUtil Instance
            => _instance ?? (_instance = new HIDMaterialUtil());

        private Material _keyMaterial;
        private Material _padMaterial;
        private Material _buttonMaterial;
        private Material _stickAreaMaterial;
        private Material _midiNoteMaterial;
        private Material _midiKnobMaterial;

        public Material GetKeyMaterial()
            => _keyMaterial ?? (_keyMaterial = LoadMaterial(
                   "key.png", "Key", "Key"));

        public Material GetPadMaterial()
            => _padMaterial ?? (_padMaterial = LoadMaterial(
                   "pad.png", "Pad", "Pad"));

        public Material GetGamepadBodyMaterial()
            => _buttonMaterial ?? (_buttonMaterial = LoadMaterial(
                   "gamepad_body.png", "GamepadBody", "GamepadBody"));

        public Material GetGamepadButtonMaterial()
            => _stickAreaMaterial ?? (_stickAreaMaterial = LoadMaterial(
                   "gamepad_button.png", "GamepadButton", "GamepadButton"));

        public Material GetMidiNoteMaterial()
            => _midiNoteMaterial ?? (_midiNoteMaterial = LoadMaterial(
                   "midi_note.png", "MidiNote", "MidiNote"));

        public Material GetMidiKnobMaterial()
            => _midiKnobMaterial ?? (_midiKnobMaterial = LoadMaterial(
                   "midi_knob.png", "MidiKnob", "MidiKnob"));
        
        
        private Material LoadMaterial(string textureFileName, string materialName, string defaultTextureName)
        {
            var result = Resources.Load<Material>("Materials/" + materialName);
            try
            {
                string imagePath = Path.Combine(Application.streamingAssetsPath, textureFileName);
                if (File.Exists(imagePath))
                {
                    var bytes = File.ReadAllBytes(imagePath);
                    var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
                    texture.LoadImage(bytes);
                    texture.Apply(false, true);
                    result.mainTexture = texture;
                }
                else
                {
                    result.mainTexture = Resources.Load<Texture2D>("Textures/" + defaultTextureName);
                }
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);
            }
            return result;
        }
    }
}

