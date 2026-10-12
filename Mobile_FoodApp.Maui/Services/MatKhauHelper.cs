using System.Security.Cryptography;

namespace FoodApp.Maui.Services;

/// <summary>Băm mật khẩu PBKDF2-SHA256 (100.000 vòng, salt ngẫu nhiên) — không lưu mật khẩu gốc.</summary>
public static class MatKhauHelper
{
    public static string TaoMuoi() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));

    public static string Bam(string matKhau, string muoi) =>
        Convert.ToBase64String(Rfc2898DeriveBytes.Pbkdf2(
            matKhau, Convert.FromBase64String(muoi), 100_000, HashAlgorithmName.SHA256, 32));

    public static bool Kiem(string matKhau, string muoi, string bam)
    {
        if (string.IsNullOrEmpty(muoi) || string.IsNullOrEmpty(bam)) return false;
        return CryptographicOperations.FixedTimeEquals(
            Convert.FromBase64String(Bam(matKhau, muoi)), Convert.FromBase64String(bam));
    }
}
