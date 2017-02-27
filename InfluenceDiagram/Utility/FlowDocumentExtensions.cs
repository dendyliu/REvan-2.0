using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace InfluenceDiagram.Utility
{
    /** FlowDocument extensions to calculate actual width
     *  source: http://social.msdn.microsoft.com/Forums/vstudio/en-US/6cd49173-b06d-4749-85aa-f6ab46c7d4af/wpf-rich-text-box-width-size-adjust-to-text?forum=wpf
     **/
    public static class FlowDocumentExtensions
    {
        /** customized code **/

        private static IEnumerable<TextElement> GetRunsAndInlinecontainers(FlowDocument doc)
        {
            for (TextPointer position = doc.ContentStart;
              position != null && position.CompareTo(doc.ContentEnd) <= 0;
              position = position.GetNextContextPosition(LogicalDirection.Forward))
            {
                TextPointer nextPosition = position.GetNextContextPosition(LogicalDirection.Forward);
                if (position.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.ElementEnd)
                {
                    if (position.Parent is Run)
                    {
                        yield return position.Parent as Run;
                    }
                    else if (position.Parent is InlineUIContainer)
                    {
                        yield return position.Parent as InlineUIContainer;
                    }
                }
            }
        }

        public static int ConvertTextPosition(this FlowDocument doc, TextPointer p)
        {
            int pos = 0;
            for (TextPointer position = doc.ContentStart;
             position != null && position.CompareTo(p) <= 0;
             position = position.GetNextContextPosition(LogicalDirection.Forward))
            {
                if (position.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.ElementEnd)
                {
                    // 
                }
                else
                {
                    TextPointer nextPosition = position.GetNextContextPosition(LogicalDirection.Forward);
                    if (position.Parent is Run)
                    {
                        Run run = position.Parent as Run;
                        TextPointer p1 = run.ContentStart;
                        TextPointer p2 = (nextPosition.CompareTo(p) <= 0) ? nextPosition : p;
                        int x2 = -p2.GetOffsetToPosition(p1);
                        pos += x2;
                    }
                    else if (position.Parent is InlineUIContainer)
                    {
                        InlineUIContainer inline = position.Parent as InlineUIContainer;
                        if (inline.Child is TextBox)
                        {
                            string variable = (inline.Child as TextBox).Tag as String;
                            pos += variable.Length + 2; // +2 for brackets
                        }
                    }
                }
            }
            return pos;
        }

        public static TextPointer InsertTextAt(this FlowDocument doc, string text, TextPointer p)
        {
            if (p.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.ElementStart)
            {
                p = p.GetNextContextPosition(LogicalDirection.Backward);
            }
            if (p.Parent is FlowDocument)
            {
                Run run = new Run(text);
                Paragraph paragraph = (doc.Blocks.FirstBlock as Paragraph);
                paragraph.Inlines.InsertBefore(paragraph.Inlines.FirstInline, run);
                return run.ContentEnd;
            }
            else if (p.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.ElementEnd)
            {
                Run run = new Run(text);
                if (p.Parent is Inline)
                {
                    Inline inline = p.Parent as Inline;
                    (inline.Parent as Paragraph).Inlines.InsertAfter(inline, run);
                }
                else if (p.Parent is Paragraph)
                {
                    (p.Parent as Paragraph).Inlines.Add(run);
                }
                else
                {
                    throw new Exception("not handled");
                }
                return run.ContentEnd;
            }
            else
            {
                if (p.Parent is Run)
                {
                    Run run = p.Parent as Run;
                    Paragraph paragraph = run.Parent as Paragraph;
                    // break up Run
                    int mid = -p.GetOffsetToPosition(run.ContentStart);                    
                    run.Text = run.Text.Insert(mid, text);

                    return run.ContentStart.GetPositionAtOffset(mid + text.Length);

                    /*Run run1 = new Run(run.Text.Substring(0, mid));
                    Run run2 = new Run(run.Text.Substring(mid));
                    paragraph.Inlines.InsertBefore(run, run1);
                    paragraph.Inlines.InsertBefore(run, container);
                    paragraph.Inlines.InsertBefore(run, run2);
                    paragraph.Inlines.Remove(run);*/
                }
                else
                {
                    throw new Exception("not handled");
                }
            }
        }

        public static void InsertInlineAt(this FlowDocument doc, InlineUIContainer container, TextPointer p)
        {
            if (p.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.ElementStart)
            {
                p = p.GetNextContextPosition(LogicalDirection.Backward);
            }
            if (p.Parent is FlowDocument)
            {
                Paragraph paragraph = (doc.Blocks.FirstBlock as Paragraph);
                paragraph.Inlines.InsertBefore(paragraph.Inlines.FirstInline, container);
            }
            else if (p.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.ElementEnd)
            {
                if (p.Parent is Inline)
                {
                    Inline inline = p.Parent as Inline;
                    (inline.Parent as Paragraph).Inlines.InsertAfter(inline, container);
                }
                else if (p.Parent is Paragraph)
                {
                    (p.Parent as Paragraph).Inlines.Add(container);
                }
                else
                {
                    throw new Exception("not handled");
                }
            }
            else
            {
                if (p.Parent is Run)
                {
                    Run run = p.Parent as Run;
                    Paragraph paragraph = run.Parent as Paragraph;
                    // break up Run
                    int mid = -p.GetOffsetToPosition(run.ContentStart);
                    Run run1 = new Run(run.Text.Substring(0, mid));
                    Run run2 = new Run(run.Text.Substring(mid));
                    paragraph.Inlines.InsertBefore(run, run1);
                    paragraph.Inlines.InsertBefore(run, container);
                    paragraph.Inlines.InsertBefore(run, run2);
                    paragraph.Inlines.Remove(run);
                }
                else
                {
                    throw new Exception("not handled");
                }
            }
        }

        public static void DeleteRunsAndInlinecontainers(this FlowDocument doc, TextPointer start = null, TextPointer end = null)
        {
            for (TextPointer position = start;
              position != null && position.CompareTo(end) <= 0;
              position = position.GetNextContextPosition(LogicalDirection.Forward))
            {
                if (position.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.ElementEnd)
                {
                    // 
                }
                else
                {
                    TextPointer nextPosition = position.GetNextContextPosition(LogicalDirection.Forward);
                    if (position.Parent is Run)
                    {
                        Run run = position.Parent as Run;
                        TextPointer p1 = (position.CompareTo(run.ContentStart) >= 0) ? position : run.ContentStart;
                        TextPointer p2 = (nextPosition.CompareTo(end) <= 0) ? nextPosition : end;
                        int x2 = -p2.GetOffsetToPosition(p1);
                        p1.DeleteTextInRun(x2);
                    }
                    else if (position.Parent is InlineUIContainer)
                    {
                        InlineUIContainer inline = position.Parent as InlineUIContainer;
                        (inline.Parent as Paragraph).Inlines.Remove(inline);
                    }
                }
            }
        }

        private static IEnumerable<string> GetRunsAndInlinecontainersString(FlowDocument doc, TextPointer start = null, TextPointer end = null)
        {
            for (TextPointer position = start;
              position != null && position.CompareTo(end) <= 0;
              position = position.GetNextContextPosition(LogicalDirection.Forward))
            {
                if (position.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.ElementEnd)
                {
                    // 
                }
                else
                {
                    TextPointer nextPosition = position.GetNextContextPosition(LogicalDirection.Forward);
                    if (position.Parent is Run)
                    {
                        Run run = position.Parent as Run;
                        TextPointer p1 = (position.CompareTo(run.ContentStart) >= 0) ? position : run.ContentStart;
                        TextPointer p2 = (nextPosition.CompareTo(end) <= 0) ? nextPosition : end;
                        int x1 = -position.GetOffsetToPosition(run.ContentStart);
                        int x2 = -p2.GetOffsetToPosition(p1);
                        yield return run.Text.Substring(x1, x2);
                    }
                    else if (position.Parent is InlineUIContainer)
                    {
                        InlineUIContainer inline = position.Parent as InlineUIContainer;
                        if (inline.Child is TextBox)
                        {
                            string variable = (inline.Child as TextBox).Tag as String;
                            yield return "[" + variable + "]";
                        }
                    }
                }
            }
        }

        public static string GetTextWithVariable(this FlowDocument doc, TextPointer start = null, TextPointer end = null)
        {
            if (start == null) start = doc.ContentStart;
            if (end == null) end = doc.ContentEnd;
            StringBuilder sb = new StringBuilder();

            foreach (string str in GetRunsAndInlinecontainersString(doc, start, end))
            {
                sb.Append(str);
            }
            return sb.ToString();
        }

        public static double CalculateWidth(this FlowDocument doc)
        {
            if (doc == null)
            {
                throw new ArgumentNullException("doc");
            }

            double width = 0;

            foreach (TextElement el in GetRunsAndInlinecontainers(doc))
            {
                if (el is Run)
                {
                    Run run = el as Run;
                    int count = run.Text.Length;

                    FormattedText output = new FormattedText(
                      run.Text,
                      CultureInfo.CurrentCulture,
                      doc.FlowDirection,
                      new Typeface(doc.FontFamily, doc.FontStyle, doc.FontWeight, doc.FontStretch),
                      doc.FontSize,
                      doc.Foreground);
                    int offset = 0;
                    output.SetFontFamily(run.FontFamily, offset, count);
                    output.SetFontStyle(run.FontStyle, offset, count);
                    output.SetFontWeight(run.FontWeight, offset, count);
                    output.SetFontSize(run.FontSize, offset, count);
                    output.SetForegroundBrush(run.Foreground, offset, count);
                    output.SetFontStretch(run.FontStretch, offset, count);
                    output.SetTextDecorations(run.TextDecorations, offset, count);

                    width += output.WidthIncludingTrailingWhitespace;
                }
                else if (el is InlineUIContainer)
                {
                    InlineUIContainer container = el as InlineUIContainer;
                    width += container.Child.RenderSize.Width;
                }
            }

            return width;
        }

        /** the code from website starts from here **/

        private static IEnumerable<TextElement> GetRunsAndParagraphs(FlowDocument doc)
        {
            for (TextPointer position = doc.ContentStart;
              position != null && position.CompareTo(doc.ContentEnd) <= 0;
              position = position.GetNextContextPosition(LogicalDirection.Forward))
            {
                if (position.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.ElementEnd)
                {
                    Run run = position.Parent as Run;

                    if (run != null)
                    {
                        yield return run;
                    }
                    else
                    {
                        Paragraph para = position.Parent as Paragraph;

                        if (para != null)
                        {
                            yield return para;
                        }
                    }
                }
            }
        }

        public static FormattedText GetFormattedText(this FlowDocument doc)
        {
            if (doc == null)
            {
                throw new ArgumentNullException("doc");
            }

            FormattedText output = new FormattedText(
              GetText(doc),
              CultureInfo.CurrentCulture,
              doc.FlowDirection,
              new Typeface(doc.FontFamily, doc.FontStyle, doc.FontWeight, doc.FontStretch),
              doc.FontSize,
              doc.Foreground);

            int offset = 0;

            foreach (TextElement el in GetRunsAndParagraphs(doc))
            {
                Run run = el as Run;

                if (run != null)
                {
                    int count = run.Text.Length;

                    output.SetFontFamily(run.FontFamily, offset, count);
                    output.SetFontStyle(run.FontStyle, offset, count);
                    output.SetFontWeight(run.FontWeight, offset, count);
                    output.SetFontSize(run.FontSize, offset, count);
                    output.SetForegroundBrush(run.Foreground, offset, count);
                    output.SetFontStretch(run.FontStretch, offset, count);
                    output.SetTextDecorations(run.TextDecorations, offset, count);

                    offset += count;
                }
                else
                {
                    offset += Environment.NewLine.Length;
                }
            }

            return output;
        }

        private static string GetText(FlowDocument doc)
        {
            StringBuilder sb = new StringBuilder();

            foreach (TextElement el in GetRunsAndParagraphs(doc))
            {
                Run run = el as Run;
                sb.Append(run == null ? Environment.NewLine : run.Text);
            }
            return sb.ToString();
        }
    }
}
