namespace Cashere.Sync.Dtos;

public record ScanBarcodeRequest(string Barcode);

public record ScanResultDto(bool Found, int? ProductId, string? ProductName, decimal UnitPrice, string? Message);
