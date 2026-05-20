using System.Collections.Generic;

namespace WpfApp1
{

    public class SymbolInfo
    {
        public string Name { get; set; }
        public string Type { get; set; }   
        public int? Value { get; set; }    
        public int Line { get; set; }
        public int Column { get; set; }
    }


    public class SymbolTable
    {
        private readonly Stack<Dictionary<string, SymbolInfo>> _scopes = new Stack<Dictionary<string, SymbolInfo>>();

        public SymbolTable()
        {

            PushScope();
        }

        public void PushScope() => _scopes.Push(new Dictionary<string, SymbolInfo>());
        public void PopScope() => _scopes.Pop();

        public SyntaxError Declare(string name, string type, int value, int line, int col)
        {
            if (_scopes.Count == 0) PushScope();
            var current = _scopes.Peek();
            if (current.ContainsKey(name))
            {
                var prev = current[name];
                return new SyntaxError
                {
                    ErrorFragment = name,
                    Location = $"строка {line}, позиция {col}",
                    Description = $"Ошибка: идентификатор \"{name}\" уже объявлен ранее (строка {prev.Line})",
                    Line = line,
                    Column = col
                };
            }
            current[name] = new SymbolInfo
            {
                Name = name,
                Type = type,
                Value = null,
                Line = line,
                Column = col
            };
            return null;
        }

        public SymbolInfo Lookup(string name)
        {
            foreach (var scope in _scopes)
            {
                if (scope.TryGetValue(name, out var info))
                    return info;
            }
            return null;
        }
    }
}