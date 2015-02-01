//#define GRIDS_IN_GRIDS

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;


namespace VisiPlacement
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        public Window1()
        {
            InitializeComponent();
            this.Loaded += new RoutedEventHandler(Window1_Loaded);
        }

        void Window1_Loaded(object sender, RoutedEventArgs e)
        {
            this.SetupView();
        }
        public void SetupView()
        {
            // setup some text blocks
            LayoutChoice_Set view1 = this.Make_MainLayout();
            rootVisual.Background = Brushes.Blue;

            rootVisual.Content = view1;
            ViewManager manager = new ViewManager(rootVisual, view1);

            
            //manager.DoLayout(view, new Size(500, 500));
        }
        private LayoutChoice_Set Make_MainLayout()
        {
            GridLayout evenLayout = GridLayout.New(new BoundProperty_List(1), BoundProperty_List.Uniform(4), LayoutScore.Zero);
#if GRIDS_IN_GRIDS
            GridLayout unevenLayout = GridLayout.New(new BoundProperty_List(1), new BoundProperty_List(4), LayoutScore.Get_UnCentered_LayoutScore(4));
            GridLayout unevenLeftLayout = GridLayout.New(new BoundProperty_List(1), new BoundProperty_List(2), LayoutScore.Zero);
            GridLayout unevenRightLayout = GridLayout.New(new BoundProperty_List(1), new BoundProperty_List(2), LayoutScore.Zero);
            unevenLayout.PutLayout(new LayoutCache(unevenLeftLayout), 0, 0);
            unevenLayout.PutLayout(new LayoutCache(unevenRightLayout), 1, 0);
#else
            GridLayout unevenLayout = GridLayout.New(new BoundProperty_List(1), new BoundProperty_List(4), LayoutScore.Get_UnCentered_LayoutScore(4));
#endif

            LayoutCache inheritanceEditingLayout = new LayoutCache(this.Make_InheritanceEditingView());
            evenLayout.PutLayout(inheritanceEditingLayout, 0, 0);
            //LayoutChoice_Set inheritanceEditingLayout = this.Make_SuggestionsLayout("Name:", "Working on the Activity Recommendation Engine", "Start Date:", "9/9/2012 4:58:13 PM", "Participation Probability:", "0.484272919402258", 6);
#if GRIDS_IN_GRIDS
            unevenLeftLayout.PutLayout(inheritanceEditingLayout, 0, 0);
#else
            unevenLayout.PutLayout(inheritanceEditingLayout, 0, 0);
#endif

            LayoutCache participationsLayout = new LayoutCache(this.Make_ParticipationEntryView());
            //LayoutChoice_Set participationsLayout = this.Make_SuggestionsLayout("Name:", "Working on the Activity Recommendation Engine", "Start Date:", "9/9/2012 4:58:13 PM", "Participation Probability:", "0.484272919402258", 6);
            evenLayout.PutLayout(participationsLayout, 1, 0);
#if GRIDS_IN_GRIDS
            unevenLeftLayout.PutLayout(participationsLayout, 1, 0);
#else
            unevenLayout.PutLayout(participationsLayout, 1, 0);
#endif

            LayoutCache suggestionsLayout = new LayoutCache(this.Make_SuggestionsLayout("Name:", "Working on the Activity Recommendation Engine", "Start Date:", "9/9/2012 4:58:13 PM", "Participation Probability:", "0.484272919402258", 6));
            evenLayout.PutLayout(suggestionsLayout, 2, 0);
#if GRIDS_IN_GRIDS
            unevenRightLayout.PutLayout(suggestionsLayout, 0, 0);
#else
            unevenLayout.PutLayout(suggestionsLayout, 2, 0);
#endif

            LayoutCache mini_visualizationMenu = new LayoutCache(this.Make__Mini_VisualizationMenu());
            //LayoutChoice_Set mini_visualizationMenu = this.Make_SuggestionsLayout("Name:", "Working on the Activity Recommendation Engine", "Start Date:", "9/9/2012 4:58:13 PM", "Participation Probability:", "0.484272919402258", 6);
            evenLayout.PutLayout(mini_visualizationMenu, 3, 0);
#if GRIDS_IN_GRIDS
            unevenRightLayout.PutLayout(mini_visualizationMenu, 1, 0);
#else
            unevenLayout.PutLayout(mini_visualizationMenu, 3, 0);
#endif

            List<LayoutChoice_Set> layoutList = new List<LayoutChoice_Set>();
            layoutList.Add(evenLayout);
            layoutList.Add(unevenLayout);
            LayoutUnion layoutSet = new LayoutUnion(layoutList);
            return new LayoutCache(layoutSet);
        }
        private LayoutChoice_Set Make_SuggestionsLayout(string label1, string value1, string label2, string value2, string label3, string value3, int numViews)
        {
            int i;
            //
            GridLayout fullLayout = GridLayout.New(new BoundProperty_List(2), new BoundProperty_List(1), LayoutScore.Zero);

            // a view containing a description and buttons
            GridLayout buttonLayout1 = GridLayout.New(new BoundProperty_List(2), new BoundProperty_List(1), LayoutScore.Zero);
            GridLayout buttonLayout2 = GridLayout.New(new BoundProperty_List(1), new BoundProperty_List(2), LayoutScore.Get_UsedSpace_LayoutScore(-1));
            TextBlock suggestionButton_textBlock = new TextBlock();
            suggestionButton_textBlock.Text = "Suggest";
            suggestionButton_textBlock.Background = Brushes.Orange;
            Button suggestionButton = new Button();
            suggestionButton.Style = new Style();

            TextBlock categoryLabel = new TextBlock();
            categoryLabel.Text = "Category:";
            categoryLabel.Background = Brushes.Orange;
            LayoutCache suggestionButton_layout = new LayoutCache(new ButtonLayout(suggestionButton, new TextblockLayout(suggestionButton_textBlock)));
            //LayoutChoice_Set suggestionButton_layout = new TextblockLayout(suggestionButton_textBlock);
            LayoutCache categoryLabel_layout = new LayoutCache(new TextblockLayout(categoryLabel));
            buttonLayout1.PutLayout(suggestionButton_layout, 0, 0);
            buttonLayout1.PutLayout(categoryLabel_layout, 0, 1);
            buttonLayout2.PutLayout(suggestionButton_layout, 0, 0);
            buttonLayout2.PutLayout(categoryLabel_layout, 1, 0);
            LayoutCache buttonLayout = new LayoutCache(new LayoutUnion(buttonLayout1, buttonLayout2));

            fullLayout.PutLayout(buttonLayout, 0, 0);



            
            // 3 by 3 with the middle column centered and all rows having equal height
            BoundProperty_List centeredWidths = new BoundProperty_List(3);
            centeredWidths.BindIndices(0, 2);
            BoundProperty_List rowHeights = BoundProperty_List.Uniform(numViews * 3);
            GridLayout centeredLayout = GridLayout.New(rowHeights, centeredWidths, LayoutScore.Zero);
            GridLayout gridLayout = GridLayout.New(rowHeights, new BoundProperty_List(2), LayoutScore.Get_UnCentered_LayoutScore(numViews));


            for (i = 0; i < numViews * 3; i += 3)
            {
                TextBlock block1 = new TextBlock();
                block1.Text = "Name:";
                block1.Background = Brushes.Blue;

                TextBlock block2 = new TextBlock();
                block2.Text = "Working on the Activity Recommendation Engine";
                block2.Background = Brushes.Blue;

                TextBlock block3 = new TextBlock();
                block3.Text = "Start Date:";
                block3.Background = Brushes.Blue;

                TextBlock block4 = new TextBlock();
                block4.Text = " 9/9/2012 4:58:13 PM";
                block4.Background = Brushes.Blue;

                TextBlock block5 = new TextBlock();
                block5.Text = "Participation Probability:";
                block5.Background = Brushes.Blue;

                TextBlock block6 = new TextBlock();
                block6.Text = "0.484272919402258";
                block6.Background = Brushes.Blue;

                centeredLayout.PutLayout(new TextblockLayout(block1), 0, i + 0);
                centeredLayout.PutLayout(new TextblockLayout(block2), 1, i + 0);
                centeredLayout.PutLayout(new TextblockLayout(block3), 0, i + 1);
                centeredLayout.PutLayout(new TextblockLayout(block4), 1, i + 1);
                centeredLayout.PutLayout(new TextblockLayout(block5), 0, i + 2);
                centeredLayout.PutLayout(new TextblockLayout(block6), 1, i + 2);

                gridLayout.PutLayout(new TextblockLayout(block1), 0, i + 0);
                gridLayout.PutLayout(new TextblockLayout(block2), 1, i + 0);
                gridLayout.PutLayout(new TextblockLayout(block3), 0, i + 1);
                gridLayout.PutLayout(new TextblockLayout(block4), 1, i + 1);
                gridLayout.PutLayout(new TextblockLayout(block5), 0, i + 2);
                gridLayout.PutLayout(new TextblockLayout(block6), 1, i + 2);



            }

            // choice of either layout
            List<LayoutChoice_Set> layoutList = new List<LayoutChoice_Set>();
            layoutList.Add(centeredLayout);
            layoutList.Add(gridLayout);
            LayoutCache layoutSet = new LayoutCache(new LayoutUnion(layoutList));

            fullLayout.PutLayout(layoutSet, 0, 1);
            return fullLayout;
        }
        private LayoutChoice_Set Make_InheritanceEditingView()
        {
            TextBlock titleBlock = new TextBlock();
            titleBlock.Text = "Enter Activities to Choose From";
            titleBlock.Background = Brushes.Gray;

            TextBlock activityName_block = new TextBlock();
            activityName_block.Text = "Activity Name";
            activityName_block.Background = Brushes.WhiteSmoke;

            TextBox activityName_box = new TextBox();
            activityName_box.Text = "Ice Skating";
            activityName_box.Background = Brushes.White;

            TextBlock parentActivity_block = new TextBlock();
            parentActivity_block.Text = "Parent Name";
            parentActivity_block.Background = Brushes.WhiteSmoke;

            TextBox parentActivity_box = new TextBox();
            parentActivity_box.Text = "Exercise";
            parentActivity_box.Background = Brushes.White;

            Button button = new Button();
            TextBlock buttonContents = new TextBlock();
            buttonContents.Text = "Ok";
            ButtonLayout buttonLayout = new ButtonLayout(button, new TextblockLayout(buttonContents));

            GridLayout textGrid = GridLayout.New(new BoundProperty_List(2), BoundProperty_List.Uniform(2), LayoutScore.Zero);
            textGrid.PutLayout(new TextblockLayout(activityName_block), 0, 0);
            textGrid.PutLayout(new TextblockLayout(parentActivity_block), 1, 0);
            textGrid.PutLayout(new TextboxLayout(activityName_box), 0, 1);
            textGrid.PutLayout(new TextboxLayout(parentActivity_box), 1, 1);
            GridLayout mainGrid = GridLayout.New(new BoundProperty_List(3), BoundProperty_List.Uniform(1), LayoutScore.Zero);
            mainGrid.PutLayout(new TextblockLayout(titleBlock), 0, 0);
            mainGrid.PutLayout(new LayoutCache(textGrid), 0, 1);
            mainGrid.PutLayout(new LayoutCache(buttonLayout), 0, 2);

            return mainGrid;
        }
        private LayoutChoice_Set Make_ParticipationEntryView()
        {
            TextBlock textBlock = new TextBlock();
            textBlock.Text = "1234567890";
            textBlock.Background = Brushes.Green;

            GridLayout gridLayout = GridLayout.New(BoundProperty_List.Uniform(1), BoundProperty_List.Uniform(1), LayoutScore.Zero);
            gridLayout.PutLayout(new TextblockLayout(textBlock), 0, 0);
            return gridLayout;
        }
        private LayoutChoice_Set Make__Mini_VisualizationMenu()
        {
            TextBlock titleBlock = new TextBlock();
            titleBlock.Text = "View Statistics";
            titleBlock.Background = Brushes.Red;

            TextBox activityName_box = new TextBox();
            activityName_box.Text = "Useful";
            activityName_box.Background = Brushes.White;

            GridLayout gridLayout = GridLayout.New(new BoundProperty_List(2), BoundProperty_List.Uniform(1), LayoutScore.Zero);
            gridLayout.PutLayout(new TextblockLayout(titleBlock), 0, 0);
            gridLayout.PutLayout(new TextboxLayout(activityName_box), 0, 1);
            return gridLayout;
        }
    }
}
