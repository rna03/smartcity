namespace SmartCity.Application.Exceptions;

public sealed class ExternalDataSourceException(string message, Exception? innerException = null)
    : Exception(message, innerException);
