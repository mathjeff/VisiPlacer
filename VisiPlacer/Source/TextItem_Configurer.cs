using System;
using Xamarin.Forms;

// for getting/setting properties of a TextBox or Label
namespace VisiPlacement
{
    public interface TextItem_Configurer
    {
        double Width { get; set; }
        double Height { get; set; }
        double FontSize { get; set; }
        // The text that this text box appears to contain, from the perspective of a user.
        // If a placeholder is present, this may be the placeholder
        // This also doesn't necessarily contain extra newlines when the line wraps
        string ModelledText { get; }
        // The text that we put into the TextBox.Text property, which may contain extra linebreaks
        string DisplayText { get; set; }
        View View { get; }
        string FontName { get; set; }
        void Add_TextChanged_Handler(System.ComponentModel.PropertyChangedEventHandler handler);
        void ApplyDefaults(ViewDefaults viewDefaults);

    }
}
