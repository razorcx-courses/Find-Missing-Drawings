using Tekla.Structures.Model;

namespace RazorCX.FindMissingDrawings.Models
{
    public class PartSummary
    {
        public int Id { get; set; }
        public int Phase { get; set; }
        public int MainPart { get; set; }
        public string Name { get; set; }
        public string Material { get; set; }
        public string Mark { get; set; }
        public Part Part { get; set; }
        public string Guid { get; set; }
    }
}