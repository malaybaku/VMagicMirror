using Baku.VMagicMirrorConfig.ViewModel;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Baku.VMagicMirrorConfig.View
{
    public class BuddyPropertyDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? BoolTemplate { get; set; }
        public DataTemplate? IntTemplate { get; set; }
        public DataTemplate? RangeIntTemplate { get; set; }
        public DataTemplate? FloatTemplate { get; set; }
        public DataTemplate? RangeFloatTemplate { get; set; }
        public DataTemplate? StringTemplate { get; set; }
        public DataTemplate? EnumTemplate { get; set; }
        public DataTemplate? Vector2Template { get; set; }
        public DataTemplate? Vector3Template { get; set; }
        public DataTemplate? QuaternionTemplate { get; set; }
        public DataTemplate? Transform2DTemplate { get; set; }
        public DataTemplate? Transform3DTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is not BuddyPropertyViewModel buddyProperty)
            {
                return base.SelectTemplate(item, container);
            }

            var result = buddyProperty.VisualType switch
            {
                BuddyPropertyType.Bool => BoolTemplate,
                BuddyPropertyType.Int => IntTemplate,
                BuddyPropertyType.RangeInt => RangeIntTemplate,
                BuddyPropertyType.Float => FloatTemplate,
                BuddyPropertyType.RangeFloat => RangeFloatTemplate,
                BuddyPropertyType.String => StringTemplate,
                BuddyPropertyType.Enum => EnumTemplate,
                BuddyPropertyType.Vector2 => Vector2Template,
                BuddyPropertyType.Vector3 => Vector3Template,
                BuddyPropertyType.Quaternion => QuaternionTemplate,
                BuddyPropertyType.Transform2D => Transform2DTemplate,
                BuddyPropertyType.Transform3D => Transform3DTemplate,
                _ => null,
            };

            return result ?? throw new NotSupportedException();
        }
    }
}
