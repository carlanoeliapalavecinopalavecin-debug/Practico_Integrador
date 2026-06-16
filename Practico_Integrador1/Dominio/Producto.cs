using System;

namespace Practico_Integrador1.Dominio
{
    public class Producto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public int IdCategoria { get; set; }
    }
}