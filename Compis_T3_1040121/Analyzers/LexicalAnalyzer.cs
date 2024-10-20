using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Compis_T3_1040121.Analyzers
{
    public class LexicalAnalyzer
    {
        private static readonly string[] TERMINALS = {
            "INTEGER", "REAL", "BOOLEAN", "STRING", "VAR", ":",";"
        };

        private static readonly Regex TOKEN_PATTERN = new Regex(
        @"\b(VAR)\b" +
        @"|([a-zA-Z_][a-zA-Z0-9_]*)" +
        @"|(:)" +
        @"|(INTEGER|REAL|BOOLEAN|STRING)" +
        @"|(;)*",
        RegexOptions.Compiled
        );
        private static readonly Regex regexInput = new Regex(@"^(\s*VAR\s*[_a-zA-Z][_a-zA-Z0-9]*\s*:\s*(REAL|STRING|BOOLEAN|INTEGER)\s*;\s*)+$");

        public static List<Lexema> GetLexicalTokens(string input)
        {
            var tokens = new List<Lexema>();
            if (!validarFormatoEntrada(input))
            {
                return tokens;
            }
            var matches = TOKEN_PATTERN.Matches(input);

            foreach (Match match in matches)
            {
                string token = match.Value;

                if (Array.Exists(TERMINALS, t => t == token))
                {
                    tokens.Add(new Lexema(token, token));
                }
                else if (Regex.IsMatch(token, @"^[a-zA-Z_][a-zA-Z0-9_]*$"))
                {
                    tokens.Add(new Lexema("IDENTIFIER", token));
                }
            }

            tokens.Add(new Lexema("$", "eof"));  // Token EOF
            return tokens;
        }
        private static bool validarFormatoEntrada(string input)
        {
            return regexInput.IsMatch(input);
        }
    }
    public class Lexema
    {
        public string Tipo { get; }
        public string Valor { get; }

        public Lexema(string tipo, string valor)
        {
            Tipo = tipo;
            Valor = valor;
        }

        public override string ToString()
        {
            return $"<{Tipo}, {Valor}>";
        }
    }
}
