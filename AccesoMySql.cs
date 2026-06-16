using System;
using MySqlConnector;
using Practico_Integrador1.Dominio;

namespace Practico_Integrador1.Datos
{
    public class AccesoMySql : IAccesoDatos
    {
        public string NombreMotor => "MySQL";
        private readonly string _conexion = "Server=127.0.0.1;Port=3306;Uid=root;Pwd=Curso.NET2026;";

        public void CrearEstructura()
        {
            Console.WriteLine("\n=== RF2: CREAR ESTRUCTURA MYSQL ===");
            try
            {
                using var conn = new MySqlConnection(_conexion);
                conn.Open();
                Console.WriteLine(" Conectado al servidor MySQL");

                using var cmdDb = new MySqlCommand("CREATE DATABASE IF NOT EXISTS practico;", conn);
                cmdDb.ExecuteNonQuery();
                Console.WriteLine(" Base de datos 'practico' creada/verificada");

                conn.ChangeDatabase("practico");

                string sqlTablas = @"
                    DROP TABLE IF EXISTS detalle_pedido;
                    DROP TABLE IF EXISTS pedidos;
                    DROP TABLE IF EXISTS productos;
                    DROP TABLE IF EXISTS clientes;
                    DROP TABLE IF EXISTS categorias;

                    CREATE TABLE categorias (
                        id INT AUTO_INCREMENT PRIMARY KEY,
                        nombre VARCHAR(100) NOT NULL UNIQUE
                    );

                    CREATE TABLE clientes (
                        id INT AUTO_INCREMENT PRIMARY KEY,
                        nombre VARCHAR(100) NOT NULL,
                        email VARCHAR(150) NOT NULL UNIQUE
                    );

                    CREATE TABLE productos (
                        id INT AUTO_INCREMENT PRIMARY KEY,
                        nombre VARCHAR(100) NOT NULL,
                        precio DECIMAL(10,2) NOT NULL CHECK (precio >= 0),
                        stock INT NOT NULL DEFAULT 0,
                        categoria_id INT NOT NULL,
                        FOREIGN KEY (categoria_id) REFERENCES categorias(id)
                    );

                    CREATE TABLE pedidos (
                        id INT AUTO_INCREMENT PRIMARY KEY,
                        cliente_id INT NOT NULL,
                        fecha DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        FOREIGN KEY (cliente_id) REFERENCES clientes(id)
                    );

                    CREATE TABLE detalle_pedido (
                        pedido_id INT NOT NULL,
                        producto_id INT NOT NULL,
                        cantidad INT NOT NULL CHECK (cantidad > 0),
                        precio_unitario DECIMAL(10,2) NOT NULL CHECK (precio_unitario >= 0),
                        PRIMARY KEY (pedido_id, producto_id),
                        FOREIGN KEY (pedido_id) REFERENCES pedidos(id) ON DELETE CASCADE,
                        FOREIGN KEY (producto_id) REFERENCES productos(id)
                    );
                ";

                using var cmdCrear = new MySqlCommand(sqlTablas, conn);
                cmdCrear.ExecuteNonQuery();
                Console.WriteLine(" Tablas creadas correctamente con relaciones");
            }
            catch (Exception ex)
            {
                Console.WriteLine($" Error RF2: {ex.Message}");
                throw;
            }
        }

