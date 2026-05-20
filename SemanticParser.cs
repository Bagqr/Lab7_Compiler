using System;
using System.Collections.Generic;
using System.Linq;

namespace WpfApp1
{
    public class SemanticParser
    {
        private const int TOKEN_FOR = 1;
        private const int TOKEN_IN = 2;
        private const int TOKEN_RANGE = 3;
        private const int TOKEN_PRINT = 4;
        private const int TOKEN_ID = 10;
        private const int TOKEN_NUM = 20;
        private const int TOKEN_ASSIGN = 30;
        private const int TOKEN_PLUS = 40;
        private const int TOKEN_MINUS = 41;
        private const int TOKEN_MUL = 42;
        private const int TOKEN_DIV = 43;
        private const int TOKEN_MOD = 44;
        private const int TOKEN_COLON = 60;
        private const int TOKEN_LPAREN = 62;
        private const int TOKEN_RPAREN = 63;
        private const int TOKEN_SEMIC = 64;
        private const int TOKEN_WS = 50;
        private const int TOKEN_NL = 51;

        private List<Lexem> _tokens;
        private int _pos;
        private Lexem _current;
        private SymbolTable _symTable;

        private List<SyntaxError> _syntaxErrors;
        private List<SyntaxError> _semanticErrors;

        public (ProgramNode Ast, List<SyntaxError> SyntaxErrors, List<SyntaxError> SemanticErrors) Analyze(List<Lexem> tokens)
        {
            _tokens = tokens.Where(t => t.Code != TOKEN_WS && t.Code != TOKEN_NL).ToList();
            _pos = 0;
            _syntaxErrors = new List<SyntaxError>();
            _semanticErrors = new List<SyntaxError>();
            _symTable = new SymbolTable();
            _current = NextToken();

            ProgramNode ast = ParseProgram();
            return (ast, _syntaxErrors, _semanticErrors);
        }

        private Lexem NextToken() => _pos < _tokens.Count ? _tokens[_pos++] : null;
        private Lexem Peek() => _pos < _tokens.Count ? _tokens[_pos] : null;

        private bool Match(int code)
        {
            if (_current != null && _current.Code == code)
            {
                _current = NextToken();
                return true;
            }
            return false;
        }

        private void AddSyntaxError(string lexeme, int line, int col, string desc)
        {
            _syntaxErrors.Add(new SyntaxError
            {
                ErrorFragment = lexeme,
                Location = $"строка {line}, позиция {col}",
                Description = desc,
                Line = line,
                Column = col
            });
        }


        private void AddSemanticError(string lexeme, int line, int col, string desc)
        {
            _semanticErrors.Add(new SyntaxError
            {
                ErrorFragment = lexeme,
                Location = $"строка {line}, позиция {col}",
                Description = desc,
                Line = line,
                Column = col
            });
        }


        private ProgramNode ParseProgram()
        {
            var node = new ProgramNode();
            while (_current != null)
            {
                var stmt = ParseStatement();
                if (stmt != null) node.Children.Add(stmt);
            }
            return node;
        }

        private AstNode ParseStatement()
        {
            if (_current == null) return null;

            switch (_current.Code)
            {
                case TOKEN_FOR: return ParseFor();
                case TOKEN_PRINT: return ParsePrint();
                case TOKEN_ID: return ParseAssignment();
                default:
                    AddSyntaxError(_current.Value, _current.Line, _current.StartPos,
                                   $"Неожиданная лексема '{_current.Value}'");
                 
                    while (_current != null && _current.Code != TOKEN_FOR && _current.Code != TOKEN_PRINT && _current.Code != TOKEN_ID)
                        _current = NextToken();
                    return null;
            }
        }


        private ForNode ParseFor()
        {
            if (!Match(TOKEN_FOR)) return null;
            string varName = null;

            if (_current?.Code == TOKEN_ID)
            {
                varName = _current.Value;
                int line = _current.Line, col = _current.StartPos;
                Match(TOKEN_ID);


                _symTable.PushScope();
                var err = _symTable.Declare(varName, "int", 0, line, col);
                if (err != null) _semanticErrors.Add(err);

            }
            else
            {
                AddSyntaxError(_current?.Value ?? "конец", _current?.Line ?? 1, _current?.StartPos ?? 1,
                               "Ожидался идентификатор после 'for'");
                return null;
            }

            if (!Match(TOKEN_IN)) { AddSyntaxError("", 0, 0, "Ожидалось 'in'"); return null; }
            if (!Match(TOKEN_RANGE)) { AddSyntaxError("", 0, 0, "Ожидалось 'range'"); return null; }
            if (!Match(TOKEN_LPAREN)) { AddSyntaxError("", 0, 0, "Ожидалось '('"); return null; }

            AstNode rangeExpr = ParseExpression();
            if (rangeExpr == null)
                AddSyntaxError("", _current?.Line ?? 1, _current?.StartPos ?? 1, "Ожидалось выражение внутри range");

            if (!Match(TOKEN_RPAREN)) { AddSyntaxError("", 0, 0, "Ожидалось ')'"); return null; }
            if (!Match(TOKEN_COLON)) { AddSyntaxError("", 0, 0, "Ожидалось ':'"); return null; }

            var forNode = new ForNode { VariableName = varName };
            var rangeNode = new RangeNode();
            if (rangeExpr != null) rangeNode.Children.Add(rangeExpr);
            forNode.Children.Add(rangeNode);


            while (_current != null && _current.Code != TOKEN_FOR && _current.Code != TOKEN_PRINT && _current.Code != TOKEN_ID)
            {
                AddSyntaxError(_current.Value, _current.Line, _current.StartPos, "Неожиданный токен в теле цикла");
                _current = NextToken();
            }

            if (_current != null && (_current.Code == TOKEN_PRINT || _current.Code == TOKEN_ID || _current.Code == TOKEN_FOR))
            {
                var bodyStmt = ParseStatement();
                if (bodyStmt != null) forNode.Children.Add(bodyStmt);
            }

            Match(TOKEN_SEMIC);
            _symTable.PopScope();
            return forNode;
        }


