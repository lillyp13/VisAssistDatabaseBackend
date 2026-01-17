using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace VisAssistDatabaseBackEnd.DataUtilities
{
    internal class ConnectionsUtilities
    {

        public static SQLiteConnection Connection => new SQLiteConnection(DatabaseConfig.ConnectionString);
        internal static void InitializeDatabase()
        {
            
            //ensure the folder exists and if not create it
            bool bFolderAlreadyExists = CheckForDatabaseDirectory();

            if (bFolderAlreadyExists)
            {
                bool bDatabaseFileExists = File.Exists(DatabaseConfig.DatabasePath);
                if (!bDatabaseFileExists)
                {
                    //the folder didn't exist so this is the first time we are creating the database...

                    //logging here 
                    //create the project_table
                    using (SQLiteConnection connection = new SQLiteConnection(Connection))
                    {
                        connection.Open();
                        string sProjectTableCommand = @"
                CREATE TABLE IF NOT EXISTS project_table (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ProjectName TEXT NOT NULL,
                    CustomerName TEXT,
                    CreatedDate TEXT NOT NULL,
                    ModifiedDate TEXT NOT NULL,
                    JobName TEXT,
                    JobNumber TEXT,
                    JobCity TEXT,
                    JobState TEXT,
                    JobStreetAddress1 TEXT,
                    JobStreetAddress2 TEXT,
                    JobZipCode TEXT,
                    ControlContractorName TEXT,
                    ControlContractorCity TEXT,
                    ControlContractorState TEXT,
                    ControlContractorStreetAddress1 TEXT,
                    ControlContractorStreetAddress2 TEXT,
                    ControlContractorZipCode TEXT,
                    ControlContractorPhone TEXT,
                    ControlContractorEmail TEXT,
                    MechanicalEngineer TEXT,
                    MechanicalContractor TEXT,
                    DesignedBy TEXT,
                    ReviewedBy TEXT,
                    FileCount INTEGER DEFAULT 0
                );";

                        using (SQLiteCommand cmd = new SQLiteCommand(sProjectTableCommand, connection))
                        {
                            cmd.ExecuteNonQuery();
                            //logging here 
                        }
                    }

                    //logging here 
                    //create the files_table
                    using (SQLiteConnection connection = new SQLiteConnection(Connection))
                    {
                        connection.Open();
                        string sFileTableCommand = @"
                CREATE TABLE IF NOT EXISTS files_table (
                    FileID INTEGER PRIMARY KEY AUTOINCREMENT,
                    ProjectID INTEGER NOT NULL,
                    RevisionID INTEGER,
                    FileName TEXT NOT NULL,
                    FilePath TEXT,
                    CreatedDate TEXT,
                    LastModifiedDate TEXT,
                    Version TEXT,
                    Class TEXT,
                    DrawingType TEXT,
                    WirePrefix TEXT,
                    IgnoreWireColor INTEGER DEFAULT 0,
                    AllowDuplicateTags INTEGER DEFAULT 0,
                    ShowPointData INTEGER DEFAULT 0,
                    FOREIGN KEY(ProjectID) REFERENCES project_table(Id)
                );
                ";
                        using (SQLiteCommand cmd = new SQLiteCommand(sFileTableCommand, connection))
                        {
                            //logging here 
                            cmd.ExecuteNonQuery();

                        }

                    }

                    //logging here
                    //create the pages_table
                    using (SQLiteConnection connection = new SQLiteConnection(Connection))
                    {
                        connection.Open();
                        string sPageTableCommand = @"
                CREATE TABLE IF NOT EXISTS pages_table (
                    PageID INTEGER PRIMARY KEY AUTOINCREMENT,
                    PageName TEXT NOT NULL,
                    ProjectID INTEGER NOT NULL,
                    FileID INTEGER NOT NULL,
                    PageIndex INTEGER,
                    CreatedDate TEXT,
                    LastModifiedDate TEXT,
                    Version TEXT,
                    Class TEXT,
                    Orientation TEXT,
                    Scale TEXT,
                    FOREIGN KEY(ProjectID) REFERENCES project_table(Id),
                    FOREIGN KEY(FileID) REFERENCES files_table(FileID)
                );
                ";

                        using (SQLiteCommand cmd = new SQLiteCommand(sPageTableCommand, connection))
                        {
                            cmd.ExecuteNonQuery();
                            //logging here
                        }
                    }


                    //                    //create the wire_shapes_table
                    //                    using (SQLiteConnection connection = new SQLiteConnection(SQLiteConnectionFactory.Create()))
                    //                    {
                    //                        connection.Open();
                    //                        string sWireTableCommand = @"
                    //CREATE TABLE IF NOT EXISTS wire_shapes_table(
                    //    wire_id INTEGER NOT NULL,
                    //    project_id INTEGER NOT NULL,
                    //    file_id INTEGER NOT NULL,
                    //    page_id INTEGER NOT NULL,
                    //    wire_pair_id INTEGER NOT NULL,
                    //    system_id INTEGER,
                    //    connection_id INTEGER,
                    //    wire_pair_role TEXT NOT NULL,
                    //    tag TEXT,
                    //    version TEXT,
                    //    class TEXT,
                    //    wire_label TEXT,
                    //    color TEXT,
                    //    x_location REAL NOT NULL,
                    //    y_location REAL NOT NULL,
                    //    auto_labeling INTEGER NOT NULL,
                    //    conductor_count INTEGER NOT NULL,
                    //    conductor_1_label TEXT,
                    //    conductor_2_label TEXT,
                    //    conductor_3_label TEXT,
                    //    conductor_4_label TEXT,
                    //    conductor_5_label TEXT,
                    //    conductor_6_label TEXT,
                    //    conductor_7_label TEXT,
                    //    conductor_8_label TEXT,
                    //    conductor_9_label TEXT,
                    //    conductor_10_label TEXT,
                    //    conductor_11_label TEXT,
                    //    conductor_12_label TEXT,
                    //    show_shield INTEGER NOT NULL,
                    //    shield_top INTEGER,
                    //    shield_bottom INTEGER,
                    //    PRIMARY KEY(wire_id),
                    //    CONSTRAINT wire_pairs_wire_shapes
                    //        FOREIGN KEY (wire_pair_id) REFERENCES wire_pairs_table (wire_pair_id),
                    //    CONSTRAINT project_info_wire_shapes
                    //        FOREIGN KEY (project_id) REFERENCES project_table (project_id),
                    //    CONSTRAINT pages_wire_shapes
                    //        FOREIGN KEY (page_id) REFERENCES pages_table (page_id),
                    //    CONSTRAINT visio_files_wire_shapes
                    //        FOREIGN KEY (file_id) REFERENCES files_table (file_id),
                    //    CONSTRAINT connections_wire_shapes
                    //        FOREIGN KEY (connection_id) REFERENCES connections_table (connection_id)
                    //);";

                    //                        using (SQLiteCommand cmd = new SQLiteCommand(sWireTableCommand, connection))
                    //                        {
                    //                            cmd.ExecuteNonQuery();

                    //                        }
                    //                    }

                    //                    //create the connections_table
                    //                    using (SQLiteConnection connection = new SQLiteConnection(SQLiteConnectionFactory.Create()))
                    //                    {
                    //                        connection.Open();
                    //                        string sConnectionsTableCommand = @"
                    //            CREATE TABLE IF NOT EXISTS connections_table (
                    //                connection_id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                    //                fromshape_id INTEGER NOT NULL,
                    //                to_shape_id INTEGER,
                    //                from_shape_class TEXT NOT NULL,
                    //                to_shape_class TEXT
                    //            );";
                    //                        using (SQLiteCommand cmd = new SQLiteCommand(sConnectionsTableCommand, connection))
                    //                        {
                    //                            cmd.ExecuteNonQuery();

                    //                        }
                    //                    }

                    //                    //create the wire_pairs_table
                    //                    using (SQLiteConnection connection = new SQLiteConnection(SQLiteConnectionFactory.Create()))
                    //                    {
                    //                        connection.Open();
                    //                        string sWirePairsTableCommand = @"
                    //            CREATE TABLE IF NOT EXISTS wire_pairs_table (
                    //                wire_pair_id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                    //                primary_wire_id INTEGER NOT NULL,
                    //                secondary_wire_id INTEGER NOT NULL,
                    //                CONSTRAINT fk_primary_wire FOREIGN KEY (primary_wire_id) REFERENCES wire_shapes_table(wire_id),
                    //                CONSTRAINT fk_secondary_wire FOREIGN KEY (secondary_wire_id) REFERENCES wire_shapes_table(wire_id)
                    //            );";
                    //                        using (SQLiteCommand cmd = new SQLiteCommand(sWirePairsTableCommand, connection))
                    //                        {
                    //                            cmd.ExecuteNonQuery();

                    //                        }

                    //                    }

                }
                else
                {
                    //logging here
                    MessageBox.Show("Database file already exists.");
                }
            }
            else
            {
                //logging here
                MessageBox.Show("Database directory and database exist" , "VisAssist");
            }

        }


        public static bool CheckForDatabaseDirectory()
        {
            bool bFolderAlreadyExists = false;
            string sFolder = Path.GetDirectoryName(DatabaseConfig.DatabasePath);
            if (!Directory.Exists(sFolder))
            {
                //the folder didn't exist
                Directory.CreateDirectory(sFolder);
            }
            else
            {
                //the folder already exists
                bFolderAlreadyExists = true;
            }


            return bFolderAlreadyExists;
        }


    }



    //just to get where to save the database
    internal static class DatabaseConfig
    {
        public static string DatabasePath
        {
            get
            {
                // Save the database on the desktop for now
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                // Put the DB in a VisAssist folder on the desktop
                return Path.Combine(desktopPath, "VisAssist", "VisAssistBackEnd.db");
            }
        }

        public static string ConnectionString =>
            $"Data Source={DatabasePath};Version=3;";
    }

}
