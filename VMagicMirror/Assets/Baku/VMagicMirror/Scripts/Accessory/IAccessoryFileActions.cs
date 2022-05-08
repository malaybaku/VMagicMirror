using UnityEngine;

namespace Baku.VMagicMirror
{
    public interface IAccessoryFileActions
    {
        void Dispose();
        void Update(float deltaTime);
        void UpdateLayout(AccessoryItemLayout layout);
        //NOTE: isVisibleが切り替わってなくても呼ばれる事がある(現行実装では冗長に呼び出しても基本無害なので…)
        void OnVisibilityChanged(bool isVisible);
    }

    public abstract class AccessoryFileActionsBase : IAccessoryFileActions
    {
        public virtual void Dispose() { }
        public virtual void Update(float deltaTime) { }
        public virtual void UpdateLayout(AccessoryItemLayout layout) { }
        public virtual void OnVisibilityChanged(bool isVisible) { }
    }
    
    public class ImageAccessoryActions : AccessoryFileActionsBase
    {
        public ImageAccessoryActions(Texture2D texture)
        {
            _texture = texture;
        }
        private Texture2D _texture;

        public override void Dispose()
        {
            if (_texture != null)
            {
                Object.Destroy(_texture);                    
            }
            _texture = null;
        }
    }
        
    public class GlbFileAccessoryActions : AccessoryFileActionsBase
    {
        public GlbFileAccessoryActions(UniGLTF.ImporterContext context, UniGLTF.RuntimeGltfInstance instance)
        {
            _context = context;
            _instance = instance;
        }
        
        private UniGLTF.ImporterContext _context;
        private UniGLTF.RuntimeGltfInstance _instance;
        
        public override void Dispose()
        {
            _context?.Dispose();
            _context = null;

            if (_instance != null)
            {
                _instance.Dispose();
            }
            _instance = null;
        }
    }
}