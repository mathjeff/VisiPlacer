using Xamarin.Forms;

// the TitledControl adds a title above another Control
namespace VisiPlacement
{
    public class TitledControl : LayoutCache
    {
        public TitledControl()
        {
            this.Initialize();
        }
        public TitledControl(string startingTitle, double titleFontSize = -1)
        {
            this.Initialize(titleFontSize);
            this.titleLayout.setText(startingTitle);
        }
        public TitledControl(string startingTitle, LayoutChoice_Set content, double titleFontSize = -1)
            : this(startingTitle, titleFontSize)
        {
            this.SetContent(content);
        }

        private void Initialize(double titleFontSize = -1)
        {
            this.titleLayout = new TextblockLayout("", titleFontSize);
            this.titleLayout.AlignHorizontally(TextAlignment.Center);
            this.gridLayout = GridLayout.New(new BoundProperty_List(2), new BoundProperty_List(1), LayoutScore.Zero);
            this.gridLayout.AddLayout(this.titleLayout);
            base.LayoutToManage = gridLayout;
        }
        public void SetTitle(string newTitle)
        {
            this.titleLayout.setText(newTitle);
        }
        public string GetTitle()
        {
            return this.titleLayout.getText();
        }
        public void SetContent(LayoutChoice_Set layout)
        {
            this.gridLayout.PutLayout(layout, 0, 1);
        }
        public LayoutChoice_Set GetContent()
        {
            return this.gridLayout.GetLayout(0, 1);
        }
        protected TextblockLayout TitleLayout
        {
            get
            {
                return this.titleLayout;
            }
        }
        TextblockLayout titleLayout;
        GridLayout gridLayout;
    }
}
