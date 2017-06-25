using System.ComponentModel.Composition;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Outlining;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

namespace SeeUsingsLater
{
    [Export(typeof(IWpfTextViewCreationListener))]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    [ContentType("CSharp")]
    internal sealed class SeeUsingsLaterTextViewCreationListener : IWpfTextViewCreationListener
    {
        [Import]
        internal IOutliningManagerService OutliningManagerService { get; set; }

        private IWpfTextView _textView;

        public void TextViewCreated(IWpfTextView textView)
        {
            if (OutliningManagerService == null || textView == null)
            {
                return;
            }

            IOutliningManager outliningManager = OutliningManagerService.GetOutliningManager(textView);

            if (outliningManager == null)
            {
                return;
            }

            outliningManager.RegionsChanged += OnRegionsChanged;
            _textView = textView;
        }

        private void OnRegionsChanged(object sender, RegionsChangedEventArgs regionsChangedEventArgs)
        {
            IOutliningManager outliningManager = sender as IOutliningManager;
            if (outliningManager != null && outliningManager.Enabled)
            {
                outliningManager.CollapseAll(regionsChangedEventArgs.AffectedSpan, Match);
            }
        }

        private bool Match(ICollapsible collapsible)
        {
            string extent = collapsible.CollapsedHintForm.ToString();
            string firstLine = GetFirstLine(extent);

            bool isUsingRegion = firstLine != null && Regex.IsMatch(firstLine, "using .*;");
            if (isUsingRegion)
            {
                return !CaretIsInExtent(collapsible.Extent);
            }

            return false;
        }

        private bool CaretIsInExtent(ITrackingSpan extent)
        {
            ITextSnapshot textSnapshot = _textView.TextSnapshot;
            int startLine = extent.GetStartPoint(textSnapshot).GetContainingLine().LineNumber;
            int endLine = extent.GetEndPoint(textSnapshot).GetContainingLine().LineNumber;
            int caretLine = _textView.Caret.Position.BufferPosition.GetContainingLine().LineNumber;

            return startLine <= caretLine && endLine + 1 >= caretLine;
        }

        private string GetFirstLine(string extent)
        {
            if (extent == null)
            {
                return null;
            }

            int index = extent.IndexOf("\n");

            if (index < 0)
            {
                return null;
            }

            return extent.Substring(0, index);
        }
    }
}
