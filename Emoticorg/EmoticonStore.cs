using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.Linq;
using System.Text;

namespace Emoticorg
{
    public class EmoticonStore
    {
        public const string VERSION = "1.0";

        public static EmoticonStore openStore(string file)
        {
            DbConnection conn = openSQLite(file);
            return new EmoticonStore(conn);
        }

        public static DbConnection openSQLite(string file)
        {
            SQLiteConnectionStringBuilder strBuild = new SQLiteConnectionStringBuilder();
            strBuild.Add("Data Source", file);
            SQLiteConnection conn = new SQLiteConnection(strBuild.ToString());
            conn.Open();
            return conn;
        }

        private DbConnection conn;

        public EmoticonStore(DbConnection conn)
        {
            this.conn = conn;
            if (!CheckTableExists("Emoticorg"))
            {
                InitDatabase();
            }
        }

        public bool CheckTableExists(string tableName)
        {
            using (DbCommand command = conn.CreateCommand())
            {
                command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='" + tableName + "';";
                return command.ExecuteScalar() != null;
            }

        }

        public void InitDatabase()
        {
            using (DbTransaction transaction = conn.BeginTransaction())
            {
                try
                {

                    using (DbCommand command = conn.CreateCommand())
                    {
                        command.CommandText = "CREATE TABLE Emoticorg (name TEXT, value TEXT, PRIMARY KEY(name));";
                        command.ExecuteNonQuery();

                        command.CommandText = "INSERT INTO Emoticorg (name, value) VALUES ('version', '" + VERSION + "');";
                        command.ExecuteNonQuery();

                        command.CommandText = "CREATE TABLE Emoticon (guid TEXT, name TEXT, category TEXT, data BLOB, lastUsed INTEGER, PRIMARY KEY(guid));";
                        command.ExecuteNonQuery();

                        command.CommandText = "CREATE TABLE EmoticonKeywords (guid TEXT, keyword TEXT, PRIMARY KEY(guid, keyword));";
                        command.ExecuteNonQuery();
                    }

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                }
            }
        }

        public void Close()
        {
            conn.Close();
        }
    }
}
