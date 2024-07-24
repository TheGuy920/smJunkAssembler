using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Xml;
using WpfAnimatedGif;

namespace ModTool.Util
{
    public static partial class FlowdocumentGenerator
    {
        private const string FlowDoc = "<FlowDocument xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" " +
                    "xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"><Paragraph>";
        private const string FlowDocEnd = "</Paragraph></FlowDocument>";
        private static string EscapeXaml(string xamlText) => xamlText.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
        private const char OpenTag = '[';
        private const char CloseTag = ']';

        private static readonly Dictionary<int, string> HeaderSizes = new()
        {
            [1] = "22px",
            [2] = "18px",
            [3] = "16px"
        };
        private static readonly Dictionary<int, string> LineHeightSizes = new()
        {
            [1] = "8px",
            [2] = "6px",
            [3] = "4px"
        };
        private static readonly char[] EndLinkChars = ['>', '"', '<'];
        private static readonly string[] QuickTags = ["b", "u", "i", "strike", "spoiler", "hr", "code"];

        private const string OpenCodeBlock = "<BlockUIContainer>" +
            "<Border BorderBrush=\"#4D4D4D\" CornerRadius=\"2\" BorderThickness=\"1\" Padding=\"10,15\" Margin=\"5,10,5,0\" HorizontalAlignment=\"Stretch\">" +
            "<TextBlock Margin=\"0,0,0,0\" HorizontalAlignment=\"Left\" VerticalAlignment=\"Center\" " +
            "FontFamily=\"Consolas, monospace\" Foreground=\"#acb2b8\" FontSize=\"11px\" LineHeight=\"8px\">";
        private const string CloseCodeBlock = "</TextBlock></Border></BlockUIContainer>";

        private enum OpenTags
        {
            Bold,
            Italic,
            Underline,
            Strikethrough,
            Spoiler,
            Code
        }

