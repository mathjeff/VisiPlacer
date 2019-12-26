using SkiaSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Xamarin.Forms;

// The TextLayout exists to consolidate some code between TextblockLayout and TextboxLayout
// Most callers should use one of those classes instead
namespace VisiPlacement
{
    public class TextLayout : LayoutChoice_Set
    {
        // returns a TextLayout that still gets fractional points while cropped
        public static LayoutChoice_Set New_Croppable(TextItem_Configurer textItem, double fontSize, bool scoreIfEmpty = true)
        {
            // We make a ScrollLayout and ask it to do the size computations but to not actually put the result into a ScrollView
            TextLayout textLayout = new TextLayout(textItem, fontSize, true, scoreIfEmpty);
            return ScrollLayout.New(textLayout, null);
        }

        public TextLayout(TextItem_Configurer textItem, double fontSize, bool allowSplittingWords = false, bool scoreIfEmpty = true)
        {
            this.TextItem_Configurer = textItem;
            this.FontSize = fontSize;
            this.ScoreIfEmpty = scoreIfEmpty;
            this.textItem_text = this.TextItem_Configurer.Text;
            this.AllowSplittingWords = allowSplittingWords;
            textItem.Add_TextChanged_Handler(new PropertyChangedEventHandler(this.On_TextChanged));
        }
        protected TextItem_Configurer TextItem_Configurer { get; set; }
        public double FontSize { get; set; }
        public bool ScoreIfEmpty { get; set; }
        public bool ScoreIfCropped { get; }
        public bool AllowSplittingWords { get; set; }

