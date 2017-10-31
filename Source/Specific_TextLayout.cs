using System;
using System.Collections.Generic;
using Xamarin.Forms;

// A Specific_TextLayout tells parameters (width, height, fontsize) of a piece of text (Label or TextBox)
namespace VisiPlacement
{
    public class Specific_TextLayout : SpecificLayout
    {
        public Specific_TextLayout(TextItem_Configurer textItem, double width, double height, double fontSize, LayoutScore score)
        {
            this.textItem = textItem;
            this.width = width;
            this.height = height;
            this.fontSize = fontSize;
            this.score = score;
            this.TextForDebugging = textItem.Text;
        }
        // sets the properties on the textblock as required by this layout
        public void PrepareTextview()
        {
            this.textItem.Width = this.Width;
            this.textItem.Height = this.Height;
            this.textItem.FontSize = this.fontSize;
        }

        public override double Width
        {
            get
            {
                return this.width;
            }
        }
        public override double Height
        {
            get
            {
                return this.height;
            }
        }
        public override LayoutScore Score
        {
            get
            {
                return this.score;
            }
        }
        public override View View
        {
            get { return this.textItem.View; }
        }
        public bool Cropped { get; set; }
        public String TextForDebugging { get; set; }

        public override View DoLayout(Size displaySize)
        {
            if (displaySize.Width != this.textItem.Width)
                this.textItem.Width = displaySize.Width;
            if (displaySize.Height != this.textItem.Height)
                this.textItem.Height = displaySize.Height;
            if (this.fontSize != this.textItem.FontSize)
                this.textItem.FontSize = this.fontSize;
            return this.textItem.View;
        }

        public override void Remove_VisualDescendents()
        {
            // can't put any views inside of a text block so there's nothing to remove
        }

        public LayoutScore BonusScore { get; set; }
        public bool LoggingEnabled { get; set; }
        public void CopyFrom(Specific_TextLayout original)
        {
            this.width = original.width;
            this.height = original.height;
            this.fontSize = original.fontSize;
            this.textItem = original.textItem;
        }

        public override SpecificLayout Clone()
        {
            Specific_TextLayout clone = new Specific_TextLayout(this.textItem, this.width, this.height, this.fontSize, this.score);
            clone.TextForDebugging = this.TextForDebugging;
            return clone;
        }

        public override IEnumerable<SpecificLayout> GetChildren()
        {
            return new LinkedList<SpecificLayout>();
        }

        private TextItem_Configurer textItem;
        private double fontSize;
        private double width;
        private double height;
        private LayoutScore score;
    }

}
