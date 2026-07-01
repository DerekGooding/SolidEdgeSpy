namespace SpyNet10.Extensions;

public static class ControlExtensions
{
    public static void InvokeIfRequired<TControl>(this TControl control, Action<TControl> action)
        where TControl : Control
    {
        if (control.InvokeRequired)
        {
            control.Invoke(action, control);
        }
        else
        {
            action(control);
        }
    }

    public static void BeginInvokeIfRequired<TControl>(this TControl control, Action<TControl> action)
        where TControl : Control
    {
        if (control.InvokeRequired)
        {
            control.BeginInvoke(action, control);
        }
        else
        {
            action(control);
        }
    }
}
