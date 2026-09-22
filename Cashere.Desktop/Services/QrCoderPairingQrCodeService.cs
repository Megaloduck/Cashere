using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;   
using Cashere.Services;
using QRCoder;

namespace Cashere.Desktop.Services;

public class QrCoderPairingQrCodeService : IPairingQrCodeService
{
    public byte[]? GeneratePairingQrCodePng(string host, int port)
    {
        try
        {
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode($"{host}:{port}", QRCodeGenerator.ECCLevel.M);
            return new PngByteQRCode(data).GetGraphic(10);
        }
        catch
        {
            // A failed render shouldn't take down the settings screen - the
            // manual host/port fields remain the source of truth either way.
            return null;
        }
    }
}