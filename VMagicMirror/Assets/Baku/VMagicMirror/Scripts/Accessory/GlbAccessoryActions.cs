namespace Baku.VMagicMirror
{
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