using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Transactions;
using System.Xml.Schema;
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
            this.textItem_text = this.TextItem_Configurer.ModelledText;
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
                return this.TextItem_Configurer.ModelledText;
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
            this.TextItem_Text = this.TextItem_Configurer.ModelledText;
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
            Specific_TextLayout specificLayout = this.ComputeDimensions(new Size(query.MaxWidth, query.MaxHeight), query.Debug, query.LayoutDefaults.TextBox_Defaults.FontName);
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
            Specific_TextLayout specificLayout = this.ComputeDimensions(new Size(0, 0), query.Debug, query.LayoutDefaults.TextBox_Defaults.FontName);
            if (query.Accepts(specificLayout))
                return this.prepareLayoutForQuery(specificLayout, query);
            specificLayout = this.ComputeDimensions(new Size(query.MaxWidth, query.MaxHeight), query.Debug, query.LayoutDefaults.TextBox_Defaults.FontName);
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
            Specific_TextLayout specificLayout = this.ComputeDimensions(new Size(0, 0), query.Debug, query.LayoutDefaults.TextBox_Defaults.FontName);
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
            Specific_TextLayout bestAllowedDimensions = this.ComputeDimensions(new Size(double.PositiveInfinity, double.PositiveInfinity), query.Debug, query.LayoutDefaults.TextBox_Defaults.FontName);
            double pixelSize = 1;
            double maxRejectedWidth = 0;
            int numIterations = 0;
            bool firstIteration = true;
            double maxWidth = query.MaxWidth;
            while (maxRejectedWidth < bestAllowedDimensions.Width - pixelSize / 2)
            {
                numIterations++;
                // given the current width, compute the required height
                Specific_TextLayout newDimensions = this.ComputeDimensions(new Size(maxWidth, double.PositiveInfinity), query.Debug, query.LayoutDefaults.TextBox_Defaults.FontName);
                if (newDimensions.Height <= query.MaxHeight && query.MinScore.CompareTo(newDimensions.Score) <= 0)
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
                        Size desiredSize = this.formatText(maxWidth, query.Debug, this.FontSize, this.fontName).Size;
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

        private FormattedParagraph formatText(double maxWidth, bool debug, double fontSize, string fontName)
        {
            // invalidate cache if font name changed
            if (fontName != this.fontName)
            {
                this.layoutsByWidth = new Dictionary<double, FormattedParagraph>();
                this.fontName = fontName;
            }
            // check the cache
            FormattedParagraph formatted;
            if (this.layoutsByWidth.TryGetValue(maxWidth, out formatted))
            {
                if (this.LoggingEnabled)
                    System.Diagnostics.Debug.WriteLine("TextLayout.formatText cache hit for width " + maxWidth + ": " + formatted.Size);
                return formatted;
            }
            // recompute and save into cache
            formatted = this.GetTextFormatter(fontName).FormatText(this.TextToFit, maxWidth, this.AllowSplittingWords, debug, fontSize);
            if (this.textItem_text == null || this.textItem_text == "")
                formatted.Text = this.textItem_text;
            this.layoutsByWidth[maxWidth] = formatted;
            return formatted;
        }

        // compute the best dimensions fitting within the given size
        private Specific_TextLayout ComputeDimensions(Size availableSize, bool debug, string fontName)
        {
            DateTime start = DateTime.Now;
            numComputations++;

            FormattedParagraph formattedText = this.formatText(availableSize.Width, debug, this.FontSize, fontName);
            Size desiredSize = formattedText.Size;
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
            Specific_TextLayout specificLayout = new Specific_TextLayout(this.TextItem_Configurer, width, height, this.FontSize, 
                this.ComputeScore(desiredSize, availableSize, this.TextToFit, formattedText.Text), 
                formattedText.Text, desiredSize, this.fontName);
            specificLayout.Cropped = cropped;

            // diagnostics
            if (this.LoggingEnabled)
            {
                DateTime end = DateTime.Now;
                TimeSpan duration = end.Subtract(start);
                System.Diagnostics.Debug.WriteLine("spent " + duration + " to measure '" + this.Summarize(this.TextToFit) + "' in " + availableSize +
                    "; desired " + desiredSize + " (formatted = " + this.Summarize(formattedText.Text) + "); requesting " + specificLayout.Size);
            }

            return specificLayout;
        }
        private string Summarize(string text)
        {
            int maxLength = 100;
            if (text == null || text.Length <= maxLength)
            {
                return text;
            }
            string suffix = "...";
            return text.Substring(0, maxLength - suffix.Length) + suffix;

        }
        private int countLinewraps(string text)
        {
            int numLineWraps = 0;
            if (text != null)
            {
                for (int i = 0; i < text.Length; i++)
                {
                    if (text[i] == '\n')
                        numLineWraps++;
                }
            }
            return numLineWraps;

        }
        private LayoutScore ComputeScore(Size desiredSize, Size availableSize, string originalText, string formattedText)
        {
            if ((originalText == "" || originalText == null) && !this.ScoreIfEmpty)
                return LayoutScore.Zero;
            bool cropped = (desiredSize.Width > availableSize.Width || desiredSize.Height > availableSize.Height);
            int numLineWraps = this.countLinewraps(formattedText);
            if (cropped)
            {
                if (this.ScoreIfCropped)
                {
                    double desiredArea = desiredSize.Width * desiredSize.Height;
                    double availableArea = availableSize.Width * availableSize.Height;
                    return this.BonusScore.Times(availableArea / desiredArea)
                        .Plus(LayoutScore.Get_CutOff_LayoutScore(1))
                        .Plus(LayoutScore.Get_UnCentered_LayoutScore(numLineWraps));
                }
                else
                {
                    return LayoutScore.Get_CutOff_LayoutScore(1);
                }
            }
            return new LayoutScore(this.BonusScore).Plus(LayoutScore.Get_UnCentered_LayoutScore(numLineWraps));
        }


        public TextFormatter GetTextFormatter(string fontName)
        {
            if (fontName == null)
                fontName = "";
            return TextFormatter.GetForFontName(fontName);
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
                {
                    LayoutScore bonus = LayoutScore.Get_UsedSpace_LayoutScore(this.FontSize);
                    if (this.AllowSplittingWords)
                        bonus = bonus.Plus(LayoutScore.Get_UnCentered_LayoutScore(1));
                    this.bonusScore = bonus;
                }
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
                Specific_TextLayout layoutForCurrentText = this.ComputeDimensions(currentSize, false, this.fontName);
                this.TextItem_Text = this.Text;
                this.layoutsByWidth = new Dictionary<double, FormattedParagraph>();
                Specific_TextLayout layoutForNewText = this.ComputeDimensions(currentSize, false, this.fontName);
                LayoutScore oldScore = layoutForCurrentText.Score;
                LayoutScore newScore = layoutForNewText.Score;
                if (!oldScore.Equals(newScore))
                {
                    // Something about the score would change if we keep the same size and use the new text
                    // Maybe we suddenly need more space and should ask for it
                    // Maybe we suddenly have enough space and we might become an interesting layout that permits a different font size
                    // In either of these cases, we want to recalculate the layout size
                    mustRedraw = true;
                }
                else
                {
                    // The score didn't change with the new text and the old layout size
                    // So, the user probably isn't interested in having us recompute the layout dimensions
                    mustRedraw = false;
                    this.TextItem_Configurer.DisplayText = layoutForNewText.DisplayText;
                }

                if (this.LoggingEnabled)
                {
                    System.Diagnostics.Debug.WriteLine("TextLayout calculating: Have size: " + currentSize + ". Old text: " + layoutForCurrentText.DisplayText +
                        ". Old target: " + layoutForCurrentText.DesiredSizeForDebugging + ". New text: " + layoutForNewText.DisplayText + ". New target: " + layoutForNewText.DesiredSizeForDebugging);
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
                    this.layoutsByWidth = new Dictionary<double, FormattedParagraph>();
                }
            }
        }

        static double numComputations = 0;
        static double numQueries = 0;
        private TextFormatter textFormatter;
        private LayoutScore bonusScore;
        private String textItem_text;
        private Dictionary<double, FormattedParagraph> layoutsByWidth = new Dictionary<double, FormattedParagraph>();
        string fontName;

    }

    public enum TextFormatterType
    {
        UNDECIDED = 0,
        UNIFORMS_MISC = 1,
        UNIFORMS_MISC_ROUND_UP = 2,
        INNATE = 3
    }

    public class TextFormatter
    {
        private static Dictionary<string, TextFormatter> formattersByFontName = new Dictionary<string, TextFormatter>();
        public static TextFormatter GetForFontName(string fontName)
        {
            if (!formattersByFontName.ContainsKey(fontName))
                formattersByFontName[fontName] = new TextFormatter(fontName);
            return formattersByFontName[fontName];
        }

        public TextFormatter(string fontName)
        {
            this.fontName = fontName;
        }

        // Tells the required size for a block of text that's supposed to fit it into a column of the given width
        // The returned size might have larger width than desiredWidth if needed for the text to fit
        public FormattedParagraph FormatText(String text, double desiredWidth, bool allowSplittingWords, bool debug, double fontSize)
        {
            FormattedParagraph result;
            if (text == null || text == "")
            {
                result = new FormattedParagraph(new Size(), text);
                if (debug)
                    System.Diagnostics.Debug.WriteLine("Formatted empty text '" + text + "' into size " + result);
                return result;
            }
            string[] blocks = text.Split('\n');
            double maxWidth = 0, totalHeight = 0;
            List<string> formattedStrings = new List<string>();
            for (int i = 0; i < blocks.Length; i++)
            {
                string block = blocks[i];
                string formatBlock = block;
                if (formatBlock == "")
                    formatBlock = "M";

                FormattedParagraph formattedBlock = this.FormatParagraph(formatBlock, desiredWidth, allowSplittingWords, fontSize);
                if (block == "")
                    formattedBlock.Text = block;
                formattedStrings.Add(formattedBlock.Text);
                Size blockSize = formattedBlock.Size;
                maxWidth = Math.Max(maxWidth, blockSize.Width);
                totalHeight += blockSize.Height;
            }
            string formattedText = String.Join("\n",formattedStrings);
            result = new FormattedParagraph(new Size(maxWidth, totalHeight), formattedText);
            if (debug)
            {
                System.Diagnostics.Debug.WriteLine("Formatted text '" + text + "'" + " using desiredWith = " + desiredWidth +
                    " and allowSplittingWords = " + allowSplittingWords + " into size " + result);
            }
            return result;
        }
        public FormattedParagraph FormatParagraph(String text, double desiredWidth, bool allowSplittingWords, double fontSize)
        {
            if (text == null || text == "")
                return new FormattedParagraph(new Size(), text);
            
            // total size of the paragraph
            Size totalSize = new Size();

            // the unsplittable units that we're going to measure
            List<string> components;
            int i;
            if (allowSplittingWords)
            {
                components = new List<string>();
                for (i = 0; i < text.Length; i++)
                {
                    components.Add(text.Substring(i, 1));
                }
            }
            else
            {
                string[] words = text.Split(' ');
                components = new List<string>();
                for (i = 0; i < words.Length; i++)
                {
                    if (i != 0)
                        components.Add(" ");
                    components.Add(words[i]);
                }
            }
            List<string> formattedLines = new List<string>();

            i = 0;
            while (i < components.Count())
            {
                // consume as many words as will fit on this line
                FormattedLine line = consumeComponents(components, i, desiredWidth, fontSize);
                // add new text
                formattedLines.Add(line.Text);
                // update size
                totalSize.Width = Math.Max(totalSize.Width, line.Size.Width);
                totalSize.Height += line.Size.Height;
                // continue to subsequent components
                i += line.NumComponents;
                // If a space character wrapped to the next line, we can skip it instead
                if (i < components.Count)
                {
                    if (components[i] == " ")
                    {
                        i++;
                    }
                }
            }
            return new FormattedParagraph(totalSize, String.Join("\n", formattedLines));
        }

        private FormattedLine consumeComponents(List<string> components, int startIndex, double desiredWidth, double fontSize)
        {
            FormattedLine bestResult = null;
            int lowCount = 1;
            int highCount = components.Count() - startIndex;
            while (highCount >= lowCount)
            {
                int testCount = Math.Min(lowCount * 2, (lowCount + highCount) / 2);

                string testText = String.Join("", components.GetRange(startIndex, testCount));
                Size requiredSize = this.getLineSize(testText, fontSize);
                if (requiredSize.Width <= desiredWidth)
                {
                    // we found some characters that all fit into one line
                    bestResult = new FormattedLine(testCount, testText, requiredSize);
                    lowCount = testCount + 1;
                }
                else
                {
                    // we tried to use too many characters at once, and they didn't all fit
                    if (bestResult == null)
                    {
                        // We can only get here on the first iteration, when measuring only the first word
                        // If not even the first word fit, then we return it anyway, so the caller knows how much space it needs
                        bestResult = new FormattedLine(testCount, testText, requiredSize);
                    }
                    highCount = testCount - 1;
                }
            }
            return bestResult;
        }

        // Returns the size of the given text
        // Does use the cache
        private Size getLineSize(string text, double fontSize)
        {
            // Ensure we have a cache of this size
            if (!this.sizesCache.ContainsKey(fontSize))
                this.sizesCache[fontSize] = new Dictionary<string, Size>();
            // If our cache is too large, clear it
            if (this.sizesCache[fontSize].Count > 4000)
                this.sizesCache[fontSize] = new Dictionary<string, Size>();
            // try to load from cache
            Dictionary<string, Size> sizeCache = this.sizesCache[fontSize];
            Size measuredSize;
            if (!sizeCache.TryGetValue(text, out measuredSize))
            {
                measuredSize = this.computeLineSize(text, fontSize);
                sizeCache[text] = measuredSize;
            }
            return measuredSize;
        }

        // TODO: can UniformsMisc be made to support UWP?

        // Computes the size required by a TextBlock that plans to display this text all in a line
        // Doesn't use any cache
        private Size computeLineSize(String text, double fontSize)
        {
            return TextMeasurer.Instance.Measure(text, fontSize, this.fontName);
        }


        private double fontLineHeight;
        private double leftMargin;
        // Dictionary<FontSize, Dictionary<Text, MeasuredSize>>
        private Dictionary<double, Dictionary<String, Size>> sizesCache = new Dictionary<double, Dictionary<string, Size>>();
        private string fontName;
    }

    public class FormattedParagraph
    {
        public FormattedParagraph(Size size, string text)
        {
            this.Size = size;
            this.Text = text;
        }
        public string Text;
        public Size Size;
    }

    class FormattedLine
    {
        public FormattedLine(int numComponents, string text, Size size)
        {
            this.NumComponents = numComponents;
            this.Text = text;
            this.Size = size;
        }
        public int NumComponents;
        public string Text;
        public Size Size;
    }


}
