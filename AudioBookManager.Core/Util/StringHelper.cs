using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioBookManager.Core
{
    public static class StringHelper
    {
        private static readonly HashSet<string> ReservedDeviceNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "CON", "PRN", "AUX", "NUL",
            "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
        };

        // Strips characters the Windows SMB redirector rejects when writing to a Samba share,
        // even though the Linux filesystem underneath would accept them. Without this, titles
        // like "Hyperion: The Fall" fail at Directory.CreateDirectory / FileStream over SMB.
        public static string ToSafeFileName(this string str)
        {
            if (string.IsNullOrEmpty(str))
                return "_";

            str = str.Replace(": ", " - ");

            var invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(str.Length);
            foreach (var c in str)
                sb.Append(Array.IndexOf(invalid, c) >= 0 ? '_' : c);

            var result = sb.ToString().TrimEnd(' ', '.');

            var stem = result;
            var dot = stem.IndexOf('.');
            if (dot >= 0) stem = stem.Substring(0, dot);
            if (ReservedDeviceNames.Contains(stem))
                result = "_" + result;

            return string.IsNullOrEmpty(result) ? "_" : result;
        }

        //Convert all first latter
        public static string ToTitleCase(this string str)
        {
            str = str.ToLower().Trim();
            var strArray = str.Split(' ');
            if (strArray.Length > 1)
            {
                strArray[0] = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(strArray[0]);
                return string.Join(" ", strArray);
            }
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(str);
        }
        public static string ToTitleCase(this string str, TitleCase tcase)
        {
            str = str.ToLower().Trim();
            switch (tcase)
            {
                case TitleCase.First:
                    var strArray = str.Split(' ');
                    if (strArray.Length > 1)
                    {
                        strArray[0] = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(strArray[0]);
                        return string.Join(" ", strArray);
                    }
                    break;
                case TitleCase.All:
                    return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(str);
                default:
                    break;
            }
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(str);
        }
    }
}
