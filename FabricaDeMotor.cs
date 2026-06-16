using System;

namespace Practico_Integrador1.Datos  
{
    public static class FabricaDeMotor
    {
        public static IAccesoDatos ObtenerMotor(Motor motor)
        {
            return motor switch
            {
                Motor.MySql => new AccesoMySql(),
                Motor.SqlServer => new AccesoSqlServer(),
                Motor.Postgres => new AccesoPostgres(),
                _ => throw new ArgumentException("Motor no válido")
            };
        }
    }
}
