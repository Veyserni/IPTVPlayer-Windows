using System;

namespace IPTVPlayer.Windows;

public sealed class IptvChannel
{
    public string Name { get; init; } = string.Empty;
    public string Group { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;

    public override string ToString() => Name;
}

public sealed class SavedAccount
{
    public string Server { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

public enum CatalogFilter
{
    All,
    Sport,
    Live,
    Movie,
    Series
}