        private PrintNode ParsePrint()
        {
            if (!Match(TOKEN_PRINT)) return null;
            if (!Match(TOKEN_LPAREN)) { AddSyntaxError("", 0, 0, "Ожидалось '(' после print"); return null; }

            var expr = ParseExpression();
            if (expr == null)
                AddSyntaxError("", _current?.Line ?? 1, _current?.StartPos ?? 1, "Ожидалось выражение внутри print");

            if (!Match(TOKEN_RPAREN)) { AddSyntaxError("", 0, 0, "Ожидалось ')' после выражения"); return null; }
            Match(TOKEN_SEMIC); 

            var node = new PrintNode();
            if (expr != null) node.Children.Add(expr);
            return node;
        }


        private AssignmentNode ParseAssignment()
        {
            string varName = _current.Value;
            int line = _current.Line, col = _current.StartPos;
            Match(TOKEN_ID);


            var symbol = _symTable.Lookup(varName);
            if (symbol == null)
            {
                AddSemanticError(varName, line, col,
                    $"Использование необъявленного идентификатора \"{varName}\"");
            }

            if (!Match(TOKEN_ASSIGN))
            {
                AddSyntaxError("", 0, 0, "Ожидался оператор '='");
                return null;
            }

            var expr = ParseExpression();
            Match(TOKEN_SEMIC);


            if (symbol != null && expr != null)
            {
                string expectedType = symbol.Type;     
                string actualType = GetExpressionType(expr);
                if (expectedType != actualType)
                {
                    AddSemanticError(varName, line, col,
                        $"Несовместимость типов: переменная '{varName}' типа '{expectedType}', " +
                        $"присваивается значение типа '{actualType}'");
                }
            }

            var node = new AssignmentNode { VariableName = varName };
            if (expr != null) node.Children.Add(expr);
            return node;
        }

        private AstNode ParseExpression() => ParseAddition();
        private AstNode ParseAddition()
        {
            var left = ParseMultiplication();
            while (_current != null && (_current.Code == TOKEN_PLUS || _current.Code == TOKEN_MINUS))
            {
                string op = _current.Value;
                Match(_current.Code);
                var right = ParseMultiplication();
                var bin = new BinaryOpNode { Operator = op };
                if (left != null) bin.Children.Add(left);
                if (right != null) bin.Children.Add(right);
                left = bin;
            }
            return left;
        }

        private AstNode ParseMultiplication()
        {
            var left = ParseFactor();
            while (_current != null && (_current.Code == TOKEN_MUL || _current.Code == TOKEN_DIV || _current.Code == TOKEN_MOD))
            {
                string op = _current.Value;
                Match(_current.Code);
                var right = ParseFactor();
                var bin = new BinaryOpNode { Operator = op };
                if (left != null) bin.Children.Add(left);
                if (right != null) bin.Children.Add(right);
                left = bin;
            }
            return left;
        }

        private AstNode ParseFactor()
        {
            if (_current == null) return null;

            if (_current.Code == TOKEN_LPAREN)
            {
                Match(TOKEN_LPAREN);
                var expr = ParseExpression();
                if (!Match(TOKEN_RPAREN)) AddSyntaxError("", 0, 0, "Ожидалась ')'");
                return expr;
            }
            else if (_current.Code == TOKEN_NUM)
            {
                if (!int.TryParse(_current.Value, out int val))
                {
                    AddSemanticError(_current.Value, _current.Line, _current.StartPos,
                        $"Целое число '{_current.Value}' выходит за допустимые пределы int");
                }
                var node = new IntLiteralNode { Value = val };
                Match(TOKEN_NUM);
                return node;
            }
            else if (_current.Code == TOKEN_ID)
            {
                string name = _current.Value;
                int line = _current.Line, col = _current.StartPos;
                Match(TOKEN_ID);

                var symbol = _symTable.Lookup(name);
                if (symbol == null)
                {
                    AddSemanticError(name, line, col,
                        $"Использование необъявленного идентификатора \"{name}\"");
                }
                return new IdentifierNode { Name = name };
            }
            else
            {
                AddSyntaxError(_current.Value, _current.Line, _current.StartPos,
                    $"Неожиданный токен '{_current.Value}' в выражении");
                _current = NextToken();
                return null;
            }
        }

        private string GetExpressionType(AstNode expr)
        {
            if (expr == null) return "unknown";
            switch (expr)
            {
                case IntLiteralNode _: return "int";
                case IdentifierNode id:
                    var sym = _symTable.Lookup(id.Name);
                    return sym?.Type ?? "unknown";
                case BinaryOpNode bin:

                    return "int";
                default:
                    return "unknown";
            }
        }


        private void CheckTypeCompatibility(string expectedType, string actualType, string fragment, int line, int col)
        {
            if (expectedType != actualType)
            {
                AddSemanticError(fragment, line, col,
                    $"Несовместимость типов: ожидается '{expectedType}', получено '{actualType}'");
            }
        }
    }
}