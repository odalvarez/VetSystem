namespace NotificationsService.Application.Exceptions;

public class NotFoundException     : Exception { public NotFoundException(string msg)     : base(msg) {} }
public class ValidationException   : Exception { public ValidationException(string msg)   : base(msg) {} }
public class ServiceUnavailableException : Exception { public ServiceUnavailableException(string msg) : base(msg) {} }