        public static FlowDocument GenerateFlowDocument(string steam)
        {
            Stopwatch sw = Stopwatch.StartNew();

            string text = EscapeXaml(steam.Replace("\r", "")) + ' ';
            StringBuilder xaml = new();
            StringBuilder workingTag = new();
            int OpenBold = 0;
            int OpenItalic = 0;
            int OpenUnderline = 0;
            int OpenStrike = 0;
            int OpenSpoiler = 0;
            int OpenCode = 0;
            List<OpenTags> TagsList = [];
            List<OpenTags> recentlyClosed = [];

            xaml.Append(FlowDoc);
            List<string> currentFontFamily = ["{StaticResource MotivaSansRegular}"];
            List<string> currentFontSize = ["14.5px"];
            List<string> currentFontWeight = ["400"];
            List<string> currentFontStyle = ["Normal"];
            List<string> currentForeground = ["#ACB2B8"];
            List<string> currentTypography = [""];
            List<string> currentLineHeight = ["6px"];
            List<string> currentFontStretch = ["Medium"];
            List<(byte[], string)> images = [];

            bool isOpen = false;
            bool isNoParse = false;
            bool badParse = false;
            Action waitingForNewListitem = null;
            bool isInList = false;
            bool waitingForLinkToEnd = false;
            bool waitingForUrl = false;
            bool waitingForImage = false;
            bool isInTable = false;
            string linkUrl = "";
            string urlName = "";
            List<char> Last5Char = new(5);
            List<int> TableColumnCount = [];

            string CurrentParagraph()
            {
                return $"\n<Paragraph FontFamily=\"{currentFontFamily[^1]}\" FontStretch=\"{currentFontStretch[^1]}\" " +
                    $"FontSize=\"{currentFontSize[^1]}\" FontWeight=\"{currentFontWeight[^1]}\" " +
                    $"FontStyle=\"{currentFontStyle[^1]}\" Foreground=\"{currentForeground[^1]}\" {currentTypography[^1]} LineHeight=\"{currentLineHeight[^1]}\">";
            }

            List<OpenTags> CloseAllOpenTags()
            {
                var rlist = TagsList.Reverse<OpenTags>().ToList();
                foreach (var tag in rlist)
                {
                    switch (tag)
                    {
                        case OpenTags.Bold:
                            currentFontWeight.RemoveAt(currentFontWeight.Count - 1);
                            xaml.Append("</Span>");
                            break;
                        case OpenTags.Italic:
                            xaml.Append("</Italic>");
                            break;
                        case OpenTags.Underline:
                            OpenUnderline--;
                            xaml.Append("</Underline>");
                            break;
                        case OpenTags.Strikethrough:
                            xaml.Append("</Span>");
                            break;
                        case OpenTags.Spoiler:
                            xaml.Append("</Span>");
                            break;
                        case OpenTags.Code:
                            xaml.Append(CloseCodeBlock);
                            xaml.Append(CurrentParagraph());
                            ReopenTags(recentlyClosed);
                            break;
                    }
                }

                TagsList.Clear();
                return rlist;
            }

            void ReopenTags(List<OpenTags> tags)
            {
                OpenTags[] copy = [.. tags];
                tags.Clear();
                foreach (OpenTags tag in copy)
                {
                    switch (tag)
                    {
                        case OpenTags.Bold:
                            currentFontWeight.Add("700");
                            xaml.Append($"<Span FontFamily=\"{{StaticResource MotivaSansBold}}\" FontWeight=\"{currentFontWeight[^1]}\" FontSize=\"{currentFontSize[^1]}\">");
                            break;
                        case OpenTags.Italic:
                            xaml.Append("<Italic>");
                            break;
                        case OpenTags.Underline:
                            OpenUnderline++;
                            xaml.Append("<Underline>");
                            break;
                        case OpenTags.Strikethrough:
                            xaml.Append("<Span TextDecorations=\"Strikethrough\">");
                            break;
                        case OpenTags.Spoiler:
                            xaml.Append("<Span Foreground=\"#ACB2B8\">");
                            break;
                        case OpenTags.Code:
                            xaml.Append("\n" + OpenCodeBlock);
                            recentlyClosed = CloseAllOpenTags();
                            xaml.Append("</Paragraph>");
                            break;

                    }
                }

                TagsList = copy.Reverse().ToList();
            }

            foreach (char c in text)
            {
                if (!isOpen && !isNoParse && c == OpenTag && !waitingForLinkToEnd)
                {
                    isOpen = true;
                    continue;
                }

                if (isOpen && char.IsWhiteSpace(c))
                    continue;

                if (c == '\n' && !waitingForLinkToEnd)
                {
                    if (!isInList && !isInTable)
                    {
                        xaml.Append("<LineBreak/>\n");
                    }
                    continue;
                }

                if (!isOpen)
                {
                    if (waitingForImage)
                    {
                        linkUrl += c;
                        continue;
                    }
                    else if (waitingForUrl)
                    {
                        urlName += c;
                        continue;
                    }
                    else if (!waitingForLinkToEnd)
                    {
                        if (Last5Char.Count >= 5)
                            Last5Char.RemoveAt(0);
                        Last5Char.Add(c);

                        if (Last5Char.Count >= 5)
                        {
                            string maybeLink = string.Join("", Last5Char);
                            char p = maybeLink[0];
                            if ((EndLinkChars.Contains(p) || char.IsWhiteSpace(p)) && maybeLink[1..] == "http")
                            {
                                waitingForLinkToEnd = true;
                                xaml.Remove(xaml.Length - 4, 4);
                                linkUrl += maybeLink[1..];
                                continue;
                            }
                        }
                    }
                    else
                    {
                        if (EndLinkChars.Contains(c) || char.IsWhiteSpace(c))
                        {
                            waitingForLinkToEnd = false;
                            var uri = new Uri(linkUrl);
                            string decor = OpenUnderline > 0 ? "Underline" : "{x:Null}";
                            int ulinec = OpenUnderline;
                            if (ulinec > 0)
                            {
                                recentlyClosed = CloseAllOpenTags();
                                ReopenTags(recentlyClosed.Where(t => t != OpenTags.Underline).ToList());
                            }

                            xaml.Append($" <Hyperlink FontWeight=\"Bold\" NavigateUri=\"{ValidateUrl(uri.AbsoluteUri)}\" Foreground=\"#ebebeb\" TextDecorations=\"{decor}\">{linkUrl}</Hyperlink>");
                            linkUrl = "";
                            if (ulinec > 0)
                            {
                                CloseAllOpenTags();
                                ReopenTags(recentlyClosed);
                            }
                            Last5Char.Clear();
                        }
                        else
                        {
                            linkUrl += c;
                        }
                        continue;
                    }

                    xaml.Append(c);
                    continue;
                }

                if (c == CloseTag)
                {
                    isOpen = false;
                    if (badParse)
                    {
                        badParse = false;
                        continue;
                    }

                    string tag = workingTag.ToString();
                    workingTag.Clear();

                    if (tag == "noparse")
                    {
                        isNoParse = true;
                        continue;
                    }

                    if (tag == "/noparse")
                    {
                        isNoParse = false;
                        continue;
                    }

                    if (QuickTags.Contains(tag))
                    {
                        string optional = "";
                        if (tag == "code" || tag == "hr")
                        {
                            recentlyClosed = CloseAllOpenTags();
                            xaml.Append("</Paragraph>\n");
                        }

                        if (tag == "b" && currentFontSize[^1] == "14.5px")
                        {
                            currentFontSize.Add("14px");
                            optional = $"FontSize=\"14px\"";
                        }

                        switch (HandleOpenTag(tag, xaml, optional))
                        {
                            case 0:
                                OpenBold++;
                                currentFontWeight.Add("700");
                                break;
                            case 1:
                                OpenUnderline++;
                                break;
                            case 2:
                                OpenItalic++;
                                break;
                            case 3:
                                OpenStrike++;
                                break;
                            case 4:
                                OpenSpoiler++;
                                break;
                            case 5:
                                xaml.Append(CurrentParagraph());
                                ReopenTags(recentlyClosed);
                                break;
                            case 6:
                                OpenCode++;
                                break;
                        }
                        continue;
                    }

                    if (tag.StartsWith('/') && QuickTags.Contains(tag[1..]))
                    {
                        switch (HandleCloseTag(tag, xaml))
                        {
                            case 0:
                                OpenBold--;
                                currentFontWeight.RemoveAt(currentFontWeight.Count - 1);
                                if (currentFontSize.Count > 1 && currentFontSize[^1] == "14px" && currentFontSize[^2] == "14.5px")
                                {
                                    currentFontSize.RemoveAt(currentFontSize.Count - 1);
                                }
                                break;
                            case 1:
                                OpenUnderline--;
                                break;
                            case 2:
                                OpenItalic--;
                                break;
                            case 3:
                                OpenStrike--;
                                break;
                            case 4:
                                OpenSpoiler--;
                                break;
                            case 6:
                                OpenCode--;
                                xaml.Append(CurrentParagraph());
                                ReopenTags(recentlyClosed);
                                break;
                        }
                        continue;
                    }

                    if (tag == "*")
                    {
                        if (!isInList)
                        {
                            continue;
                        }

                        if (waitingForNewListitem is not null)
                        {
                            waitingForNewListitem();
                            waitingForNewListitem = null;
                        }

                        waitingForNewListitem = () =>
                        {
                            currentFontSize.RemoveAt(currentFontSize.Count - 1);
                            recentlyClosed = CloseAllOpenTags();
                            xaml.Append("</Paragraph></ListItem>");
                        };

                        currentFontSize.Add("14.01px");
                        xaml.Append($"<ListItem>{CurrentParagraph()}");
                        ReopenTags(recentlyClosed);
                        continue;
                    }

                    if (tag == "list" || tag == "/list" || tag == "olist" || tag == "/olist")
                    {
                        if (tag.Contains('/'))
                        {
                            if (waitingForNewListitem is not null)
                            {
                                waitingForNewListitem();
                                waitingForNewListitem = null;
                            }
                            currentLineHeight.RemoveAt(currentLineHeight.Count - 1);
                            xaml.Append("</List>");
                            xaml.Append(CurrentParagraph());
                            ReopenTags(recentlyClosed);
                            isInList = false;
                        }
                        else
                        {
                            string numbered = tag == "olist" ? "MarkerStyle=\"Decimal\"" : "";

                            recentlyClosed = CloseAllOpenTags();
                            xaml.Append("</Paragraph>\n");
                            xaml.Append($"<List Foreground=\"#acb2b8\" {numbered}>");
                            currentLineHeight.Add("22px");
                            isInList = true;
                        }
                        continue;
                    }

                    if (tag == "h1" || tag == "h2" || tag == "h3")
                    {
                        // #5AA9D6, 18
                        int index = int.Parse(tag[1].ToString());
                        string fontsize = HeaderSizes[index];
                        string lineheight = LineHeightSizes[index];

                        currentForeground.Add("#5AA9D6");
                        currentFontFamily.Add("{StaticResource MotivaSansRegular}");
                        currentFontSize.Add(fontsize);
                        currentLineHeight.Add(lineheight);
                        currentFontWeight.Add("500");
                        currentFontStretch.Add("Normal");

                        recentlyClosed = CloseAllOpenTags();
                        xaml.Append("</Paragraph>");
                        xaml.Append(CurrentParagraph());
                        ReopenTags(recentlyClosed);
                        continue;
                    }

                    if (tag.StartsWith('/'))
                    {
                        if (tag[1..] == "h1" || tag[1..] == "h2" || tag[1..] == "h3")
                        {
                            currentForeground.RemoveAt(currentForeground.Count - 1);
                            currentFontSize.RemoveAt(currentFontSize.Count - 1);
                            currentLineHeight.RemoveAt(currentLineHeight.Count - 1);
                            currentFontFamily.RemoveAt(currentFontFamily.Count - 1);
                            currentFontWeight.RemoveAt(currentFontWeight.Count - 1);
                            currentFontStretch.RemoveAt(currentFontStretch.Count - 1);

                            recentlyClosed = CloseAllOpenTags();
                            xaml.Append("</Paragraph>");
                            xaml.Append(CurrentParagraph());
                            ReopenTags(recentlyClosed);
                            continue;
                        }
                    }

                    if (tag.Contains("url"))
                    {
                        if (tag.StartsWith("url"))
                        {
                            waitingForUrl = true;
                            linkUrl = tag[(tag.IndexOf('=') + 1)..];
                            urlName = "";
                            continue;
                        }

                        if (tag.StartsWith("/url") && waitingForUrl)
                        {
                            waitingForUrl = false;
                            string decor = OpenUnderline > 0 ? "Underline" : "{x:Null}";
                            int ulinec = OpenUnderline;
                            if (ulinec > 0)
                            {
                                recentlyClosed = CloseAllOpenTags();
                                ReopenTags(recentlyClosed.Where(t => t != OpenTags.Underline).ToList());
                            }

                            xaml.Append($" <Hyperlink FontWeight=\"Bold\" NavigateUri=\"{ValidateUrl(linkUrl)}\" Foreground=\"#ebebeb\" TextDecorations=\"{decor}\">{urlName}</Hyperlink>");
                            if (ulinec > 0)
                            {
                                CloseAllOpenTags();
                                ReopenTags(recentlyClosed);
                            }
                            urlName = "";
                            linkUrl = "";
                            continue;
                        }
                    }

                    if (tag.Contains("img"))
                    {
                        if (tag.StartsWith("img"))
                        {
                            if (tag.Contains('='))
                            {
                                string imgurl = tag[(tag.IndexOf('=') + 1)..];
                                var imageBytes = GetImage(imgurl);
                                images.Add((imageBytes, imgurl));

                                var img = $"\n<InlineUIContainer><Grid/></InlineUIContainer>\n";
                                xaml.Append(img);
                            }
                            else
                            {
                                waitingForImage = true;
                                linkUrl = "";
                            }
                            continue;
                        }
                        else if (tag.StartsWith("/img"))
                        {
                            waitingForImage = false;
                            string imgurl = linkUrl.Trim();
                            var imageBytes = GetImage(imgurl);
                            images.Add((imageBytes, imgurl));

                            var img = $"\n<InlineUIContainer><Grid/></InlineUIContainer>\n";
                            xaml.Append(img);
                            continue;
                        }
                    }

                    if (isInTable && tag.Contains("tr"))
                    {
                        if (tag.StartsWith("tr"))
                        {
                            xaml.Append("<TableRow>");
                            continue;
                        }
                        else if (tag.StartsWith("/tr"))
                        {
                            xaml.Append("</TableRow>");
                            continue;
                        }
                    }

                    if (isInTable && (tag.Contains("td") || tag.Contains("th")))
                    {
                        if (tag.StartsWith("td") || tag.StartsWith("th"))
                        {
                            if (tag.StartsWith("th"))
                                TableColumnCount[^1]++;

                            currentFontSize.Add("14.01px");
                            xaml.Append("<TableCell Padding=\"5\" BorderBrush=\"#4D4D4D\" BorderThickness=\"1\">" + CurrentParagraph()[..^1] + " Margin=\"0,5,0,0\">");
                            continue;
                        }
                        else if (tag.StartsWith("/td") || tag.StartsWith("/th"))
                        {
                            currentFontSize.RemoveAt(currentFontSize.Count - 1);
                            xaml.Append("</Paragraph></TableCell>\n");
                            continue;
                        }
                    }

                    if (tag.Contains("table"))
                    {
                        if (tag.StartsWith("table"))
                        {
                            isInTable = true;
                            currentForeground.Add("#acb2b8");
                            currentFontSize.Add("14.01px");

                            recentlyClosed = CloseAllOpenTags();
                            xaml.Append("</Paragraph>\n");
                            xaml.Append("<Table><TableRowGroup>");
                            TableColumnCount.Add(1);
                            continue;
                        }
                        else if (tag.StartsWith("/table"))
                        {
                            isInTable = false;
                            currentForeground.RemoveAt(currentForeground.Count - 1);
                            currentFontSize.RemoveAt(currentFontSize.Count - 1);
                            xaml.Append("</TableRowGroup></Table>");
                            xaml.Append(CurrentParagraph());
                            ReopenTags(recentlyClosed);
                            continue;
                        }
                    }

                    Debug.WriteLine($"Skipping: {tag}");
                    continue;
                }
                workingTag.Append(c);
            }

            xaml.Append(FlowDocEnd);
            // Use StringReader and XmlReader to read the XAML.
            string _xaml = xaml.ToString();
            _xaml = RedundantLB().Replace(_xaml, "$1");
            _xaml = Images().Replace(_xaml, "$1");

            //Debug.WriteLine(sw.ElapsedMilliseconds);

            using StringReader stringReader = new(_xaml);
            using XmlReader xmlReader = XmlReader.Create(stringReader);
            // Parse the XAML to a FlowDocument
            FlowDocument doc = (FlowDocument)XamlReader.Load(xmlReader);
            doc.PageWidth = 635;
            
            if (doc.Blocks.FirstBlock is Paragraph pp && string.IsNullOrWhiteSpace(new TextRange(pp.ContentStart, pp.ContentEnd).Text))
                doc.Blocks.Remove(doc.Blocks.FirstBlock);
            
            doc.FontFamily = Application.Current.Resources["MotivaSansRegular"] as FontFamily;
            doc.FontSize = 14;
            doc.FontWeight = FontWeights.Normal;
            doc.FontStyle = FontStyles.Normal;
            doc.FontStretch = FontStretches.Normal;
            doc.Background = Brushes.Transparent;
            doc.Foreground = new SolidColorBrush(Color.FromRgb(172, 178, 184));

            RemoveEmptyBlocks(doc);
            AddHyperlinkEventHandlers(doc, TableColumnCount);
            PopulateImages(doc.Blocks, images);

            //Debug.WriteLine(sw.ElapsedMilliseconds);
            //Debug.WriteLine(XamlWriter.Save(doc));

            return doc;
        }

