using System.Drawing;
using System.Windows.Forms;
using System.Windows.Resources;

namespace SnowDesktop.UI;

internal static class TrayIconHelper
{
    public static Icon CreateSnowflakeIcon()
    {
        try
        {
            var uri = new Uri("pack://application:,,,/Assets/snezhok.ico");
            StreamResourceInfo? info = System.Windows.Application.GetResourceStream(uri);
            if (info is not null)
            {
                using (info.Stream)
                {
                    return new Icon(info.Stream, SystemInformation.SmallIconSize);
                }
            }
        }
        catch
        {
            // Fallback to associated executable icon below.
        }

        string? exePath = Environment.ProcessPath;
        if (!string.IsNullOrEmpty(exePath))
        {
            Icon? assoc = Icon.ExtractAssociatedIcon(exePath);
            if (assoc is not null)
            {
                return assoc;
            }
        }

        return (Icon)SystemIcons.Application.Clone();
    }
}
