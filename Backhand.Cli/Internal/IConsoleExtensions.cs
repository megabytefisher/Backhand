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

            StringBuilder stringBuilder = new StringBuilder();
            WriteTableHeader(stringBuilder, columns, rows, options);
            WriteTableRows(stringBuilder, columns, rows, options);
            console.Write(stringBuilder.ToString());
        }

        private static void WriteTableHeader<T>(StringBuilder stringBuilder, ICollection<ConsoleTableColumn<T>> columns, ICollection<T> rows, ConsoleTableOptions options)
        {
            bool any = false;
            foreach ((ConsoleTableColumn<T> column, int maxLength) in columns.Select(c => (c, rows.Max(r => c.GetText(r).Length))))
            {
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

        private static void WriteTableRows<T>(StringBuilder stringBuilder, ICollection<ConsoleTableColumn<T>> columns, ICollection<T> rows, ConsoleTableOptions options)
        {
            List<(ConsoleTableColumn<T> columns, int maxLength)> columnMaxLengths = columns.Select(c => (c, rows.Max(r => c.GetText(r).Length))).ToList();

            foreach (T row in rows)
            {
                bool any = false;
                foreach ((ConsoleTableColumn<T> column, int maxLength) in columnMaxLengths)
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
