using System;
using Practico_Integrador1.Dominio;

namespace Practico_Integrador1.Datos
{
    public interface IAccesoDatos
    {
        string NombreMotor { get; }
        void CrearEstructura();
        void InsertarDatosPrueba();
        void EjecutarOperaciones();
        void DemostrarRollback();
    }
}