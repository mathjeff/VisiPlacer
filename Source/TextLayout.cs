using SkiaSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Xamarin.Forms;

namespace VisiPlacement
{
    public class TextLayout : LayoutChoice_Set
    {
        public TextLayout(TextItem_Configurer textItem, double fontSize)
        {
            this.TextItem_Configurer = textItem;
            this.FontSize = fontSize;
            this.ScoreIfEmpty = true;
            this.previousText = this.TextItem_Configurer.Text;
            textItem.Add_TextChanged_Handler(new PropertyChangedEventHandler(this.On_TextChanged));

        }
        protected TextItem_Configurer TextItem_Configurer { get; set; }
        public double FontSize { get; set; }
        public bool ScoreIfEmpty { get; set; }

        public String Text
        {
            get
            {
                return this.TextItem_Configurer.Text;
            }
        }
        private String TextToFit
        {
            get
            {
                String result = this.Text;
                if ((result == "" || result == null) && this.ScoreIfEmpty)
                    result = "A";
                return result;
            }
        }
        private TextFormatter MakeTextFormatter()
        {
            TextFormatter formatter = TextFormatter.Default;
            formatter.FontSize = this.FontSize;
            return formatter;
        }

        public static TimeSpan TextTime = new TimeSpan();
        public static int NumMeasures = 0;
        public override SpecificLayout GetBestLayout(LayoutQuery query)
        {
            // don't bother doing the layout if the required score is too high
            if (query.MinScore.CompareTo(this.BestPossibleScore) > 0)
                return null;

            TextLayout.NumMeasures++;
            if (TextLayout.NumMeasures % 80 == 0)
            {
                System.Diagnostics.Debug.WriteLine("num text measurements = " + TextLayout.NumMeasures);
            }
            DateTime startTime = DateTime.Now;
            this.previousText = this.TextItem_Configurer.Text;
            //ErrorReporter.ReportParadox("avg num computations per query = " + (double)numComputations / (double)numQueries);
            numQueries++;



            SpecificLayout result;
            if (query.MinimizesWidth())
                result = this.Get_MinWidth_Layout(query);
            else
            {
                if (query.MinimizesHeight())
                    result = this.Get_MinHeight_Layout(query);
                else
                    result = this.Get_MaxScoring_Layout(query);
            }
            DateTime endTime = DateTime.Now;
            TextLayout.TextTime += endTime.Subtract(startTime);
            return result;
        }


        // computes the size of the highest-scoring layout satisfying the given criteria
        private SpecificLayout Get_MaxScoring_Layout(LayoutQuery query)
        {
            if (query.MaxWidth < 0 || query.MaxHeight < 0)
                return null;
            Specific_TextLayout specificLayout = this.ComputeDimensions(new Size(query.MaxWidth, query.MaxHeight));
            if (query.Accepts(specificLayout))
                return this.prepareLayoutForQuery(specificLayout, query);
            return null;
        }
        // computes the size of the layout with smallest height satisfying the given criteria
        private SpecificLayout Get_MinHeight_Layout(LayoutQuery query)
        {
            if (query.MaxWidth < 0 || query.MaxHeight < 0)
                return null;
            // first check whether this query will accept a cropped layout
            Specific_TextLayout specificLayout = this.ComputeDimensions(new Size(0, 0));
            if (query.Accepts(specificLayout))
                return this.prepareLayoutForQuery(specificLayout, query);
            specificLayout = this.ComputeDimensions(new Size(query.MaxWidth, query.MaxHeight));
            if (query.Accepts(specificLayout))
                return this.prepareLayoutForQuery(specificLayout.GetBestLayout(query), query);
            return null;
        }
        // computes the size of the layout with smallest width satisfying the given criteria
        private SpecificLayout Get_MinWidth_Layout(LayoutQuery query)
        {
            if (query.MaxWidth < 0 || query.MaxHeight < 0)
                return null;
            // first check whether this query will accept a cropped layout
            Specific_TextLayout specificLayout = this.ComputeDimensions(new Size(0, 0));
            if (query.Accepts(specificLayout))
                return this.prepareLayoutForQuery(specificLayout, query);
            // not satisfied with cropping so we need to try harder to do a nice-looking layout
            Specific_TextLayout nonCropping_layout = this.Get_NonCropping_MinWidthLayout(query);
            if (nonCropping_layout != null)
                return this.prepareLayoutForQuery(nonCropping_layout, query);
            return null;
        }

