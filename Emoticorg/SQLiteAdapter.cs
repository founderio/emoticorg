using System;
using System.Data.Common;
using System.Data.SQLite;
using Mono.Data.Sqlite;

namespace Emoticorg
{
	/// <summary>
	/// Adapter to the available SQLite implementation.
	/// Automatically decides whether to use the Mono version shipped with the Mono Runtime
	/// or the Windows implementation shipped with this application.
	/// If running on Mono, the Mono implementation has priority, even on Windows.
	/// </summary>
	public static class SQLiteAdapter
	{
		private static bool isUsingMono = false;

		static SQLiteAdapter() {
			isUsingMono = IsRunningOnMono();
		}

		/// <summary>
		/// Create a new DbParameter depending on the available SQLite implementation.
		/// </summary>
		/// <returns>The parameter.</returns>
		/// <param name="name">Name of the parameter.</param>
		/// <param name="value">Value of the parameter.</param>
		public static DbParameter NewParameter(string name, object value) {
			if(isUsingMono) {
				return NewParameterMono (name, value);
			} else {
				return NewParameterDotNet (name, value);
			}
		}

		/// <summary>
		/// Internal. Creates a new DbParameter for the Mono implementation.
		/// </summary>
		/// <returns>The parameter.</returns>
		/// <param name="name">Name of the parameter.</param>
		/// <param name="value">Value of the parameter.</param>
		private static DbParameter NewParameterMono(string name, object value) {
			return new SqliteParameter(name, value);
		}

		/// <summary>
		/// Internal. Creates a new DbParameter for the .NET/Windows implementation.
		/// </summary>
		/// <returns>The parameter.</returns>
		/// <param name="name">Name of the parameter.</param>
		/// <param name="value">Value of the parameter.</param>
		private static DbParameter NewParameterDotNet(string name, object value) {
			return new SQLiteParameter(name, value);
		}


		/// <summary>
		/// Create a new DbConnection to the specified file, depending on the available SQLite implementation.
		/// </summary>
		/// <returns>The database connection.</returns>
		/// <param name="file">The SQLite File to connect to</param>
		public static DbConnection Connect(string file) {
			if(isUsingMono) {
				return ConnectMono (file);
			} else {
				return ConnectDotNet (file);
			}
		}

		/// <summary>
		/// Internal. Creates a new DbConnection for the .NET/Windows implementation.
		/// </summary>
		/// <returns>The database connection.</returns>
		/// <param name="file">The SQLite File to connect to</param>
		private static DbConnection ConnectDotNet(string file) {
			SQLiteConnectionStringBuilder strBuild = new SQLiteConnectionStringBuilder();
			strBuild.Add("Data Source", file);
			SQLiteConnection conn = new SQLiteConnection(strBuild.ToString());
			conn.Open();
			return conn;
		}

		/// <summary>
		/// Internal. Creates a new DbConnection for the Mono implementation.
		/// </summary>
		/// <returns>The database connection.</returns>
		/// <param name="file">The SQLite File to connect to</param>
		private static DbConnection ConnectMono(string file) {
			SqliteConnectionStringBuilder strBuild = new SqliteConnectionStringBuilder ();
			strBuild.Add("URI", "file:" + file);
			SqliteConnection conn = new SqliteConnection(strBuild.ToString());
			conn.Open();
			return conn;
		}

		/// <summary>
		/// Determines if we are running on mono.
		/// </summary>
		/// <returns><c>true</c> if we are running on mono; otherwise, <c>false</c>.</returns>
		public static bool IsRunningOnMono ()
		{
			return Type.GetType ("Mono.Runtime") != null;
		}

	}
}

