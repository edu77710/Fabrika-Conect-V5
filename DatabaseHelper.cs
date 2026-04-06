using System;
using System.Data;
using System.Data.SQLite;
using System.IO;

namespace FotoEnvio
{
    public class DatabaseHelper
    {
        private static string _dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FotoEnvio", "clientes.db");

        public static void Initialize()
        {
            string dir = Path.GetDirectoryName(_dbPath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            using (var conn = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                conn.Open();
                string sql = @"
                    CREATE TABLE IF NOT EXISTS Clientes (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Nome TEXT NOT NULL,
                        Email TEXT,
                        Telefone TEXT,
                        Pasta TEXT,
                        DataCadastro TEXT DEFAULT (datetime('now','localtime'))
                    );
                    CREATE TABLE IF NOT EXISTS Fotos (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        ClienteId INTEGER,
                        NomeArquivo TEXT,
                        CaminhoLocal TEXT,
                        CaminhoServidor TEXT,
                        DataEnvio TEXT DEFAULT (datetime('now','localtime')),
                        FOREIGN KEY (ClienteId) REFERENCES Clientes(Id)
                    );";
                using (var cmd = new SQLiteCommand(sql, conn))
                    cmd.ExecuteNonQuery();
            }
        }

        public static int InserirCliente(string nome, string email, string telefone, string pasta)
        {
            using (var conn = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                conn.Open();
                string sql = "INSERT INTO Clientes (Nome, Email, Telefone, Pasta) VALUES (@nome, @email, @tel, @pasta); SELECT last_insert_rowid();";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@nome", nome);
                    cmd.Parameters.AddWithValue("@email", email ?? "");
                    cmd.Parameters.AddWithValue("@tel", telefone ?? "");
                    cmd.Parameters.AddWithValue("@pasta", pasta ?? "");
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        public static void InserirFoto(int clienteId, string nomeArquivo, string caminhoLocal, string caminhoServidor)
        {
            using (var conn = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                conn.Open();
                string sql = "INSERT INTO Fotos (ClienteId, NomeArquivo, CaminhoLocal, CaminhoServidor) VALUES (@cid, @nome, @local, @serv)";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@cid", clienteId);
                    cmd.Parameters.AddWithValue("@nome", nomeArquivo);
                    cmd.Parameters.AddWithValue("@local", caminhoLocal);
                    cmd.Parameters.AddWithValue("@serv", caminhoServidor);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static DataTable ListarClientes()
        {
            using (var conn = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                conn.Open();
                string sql = "SELECT Id, Nome, Email, Telefone, Pasta, DataCadastro FROM Clientes ORDER BY DataCadastro DESC";
                using (var da = new SQLiteDataAdapter(sql, conn))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    return dt;
                }
            }
        }
    }
}
