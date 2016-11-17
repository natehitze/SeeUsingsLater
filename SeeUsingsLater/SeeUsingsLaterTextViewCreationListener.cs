using System.ComponentModel.Composition;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Outlining;
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

        private IOutliningManager OutliningManager { get; set; }

        public void TextViewCreated(IWpfTextView textView)
        {
            if (OutliningManagerService == null || textView == null)
            {
                return;
            }

            OutliningManager = OutliningManagerService.GetOutliningManager(textView);

            if (OutliningManager == null)
            {
                return;
            }

            OutliningManager.RegionsChanged += OnRegionsChanged;
        }

        private void OnRegionsChanged(object sender, RegionsChangedEventArgs regionsChangedEventArgs)
        {
            OutliningManager.CollapseAll(regionsChangedEventArgs.AffectedSpan, Match);
        }

        private bool Match(ICollapsible collapsible)
        {
            string extent = collapsible.CollapsedHintForm.ToString();
            string firstLine = GetFirstLine(extent);
            
            return firstLine != null && Regex.IsMatch(firstLine, "using .*;");
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
