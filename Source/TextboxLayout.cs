﻿using System.Collections.Generic;
using System.ComponentModel;
using Xamarin.Forms;

// A TextboxLayout is what callers should make if they want to display editable text
namespace VisiPlacement
{
    public class TextboxLayout : LayoutCache
    {
        public TextboxLayout()
            :this(new Editor())
        {
        }
        public TextboxLayout(Editor textBox)
        {
            Effect effect = Effect.Resolve("VisiPlacement.TextItemEffect");
            textBox.Effects.Add(effect);

            textBox.Margin = new Thickness();

            this.TextBox = textBox;
            textBox.BackgroundColor = Color.LightGray;
            this.TextBox.Margin = new Thickness();

            this.layouts.Add(new TextLayout(new TextBox_Configurer(textBox), 30));
            this.layouts.Add(new TextLayout(new TextBox_Configurer(textBox), 16));

            this.LayoutToManage = new LayoutUnion(layouts);

        }

        public bool ScoreIfEmpty
        {
            set
            {
                foreach (TextLayout layout in this.layouts)
                {
                    layout.ScoreIfEmpty = value;
                }
            }
        }

        public bool ScoreIfCropped
        {
            set
            {
                foreach (TextLayout layout in this.layouts)
                {
                    layout.ScoreIfCropped = value;
                }
            }
        }

        public bool LoggingEnabled
        {
            set
            {
                foreach (TextLayout layout in this.layouts)
                {
                    layout.LoggingEnabled = value;
                }
            }
        }

        private void Setup_PropertyChange_Listener(string propertyName, View element, PropertyChangedEventHandler callback)
        {
            this.TextBox.PropertyChanged += callback;
        }

        private List<TextLayout> layouts = new List<TextLayout>();
        private Editor TextBox;
    }

    // The TextBox_Configurer is an implementation detail that facilitates sharing code between TextblockLayout and TextboxLayout
    // The TextBox_Configurer probably isn't interesting to external callers
    public class TextBox_Configurer : TextItem_Configurer
    {
        public TextBox_Configurer(Editor TextBox)
        {
            this.TextBox = TextBox;
        }
        public double Width
        {
            get { return this.TextBox.WidthRequest; }
            set
            {
                this.TextBox.WidthRequest = value;
            }
        }
        public double Height
        {
            get { return this.TextBox.HeightRequest; }
            set
            {
                this.TextBox.HeightRequest = value;
            }
        }
        public double FontSize
        {
            get { return this.TextBox.FontSize; }
            set { this.TextBox.FontSize = value; }
        }
        public string Text
        {
            get { return this.TextBox.Text; }
            set { this.TextBox.Text = value; }
        }
        public View View
        {
            get
            {
                return this.TextBox;
            }
        }
        public void Add_TextChanged_Handler(PropertyChangedEventHandler handler)
        {
            this.TextBox.PropertyChanged += handler;
        }

        private Editor TextBox;
    }
}
