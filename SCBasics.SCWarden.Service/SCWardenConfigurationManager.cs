using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace SCBasics.SCWarden.Service
{
    public class SCWardenConfigurationManager
    {
        public static readonly string MongoInstanceNameToCheck;
        public static readonly MongoUrl MongoDBConnectionStringUrl;
        public static readonly string MSSQLConnectionString;

        public static bool IsDiskWatcherEnabled { get; set; }

        public static string[] PartitionsToCheck { get; set; }

        public static long FreeSpaceToEnsure { get; set; }

        public static bool IsMongoDBWatcherEnabled { get; set; }

        public static string MongoConnectionStringNameToCheck { get; set; }

        public static bool IsMSSQLWatcherEnabled { get; set; }

        public static string MSSQLConnectionStringNameToCheck { get; set; }

        public static bool IsPerformanceWatcherEnabled { get; set; }

        public static double CPUThreshold { get; set; }
        public static double RAMThreshold { get; set; }

        /// <summary>
        /// Checks Keepalive/hearbeat
        /// </summary>
        public static bool IsWebWatcherEnabled { get; set; }

        //public static bool IsServerWatcherEnabled { get; set; }

        public static bool IsWebPanelIntegrationEnabled { get; set; }

        public static string WebPanelURL { get; set; }

        public static string WebPanelAPIKey { get; set; }

        public static string WebPanelOrganizationId { get; set; }


        /// <summary>
        /// Logs everything in log file. Mainly for troubleshooting
        /// </summary>
        public static bool IsInfoLoggingEnabled { get; set; }


        /// <summary>
        /// IN?
        /// </summary>
        public int Interval { get; set; }
        
        static SCWardenConfigurationManager()
        {

            // ===================Watchers==================
            // Disk watcher
            IsDiskWatcherEnabled = true;
            PartitionsToCheck = new string[] { "C" };
            FreeSpaceToEnsure = 100000000;

            // Mongo DB
            IsMongoDBWatcherEnabled = true;
            MongoConnectionStringNameToCheck = "analytics";
            MongoInstanceNameToCheck = "HILP2 - sc82rev160617";

            if(IsMongoDBWatcherEnabled)
            {
                var connectionString = 
                    ConfigurationManager.ConnectionStrings[MongoConnectionStringNameToCheck].ConnectionString;
                                
                MongoClient client = new MongoClient(connectionString);

                MongoDBConnectionStringUrl = new MongoUrl(connectionString);

            }
            // MS SQL
            IsMSSQLWatcherEnabled = true;
            MSSQLConnectionStringNameToCheck = "master";
            if(IsMSSQLWatcherEnabled)
            {
                MSSQLConnectionString = ConfigurationManager.ConnectionStrings[MSSQLConnectionStringNameToCheck].ConnectionString;
            }

            // Performance Watcher
            IsPerformanceWatcherEnabled = true;
            CPUThreshold = 80;
            RAMThreshold = 10;

            // Web Watcher
            IsWebWatcherEnabled = true;
            

            // ===================Integrations==================
            // Web Panel Integration
            IsWebPanelIntegrationEnabled = true;
            WebPanelURL = "https://panel.getwarden.net/api";
            WebPanelAPIKey = "rm66vO3apAyvhrv3E8/Y9zON0jK7G+nTtGiz6k/jzpUZmsPu+WEkJ+NoM+8ftxBu1K/LYsUGgAy1";
            WebPanelOrganizationId = "f99be150-da32-4ebb-a63d-1ce5a845ad22";

            // Misc
            IsInfoLoggingEnabled = true;
        }



    }
}
