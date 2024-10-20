using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Compis_T3_1040121.Analyzers
{
    public class FileAnalyzer
    {
        // Regex captura secciones y validaciones
        private static readonly Regex productionsRegex = new Regex(@"^\s*<([a-zA-Z_][a-zA-Z0-9_]*)>\s*=\s*((?:<([a-zA-Z_][a-zA-Z0-9_]*)>|'[^']+'|\"".*\""|[a-zA-Z_][a-zA-Z0-9_]*|ε|\s|\||:|;|\.\s*)+)\s*$", RegexOptions.Multiline);

        //Guarda datos del archivo en las listas
        private string texto;
        private Dictionary<string, List<string>> productions = new Dictionary<string, List<string>>();

        //vamos a almacenar los terminales y los no terminales
        public List<string> nonTerminals = new List<string>();
        public List<string> terminals = new List<string>();

        public FileAnalyzer(string textoCompleto)
        {
            texto = textoCompleto;
        }

        public void ValidarTexto()
        {
            if (productionsRegex.IsMatch(texto))
            {
                Console.WriteLine($"La sección es válida.\n");
                GuardarProductions(texto);
                ValidarProducciones();

            }
            else
            {
                Console.WriteLine($"La sección NO es válida.\n");
            }


        }

        private void GuardarProductions(string contenido)
        {
            // Separar el contenido en líneas, ignorando las líneas vacías
            var lineas = contenido.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var linea in lineas)
            {
                // Buscar el índice del primer signo '='
                int indexOfEquals = linea.IndexOf('=');

                if (indexOfEquals != -1)
                {
                    // Dividir en dos partes: izquierda y derecha
                    string left = linea.Substring(0, indexOfEquals).Trim();  // La parte izquierda de la producción
                    string right = linea.Substring(indexOfEquals + 1).Trim(); // La parte derecha de la producción

                    // Si la producción no existe en el diccionario, la creamos
                    if (!productions.ContainsKey(left))
                    {
                        productions[left] = new List<string>();
                    }

                    // Separar la parte derecha por espacios y agregar cada palabra a la lista
                    var rightElements = right.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    productions[left].AddRange(rightElements); // Añadir las palabras de la parte derecha

                    // Mostrar el resultado para fines de depuración
                    Console.WriteLine($"Producción añadida: {left} = {string.Join(" ", rightElements)}");
                }
            }

            Console.WriteLine("Producciones registradas correctamente.");
        }

        public void ValidarProducciones()
        {
            Console.WriteLine("Validando Producciones...\n");

            // Recorrer cada producción registrada
            foreach (var produccion in productions)
            {
                string noTerminal = produccion.Key;
                List<string> elementos = produccion.Value;

                Console.WriteLine($"Validando producción: {noTerminal}");
                Console.WriteLine();

                foreach (var elemento in elementos)
                {
                    string cleanedElement = elemento.Trim();

                    // Si el elemento está entre <> (es un no terminal), verificar que exista en las producciones
                    if (cleanedElement.StartsWith("<") && cleanedElement.EndsWith(">"))
                    {
                        Console.WriteLine($"Elemento no terminal encontrado: {cleanedElement}");

                        // Verificar si el no terminal está definido en las producciones
                        if (!productions.ContainsKey(cleanedElement))
                        {
                            Console.WriteLine("---------------------------------------------------");
                            Console.WriteLine($"Error: El no terminal '{cleanedElement}' no está definido en las producciones.");
                            Console.WriteLine("---------------------------------------------------");
                        }
                        else
                        {
                            Console.WriteLine($"No terminal '{cleanedElement}' está correctamente definido.");
                            nonTerminals.Add(cleanedElement);
                        }
                        continue; // Pasar al siguiente elemento
                    }

                    // Verificar si el elemento existe en tokens, keywords o sets
                    if (EsTerminal(cleanedElement))
                    {
                        Console.WriteLine($"Elemento terminal válido: {cleanedElement}");
                        terminals.Add(cleanedElement);
                    }
                    else
                    {
                        Console.WriteLine("---------------------------------------------------");
                        Console.WriteLine($"Error: El elemento '{cleanedElement}' no existe en tokens, keywords o sets.");
                        Console.WriteLine("---------------------------------------------------");
                    }
                }

                Console.WriteLine("---------------------------------------------------\n");
            }

            Console.WriteLine("Validación de producciones completada.");
        }
        private bool EsTerminal(string element)
        {
            element = element.Trim('\'', ' ');
            if (element.Contains("\\\"\\\"") || element == "\"")
            {
                return true;  // Considerar las comillas dobles como un terminal válido
            }

            // Verifica si el elemento está en sets, tokens, keywords o si es épsilon (ε)
            return
                element == "ε" // Considera ε como un símbolo válido para épsilon
                || new[] { ";",":", "VAR", "REAL", "BOOLEAN",
                    "STRING", "identifier", "INTEGER" }.Contains(element);
        }


        public List<string> getNonTerminals()
        {

            return nonTerminals;
        }

        public List<string> getTerminals()
        {
            return terminals
            .Select(t => t.Trim())  // Elimina espacios en blanco al inicio y final.
            .Select(t => t.ToUpper())  // Normaliza el texto (opcional, si la comparación es case-insensitive).
            .Distinct()
            .ToList();
        }

        public Dictionary<string, List<string>> getProductions()
        {
            //string nuevaCategoria = "<S>";
            //List<string> nuevaLista = new List<string> { "<program>", "$" };
            //productions[nuevaCategoria] = nuevaLista;
            return productions;
        }

    }
}
