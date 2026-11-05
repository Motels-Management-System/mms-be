namespace Mms.Application.Common;

public class ForbiddenException(string message = "Bạn không có quyền thực hiện thao tác này.")
    : Exception(message);
