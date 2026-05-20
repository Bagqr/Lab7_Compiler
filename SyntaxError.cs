using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1
{
    public class SyntaxError
    {
        public string ErrorFragment { get; set; }
        public string Location { get; set; }
        public string Description { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }
    }
}