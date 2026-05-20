using System.Collections.Generic;
using System.Text;

namespace WpfApp1
{
    public enum NodeKind
    {
        Program,
        ForLoop,
        Range,
        Print,
        Assignment,
        BinaryOp,
        Identifier,
        IntLiteral
    }

    public abstract class AstNode
    {
        public NodeKind Kind { get; protected set; }
        public List<AstNode> Children { get; } = new List<AstNode>();

        public string Print()
        {
            var sb = new StringBuilder();
            PrintInternal(this, "", true, sb);
            return sb.ToString();
        }

        private static void PrintInternal(AstNode node, string indent, bool isLast, StringBuilder sb)
        {
            sb.Append(indent);
            sb.Append(isLast ? "└── " : "├── ");

            switch (node)
            {
                case ProgramNode p:
                    sb.AppendLine("ProgramNode");
                    break;
                case ForNode f:
                    sb.Append("ForNode");
                    if (!string.IsNullOrEmpty(f.VariableName))
                        sb.Append($" (variable: \"{f.VariableName}\")");
                    sb.AppendLine();
                    break;
                case RangeNode r:
                    sb.AppendLine("RangeNode");
                    break;
                case PrintNode pn:
                    sb.AppendLine("PrintNode");
                    break;
                case AssignmentNode a:
                    sb.Append("AssignmentNode");
                    if (!string.IsNullOrEmpty(a.VariableName))
                        sb.Append($" (variable: \"{a.VariableName}\")");
                    sb.AppendLine();
                    break;
                case BinaryOpNode b:
                    sb.AppendLine($"BinaryOpNode (operator: '{b.Operator}')");
                    break;
                case IdentifierNode id:
                    sb.AppendLine($"IdentifierNode (name: \"{id.Name}\")");
                    break;
                case IntLiteralNode lit:
                    sb.AppendLine($"IntLiteralNode (value: {lit.Value})");
                    break;
                default:
                    sb.AppendLine("?");
                    break;
            }

            for (int i = 0; i < node.Children.Count; i++)
            {
                var child = node.Children[i];
                bool lastChild = (i == node.Children.Count - 1);
                string childIndent = indent + (isLast ? "    " : "│   ");
                PrintInternal(child, childIndent, lastChild, sb);
            }
        }
    }

    public class ProgramNode : AstNode
    {
        public ProgramNode() => Kind = NodeKind.Program;
    }

    public class ForNode : AstNode
    {
        public string VariableName { get; set; }
        public ForNode() => Kind = NodeKind.ForLoop;
    }

    public class RangeNode : AstNode
    {
        public RangeNode() => Kind = NodeKind.Range;
    }

    public class PrintNode : AstNode
    {
        public PrintNode() => Kind = NodeKind.Print;
    }

    public class AssignmentNode : AstNode
    {
        public string VariableName { get; set; }
        public AssignmentNode() => Kind = NodeKind.Assignment;
    }

    public class BinaryOpNode : AstNode
    {
        public string Operator { get; set; }
        public BinaryOpNode() => Kind = NodeKind.BinaryOp;
    }

    public class IdentifierNode : AstNode
    {
        public string Name { get; set; }
        public IdentifierNode() => Kind = NodeKind.Identifier;
    }

    public class IntLiteralNode : AstNode
    {
        public int Value { get; set; }
        public IntLiteralNode() => Kind = NodeKind.IntLiteral;
    }
}