        private static string ValidateUrl(string url)
        {
            var uri = new Uri(url);
            //if (url.StartsWith("steam://"))
            //    return url[(url.IndexOf("http")+1)..];
            if (uri.Host.StartsWith("steamcommunity.com"))
                return "steam://openurl/" + url;
            if (!url.StartsWith("http") && !url.StartsWith("steam://"))
                return "https://" + url;

            return url;
        }

        private static int HandleOpenTag(string tag, StringBuilder xaml, string optional = "")
        {
            switch (tag)
            {
                case "b":
                    xaml.Append($"<Span FontFamily=\"{{StaticResource MotivaSansBold}}\" FontWeight=\"700\" {optional}>");
                    return 0;
                case "u":
                    xaml.Append("<Underline>");
                    return 1;
                case "i":
                    xaml.Append("<Italic>");
                    return 2;
                case "strike":
                    xaml.Append("<Span TextDecorations=\"Strikethrough\">");
                    return 3;
                case "spoiler":
                    xaml.Append("<Span Foreground=\"#ACB2B8\">");
                    return 4;
                case "hr":
                    xaml.Append("<BlockUIContainer><Rectangle Height=\"2\" Fill=\"#FFFFFF\" Margin=\"0,0,0,25\"/></BlockUIContainer>");
                    return 5;
                case "code":
                    xaml.Append(OpenCodeBlock);
                    return 6;
            }

            return -1;
        }

