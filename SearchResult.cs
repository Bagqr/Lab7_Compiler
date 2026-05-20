namespace WpfApp1
{
    public class SearchResult
    {
        public string FoundText { get; set; }
        public int Length { get; set; }

        public string Position { get; set; } 
        public int Offset { get; set; }      
        public int Line { get; set; }
        public int Column { get; set; }
    }
}