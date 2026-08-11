using System.Runtime.InteropServices;

namespace CrossOff.LobbyDodger;

internal static class WindowChrome
{
    private const int ImmersiveDarkMode = 20;
    private const int ImmersiveDarkModeBefore20H1 = 19;

    public static void ApplyDarkTitleBar(IntPtr windowHandle)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763))
        {
            return;
        }

        try
        {
            int enabled = 1;
            int result = DwmSetWindowAttribute(
                windowHandle,
                ImmersiveDarkMode,
                ref enabled,
                sizeof(int));

            if (result != 0)
            {
                _ = DwmSetWindowAttribute(
                    windowHandle,
                    ImmersiveDarkModeBefore20H1,
                    ref enabled,
                    sizeof(int));
            }
        }
        catch (DllNotFoundException)
        {
            // Keep the standard title bar on unsupported Windows editions.
        }
        catch (EntryPointNotFoundException)
        {
            // Keep the standard title bar when DWM does not expose this attribute.
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        IntPtr windowHandle,
        int attribute,
        ref int attributeValue,
        int attributeSize);
}
