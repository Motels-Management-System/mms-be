namespace Mms.Domain.Exceptions;

/// <summary>Vi phạm quy tắc nghiệp vụ. Tầng API sẽ map thành 400/409.</summary>
public class DomainException(string message) : Exception(message);
