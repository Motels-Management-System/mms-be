using Microsoft.EntityFrameworkCore;
using Mms.Application.Abstractions;
using Mms.Infrastructure.Persistence;
using Npgsql;
using NpgsqlTypes;

namespace Mms.Infrastructure.Reports;

/// <summary>
/// Báo cáo viết SQL thuần vì generate_series và window function không dịch được từ LINQ.
/// Khoảng báo cáo luôn là nửa mở [from, to) khớp với quy ước khoảng thuê.
/// </summary>
public sealed class ReportQueries(AppDbContext dbContext) : IReportQueries
{
    private const string DaysAndRoomDaysCte = """
        WITH days AS (
            SELECT generate_series(@from::date, @to::date - 1, interval '1 day')::date AS "Day"
        ),
        room_days AS (
            SELECT d."Day", r."Id" AS "RoomId", bool_or(c."Id" IS NOT NULL) AS "Occupied"
            FROM days d
            CROSS JOIN "Rooms" r
            LEFT JOIN "Contracts" c
                   ON c."RoomId" = r."Id"
                  AND c."Status" <> 'Draft'
                  AND c."StartDate" <= d."Day"
                  AND c."EndDate" > d."Day"
            GROUP BY d."Day", r."Id"
        )
        """;

    private const string GroupByRoomType = """
        SELECT rt."Name" AS "GroupKey",
               min(rd."Day") AS "PeriodStart",
               max(rd."Day") AS "PeriodEnd",
               count(DISTINCT rd."RoomId")::int AS "RoomCount",
               count(*)::int AS "RoomDays",
               count(*) FILTER (WHERE rd."Occupied")::int AS "OccupiedRoomDays"
        FROM room_days rd
        JOIN "Rooms" r ON r."Id" = rd."RoomId"
        JOIN "RoomTypes" rt ON rt."Id" = r."RoomTypeId"
        GROUP BY rt."Name"
        ORDER BY rt."Name"
        """;

    private const string GroupByMonth = """
        SELECT to_char(rd."Day", 'YYYY-MM') AS "GroupKey",
               min(rd."Day") AS "PeriodStart",
               max(rd."Day") AS "PeriodEnd",
               count(DISTINCT rd."RoomId")::int AS "RoomCount",
               count(*)::int AS "RoomDays",
               count(*) FILTER (WHERE rd."Occupied")::int AS "OccupiedRoomDays"
        FROM room_days rd
        GROUP BY to_char(rd."Day", 'YYYY-MM')
        ORDER BY to_char(rd."Day", 'YYYY-MM')
        """;

    // LEAD trên StartDate của từng phòng cho ra ngày bắt đầu hợp đồng kế tiếp;
    // hiệu số với EndDate trước đó là số ngày phòng bỏ trống (0 ngày nếu gia hạn liền mạch).
    private const string VacancyGapsSql = """
        WITH pairs AS (
            SELECT c."RoomId",
                   c."EndDate" AS "PreviousEnd",
                   lead(c."StartDate") OVER (
                       PARTITION BY c."RoomId"
                       ORDER BY c."StartDate", c."EndDate"
                   ) AS "NextStart"
            FROM "Contracts" c
            WHERE c."Status" <> 'Draft'
        )
        SELECT p."RoomId" AS "RoomId",
               r."Code" AS "RoomCode",
               rt."Name" AS "RoomTypeName",
               count(*)::int AS "GapCount",
               sum(p."NextStart" - p."PreviousEnd")::int AS "VacancyDays",
               max(p."NextStart" - p."PreviousEnd")::int AS "MaxVacancyDays"
        FROM pairs p
        JOIN "Rooms" r ON r."Id" = p."RoomId"
        JOIN "RoomTypes" rt ON rt."Id" = r."RoomTypeId"
        WHERE p."NextStart" IS NOT NULL
          AND p."PreviousEnd" >= @from::date
          AND p."NextStart" <= @to::date
        GROUP BY p."RoomId", r."Code", rt."Name"
        ORDER BY r."Code"
        """;

