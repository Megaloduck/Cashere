using System;
using System.Collections.Generic;

namespace Cashere.Models;

public class Supplier
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }

    public ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
}
