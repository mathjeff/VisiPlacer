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
    public class LayoutDefaults
    {
        public string DisplayName;
        public string PersistedName;

        public TextBlock_Defaults TextBlock_Defaults;
        public TextBox_Defaults TextBox_Defaults;
        public ButtonDefaults ButtonWithBevel_Defaults;
        public ButtonDefaults ButtonWithoutBevel_Defaults;

        public Color ApplicationBackground;
    }

    public class TextBlock_Defaults
    {
        public Color TextColor;
        public Color BackgroundColor;
    }

    public class TextBox_Defaults
    {
        public Color TextColor;
        public Color BackgroundColor;
    }

    public class ButtonDefaults
    {
        public Color TextColor;
        public Color OuterBevelColor;
        public Color InnerBevelColor;
        public Color BackgroundColor;
    }

    // A LayoutDefaults_Builder converts from properties that a user thinks about (normal text foreground color, normal text background color, etc)
    // to objects that a developer thinks about (text block text and background colors, text box text and background colors, etc)

    public class LayoutDefaults_Builder
    {

        public LayoutDefaults_Builder UneditableText_Color(Color color)
        {
            this.uneditableTextColor = color;
            return this;
        }

        public LayoutDefaults_Builder UneditableText_Background(Color color)
        {
            this.uneditableTextBackgroundColor = color;
            return this;
        }

        public LayoutDefaults_Builder DisplayName(string name)
        {
            this.displayName = name;
            return this;
        }
        public LayoutDefaults Build()
        {
            LayoutDefaults defaults = new LayoutDefaults();

            defaults.PersistedName = this.displayName;
            defaults.DisplayName = this.displayName;
            defaults.ApplicationBackground = this.uneditableTextBackgroundColor;

            TextBlock_Defaults textblockDefaults = new TextBlock_Defaults();
            textblockDefaults.TextColor = this.uneditableTextColor;
            textblockDefaults.BackgroundColor = this.uneditableTextBackgroundColor;
            defaults.TextBlock_Defaults = textblockDefaults;

            TextBox_Defaults textboxDefaults = new TextBox_Defaults();
            textboxDefaults.TextColor = this.uneditableTextBackgroundColor;
            textboxDefaults.BackgroundColor = this.uneditableTextColor;
            defaults.TextBox_Defaults = textboxDefaults;

            ButtonDefaults buttonDefaults = new ButtonDefaults();
            buttonDefaults.TextColor = this.uneditableTextColor;
            buttonDefaults.BackgroundColor = this.uneditableTextBackgroundColor;
            defaults.ButtonWithBevel_Defaults = buttonDefaults;

            ButtonDefaults buttonWithoutBevelDefaults = new ButtonDefaults();
            buttonWithoutBevelDefaults.TextColor = this.uneditableTextBackgroundColor;
            buttonWithoutBevelDefaults.BackgroundColor = this.uneditableTextColor;
            defaults.ButtonWithoutBevel_Defaults = buttonWithoutBevelDefaults;

            return defaults;
        }

        private Color uneditableTextColor;
        private Color uneditableTextBackgroundColor;
        private string displayName;
    }
}
