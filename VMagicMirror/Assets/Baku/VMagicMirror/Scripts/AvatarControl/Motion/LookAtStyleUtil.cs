namespace Baku.VMagicMirror
{
    public enum LookAtStyles
    {
        Fixed,
        MousePointer,
        MainCamera,
    }
    
    public static class LookAtStyleUtil 
    {
        private const string UseLookAtPointNone = nameof(UseLookAtPointNone);
        private const string UseLookAtPointMousePointer = nameof(UseLookAtPointMousePointer);
        private const string UseLookAtPointMainCamera = nameof(UseLookAtPointMainCamera);

        public static LookAtStyles GetLookAtStyle(string content)
        {
            return 
                (content == UseLookAtPointNone) ? LookAtStyles.Fixed :
                (content == UseLookAtPointMousePointer) ? LookAtStyles.MousePointer :
                (content == UseLookAtPointMainCamera) ? LookAtStyles.MainCamera :
                LookAtStyles.MousePointer;
        }
    }
}
