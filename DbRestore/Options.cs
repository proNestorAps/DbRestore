using System;
using System.IO;
using System.Linq;
using CommandLine;
using CommandLine.Text;

namespace DbRestore
{
  class Options
  {
    [Option('f', "file", Required = true, HelpText = "Backup file to restore.")]
    public string BackupFile { get; set; }

    [Option('n', "name", Required = false, HelpText = "The name the database is restored as.")]
    public string Name { get; set; }

    [Option('i', "instance", Required = false, HelpText = "The name of the SQL Server instance to restore on.")]
    public string InstanceName { get; set; }

    [HelpOption]
    public string GetUsage()
    {
      return HelpText.AutoBuild(this,
        (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
    }

    public bool Validate()
    {
      string path = BackupFile;

      if (!Path.IsPathRooted(path))
      {
        path = Path.Combine(Directory.GetCurrentDirectory(), path);
      }

      if (!File.Exists(path))
      {
        if (Directory.Exists(path))
        {
          Console.Error.WriteLine("The specified file is directory: " + path);
        }
        else
        {
          Console.Error.WriteLine("Could not find the file: " + path);
        }
        return false;
      }

      BackupFile = path;

      var instances = SqlServerInstance.GetInstalledInstances();
      if (instances.Count == 0)
      {
        Console.Error.WriteLine("Could not find any SQL Server instances on this machine.");
        return false;
      }

      SqlServerInstance selectedInstance = null;
      if (!string.IsNullOrEmpty(InstanceName))
      {
        selectedInstance = instances.SingleOrDefault(i => i.Name.Equals(InstanceName, StringComparison.OrdinalIgnoreCase));
        if (selectedInstance == null)
        {
          Console.Error.WriteLine("Could not find any SQL Server with the instance name " + InstanceName);
          return false;
        }
      }

      if (selectedInstance == null)
      {
        selectedInstance = instances.OrderByDescending(x => x.Version).First();
        Console.Write("No instance name was provided. ");
        if (instances.Count == 1)
        {
          Console.WriteLine("Using the only instance installed on this machine:");
        }
        else
        {
          Console.WriteLine("Using the newest version, since more than one SQL Server instance is installed on this machine:");
        }
      }

      InstanceName = selectedInstance.Name;

      string dbName = Name ?? Path.GetFileNameWithoutExtension(path);
      Name = dbName;

      return true;
    }
  }

}
