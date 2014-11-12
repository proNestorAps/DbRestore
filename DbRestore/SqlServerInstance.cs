using System;
using System.Collections.Generic;
using Microsoft.Win32;

namespace DbRestore
{
  class SqlServerInstance
  {
    public string Name { get; set; }
    public Version Version { get; set; }

    public override string ToString()
    {
      return Name ?? string.Empty;
    }

    public static IReadOnlyList<SqlServerInstance> GetInstalledInstances()
    {
      List<SqlServerInstance> instances = new List<SqlServerInstance>();
      instances.AddRange(GetSqlServerInstanceNamesFromRegistry(RegistryView.Registry32));
      if (Environment.Is64BitOperatingSystem)
      {
        instances.AddRange(GetSqlServerInstanceNamesFromRegistry(RegistryView.Registry64));
      }
      return instances.AsReadOnly();
    }

    private static IEnumerable<SqlServerInstance> GetSqlServerInstanceNamesFromRegistry(RegistryView registryView)
    {
      using (RegistryKey key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, registryView))
      {
        using (RegistryKey instanceNamesKey = key.OpenSubKey(@"SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL"))
        {
          foreach (string instanceName in instanceNamesKey.GetValueNames())
          {
            // E.g. HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Microsoft SQL Server\MSSQL10_50.SQLSERVER2008\MSSQLServer\CurrentVersion
            using (RegistryKey versionKey = key.OpenSubKey(string.Format("SOFTWARE\\Microsoft\\Microsoft SQL Server\\{0}\\MSSQLServer\\CurrentVersion", instanceName)))
            {
              Version version = new Version(versionKey.GetValue("CurrentVersion").ToString());
              yield return new SqlServerInstance { Name = instanceName, Version = version };
            }
          }
        }
      }
    }
  }
}
