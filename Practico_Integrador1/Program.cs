using Practico_Integrador1.Datos;

namespace Practico_Integrador1
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("===== PRÁCTICO INTEGRADOR - ADO.NET =====");
            Console.WriteLine("Cumpliendo todos los requisitos: RF2, RF3, RF4, RF5\n");

            Motor motorElegido;

            if (args.Length > 0)
            {
                motorElegido = args[0].ToLower() switch
                {
                    "sqlserver" => Motor.SqlServer,
                    "postgres" => Motor.Postgres,
                    _ => Motor.MySql
                };
            }
            else
            {
                Console.WriteLine("Elegí el motor de base de datos:");
                Console.WriteLine("1 - MySQL");
                Console.WriteLine("2 - SQL Server");
                Console.WriteLine("3 - PostgreSQL");
                Console.Write("Ingresá opción: ");

                string opcion = Console.ReadLine() ?? "1";
                motorElegido = opcion switch
                {
                    "2" => Motor.SqlServer,
                    "3" => Motor.Postgres,
                    _ => Motor.MySql
                };
            }

            IAccesoDatos acceso = FabricaDeMotor.ObtenerMotor(motorElegido);
            Console.WriteLine($"\n>>> Usando motor: {acceso.NombreMotor} <<<");

            try
            {
                acceso.CrearEstructura();
                acceso.InsertarDatosPrueba();
                acceso.EjecutarOperaciones();
                acceso.DemostrarRollback();

                Console.WriteLine("\n===== TODOS LOS PROCESOS FINALIZADOS CORRECTAMENTE =====");
                Console.WriteLine(" Se cumplieron todas las consignas del trabajo.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ ERROR GENERAL: {ex.Message}");
            }
        }
    }
}
