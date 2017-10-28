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
        String Text { get; set; }
        View View { get; }
        void Add_TextChanged_Handler(System.ComponentModel.PropertyChangedEventHandler handler);
    }
}
