using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace DatabaseCreator
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("Запуск программы");
            for (int i = 0; i < 3; i++) { Console.Write("."); Thread.Sleep(500); }
            Console.Clear();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Введи имя базы данных:");
            Console.ResetColor();
            string dbName = Console.ReadLine();

            if (string.IsNullOrEmpty(dbName))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Имя базы данных не может быть пустым.");
                Console.ResetColor();
                return;
            }

            List<Table> tables = new List<Table>();

            while (true)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine("\nГлавное меню:");
                string[] options = { "Добавить таблицу", "Добавить данные в таблицу", "Сохранить в SQL файл и выйти" };
                int choice = DisplayMenu(options);

                switch (choice)
                {
                    case 0:
                        tables.Add(CreateTable());
                        break;
                    case 1:
                        AddDataToTable(tables);
                        break;
                    case 2:
                        SaveToSqlFile(dbName, tables);
                        return;
                    default:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Неверный выбор, попробуйте снова.");
                        Console.ResetColor();
                        break;
                }
            }
        }

        static int DisplayMenu(string[] options)
        {
            int index = 0;
            ConsoleKey key;

            do
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine("Выберите действие:\n");

                for (int i = 0; i < options.Length; i++)
                {
                    if (i == index)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("-> " + options[i]);
                    }
                    else
                    {
                        Console.ResetColor();
                        Console.WriteLine("   " + options[i]);
                    }
                }

                key = Console.ReadKey(true).Key;

                if (key == ConsoleKey.UpArrow)
                {
                    index = (index == 0) ? options.Length - 1 : index - 1;
                }
                else if (key == ConsoleKey.DownArrow)
                {
                    index = (index == options.Length - 1) ? 0 : index + 1;
                }

            } while (key != ConsoleKey.Enter);

            Console.ResetColor();
            return index;
        }

        static Table CreateTable()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("Введи имя таблицы: ");
            Console.ResetColor();
            string tableName = Console.ReadLine();

            List<Column> columns = new List<Column>();

            while (true)
            {
                string[] options = { "Добавить колонку", "Закончить создание таблицы" };
                int choice = DisplayMenu(options);

                if (choice == 0)
                {
                    columns.Add(CreateColumn());
                }
                else if (choice == 1)
                {
                    break;
                }
            }

            return new Table { Name = tableName, Columns = columns, Rows = new List<Row>() };
        }

        static Column CreateColumn()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("Введи имя колонки: ");
            Console.ResetColor();
            string columnName = Console.ReadLine();

            string[] dataTypes = { "INT", "VARCHAR(255)", "DATE", "TEXT", "FLOAT" };
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Выберите тип данных для колонки:");
            int dataTypeIndex = DisplayMenu(dataTypes);
            string dataType = dataTypes[dataTypeIndex];

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("Является ли эта колонка PRIMARY KEY? (y/n): ");
            Console.ResetColor();
            bool isPrimaryKey = Console.ReadLine().ToLower() == "y";

            return new Column { Name = columnName, DataType = dataType, IsPrimaryKey = isPrimaryKey };
        }

        static void AddDataToTable(List<Table> tables)
        {
            if (tables.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Нет доступных таблиц для добавления данных.");
                Console.ResetColor();
                return;
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Выберите таблицу для добавления данных:");
            int tableIndex = DisplayMenu(tables.ConvertAll(t => t.Name).ToArray());
            Table selectedTable = tables[tableIndex];

            Row newRow = new Row();
            foreach (var column in selectedTable.Columns)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write($"Введи значение для {column.Name} ({column.DataType}): ");
                Console.ResetColor();
                newRow.Values.Add(Console.ReadLine());
            }

            selectedTable.Rows.Add(newRow);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Данные успешно добавлены.");
            Console.ResetColor();
        }

        static void SaveToSqlFile(string dbName, List<Table> tables)
        {
            string filePath = $"{dbName}.sql";
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                writer.WriteLine($"CREATE DATABASE `{dbName}`;");
                writer.WriteLine($"USE `{dbName}`;");
                writer.WriteLine();

                foreach (var table in tables)
                {
                    writer.WriteLine(GenerateCreateTableSql(table));
                    writer.WriteLine();
                    foreach (var row in table.Rows)
                    {
                        writer.WriteLine(GenerateInsertRowSql(table, row));
                    }
                }
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\nSQL-файл '{filePath}' успешно создан.");
            Console.ResetColor();
        }

        static string GenerateCreateTableSql(Table table)
        {
            StringBuilder sql = new StringBuilder();
            sql.AppendLine($"CREATE TABLE `{table.Name}` (");
            List<string> columnDefinitions = new List<string>();

            foreach (var column in table.Columns)
            {
                string columnDef = $"  `{column.Name}` {column.DataType}";
                if (column.IsPrimaryKey)
                    columnDef += " PRIMARY KEY";
                columnDefinitions.Add(columnDef);
            }

            sql.AppendLine(string.Join(",\n", columnDefinitions));
            sql.Append(");");
            return sql.ToString();
        }

        static string GenerateInsertRowSql(Table table, Row row)
        {
            StringBuilder sql = new StringBuilder();
            sql.Append($"INSERT INTO `{table.Name}` (");
            sql.Append(string.Join(", ", table.Columns.ConvertAll(c => $"`{c.Name}`")));
            sql.Append(") VALUES (");
            sql.Append(string.Join(", ", row.Values.ConvertAll(v => $"'{v}'")));
            sql.Append(");");
            return sql.ToString();
        }
    }

    class Table
    {
        public string Name { get; set; }
        public List<Column> Columns { get; set; }
        public List<Row> Rows { get; set; }
    }

    class Column
    {
        public string Name { get; set; }
        public string DataType { get; set; }
        public bool IsPrimaryKey { get; set; }
    }

    class Row
    {
        public List<string> Values { get; set; } = new List<string>();
    }
}