        public void InsertarDatosPrueba()
        {
            Console.WriteLine("\n=== RF3: INSERTAR DATOS DE PRUEBA ===");
            using var conn = new MySqlConnection(_conexion + "Database=practico;");
            conn.Open();
            using var tran = conn.BeginTransaction();

            try
            {
                int[] idsCat = new int[3];
                string sqlCat = "INSERT INTO categorias (nombre) VALUES (@nom); SELECT LAST_INSERT_ID();";
                for (int i = 0; i < 3; i++)
                {
                    using var cmd = new MySqlCommand(sqlCat, conn, tran);
                    cmd.Parameters.AddWithValue("@nom", $"Categoría {i + 1}");
                    idsCat[i] = Convert.ToInt32(cmd.ExecuteScalar());
                }
                Console.WriteLine(" Categorías insertadas");

                int[] idsProd = new int[5];
                string sqlProd = "INSERT INTO productos (nombre, precio, stock, categoria_id) VALUES (@nom, @prec, @stk, @cat); SELECT LAST_INSERT_ID();";
                for (int i = 0; i < 5; i++)
                {
                    using var cmd = new MySqlCommand(sqlProd, conn, tran);
                    cmd.Parameters.AddWithValue("@nom", $"Producto {i + 1}");
                    cmd.Parameters.AddWithValue("@prec", 100.00m * (i + 1));
                    cmd.Parameters.AddWithValue("@stk", 10);
                    cmd.Parameters.AddWithValue("@cat", idsCat[i % 3]);
                    idsProd[i] = Convert.ToInt32(cmd.ExecuteScalar());
                }
                Console.WriteLine(" Productos insertados");

                int[] idsCli = new int[2];
                string sqlCli = "INSERT INTO clientes (nombre, email) VALUES (@nom, @mail); SELECT LAST_INSERT_ID();";
                for (int i = 0; i < 2; i++)
                {
                    using var cmd = new MySqlCommand(sqlCli, conn, tran);
                    cmd.Parameters.AddWithValue("@nom", $"Cliente {i + 1}");
                    cmd.Parameters.AddWithValue("@mail", $"cli{i + 1}@mail.com");
                    idsCli[i] = Convert.ToInt32(cmd.ExecuteScalar());
                }
                Console.WriteLine(" Clientes insertados");

                int[] idsPed = new int[2];
                string sqlPed = "INSERT INTO pedidos (cliente_id) VALUES (@cli); SELECT LAST_INSERT_ID();";
                for (int i = 0; i < 2; i++)
                {
                    using var cmd = new MySqlCommand(sqlPed, conn, tran);
                    cmd.Parameters.AddWithValue("@cli", idsCli[i]);
                    idsPed[i] = Convert.ToInt32(cmd.ExecuteScalar());
                }
                Console.WriteLine(" Pedidos insertados");

                string sqlDet = "INSERT INTO detalle_pedido (pedido_id, producto_id, cantidad, precio_unitario) VALUES (@ped, @prod, @cant, @prec);";
                for (int i = 0; i < 2; i++)
                {
                    using var cmd = new MySqlCommand(sqlDet, conn, tran);
                    cmd.Parameters.AddWithValue("@ped", idsPed[0]);
                    cmd.Parameters.AddWithValue("@prod", idsProd[i]);
                    cmd.Parameters.AddWithValue("@cant", 2);
                    cmd.Parameters.AddWithValue("@prec", 100.00m * (i + 1));
                    cmd.ExecuteNonQuery();
                }
                for (int i = 2; i < 5; i++)
                {
                    using var cmd = new MySqlCommand(sqlDet, conn, tran);
                    cmd.Parameters.AddWithValue("@ped", idsPed[1]);
                    cmd.Parameters.AddWithValue("@prod", idsProd[i]);
                    cmd.Parameters.AddWithValue("@cant", 1);
                    cmd.Parameters.AddWithValue("@prec", 100.00m * (i + 1));
                    cmd.ExecuteNonQuery();
                }
                Console.WriteLine(" Detalles de pedidos insertados");

                tran.Commit();
                Console.WriteLine(" TRANSACCIÓN FINALIZADA: COMMIT - Todos los datos guardados");
            }
            catch (Exception ex)
            {
                tran.Rollback();
                Console.WriteLine($" Error RF3: {ex.Message} → SE HIZO ROLLBACK (no se guardó nada)");
                throw;
            }
        }

