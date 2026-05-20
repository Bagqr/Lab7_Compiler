using System.Collections.Generic;
using System.Linq;

namespace WpfApp1
{
    public class IRGenerator
    {
        private List<IRInstruction> _code;
        private int _tempCounter;
        private string NewTemp() => $"t{_tempCounter++}";

        public List<IRInstruction> Generate(ForNode forNode)
        {
            _code = new List<IRInstruction>();
            _tempCounter = 0;

            var rangeNode = forNode.Children.OfType<RangeNode>().FirstOrDefault();
            var limitExpr = rangeNode?.Children.FirstOrDefault() as IntLiteralNode;
            if (limitExpr == null) throw new System.Exception("Ожидается константа в range");

            string varName = forNode.VariableName;
            int limit = limitExpr.Value;

            _code.Add(new IRInstruction { Op = IROp.Const, Dest = varName, IntVal = 0 });

            string startLabel = "L_start";
            string endLabel = "L_end";

            _code.Add(new IRInstruction { Op = IROp.Label, LabelName = startLabel });
            _code.Add(new IRInstruction { Op = IROp.Br, Arg1 = $"{varName} >= {limit}", Arg2 = endLabel });

            var body = forNode.Children.FirstOrDefault(c => !(c is RangeNode));
            if (body != null)
                GenerateStatement(body);

            _code.Add(new IRInstruction { Op = IROp.Add, Dest = varName, Arg1 = varName, Arg2 = "1" });
            _code.Add(new IRInstruction { Op = IROp.Goto, Arg1 = startLabel });
            _code.Add(new IRInstruction { Op = IROp.Label, LabelName = endLabel });

            return _code;
        }

        private void GenerateStatement(AstNode node)
        {
            if (node is PrintNode print)
            {
                var arg = print.Children.FirstOrDefault();
                if (arg is IdentifierNode id)
                {
                    _code.Add(new IRInstruction { Op = IROp.Print, Arg1 = id.Name });
                }
            }
            else
            {
                foreach (var child in node.Children)
                    GenerateStatement(child);
            }
        }
    }
}