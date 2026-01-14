using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Text;
using System.Threading.Tasks;
using static VisAssistDatabaseBackEnd.DataUtilities.ConnectionsUtilities;

namespace VisAssistDatabaseBackEnd.DataUtilities
{
    internal class DatabaseSeeding
    {
        public static void SeedProjects()
        {
            //create a new sqlite connection
            using (SQLiteConnection connection = new SQLiteConnection(SQLiteConnectionFactory.Create()))
            {
                //open the connection
                connection.Open();

                //this is the practice data sql...
                string sInsert = GetProjectSeedData();

                //create a new command using the sql statement (sInsert) and the open connection
                using (SQLiteCommand cmd = new SQLiteCommand(sInsert, connection))
                {
                    //execute the command (in this case it is an INSERT)
                    cmd.ExecuteNonQuery();

                }
            }
        }

        private static string GetProjectSeedData()
        {
            string sData = @"
        INSERT INTO project_table (
            ProjectName, CustomerName, CreatedDate, ModifiedDate,
            JobName, JobNumber, JobCity, JobState,
            JobStreetAddress1, JobStreetAddress2, JobZipCode,
            ControlContractorName, ControlContractorCity, ControlContractorState,
            ControlContractorStreetAddress1, ControlContractorStreetAddress2, ControlContractorZipCode,
            ControlContractorPhone, ControlContractorEmail,
            MechanicalEngineer, MechanicalContractor,
            DesignedBy, ReviewedBy, FileCount
        ) VALUES
        (
            'North Campus BAS Upgrade', 'Evergreen Health Systems',
            '2026-01-05', '2026-01-10',
            'North Campus Mechanical Renovation', 'EHS-24017',
            'Denver', 'CO',
            '1850 Clarkson St', 'Building C', '80218',
            'Rocky Mountain Controls', 'Denver', 'CO',
            '720 W 10th Ave', 'Suite 400', '80204',
            '303-555-9123', 'projects@rmcontrols.com',
            'Morrison Engineering', 'Front Range Mechanical',
            'J. McCartney', 'A. Simmons',
            18
        );";
            return sData;
        }

        public static void SeedFiles()
        {
            //create a new sqlite connection
            using (SQLiteConnection connection = new SQLiteConnection(SQLiteConnectionFactory.Create()))
            {
                //open the connection
                connection.Open();
                //this is the practice data sql...
                string sInsert = GetFileSeedData();

                //create a new command using the sql statement (sInsert) and the open connection
                using (SQLiteCommand cmd = new SQLiteCommand(sInsert, connection))
                {
                    //execute the command line (in this case it is an INSERT)
                    cmd.ExecuteNonQuery();

                }
            }
        }

        public static void SeedPages()
        {
            //create a new sqlite connection
            using (SQLiteConnection connection = new SQLiteConnection(SQLiteConnectionFactory.Create()))
            {
                //open the connection
                connection.Open();
                //this is the practice data sql...
                string sInsert = GetPagesSeedData();

                //create a new command using the sql statement (sInsert) and the open connection
                using (SQLiteCommand cmd = new SQLiteCommand(sInsert, connection))
                {
                    //execute the command line (in this case it is an INSERT)
                    cmd.ExecuteNonQuery();

                }
            }
        }

        private static string GetFileSeedData()
        {
            string sData = @"INSERT INTO files_Table (ProjectID, RevisionID, FileName, FilePath, CreatedDate, LastModifiedDate,
                           Version, Class, DrawingType, WirePrefix, IgnoreWireColor, AllowDuplicateTags, ShowPointData) 
            VALUES
                (1, 1, 'NorthCampus_BAS.dwg', 'C:\\Projects\\NorthCampus\\BAS', '2026-01-05 08:30:00', '2026-01-10 17:00:00',
                'v1.0', 'A', 'Mechanical', 'WP-', 0, 0, 1),(1, 2, 'NorthCampus_HVAC.pdf', 'C:\\Projects\\NorthCampus\\HVAC', '2026-01-06 09:00:00', '2026-01-10 16:45:00',
                'v1.1', 'B', 'HVAC', '', 1, 0, 0),(1, 1, 'CentralLibrary_HVAC.dwg', 'C:\\Projects\\CentralLibrary\\HVAC', '2025-11-18 08:00:00', '2026-01-02 15:30:00',
                'v2.0', 'A', 'Electrical', 'CL-', 0, 1, 1),(1, 1, 'DataCenterCooling_Layout.dwg', 'C:\\Projects\\DataCenter\\Cooling', '2025-12-01 10:00:00', '2026-01-11 14:00:00',
                'v1.0', 'C', 'Mechanical', 'DC-', 0, 0, 0),(1, 2, 'DataCenterCooling_PLC.pdf', 'C:\\Projects\\DataCenter\\Cooling', '2025-12-02 09:15:00', '2026-01-11 13:45:00',
                'v1.1', 'C', 'Electrical', '', 1, 0, 1);";
            return sData;
        }

