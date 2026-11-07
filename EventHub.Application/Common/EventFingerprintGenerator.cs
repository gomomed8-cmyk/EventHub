using EventHub.Domain.Entities;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace EventHub.Application.Common;

public static class EventFingerprintGenerator
{
    public static string Generate(Event e)
    {
        var categories = e.TicketCategories
            .Select(c => new
            {
                Name = c.Name.Trim().ToUpperInvariant(),
                c.Price,
                c.Capacity
            })
            .OrderBy(c => c.Name, StringComparer.Ordinal)
            .ThenBy(c => c.Price)
            .ThenBy(c => c.Capacity)
            .ToArray();

        var canonicalData = JsonSerializer.Serialize(new
        {
            e.OrganizerId,
            Name = e.Name.Trim(),
            Description = e.Description.Trim(),
            e.StartDate,
            e.EndDate,
            e.Category,
            e.TicketMode,
            e.Price,
            e.Capacity,
            Categories = categories
        });

        var hash = SHA256.HashData(
            Encoding.UTF8.GetBytes(canonicalData));

        return Convert.ToHexString(hash);
    }
}