using QRCoder;

namespace Strawberry.Services;

public class QrCodeService
{
    public string GenerateQrCodeBase64(string code)
    {
        using var generator = new QRCodeGenerator();
        var data = generator.CreateQrCode(code, QRCodeGenerator.ECCLevel.M);
        using var qrCode = new PngByteQRCode(data);
        var bytes = qrCode.GetGraphic(10);
        return Convert.ToBase64String(bytes);
    }

    public string GenerateRandomCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 6)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}
