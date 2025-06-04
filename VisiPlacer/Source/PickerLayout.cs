using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace VisiPlacement
{

    public class PickerLayout : TitledControl
    {
        public PickerLayout(Picker picker)
        {
            this.picker = picker;
            this.SetTitle(picker.Title);

            picker.BackgroundColor = Colors.White;
            picker.TextColor = Colors.Black;

            this.SetContent(new TextLayout(new PickerConfigurer(picker), 16));
        }
        public Picker Picker
        {
            get
            {
                return this.Picker;
            }
        }
        private Picker picker;
    }

    class PickerConfigurer : TextItem_Configurer
    {
        public PickerConfigurer(Picker picker)
        {
            this.picker = picker;
            this.picker.SelectedIndexChanged += TextBox_SelectedIndexChanged;
            this.ModelledText = this.DisplayText;
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
        public string ModelledText { get; set; }

        public string DisplayText
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
        public string FontName
        {
            get { return this.picker.FontFamily; }
            set { this.picker.FontFamily = value; }
        }
        public View View
        {
            get
            {
                return this.picker;
            }
        }
        public void ApplyDefaults(ViewDefaults layoutDefaults)
        {
            // TODO: should a Picker change colors based on LayoutDefaults?
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
