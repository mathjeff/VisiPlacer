using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace VisiPlacement
{

    public class PickerLayout : TitledControl
    {
        public PickerLayout(Picker picker)
        {
            this.SetTitle(picker.Title);

            picker.BackgroundColor = Color.White;
            picker.TextColor = Color.Black;

            this.SetContent(new TextLayout(new PickerConfigurer(picker), 16));
        }
    }

    class PickerConfigurer : TextItem_Configurer
    {
        public PickerConfigurer(Picker picker)
        {
            this.picker = picker;
            this.picker.SelectedIndexChanged += TextBox_SelectedIndexChanged;
        }

        public double Width
        {
            get { return this.picker.WidthRequest; }
            set
            {
                this.picker.WidthRequest = value;
            }
        }
        public double Height
        {
            get { return this.picker.HeightRequest; }
            set
            {
                this.picker.HeightRequest = value;
            }
        }
        public double FontSize
        {
            get { return 16; }
            set { /* do nothing */ }
        }
        public string Text
        {
            get
            {
                object item = this.picker.SelectedItem;
                if (item == null)
                    return "";
                return item.ToString();
            }
            set
            {
                this.picker.SelectedItem = value;
            }
        }
        public View View
        {
            get
            {
                return this.picker;
            }
        }
        public void Add_TextChanged_Handler(PropertyChangedEventHandler handler)
        {
            this.textChanged_handlers.Add(handler);
        }

        private void TextBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            foreach (PropertyChangedEventHandler handler in this.textChanged_handlers)
            {
                handler.Invoke(sender, new PropertyChangedEventArgs("SelectedItem"));
            }
        }

        private Picker picker;
        List<PropertyChangedEventHandler> textChanged_handlers = new List<PropertyChangedEventHandler>();
    }
}
