namespace RazorCX.FindMissingDrawings.Models
{
    public class DisplayOptions
    {
        public int Phase { get; set; }
        public int MainPart { get; set; }
        public string MaterialType { get; set; }
        public bool ZoomSelected { get; set; }
        public bool IncludeNonSteel { get; set; }
    }
}