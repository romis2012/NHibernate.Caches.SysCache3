using System;
using System.Data;
using System.Data.SqlClient;
using System.Runtime.Caching;

namespace NHibernate.Caches.SysCache3
{
	public class SqlCommandCacheDependencyEnlister : ICacheDependencyEnlister
	{
		/// <summary>sql command to use for creating notifications</summary>
		private readonly string command;

        /// <summary>SQL command timeout. If null, the default is used.</summary>
        private readonly int? commandTimeout;

		/// <summary>The name of the connection string</summary>
		private readonly string connectionName;

		/// <summary>The connection string to use for connection to the date source</summary>
		private readonly string connectionString;

		/// <summary>indicates if the command is a stored procedure or not</summary>
		private readonly bool isStoredProcedure;

	    
		public SqlCommandCacheDependencyEnlister(string command, bool isStoredProcedure,
		                                         IConnectionStringProvider connectionStringProvider)
			: this(command, isStoredProcedure, null, null, connectionStringProvider) {}


        public SqlCommandCacheDependencyEnlister(string command, bool isStoredProcedure, 
            int? commandTimeout, string connectionName, IConnectionStringProvider connectionStringProvider)
		{
			//validate the parameters
			if (String.IsNullOrEmpty(command))
			{
				throw new ArgumentNullException("command");
			}

			if (connectionStringProvider == null)
			{
				throw new ArgumentNullException("connectionStringProvider");
			}

			this.command = command;
			this.isStoredProcedure = isStoredProcedure;
            this.commandTimeout = commandTimeout;
		    this.connectionName = connectionName;

			connectionString = String.IsNullOrEmpty(this.connectionName) ? connectionStringProvider.GetConnectionString() : connectionStringProvider.GetConnectionString(this.connectionName);

			SqlDependency.Start(connectionString);
		}

		#region ICacheDependencyEnlister Members

		public ChangeMonitor Enlist()
		{
			using (var connection = new SqlConnection(connectionString))
			{
				using (var exeCommand = new System.Data.SqlClient.SqlCommand(command, connection))
				{
					//is the command a sproc
					if (isStoredProcedure)
					{
						exeCommand.CommandType = CommandType.StoredProcedure;
					}

                    if (commandTimeout.HasValue)
				        exeCommand.CommandTimeout = this.commandTimeout.Value;

					var dependency = new SqlDependency(exeCommand);
                    var monitor = new SqlChangeMonitor(dependency);

					connection.Open();
					exeCommand.ExecuteNonQuery();

					return monitor;

				}
			}
		}

		#endregion
	}
}