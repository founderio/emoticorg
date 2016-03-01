using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using Semver;

namespace Emoticorg
{
    public class EmoticonStore
    {
        public const string VERSION = "1.1";
        public SemVersion CURRENT_VERSION = SemVersion.Parse(VERSION);

        private string loadedVersionString;
        private SemVersion loadedVersion;

        private bool readable = false;
        private bool needsUpgrade = false;

        public static EmoticonStore openStore(string file)
        {
            DbConnection conn = openSQLite(file);
            return new EmoticonStore(conn);
        }

        public static DbConnection openSQLite(string file)
        {
			return SQLiteAdapter.Connect (file);
        }

        private DbConnection conn;

        public EmoticonStore(DbConnection conn)
        {
            this.conn = conn;
            if (!CheckTableExists("Emoticorg"))
            {
                InitDatabase();
            }
            RefreshVersion();
        }

		/// <summary>
		/// Reloads the version from the database & populates the status info for readable & needsUpgrade.
		/// </summary>
        private void RefreshVersion()
        {
            using (DbCommand command = conn.CreateCommand())
            {
                command.CommandText = "SELECT value FROM Emoticorg WHERE name='version';";
                loadedVersionString = (string)command.ExecuteScalar();
            }
            this.loadedVersion = SemVersion.Parse(loadedVersionString);
            if (loadedVersion == CURRENT_VERSION)
            {
                readable = true;
            }
            else if (loadedVersion < CURRENT_VERSION)
            {
                needsUpgrade = true;
                if (CanUpgrade(loadedVersionString))
                {
                    readable = true;
                }
            }
        }

        public bool IsReadable
        {
            get
            {
                return readable;
            }
        }

        public bool NeedsUpgrade
        {
            get
            {
                return needsUpgrade;
            }
        }

        public SemVersion LoadedVersion
        {
            get
            {
                return loadedVersion;
            }
        }

        public string LoadedVersionString
        {
            get
            {
                return loadedVersionString;
            }
        }

        public static bool CanUpgrade(string version)
        {
            if (version == "1.0")
            {
                return true;
            }
            return false;
        }

        public bool CheckTableExists(string tableName)
        {
            using (DbCommand command = conn.CreateCommand())
            {
                command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='" + tableName + "';";
                return command.ExecuteScalar() != null;
            }

        }

        private void InitDatabase()
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

                        command.CommandText = "CREATE TABLE Emoticon (guid TEXT, name TEXT, category TEXT, data BLOB, lastUsed INTEGER, type INTEGER, " +
                        "parentGuid TEXT, keyboardEquivalent TEXT, keyboardRegex TEXT, flags INTEGER, " +
                        "PRIMARY KEY(guid));";
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

        public void Upgrade()
        {
            RefreshVersion();
            if (loadedVersionString == "1.0")
            {
                using (DbTransaction transaction = conn.BeginTransaction())
                {
                    try
                    {

                        using (DbCommand command = conn.CreateCommand())
                        {


                            command.CommandText = "ALTER TABLE Emoticon ADD COLUMN parentGuid TEXT;";
                            command.ExecuteNonQuery();
                            command.CommandText = "ALTER TABLE Emoticon ADD COLUMN keyboardEquivalent TEXT;";
                            command.ExecuteNonQuery();
                            command.CommandText = "ALTER TABLE Emoticon ADD COLUMN keyboardRegex TEXT;";
                            command.ExecuteNonQuery();
                            command.CommandText = "ALTER TABLE Emoticon ADD COLUMN flags INTEGER;";
                            command.ExecuteNonQuery();


                            command.CommandText = "UPDATE Emoticorg SET value = '" + VERSION + "' WHERE name='version';";
                            command.ExecuteNonQuery();
                            command.CommandText = "INSERT INTO Emoticorg (name, value) VALUES ('upgradeTo" + VERSION + "', '" + loadedVersionString + "');";
                            command.ExecuteNonQuery();
                        }

                        transaction.Commit();

                        RefreshVersion();
                    }
                    catch
                    {
                        transaction.Rollback();
                    }
                }
            }
            else
            {
                throw new InvalidOperationException("Cannot upgrade this version.");
            }
        }

