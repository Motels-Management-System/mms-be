namespace Mms.Api.IntegrationTests;

/// <summary>Body ProblemDetails mà middleware trả về, kèm mã lỗi ổn định cho frontend.</summary>
public sealed record ProblemResponse(int Status, string Title, string Code, string? Detail);
