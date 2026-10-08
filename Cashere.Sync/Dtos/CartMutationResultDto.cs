namespace Cashere.Sync.Dtos;

public record CartMutationResultDto(bool Success, string? Message, CartDto Cart);
