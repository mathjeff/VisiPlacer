using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VisiPlacement;
using Xamarin.Forms;

namespace Sample
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();

            ContentView contentView = new ContentView();
            this.Content = contentView;

            LayoutChoice_Set layout = new TextMeasurement_Test_Layout();
            VisualDefaults_Builder defaultsBuilder = new VisualDefaults_Builder();
            defaultsBuilder.FontName("SatellaRegular.ttf#Satella");

            ViewManager viewManager = new ViewManager(contentView, layout, defaultsBuilder.Build());
        }
    }
}
