using System;
using System.Collections.Generic;
using System.CommandLine.DragonFruit;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Text.Json;

namespace DbScriptGenerator
{

    public static class Exensions
    {
        private static readonly Random Rand = new Random();

        public static T RandomValue<T>(this T[] arr)
            => arr.Length == 0 ? throw new ArgumentException("Arr can not be empty") : arr[Rand.Next(0, arr.Length)];
    }
    class Program
    {
        private static readonly Random Rand = new Random();
        private static string DatabaseName = "FatSchemaTestDatabase";
        private static string[] ColumnTypes = { "int", "smallint", "bigint", "varchar(255)", "bit" };

        /// <summary>
        /// Generates a DB script to fill a database with a various amount of schema
        /// </summary>
        /// <param name="numTables"></param>
        /// <param name="numColumnsPerTable"></param>
        /// <param name="numIndexesPerTable"></param>
        static void Main(int numTables = 10, int numColumnsPerTable = 5, int numIndexesPerTable = 3, int numRowsOfData = 1)
        {
            var names = new NameGenerator().RandomNames();

            var generated_tables = names.Take(numTables)
                                        .Select(tableName => (tableName, GenerateTable(tableName, numColumnsPerTable, numIndexesPerTable, names)))
                                        .Select(s => new { s.tableName, s.Item2.columnTypes, s.Item2.columnNames, tableScript = s.Item2.table })
                                        .ToList();

            var creationString = new StringBuilder().AppendLine(GenerateCheckDropCreateDatabase())
                                                    .AppendJoin(Environment.NewLine, generated_tables.Select(s => s.tableScript)).ToString();

            var tableCreation = Path.GetTempFileName();
            var dataSeed = Path.GetTempFileName();

            File.WriteAllText(tableCreation, creationString);

            int chunkSize = Convert.ToInt32(Math.Floor((double)numRowsOfData / numColumnsPerTable));
            foreach (var generatedSet in generated_tables)
            {
                var numLoopedIterations = chunkSize == 0 ? 0 : numRowsOfData / chunkSize;
                for (int i = 0; i < numLoopedIterations; i++)
                {
                    var dataInsert = GenerateTableInsert(generatedSet.tableName, generatedSet.columnNames.Skip(1).ToArray(),
                        generatedSet.columnTypes.Skip(1).ToArray(), chunkSize);

                    File.AppendAllText(dataSeed, dataInsert);
                }

                var finalInsert= GenerateTableInsert(generatedSet.tableName, generatedSet.columnNames.Skip(1).ToArray(),
                    generatedSet.columnTypes.Skip(1).ToArray(), numRowsOfData % 1000);

                File.AppendAllText(dataSeed, finalInsert);
            }

            Process.StartProcess("notepad", tableCreation);
            Process.StartProcess("notepad", dataSeed);
        }

        private static string GenerateTableInsert(string tableName, string[] columnNames, string[] columnTypes, int numRows)
            => $@"
INSERT INTO [dbo].[{tableName}]
(
{GenerateColumnListing(columnNames)}
)
VALUES
{string.Join(Environment.NewLine, Enumerable.Range(0, numRows).Select((s, i) => (i == 0 ? string.Empty : ",") + GenerateDataInserts(columnTypes)))}
GO

";

        private static string GenerateColumnListing(string[] columnNames)
            => string.Join(Environment.NewLine, columnNames.Select((s, i) => (i == 0 ? string.Empty : ",") + "[" + s + "]"));

        private static string GenerateDataInserts(string[] columnTypes)
            => "(" + string.Join(Environment.NewLine, columnTypes.Select((s, i) => (i == 0 ? string.Empty : ",") + GenerateDataInsert(s))) + ")";

        private static string GenerateDataInsert(string columnType)
            => columnType switch
               {
                   "int" => Rand.Next(int.MaxValue).ToString(),
                   "smallint" => Rand.Next(short.MaxValue).ToString(),
                   "bigint" => Rand.Next(int.MaxValue).ToString(),
                   "varchar(255)" => $"'{Guid.NewGuid()}'",
                   "bit" => Rand.Next(1) == 1 ? "0" : "1"
               };

        private static string GenerateCheckDropCreateDatabase()
            => $@"
USE master
IF EXISTS(select * from sys.databases where name='{DatabaseName}') BEGIN
ALTER DATABASE {DatabaseName} SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
DROP DATABASE {DatabaseName}
END
GO
CREATE DATABASE {DatabaseName}
GO
USE {DatabaseName}
GO
";

        private static (string table, string[] columnNames, string[] columnTypes) GenerateTable(string tableName, int numColumns, int numIndexesPerTable, IEnumerable<string> names)
            => (new StringBuilder().AppendLine($"CREATE TABLE {tableName} (")
                                  .AppendJoin(Environment.NewLine, GenerateColumns(numColumns, names, out var columnNames, out var columnTypes))
                                  .AppendLine()
                                  .AppendJoin(Environment.NewLine, GeneratePrimaryKey(tableName, columnNames[0]))
                                  .AppendJoin(Environment.NewLine, GenerateIndexes(tableName, numIndexesPerTable, columnNames.Skip(1).ToArray()))
                                  .AppendLine(");")
                                  .AppendLine("GO")
                                  .AppendLine()
                                  .ToString(), columnNames, columnTypes);

        private static IEnumerable<string> GenerateColumns(int numColumns, IEnumerable<string> names, out string[] columnNames, out string[] columnTypes)
        {
            columnNames = names.Take(numColumns).ToArray();

            var columns = Enumerable.Range(0, numColumns)
                                    .Select((s, i) => i == 0 ? "int" : ColumnTypes.RandomValue())
                                    .ToArray();

            columnTypes = columns;

            return columnNames.Select((s, i) => $"{(i == 0 ? string.Empty : ",")}{s} {columns[i]} NOT NULL" + (i == 0 ? " IDENTITY(1,1)" : string.Empty));
        }

        private static string GeneratePrimaryKey(string tableName, string columnName)
            => $"CONSTRAINT PK_{tableName} PRIMARY KEY CLUSTERED ({columnName})";

        private static IEnumerable<string> GenerateIndexes(string tableName, int numIndexesPerTable, string[] columnNames)
            => columnNames.Select(s => $",INDEX IX_{tableName}_{s} NONCLUSTERED ({s})").TakeWhile((s, i) => i <= numIndexesPerTable);
    }

    class NameGenerator
    {
        private HashSet<string> _usedNames = new HashSet<string>();
        private string[] _animals;
        private string[] _adjectives;

        public NameGenerator()
        {
            _animals = File.ReadAllLines("animals.txt");
            _adjectives = File.ReadAllLines("adjectives.txt");
        }

        public IReadOnlyCollection<string> UsedNames() => _usedNames;

        public IEnumerable<string> RandomNames()
        {
            for (int i = 0; i < int.MaxValue; i++)
            {
                string? nextName = null;

                while (nextName == null || _usedNames.Contains(nextName))
                    nextName = $"{_adjectives.RandomValue()}{_adjectives.RandomValue()}{_animals.RandomValue()}";

                _usedNames.Add(nextName);

                yield return nextName;
            }
        }
    }
}
