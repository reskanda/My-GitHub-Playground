using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;

namespace HubPasswordHasher
{
    public class WebConfig
    {
        public static string GetAppSettingValue(string key)
        {
            string val = ConfigurationManager.AppSettings[key];
            if (val == null || string.IsNullOrEmpty(val))
            {
                throw new Exception(string.Format("Fatal error: missing app setting in web.config file for the '{0}' key", key));
            }
            return val;
        }

        public static string GetAppConnectionString(string key)
        {
            string val = null;
            ConnectionStringSettings mySetting = ConfigurationManager.ConnectionStrings[key];
            if (mySetting == null || string.IsNullOrEmpty(mySetting.ConnectionString))
            {
                throw new Exception(string.Format("Fatal error: missing connecting string in web.config file for the '{0}' key", key));
            }
            val = mySetting.ConnectionString;
            return val;
        }
    }
}