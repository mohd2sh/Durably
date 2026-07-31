namespace Durably;

internal static class EfPersistenceExceptionHelper
{
    public static bool IsDuplicateKey(Exception ex)
    {
        for (var current = ex; current is not null; current = current.InnerException)
        {
            var message = current.Message;
            if (message.IndexOf("PRIMARY KEY", StringComparison.OrdinalIgnoreCase) >= 0
                || message.IndexOf("duplicate key", StringComparison.OrdinalIgnoreCase) >= 0
                || message.IndexOf("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase) >= 0
                || message.IndexOf("23505", StringComparison.Ordinal) >= 0)
            {
                return true;
            }
        }

        return false;
    }
}