        private static string GetPagesSeedData()
        {
            string sData = @"INSERT INTO pages_Table (PageName, ProjectID, FileID, PageIndex, CreatedDate, LastModifiedDate,
                                Version, Class, Orientation, Scale) 
                            VALUES('North Campus BAS Sheet 1', 1, 1, 1, '2026-01-05 08:30:00', '2026-01-10 17:00:00','v1.0', 'A', 'Landscape', '1:50'),
                                ('North Campus BAS Sheet 2', 1, 1, 2, '2026-01-05 08:45:00', '2026-01-10 17:00:00','v1.0', 'A', 'Portrait', '1:50'),
                                ('Central Library HVAC Sheet 1', 1, 3, 1, '2025-11-18 08:00:00', '2026-01-02 15:30:00','v2.0', 'B', 'Landscape', '1:100'),
                                ('Central Library HVAC Sheet 2', 1, 3, 2, '2025-11-18 08:15:00', '2026-01-02 15:30:00','v2.0', 'B', 'Portrait', '1:100'),
                                ('Data Center Cooling Sheet 1', 1, 4, 1, '2025-12-01 10:00:00', '2026-01-11 14:00:00','v1.0', 'C', 'Landscape', '1:75'),
                                ('Data Center Cooling Sheet 2', 1, 5, 2, '2025-12-02 09:15:00', '2026-01-11 13:45:00','v1.1', 'C', 'Portrait', '1:75');";
            return sData;
        }

        internal static void UpdateProjectInfoWithSeedData()
        {
            //create a new sqlite connection
            using (SQLiteConnection connection = new SQLiteConnection(SQLiteConnectionFactory.Create()))
            {
                //open the connection
                connection.Open();

                //this is the practice data sql..
                string sUpdate = GetProjectSeedChange();

                //create a new command using the sql statment (sUpdate) and the open connection
                using (SQLiteCommand cmd = new SQLiteCommand(sUpdate, connection))
                {
                    //execute the command line (in this case it is an UPDATE)
                    cmd.ExecuteNonQuery();

                }
            }
        }

        private static string GetProjectSeedChange()
        {
            string sData = @"UPDATE project_table
                            SET 
                                ReviewedBy = 'Lilly'
                            WHERE Id = 1;";
            return sData;

        }

        
        internal static void UpdatePageInfoWithSeedData()
        {
            //crate a new sqlite connection
            using (SQLiteConnection connection = new SQLiteConnection(SQLiteConnectionFactory.Create()))
            {
                //open the connection
                connection.Open();

               //this is the practice data sql...
                string sUpdate = GetPageSeedChange();

                //create a new command using the sql statement (sUpdate) and the open connection
                using (SQLiteCommand cmd = new SQLiteCommand(sUpdate, connection))
                {
                    //execute the command line (in this case it is an UPDATE)
                    cmd.ExecuteNonQuery();

                }
            }
        }

        private static string GetPageSeedChange()
        {
            //going to update a PageName
            string sData = @"UPDATE pages_table
                            SET
                                PageName = 'Changed Page Name'
                            WHERE PageID = 5;";
            return sData;
        }

        internal static string GetPageNameWithSeedData()
        {
           //create a new sqlite connection
            using (SQLiteConnection connection = new SQLiteConnection(SQLiteConnectionFactory.Create()))
            {
                //open the connection
                connection.Open();

                //this is the practice data sql...
                string sGet = ReturnPageNameWithSeedData();

                //create a new command using the sql statement (sGet) and the open connection
                using (SQLiteCommand cmd = new SQLiteCommand(sGet, connection))
                {
                    //execute the query adn read the result
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        //moves to the first row if it exists and get the first column (our pagename..)
                        return reader.Read() ? reader.GetString(0) : null;
                    }
                }

            }
        }

        private static string ReturnPageNameWithSeedData()
        {
            string sData = @"SELECT PageName 
                            FROM pages_table
                            WHERE PageID = 5;";
            return sData;
        }
    }
}
