namespace AuthService.Application.Exceptions;

public class ConflictException     : Exception { public ConflictException(string msg)     : base(msg) {} }
public class NotFoundException     : Exception { public NotFoundException(string msg)     : base(msg) {} }
public class UnauthorizedException : Exception { public UnauthorizedException(string msg) : base(msg) {} }
public class ValidationException   : Exception { public ValidationException(string msg)   : base(msg) {} }
