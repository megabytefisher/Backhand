using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;

namespace Backhand.Cli.Internal
{
    public static class IConsoleExtensions
    {
        public static void WriteTable<T>(this IConsole console, ICollection<ConsoleTableColumn<T>> columns, ICollection<T> rows, ConsoleTableOptions? options = null)
        {
            options = options ?? new ConsoleTableOptions();

            Dictionary<ConsoleTableColumn<T>, int> columnMaxLengths = GetColumnMaxLengths(columns, rows);

            StringBuilder stringBuilder = new StringBuilder();
            WriteTableHeader(stringBuilder, columns, rows, columnMaxLengths, options);
            WriteTableRows(stringBuilder, columns, rows, columnMaxLengths, options);
            console.Write(stringBuilder.ToString());
        }

        private static Dictionary<ConsoleTableColumn<T>, int> GetColumnMaxLengths<T>(ICollection<ConsoleTableColumn<T>> columns, ICollection<T> rows)
        {
            Dictionary<ConsoleTableColumn<T>, int> columnMaxLengths = new Dictionary<ConsoleTableColumn<T>, int>();
            foreach (ConsoleTableColumn<T> column in columns)
            {
                columnMaxLengths[column] = Math.Max(column.Header.Length, rows.Max(r => column.GetText(r).Length));
            }
            return columnMaxLengths;
        }

        private static void WriteTableHeader<T>(StringBuilder stringBuilder, ICollection<ConsoleTableColumn<T>> columns, ICollection<T> rows, Dictionary<ConsoleTableColumn<T>, int> columnLengths, ConsoleTableOptions options)
        {
            bool any = false;
            foreach (ConsoleTableColumn<T> column in columns)
            {
                int maxLength = columnLengths[column];
                any = true;

                string columnNameText = column.Header;

                if (column.Width.HasValue)
                {
                    columnNameText = columnNameText.Substring(0, column.Width.Value);
                }

                if (column.IsRightAligned)
                {
                    columnNameText = columnNameText.PadLeft(column.Width ?? maxLength, ' ');
                }
                else
                {
                    columnNameText = columnNameText.PadRight(column.Width ?? maxLength, ' ');
                }

                stringBuilder.Append(columnNameText);
                stringBuilder.Append(new string(' ', options.ColumnPadding));
            }

            if (any)
            {
                stringBuilder.Remove(stringBuilder.Length - 1, 1);
            }

            stringBuilder.AppendLine();
        }

        private static void WriteTableRows<T>(StringBuilder stringBuilder, ICollection<ConsoleTableColumn<T>> columns, ICollection<T> rows, Dictionary<ConsoleTableColumn<T>, int> columnLengths, ConsoleTableOptions options)
        {
            foreach (T row in rows)
            {
                bool any = false;
                foreach ((ConsoleTableColumn<T> column, int maxLength) in columnLengths)
                {
                    any = true;

                    string rowText = column.GetText(row);

                    if (column.Width.HasValue)
                    {
                        rowText = rowText.Substring(0, column.Width.Value);
                    }

                    if (column.IsRightAligned)
                    {
                        rowText = rowText.PadLeft(column.Width ?? maxLength, ' ');
                    }
                    else
                    {
                        rowText = rowText.PadRight(column.Width ?? maxLength, ' ');
                    }

                    stringBuilder.Append(rowText);
                    stringBuilder.Append(new string(' ', options.ColumnPadding));
                }

                if (any)
                {
                    stringBuilder.Remove(stringBuilder.Length - 1, 1);
                }

                stringBuilder.AppendLine();
            }
        }
    }
}