        private int CountQuery(string table, string query, string filter)
        {
            using (DbCommand command = conn.CreateCommand())
            {
                string commandText = "SELECT COUNT(guid) FROM " + table + " " + query;

				// Strip the ORDER BY clause from the query, as count() does not support that.
				int pos = commandText.IndexOf("ORDER BY", StringComparison.InvariantCultureIgnoreCase);
                if (pos > -1)
                {
                    commandText = commandText.Substring(0, pos);
                }
                command.CommandText = commandText;
                if (filter != null)
                {
					command.Parameters.Add(SQLiteAdapter.NewParameter("filter", '%' + filter + '%'));
                }

                return (int)(long)command.ExecuteScalar();
            }
        }

        public int CountQueryEmoticons(string query, string filter)
        {
            return this.CountQuery("Emoticon", query, filter);
        }

        /// <summary>
        /// Queries emoticons from the database, using offset &amp; count.
        /// The parameters are inserted as query like so: 'SELECT * FROM Emoticon [query] LIMIT [count] OFFSET [offset]'.
        /// </summary>
        /// <param name="query">A query string, something along the lines of 'WHERE something ORDER BY else ASC'</param>
        /// <param name="filter">If non-null, this adds a parameter value for 'filter'. This parameter has to be in the query for this to work.</param>
        /// <param name="offset">Zero-based index to offset the results</param>
        /// <param name="count">Amount of emoticons to query</param>
        /// <returns></returns>
        public List<Emoticon> PartialQueryEmoticons(string query, string filter, int offset, int count)
        {
            using (DbCommand command = conn.CreateCommand())
            {
				command.CommandText = string.Format("SELECT * FROM Emoticon {0} LIMIT {1} OFFSET {2}",  query , count, offset);
                if (filter != null)
                {
					command.Parameters.Add(SQLiteAdapter.NewParameter("filter", '%' + filter + '%'));
                }
                using (DbDataReader reader = command.ExecuteReader())
                {
                    return ParseEmoticonResult(reader);
                }
            }
        }

		/// <summary>
		/// Read all emoticon categories from the database.
		/// </summary>
		/// <returns>The categories.</returns>
		public List<string> GetCategories() {
			using (DbCommand command = conn.CreateCommand ()) {
				command.CommandText = "SELECT DISTINCT category FROM Emoticon ORDER BY category ASC";
				List<string> groups = new List<string> ();
				using (DbDataReader reader = command.ExecuteReader()) {
					while (reader.Read ()) {
						string group = ReadString (reader, "category", "");
						if ("" != group) {
							groups.Add (group);
						}
					}
				}
				return groups;
			}
		}

        private string ReadString(DbDataReader reader, string column, string defaultValue)
        {
            int ordinal = reader.GetOrdinal(column);
            if (reader.IsDBNull(ordinal))
            {
                return defaultValue;
            }
            else
            {
                return reader.GetString(ordinal);
            }
        }

        private int ReadInt(DbDataReader reader, string column, int defaultValue)
        {
            int ordinal = reader.GetOrdinal(column);
            if (reader.IsDBNull(ordinal))
            {
                return defaultValue;
            }
            else
            {
                return reader.GetInt32(ordinal);
            }
        }

        private long ReadLong(DbDataReader reader, string column, long defaultValue)
        {
            int ordinal = reader.GetOrdinal(column);
            if (reader.IsDBNull(ordinal))
            {
                return defaultValue;
            }
            else
            {
                return reader.GetInt64(ordinal);
            }
        }