        private static int HandleCloseTag(string tag, StringBuilder xaml)
        {
            switch (tag)
            {
                case "/b":
                    xaml.Append("</Span>");
                    return 0;
                case "/u":
                    xaml.Append("</Underline>");
                    return 1;
                case "/i":
                    xaml.Append("</Italic>");
                    return 2;
                case "/strike":
                    xaml.Append("</Span>");
                    return 3;
                case "/spoiler":
                    xaml.Append("</Span>");
                    return 4;
                case "/hr":
                    // not a thing
                    return 5;
                case "/code":
                    xaml.Append(CloseCodeBlock);
                    return 6;
            }

            return -1;
        }

        private static void RemoveEmptyBlocks(FlowDocument document)
        {
            var blocksToRemove = new List<Block>();

            foreach (Block block in document.Blocks.ToArray())
            {
                if (BlockIsEmpty(block))
                {
                    blocksToRemove.Add(block);
                }
            }

            foreach (Block block in blocksToRemove)
            {
                document.Blocks.Remove(block);
            }
        }

        private static bool BlockIsEmpty(Block block)
        {
            // Check if the block is a Paragraph
            if (block is Paragraph paragraph)
            {
                // Remove the Paragraph if it only contains whitespaces or is empty
                string text = new TextRange(paragraph.ContentStart, paragraph.ContentEnd).Text;
                bool empty = string.IsNullOrWhiteSpace(text);
                bool any = paragraph.Inlines.OfType<LineBreak>().Any();
                if (paragraph.Inlines.Count > 0 && paragraph.Inlines.Last() is LineBreak lb)
                    paragraph.Inlines.Remove(lb);

                return empty && !any;
            }

            // Add more checks here if you want to handle other types of blocks (e.g., Section, List)

            return false;
        }

