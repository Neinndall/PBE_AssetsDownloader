// PBE_AssetsDownloader/Info/ApplicationInfos.cs
using System;
using System.Reflection;

namespace PBE_AssetsDownloader.Info
{
    public static class ApplicationInfos
    {
        public static string Version 
        {
            get
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                return version != null ? $"v{version.Major}.{version.Minor}.{version.Build}" : "vUnknown";
            }
        }
    }
}