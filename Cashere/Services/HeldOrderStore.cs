using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Cashere.ViewModels.Pos;

namespace Cashere.Services;

public sealed class HeldOrderStore
{
    private readonly string _path = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Cashere", "held-orders.json");

    public IReadOnlyList<HeldOrder> Load()
    {
        if (!File.Exists(_path)) return Array.Empty<HeldOrder>();
        var orders = JsonSerializer.Deserialize<List<HeldOrder>>(File.ReadAllText(_path));
        return orders is null ? Array.Empty<HeldOrder>() : orders;
    }

    public void Save(IReadOnlyCollection<HeldOrder> orders)
    {
        var directory = Path.GetDirectoryName(_path)!;
        Directory.CreateDirectory(directory);
        var temporaryPath = _path + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(orders));
        File.Move(temporaryPath, _path, overwrite: true);
    }
}
