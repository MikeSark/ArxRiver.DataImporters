namespace ArxRiver.DataImporters.Tsv.Importing;

/// <summary>
/// TSV line parser. Handles quoted fields, embedded delimiters, and escaped quotes.
/// </summary>
internal static class TsvParser
{
    public static string[] ParseLine(string line, char delimiter = '\t')
    {
        var fields = new List<string>();
        var i = 0;

        while (i <= line.Length)
        {
            if (i == line.Length)
            {
                // Trailing delimiter produced an empty final field
                if (fields.Count > 0 && i > 0 && line[i - 1] == delimiter)
                    fields.Add("");
                break;
            }

            if (line[i] == '"')
            {
                // Quoted field
                var sb = new System.Text.StringBuilder();
                i++; // skip opening quote
                while (i < line.Length)
                {
                    if (line[i] == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"')
                        {
                            sb.Append('"');
                            i += 2;
                        }
                        else
                        {
                            i++; // skip closing quote
                            break;
                        }
                    }
                    else
                    {
                        sb.Append(line[i]);
                        i++;
                    }
                }

                fields.Add(sb.ToString());

                // Skip delimiter after closing quote
                if (i < line.Length && line[i] == delimiter)
                    i++;
            }
            else
            {
                // Unquoted field
                var start = i;
                while (i < line.Length && line[i] != delimiter)
                    i++;

                fields.Add(line.Substring(start, i - start));

                if (i < line.Length)
                    i++; // skip delimiter
            }
        }

        // Handle empty line
        if (fields.Count == 0)
            fields.Add("");

        return fields.ToArray();
    }
}
