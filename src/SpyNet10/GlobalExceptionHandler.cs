using System.Diagnostics;

namespace SpyNet10;

static class GlobalExceptionHandler
{
    public static void HandleException()
    {
#if DEBUG
        Debugger.Break();
#endif
    }

    public static void HandleException(System.Exception ex)
    {
#if DEBUG
        Debugger.Break();
#endif
    }
}
