using System;
using CommandLine;

namespace DbRestore
{
  class Program
  {
    static void Main(string[] args)
    {
      var options = new Options();
      if (Parser.Default.ParseArguments(args, options))
      {
        if (options.Validate())
        {
          try
          {
            Db.RestoreDatabase(options.InstanceName, options.BackupFile, options.Name);
          }
          catch (Exception ex)
          {
            Console.Error.WriteLine("Unexpected error occurred while restoring: " + ex.Message);
          }
        }
      }
    }
  }
}
