using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Mms.Application.Common;
using Mms.Domain.Exceptions;
using Npgsql;

namespace Mms.Api.Middleware;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken ct)
    {
        var (statusCode, code, title, detail, errors) = exception switch
        {
            ValidationException vex => (
                StatusCodes.Status400BadRequest,
                "VALIDATION_ERROR",
                "Dữ liệu không hợp lệ.",
                vex.Message,
                vex.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }).ToArray() as object),

            UnauthorizedException uex => (
                StatusCodes.Status401Unauthorized,
                "UNAUTHORIZED",
                "Không được phép truy cập.",
                ueexMessage(uex),
                null as object),

            ForbiddenException fex => (
                StatusCodes.Status403Forbidden,
                "FORBIDDEN",
                "Không đủ quyền.",
                fex.Message,
                null as object),

            DomainException dex => (
                StatusCodes.Status422UnprocessableEntity,
                "BUSINESS_RULE_VIOLATION",
                "Vi phạm quy tắc nghiệp vụ.",
                dex.Message,
                null as object),

            NotFoundException nex => (
                StatusCodes.Status404NotFound,
                "NOT_FOUND",
                "Không tìm thấy.",
                nex.Message,
                null as object),

            ConflictException cex when cex is RoomDateOverlapException => (
                StatusCodes.Status409Conflict,
                "ROOM_DATE_OVERLAP",
                "Trùng lịch phòng.",
                cex.Message,
                null as object),

            ConflictException cex => (
                StatusCodes.Status409Conflict,
                "DUPLICATE",
                "Xung đột dữ liệu.",
                cex.Message,
                null as object),

            Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException => (
                StatusCodes.Status409Conflict,
                "CONCURRENCY_CONFLICT",
                "Dữ liệu đã bị thay đổi bởi người khác.",
                "Vui lòng tải lại và thử lại.",
                null as object),

            _ => HandleDatabaseException(exception) ?? (
                StatusCodes.Status500InternalServerError,
                "INTERNAL_ERROR",
                "Lỗi hệ thống.",
                "Đã xảy ra lỗi không mong muốn.",
                null as object)
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Lỗi không mong muốn");
        else
            logger.LogWarning(exception, "Handled exception: {Code}", code);

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Type = $"https://httpstatuses.com/{statusCode}"
        };
        problemDetails.Extensions["code"] = code;
        if (errors is not null)
            problemDetails.Extensions["errors"] = errors;

        await httpContext.Response.WriteAsJsonAsync(problemDetails, ct);
        return true;
    }

    private static string ueexMessage(UnauthorizedException uex) => uex.Message;

    private static (int, string, string, string, object?)? HandleDatabaseException(Exception exception)
    {
        var pgEx = exception as PostgresException
                   ?? exception.InnerException as PostgresException;

        if (pgEx is null) return null;

        return pgEx.SqlState switch
        {
            "23505" => (StatusCodes.Status409Conflict, "DUPLICATE",
                "Dữ liệu trùng lặp.", "Bản ghi đã tồn tại.", null),

            "23P01" => (StatusCodes.Status409Conflict, "ROOM_DATE_OVERLAP",
                "Trùng lịch phòng.", "Khoảng thời gian thuê trùng với hợp đồng khác.", null),

            _ => null
        };
    }
}
