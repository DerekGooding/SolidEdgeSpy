using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace SpyNet10.InteropServices;

public static class MarshalEx
{
    public static int CreateInstance(string progID, out IntPtr p)
    {
        var hr = 0;
        var clsid = Guid.Empty;
        p = IntPtr.Zero;

        if (Succeeded(hr = NativeMethods.CLSIDFromString(progID, out clsid)))
        {
            var iid = new Guid(NativeMethods.IID_IUnknown);

            if (Succeeded(hr = NativeMethods.CoCreateInstance(clsid, IntPtr.Zero, 23, iid, out p)))
            {
                hr = NativeMethods.OleRun(p);
            }
        }

        return hr;
    }

    public static int CreateInstance(string progID, out ComPtr p)
    {
        var hr = 0;
        var clsid = Guid.Empty;
        var pUnk = IntPtr.Zero;
        p = IntPtr.Zero;

        if (Succeeded(hr = CreateInstance(progID, out pUnk)))
        {
            p = new ComPtr(pUnk);
            Marshal.Release(pUnk);
        }

        return hr;
    }

    //public static int CreateInstance<T>(string progID, out ComPtr<T> p)
    //{
    //    int hr = 0;
    //    Guid clsid = Guid.Empty;
    //    IntPtr pUnk = IntPtr.Zero;
    //    p = IntPtr.Zero;

    //    if (Succeeded(hr = CreateInstance(progID, out pUnk)))
    //    {
    //        p = new ComPtr<T>(pUnk);
    //        Marshal.Release(pUnk);
    //    }

    //    return hr;
    //}

    public static int GetActiveObject(string progID, out IntPtr p)
    {
        var hr = 0;
        var clsid = Guid.Empty;
        p = IntPtr.Zero;

        if (Succeeded(hr = NativeMethods.CLSIDFromString(progID, out clsid)))
        {
            hr = NativeMethods.GetActiveObject(clsid, IntPtr.Zero, out p);
        }

        return hr;
    }

    public static int GetActiveObject(string progID, out ComPtr p)
    {
        var hr = 0;
        var clsid = Guid.Empty;
        var pUnk = IntPtr.Zero;
        p = IntPtr.Zero;

        if (Succeeded(hr = GetActiveObject(progID, out pUnk)))
        {
            p = new ComPtr(pUnk);
            Marshal.Release(pUnk);
        }

        return hr;
    }

    //public static int GetActiveObject<T>(string progID, out ComPtr<T> p)
    //{
    //    int hr = 0;
    //    Guid clsid = Guid.Empty;
    //    IntPtr pUnk = IntPtr.Zero;
    //    p = IntPtr.Zero;

    //    if (Succeeded(hr = GetActiveObject(progID, out pUnk)))
    //    {
    //        p = new ComPtr<T>(pUnk);
    //        Marshal.Release(pUnk);
    //    }

    //    return hr;
    //}

    /// <summary>
    /// Returns an array of Guids by QueryInterface()'ing all IIDs known to this system.
    /// </summary>
    public static Dictionary<Guid, string> QueryInterfaces(IntPtr pUnk)
    {
        var list = new Dictionary<Guid, string>();

        try
        {
            using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, RegistryView.Default))
            {
                using (var interfaceKey = baseKey.OpenSubKey("Interface"))
                {
                    foreach (var iid in interfaceKey.GetSubKeyNames())
                    {
                        try
                        {
                            var guid = Guid.Empty;

                            if (Guid.TryParse(iid, out guid))
                            {
                                var ppv = IntPtr.Zero;

                                if (Marshal.QueryInterface(pUnk, ref guid, out ppv) == 0)
                                {
                                    using (var iidKey = interfaceKey.OpenSubKey(iid))
                                    {
                                        var defaultValue = iidKey.GetValue(null);
                                        list.Add(guid, string.Format("{0}", defaultValue));
                                        Marshal.Release(ppv);
                                    }
                                }
                            }
                        }
                        catch
                        {
                        }
                    }
                }
            }
        }
        catch
        {
        }
        finally
        {
        }

        return list;
    }

    /// <summary>
    /// Returns an array of Guids by QueryInterface()'ing all IIDs known to this system.
    /// </summary>
    public static Guid[] QueryInterfaces(object o)
    {
        var list = new List<Guid>();

        if (Marshal.IsComObject(o))
        {
            var pUnk = IntPtr.Zero;
            try
            {
                pUnk = Marshal.GetIUnknownForObject(o);

                using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, RegistryView.Default))
                {
                    using (var interfaceKey = baseKey.OpenSubKey("Interface"))
                    {
                        foreach (var iid in interfaceKey.GetSubKeyNames())
                        {
                            try
                            {
                                var guid = Guid.Empty;

                                if (Guid.TryParse(iid, out guid))
                                {
                                    var ppv = IntPtr.Zero;

                                    if (Marshal.QueryInterface(pUnk, ref guid, out ppv) == 0)
                                    {
                                        list.Add(guid);
                                        Marshal.Release(ppv);
                                    }
                                }
                            }
                            catch
                            {
                            }
                        }
                    }
                }
            }
            catch
            {
            }
            finally
            {
                if (!pUnk.Equals(IntPtr.Zero))
                {
                    Marshal.Release(pUnk);
                }
            }
        }

        return [.. list];
    }

    public static bool Succeeded(int hr) => NativeMethods.Succeeded(hr);

    public static bool Failed(int hr) => NativeMethods.Failed(hr);
}