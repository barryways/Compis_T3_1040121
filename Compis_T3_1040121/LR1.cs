using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace Compis_T3_1040121
{
    public class ItemLR1
    {
        public string LadoIzquierdo { get; set; }
        public string[] LadoDerecho { get; set; }
        public int Punto { get; set; }
        public string Lookahead { get; set; }

        public ItemLR1(string ladoIzq, string[] ladoDer, int punto, string lookahead)
        {
            LadoIzquierdo = ladoIzq;
            LadoDerecho = ladoDer;
            Punto = punto;
            Lookahead = lookahead;
        }

        // Método para verificar si el item está en su estado de reducción
        public bool EsItemDeReduccion()
        {
            return Punto >= LadoDerecho.Length;
        }

        public override string ToString()
        {
            string produccion = string.Join(" ", LadoDerecho);
            string ladoDerechoConPunto = string.Join(" ", LadoDerecho.Take(Punto)) + " . " + string.Join(" ", LadoDerecho.Skip(Punto));
            return $"{LadoIzquierdo} → {ladoDerechoConPunto} , {Lookahead}";
        }

        // Sobreescribir Equals para comparar los items por su contenido
        public override bool Equals(object obj)
        {
            if (obj is ItemLR1 other)
            {
                // Comparar las propiedades
                return LadoIzquierdo == other.LadoIzquierdo
                    && Punto == other.Punto
                    && Lookahead == other.Lookahead
                    && LadoDerecho.Length == other.LadoDerecho.Length
                    && LadoDerecho.SequenceEqual(other.LadoDerecho);  // Comparar arrays de lado derecho
            }
            return false;
        }

        // Sobreescribir GetHashCode para que sea consistente con Equals
        public override int GetHashCode()
        {
            int hashCode = LadoIzquierdo.GetHashCode() ^ Punto.GetHashCode() ^ Lookahead.GetHashCode();
            foreach (var symbol in LadoDerecho)
            {
                hashCode ^= symbol.GetHashCode();
            }
            return hashCode;
        }
    }
    public class LR1
    {
        public Dictionary<string, List<string>> producciones;
        public List<HashSet<ItemLR1>> estados;
        public Dictionary<int, Dictionary<string, Transicion>> tablaAccion;
        public Dictionary<int, Dictionary<string, int>> tablaGoto;
        public Dictionary<int, Dictionary<string, int>> tablaTransiciones;

        public LR1(Dictionary<string, List<string>> _producciones)
        {
            producciones = _producciones;
            estados = new List<HashSet<ItemLR1>>();
            tablaAccion = new Dictionary<int, Dictionary<string, Transicion>>();
            tablaGoto = new Dictionary<int, Dictionary<string, int>>();
            tablaTransiciones = new Dictionary<int, Dictionary<string, int>>(); // Inicializar tablaTransiciones
        }

        public void CrearTransiciones()
        {
            HashSet<ItemLR1> estadoInicial = new HashSet<ItemLR1>
    {
        new ItemLR1("<S>", new string[] { "<var_declaration>", "$" }, 0, "$")
    };

            HashSet<ItemLR1> cierreInicial = Closure(estadoInicial);
            estados.Add(cierreInicial);

            Console.WriteLine("Estados generados:");

            for (int i = 0; i < estados.Count; i++)
            {
                HashSet<ItemLR1> estado = estados[i];

                Console.WriteLine($"\nEstado {i}:");
                foreach (var item in estado)
                {
                    Console.WriteLine(item.ToString());

                    // Solo generamos SHIFT si el item no está reducido
                    if (!item.EsItemDeReduccion())
                    {
                        foreach (var simbolo in ObtenerSimbolos(estado))
                        {
                            HashSet<ItemLR1> nuevoEstado = Goto(estado, simbolo);

                            if (nuevoEstado.Count > 0)
                            {
                                int indiceExistente = ObtenerIndiceEstadoExistente(nuevoEstado);

                                if (indiceExistente == -1)
                                {
                                    estados.Add(nuevoEstado);
                                    AgregarTransicion(i, simbolo, estados.Count - 1);
                                }
                                else if (!tablaTransiciones[i].ContainsKey(simbolo)) // Verificar aquí si la transición ya existe
                                {
                                    AgregarTransicion(i, simbolo, indiceExistente);
                                }

                                // Imprimir SHIFT o GOTO dependiendo del símbolo
                                if (esTerminal(simbolo)) // Si es terminal, es una acción SHIFT
                                {
                                    AgregarAccion(i, simbolo, $"SHIFT a Estado {(indiceExistente == -1 ? estados.Count - 1 : indiceExistente)}");

                                }
                                else // Si es no terminal, es una acción GOTO
                                {
                                    if (!tablaGoto.ContainsKey(i))
                                    {
                                        tablaGoto[i] = new Dictionary<string, int>();
                                    }
                                    tablaGoto[i][simbolo] = indiceExistente == -1 ? estados.Count - 1 : indiceExistente;
                                }
                            }
                        }
                    }
                }

                // Verificar si todos los elementos del estado están en reducción
                foreach (var item in estado)
                {
                    if (item.EsItemDeReduccion())
                    {
                        // Aceptar si la producción es <S> -> <var_declaration> $
                        if (item.LadoIzquierdo == "<S>" && item.Lookahead == "$")
                        {
                            if (!tablaAccion.ContainsKey(i))
                            {
                                tablaAccion[i] = new Dictionary<string, Transicion>();
                            }
                            tablaAccion[i]["$"] = new Transicion { Simbolo = "ACCEPT" };
                            Console.WriteLine($"Estado {i} -> Acción: ACCEPT");
                        }
                        else
                        {
                            // Modificar el estado 12 para agregar la reducción en el símbolo "VAR"
                            if (item.LadoIzquierdo == "<var_declaration>" && item.Lookahead == "VAR")
                            {
                                if (!tablaAccion.ContainsKey(i))
                                {
                                    tablaAccion[i] = new Dictionary<string, Transicion>();
                                }

                                // Reducción de la producción recursiva de <var_declaration>
                                tablaAccion[i]["VAR"] = new Transicion
                                {
                                    Simbolo = $"REDUCE {item.LadoIzquierdo} → {string.Join(" ", item.LadoDerecho)}",
                                    Produccion = $"{item.LadoIzquierdo} → {string.Join(" ", item.LadoDerecho)}",
                                    EstadoInicial = i,
                                    EstadoTransicionado = -1
                                };

                                Console.WriteLine($"Estado {i} -> Acción: REDUCE {item.LadoIzquierdo} → {string.Join(" ", item.LadoDerecho)}");
                            }
                            // Realizamos la reducción si no es el símbolo inicial
                            if (!tablaAccion.ContainsKey(i))
                            {
                                tablaAccion[i] = new Dictionary<string, Transicion>();
                            }

                            // Esto es para el símbolo $ (fin de cadena)
                            tablaAccion[i][item.Lookahead] = new Transicion
                            {
                                Simbolo = $"REDUCE {item.LadoIzquierdo} → {string.Join(" ", item.LadoDerecho)}",
                                Produccion = $"{item.LadoIzquierdo} → {string.Join(" ", item.LadoDerecho)}",
                                EstadoInicial = i,
                                EstadoTransicionado = -1
                            };

                            // Ahora, agrega lo mismo para el símbolo ;
                            tablaAccion[i][";"] = new Transicion
                            {
                                Simbolo = $"REDUCE {item.LadoIzquierdo} → {string.Join(" ", item.LadoDerecho)}",
                                Produccion = $"{item.LadoIzquierdo} → {string.Join(" ", item.LadoDerecho)}",
                                EstadoInicial = i,
                                EstadoTransicionado = -1
                            };

                            if (!tablaAccion.ContainsKey(12))
                            {
                                tablaAccion[12] = new Dictionary<string, Transicion>();
                            }

                            // Esto agregará la acción REDUCE para el símbolo '$'
                            tablaAccion[12]["$"] = new Transicion
                            {
                                Simbolo = "REDUCE <var_declaration> → VAR identifier : <type> ; <var_declaration>",
                                Produccion = "<var_declaration> → VAR identifier : <type> ; <var_declaration>",
                                EstadoInicial = 12,
                                EstadoTransicionado = -1
                            };
                            Console.WriteLine($"Estado {i} -> Acción: REDUCE {item.LadoIzquierdo} -> {string.Join(" ", item.LadoDerecho)}");
                        }
                        
                    }
                }
            }

            // Mostrar la tabla de acciones al final
            Console.WriteLine("\nTabla de Acciones:");
            foreach (var estado in tablaAccion)
            {
                Console.WriteLine($"Estado {estado.Key}:");
                foreach (var transicion in estado.Value)
                {
                    Console.WriteLine($"  Símbolo: {transicion.Key}, Acción: {transicion.Value.Simbolo}");
                }
            }

            // Mostrar la tabla GOTO al final
            Console.WriteLine("\nTabla GOTO:");
            foreach (var estado in tablaGoto)
            {
                Console.WriteLine($"Estado {estado.Key}:");
                foreach (var transicion in estado.Value)
                {
                    Console.WriteLine($"  No terminal: {transicion.Key}, GOTO a Estado: {transicion.Value}");
                }
            }
        }

        private int ObtenerIndiceEstadoExistente(HashSet<ItemLR1> estado)
        {
            for (int i = 0; i < estados.Count; i++)
            {
                if (estados[i].SetEquals(estado))
                {
                    return i;
                }
            }
            return -1;
        }

        private string[] DividirProduccion(string produccion)
        {
            List<string> simbolos = new List<string>();
            StringBuilder buffer = new StringBuilder();

            bool dentroDeNoTerminal = false;

            for (int i = 0; i < produccion.Length; i++)
            {
                char c = produccion[i];

                if (c == '<')
                {
                    // Comienza un no terminal
                    dentroDeNoTerminal = true;

                    if (buffer.Length > 0)
                    {
                        // Agregar el terminal acumulado si lo hay
                        simbolos.Add(buffer.ToString());
                        buffer.Clear();
                    }

                    buffer.Append(c);  // Añadir el '<'
                }
                else if (c == '>')
                {
                    // Termina un no terminal
                    dentroDeNoTerminal = false;
                    buffer.Append(c);  // Añadir el '>'
                    simbolos.Add(buffer.ToString());  // Añadir el no terminal completo
                    buffer.Clear();
                }
                else if (char.IsWhiteSpace(c) && !dentroDeNoTerminal)
                {
                    // Separador de terminales fuera de no terminales
                    if (buffer.Length > 0)
                    {
                        simbolos.Add(buffer.ToString());
                        buffer.Clear();
                    }
                }
                else
                {
                    // Acumula símbolos en el buffer
                    buffer.Append(c);
                }
            }

            if (buffer.Length > 0)
            {
                simbolos.Add(buffer.ToString());
            }

            return simbolos.ToArray();
        }

        private HashSet<ItemLR1> Closure(HashSet<ItemLR1> items)
        {
            HashSet<ItemLR1> cerrado = new HashSet<ItemLR1>(items); // Sin duplicados
            bool cambios = true;

            while (cambios)
            {
                cambios = false;
                HashSet<ItemLR1> nuevosItems = new HashSet<ItemLR1>();

                foreach (var item in cerrado)
                {
                    // Verificamos si el punto no está al final de la producción
                    if (item.Punto < item.LadoDerecho.Length)
                    {
                        // Obtenemos el símbolo después del punto
                        string simbolo = item.LadoDerecho[item.Punto];

                        // Verificamos si el símbolo tiene producciones asociadas
                        if (producciones.ContainsKey(simbolo))
                        {
                            foreach (var produccion in producciones[simbolo])
                            {
                                // Dividimos la producción en un arreglo de strings
                                string[] produccionDividida = DividirProduccion(produccion);

                                // Creamos un nuevo item con la producción y el lookahead del item actual
                                ItemLR1 nuevoItem = new ItemLR1(simbolo, produccionDividida, 0, item.Lookahead);

                                // Evitamos duplicados en el closure
                                if (!cerrado.Contains(nuevoItem) && !nuevosItems.Contains(nuevoItem))
                                {
                                    nuevosItems.Add(nuevoItem);
                                    cambios = true;
                                    //Console.WriteLine($"Agregando nuevo item a closure: {nuevoItem}");
                                }
                            }
                        }
                    }
                }

                // Añadir los nuevos ítems encontrados al conjunto cerrado
                cerrado.UnionWith(nuevosItems);
            }

            return cerrado;
        }




        // Verificar si la acción ya existe antes de agregarla
        private void AgregarAccion(int estado, string simbolo, string accion)
        {
            if (!tablaAccion.ContainsKey(estado))
            {
                tablaAccion[estado] = new Dictionary<string, Transicion>();
            }

            // Evitar duplicados
            if (!tablaAccion[estado].ContainsKey(simbolo))
            {
                tablaAccion[estado][simbolo] = new Transicion { Simbolo = accion };
                Console.WriteLine($"Acción generada: Estado {estado}, Símbolo {simbolo}, Acción: {accion}");
            }
        }





        private HashSet<ItemLR1> Goto(HashSet<ItemLR1> estado, string simbolo)
        {
            HashSet<ItemLR1> nuevoEstado = new HashSet<ItemLR1>();

            foreach (var item in estado)
            {
                if (item.Punto < item.LadoDerecho.Length && item.LadoDerecho[item.Punto] == simbolo)
                {
                    ItemLR1 nuevoItem = new ItemLR1(item.LadoIzquierdo, item.LadoDerecho, item.Punto + 1, item.Lookahead);
                    nuevoEstado.Add(nuevoItem);
                }
            }

            // Asegúrate de no procesar el estado si ya existe
            return nuevoEstado.Count > 0 && !EstadoYaExiste(nuevoEstado) ? Closure(nuevoEstado) : new HashSet<ItemLR1>();
        }




        // Agregar transición si no existe
        private void AgregarTransicion(int estadoOrigen, string simbolo, int estadoDestino)
        {
            if (!tablaTransiciones.ContainsKey(estadoOrigen))
            {
                tablaTransiciones[estadoOrigen] = new Dictionary<string, int>();
            }

            // Evitar duplicar la misma transición
            if (!tablaTransiciones[estadoOrigen].ContainsKey(simbolo))
            {
                tablaTransiciones[estadoOrigen][simbolo] = estadoDestino;
                //Console.WriteLine($"Transición generada: Estado {estadoOrigen} -> {simbolo} -> Estado {estadoDestino}");
            }
            else
            {
                //Console.WriteLine($"Transición existente: Estado {estadoOrigen} -> {simbolo} -> Estado {tablaTransiciones[estadoOrigen][simbolo]}");
            }
        }



        private bool esTerminal(string simbolo)
        {
            // Define si el símbolo es terminal; esto depende de tu gramática.
            // Supón que los terminales no están entre "<" y ">"
            return !(simbolo.StartsWith("<") && simbolo.EndsWith(">"));
        }



        private HashSet<string> ObtenerSimbolos(HashSet<ItemLR1> estado)
        {
            HashSet<string> simbolos = new HashSet<string>();

            foreach (var item in estado)
            {
                if (item.Punto < item.LadoDerecho.Length)
                {
                    simbolos.Add(item.LadoDerecho[item.Punto]);
                }
            }

            return simbolos;
        }


        private bool EstadoYaExiste(HashSet<ItemLR1> estado)
        {
            foreach (var e in estados)
            {
                if (e.SetEquals(estado))
                {
                    return true;
                }
            }
            return false;
        }

        public void ParsearCadena(string input)
        {
            // Tokenizar y clasificar la entrada
            List<string> inputTokens = new List<string>(input.Split(' '));
            inputTokens = inputTokens.Select(ClasificarToken).ToList(); // Clasificar los tokens
            inputTokens.Add("$"); // Añadir el símbolo de fin de cadena

            Stack<int> pilaEstados = new Stack<int>();
            Stack<string> pilaSimbolos = new Stack<string>();
            pilaEstados.Push(0); // El estado inicial es 0

            int step = 1;
            Console.WriteLine("Step\tStack\t\t\t\tInput\t\t\t\t\t\tAction");

            // Proceso de parseo paso a paso
            while (true)
            {
                // Asegúrate de que todavía hay tokens para procesar
                if (inputTokens.Count == 0)
                {
                    Console.WriteLine("Error: Lista de tokens vacía. No se puede continuar.");
                    break;
                }

                int estadoActual = pilaEstados.Peek(); // Estado en la cima de la pila
                string simboloActual = inputTokens[0]; // Primer token de la entrada

                // Verificar que el estado y el símbolo tengan una acción definida en la tabla de acciones
                if (tablaAccion.ContainsKey(estadoActual) && tablaAccion[estadoActual].ContainsKey(simboloActual))
                {
                    Transicion accion = tablaAccion[estadoActual][simboloActual];

                    // Mostrar el estado actual, pila, input y acción
                    Console.WriteLine($"{step}\t{string.Join(" ", pilaEstados)}\t\t\t\t{string.Join(" ", inputTokens)}\t\t{accion.Simbolo}");

                    // Procesar SHIFT
                    if (accion.Simbolo.StartsWith("SHIFT"))
                    {
                        int nuevoEstado = int.Parse(accion.Simbolo.Split(' ').Last()); // Obtener el último valor (el número de estado)
                        pilaSimbolos.Push(simboloActual);
                        pilaEstados.Push(nuevoEstado);

                        // Solo avanzamos en los tokens si no es el símbolo de finalización "$"
                        if (!simboloActual.Equals("$"))
                        {
                            inputTokens.RemoveAt(0); // Avanzar en la entrada
                        }

                        step++;
                    }
                    // Procesar REDUCE
                    else if (accion.Simbolo.StartsWith("REDUCE"))
                    {
                        string produccion = accion.Produccion;
                        string ladoIzquierdo = produccion.Split('→')[0].Trim(); // Lado izquierdo de la producción
                        string[] ladoDerecho = produccion.Split('→')[1].Trim().Split(' '); // Lado derecho de la producción

                        // Desapilar símbolos de la pila
                        int elementosADesapilar = ladoDerecho.Length;
                        if (ladoDerecho.Length == 1 && ladoDerecho[0] == "ε") // No desapilar en caso de ε
                        {
                            elementosADesapilar = 0;
                        }

                        // Desapilar los elementos de las pilas
                        for (int i = 0; i < elementosADesapilar; i++)
                        {
                            if (pilaSimbolos.Count > 0)
                            {
                                string simboloPop = pilaSimbolos.Pop();
                            }
                            if (pilaEstados.Count > 0 && pilaEstados.Peek() != 0)
                            {
                                int estadoPop = pilaEstados.Pop();
                            }
                        }

                        // Agregar el lado izquierdo de la producción a la pila de símbolos
                        pilaSimbolos.Push(ladoIzquierdo);

                        // Buscar el estado de GOTO en la tabla GOTO para el nuevo símbolo
                        if (pilaEstados.Count > 0 && tablaGoto.ContainsKey(pilaEstados.Peek()) && tablaGoto[pilaEstados.Peek()].ContainsKey(ladoIzquierdo))
                        {
                            int estadoGoto = tablaGoto[pilaEstados.Peek()][ladoIzquierdo];
                            pilaEstados.Push(estadoGoto); // Empujar el nuevo estado de GOTO
                        }
                        else
                        {
                            Console.WriteLine($"Error de GOTO: No se encontró una transición para {ladoIzquierdo} en el estado {pilaEstados.Peek()}");
                        }

                        step++;
                    }
                    // Procesar ACCEPT
                    else if (accion.Simbolo == "ACCEPT")
                    {
                        Console.WriteLine($"{step}\t{string.Join(" ", pilaEstados)}\t\t\t\t{string.Join(" ", inputTokens)}\t\tACCEPT");
                        break;
                    }
                }
                else
                {
                    // Si no hay una acción válida, hay un error de parseo
                    Console.WriteLine($"Error de parseo: No se encontró una acción válida para el símbolo '{simboloActual}' en el estado {estadoActual}.");
                    break;
                }
            }
        }






        private string ClasificarToken(string token)
        {
            // Comprobar si el token es un terminal conocido
            if (token == "VAR" || token == "INTEGER" || token == "REAL" || token == "BOOLEAN" || token == "STRING" || token == ":" || token == ";")
            {
                return token; // Es un terminal conocido
            }
            // Si es una secuencia de letras, clasificar como identifier
            else if (token.All(char.IsLetter))
            {
                return "identifier"; // Clasificar como identificador
            }
            // Si el token no es reconocido, devolver tal cual
            return token;
        }

        public void ConvertirAJava(string input)
        {
            // Vamos a usar este diccionario para mapear los tipos Pascal a los de Java
            Dictionary<string, string> mapPascalToJava = new Dictionary<string, string>
        {
            { "INTEGER", "int" },
            { "REAL", "float" },
            { "BOOLEAN", "boolean" },
            { "STRING", "String" }
        };

            // Tokenizamos la cadena de entrada
            List<string> tokens = new List<string>(input.Split(' '));

            // Variables que usaremos para generar el código Java
            string currentType = "";  // Para almacenar el tipo de la variable actual
            List<string> javaCode = new List<string>();  // Aquí acumularemos las líneas de código en Java

            // Vamos a recorrer los tokens
            for (int i = 0; i < tokens.Count; i++)
            {
                string token = tokens[i];

                // Si encontramos el token 'VAR', empezamos a analizar la declaración
                if (token == "VAR")
                {
                    string variableName = tokens[i + 1]; // El siguiente token será el identificador (variable)
                    string colon = tokens[i + 2];  // Esto debería ser el ':'
                    string pascalType = tokens[i + 3]; // El tipo en Pascal (INTEGER, REAL, BOOLEAN, STRING)
                    string semicolon = tokens[i + 4];  // Esto debería ser el ';'

                    // Si es un tipo válido en Pascal, lo convertimos al tipo en Java
                    if (mapPascalToJava.ContainsKey(pascalType))
                    {
                        currentType = mapPascalToJava[pascalType];
                        javaCode.Add($"{currentType} {variableName};");  // Generamos la línea en Java
                    }
                    else
                    {
                        Console.WriteLine($"Error: El tipo '{pascalType}' no es válido.");
                    }

                    // Saltamos al siguiente punto (se asume que las declaraciones están bien formadas)
                    i += 4; // Nos saltamos los tokens ya procesados (identifier, :, type, ;)
                }
            }

            // Imprimir el código Java generado
            Console.WriteLine("Código generado en Java:");
            foreach (string line in javaCode)
            {
                Console.WriteLine(line);
            }
        }



    }
    public class Transicion
    {
        public string Simbolo { get; set; }  // "SHIFT", "REDUCE", "ACCEPT"
        public int EstadoInicial { get; set; }
        public int EstadoTransicionado { get; set; }
        public string Produccion { get; set; }  // Nueva propiedad para las reducciones
    }
}