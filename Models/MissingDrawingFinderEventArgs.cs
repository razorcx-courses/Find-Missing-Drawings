using System;

namespace RazorCX.FindMissingDrawings.Models
{
    public class MissingDrawingFinderEventArgs : EventArgs
    {
        public PartSummary PartSummary { get; set; }
    }
}