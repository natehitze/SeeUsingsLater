using System.ComponentModel.Composition;
using System.Diagnostics;
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
        private bool _firstLoad;

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
            _firstLoad = true;
        }

        private void OnRegionsChanged(object sender, RegionsChangedEventArgs regionsChangedEventArgs)
        {
            IOutliningManager outliningManager = sender as IOutliningManager;
            if (outliningManager != null && outliningManager.Enabled)
            {
                // Collapses all of the regions within the span where Match() returns true.
                outliningManager.CollapseAll(regionsChangedEventArgs.AffectedSpan, Match);
            }
        }

        // Returns true when the collapsible should be collapsed.
        private bool Match(ICollapsible collapsible)
        {
            string data = collapsible?.CollapsedHintForm?.ToString();
            string firstLine = GetFirstLine(data);

            bool isUsingRegion = firstLine != null && Regex.IsMatch(firstLine, "using .*;");
            if (isUsingRegion && collapsible?.Extent != null)
            {
                // We only want to collapse if the carent is not within the region or if this is the first load of the document
                bool collapse = !CaretIsInExtent(collapsible.Extent) || _firstLoad;
                _firstLoad = false;

                return collapse;
            }

            return false;
        }

        /// <summary>
        /// Checks if the caret is within the extent by checking if it is on a line between the extent's first line and the line after the extent's last line.
        /// </summary>
        /// <param name="extent"></param>
        /// <returns></returns>
        private bool CaretIsInExtent(ITrackingSpan extent)
        {
            ITextSnapshot textSnapshot = _textView.TextSnapshot;
            int startLine = extent.GetStartPoint(textSnapshot).GetContainingLine().LineNumber;
            int endLine = extent.GetEndPoint(textSnapshot).GetContainingLine().LineNumber;
            int caretLine = _textView.Caret.Position.BufferPosition.GetContainingLine().LineNumber;

            return startLine <= caretLine && endLine + 1 >= caretLine;
        }

        /// <summary>
        /// Returns the first line within the data
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private string GetFirstLine(string data)
        {
            if (data == null)
            {
                return null;
            }

            int index = data.IndexOf("\n");

            if (index < 0)
            {
                return null;
            }

            return data.Substring(0, index);
        }
    }
}
