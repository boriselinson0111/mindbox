using System;
using System.ComponentModel;
using System.Configuration;
using System.Linq;

namespace CompanyName.Migrations {
	internal static class Program {
		private const string DatabaseServer = "db-server";
		private const string DatabaseName = "db-name";
		private const string DatabaseCredentials = "db-creds";
		private const string StorageFolder = "storage-folder";

		private static void HelpUsage() {
			Console.WriteLine($"Usage: mig [--version] [--help] [--list] [--{DatabaseServer}=<value>]");
			Console.WriteLine($"           [--{DatabaseName}=<value>] [--{DatabaseCredentials}=<value>]");
			Console.WriteLine($"           [--{StorageFolder}=<value>] [--up=\"migration_name\"]");
			Console.WriteLine();
			Console.WriteLine("These are common Mig commands used in various situations:");
			Console.WriteLine();
			Console.WriteLine("     --list             lists all available migrations");
			Console.WriteLine("     --up               perform upgrade process");
			Console.WriteLine("     --db-server        specify DatabaseServer");
			Console.WriteLine("     --db-name          specify DatabaseName");
			Console.WriteLine("     --db-creds         specify DatabaseCredentials");
			Console.WriteLine("     --storage-folder   specify StorageFolder");
		}

		private static int Main(string[] args) {
			if (args.Length == 0) {
				HelpUsage();
				return -1;
			}

			string migrationName = null;
			foreach (var arg in args) {
				switch (arg) {
					case "--version":
						Console.WriteLine("mig version 1.0");
						return 0;
					case "--help":
						HelpUsage();
						return 0;
					case "--list": {
						GetMigrationTypes()
							.GetMigrationNames()
							.ToList()
							.ForEach(Console.WriteLine);
						return 0;
					}
					case string x when x.StartsWith(OptStr("up")):
						migrationName = x.Substring(OptStr("up").Length);
						Console.WriteLine($"up={migrationName}");
						break;
					case string x when x.StartsWith(OptStr(DatabaseServer)):
						ConfigurationManager.AppSettings[nameof(DatabaseServer)] = x.Substring(OptStr(DatabaseServer).Length);
						Console.WriteLine($"{nameof(DatabaseServer)}={ConfigurationManager.AppSettings[nameof(DatabaseServer)]}");
						break;
					case string x when x.StartsWith(OptStr(DatabaseName)):
						ConfigurationManager.AppSettings[nameof(DatabaseName)] = x.Substring(OptStr(DatabaseName).Length);
						Console.WriteLine($"{nameof(DatabaseName)}={ConfigurationManager.AppSettings[nameof(DatabaseName)]}");
						break;
					case string x when x.StartsWith(OptStr(DatabaseCredentials)):
						ConfigurationManager.AppSettings[nameof(DatabaseCredentials)] = x.Substring(OptStr(DatabaseCredentials).Length);
						Console.WriteLine($"{nameof(DatabaseCredentials)}={ConfigurationManager.AppSettings[nameof(DatabaseCredentials)]}");
						break;
					case string x when x.StartsWith(OptStr(StorageFolder)):
						ConfigurationManager.AppSettings[nameof(StorageFolder)] = x.Substring(OptStr(StorageFolder).Length);
						Console.WriteLine($"{nameof(StorageFolder)}={ConfigurationManager.AppSettings[nameof(StorageFolder)]}");
						break;
					default:
						HelpUsage();
						return -1;
				}
			}

			if (migrationName == null) {
				Console.WriteLine("error: missing migration_name");
				return -1;
			}
			Type[] migrationTypes = GetMigrationTypes();
			int index = Array.IndexOf(migrationTypes.GetMigrationNames(), migrationName);
			if (index == -1) {
				Console.WriteLine($"error: bad migration_name = {migrationName}");
				return -1;
			}
			try {
				var migration = (IMigration)Activator.CreateInstance(migrationTypes[index]);
				migration.Up();
			} catch (Exception e) {
				Console.WriteLine($"error: Exception {e}");
				return -1;
			}
			Console.WriteLine("Migration completed");
			return 0;
		}

		private static string OptStr(string optName) {
			return "--" + optName + "=";
		}

		private static Type[] GetMigrationTypes() {
			return AppDomain.CurrentDomain
			                .GetAssemblies()
			                .SelectMany(x => x.GetTypes())
			                .Where(x => typeof(IMigration).IsAssignableFrom(x) &&
			                            !x.IsAbstract)
			                .ToArray();
		}

		private static string[] GetMigrationNames(this Type[] migrationTypes) {
			return migrationTypes
			       .Select(GetMigrationName)
			       .Where(x => !string.IsNullOrEmpty(x))
			       .ToArray();
		}

		private static string GetMigrationName(this Type type) {
			if (typeof(IMigration).IsAssignableFrom(type) &&
			    !type.IsAbstract) {
				var descriptions = (DescriptionAttribute[])
					type.GetCustomAttributes(typeof(DescriptionAttribute), false);

				if (descriptions.Length == 0) {
					return null;
				}
				return descriptions[0].Description;
			}
			return null;
		}
	}
}