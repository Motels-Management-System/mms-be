namespace Mms.Application.Common;

public class UnauthorizedException(string message = "Chưa xác thực hoặc thông tin đăng nhập không chính xác.")
    : Exception(message);
