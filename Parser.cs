using System.Collections.Generic;
using System.Linq;

namespace WpfApp1
{
    public class Parser
    {
        private enum State
        {
            ExpectFor,
            ExpectId,
            ExpectIn,
            ExpectRange,
            ExpectLParen,
            ExpectNumber,
            ExpectRParen,
            ExpectColon,
            ExpectPrint,
            ExpectLParenPrint,
            ExpectIdPrint,
            ExpectRParenPrint,
            ExpectSemicolon,
            Accept
        }

        private List<Lexem> _tokens;
        private int _position;
        private Lexem _current;
        private List<SyntaxError> _errors;

        private const int TOKEN_FOR = 1;
        private const int TOKEN_IN = 2;
        private const int TOKEN_RANGE = 3;
        private const int TOKEN_PRINT = 4;
        private const int TOKEN_ID = 10;
        private const int TOKEN_NUM = 20;
        private const int TOKEN_COLON = 60;
        private const int TOKEN_SEMICOLON = 64;
        private const int TOKEN_LPAREN = 62;
        private const int TOKEN_RPAREN = 63;
        private const int TOKEN_WHITESPACE = 50;
        private const int TOKEN_NEWLINE = 51;
        private const int TOKEN_ERROR = 90;

        public List<SyntaxError> Parse(List<Lexem> tokens)
        {
            _tokens = tokens;
            _position = 0;
            _errors = new List<SyntaxError>();
            GetNextToken();

            if (_tokens.Count == 0)
            {
                AddError("", 1, 1, "ключевое слово 'for'", "конец строки", "Пустая строка");
                return _errors;
            }

            State state = State.ExpectFor;
            int safety = 0;

            while (state != State.Accept && safety < 10000)
            {
                safety++;
                if (MatchesState(state, _current))
                {
                    state = ConsumeToken(state);
                    continue;
                }

                AddStateError(state, _current);
                if (_current == null) break;
                Recover(ref state);
                if (_current == null) break;
            }

            if (_current != null)
            {
                AddError(_current.Value, _current.Line, _current.StartPos, "конец строки", _current.Value, "Лишние символы после завершения программы");
                while (_current != null)
                    GetNextToken();
            }

            return _errors;
        }

        private void GetNextToken()
        {
            if (_position < _tokens.Count)
            {
                _current = _tokens[_position++];
                while (_current != null && (_current.Code == TOKEN_WHITESPACE || _current.Code == TOKEN_NEWLINE))
                {
                    if (_position < _tokens.Count)
                        _current = _tokens[_position++];
                    else
                        _current = null;
                }
            }
            else
                _current = null;
        }

        private bool MatchesState(State state, Lexem token)
        {
            if (token == null)
                return state == State.Accept;

            if (token.Code == TOKEN_ERROR)
                return false;

            switch (state)
            {
                case State.ExpectFor: return token.Code == TOKEN_FOR;
                case State.ExpectId: return token.Code == TOKEN_ID;
                case State.ExpectIn: return token.Code == TOKEN_IN;
                case State.ExpectRange: return token.Code == TOKEN_RANGE;
                case State.ExpectLParen: return token.Code == TOKEN_LPAREN;
                case State.ExpectNumber: return token.Code == TOKEN_NUM;
                case State.ExpectRParen: return token.Code == TOKEN_RPAREN;
                case State.ExpectColon: return token.Code == TOKEN_COLON;
                case State.ExpectPrint: return token.Code == TOKEN_PRINT;
                case State.ExpectLParenPrint: return token.Code == TOKEN_LPAREN;
                case State.ExpectIdPrint: return token.Code == TOKEN_ID;
                case State.ExpectRParenPrint: return token.Code == TOKEN_RPAREN;
                case State.ExpectSemicolon: return token.Code == TOKEN_SEMICOLON;
                case State.Accept: return token == null;
                default: return false;
            }
        }

        private State ConsumeToken(State state)
        {
            GetNextToken();
            switch (state)
            {
                case State.ExpectFor: return State.ExpectId;
                case State.ExpectId: return State.ExpectIn;
                case State.ExpectIn: return State.ExpectRange;
                case State.ExpectRange: return State.ExpectLParen;
                case State.ExpectLParen: return State.ExpectNumber;
                case State.ExpectNumber: return State.ExpectRParen;
                case State.ExpectRParen: return State.ExpectColon;
                case State.ExpectColon: return State.ExpectPrint;
                case State.ExpectPrint: return State.ExpectLParenPrint;
                case State.ExpectLParenPrint: return State.ExpectIdPrint;
                case State.ExpectIdPrint: return State.ExpectRParenPrint;
                case State.ExpectRParenPrint: return State.ExpectSemicolon;
                case State.ExpectSemicolon: return State.Accept;
                default: return State.Accept;
            }
        }

