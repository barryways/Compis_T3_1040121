using Compis_T3_1040121.Analyzers;

namespace Compis_T3_1040121
{
    internal class Program
    {
        private static FileAnalyzer fileAnalyzer;
        private static string textoCompleto = "";
        private static List<string> terminals = new List<string>();
        private static Dictionary<string, List<string>> rawProductions = new Dictionary<string, List<string>>();
        private static List<Lexema> tokens = new List<Lexema>();


        static void Main(string[] args)
        {
            string input = "";
            Console.WriteLine("Compiladores: Tarea #3 -Carlos Daniel Barrientos Castillo {1040121}");
            try
            {
                //Console.WriteLine("Introduce la cadena a evaluar");
                //input = Console.ReadLine();
                useFileAnalyze();
                useLexicalAnalyzer();
                useLR1();


            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            Console.ReadKey();
        }
        static void useFileAnalyze()
        {
            LeerArchivo("../../../GFC.txt");
            fileAnalyzer = new FileAnalyzer(textoCompleto);
            fileAnalyzer.ValidarTexto(); // Analiza y valida el contenido del archivo
            terminals = fileAnalyzer.getTerminals();
            fileAnalyzer.getNonTerminals();
            rawProductions = fileAnalyzer.getProductions();
        }
        static void useLexicalAnalyzer()
        {
            tokens = LexicalAnalyzer.GetLexicalTokens(" VAR x : INTEGER ; VAR identificador : REAL ;");
            if (tokens.Count() == 0)
            {
                Console.WriteLine("La entrada no tiene el formato correcto");
            }
            else
            {
                Console.WriteLine("Tokens encontrados:");
                foreach (var token in tokens)
                {
                    Console.WriteLine(token);
                }
            }
        }
        static void useLR1()
        {

            Dictionary<string, List<string>> _productions = new Dictionary<string, List<string>>
            {
                {"<var_declaration>", new List<string> {"VAR identifier : <type> ; <var_declaration>", "ε"} },
                { "<type>", new List<string> { "INTEGER", "REAL", "BOOLEAN", "STRING" } }
            };


            LR1 parser = new LR1(_productions);
            parser.CrearTransiciones();



            string input = "VAR x : INTEGER ;";

            // Llamar al método ParsearCadena para mostrar el proceso de parseo
            parser.ParsearCadena(input);


        }

        static void LeerArchivo(string rutaArchivo)
        {
            try
            {
                using (StreamReader sr = new StreamReader(rutaArchivo))
                {
                    string linea;
                    while ((linea = sr.ReadLine()) != null)
                    {
                        textoCompleto += linea + Environment.NewLine; // Mantener el formato de líneas
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"El archivo no pudo ser leído: {e.Message}");
            }
        }
    }
}
