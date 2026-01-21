using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static VisAssistDatabaseBackEnd.DataUtilities.ConnectionsUtilities;

namespace VisAssistDatabaseBackEnd.DataUtilities
{
    internal class DatabaseSeeding
    {
        //SEEDING DATA
        public static void SeedProjects()
        {
            using (SQLiteConnection sqliteconConnection = new SQLiteConnection(DatabaseConfig.ConnectionString))
            {
                sqliteconConnection.Open();

                string sInsert = @"
                        INSERT INTO project_table (
                            ProjectName, CustomerName, CreatedDate, LastModifiedDate, 
                            JobName, JobNumber, JobCity, JobState, JobStreetAddress1, JobStreetAddress2, JobZipCode,
                            ControlContractorName, ControlContractorCity, ControlContractorState, ControlContractorStreetAddress1, 
                            ControlContractorStreetAddress2, ControlContractorZipCode, ControlContractorPhone, ControlContractorEmail,
                            MechanicalEngineer, MechanicalContractor, DesignedBy, ReviewedBy, FileCount)
                        SELECT
                            'North Campus BAS Upgrade', 'Evergreen Health Systems','2026-01-05', '2026-01-10',
                            'North Campus Mechanical Renovation', 'EHS-24017','Denver', 'CO',
                            '1850 Clarkson St', 'Building C', '80218','Rocky Mountain Controls', 'Denver', 'CO',
                            '720 W 10th Ave', 'Suite 400', '80204','303-555-9123', 'projects@rmcontrols.com',
                            'Morrison Engineering', 'Front Range Mechanical','J. McCartney', 'A. Simmons',18
                        WHERE NOT EXISTS 
                            (SELECT 1 FROM project_table);";

                using (SQLiteCommand sqlitecmdCommand = new SQLiteCommand(sInsert, sqliteconConnection))
                {
                    int iRowsInserted = sqlitecmdCommand.ExecuteNonQuery();
                    if(iRowsInserted == 0)
                    {
                        MessageBox.Show("There is already a record in the project_table.", "VisAssist");
                    }
                }
            }
        }


        public static void SeedFiles()
        {
            //create a new sqlite connection
            using (SQLiteConnection sqliteconConnection = new SQLiteConnection(DatabaseConfig.ConnectionString))
            {
                //open the connection
                sqliteconConnection.Open();
                //this is the practice data sql...adds one file 
                //create a new command using the sql statement (sInsert) and the open connection
                string sInsert = @"INSERT INTO files_table
                                (ProjectID, RevisionID, FileName, FilePath, CreatedDate, LastModifiedDate, Version, Class,
                                DrawingType, WirePrefix, IgnoreWireColor, AllowDuplicateTags, ShowPointData)
                                VALUES
                                (1, 1, 'NorthCampus_BAS.dwg', 'C:\\Projects\\NorthCampus\\BAS', '2026-01-05 08:30:00', 
                                '2026-01-10 17:00:00', '1.2.1', 'VisAssistDocument', 'Mechanical', 'WP-', FALSE, TRUE, FALSE),
                                (1, 1, 'NorthCampus_ELEC.dwg', 'C:\\Projects\\NorthCampus\\Electrical',
                                '2026-01-06 09:15:00', '2026-01-11 16:45:00','1.0.0', 'VisAssistDocument', 'Electrical', 'EL-', TRUE, FALSE, TRUE);";
                //create a new command using the sql statement (sInsert) and the open connection
                using (SQLiteCommand sqlitecmdCommand = new SQLiteCommand(sInsert, sqliteconConnection))
                {
                    //execute the command line (in this case it is an INSERT)
                    sqlitecmdCommand.ExecuteNonQuery();

                }

            }
        }

        public static void SeedPages()
        {
            //create a new sqlite connection
            using (SQLiteConnection sqliteconConnection = new SQLiteConnection(DatabaseConfig.ConnectionString))
            {
                //open the connection
                sqliteconConnection.Open();
                //this is the practice data sql...
                string sInsert = @"INSERT INTO pages_Table (PageName, ProjectID, FileID, PageIndex, CreatedDate, LastModifiedDate,
                                Version, Class, Orientation, Scale) 
                                VALUES('North Campus BAS Sheet 1', 1, 1, 1, '2026-01-05 08:30:00', '2026-01-10 17:00:00','v1.0', 'A', 'Landscape', '1:50'),
                                ('North Campus BAS Sheet 2', 1, 1, 2, '2026-01-05 08:45:00', '2026-01-10 17:00:00','v1.0', 'A', 'Portrait', '1:50'),
                                ('Central Library HVAC Sheet 1', 1, 1, 3, '2025-11-18 08:00:00', '2026-01-02 15:30:00','v2.0', 'B', 'Landscape', '1:100'),
                                ('Central Library HVAC Sheet 2', 1, 2, 1, '2025-11-18 08:15:00', '2026-01-02 15:30:00','v2.0', 'B', 'Portrait', '1:100'),
                                ('Data Center Cooling Sheet 1', 1, 2, 2, '2025-12-01 10:00:00', '2026-01-11 14:00:00','v1.0', 'C', 'Landscape', '1:75'),
                                ('Data Center Cooling Sheet 2', 1, 2, 3, '2025-12-02 09:15:00', '2026-01-11 13:45:00','v1.1', 'C', 'Portrait', '1:75');";

                //create a new command using the sql statement (sInsert) and the open connection
                using (SQLiteCommand sqlitecmdCommand = new SQLiteCommand(sInsert, sqliteconConnection))
                {
                    //execute the command line (in this case it is an INSERT)
                    sqlitecmdCommand.ExecuteNonQuery();

                }
            }
        }



    }
}
