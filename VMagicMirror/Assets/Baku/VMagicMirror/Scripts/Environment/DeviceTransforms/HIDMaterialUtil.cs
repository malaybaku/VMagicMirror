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

        private Material _penTabletMaterial;
        private Material _penMaterial;
        private Material _arcadeStickMaterial;
        private Material _carHandleMaterial;

        public Material GetKeyMaterial()
            => _keyMaterial ??= LoadMaterial("key.png", "Key", "Key");

        public Material GetPadMaterial()
            => _padMaterial ??= LoadMaterial("pad.png", "Pad", "Pad");

        public Material GetGamepadBodyMaterial()
            => _buttonMaterial ??= LoadMaterial("gamepad_body.png", "GamepadBody", "");

        public Material GetGamepadButtonMaterial()
            => _stickAreaMaterial= LoadMaterial("gamepad_button.png", "GamepadButton", "GamepadButton");

        public Material GetMidiNoteMaterial()
            => _midiNoteMaterial ??= LoadMaterial("midi_note.png", "MidiNote", "MidiNote");

        public Material GetMidiKnobMaterial()
            => _midiKnobMaterial ??= LoadMaterial("midi_knob.png", "MidiKnob", "MidiKnob");

        //NOTE: ペンタブ/アケコンはデフォルトのテクスチャを使いまわしてることに注意。
        public Material GetPenTabletMaterial()
            => _penTabletMaterial ??= LoadMaterial("pen_tablet.png", "PenTablet", "Pad");
        
        public Material GetPenMaterial()
            => _penMaterial ??= LoadMaterial("pen.png", "Pen", "Pen");

        public Material GetArcadeStickMaterial()
            => _arcadeStickMaterial ??= LoadMaterial("arcade_stick.png", "ArcadeStickItem", "Key");

        public Material GetCarHandleMaterial()
            => _carHandleMaterial ??= LoadMaterial("car_handle.png", "CarHandle", "CarHandle");
        
        private Material LoadMaterial(string textureFileName, string materialName, string defaultTextureName)
        {
            var result = Resources.Load<Material>("Materials/" + materialName);
            try
            {
                string imagePath = SpecialFiles.GetTextureReplacementPath(textureFileName);
                if (File.Exists(imagePath))
                {
                    var bytes = File.ReadAllBytes(imagePath);
                    var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
                    texture.LoadImage(bytes);
                    texture.Apply(false, true);
                    result.mainTexture = texture;
                }
                else if (!string.IsNullOrEmpty(defaultTextureName))
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

