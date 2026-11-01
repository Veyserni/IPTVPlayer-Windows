using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace IPTVPlayer.Windows;

public static partial class M3uParser
{
    [GeneratedRegex("group-title=\\\"(?<group>[^\\\"]*)\\\"", RegexOptions.IgnoreCase)]
    private static partial Regex GroupRegex();

    [GeneratedRegex("tvg-name=\\\"(?<name>[^\\\"]*)\\\"", RegexOptions.IgnoreCase)]
    private static partial Regex NameRegex();

    public static async Task<List<IptvChannel>> ParseAsync(Stream stream, CancellationToken cancellationToken)
    {
        var result = new List<IptvChannel>();
        using var reader = new StreamReader(stream);

        string? pendingInfo = null;
        while (!reader.EndOfStream)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string? line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (line.StartsWith("#EXTINF", StringComparison.OrdinalIgnoreCase))
            {
                pendingInfo = line;
                continue;
            }

            if (line.StartsWith("#", StringComparison.Ordinal))
                continue;

            if (pendingInfo is null)
                continue;

            string name = ParseName(pendingInfo);
            string group = GroupRegex().Match(pendingInfo).Groups["group"].Value.Trim();
            result.Add(new IptvChannel { Name = name, Group = group, Url = line.Trim() });
            pendingInfo = null;
        }

        return result;
    }

    private static string ParseName(string info)
    {
        Match tvgName = NameRegex().Match(info);
        if (tvgName.Success && !string.IsNullOrWhiteSpace(tvgName.Groups["name"].Value))
            return tvgName.Groups["name"].Value.Trim();

        int comma = info.LastIndexOf(',');
        return comma >= 0 && comma < info.Length - 1
            ? info[(comma + 1)..].Trim()
            : "Kanal";
    }
}
