using System.Collections.Generic;
using VMagicMirror.Buddy;

namespace Baku.VMagicMirror.Buddy.Api
{
    public class GuiApi : IGui
    {
        private readonly BuddyGuiCanvas _canvas;
        private readonly List<BuddyGuiAreaInstance> _instances = new();
        public GuiApi(BuddyGuiCanvas canvas)
        {
            _canvas = canvas;
        }
        
        public IGuiArea CreateGuiArea()
        {
            var instance = _canvas.CreateGuiAreaInstance();
            _instances.Add(instance);
            return new GuiArea(instance);
        }

        internal void Dispose()
        {
            foreach (var i in _instances)
            {
                i.Dispose();
            }
            _instances.Clear();
        }
    }

    public class GuiArea : IGuiArea
    {
        private readonly BuddyGuiAreaInstance _instance;
        public GuiArea(BuddyGuiAreaInstance instance)
        {
            _instance = instance;
            _transform = new Transform2D(instance.GetTransform2DInstance());
        }

        private readonly Transform2D _transform;
        public ITransform2D Transform => _transform;
        
        public Vector2 Position
        {
            get => _instance.Position.ToApiValue();
            set => _instance.Position = value.ToEngineValue();
        }

        public Vector2 Size
        {
            get => _instance.Size.ToApiValue();
            set => _instance.Size = value.ToEngineValue();
        }

        public Vector2 Pivot
        {
            get => _instance.Pivot.ToApiValue();
            set => _instance.Pivot = value.ToEngineValue();
        }

        public void SetActive(bool active) => _instance.SetActive(active);

        public void ShowText(string content, bool immediate) => _instance.ShowText(content, immediate);
    }
}
