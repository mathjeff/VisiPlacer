using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace VisiPlacement
{
    public class SingleSelect_Choice
    {
        public SingleSelect_Choice(String choice, Color backgroundColor)
        {
            this.Content = choice;
            this.BackgroundColor = backgroundColor;
        }
        public String Content;
        public Color BackgroundColor;
    }

    public class SingleSelect : ContainerLayout
    {
        public event SingleSelect_UpdatedHandler Updated;
        public delegate void SingleSelect_UpdatedHandler(SingleSelect singleSelect);

        public SingleSelect(string label, List<String> choices)
        {
            List<SingleSelect_Choice> buttonChoices = new List<SingleSelect_Choice>();
            foreach (String content in choices)
            {
                buttonChoices.Add(new SingleSelect_Choice(content, Color.FromRgba(0, 0, 0, 0)));
            }
            this.items = buttonChoices;
            this.label = label;
            this.initialize();
        }

        public SingleSelect(string label, List<SingleSelect_Choice> choices)
        {
            this.items = choices;
            this.label = label;
            this.initialize();
        }
        private void initialize()
        {
            this.updateAppearance();
        }


        public string SelectedItem
        {
            get
            {
                return this.items[this.selectedIndex].Content;
            }
        }

        public int SelectedIndex
        {
            get
            {
                return this.selectedIndex;
            }
            set
            {
                this.selectedIndex = value;
                this.updateAppearance();
            }
        }

        public void Advance()
        {
            this.SelectIndex((this.selectedIndex + 1) % this.items.Count);
        }
        private void AdvanceButton_Clicked(object sender, EventArgs e)
        {
            this.Advance();
        }

        public void SelectIndex(int index)
        {
            this.selectedIndex = index;
            this.updateAppearance();
            if (this.Updated != null)
                this.Updated.Invoke(this);
        }

        private void updateAppearance()
        {
            ButtonLayout singleButton = this.SingleButtonLayout;

            SingleSelect_Choice choice = this.items[this.selectedIndex];
            singleButton.setText(choice.Content);
            this.singleButton.BackgroundColor = choice.BackgroundColor;

            if (this.items.Count <= 2)
            {
                // If there are only two choices, we just show one button to make it extra obvious which one is chosen
                this.SubLayout = singleButton;
            }
            else
            {
                // If there are >= 3 choices, we show either one button or all of them depending on how much space there is
                this.SubLayout = new LayoutUnion(singleButton, this.SplayedLayout);
            }
        }
        // the button we show when we're only showing one of the available choices at a time
        private ButtonLayout SingleButtonLayout
        {
            get
            {
                if (this.singleButtonLayout == null)
                {
                    this.singleButton = new Button();
                    this.singleButton.Clicked += AdvanceButton_Clicked;
                    // When there's only one button, we want it to look like a text field that the user edits, so we don't show the bevel
                    this.singleButtonLayout = ButtonLayout.WithoutBevel(this.singleButton);
                }
                return this.singleButtonLayout;
            }
        }
        // the text block we show in place of the selected item
        private TextblockLayout SelectedLabel
        {
            get
            {
                if (this.selectedLabel == null)
                {
                    this.selectedLabel = new TextblockLayout().AlignHorizontally(TextAlignment.Center).AlignVertically(TextAlignment.Center);
                }
                return this.selectedLabel;
            }
        }

        // the layout that shows all of the choices at once
        private LayoutChoice_Set SplayedLayout
        {
            get
            {
                GridLayout_Builder builder = new Horizontal_GridLayout_Builder().Uniform();
                for (int i = 0; i < this.items.Count; i++)
                {
                    if (this.selectedIndex == i)
                    {
                        // text block
                        TextblockLayout layout = this.SelectedLabel;
                        layout.setText(items[i].Content);
                        //layout.setBackgroundColor(items[i].BackgroundColor);

                        builder.AddLayout(layout);
                    }
                    else
                    {
                        // button
                        Button button = this.getChoiceButton(i);
                        // when there are multiple buttons, we do want to show bevels on the buttons to make them look more different from the selected text field
                        ButtonLayout buttonLayout = new ButtonLayout(button);
                        builder.AddLayout(buttonLayout);
                    }
                }
                LayoutChoice_Set content = builder.BuildAnyLayout();
                if (this.label != null)
                {
                    // add label above the choices
                    if (this.titledSplayedContent == null)
                    {
                        this.titledSplayedContent = GridLayout.New(new BoundProperty_List(1), new BoundProperty_List(2), LayoutScore.Zero);
                        this.titledSplayedContent.PutLayout(new TextblockLayout(this.label).AlignVertically(TextAlignment.Center), 0, 0);
                    }
                    this.titledSplayedContent.PutLayout(content, 1, 0);
                    return this.titledSplayedContent;
                }
                else
                {
                    // no label was given, so just show the choices
                    return content;
                }
            }
        }

        // returns the button for choosing the given choice index
        private Button getChoiceButton(int index)
        {
            if (this.buttonChoices == null)
                this.buttonChoices = new List<Button>();
            while (this.buttonChoices.Count <= index)
            {
                Button button = new Button();
                SingleSelect_Choice otherItem = this.items[this.buttonChoices.Count];
                button.Text = otherItem.Content;
                //button.BackgroundColor = otherItem.BackgroundColor;
                button.Clicked += JumpButton_Clicked;
                this.buttonChoices.Add(button);
            }
            return this.buttonChoices[index];
        }

        // called when the button for a specific choice is clicked
        private void JumpButton_Clicked(object sender, EventArgs e)
        {
            for (int i = 0; i < this.buttonChoices.Count; i++)
            {
                if (this.buttonChoices[i] == sender)
                {
                    this.SelectIndex(i);
                    return;
                }
            }
            throw new ArgumentException("SingleSelect did not recognize jump button " + sender);
        }

        List<SingleSelect_Choice> items;
        TextblockLayout selectedLabel;
        int selectedIndex;
        ButtonLayout singleButtonLayout;
        Button singleButton;
        List<Button> buttonChoices;
        string label;
        GridLayout titledSplayedContent;
    }
}