        // computes the layout dimensions of the layout of minimum width such that there is no cropping
        private Specific_TextLayout Get_NonCropping_MinWidthLayout(LayoutQuery query)
        {
            // if the width is really small, the text block doesn't realize that it doesn't need any space
            double maxWidth = query.MaxWidth;

            Size bestAllowedSize = new Size(double.PositiveInfinity, 0);
            double pixelSize = 1;
            double maxRejectedWidth = 0;
            while (maxRejectedWidth < bestAllowedSize.Width - pixelSize / 2)
            {
                // given the current width, compute the required height
                Specific_TextLayout newDimensions = this.ComputeDimensions(new Size(maxWidth, double.PositiveInfinity));
                if (newDimensions.Height <= query.MaxHeight && !newDimensions.Cropped)
                {
                    // this layout fits in the required dimensions
                    if (newDimensions.Width < bestAllowedSize.Width && newDimensions.Width <= query.MaxWidth)
                    {
                        // we even have improved on the best layout found so far
                        bestAllowedSize = new Size(newDimensions.Width, newDimensions.Height);
                        maxWidth = newDimensions.Width;
                    }
                    else
                    {
                        // we've found a layout having sufficiently small width and height, but it isn't any better than what we'd previously found
                        // So, we're not making progress with this process and should quit
                        break;
                    }
                }
                else
                {
                    // this layout does not fit in the required dimensions
                    if (maxWidth > maxRejectedWidth)
                        maxRejectedWidth = maxWidth;
                    if (maxWidth <= 0)
                        break;
                    // if the first layout we found was too tall, then there must be some cropping
                    if (double.IsPositiveInfinity(bestAllowedSize.Width))
                        return null;
                }
                // calculate a new size
                double desiredArea = newDimensions.Height * maxWidth;
                maxWidth = desiredArea / query.MaxHeight;
                if (maxWidth < maxRejectedWidth + pixelSize / 2)
                    maxWidth = maxRejectedWidth + pixelSize / 2;
                if (maxWidth > bestAllowedSize.Width - pixelSize / 2)
                    maxWidth = bestAllowedSize.Width - pixelSize / 2;
            }
            if (bestAllowedSize.Width > query.MaxWidth)
                return null;
            Specific_TextLayout dimensions = this.ComputeDimensions(bestAllowedSize);
            return dimensions;
        }

        // compute the best dimensions fitting within the given size
        private Specific_TextLayout ComputeDimensions(Size availableSize)
        {
            return this.ComputeDimensions(availableSize, this.TextToFit);
        }
        private Specific_TextLayout ComputeDimensions(Size availableSize, string text)
        {
            numComputations++;
            //ErrorReporter.ReportParadox("Text items: computations per query: " + AverageNumComputationsPerQuery);

            TextFormatter textFormatter = this.MakeTextFormatter();
            Size desiredSize = textFormatter.FormatText(text, availableSize.Width);
            bool cropped = false;
            double width, height;
            // assign the width, height, and score
            if (desiredSize.Width <= availableSize.Width && desiredSize.Height <= availableSize.Height)
            {
                // no cropping is necessary
                cropped = false;
                width = desiredSize.Width;
                height = desiredSize.Height;
            }
            else
            {
                // cropping
                cropped = true;
                width = height = 0;
            }
            Specific_TextLayout specificLayout = new Specific_TextLayout(this.TextItem_Configurer, width, height, this.FontSize, this.ComputeScore(cropped, text));
            specificLayout.Cropped = cropped;

            // diagnostics
            if (this.LoggingEnabled)
                ErrorReporter.ReportParadox("measuring: maxWidth = " + availableSize.Width.ToString() + " maxHeight = " + availableSize.Height.ToString());
            if (this.LoggingEnabled)
                ErrorReporter.ReportParadox("measure: desired width = " + specificLayout.Width.ToString() + " desired height = " + specificLayout.Height.ToString());

            return specificLayout;
        }
        private LayoutScore ComputeScore(bool dimensionsAreCropped, string text)
        {
            if (text == "" && !this.ScoreIfEmpty)
                return LayoutScore.Zero;
            LayoutScore score = new LayoutScore(this.BonusScore);
            if (dimensionsAreCropped)
                return LayoutScore.Get_CutOff_LayoutScore(1);
            return score;
        }


