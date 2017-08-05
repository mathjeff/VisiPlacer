using System;
using Windows.UI.Xaml;
using Windows.UI.Text;
using Windows.UI.Xaml.Media;


// for getting/setting properties of a TextBox or TextBlock
namespace VisiPlacement
{
    public interface TextItem_Configurer
    {
        double Width { get; set; }
        double Height { get; set; }
        double FontSize { get; set; }
        FontFamily FontFamily { get; set; }
        FontStyle FontStyle { get; set; }
        FontWeight FontWeight { get; set; }
        FontStretch FontStretch { get; set; }
        FrameworkElement View { get; }
        String Text { get; set; }
        void Add_TextChanged_Handler(PropertyChangedCallback handler);
        //void Add_LostFocus_Handler(RoutedEventHandler handler);
        //TextItem_Configurer Clone();
    }
}