        private static void AddHyperlinkEventHandlers(FlowDocument document, List<int> tableColumns)
        {
            foreach (Block block in document.Blocks)
            {
                if (block is Paragraph paragraph)
                {
                    CheckLinkInlines(paragraph.Inlines);
                }
                if (block is Table tbl)
                {
                    tbl.Columns.Clear();
                    for (int i = 0; i < tableColumns[0]; i++)
                        tbl.Columns.Add(new TableColumn() { Width = new GridLength(0, GridUnitType.Auto) });

                    tbl.TextAlignment = TextAlignment.Left;
                    tbl.Padding = new Thickness(0);
                    tbl.Margin = new Thickness(0);
                    tbl.CellSpacing = 0;


                    AutoresizeColumns(tbl);
                    tableColumns.RemoveAt(0);
                }
                // Add more checks if your document structure contains other block types like Sections
            }
        }

        private static void AutoresizeColumns(Table table)
        {
            TableColumnCollection columns = table.Columns;
            TableRowCollection rows = table.RowGroups[0].Rows;
            TableCellCollection cells;
            TableRow row;
            TableCell cell;

            int columnCount = columns.Count;
            int rowCount = rows.Count;
            int cellCount = 0;

            double[] columnWidths = new double[columnCount];
            double columnWidth;

            // loop through all rows
            for (int r = 0; r < rowCount; r++)
            {
                row = rows[r];
                cells = row.Cells;
                cellCount = cells.Count;

                // loop through all cells in the row    
                for (int c = 0; c < columnCount && c < cellCount; c++)
                {
                    cell = cells[c];
                    columnWidth = GetDesiredWidth(new TextRange(cell.ContentStart, cell.ContentEnd)) + 19;

                    if (columnWidth > columnWidths[c])
                    {
                        columnWidths[c] = columnWidth;
                    }
                }
            }

            // set the columns width to the widest cell
            for (int c = 0; c < columnCount; c++)
            {
                columns[c].Width = new GridLength(columnWidths[c]);
            }
        }

