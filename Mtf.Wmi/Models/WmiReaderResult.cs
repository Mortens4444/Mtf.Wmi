using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Mtf.WmiHelper.Models
{
    public class WmiReaderResult : IEnumerable<IEnumerable<object>>
    {
        private readonly List<IEnumerable<object>> rows = new List<IEnumerable<object>>();

        public WmiReaderResult(string commaSeparatedColumnNames = null, IEnumerable<IEnumerable<object>> initialRows = null)
        {
            ColumnIndexes = commaSeparatedColumnNames?
                .Split(',')
                .Select((name, index) => new { name, index })
                .ToDictionary(x => x.name.Trim(), x => x.index, StringComparer.OrdinalIgnoreCase)
                ?? new Dictionary<string, int>();

            if (initialRows != null)
            {
                foreach (var row in initialRows)
                {
                    rows.Add(row);
                }
            }
        }

        public Dictionary<string, int> ColumnIndexes { get; private set; }

        public int RowCount => rows.Count;

        public IEnumerable<object> this[int rowIndex] => rows.ElementAt(rowIndex);

        public object this[int rowIndex, int columnIndex] => rows.ElementAt(rowIndex).ElementAt(columnIndex);

        public object this[int rowIndex, string columnName] => ColumnIndexes.TryGetValue(columnName, out var columnIndex)
            ? rows.ElementAt(rowIndex).ElementAt(columnIndex) : throw new InvalidOperationException($"Column '{columnName}' not found.");

        public void AddRow(IEnumerable<object> row) => rows.Add(row);

        public IEnumerator<IEnumerable<object>> GetEnumerator() => rows.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
