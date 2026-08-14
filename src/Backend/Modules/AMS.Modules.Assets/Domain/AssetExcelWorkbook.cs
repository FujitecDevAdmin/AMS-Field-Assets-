using System.Globalization;
using System.IO.Compression;
using System.Xml.Linq;

namespace AMS.Modules.Assets.Domain;

/// <summary>Reads the first worksheet of an Open XML workbook without leaking a document-library type.</summary>
public static class AssetExcelWorkbook
{
    public static IReadOnlyList<IReadOnlyDictionary<string, string?>> Read(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
        var sharedStrings = ReadSharedStrings(archive);
        var worksheet = archive.GetEntry("xl/worksheets/sheet1.xml")
            ?? throw new InvalidDataException("The workbook does not contain its first worksheet.");

        using var worksheetStream = worksheet.Open();
        var document = XDocument.Load(worksheetStream);
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var rows = document.Descendants(ns + "row")
            .Select(row => ReadRow(row, ns, sharedStrings))
            .ToList();

        if (rows.Count == 0)
        {
            return [];
        }

        var headerRowIndex = -1;
        for (var index = 0; index < Math.Min(rows.Count, 10); index++)
        {
            if (IsAssetHeaderRow(rows[index]))
            {
                headerRowIndex = index;
                break;
            }
        }

        if (headerRowIndex < 0)
        {
            throw new InvalidDataException(
                "The first 10 rows do not contain the required Asset No, Asset Name and TechnicalGroup headers.");
        }

        var headers = rows[headerRowIndex].Select(value => value?.Trim() ?? string.Empty).ToArray();
        var result = new List<IReadOnlyDictionary<string, string?>>();
        foreach (var row in rows.Skip(headerRowIndex + 1))
        {
            var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < headers.Length; index++)
            {
                if (headers[index].Length > 0)
                {
                    values[headers[index]] = index < row.Count ? row[index] : null;
                }
            }

            if (values.Values.Any(value => !string.IsNullOrWhiteSpace(value)))
            {
                result.Add(values);
            }
        }

        return result;
    }

    private static bool IsAssetHeaderRow(IReadOnlyCollection<string?> row)
    {
        var headers = row
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return headers.Contains("Asset No")
            && headers.Contains("Asset Name")
            && headers.Contains("TechnicalGroup");
    }

    private static List<string> ReadSharedStrings(ZipArchive archive)
    {
        var entry = archive.GetEntry("xl/sharedStrings.xml");
        if (entry is null)
        {
            return [];
        }

        using var stream = entry.Open();
        var document = XDocument.Load(stream);
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        return document.Descendants(ns + "si")
            .Select(item => string.Concat(item.Descendants(ns + "t").Select(text => text.Value)))
            .ToList();
    }

    private static List<string?> ReadRow(XElement row, XNamespace ns, List<string> sharedStrings)
    {
        var values = new List<string?>();
        foreach (var cell in row.Elements(ns + "c"))
        {
            var reference = cell.Attribute("r")?.Value ?? "A1";
            var column = ColumnIndex(reference);
            while (values.Count <= column)
            {
                values.Add(null);
            }

            var type = cell.Attribute("t")?.Value;
            var raw = type == "inlineStr"
                ? string.Concat(cell.Descendants(ns + "t").Select(text => text.Value))
                : cell.Element(ns + "v")?.Value;
            if (type == "s" && int.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out var sharedIndex)
                && sharedIndex >= 0 && sharedIndex < sharedStrings.Count)
            {
                raw = sharedStrings[sharedIndex];
            }

            values[column] = raw;
        }

        return values;
    }

    private static int ColumnIndex(string reference)
    {
        var index = 0;
        foreach (var character in reference.TakeWhile(char.IsLetter))
        {
            index = (index * 26) + char.ToUpperInvariant(character) - 'A' + 1;
        }

        return index - 1;
    }
}
