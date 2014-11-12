using System;
using System.Data;
using Dapper;

namespace DbRestore
{
  class Db
  {
    public static void RestoreDatabase(string sqlServerInstanceName, string backupFilePath, string dbName)
    {
      const string host = "localhost";

      Console.WriteLine("Restoring to " + sqlServerInstanceName);
      const string connectionStringFormat = "Data Source={0}\\{1}; Initial Catalog=master; Integrated Security=true";
      string connString = string.Format(connectionStringFormat, host, sqlServerInstanceName);

      const string commandTxt = @"declare @fileListTable table
(
    LogicalName          nvarchar(128),
    PhysicalName         nvarchar(260),
    [Type]               char(1),
    FileGroupName        nvarchar(128),
    Size                 numeric(20,0),
    MaxSize              numeric(20,0),
    FileID               bigint,
    CreateLSN            numeric(25,0),
    DropLSN              numeric(25,0),
    UniqueID             uniqueidentifier,
    ReadOnlyLSN          numeric(25,0),
    ReadWriteLSN         numeric(25,0),
    BackupSizeInBytes    bigint,
    SourceBlockSize      int,
    FileGroupID          int,
    LogGroupGUID         uniqueidentifier,
    DifferentialBaseLSN  numeric(25,0),
    DifferentialBaseGUID uniqueidentifier,
    IsReadOnl            bit,
    IsPresent            bit,
    TDEThumbprint        varbinary(32) -- remove this column if using SQL 2005
)
INSERT INTO @fileListTable EXEC('restore filelistonly from disk = ''{0}''')
DECLARE @databasename varchar(255)
DECLARE @logname varchar(255)
Set @databasename = (select LogicalName from @fileListTable WHERE Type = 'D')
Set @logname = (select LogicalName from @fileListTable WHERE Type = 'L')
IF 
(
	EXISTS (SELECT name FROM master.dbo.sysdatabases WHERE ('[' + name + ']' = '{1}' OR name = '{1}'))
)
BEGIN
	ALTER DATABASE [{1}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
	DROP DATABASE [{1}]
END
RESTORE DATABASE [{1}]
FROM DISK = '{0}'
WITH MOVE @databasename TO '{2}{1}.mdf', MOVE @logname TO '{3}{1}_log.ldf'";

      //E.g.: @"C:\Program Files\Microsoft SQL Server\MSSQL10_50.SQLSERVER2008\MSSQL\DATA\";
      string sqlDataFolder = ExecuteQuery(connString,
@"SELECT SUBSTRING(physical_name, 1, CHARINDEX(N'master.mdf', LOWER(physical_name)) - 1)
  FROM master.sys.master_files
  WHERE database_id = 1 AND file_id = 1");

      //E.g.: @"C:\Program Files\Microsoft SQL Server\MSSQL10_50.SQLSERVER2008\MSSQL\DATA\";
      string sqlLogFolder = ExecuteQuery(connString,
@"SELECT SUBSTRING(physical_name, 1, CHARINDEX(N'mastlog.ldf', LOWER(physical_name)) - 1)
  FROM master.sys.master_files
  WHERE database_id = 1 AND file_id = 2");

      string command = string.Format(commandTxt, backupFilePath, dbName, sqlDataFolder, sqlLogFolder);
      //Logger.Info("Restoring database {0}. For Application {0}", Name);
      //Logger.Info("Restore scriipt {0}", command);
      try
      {
        ExecuteNonQuery(connString, command);
        Console.WriteLine("Database {0} restored successfully.", dbName);
      }
      catch (Exception ex)
      {
        if (ex.Message.Contains("Reason: 15105"))
        {
          Console.Error.WriteLine("SQL Server does not have permission to read from specified directory. Please try to move the file to a different location.");
          return;
        }

        Console.Error.WriteLine("Database restore of {0} failed with the following message: {1}", dbName, ex.Message);
      }
    }

    private static string ExecuteQuery(string connectionString, string dbScript)
    {
      using (IDbConnection dbConnection = new System.Data.SqlClient.SqlConnection(connectionString))
      {
        return dbConnection.ExecuteScalar<string>(dbScript);
      }
    }

    private static void ExecuteNonQuery(string connectionString, string dbScript)
    {
      using (IDbConnection dbConnection = new System.Data.SqlClient.SqlConnection(connectionString))
      {
        dbConnection.Execute(dbScript);
      }
    }
  }
}
