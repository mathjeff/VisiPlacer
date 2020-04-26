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
        // The text that this TextItem represents
        String ModelledText { get; set; }
        // The text that we put into the textbox, which may contain extra linebreaks
        String DisplayText { get; set; }
        View View { get; }
        void Add_TextChanged_Handler(System.ComponentModel.PropertyChangedEventHandler handler);
    }
}
