using System;
using System.Data.Common;
using System.Data.SQLite;
using Mono.Data.Sqlite;

namespace Emoticorg
{
	public static class SQLiteAdapter
	{
		private static bool isUsingMono = false;

		static SQLiteAdapter() {
			isUsingMono = IsRunningOnMono();
		}

		public static DbParameter NewParameter(string name, object value) {
			if(isUsingMono) {
				return NewParameterMono (name, value);
			} else {
				return NewParameterDotNet (name, value);
			}
		}

		private static DbParameter NewParameterMono(string name, object value) {
			return new SqliteParameter(name, value);
		}

		private static DbParameter NewParameterDotNet(string name, object value) {
			return new SQLiteParameter(name, value);
		}


		public static DbConnection Connect(string file) {
			if(isUsingMono) {
				return ConnectMono (file);
			} else {
				return ConnectDotNet (file);
			}
		}

		private static DbConnection ConnectDotNet(string file) {
			SQLiteConnectionStringBuilder strBuild = new SQLiteConnectionStringBuilder();
			strBuild.Add("Data Source", file);
			SQLiteConnection conn = new SQLiteConnection(strBuild.ToString());
			conn.Open();
			return conn;
		}

		private static DbConnection ConnectMono(string file) {
			SqliteConnectionStringBuilder strBuild = new SqliteConnectionStringBuilder ();
			strBuild.Add("URI", "file:" + file);
			SqliteConnection conn = new SqliteConnection(strBuild.ToString());
			conn.Open();
			return conn;
		}

		public static bool IsRunningOnMono ()
		{
			return Type.GetType ("Mono.Runtime") != null;
		}

		public static bool IsMonoSqliteAvailable() {
			return Type.GetType ("Mono.Data.Sqlite.SqliteConnection") != null;
		}

	}
}

