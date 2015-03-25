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

                        command.CommandText = "CREATE TABLE Emoticon (guid TEXT, name TEXT, category TEXT, data BLOB, lastUsed INTEGER, type INTEGER, PRIMARY KEY(guid));";
                        command.ExecuteNonQuery();

                        command.CommandText = "CREATE TABLE EmoticonKeywords (guid TEXT, keyword TEXT, PRIMARY KEY(guid, keyword));";
                        command.ExecuteNonQuery();
                    }

                    InsertTestData();

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                }
            }
        }

        private void InsertTestData()
        {
            using (DbCommand command = conn.CreateCommand())
            {

                command.CommandText = "INSERT INTO Emoticon (guid, name, category, lastUsed) VALUES ('someguid1', 'name ahjo', 'cat1', " + DateTime.Now.Ticks + ");";
                command.ExecuteNonQuery();

                command.CommandText = "INSERT INTO Emoticon (guid, name, category, lastUsed) VALUES ('someguid2', 'name sdfsd', 'cat2', " + DateTime.Now.Ticks + ");";
                command.ExecuteNonQuery();

                command.CommandText = "INSERT INTO Emoticon (guid, name, category, lastUsed) VALUES ('someguid3', 'name raherjo', 'cat1', " + DateTime.Now.Ticks + ");";
                command.ExecuteNonQuery();

                command.CommandText = "INSERT INTO Emoticon (guid, name, category, lastUsed) VALUES ('someguid4', 'name mnbv', 'cat2', " + DateTime.Now.Ticks + ");";
                command.ExecuteNonQuery();
            }
        }

        private int CountQuery(string table, string query)
        {
            using (DbCommand command = conn.CreateCommand())
            {
                string commandText = "SELECT COUNT(guid) FROM " + table + " " + query;
                int pos = commandText.IndexOf("ORDER BY", StringComparison.InvariantCultureIgnoreCase);
                if (pos > -1)
                {
                    commandText = commandText.Substring(0, pos);
                }
                command.CommandText = commandText;

                return (int)(long)command.ExecuteScalar();
            }
        }

        public int CountQueryEmoticons(string query)
        {
            return this.CountQuery("Emoticon", query);
        }

        public List<Emoticon> PartialQueryEmoticons(string query, int offset, int count)
        {
            using (DbCommand command = conn.CreateCommand())
            {
                command.CommandText = "SELECT * FROM Emoticon " + query + " LIMIT " + count + " OFFSET " + offset;
                using (DbDataReader reader = command.ExecuteReader())
                {
                    return ParseEmoticonResult(reader);
                }
            }
        }

        public List<Emoticon> ParseEmoticonResult(DbDataReader reader)
        {
            List<Emoticon> emoticons = new List<Emoticon>();
            while (reader.Read())
            {
                Emoticon emot = new Emoticon();

                string guid = reader.GetString(reader.GetOrdinal("guid"));
                string name = reader.GetString(reader.GetOrdinal("name"));
                string category = reader.GetString(reader.GetOrdinal("category"));
                object value = reader.GetValue(reader.GetOrdinal("data"));
                byte[] data;
                if (value == DBNull.Value)
                {
                    data = null;
                }
                else
                {
                    data = (byte[])value;
                }
                value = reader.GetValue(reader.GetOrdinal("lastUsed"));
                long lastUsed;
                if (value == DBNull.Value)
                {
                    lastUsed = 0;
                }
                else
                {
                    lastUsed = (long)value;
                }
                int type;
                value = reader.GetValue(reader.GetOrdinal("type"));
                if (value == DBNull.Value)
                {
                    type = -1;
                }
                else
                {
                    type = (int)(long)value;
                }

                emot.guid = guid;
                emot.name = name;
                emot.category = category;
                emot.type = type;
                emot.data = data;
                emot.lastUsed = lastUsed;

                emoticons.Add(emot);
            }
            return emoticons;
        }

        public void UpdateEmoticon(Emoticon emot)
        {
            using (DbCommand command = conn.CreateCommand())
            {
                //TODO: command.Prepare() + command.Parameters
                if (emot.guid == null)
                {
                    emot.guid = Guid.NewGuid().ToString();
                    emot.lastUsed = 0;
                    command.CommandText = "INSERT INTO Emoticon (guid, name, category, lastUsed, type, data) VALUES (@guid, @name, @category, @lastUsed, @type, @data);";
                    command.Prepare();
                }
                else
                {
                    command.CommandText = "UPDATE Emoticon SET name=@name, category=@category, lastUsed=@lastUsed, type=@type, data=@data WHERE guid=@guid;";
                    command.Prepare();

                }
                command.Parameters.Add(new SQLiteParameter("guid", emot.guid));
                command.Parameters.Add(new SQLiteParameter("name", emot.name));
                command.Parameters.Add(new SQLiteParameter("category", emot.category));
                command.Parameters.Add(new SQLiteParameter("type", emot.type));
                command.Parameters.Add(new SQLiteParameter("data", emot.data));
                if (emot.lastUsed > 0)
                {
                    command.Parameters.Add(new SQLiteParameter("lastUsed", emot.lastUsed));
                }
                else
                {
                    command.Parameters.Add(new SQLiteParameter("lastUsed", DBNull.Value));
                }
                command.ExecuteNonQuery();
            }
        }

        public void Close()
        {
            conn.Close();
        }
    }
}
