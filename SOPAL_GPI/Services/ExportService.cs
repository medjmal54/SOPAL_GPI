using ClosedXML.Excel;
using System.Globalization;
using System.Text;

namespace SOPAL_GPI.Services;

public static class ExportService
{
    public static byte[] BuildXlsx(string sheetName, string[] headers, IEnumerable<object?[]> rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(sheetName);

        for (var c = 0; c < headers.Length; c++)
        {
            sheet.Cell(1, c + 1).Value = headers[c];
            sheet.Cell(1, c + 1).Style.Font.Bold = true;
        }

        var rowIndex = 2;
        foreach (var row in rows)
        {
            for (var c = 0; c < row.Length && c < headers.Length; c++)
            {
                var value = row[c];
                var cell = sheet.Cell(rowIndex, c + 1);
                switch (value)
                {
                    case DateTime dateTime: cell.Value = dateTime; break;
                    case DateOnly date: cell.Value = date.ToDateTime(TimeOnly.MinValue); break;
                    case bool boolean: cell.Value = boolean; break;
                    case int number: cell.Value = number; break;
                    case double real: cell.Value = real; break;
                    case decimal money: cell.Value = money; break;
                    default: cell.Value = Convert.ToString(value, CultureInfo.InvariantCulture) ?? ""; break;
                }
            }
            rowIndex++;
        }

        sheet.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public static byte[] BuildCsv(string[] headers, IEnumerable<object?[]> rows)
    {
        var builder = new StringBuilder();
        builder.Append('\uFEFF');
        builder.AppendLine(string.Join(';', headers.Select(Escape)));

        foreach (var row in rows)
            builder.AppendLine(string.Join(';', row.Select(Escape)));

        return Encoding.UTF8.GetBytes(builder.ToString());
    }

    private static string Escape(object? value) =>
        $"\"{Convert.ToString(value, CultureInfo.InvariantCulture)?.Replace("\"", "\"\"") ?? ""}\"";
}
