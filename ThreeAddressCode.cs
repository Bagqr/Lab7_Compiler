namespace WpfApp1
{
    public enum IROp
    {
        Const,   
        Copy,    
        Add, Sub, Mul, Div,
        Print,   
        Label,
        Br,     
        Goto
    }

    public class IRInstruction
    {
        public IROp Op { get; set; }
        public string Dest { get; set; }      
        public string Arg1 { get; set; }
        public string Arg2 { get; set; }
        public int IntVal { get; set; }     
        public string LabelName { get; set; } 

        public override string ToString()
        {
            switch (Op)
            {
                case IROp.Const: return $"{Dest} = {IntVal}";
                case IROp.Copy: return $"{Dest} = {Arg1}";
                case IROp.Add: return $"{Dest} = {Arg1} + {Arg2}";
                case IROp.Sub: return $"{Dest} = {Arg1} - {Arg2}";
                case IROp.Mul: return $"{Dest} = {Arg1} * {Arg2}";
                case IROp.Div: return $"{Dest} = {Arg1} / {Arg2}";
                case IROp.Print: return $"print {Arg1}";
                case IROp.Label: return $"{LabelName}:";
                case IROp.Br: return $"if {Arg1} goto {Arg2}";
                case IROp.Goto: return $"goto {Arg1}";
                default: return Op.ToString();
            }
        }
    }
}