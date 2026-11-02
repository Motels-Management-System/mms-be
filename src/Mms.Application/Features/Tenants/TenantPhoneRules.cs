namespace Mms.Application.Features.Tenants;

/// <summary>Định dạng số điện thoại dùng chung cho validator hồ sơ người thuê: số di động VN hoặc số quốc tế.</summary>
public static class TenantPhoneRules
{
    public const string Pattern = @"^0[0-9]{9,10}$|^\+[0-9]{9,14}$";
    public const string Message = "Số điện thoại không đúng định dạng (ví dụ 0901234567).";
}
