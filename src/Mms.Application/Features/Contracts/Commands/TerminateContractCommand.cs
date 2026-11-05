namespace Mms.Application.Features.Contracts.Commands;

/// <summary>Chấm dứt sớm: rút ngắn EndDate; phải báo trước ít nhất EarlyTerminationNoticeDays ngày.</summary>
public sealed record TerminateContractCommand(
    Guid ContractId,
    DateOnly TerminationDate);
