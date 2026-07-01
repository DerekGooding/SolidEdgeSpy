using SpyNet10.InteropServices;
using System.Reflection;

namespace SpyNet10.Extensions;

public static class ObjectExtensions
{
    public static bool IsIDispatch(this object o)
    {
        return o is IDispatch;
    }

    public static object SafeInvokeGetProperty(this object o, string name, object defaultValue)
    {
        try
        {
            if (o.IsIDispatch())
            {
                return o.GetType().InvokeMember(name, BindingFlags.GetProperty, null, o, null);
            }
        }
        catch
        {
            //GlobalExceptionHandler.HandleException();
        }

        return defaultValue;
    }
}
