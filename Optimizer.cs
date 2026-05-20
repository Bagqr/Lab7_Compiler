using System.Collections.Generic;
using System.Linq;

namespace WpfApp1
{
    public static class Optimizer
    {
        public static List<IRInstruction> LoopUnrolling(List<IRInstruction> code)
        {
            var newCode = new List<IRInstruction>();
            int i = 0;
            while (i < code.Count)
            {

                if (i + 6 < code.Count &&
                    code[i].Op == IROp.Const && code[i].Dest == "i" && code[i].IntVal == 0 &&
                    code[i + 1].Op == IROp.Label && code[i + 1].LabelName == "L_start" &&
                    code[i + 2].Op == IROp.Br && code[i + 2].Arg1.StartsWith("i >= ") &&
                    code[i + 3].Op == IROp.Print && code[i + 3].Arg1 == "i" &&
                    code[i + 4].Op == IROp.Add && code[i + 4].Dest == "i" && code[i + 4].Arg1 == "i" && code[i + 4].Arg2 == "1" &&
                    code[i + 5].Op == IROp.Goto && code[i + 5].Arg1 == "L_start" &&
                    code[i + 6].Op == IROp.Label && code[i + 6].LabelName == "L_end")
                {
                    string cond = code[i + 2].Arg1;
                    string numStr = cond.Split(' ').Last();
                    if (int.TryParse(numStr, out int N) && N <= 10)
                    {
                        for (int val = 0; val < N; val++)
                        {
                            newCode.Add(new IRInstruction { Op = IROp.Print, Arg1 = val.ToString() });
                        }
                        i += 7; 
                        continue;
                    }
                }
                newCode.Add(code[i]);
                i++;
            }
            return newCode;
        }

        public static List<IRInstruction> RemoveDeadLabels(List<IRInstruction> code)
        {
            var usedLabels = new HashSet<string>();
            foreach (var instr in code)
            {
                if (instr.Op == IROp.Goto)
                    usedLabels.Add(instr.Arg1);
                else if (instr.Op == IROp.Br)
                {
                    usedLabels.Add(instr.Arg2);
                }
            }
            var newCode = new List<IRInstruction>();
            foreach (var instr in code)
            {
                if (instr.Op == IROp.Label && !usedLabels.Contains(instr.LabelName))
                    continue;
                newCode.Add(instr);
            }
            return newCode;
        }
    }
}