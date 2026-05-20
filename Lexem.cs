namespace WpfApp1
{
    public class Lexem
    {
        public int Code { get; set; }         
        public string Type { get; set; }    
        public string Value { get; set; }     
        public int Line { get; set; }       
        public int StartPos { get; set; }     
        public int EndPos { get; set; }      
        public bool IsError { get; set; }     
        public string Location => $"строка {Line}, {StartPos}-{EndPos}";
    }
}