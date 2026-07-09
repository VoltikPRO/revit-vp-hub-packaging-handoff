namespace LicensingSystem.Contracts.Common;

public sealed class ErrorResponse
{
    public ErrorDetail Error { get; init; } = default!;
}

public sealed class ErrorDetail
{
    public string Code { get; init; } = "";
    public string Message { get; init; } = "";
    public object? Details { get; init; }
}

