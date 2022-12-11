using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Baku.VMagicMirror
{
    public class ImageAccessoryActions : AccessoryFileActionsBase
    {
        public ImageAccessoryActions(AccessoryFile file, Texture2D texture)
        {
            _file = file;
            _texture = texture;
            _rawSize = _texture != null ? Math.Max(_texture.width, _texture.height) : 0;
        }
        private readonly AccessoryFile _file;
        private Texture2D _texture;
        private int _rawSize;

        public override void UpdateLayout(AccessoryItemLayout layout)
        {
            if (_file.Bytes == null || _file.Bytes.Length == 0)
            {
                return;
            }

            AccessoryTextureResizer.ResizeImage(
                _texture, layout.GetResolutionLimitSize(), _file.Bytes, _rawSize
                );
        }

        public override void Dispose()
        {
            if (_texture != null)
            {
                Object.Destroy(_texture);                    
            }
            _texture = null;
        }
    }
}