        public String Text
        {
            get
            {
                return this.TextItem_Configurer.Text;
            }
        }
        public int TextLength
        {
            get
            {
                if (this.Text == null)
                    return 0;
                return this.Text.Length;
            }
        }
        private String TextToFit
        {
            get
            {
                String result = this.textItem_text;
                if ((result == "" || result == null) && this.ScoreIfEmpty)
                    result = "A";
                return result;
            }
        }
        private TextFormatter MakeTextFormatter()
        {
            return TextFormatter.ForSize(this.FontSize);
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
            this.TextItem_Text = this.TextItem_Configurer.Text;
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
            Specific_TextLayout specificLayout = this.ComputeDimensions(new Size(query.MaxWidth, query.MaxHeight), query.Debug);
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
            Specific_TextLayout specificLayout = this.ComputeDimensions(new Size(0, 0), query.Debug);
            if (query.Accepts(specificLayout))
                return this.prepareLayoutForQuery(specificLayout, query);
            specificLayout = this.ComputeDimensions(new Size(query.MaxWidth, query.MaxHeight), query.Debug);
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
            Specific_TextLayout specificLayout = this.ComputeDimensions(new Size(0, 0), query.Debug);
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

            Specific_TextLayout bestAllowedDimensions = this.ComputeDimensions(new Size(double.PositiveInfinity, double.PositiveInfinity), query.Debug);
            double pixelSize = 1;
            double maxRejectedWidth = 0;
            int numIterations = 0;
            bool firstIteration = true;
            double maxWidth = query.MaxWidth;
            while (maxRejectedWidth < bestAllowedDimensions.Width - pixelSize / 2)
            {
                numIterations++;
                // given the current width, compute the required height
                Specific_TextLayout newDimensions = this.ComputeDimensions(new Size(maxWidth, double.PositiveInfinity), query.Debug);
                if (newDimensions.Height <= query.MaxHeight && !newDimensions.Cropped)
                {
                    // this layout fits in the required dimensions
                    if (newDimensions.Width <= bestAllowedDimensions.Width && newDimensions.Width <= query.MaxWidth)
                    {
                        // this layout is at least as good as the best layout we found so far
                        bestAllowedDimensions = newDimensions;
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
                    // if the first layout we found was too tall, then there will need to be some cropping
                    if (double.IsPositiveInfinity(bestAllowedDimensions.Width))
                        return null;
                }
                // calculate a new size, by guessing based on required area
                double desiredArea = newDimensions.Height * Math.Max(maxWidth, newDimensions.Width);
                maxWidth = desiredArea / query.MaxHeight;
                // Make sure that the next value we check is inside the range that we haven't checked yet, to make sure we're making progress
                // If our area-based is outside the unexplored range, then from now on just split the remaining range in half on each iteration
                if (maxWidth < (maxRejectedWidth + pixelSize / 2))
                {
                    if (firstIteration)
                    {
                        // The first time that we find we have enough area to make the width very tiny, we calculate the true minimum amount of width required
                        Size desiredSize = this.formatText(maxWidth, query.Debug);
                        if (desiredSize.Width > maxWidth)
                            maxRejectedWidth = desiredSize.Width - pixelSize / 2;
                        maxWidth = desiredSize.Width;
                    }
                    else
                    {
                        // The second time we find that we have enough area to make the width very tiny, we don't recalculate the true min width required because we already did
                        // Instead we just do a binary search
                        maxWidth = (maxRejectedWidth + bestAllowedDimensions.Width) / 2;
                    }
                }
                else
                {
                    if (maxWidth > (bestAllowedDimensions.Width - pixelSize / 2))
                    {
                        maxWidth = (maxRejectedWidth + bestAllowedDimensions.Width) / 2;
                    }
                }
                firstIteration = false;
            }
            if (this.LoggingEnabled)
                System.Diagnostics.Debug.WriteLine("Spent " + numIterations + " iterations in Get_NonCropping_MinWidthLayout with query = " + query + " and text length = " + this.TextLength);
            if (!query.Accepts(bestAllowedDimensions))
                return null;
            return bestAllowedDimensions;
        }

        private Size formatText(double maxWidth, bool debug)
        {
            // check the cache
            Size size;
            if (this.layoutsByWidth.TryGetValue(maxWidth, out size))
            {
                if (this.LoggingEnabled)
                    System.Diagnostics.Debug.WriteLine("TextLayout.formatText cache hit for width " + maxWidth + ": " + size);
                return size;
            }
            // recompute and save into cache
            size = this.TextFormatter.FormatText(this.TextToFit, maxWidth, this.AllowSplittingWords, debug);
            this.layoutsByWidth[maxWidth] = size;
            return size;
        }

        // compute the best dimensions fitting within the given size
        private Specific_TextLayout ComputeDimensions(Size availableSize, bool debug)
        {
            DateTime start = DateTime.Now;
            numComputations++;

            Size desiredSize = this.formatText(availableSize.Width, debug);
            if (desiredSize.Width < 0 || desiredSize.Height < 0)
            {
                ErrorReporter.ReportParadox("Illegal size " + desiredSize + " returned by textFormatter.FormatText");
            }
            bool cropped = false;
            double width, height;
            // assign the width, height, and score
            if (desiredSize.Width <= availableSize.Width && desiredSize.Height <= availableSize.Height)
            {
                // no cropping is necessary
                width = desiredSize.Width;
                height = desiredSize.Height;
            }
            else
            {
                // cropping
                cropped = true;
                if (this.ScoreIfCropped)
                {
                    width = Math.Min(desiredSize.Width, availableSize.Width);
                    height = Math.Min(desiredSize.Height, availableSize.Height);
                }
                else
                {
                    width = height = 0;
                }
            }
            Specific_TextLayout specificLayout = new Specific_TextLayout(this.TextItem_Configurer, width, height, this.FontSize, this.ComputeScore(desiredSize, availableSize, this.TextToFit), this.textItem_text, desiredSize);
            specificLayout.Cropped = cropped;

            // diagnostics
            if (this.LoggingEnabled)
            {
                int maxLengthToLog = 100;
                string textToLog;
                if (this.Text.Length > maxLengthToLog)
                    textToLog = this.Text.Substring(0, maxLengthToLog) + "...";
                else
                    textToLog = this.Text;
                DateTime end = DateTime.Now;
                TimeSpan duration = end.Subtract(start);
                System.Diagnostics.Debug.WriteLine("spent " + duration + " to measure '" + textToLog + "' in " + availableSize + "; desired " + desiredSize + "; requesting " + specificLayout.Size);
            }

            return specificLayout;
        }
        private LayoutScore ComputeScore(Size desiredSize, Size availableSize, string text)
        {
            if ((text == "" || text == null) && !this.ScoreIfEmpty)
                return LayoutScore.Zero;
            bool cropped = (desiredSize.Width > availableSize.Width || desiredSize.Height > availableSize.Height);
            if (cropped)
            {
                if (this.ScoreIfCropped)
                {
                    double desiredArea = desiredSize.Width * desiredSize.Height;
                    double availableArea = availableSize.Width * availableSize.Height;
                    return this.BonusScore.Times(availableArea / desiredArea).Plus(LayoutScore.Get_CutOff_LayoutScore(1));
                }
                else
                {
                    return LayoutScore.Get_CutOff_LayoutScore(1);
                }
            }
            return new LayoutScore(this.BonusScore);
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
            if (this.Text != this.textItem_text)
            {
                this.considerAnnouncingChanges();
                this.TextItem_Text = this.Text;
            }
        }

        private void considerAnnouncingChanges()
        {
            if (this.LoggingEnabled)
                System.Diagnostics.Debug.WriteLine("considerAnnouncingChanges textItem_text = " + this.textItem_text + " Text = " + this.Text);
            if (this.Get_ChangedSinceLastRender())
                return;
            bool mustRedraw = false;
            if (!this.ScoreIfEmpty && (this.Text == "" || this.Text == null || this.textItem_text == "" || this.textItem_text == null))
            {
                mustRedraw = true;
            }
            else
            {
                View view = this.TextItem_Configurer.View;
                Size currentSize = new Size(view.Width, view.Height);
                Specific_TextLayout layoutForCurrentText = this.ComputeDimensions(currentSize, false);
                this.TextItem_Text = this.Text;
                this.layoutsByWidth = new Dictionary<double, Size>();
                Specific_TextLayout layoutForNewText = this.ComputeDimensions(currentSize, false);
                if (!layoutForNewText.Cropped)
                {
                    if (!layoutForCurrentText.Cropped)
                    {
                        // The new text fits in the existing box, so this box doesn't need any more space
                        // It is possible that the text has shrunken and another box now can use extra space, but the user probably doesn't care about that right now
                        mustRedraw = false;
                    }
                    else
                    {
                        // This layout suddenly no longer needs to be cropped. That might be something that the user cares about
                        // This might allow us to increase the font size, for example
                        mustRedraw = true;
                        if (this.LoggingEnabled)
                            System.Diagnostics.Debug.WriteLine("TextLayout needs more space: no longer needs to be cropped");
                    }
                }
                else
                {
                    if (!layoutForCurrentText.Cropped)
                    {
                        // If we leave the render size the same then the text suddenly gets cropped, so we ask the layout engine for more space
                        mustRedraw = true;
                        if (this.LoggingEnabled)
                            System.Diagnostics.Debug.WriteLine("TextLayout needs more space: no longer has enough space");
                    }
                    else
                    {
                        if (this.LoggingEnabled)
                            System.Diagnostics.Debug.WriteLine("TextLayout was cropped and still cropped");
                        // If the text layout was cropped before and after then we might need to ask the engine for more space,
                        // but it would annoy the user to keep redoing the layout and probably see no change
                        // So, we don't bother asking for more space since we probably already had as much space as we could get anyway
                        mustRedraw = false;
                    }
                }
                if (this.LoggingEnabled)
                {
                    System.Diagnostics.Debug.WriteLine("TextLayout calculating: Have size: " + currentSize + ". Old text: " + layoutForCurrentText.TextForDebugging +
                        ". Old target: " + layoutForCurrentText.DesiredSizeForDebugging + ". New text: " + layoutForNewText.TextForDebugging + ". New target: " + layoutForNewText.DesiredSizeForDebugging);
                }

            }

            this.AnnounceChange(mustRedraw);
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

        private String TextItem_Text
        {
            set
            {
                if (this.textItem_text != value)
                {
                    this.textItem_text = value;
                    this.layoutsByWidth = new Dictionary<double, Size>();
                }
            }
        }

        static double numComputations = 0;
        static double numQueries = 0;
        private TextFormatter textFormatter;
        private LayoutScore bonusScore;
        private String textItem_text;
        private Dictionary<double, Size> layoutsByWidth = new Dictionary<double, Size>();

    }

    enum TextFormatterType
    {
        UNDECIDED = 0,
        UNIFORMS_MISC = 1,
        INNATE = 2
    }

    public class TextFormatter
    {
        private static TextFormatterType textFormatterType = TextFormatterType.UNDECIDED;

        private static Dictionary<double, TextFormatter> formattersByFontSize = new Dictionary<double, TextFormatter>();
        public static TextFormatter ForSize(double size)
        {
            if (!formattersByFontSize.ContainsKey(size))
            {
                TextFormatter formatter = new TextFormatter();
                formatter.FontSize = size;
                formattersByFontSize[size] = formatter;
            }
            return formattersByFontSize[size];
        }

        // Tells the required size for a block of text that's supposed to fit it into a column of the given width
        // The returned size might have larger width than desiredWidth if needed for the text to fit
        public Size FormatText(String text, double desiredWidth, bool allowSplittingWords, bool debug)
        {
            Size result;
            if (text == null || text == "")
            {
                result = new Size();
                if (debug)
                    System.Diagnostics.Debug.WriteLine("Formatted empty text '" + text + "' into size " + result);
                return result;
            }
            string[] blocks = text.Split('\n');
            double maxWidth = 0, totalHeight = 0;
            for (int i = 0; i < blocks.Length; i++)
            {
                string block = blocks[i];
                if (block == "")
                    block = "M";

                Size blockSize = this.FormatParagraph(block, desiredWidth, allowSplittingWords);
                maxWidth = Math.Max(maxWidth, blockSize.Width);
                totalHeight += blockSize.Height;
            }
            result = new Size(maxWidth, totalHeight);
            if (debug)
            {
                System.Diagnostics.Debug.WriteLine("Formatted text '" + text + "'" + " using desiredWith = " + desiredWidth +
                    " and allowSplittingWords = " + allowSplittingWords + " into size " + result);
            }
            return result;
        }
        public Size FormatParagraph(String text, double desiredWidth, bool allowSplittingWords)
        {
            if (text == null || text == "")
                return new Size();

            // previous unsplittable unit in the current line
            String prevComponentInLine = "";
            // current size of the current line
            Size lineSize = new Size();
            // total size of the paragraph
            Size totalSize = new Size();

            // the unsplittable units that we're going to measure
            string[] components;
            int i;
            if (allowSplittingWords)
            {
                components = new string[text.Length];
                for (i = 0; i < text.Length; i++)
                {
                    components[i] = text.Substring(i, 1);
                }
            }
            else
            {
                components = text.Split(' ');
            }

            // loop until done
            i = 0;
            while (i < components.Length)
            {
                // current unit that we want to add
                String currentComponent = components[i];
                // the text that we're considering making be the ending of this line
                String currentExpandedComponent;
                if (allowSplittingWords)
                {
                    currentExpandedComponent = prevComponentInLine + currentComponent;
                }
                else
                {
                    if (prevComponentInLine != "")
                    {
                        currentExpandedComponent = prevComponentInLine + " " + currentComponent;
                    }
                    else
                    {
                        if (currentComponent == "")
                            currentComponent = " ";
                        currentExpandedComponent = currentComponent;
                    }
                }
                // the size of the new text
                Size currentCharsSize = this.getBlockSize(currentExpandedComponent);
                // the size of the existing ending of the line
                Size prevCharSize = this.getBlockSize(prevComponentInLine);
                // the new size of the current line if we add this text to this line
                Size new_lineSize = new Size(lineSize.Width + currentCharsSize.Width - prevCharSize.Width, Math.Max(lineSize.Height, currentCharsSize.Height));
                if (new_lineSize.Width > desiredWidth && prevComponentInLine != "")
                {
                    // line wrap
                    totalSize.Width = Math.Max(totalSize.Width, lineSize.Width);
                    totalSize.Height += lineSize.Height;
                    prevComponentInLine = "";
                    lineSize = new Size();
                }
                else
                {
                    // extend current line
                    lineSize = new_lineSize;
                    prevComponentInLine = currentComponent;
                    i++;
                }
            }
            totalSize.Width = Math.Max(totalSize.Width, lineSize.Width);
            totalSize.Height += lineSize.Height;
            return totalSize;
        }

        private Size getBlockSize(String text)
        {
            Size size;
            // clear cache if too large
            if (this.sizeCache != null && this.sizeCache.Count > 2000)
                this.sizeCache = null;
            // try to load from cache
            if (this.sizeCache == null)
                this.sizeCache = new Dictionary<string, Size>();
            if (!this.sizeCache.TryGetValue(text, out size))
            {
                if (text.Length > 2)
                {
                    size = new Size();
                    String prevCharacter = "";
                    for (int i = 0; i < text.Length; i++)
                    {
                        String currentCharacter = text.Substring(i, 1);
                        Size prevSize = this.getBlockSize(prevCharacter);
                        Size currentSize = this.getBlockSize(prevCharacter + currentCharacter);
                        size.Height = Math.Max(size.Height, currentSize.Height);
                        size.Width += currentSize.Width - prevSize.Width;
                        prevCharacter = currentCharacter;
                    }
                }
                else
                {
                    size = this.computeLineSize(text);
                }
                this.sizeCache[text] = size;
            }
            return size;
        }

        // TODO: can UniformsMisc be made to support UWP?
        private void chooseTextFormatterType()
        {
            if (textFormatterType == TextFormatterType.UNDECIDED)
            {
                try
                {
                    Uniforms.Misc.TextUtils.GetTextSize("A", double.PositiveInfinity, this.FontSize);
                    textFormatterType = TextFormatterType.UNIFORMS_MISC;
                }
                catch (Exception e)
                {
                    textFormatterType = TextFormatterType.INNATE;
                }
            }
        }
        // computes the size required by a TextBlock that plans to display this text all in a line
        private Size computeLineSize(String text)
        {
            this.chooseTextFormatterType();
            if (textFormatterType == TextFormatterType.UNIFORMS_MISC)
                return Uniforms.Misc.TextUtils.GetTextSize(text, double.PositiveInfinity, this.FontSize);
            // Get enough width for the given text, and enough height to accomodate the line spacing
            double width = this.computeGlyphSize(text).Width;
            return new Size(width + this.leftMargin, this.fontLineHeight);
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

        private SKPaint getTextBlock()
        {
            if (this.textBlock == null)
            {
                this.textBlock = new SKPaint();
                Label label = new Label();
                this.textBlock.TextSize = (float)this.FontSize;
                this.textBlock.Typeface = SKTypeface.FromFamilyName(label.FontFamily);

                // leave enough height for the characters for the highest ("M") and lowest ("g") characters
                SKFontMetrics metrics = new SKFontMetrics();
                textBlock.GetFontMetrics(out metrics);
                this.fontLineHeight = metrics.Descent - metrics.Ascent;
                SKRect bounds = new SKRect();
                this.textBlock.MeasureText("M", ref bounds);
                this.leftMargin = bounds.Left;
            }
            return this.textBlock;
        }

        // Computes the size of the letters in the given string, and doesn't add any extra margins
        // TODO: make this more accurate
        private Size computeGlyphSize(String text)
        {
            if (text == null || text == "")
                return new Size();
            SKPaint textBlock = this.getTextBlock();
            SKRect bounds = new SKRect();
            double width = textBlock.MeasureText(text, ref bounds);

            return new Size(bounds.Width, bounds.Height);
            
        }


        private double fontSize;
        private double fontLineHeight;
        private double leftMargin;
        private Dictionary<String, Size> sizeCache;
        private SKPaint textBlock;

    }


}