        public void EjecutarOperaciones()
        {
            Console.WriteLine("\n=== RF4: EJECUTAR OPERACIONES ===");
            using var conn = new MySqlConnection(_conexion + "Database=practico;");
            conn.Open();
            using var tran = conn.BeginTransaction();

            try
            {
                Console.WriteLine("\n🔍 C1: Consulta - Productos con su Categoría");
                string c1 = @"
                    SELECT p.id, p.nombre, p.precio, c.nombre AS categoria
                    FROM productos p
                    INNER JOIN categorias c ON p.categoria_id = c.id";

                using (var cmd = new MySqlCommand(c1, conn, tran))
                using (var lector = cmd.ExecuteReader())
                {
                    while (lector.Read())
                    {
                        Console.WriteLine($"#{lector["id"]} | {lector["nombre"]} | Precio: ${lector["precio"]:F2} | Categoría: {lector["categoria"]}");
                    }
                }

                Console.WriteLine("\n C2: Consulta - Detalle y Total de Pedido #1");
                string c2 = @"
                    SELECT pr.nombre, d.cantidad, d.precio_unitario, (d.cantidad * d.precio_unitario) AS subtotal
                    FROM detalle_pedido d
                    JOIN productos pr ON d.producto_id = pr.id
                    WHERE d.pedido_id = 1";

                decimal total = 0;
                using (var cmd = new MySqlCommand(c2, conn, tran))
                using (var lector = cmd.ExecuteReader())
                {
                    while (lector.Read())
                    {
                        decimal sub = lector.GetDecimal("subtotal");
                        total += sub;
                        Console.WriteLine($"{lector["nombre"]} | Cant: {lector["cantidad"]} | P.Unit: ${lector["precio_unitario"]:F2} | Subtotal: ${sub:F2}");
                    }
                }
                Console.WriteLine($" TOTAL DEL PEDIDO: ${total:F2}");

                Console.WriteLine("\n U1: Actualización - Aumentar 10% precio Categoría 1");
                string u1 = "UPDATE productos SET precio = precio * 1.10 WHERE categoria_id = @cat";
                using (var cmd = new MySqlCommand(u1, conn, tran))
                {
                    cmd.Parameters.AddWithValue("@cat", 1);
                    int filas = cmd.ExecuteNonQuery();
                    Console.WriteLine($" Actualizadas: {filas} filas | Precios aumentados 10%");
                }

                Console.WriteLine("\n D1: Eliminación - Borrar línea Pedido 1 - Producto 1");
                string d1 = "DELETE FROM detalle_pedido WHERE pedido_id = @ped AND producto_id = @prod";
                using (var cmd = new MySqlCommand(d1, conn, tran))
                {
                    cmd.Parameters.AddWithValue("@ped", 1);
                    cmd.Parameters.AddWithValue("@prod", 1);
                    int filas = cmd.ExecuteNonQuery();
                    Console.WriteLine($" Borradas: {filas} filas");
                }

                tran.Commit();
                Console.WriteLine(" Todas las operaciones confirmadas");
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
            Console.WriteLine("\n=== RF5: DEMOSTRACIÓN DE TRANSACCIÓN Y ROLLBACK ===");
            using var conn = new MySqlConnection(_conexion + "Database=practico;");
            conn.Open();

            decimal antes;
            using (var cmd = new MySqlCommand("SELECT precio FROM productos WHERE id = 1", conn))
                antes = Convert.ToDecimal(cmd.ExecuteScalar());
            Console.WriteLine($" Precio ANTES de la transacción: ${antes:F2}");

            try
            {
                using var tran = conn.BeginTransaction();
                using var cmdUpd = new MySqlCommand("UPDATE productos SET precio = 999.99 WHERE id = 1", conn, tran);
                cmdUpd.ExecuteNonQuery();
                Console.WriteLine(" Precio cambiado temporalmente a 999.99 (solo en memoria)");
                throw new Exception(" ERROR SIMULADO: Algo salió mal, se deshace todo");
            }
            catch (Exception ex)
            {
                Console.WriteLine($" Excepción capturada: {ex.Message}");
                Console.WriteLine(" Se ejecuta ROLLBACK... se deshace el cambio");
            }

            decimal despues;
            using (var cmd = new MySqlCommand("SELECT precio FROM productos WHERE id = 1", conn))
                despues = Convert.ToDecimal(cmd.ExecuteScalar());
            Console.WriteLine($" Precio DESPUÉS: ${despues:F2}");

            if (antes == despues)
                Console.WriteLine(" CORRECTO: Rollback funcionó. El dato NO cambió en la base.");
            else
                Console.WriteLine(" FALLO: El dato cambió, algo salió mal.");
        }
    }
}
