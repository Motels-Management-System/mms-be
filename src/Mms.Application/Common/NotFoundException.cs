namespace Mms.Application.Common;

public class NotFoundException : Exception
{
    public NotFoundException(string message)
        : base(message) { }

    public NotFoundException(string entityName, object key)
        : base($"{entityName} với khóa '{key}' không tìm thấy.") { }
}
