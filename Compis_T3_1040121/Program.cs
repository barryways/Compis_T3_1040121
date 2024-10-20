using Compis_T3_1040121.Analyzers;

namespace Compis_T3_1040121
{
    internal class Program
    {
        private static FileAnalyzer fileAnalyzer;
        private static string textoCompleto = ""; 
        private static List<string> terminals = new List<string>();

        static void Main(string[] args)
        {
            string input = "";
            Console.WriteLine("Compiladores: Tarea #3 -Carlos Daniel Barrientos Castillo {1040121}");
            
            try
            {
                //Console.WriteLine("Introduce la cadena a evaluar");
                //input = Console.ReadLine();
                
                LeerArchivo("../../../GFC.txt");
                fileAnalyzer = new FileAnalyzer(textoCompleto);
                fileAnalyzer.ValidarTexto(); // Analiza y valida el contenido del archivo

                terminals = fileAnalyzer.getTerminals();
                fileAnalyzer.getNonTerminals();
                var rawProductions = fileAnalyzer.getProductions();
                var tokens = LexicalAnalyzer.GetLexicalTokens(" VAR x : INTEGER ;");

                Console.WriteLine("Tokens encontrados:");
                foreach (var token in tokens)
                {
                    Console.WriteLine(token);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            Console.ReadKey();
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
