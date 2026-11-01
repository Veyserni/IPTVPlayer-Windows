using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace IPTVPlayer.Windows;

public sealed class AccountStore
{
    private readonly string _filePath;

    public AccountStore()
    {
        string root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "IPTVPlayerWindows");
        Directory.CreateDirectory(root);
        _filePath = Path.Combine(root, "accounts.json");
    }

    public List<SavedAccount> Load()
    {
        try
        {
            if (!File.Exists(_filePath))
                return new List<SavedAccount>();

            string json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<SavedAccount>>(json) ?? new List<SavedAccount>();
        }
        catch
        {
            return new List<SavedAccount>();
        }
    }

    public void SaveSuccessful(SavedAccount account)
    {
        List<SavedAccount> accounts = Load();
        accounts.RemoveAll(x =>
            string.Equals(x.Server, account.Server, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(x.Username, account.Username, StringComparison.OrdinalIgnoreCase));
        accounts.Insert(0, account);
        accounts = accounts.Take(5).ToList();

        string json = JsonSerializer.Serialize(accounts, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_filePath, json);
    }
}
