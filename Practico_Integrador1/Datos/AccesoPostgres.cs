using System;
using Npgsql;
using Practico_Integrador1.Dominio;

namespace Practico_Integrador1.Datos
{
    public class AccesoPostgres : IAccesoDatos
    {
        public string NombreMotor => "PostgreSQL";

        private readonly string _conexionPractico =
            "Host=localhost;Port=5432;Username=postgres;Password=postgres;Database=practico;";

        public void CrearEstructura()
        {
            Console.WriteLine("\n=== RF2: CREAR ESTRUCTURA POSTGRES ===");
            try
            {
                using var conn = new NpgsqlConnection(_conexionPractico);
                conn.Open();

                using var cmd = new NpgsqlCommand(@"
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
                        email VARCHAR(100) NOT NULL UNIQUE
                    );

                    CREATE TABLE productos (
                        id SERIAL PRIMARY KEY,
                        nombre VARCHAR(100) NOT NULL,
                        precio NUMERIC(10,2) NOT NULL,
                        stock INT NOT NULL DEFAULT 0,
                        categoria_id INT NOT NULL REFERENCES categorias(id)
                    );

                    CREATE TABLE pedidos (
                        id SERIAL PRIMARY KEY,
                        cliente_id INT NOT NULL REFERENCES clientes(id) ON DELETE CASCADE,
                        fecha TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
                    );

                    CREATE TABLE detalle_pedido (
                        pedido_id INT NOT NULL REFERENCES pedidos(id) ON DELETE CASCADE,
                        producto_id INT NOT NULL REFERENCES productos(id),
                        cantidad INT NOT NULL,
                        precio_unitario NUMERIC(10,2) NOT NULL,
                        PRIMARY KEY (pedido_id, producto_id)
                    );
                ", conn);
                cmd.ExecuteNonQuery();

                Console.WriteLine("Base 'practico' creada.");
                Console.WriteLine("Estructura (5 tablas) creada.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
            }
        }

        public void InsertarDatosPrueba()
        {
            Console.WriteLine("\n=== RF3: INSERTAR DATOS POSTGRES ===");
            try
            {
                using var conn = new NpgsqlConnection(_conexionPractico);
                conn.Open();
                using var tran = conn.BeginTransaction();

                new NpgsqlCommand("TRUNCATE TABLE detalle_pedido, pedidos, productos, clientes, categorias RESTART IDENTITY CASCADE;", conn, tran).ExecuteNonQuery();

                new NpgsqlCommand("INSERT INTO categorias (nombre) VALUES ('Electrónica'),('Libros'),('Hogar');", conn, tran).ExecuteNonQuery();
                new NpgsqlCommand("INSERT INTO clientes (nombre,email) VALUES ('Juan Pérez','juan@mail.com'),('Ana Gómez','ana@mail.com');", conn, tran).ExecuteNonQuery();

                new NpgsqlCommand(@"
                    INSERT INTO productos (nombre,precio,stock,categoria_id) VALUES
                    ('Notebook 14""',850000,10,1),
                    ('Mouse inalámbrico',12000,50,1),
                    ('Teclado mecánico',35000,20,1),
                    ('Clean Code',28000,15,2),
                    ('Lámpara LED escritorio',15000,30,3);", conn, tran).ExecuteNonQuery();

                new NpgsqlCommand("INSERT INTO pedidos (cliente_id,fecha) VALUES (1,CURRENT_TIMESTAMP),(2,CURRENT_TIMESTAMP);", conn, tran).ExecuteNonQuery();

                new NpgsqlCommand(@"
                    INSERT INTO detalle_pedido (pedido_id,producto_id,cantidad,precio_unitario) VALUES
                    (1,1,1,850000),
                    (1,2,2,12000),
                    (1,3,1,35000),
                    (2,4,1,28000),
                    (2,5,2,15000);", conn, tran).ExecuteNonQuery();

                tran.Commit();
                Console.WriteLine("Datos de prueba insertados (commit).");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
            }
        }

        public void EjecutarOperaciones()
        {
            Console.WriteLine("\n=== RF4: OPERACIONES POSTGRES ===");
            using var conn = new NpgsqlConnection(_conexionPractico);
            conn.Open();
            using var tran = conn.BeginTransaction();

            Console.WriteLine("\n[C1] Productos con su categoría:");
            using var cmdC1 = new NpgsqlCommand(@"
                SELECT p.id,p.nombre,p.precio,c.nombre AS categoria
                FROM productos p INNER JOIN categorias c ON c.id=p.categoria_id
                ORDER BY p.id;", conn, tran);
            using var readerC1 = cmdC1.ExecuteReader();
            while (readerC1.Read())
                Console.WriteLine($"#{readerC1["id"]} {readerC1["nombre"]} → ${((decimal)readerC1["precio"]).ToString("F2")} [{readerC1["categoria"]}]");
            readerC1.Close();

            Console.WriteLine("\n[C2] Detalle y total del pedido #1:");
            using var cmdC2 = new NpgsqlCommand(@"
                SELECT pr.nombre,d.cantidad,d.precio_unitario,(d.cantidad*d.precio_unitario) AS subtotal
                FROM detalle_pedido d INNER JOIN productos pr ON pr.id=d.producto_id
                WHERE d.pedido_id=1;", conn, tran);
            using var readerC2 = cmdC2.ExecuteReader();
            decimal total = 0;
            while (readerC2.Read())
            {
                decimal subtotal = (decimal)readerC2["subtotal"];
                total += subtotal;
                Console.WriteLine($"{readerC2["nombre"]} x{readerC2["cantidad"]} @ ${((decimal)readerC2["precio_unitario"]).ToString("F2")} = ${subtotal.ToString("F2")}");
            }
            readerC2.Close();
            Console.WriteLine($"Total del pedido #1: ${total.ToString("F2")}");

            int filasU1 = new NpgsqlCommand("UPDATE productos SET precio=precio*1.1 WHERE categoria_id=1;", conn, tran).ExecuteNonQuery();
            Console.WriteLine($"\n[U1] Subí 10% precios de categoría #1 → {filasU1} filas.");

            int filasD1 = new NpgsqlCommand("DELETE FROM detalle_pedido WHERE pedido_id=1 AND producto_id=2;", conn, tran).ExecuteNonQuery();
            Console.WriteLine($"[D1] Borré línea (pedido 1, producto 2) → {filasD1} filas.");

            tran.Commit();
            Console.WriteLine("Operaciones confirmadas (commit).");
        }

        public void DemostrarRollback()
        {
            Console.WriteLine("\n=== RF5: DEMOSTRAR ROLLBACK POSTGRES ===");
            using var conn = new NpgsqlConnection(_conexionPractico);
            conn.Open();
            using var tran = conn.BeginTransaction();
            try
            {
                decimal precioAntes = (decimal)new NpgsqlCommand("SELECT precio FROM productos WHERE id=1;", conn, tran).ExecuteScalar();
                Console.WriteLine($"Precio del producto #1 ANTES: ${precioAntes.ToString("F2")}");

                new NpgsqlCommand("UPDATE productos SET precio=1 WHERE id=1;", conn, tran).ExecuteNonQuery();
                Console.WriteLine("UPDATE aplicado (precio → 1) dentro de la transacción.");

                throw new Exception("Error simulado: algo salió mal.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Excepción capturada → ROLLBACK. ({ex.Message})");
                tran.Rollback();
            }

            decimal precioDespues = (decimal)new NpgsqlCommand("SELECT precio FROM productos WHERE id=1;", conn).ExecuteScalar();
            Console.WriteLine($"Precio del producto #1 DESPUÉS: ${precioDespues.ToString("F2")}");
            Console.WriteLine("OK: el rollback funcionó, el dato NO cambió.");
        }
    }
}
