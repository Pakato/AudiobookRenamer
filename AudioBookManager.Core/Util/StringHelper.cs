using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioBookManager.Core
{
    public static class StringHelper
    {
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
