using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cashere.Services;

// Desktop-only: renders the till's LAN pairing address as a QR code so
// mobile's "SCAN TO PAIR" can populate Host/Port without typing an IP.
public interface IPairingQrCodeService
{
    byte[]? GeneratePairingQrCodePng(string host, int port);
}