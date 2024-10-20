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

        public LR1(Dictionary<string, List<string>> _producciones)
        {
            producciones = _producciones;
            estados = new List<HashSet<ItemLR1>>();
            tablaAccion = new Dictionary<int, Dictionary<string, Transicion>>();
            tablaGoto = new Dictionary<int, Dictionary<string, int>>();
        }

        public void CrearTransiciones()
        {
            HashSet<ItemLR1> estadoInicial = new HashSet<ItemLR1>
            {
                new ItemLR1("<S>", new string[] { "<var_declaration>", "$" }, 0, "$")
            };

            HashSet<ItemLR1> cierreInicial = Closure(estadoInicial);

            estados.Add(cierreInicial);

            for (int i = 0; i < estados.Count; i++)
            {
                HashSet<ItemLR1> estado = estados[i];

                foreach (var simbolo in ObtenerSimbolos(estado))
                {
                    HashSet<ItemLR1> nuevoEstado = Goto(estado, simbolo);

                    if (nuevoEstado.Count > 0 && !EstadoYaExiste(nuevoEstado))
                    {
                        estados.Add(nuevoEstado);
                    }

                    AgregarTransicion(i, simbolo, nuevoEstado);
                }

                foreach (var item in estado)
                {
                    if (item.EsItemDeReduccion())
                    {
                        if (item.LadoIzquierdo == "<S>" && item.Lookahead == "$")
                        {
                            tablaAccion[i] = new Dictionary<string, Transicion> { { "$", new Transicion { Simbolo = "ACCEPT" } } };
                        }
                        else
                        {
                            tablaAccion[i] = new Dictionary<string, Transicion> { { item.Lookahead, new Transicion { Simbolo = "REDUCE" } } };
                        }
                    }
                }
            }
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
            HashSet<ItemLR1> cerrado = new HashSet<ItemLR1>(items);

            bool cambios = true;
            while (cambios)
            {
                cambios = false;

                HashSet<ItemLR1> nuevosItems = new HashSet<ItemLR1>();

                foreach (var item in cerrado)
                {
                    if (item.Punto < item.LadoDerecho.Length)
                    {
                        string simbolo = item.LadoDerecho[item.Punto];

                        if (producciones.ContainsKey(simbolo))
                        {
                            foreach (var produccion in producciones[simbolo])
                            {
                                string[] ladoDerecho = DividirProduccion(produccion);

                                Console.WriteLine($"Creando ItemLR1: {simbolo} -> {string.Join(" ", ladoDerecho)}");

                                ItemLR1 nuevoItem = new ItemLR1(simbolo, ladoDerecho, 0, item.Lookahead);
                                if (!cerrado.Contains(nuevoItem))
                                {
                                    nuevosItems.Add(nuevoItem);
                                    cambios = true;
                                }
                            }
                        }
                    }
                }

                foreach (var nuevoItem in nuevosItems)
                {
                    cerrado.Add(nuevoItem);
                }
            }

            return cerrado;
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

            return Closure(nuevoEstado);
        }

        private void AgregarTransicion(int estado, string simbolo, HashSet<ItemLR1> nuevoEstado)
        {
            if (nuevoEstado.Count == 0) return;

            int indiceNuevoEstado = estados.IndexOf(nuevoEstado);

            if (!tablaAccion.ContainsKey(estado))
            {
                tablaAccion[estado] = new Dictionary<string, Transicion>();
            }

            tablaAccion[estado][simbolo] = new Transicion
            {
                Simbolo = "SHIFT",
                EstadoInicial = estado,
                EstadoTransicionado = indiceNuevoEstado
            };
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
    }
}