        [Obsolete]
        private static double GetDesiredWidth(TextRange textRange)
        {
            return new FormattedText(
                textRange.Text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(
                    textRange.GetPropertyValue(TextElement.FontFamilyProperty) as FontFamily,
                    (FontStyle)textRange.GetPropertyValue(TextElement.FontStyleProperty),
                    (FontWeight)textRange.GetPropertyValue(TextElement.FontWeightProperty),
                    FontStretches.Normal),
                    (double)textRange.GetPropertyValue(TextElement.FontSizeProperty),
                Brushes.Black,
                null,
                TextFormattingMode.Display).Width;
        }

        private static void CheckLinkInlines(InlineCollection inlines)
        {
            var _inlines = inlines.ToArray();
            foreach (Inline inline in _inlines)
            {
                if (inline is Hyperlink hyperlink)
                {
                    hyperlink.Cursor = Cursors.Hand;
                    hyperlink.MouseEnter += (s, e) => hyperlink.Foreground = new SolidColorBrush(Color.FromRgb(92, 168, 190));
                    hyperlink.MouseLeave += (s, e) => hyperlink.Foreground = new SolidColorBrush(Color.FromRgb(235, 235, 235));
                    hyperlink.RequestNavigate += HyperlinkRequestNavigate;
                }

                if (inline is Bold bld)
                {
                    CheckLinkInlines(bld.Inlines);
                }
                else if (inline is Span spn)
                {
                    CheckLinkInlines(spn.Inlines);
                }
                else if (inline is Italic itl)
                {
                    CheckLinkInlines(itl.Inlines);
                }
                else if (inline is Underline undln)
                {
                    CheckLinkInlines(undln.Inlines);
                }
            }
        }

