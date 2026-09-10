namespace SmartCity.Application.Exceptions;

public sealed class DataPersistenceException(string message, Exception? innerException = null)
    : Exception(message, innerException);
