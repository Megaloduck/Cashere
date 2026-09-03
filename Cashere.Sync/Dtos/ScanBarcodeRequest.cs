namespace Cashere.Sync.Dtos;

public record ScanBarcodeRequest(string Barcode, int Quantity = 1);

public record ScanResultDto(bool Found, string? ProductName, string? Message, CartDto? Cart);