        /// <summary>
        /// Reads database return values into a list of Emoticons.
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public List<Emoticon> ParseEmoticonResult(DbDataReader reader)
        {
            List<Emoticon> emoticons = new List<Emoticon>();
            while (reader.Read())
            {
                Emoticon emot = new Emoticon();

                string guid = ReadString(reader, "guid", "");
                string name = ReadString(reader, "name", "");
                string category = ReadString(reader, "category", "");


                string parentGuid = ReadString(reader, "parentGuid", "");
                string keyboardEquivalent = ReadString(reader, "keyboardEquivalent", "");
                string keyboardRegex = ReadString(reader, "keyboardRegex", "");
                int flags = ReadInt(reader, "flags", 0);

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
                long lastUsed = ReadLong(reader, "lastUsed", 0);
                int type = ReadInt(reader, "type", -1);


                if (keyboardEquivalent != null)
                {
                    string[] keyboardEquivalentSplit = keyboardEquivalent.Split(';');

                    for (int i = 0; i < keyboardEquivalentSplit.Length; i++)
                    {
                        keyboardEquivalentSplit[i] = keyboardEquivalentSplit[i].Replace(":,", ";").Replace("::", ":");
                    }
                    emot.keyboardEquivalent = keyboardEquivalentSplit;
                }
                else
                {
                    emot.keyboardEquivalent = new string[0];
                }

                emot.guid = guid;
                emot.name = name;
                emot.category = category;
                emot.type = type;
                emot.data = data;
                emot.lastUsed = lastUsed;
                emot.parentGuid = parentGuid;
                emot.keyboardRegex = keyboardRegex;
                emot.flags = emot.flags;

                emoticons.Add(emot);
            }
            return emoticons;
        }

        /// <summary>
        /// Updates the database with the passed emoticon or creates a new one.
        /// A new emoticon is created when the emoticon GUID is null.
        /// </summary>
        /// <param name="emot"></param>
        public void UpdateEmoticon(Emoticon emot)
        {
            using (DbCommand command = conn.CreateCommand())
            {
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
				command.Parameters.Add(SQLiteAdapter.NewParameter("guid", emot.guid));
				command.Parameters.Add(SQLiteAdapter.NewParameter("name", emot.name));
				command.Parameters.Add(SQLiteAdapter.NewParameter("category", emot.category));
				command.Parameters.Add(SQLiteAdapter.NewParameter("type", emot.type));
				command.Parameters.Add(SQLiteAdapter.NewParameter("data", emot.data));
				command.Parameters.Add(SQLiteAdapter.NewParameter("parentGuid", emot.parentGuid));
				command.Parameters.Add(SQLiteAdapter.NewParameter("keyboardRegex", emot.keyboardRegex));
				command.Parameters.Add(SQLiteAdapter.NewParameter("flags", emot.flags));

                string[] keyboardEquivalent = emot.keyboardEquivalent;

                if (keyboardEquivalent != null && keyboardEquivalent.Length > 0)
                {
                    StringBuilder strBuilder = new StringBuilder(keyboardEquivalent[0].Replace(":", "::").Replace(";", ":,"));
                    for (int i = 1; i < keyboardEquivalent.Length; i++)
                    {
                        strBuilder.Append(';').Append(keyboardEquivalent[i].Replace(":", "::").Replace(";", ":,"));
                    }
					command.Parameters.Add(SQLiteAdapter.NewParameter("keyboardEquivalent", strBuilder.ToString()));
                }
                else
                {
					command.Parameters.Add(SQLiteAdapter.NewParameter("keyboardEquivalent", ""));
                }

                // Only save lastUsed when we actually have used it. Zero means unused
                if (emot.lastUsed > 0)
                {
					command.Parameters.Add(SQLiteAdapter.NewParameter("lastUsed", emot.lastUsed));
                }
                else
                {
					command.Parameters.Add(SQLiteAdapter.NewParameter("lastUsed", DBNull.Value));
                }
                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Marks an emoticon last usage using the current date.
        /// </summary>
        /// <param name="guid"></param>
        public void UseEmoticon(string guid)
        {
            using (DbCommand command = conn.CreateCommand())
            {
                command.CommandText = "UPDATE Emoticon SET lastUsed=@lastUsed WHERE guid=@guid";
				command.Parameters.Add(SQLiteAdapter.NewParameter("guid", guid));
				command.Parameters.Add(SQLiteAdapter.NewParameter("lastUsed", DateTime.Now.Ticks));
                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Closes the underlying database connection.
        /// </summary>
        public void Close()
        {
            conn.Close();
        }
    }
}
