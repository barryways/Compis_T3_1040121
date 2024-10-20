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
                            // Realizamos la reducción si no es el símbolo inicial
                            if (!tablaAccion.ContainsKey(i))
                            {
                                tablaAccion[i] = new Dictionary<string, Transicion>();
                            }

                            tablaAccion[i][item.Lookahead] = new Transicion
                            {
                                Simbolo = $"REDUCE {item.LadoIzquierdo} → {string.Join(" ", item.LadoDerecho)}",
                                Produccion = $"{item.LadoIzquierdo} → {string.Join(" ", item.LadoDerecho)}",
                                EstadoInicial = i,
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
    }
    public class Transicion
    {
        public string Simbolo { get; set; }  // "SHIFT", "REDUCE", "ACCEPT"
        public int EstadoInicial { get; set; }
        public int EstadoTransicionado { get; set; }
        public string Produccion { get; set; }  // Nueva propiedad para las reducciones
    }
}