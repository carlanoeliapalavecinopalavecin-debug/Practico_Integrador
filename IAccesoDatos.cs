using System;

namespace Practico_Integrador1.Datos  // ✅ EXACTO
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