        public TextFormatter TextFormatter
        {
            get
            {
                this.textFormatter = this.MakeTextFormatter();
                return this.textFormatter;
            }
        }
        public LayoutScore BestPossibleScore
        {
            get
            {
                return this.BonusScore;
            }
        }
        public LayoutScore BonusScore 
        {
            get
            {
                if (this.bonusScore == null)
                    this.bonusScore = LayoutScore.Get_UsedSpace_LayoutScore(this.FontSize);
                return this.bonusScore;
            }
            set
            {
                this.bonusScore = value;
            }
        }
        public void On_TextChanged(object sender, PropertyChangedEventArgs e)
        {
            this.On_TextChanged();
        }
        public void On_TextChanged()
        {
            // when we set the size of the text item, it generates a change event that we don't want
            // So, here we make sure that it actually changed
            if (this.Text != this.previousText)
            {
                if (this.Get_ChangedSinceLastRender())
                    return; // nothing new to report
                bool mustRedraw = false;
                if (!this.ScoreIfEmpty && (this.Text == "" || this.previousText == ""))
                {
                    mustRedraw = true;
                }
                else
                {
                    Specific_TextLayout layoutForNewText = this.ComputeDimensions(new Size(this.TextItem_Configurer.View.Width, this.TextItem_Configurer.View.Height), this.Text);
                    if (!layoutForNewText.Cropped)
                    {
                        // The new text fits in the existing box, so this box doesn't need any more space
                        // It is possible that the text has shrunken and another box now can use extra space, but the user probably doesn't care about that right now
                        mustRedraw = false;
                    }
                    else
                    {
                        View view = this.TextItem_Configurer.View;
                        Size currentSize = new Size(view.Width, view.Height);
                        Specific_TextLayout layoutForCurrentText = this.ComputeDimensions(currentSize, this.previousText);
                        if (!layoutForCurrentText.Cropped)
                        {
                            // If we leave the render size the same then the text suddenly gets cropped, so we ask the layout engine for more space
                            mustRedraw = true;
                        }
                        else
                        {
                            // If the text layout was cropped before and after then we might need to ask the engine for more space,
                            // but it would annoy the user to keep redoing the layout and probably see no change
                            // So, we don't bother asking for more space since we probably already had as much space as we could get anyway
                            mustRedraw = false;
                        }
                    }
                }

                this.previousText = this.Text;

                this.AnnounceChange(mustRedraw);
            }
        }

        public bool LoggingEnabled { get; set; }
        public static double AverageNumComputationsPerQuery
        {
            get
            {
                if (numQueries < 0)
                    return -1;
                return numComputations / numQueries;
            }
        }

        static double numComputations = 0;
        static double numQueries = 0;
        private TextFormatter textFormatter;
        private LayoutScore bonusScore;
        private String previousText;

    }


    // the entire TextFormatter class should be unnecessary, but Silverlight doesn't seem to otherwise support synchronously computing the size of some text
    public class TextFormatter
    {
        public static TextFormatter Default
        {
            get
            {
                if (defaultFormatter == null)
                    defaultFormatter = new TextFormatter();
                return defaultFormatter;
            }
        }
        private static TextFormatter defaultFormatter;

        public TextFormatter()
        {
        }

        // Tells the required size for a block of text that's supposed to fit it into a column of the given width
        public Size FormatText(String text, double desiredWidth)
        {
            if (text == null || text == "")
                return new Size();

            double maxWidth = 0;
            double totalHeight = 0;
            double x = 0;
            double currentHeight = 0;
            string[] components = text.Split(' ');
            string blockText;
            foreach (string component in components)
            {
                blockText = component + " "; // it does waste some room to always make room for the space, but it seems to mirror how Silverlight actually does the layout
                Size blockSize = this.formatBlock(blockText);
                if (x + blockSize.Width > desiredWidth && x > 0)
                {
                    // go to the next line
                    totalHeight += currentHeight;
                    currentHeight = 0;
                    maxWidth = Math.Max(maxWidth, x);
                    x = 0;
                }
                // add this word to the end of the current line
                currentHeight = Math.Max(currentHeight, blockSize.Height);
                x += blockSize.Width;
            }
            maxWidth = Math.Max(x, maxWidth);
            totalHeight += currentHeight;
            Size size1 = new Size(maxWidth, totalHeight);
            return size1;
        }

        private Size formatBlock(String text)
        {
            Size size;
            // try to load from cache
            if (this.sizeCache == null)
                this.sizeCache = new Dictionary<string, Size>();
            if (!this.sizeCache.TryGetValue(text, out size))
            {
                size = this.computeSize(text);
                this.sizeCache[text] = size;
            }
            return size;
        }
        private Size computeSize(String text)
        {
            SKPaint textBlock = this.getTextBlock();
            double width = textBlock.MeasureText(text);
            
            // kind of hacky but effective
            //double width = this.fontSize * text.Length / 2;
            return new Size(width, this.fontSize);
        }

        public double FontSize
        {
            get
            {
                return this.fontSize;
            }
            set
            {
                if (!(value == this.FontSize))
                {
                    this.textBlock = null;
                    this.sizeCache = null;
                }
                this.fontSize = value;
            }
        }
        private double fontSize;



        private Dictionary<String, Size> sizeCache;
        private SKPaint textBlock;
        private SKPaint getTextBlock()
        {
            if (this.textBlock == null)
            {
                this.textBlock = new SKPaint();
                this.textBlock.TextSize = (float)this.FontSize;
            }
            return this.textBlock;
        }
    }


}
