using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AudioBookManager
{
    public static class AppStart
    {
        public static string DownloadPath { get; set; }
        public static string DefaultPath { get; set; }

        public static void Configure()
        {
            DefaultPath = new AppSettingsReader().GetValue("DefaultPath",typeof(string)) as String;
            DownloadPath = new AppSettingsReader().GetValue("DefaultDownload", typeof(string)) as String;
        }
    }
}
