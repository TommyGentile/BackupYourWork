using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Configuration;

namespace BackupYourWork
{
    class ConfigManager
    {
        public double days_interval;
        public DirectoryInfo backup_path;
        public string backup_foldername;
        public string[] extensions;

        public bool mail_enable;
        public string mail_host;
        public int? mail_port;
        public string mail_from;
        public string mail_to;

        private static ConfigManager instance;

        private ConfigManager() { }

        public static ConfigManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new ConfigManager();
                }
                return instance;
            }
        }

        /// <summary>
        /// Carica le Impostazioni dal File Config
        /// </summary>
        public String readSettings()
        {
            Double.TryParse(ConfigurationManager.AppSettings["days_interval"], out days_interval);

            extensions = ConfigurationManager.AppSettings["extensions"].Split('|');

            string tmp_s = ConfigurationManager.AppSettings["backup_path"] ?? String.Empty;
            if (!tmp_s.EndsWith("\\")) tmp_s += "\\";
            backup_path = new DirectoryInfo(tmp_s);
            if (!backup_path.Exists) return "Backup folder path not found.";

            backup_foldername = ConfigurationManager.AppSettings["backup_foldername"] ?? "Backup";

            mail_host = ConfigurationManager.AppSettings["mail_host"];

            int tmp_i;
            if (Int32.TryParse(ConfigurationManager.AppSettings["mail_port"], out tmp_i))
                mail_port = tmp_i;

            mail_from = ConfigurationManager.AppSettings["mail_from"] ?? String.Empty;
            mail_to = ConfigurationManager.AppSettings["mail_to"] ?? String.Empty;

            Boolean tmp_b;
            if (Boolean.TryParse(ConfigurationManager.AppSettings["mail_enable"], out tmp_b))
                mail_enable = tmp_b;

            return null;
        }
    }
}
