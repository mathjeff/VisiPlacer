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
        public ScaledFont Font;
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
        // color of text in the button
        public Color TextColor;

        // at the edges of the button there is a bevel. This is the color of the outer bevel
        public Color OuterBevelColor;
        // color of the inner bevel
        public Color InnerBevelColor;

        // primary color of background gradient
        public Color BackgroundColorPrimary;
        // secondary color of background gradient, if set
        public Color BackgroundColorSecondary;

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
        public VisualDefaults_Builder ApplicationBackground(Color color)
        {
            this.applicationBackground = color;
            return this;
        }
        public VisualDefaults_Builder ButtonInnerBevelColor(Color color)
        {
            this.buttonInnerBevelColor = color;
            return this;
        }
        public VisualDefaults_Builder ButtonOuterBevelColor(Color color)
        {
            this.buttonOuterBevelColor = color;
            return this;
        }
        public VisualDefaults_Builder ButtonBackgroundSecondaryColor(Color color)
        {
            this.buttonBackgroundSecondaryColor = color;
            return this;
        }
        public VisualDefaults_Builder FontName(string name)
        {
            if (Device.RuntimePlatform == Device.Android)
            {
                // on Android, we have to specify the filename and the font name, something like myfile.ttf#myfontname
                this.fontName = name;
            }
            else
            {
                if (Device.RuntimePlatform == Device.UWP)
                {
                    // On Windows, we have to give the full filepath
                    this.fontName = "/Assets/Fonts/" + name;
                }
                else
                {
                    // On other operating systems, we just use the name of the font
                    int poundIndex = name.IndexOf('#');
                    if (poundIndex >= 0)
                        name = name.Substring(poundIndex + 1);
                    this.fontName = name;
                }
            }
            return this;
        }

        public VisualDefaults_Builder FontSizeMultiplier(double multiplier)
        {
            this.fontSizeMultiplier = multiplier;
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
            viewDefaults.ApplicationBackground = this.applicationBackground;

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
            buttonDefaults.BackgroundColorPrimary = this.uneditableTextBackgroundColor;
            if (this.buttonBackgroundSecondaryColor != null)
                buttonDefaults.BackgroundColorSecondary = this.buttonBackgroundSecondaryColor.Value;
            if (this.buttonInnerBevelColor != null)
                buttonDefaults.InnerBevelColor = this.buttonInnerBevelColor.Value;
            else
                buttonDefaults.InnerBevelColor = Color.DarkGray;
            if (this.buttonOuterBevelColor != null)
                buttonDefaults.OuterBevelColor = this.buttonOuterBevelColor.Value;
            else
                buttonDefaults.OuterBevelColor = Color.LightGray;
            viewDefaults.ButtonWithBevel_Defaults = buttonDefaults;

            ButtonViewDefaults buttonWithoutBevelDefaults = new ButtonViewDefaults();
            buttonWithoutBevelDefaults.TextColor = this.uneditableTextBackgroundColor;
            buttonWithoutBevelDefaults.BackgroundColorPrimary = this.uneditableTextColor;
            viewDefaults.ButtonWithoutBevel_Defaults = buttonWithoutBevelDefaults;

            LayoutDefaults layoutDefaults = new LayoutDefaults();

            TextBox_LayoutDefaults textbox_layoutDefaults = new TextBox_LayoutDefaults();
            textbox_layoutDefaults.Font = new ScaledFont();
            textbox_layoutDefaults.Font.Name = this.fontName;
            textbox_layoutDefaults.Font.SizeMultiplier = this.fontSizeMultiplier;
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
        private Color applicationBackground;
        private Color? buttonInnerBevelColor;
        private Color? buttonOuterBevelColor;
        private Color? buttonBackgroundSecondaryColor;
        private string fontName;
        private double fontSizeMultiplier = 1;
    }
}
