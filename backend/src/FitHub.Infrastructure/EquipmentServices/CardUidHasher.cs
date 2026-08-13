using System.Security.Cryptography;
using System.Text;
using FitHub.Application.Equipment;
using Microsoft.Extensions.Configuration;
namespace FitHub.Infrastructure.EquipmentServices;
public sealed class CardUidHasher(IConfiguration configuration) : ICardUidHasher
{
    public string Hash(string uid)
    {
        var pepper = configuration["EquipmentDevices:CardUidPepper"] ?? throw new InvalidOperationException("EquipmentDevices:CardUidPepper is not configured.");
        return Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(pepper), Encoding.UTF8.GetBytes(uid.Trim())));
    }
}