        private static void HyperlinkRequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

        private static byte[] GetImage(string imageUrl)
        {
            try
            {
                using HttpClient client = new();
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
                var response = client.GetAsync(imageUrl.Trim(), HttpCompletionOption.ResponseContentRead).GetAwaiter().GetResult();
                response.EnsureSuccessStatusCode();
                var imageStream = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();

                imageStream.Position = 0;
                byte[] buffer = new byte[imageStream.Length];
                imageStream.Read(buffer, 0, buffer.Length);
                return buffer;
            }
            catch (Exception ex)
            {
                // Handle exceptions (network errors, invalid image, etc.)
                Debug.WriteLine(ex.Message);
            }

            return null;
        }

        private static void PopulateImages(BlockCollection blocks, List<(byte[], string)> imageContents)
        {
            if (imageContents.Count <= 0)
                return;

            foreach (Block block in blocks)
            {
                if (block is Paragraph paragraph)
                {
                    foreach (Inline inline in paragraph.Inlines)
                    {
                        if (inline is InlineUIContainer uiContainer && uiContainer.Child is Grid grd && grd.Children.Count == 0)
                        {
                            var (data, _) = imageContents[0];
                            MemoryStream imageData = new(data);
                            Image image = new();
                            ImageBehavior.SetRepeatBehavior(image, RepeatBehavior.Forever);
                            ImageBehavior.SetAnimatedSource(image, ByteArrayToSource(imageData));

                            if (imageContents.Count > 0)
                            {
                                image.HorizontalAlignment = HorizontalAlignment.Left;
                                image.VerticalAlignment = VerticalAlignment.Top;
                                image.Stretch = Stretch.None;
                                image.Margin = new Thickness(0, 20, 0, 5);
                                imageContents.RemoveAt(0);
                                grd.Children.Add(image);
                            }
                        }
                    }
                }
                // Add more cases here if your FlowDocument contains other types of Blocks
            }
        }

        private static ImageSource ByteArrayToSource(Stream imageData)
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;  // To ensure the stream can be disposed after
            image.StreamSource = imageData;
            image.EndInit();
            image.Freeze();  // To make it cross-thread accessible
            return image;
        }

        [GeneratedRegex(@"(<\s*Paragraph[^>]*?>)\s*<\s*LineBreak\s*\/\s*>", RegexOptions.Multiline)]
        private static partial Regex RedundantLB();

        [GeneratedRegex(@"(<\s*Paragraph[^>]*?>)\s*<\s*LineBreak\s*\/\s*><\s*\/\s*Paragraph\s*>", RegexOptions.Multiline)]
        private static partial Regex SingleRedundantLB();

        [GeneratedRegex(@"<\s*LineBreak\s*\/\s*>\s*<\s*LineBreak\s*\/\s*>", RegexOptions.Multiline)]
        private static partial Regex DoubleLB();

        [GeneratedRegex(@"<\s*LineBreak\s*\/\s*>(<\s*\/\s*Paragraph\s*>)", RegexOptions.Multiline)]
        private static partial Regex SingleEndRedundantLB();

        [GeneratedRegex(@"<\s*LineBreak\s*\/\s*>\s*(<InlineUIContainer><Grid/></InlineUIContainer>\s*<\s*LineBreak\s*\/\s*>)", RegexOptions.Multiline)]
        private static partial Regex Images();
    }
}
