using System;
using Npgsql;
using Practico_Integrador1.Dominio;

namespace Practico_Integrador1.Datos
{
    public class AccesoPostgres : IAccesoDatos
    {
        public string NombreMotor => "PostgreSQL";
        private readonly string _conexion = "Host=127.0.0.1;Port=5432;Username=postgres;Password=Curso.NET2026;";

        public void CrearEstructura()
        {
            Console.WriteLine("\n=== RF2: CREAR ESTRUCTURA POSTGRESQL ===");
            try
            {
                using var conn = new NpgsqlConnection(_conexion);
                conn.Open();
                Console.WriteLine(" Conectado a PostgreSQL");

                using var cmdDb = new NpgsqlCommand(@"
                    SELECT 1 FROM pg_database WHERE datname = 'practico';", conn);
                if (cmdDb.ExecuteScalar() == null)
                {
                    using var cmdCreate = new NpgsqlCommand("CREATE DATABASE practico;", conn);
                    cmdCreate.ExecuteNonQuery();
                    Console.WriteLine(" Base 'practico' creada");
                }
                else
                    Console.WriteLine(" Base 'practico' ya existe");

                conn.ChangeDatabase("practico");

                string sqlTablas = @"
                    DROP TABLE IF EXISTS detalle_pedido;
                    DROP TABLE IF EXISTS pedidos;
                    DROP TABLE IF EXISTS productos;
                    DROP TABLE IF EXISTS clientes;
                    DROP TABLE IF EXISTS categorias;

                    CREATE TABLE categorias (
                        id SERIAL PRIMARY KEY,
                        nombre VARCHAR(100) NOT NULL UNIQUE
                    );

                    CREATE TABLE clientes (
                        id SERIAL PRIMARY KEY,
                        nombre VARCHAR(100) NOT NULL,
                        email VARCHAR(150) NOT NULL UNIQUE
                    );

                    CREATE TABLE productos (
                        id SERIAL PRIMARY KEY,
                        nombre VARCHAR(100) NOT NULL,
                        precio NUMERIC(10,2) NOT NULL CHECK (precio >= 0),
                        stock INT NOT NULL DEFAULT 0,
                        categoria_id INT NOT NULL,
                        FOREIGN KEY (categoria_id) REFERENCES categorias(id)
                    );

                    CREATE TABLE pedidos (
                        id SERIAL PRIMARY KEY,
                        cliente_id INT NOT NULL,
                        fecha TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        FOREIGN KEY (cliente_id) REFERENCES clientes(id)
                    );

                    CREATE TABLE detalle_pedido (
                        pedido_id INT NOT NULL,
                        producto_id INT NOT NULL,
                        cantidad INT NOT NULL CHECK (cantidad > 0),
                        precio_unitario NUMERIC(10,2) NOT NULL CHECK (precio_unitario >= 0),
                        PRIMARY KEY (pedido_id, producto_id),
                        FOREIGN KEY (pedido_id) REFERENCES pedidos(id) ON DELETE CASCADE,
                        FOREIGN KEY (producto_id) REFERENCES productos(id)
                    );
                ";

                using var cmdCrear = new NpgsqlCommand(sqlTablas, conn);
                cmdCrear.ExecuteNonQuery();
                Console.WriteLine(" Tablas creadas");
            }
            catch (Exception ex)
            {
                Console.WriteLine($" Error RF2: {ex.Message}");
                throw;
            }
        }

        public void InsertarDatosPrueba()
        {
            Console.WriteLine("\n=== RF3: INSERTAR DATOS POSTGRESQL ===");
            using var conn = new NpgsqlConnection(_conexion + "Database=practico;");
            conn.Open();
            using var tran = conn.BeginTransaction();

            try
            {
                int[] idsCat = new int[3];
                string sqlCat = "INSERT INTO categorias (nombre) VALUES (@nom) RETURNING id;";
                for (int i = 0; i < 3; i++)
                {
                    using var cmd = new NpgsqlCommand(sqlCat, conn, tran);
                    cmd.Parameters.AddWithValue("@nom", $"Categoría {i + 1}");
                    idsCat[i] = Convert.ToInt32(cmd.ExecuteScalar());
                }

                int[] idsProd = new int[5];
                string sqlProd = "INSERT INTO productos (nombre, precio, stock, categoria_id) VALUES (@nom, @prec, @stk, @cat) RETURNING id;";
                for (int i = 0; i < 5; i++)
                {
                    using var cmd = new NpgsqlCommand(sqlProd, conn, tran);
                    cmd.Parameters.AddWithValue("@nom", $"Producto {i + 1}");
                    cmd.Parameters.AddWithValue("@prec", 100.00m * (i + 1));
                    cmd.Parameters.AddWithValue("@stk", 10);
                    cmd.Parameters.AddWithValue("@cat", idsCat[i % 3]);
                    idsProd[i] = Convert.ToInt32(cmd.ExecuteScalar());
                }

                int[] idsCli = new int[2];
                string sqlCli = "INSERT INTO clientes (nombre, email) VALUES (@nom, @mail) RETURNING id;";
                for (int i = 0; i < 2; i++)
                {
                    using var cmd = new NpgsqlCommand(sqlCli, conn, tran);
                    cmd.Parameters.AddWithValue("@nom", $"Cliente {i + 1}");
                    cmd.Parameters.AddWithValue("@mail", $"cli{i + 1}@mail.com");
                    idsCli[i] = Convert.ToInt32(cmd.ExecuteScalar());
                }

                int[] idsPed = new int[2];
                string sqlPed = "INSERT INTO pedidos (cliente_id) VALUES (@cli) RETURNING id;";
                for (int i = 0; i < 2; i++)
                {
                    using var cmd = new NpgsqlCommand(sqlPed, conn, tran);
                    cmd.Parameters.AddWithValue("@cli", idsCli[i]);
                    idsPed[i] = Convert.ToInt32(cmd.ExecuteScalar());
                }

                string sqlDet = "INSERT INTO detalle_pedido (pedido_id, producto_id, cantidad, precio_unitario) VALUES (@ped, @prod, @cant, @prec);";
                for (int i = 0; i < 2; i++)
                {
                    using var cmd = new NpgsqlCommand(sqlDet, conn, tran);
                    cmd.Parameters.AddWithValue("@ped", idsPed[0]);
                    cmd.Parameters.AddWithValue("@prod", idsProd[i]);
                    cmd.Parameters.AddWithValue("@cant", 2);
                    cmd.Parameters.AddWithValue("@prec", 100.00m * (i + 1));
                    cmd.ExecuteNonQuery();
                }
                for (int i = 2; i < 5; i++)
                {
                    using var cmd = new NpgsqlCommand(sqlDet, conn, tran);
                    cmd.Parameters.AddWithValue("@ped", idsPed[1]);
                    cmd.Parameters.AddWithValue("@prod", idsProd[i]);
                    cmd.Parameters.AddWithValue("@cant", 1);
                    cmd.Parameters.AddWithValue("@prec", 100.00m * (i + 1));
                    cmd.ExecuteNonQuery();
                }

                tran.Commit();
                Console.WriteLine(" Datos insertados");
            }
            catch (Exception ex)
            {
                tran.Rollback();
                Console.WriteLine($" Error RF3: {ex.Message} → ROLLBACK");
                throw;
            }
        }

        public void EjecutarOperaciones()
        {
            Console.WriteLine("\n=== RF4: OPERACIONES POSTGRESQL ===");
            using var conn = new NpgsqlConnection(_conexion + "Database=practico;");
            conn.Open();
            using var tran = conn.BeginTransaction();

            try
            {
                Console.WriteLine("\n C1: Productos con categoría");
                string c1 = @"
                    SELECT p.id, p.nombre, p.precio, c.nombre AS categoria
                    FROM productos p
                    INNER JOIN categorias c ON p.categoria_id = c.id";

                using (var cmd = new NpgsqlCommand(c1, conn, tran))
                using (var lector = cmd.ExecuteReader())
                {
                    while (lector.Read())
                        Console.WriteLine($"#{lector["id"]} {lector["nombre"]} | ${lector["precio"]:F2} | {lector["categoria"]}");
                }

                Console.WriteLine("\n C2: Total Pedido #1");
                string c2 = @"
                    SELECT pr.nombre, d.cantidad, d.precio_unitario, (d.cantidad * d.precio_unitario) AS subtotal
                    FROM detalle_pedido d
                    JOIN productos pr ON d.producto_id = pr.id
                    WHERE d.pedido_id = 1";

                decimal total = 0;
                using (var cmd = new NpgsqlCommand(c2, conn, tran))
                using (var lector = cmd.ExecuteReader())
                {
                    while (lector.Read())
                    {
                        decimal sub = lector.GetDecimal("subtotal");
                        total += sub;
                        Console.WriteLine($"{lector["nombre"]} x{lector["cantidad"]} | Subtotal: ${sub:F2}");
                    }
                }
                Console.WriteLine($" TOTAL: ${total:F2}");

                string u1 = "UPDATE productos SET precio = precio * 1.10 WHERE categoria_id = @cat";
                using (var cmd = new NpgsqlCommand(u1, conn, tran))
                {
                    cmd.Parameters.AddWithValue("@cat", 1);
                    int filas = cmd.ExecuteNonQuery();
                    Console.WriteLine($" Actualizadas: {filas} filas");
                }

                string d1 = "DELETE FROM detalle_pedido WHERE pedido_id = @ped AND producto_id = @prod";
                using (var cmd = new NpgsqlCommand(d1, conn, tran))
                {
                    cmd.Parameters.AddWithValue("@ped", 1);
                    cmd.Parameters.AddWithValue("@prod", 1);
                    int filas = cmd.ExecuteNonQuery();
                    Console.WriteLine($" Borradas: {filas} filas");
                }

                tran.Commit();
            }
            catch (Exception ex)
            {
                tran.Rollback();
                Console.WriteLine($" Error RF4: {ex.Message} → ROLLBACK");
                throw;
            }
        }

        public void DemostrarRollback()
        {
            Console.WriteLine("\n=== RF5: ROLLBACK POSTGRESQL ===");
            using var conn = new NpgsqlConnection(_conexion + "Database=practico;");
            conn.Open();

            decimal antes;
            using (var cmd = new NpgsqlCommand("SELECT precio FROM productos WHERE id = 1", conn))
                antes = Convert.ToDecimal(cmd.ExecuteScalar());
            Console.WriteLine($" Precio ANTES: ${antes:F2}");

            try
            {
                using var tran = conn.BeginTransaction();
                using var cmdUpd = new NpgsqlCommand("UPDATE productos SET precio = 999 WHERE id = 1", conn, tran);
                cmdUpd.ExecuteNonQuery();
                Console.WriteLine(" Precio cambiado");
                throw new Exception(" ERROR");
            }
            catch (Exception ex)
            {
                Console.WriteLine($" Excepción: {ex.Message} → SE DESHACE");
            }

            decimal despues;
            using (var cmd = new NpgsqlCommand("SELECT precio FROM productos WHERE id = 1", conn))
                despues = Convert.ToDecimal(cmd.ExecuteScalar());
            Console.WriteLine($" Precio DESPUÉS: ${despues:F2}");
            Console.WriteLine(antes == despues ? " CORRECTO" : " FALLO");
        }
    }
}
