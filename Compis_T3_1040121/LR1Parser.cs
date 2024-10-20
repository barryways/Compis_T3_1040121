using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compis_T3_1040121

{
    internal class Production
    {
        public string Left { get; }
        public List<string> Right { get; }

        public Production(string left, List<string> right)
        {
            Left = left;
            Right = right;
        }

        public override string ToString()
        {
            return $"{Left} → {string.Join(" ", Right)}";
        }
    }

    internal class LR1Item
    {
        public Production Production { get; }
        public int DotPosition { get; }
        public string Lookahead { get; }

        public LR1Item(Production production, int dotPosition, string lookahead)
        {
            Production = production;
            DotPosition = dotPosition;
            Lookahead = lookahead;
        }

        public bool IsComplete => DotPosition == Production.Right.Count;

        public override string ToString()
        {
            var rightWithDot = Production.Right.Take(DotPosition)
                                 .Concat(new[] { "." })
                                 .Concat(Production.Right.Skip(DotPosition));
            return $"{Production.Left} → {string.Join(" ", rightWithDot)}, {Lookahead}";
        }

        public override bool Equals(object obj)
        {
            if (obj is LR1Item other)
            {
                return Production.Left == other.Production.Left
                    && Production.Right.SequenceEqual(other.Production.Right)
                    && DotPosition == other.DotPosition
                    && Lookahead == other.Lookahead;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return (Production, DotPosition, Lookahead).GetHashCode();
        }
    }

    public class LR1Parser
    {
        private Dictionary<string, List<string>> _productions;
        private List<Production> _productionRules = new List<Production>();
        private Dictionary<int, Dictionary<string, string>> _actionTable = new Dictionary<int, Dictionary<string, string>>();
        private Dictionary<int, Dictionary<string, int>> _gotoTable = new Dictionary<int, Dictionary<string, int>>();

        private int stateCounter = 0;
        private List<HashSet<LR1Item>> states = new List<HashSet<LR1Item>>();

        public LR1Parser(Dictionary<string, List<string>> productions)
        {
            _productions = productions;
            ConvertProductions();
            BuildTables();
        }

        private void ConvertProductions()
        {
            foreach (var production in _productions)
            {
                foreach (var rule in production.Value)
                {
                    var symbols = rule.Split(' ').ToList();
                    _productionRules.Add(new Production(production.Key, symbols));
                }
            }
        }

        private void BuildTables()
        {
            // Generación de la tabla inicial
            GenerateInitialState();
            // Procesar todos los estados generados
            GenerateStatesAndTransitions();
        }

        private void GenerateInitialState()
        {
            // Agregar estado inicial con producción extendida <S'> → . <S>, $
            var startProduction = new Production("<S'>", new List<string> { _productionRules[0].Left });
            var initialItem = new LR1Item(startProduction, 0, "$");
            var startState = Closure(new HashSet<LR1Item> { initialItem });

            states.Add(startState);
        }

        private void GenerateStatesAndTransitions()
        {
            for (int i = 0; i < states.Count; i++)
            {
                var state = states[i];
                var transitions = new Dictionary<string, HashSet<LR1Item>>();

                // Generar las transiciones para cada símbolo
                foreach (var item in state)
                {
                    if (!item.IsComplete)
                    {
                        var symbol = item.Production.Right[item.DotPosition];

                        if (!transitions.ContainsKey(symbol))
                        {
                            transitions[symbol] = new HashSet<LR1Item>();
                        }

                        transitions[symbol].Add(new LR1Item(item.Production, item.DotPosition + 1, item.Lookahead));
                    }
                }

                // Procesar las transiciones
                foreach (var transition in transitions)
                {
                    var nextState = Closure(transition.Value);

                    int stateIndex = states.FindIndex(s => s.SetEquals(nextState));
                    if (stateIndex == -1)
                    {
                        stateIndex = states.Count;
                        states.Add(nextState);
                    }

                    if (_productions.ContainsKey(transition.Key))
                    {
                        if (!_gotoTable.ContainsKey(i))
                            _gotoTable[i] = new Dictionary<string, int>();

                        _gotoTable[i][transition.Key] = stateIndex;
                    }
                    else
                    {
                        if (!_actionTable.ContainsKey(i))
                            _actionTable[i] = new Dictionary<string, string>();

                        _actionTable[i][transition.Key] = $"s{stateIndex}";
                    }
                }
            }
        }

        private HashSet<LR1Item> Closure(HashSet<LR1Item> items)
        {
            var closureSet = new HashSet<LR1Item>(items);

            bool added;
            do
            {
                added = false;
                var newItems = new HashSet<LR1Item>();

                foreach (var item in closureSet)
                {
                    if (item.IsComplete) continue;

                    var symbol = item.Production.Right[item.DotPosition];
                    if (_productions.ContainsKey(symbol))
                    {
                        foreach (var prod in _productions[symbol])
                        {
                            var symbols = prod.Split(' ').ToList();
                            var newItem = new LR1Item(new Production(symbol, symbols), 0, item.Lookahead);

                            if (!closureSet.Contains(newItem))
                            {
                                newItems.Add(newItem);
                            }
                        }
                    }
                }

                if (newItems.Any())
                {
                    added = true;
                    closureSet.UnionWith(newItems);
                }

            } while (added);

            return closureSet;
        }

        public void PrintTables()
        {
            Console.WriteLine("Tabla ACTION:");
            foreach (var state in _actionTable)
            {
                Console.WriteLine($"Estado {state.Key}:");
                foreach (var entry in state.Value)
                {
                    Console.WriteLine($"  {entry.Key} -> {entry.Value}");
                }
            }

            Console.WriteLine("Tabla GOTO:");
            foreach (var state in _gotoTable)
            {
                Console.WriteLine($"Estado {state.Key}:");
                foreach (var entry in state.Value)
                {
                    Console.WriteLine($"  {entry.Key} -> {entry.Value}");
                }
            }
        }

        public void Parse(List<string> input)
        {
            Stack<int> states = new Stack<int>();
            Stack<string> symbols = new Stack<string>();
            states.Push(0);

            int index = 0;
            string lookahead = input[index];

            while (true)
            {
                int currentState = states.Peek();
                Console.WriteLine($"Estado actual: {currentState}, Símbolo de vista: {lookahead}");

                if (_actionTable.ContainsKey(currentState) && _actionTable[currentState].ContainsKey(lookahead))
                {
                    string action = _actionTable[currentState][lookahead];

                    if (action.StartsWith("s")) // shift
                    {
                        Console.WriteLine($"Acción: shift {action.Substring(1)}");
                        states.Push(int.Parse(action.Substring(1)));
                        symbols.Push(lookahead);
                        index++;
                        lookahead = input[index];
                    }
                    else if (action.StartsWith("r")) // reduce
                    {
                        // Aplicar reducción
                        Console.WriteLine($"Acción: reduce");
                    }
                    else if (action == "accept")
                    {
                        Console.WriteLine("Cadena aceptada.");
                        break;
                    }
                }
                else
                {
                    Console.WriteLine("Error de sintaxis.");
                    break;
                }
            }
        }
    }

}