        private void AddStateError(State state, Lexem token)
        {
            string found = token?.Value ?? "конец строки";
            int line = token?.Line ?? 1;
            int pos = token?.StartPos ?? 1;

            switch (state)
            {
                case State.ExpectFor:
                    AddError(found, line, pos, "'for'", found, "Программа должна начинаться с 'for'");
                    break;
                case State.ExpectId:
                    AddError(found, line, pos, "идентификатор", found, "Ожидается идентификатор после 'for'");
                    break;
                case State.ExpectIn:
                    AddError(found, line, pos, "'in'", found, "Ожидается 'in' после идентификатора");
                    break;
                case State.ExpectRange:
                    AddError(found, line, pos, "'range'", found, "Ожидается 'range' после 'in'");
                    break;
                case State.ExpectLParen:
                    AddError(found, line, pos, "'('", found, "Ожидается '(' после 'range'");
                    break;
                case State.ExpectNumber:
                    AddError(found, line, pos, "целое число", found, "Ожидается число в скобках range");
                    break;
                case State.ExpectRParen:
                    AddError(found, line, pos, "')'", found, "Ожидается ')' после числа");
                    break;
                case State.ExpectColon:
                    AddError(found, line, pos, "':'", found, "Ожидается ':' после ')'");
                    break;
                case State.ExpectPrint:
                    AddError(found, line, pos, "'print'", found, "Ожидается 'print' после ':'");
                    break;
                case State.ExpectLParenPrint:
                    AddError(found, line, pos, "'('", found, "Ожидается '(' после 'print'");
                    break;
                case State.ExpectIdPrint:
                    AddError(found, line, pos, "идентификатор", found, "Ожидается идентификатор внутри print");
                    break;
                case State.ExpectRParenPrint:
                    AddError(found, line, pos, "')'", found, "Ожидается ')' после идентификатора в print");
                    break;
                case State.ExpectSemicolon:
                    AddError(found, line, pos, "';'", found, "Ожидается ';' после ')'");
                    break;
            }
        }

        private void Recover(ref State state)
        {
            if (state == State.ExpectFor)
            {
                if (_current != null)
                {
                    GetNextToken();
                    state = State.ExpectId;
                }
                else
                {
                    state = State.Accept;
                }
                return;
            }

            if (state == State.ExpectRange)
            {
                if (_current != null && _current.Code == TOKEN_LPAREN)
                {
                    GetNextToken();
                    state = State.ExpectNumber;
                }
                else
                {
                    if (_current != null)
                        GetNextToken();
                    while (_current != null && _current.Code != TOKEN_LPAREN)
                        GetNextToken();
                    if (_current != null && _current.Code == TOKEN_LPAREN)
                        GetNextToken();
                    state = State.ExpectNumber;
                }
                return;
            }

            int[] syncTokens = GetSyncTokens(state);
            while (_current != null && !syncTokens.Contains(_current.Code))
            {
                GetNextToken();
            }

            if (_current != null)
                state = DetermineStateAfterRecovery(state, _current.Code);
            else
                state = State.Accept;
        }

        private int[] GetSyncTokens(State state)
        {
            switch (state)
            {
                case State.ExpectFor:
                    return new int[] { TOKEN_FOR, TOKEN_ID, TOKEN_IN, TOKEN_RANGE, TOKEN_PRINT };
                case State.ExpectId:
                    return new int[] { TOKEN_ID, TOKEN_IN, TOKEN_RANGE, TOKEN_COLON, TOKEN_PRINT };
                case State.ExpectIn:
                    return new int[] { TOKEN_IN, TOKEN_RANGE, TOKEN_LPAREN, TOKEN_COLON, TOKEN_PRINT };
                case State.ExpectRange:
                    return new int[] { TOKEN_RANGE, TOKEN_LPAREN, TOKEN_NUM, TOKEN_COLON, TOKEN_PRINT };
                case State.ExpectLParen:
                    return new int[] { TOKEN_LPAREN, TOKEN_NUM, TOKEN_RPAREN, TOKEN_COLON, TOKEN_PRINT };
                case State.ExpectNumber:
                    return new int[] { TOKEN_NUM, TOKEN_RPAREN, TOKEN_COLON, TOKEN_PRINT };
                case State.ExpectRParen:
                    return new int[] { TOKEN_RPAREN, TOKEN_COLON, TOKEN_PRINT };
                case State.ExpectColon:
                    return new int[] { TOKEN_COLON, TOKEN_PRINT };
                case State.ExpectPrint:
                    return new int[] { TOKEN_PRINT, TOKEN_LPAREN, TOKEN_ID, TOKEN_RPAREN, TOKEN_SEMICOLON };
                case State.ExpectLParenPrint:
                    return new int[] { TOKEN_LPAREN, TOKEN_ID, TOKEN_RPAREN, TOKEN_SEMICOLON };
                case State.ExpectIdPrint:
                    return new int[] { TOKEN_ID, TOKEN_RPAREN, TOKEN_SEMICOLON };
                case State.ExpectRParenPrint:
                    return new int[] { TOKEN_RPAREN, TOKEN_SEMICOLON };
                case State.ExpectSemicolon:
                    return new int[] { TOKEN_SEMICOLON };
                default:
                    return new int[0];
            }
        }

        private State DetermineStateAfterRecovery(State oldState, int tokenCode)
        {
            switch (tokenCode)
            {
                case TOKEN_FOR: return State.ExpectFor;
                case TOKEN_ID: return (oldState == State.ExpectFor || oldState == State.ExpectId) ? State.ExpectId : State.ExpectIdPrint;
                case TOKEN_IN: return State.ExpectIn;
                case TOKEN_RANGE: return State.ExpectRange;
                case TOKEN_LPAREN: return (oldState == State.ExpectLParen || oldState == State.ExpectRange) ? State.ExpectLParen : State.ExpectLParenPrint;
                case TOKEN_NUM: return State.ExpectNumber;
                case TOKEN_RPAREN: return (oldState == State.ExpectRParen) ? State.ExpectRParen : State.ExpectRParenPrint;
                case TOKEN_COLON: return State.ExpectColon;
                case TOKEN_PRINT: return State.ExpectPrint;
                case TOKEN_SEMICOLON: return State.ExpectSemicolon;
                default: return oldState;
            }
        }

        private void AddError(string invalidFragment, int line, int position, string expected, string found, string description)
        {
            if (_errors.Any(e => e.Line == line && e.Column == position && e.Description == description))
                return;

            _errors.Add(new SyntaxError
            {
                ErrorFragment = invalidFragment,
                Location = $"строка {line}, позиция {position}",
                Description = description,
                Line = line,
                Column = position
            });
        }
    }
}
