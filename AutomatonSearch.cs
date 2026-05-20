using System.Collections.Generic;
using System.Text;

namespace WpfApp1
{
    public static class AutomatonSearch
    {
        private enum State
        {
            Start,
            Hash,
            HashEnd,          
            DoubleQuote1,     
            DoubleQuote2,     
            InDoubleTriple,   
            CloseDouble1,     
            CloseDouble2,     
            DoubleEnd,        
            SingleQuote1,
            SingleQuote2,
            InSingleTriple,
            CloseSingle1,
            CloseSingle2,
            SingleEnd,
            Dead
        }

        public class SearchResults
        {
            public string FoundText { get; set; }
            public int Offset { get; set; }
            public int Length { get; set; }
            public int Line { get; set; }
            public int Column { get; set; }
            public string Position { get; set; }
        }

        public static List<SearchResults> FindTimeOccurrences(string text)
        {
            var results = new List<SearchResults>();
            if (string.IsNullOrEmpty(text)) return results;

            int line = 1, column = 1;
            int offset = 0;
            State state = State.Start;
            int startOffset = -1, startLine = 0, startColumn = 0;
            var buffer = new StringBuilder();

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                State nextState = Transition(state, c);

                if (state == State.Start && nextState != State.Dead)
                {
                    startOffset = offset;
                    startLine = line;
                    startColumn = column;
                    buffer.Clear();
                }

                if (nextState != State.Dead && nextState != State.HashEnd)
                {
                    buffer.Append(c);
                }

                if (nextState == State.HashEnd || nextState == State.DoubleEnd || nextState == State.SingleEnd)
                {
                    results.Add(new SearchResults
                    {
                        FoundText = buffer.ToString(),
                        Offset = startOffset,
                        Length = buffer.Length,
                        Line = startLine,
                        Column = startColumn,
                        Position = $"строка {startLine}, столбец {startColumn}"
                    });
                    nextState = State.Start;
                    buffer.Clear();
                }
                else if (nextState == State.Dead && state != State.Start)
                {
                    nextState = Transition(State.Start, c);
                    buffer.Clear();
                    if (nextState != State.Dead)
                    {
                        startOffset = offset;
                        startLine = line;
                        startColumn = column;
                        buffer.Append(c);
                    }
                }

                state = nextState;

                offset++;
                if (c == '\n')
                {
                    line++;
                    column = 1;
                }
                else
                {
                    column++;
                }
            }

            if (state == State.Hash)
            {
                results.Add(new SearchResults
                {
                    FoundText = buffer.ToString(),
                    Offset = startOffset,
                    Length = buffer.Length,
                    Line = startLine,
                    Column = startColumn,
                    Position = $"строка {startLine}, столбец {startColumn}"
                });
            }

            return results;
        }

        private static State Transition(State s, char c)
        {
            switch (s)
            {
                case State.Start:
                    if (c == '#') return State.Hash;
                    if (c == '"') return State.DoubleQuote1;
                    if (c == '\'') return State.SingleQuote1;
                    return State.Dead;

                case State.Hash:
                    if (c == '\n') return State.HashEnd;
                    return State.Hash;

                case State.DoubleQuote1:
                    return c == '"' ? State.DoubleQuote2 : State.Dead;

                case State.DoubleQuote2:
                    return c == '"' ? State.InDoubleTriple : State.Dead;

                case State.InDoubleTriple:
                    if (c == '"') return State.CloseDouble1;
                    return State.InDoubleTriple;   

                case State.CloseDouble1:           
                    if (c == '"') return State.CloseDouble2;
                    return State.InDoubleTriple;   

                case State.CloseDouble2:           
                    if (c == '"') return State.DoubleEnd;
                    return State.InDoubleTriple;  

                case State.SingleQuote1:
                    return c == '\'' ? State.SingleQuote2 : State.Dead;

                case State.SingleQuote2:
                    return c == '\'' ? State.InSingleTriple : State.Dead;

                case State.InSingleTriple:
                    if (c == '\'') return State.CloseSingle1;
                    return State.InSingleTriple;

                case State.CloseSingle1:
                    if (c == '\'') return State.CloseSingle2;
                    return State.InSingleTriple;

                case State.CloseSingle2:
                    if (c == '\'') return State.SingleEnd;
                    return State.InSingleTriple;

                default:
                    return State.Dead;
            }
        }
    }
}