    // Gia hạn = có hợp đồng khác cùng phòng bắt đầu đúng ngày kết thúc và giữ lại ít nhất một người ở.
    // Có hợp đồng nối tiếp nhưng khác hẳn người ở = phòng thay người mới (quay vòng).
    private const string RenewalRateSql = """
        WITH ended AS (
            SELECT c."Status",
                   EXISTS (
                       SELECT 1
                       FROM "Contracts" s
                       WHERE s."RoomId" = c."RoomId"
                         AND s."Id" <> c."Id"
                         AND s."StartDate" = c."EndDate"
                         AND EXISTS (
                             SELECT 1
                             FROM "ContractTenants" kept
                             JOIN "ContractTenants" next ON next."TenantId" = kept."TenantId"
                             WHERE kept."ContractId" = c."Id"
                               AND next."ContractId" = s."Id"
                         )
                   ) AS "Renewed",
                   EXISTS (
                       SELECT 1
                       FROM "Contracts" s
                       WHERE s."RoomId" = c."RoomId"
                         AND s."Id" <> c."Id"
                         AND s."StartDate" = c."EndDate"
                   ) AS "Refilled"
            FROM "Contracts" c
            WHERE c."Status" <> 'Draft'
              AND c."EndDate" >= @from::date
              AND c."EndDate" < @to::date
        )
        SELECT count(*)::int AS "EndedContracts",
               count(*) FILTER (WHERE e."Renewed")::int AS "RenewedContracts",
               count(*) FILTER (WHERE NOT e."Renewed" AND e."Refilled")::int AS "ReplacedByNewTenants",
               count(*) FILTER (WHERE NOT e."Renewed" AND NOT e."Refilled")::int AS "LeftVacant",
               count(*) FILTER (WHERE e."Status" = 'Terminated')::int AS "EarlyTerminated"
        FROM ended e
        """;

    public async Task<IReadOnlyList<OccupancyBucketRow>> GetOccupancyAsync(
        DateOnly from, DateOnly to, bool groupByRoomType, CancellationToken ct)
    {
        var sql = string.Concat(DaysAndRoomDaysCte, "\n",
            groupByRoomType ? GroupByRoomType : GroupByMonth);

        var rows = await dbContext.Database
            .SqlQueryRaw<OccupancyRow>(sql, DateParameters(from, to))
            .ToListAsync(ct);

        return rows
            .Select(r => new OccupancyBucketRow(
                r.GroupKey, r.PeriodStart, r.PeriodEnd, r.RoomCount, r.RoomDays, r.OccupiedRoomDays))
            .ToList();
    }

    public async Task<IReadOnlyList<VacancyGapRow>> GetVacancyGapsAsync(
        DateOnly from, DateOnly to, CancellationToken ct)
    {
        var rows = await dbContext.Database
            .SqlQueryRaw<VacancyGapResultRow>(VacancyGapsSql, DateParameters(from, to))
            .ToListAsync(ct);

        return rows
            .Select(r => new VacancyGapRow(
                r.RoomId, r.RoomCode, r.RoomTypeName, r.GapCount, r.VacancyDays, r.MaxVacancyDays))
            .ToList();
    }

    public async Task<RenewalRateRow> GetRenewalRateAsync(DateOnly from, DateOnly to, CancellationToken ct)
    {
        var rows = await dbContext.Database
            .SqlQueryRaw<RenewalRateResultRow>(RenewalRateSql, DateParameters(from, to))
            .ToListAsync(ct);

        // Aggregate không có GROUP BY luôn trả đúng một dòng, kể cả khi kỳ báo cáo trống.
        var row = rows.Single();

        return new RenewalRateRow(
            row.EndedContracts,
            row.RenewedContracts,
            row.ReplacedByNewTenants,
            row.LeftVacant,
            row.EarlyTerminated);
    }

    private static object[] DateParameters(DateOnly from, DateOnly to)
        =>
        [
            new NpgsqlParameter("from", NpgsqlDbType.Date) { Value = from },
            new NpgsqlParameter("to", NpgsqlDbType.Date) { Value = to },
        ];
}
