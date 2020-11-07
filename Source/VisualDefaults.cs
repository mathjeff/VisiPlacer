using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

// A LayoutDefaults contains settings that are likely to be common to many layouts but different across applications or users and are too verbose for us to
// want to specify them explicitly on every layout
// In practice, a LayoutDefaults is likely to specify which colors to use
namespace VisiPlacement
{
    // a LayoutDefaults provides default values for various types of layouts
    public class VisualDefaults
    {
        public LayoutDefaults LayoutDefaults;
        public ViewDefaults ViewDefaults;

        public string DisplayName;
        public string PersistedName;
    }
    // a LayoutDefaults stores default properties that are specifically needed when computing layout dimensions
    public class LayoutDefaults
    {
        public TextBox_LayoutDefaults TextBox_Defaults;
    }
    public class TextBox_LayoutDefaults
    {
        public string FontName;
    }
    // a ViewDefaults stores default properties that affect the display but that don't affect the layout dimensions
    public class ViewDefaults
    {
        public TextBlock_ViewDefaults TextBlock_Defaults;
        public TextBox_ViewDefaults TextBox_Defaults;
        public ButtonViewDefaults ButtonWithBevel_Defaults;
        public ButtonViewDefaults ButtonWithoutBevel_Defaults;

        public Color ApplicationBackground;
    }

    public class TextBlock_ViewDefaults
    {
        public Color TextColor;
        public Color BackgroundColor;
    }

    public class TextBox_ViewDefaults
    {
        public Color TextColor;
        public Color BackgroundColor;
    }

    public class ButtonViewDefaults
    {
        public Color TextColor;
        public Color OuterBevelColor;
        public Color InnerBevelColor;
        public Color BackgroundColor;
    }

    // A LayoutDefaults_Builder converts from properties that a user thinks about (normal text foreground color, normal text background color, etc)
    // to objects that a developer thinks about (text block text and background colors, text box text and background colors, etc)

    public class VisualDefaults_Builder
    {

        public VisualDefaults_Builder UneditableText_Color(Color color)
        {
            this.uneditableTextColor = color;
            return this;
        }

        public VisualDefaults_Builder UneditableText_Background(Color color)
        {
            this.uneditableTextBackgroundColor = color;
            return this;
        }
        public VisualDefaults_Builder FontName(string name)
        {
            if (Device.RuntimePlatform == Device.Android)
            {
                // on Android, we use a long path for specifying the font name, something like myfile.ttf#myfontname
                this.fontName = name;
            }
            else
            {
                // On other operating systems, we just use the name of the font
                int poundIndex = name.IndexOf("#");
                if (poundIndex >= 0)
                    name = name.Substring(poundIndex);
                this.fontName = name;
            }
            return this;
        }

        public VisualDefaults_Builder DisplayName(string name)
        {
            this.displayName = name;
            return this;
        }
        public VisualDefaults Build()
        {
            ViewDefaults viewDefaults = new ViewDefaults();
            viewDefaults.ApplicationBackground = this.uneditableTextBackgroundColor;

            TextBlock_ViewDefaults textblockDefaults = new TextBlock_ViewDefaults();
            textblockDefaults.TextColor = this.uneditableTextColor;
            textblockDefaults.BackgroundColor = this.uneditableTextBackgroundColor;
            viewDefaults.TextBlock_Defaults = textblockDefaults;

            TextBox_ViewDefaults textboxDefaults = new TextBox_ViewDefaults();
            textboxDefaults.TextColor = this.uneditableTextBackgroundColor;
            textboxDefaults.BackgroundColor = this.uneditableTextColor;
            viewDefaults.TextBox_Defaults = textboxDefaults;

            ButtonViewDefaults buttonDefaults = new ButtonViewDefaults();
            buttonDefaults.TextColor = this.uneditableTextColor;
            buttonDefaults.BackgroundColor = this.uneditableTextBackgroundColor;
            viewDefaults.ButtonWithBevel_Defaults = buttonDefaults;

            ButtonViewDefaults buttonWithoutBevelDefaults = new ButtonViewDefaults();
            buttonWithoutBevelDefaults.TextColor = this.uneditableTextBackgroundColor;
            buttonWithoutBevelDefaults.BackgroundColor = this.uneditableTextColor;
            viewDefaults.ButtonWithoutBevel_Defaults = buttonWithoutBevelDefaults;

            LayoutDefaults layoutDefaults = new LayoutDefaults();

            TextBox_LayoutDefaults textbox_layoutDefaults = new TextBox_LayoutDefaults();
            textbox_layoutDefaults.FontName = this.fontName;
            layoutDefaults.TextBox_Defaults = textbox_layoutDefaults;

            VisualDefaults visualDefaults = new VisualDefaults();
            visualDefaults.LayoutDefaults = layoutDefaults;
            visualDefaults.ViewDefaults = viewDefaults;

            visualDefaults.PersistedName = this.displayName;
            visualDefaults.DisplayName = this.displayName;

            return visualDefaults;
        }

        private string displayName;

        private Color uneditableTextColor;
        private Color uneditableTextBackgroundColor;
        private string fontName;
    }
}
