using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Visio = Microsoft.Office.Interop.Visio;

namespace VisAssistDatabaseBackEnd.DataUtilities
{
    internal class ConnectionsUtilities
    {

        public static SQLiteConnection Connection => new SQLiteConnection(DatabaseConfig.ConnectionString);
        internal static void InitializeDatabase(string sFilePath)
        {
            
            //ensure the folder exists and if not create it
            bool bFolderAlreadyExists = CheckForDatabaseDirectory(sFilePath);

            if (bFolderAlreadyExists)
            {
                bool bDatabaseFileExists = File.Exists(DatabaseConfig.DatabasePath);
                if (!bDatabaseFileExists)
                {
                    //the folder didn't exist so this is the first time we are creating the database...

                    //logging here 
                    //create the project_table
                    using (SQLiteConnection connection = new SQLiteConnection(DatabaseConfig.ConnectionString))
                    {
                        connection.Open();
                        string sProjectTableCommand = @"
                CREATE TABLE IF NOT EXISTS project_table (
                    Id TEXT PRIMARY KEY,
                    ProjectName TEXT NOT NULL,
                    CustomerName TEXT,
                    CreatedDate TEXT NOT NULL,
                    LastModifiedDate TEXT NOT NULL,
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
                    using (SQLiteConnection connection = new SQLiteConnection(DatabaseConfig.ConnectionString))
                    {
                        connection.Open();
                        //enable foreign key enforcemnt for this connection
                        using (SQLiteCommand sqlitcmdPragma = new SQLiteCommand("PRAGMA foreign_keys = ON;", connection))
                        {
                            sqlitcmdPragma.ExecuteNonQuery();
                        }
                        string sFileTableCommand = @"
                CREATE TABLE IF NOT EXISTS files_table (
                    FileID TEXT PRIMARY KEY,
                    ProjectID TEXT NOT NULL,
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
                    FOREIGN KEY(ProjectID) REFERENCES project_table(Id) ON DELETE CASCADE
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
                    using (SQLiteConnection connection = new SQLiteConnection(DatabaseConfig.ConnectionString))
                    {
                        connection.Open();

                        //enable foreign key enforcemnt for this connection
                        using (SQLiteCommand sqlitcmdPragma = new SQLiteCommand("PRAGMA foreign_keys = ON;", connection))
                        {
                            sqlitcmdPragma.ExecuteNonQuery();
                        }
                        string sPageTableCommand = @"
                CREATE TABLE IF NOT EXISTS pages_table (
                    PageID TEXT PRIMARY KEY,
                    PageName TEXT NOT NULL,
                    ProjectID TEXT NOT NULL,
                    FileID TEXT NOT NULL,
                    PageIndex INTEGER,
                    CreatedDate TEXT,
                    LastModifiedDate TEXT,
                    Version TEXT,
                    Class TEXT,
                    Orientation TEXT,
                    Scale TEXT,
                    FOREIGN KEY(ProjectID) REFERENCES project_table(Id) ON DELETE CASCADE,
                    FOREIGN KEY(FileID) REFERENCES files_table(FileID) ON DELETE CASCADE
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

                    ////enable foreign key enforcemnt for this connection
                    //using (SQLiteCommand sqlitcmdPragma = new SQLiteCommand("PRAGMA foreign_keys = ON;", connection))
                    //{
                    //    sqlitcmdPragma.ExecuteNonQuery();
                    //}
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
                    //        FOREIGN KEY (wire_pair_id) REFERENCES wire_pairs_table (wire_pair_id) ON DELETE CASCADE,
                    //    CONSTRAINT project_info_wire_shapes
                    //        FOREIGN KEY (project_id) REFERENCES project_table (project_id) ON DELETE CASCADE,
                    //    CONSTRAINT pages_wire_shapes
                    //        FOREIGN KEY (page_id) REFERENCES pages_table (page_id) ON DELETE CASCADE,
                    //    CONSTRAINT visio_files_wire_shapes
                    //        FOREIGN KEY (file_id) REFERENCES files_table (file_id) ON DELETE CASCADE,
                    //    CONSTRAINT connections_wire_shapes
                    //        FOREIGN KEY (connection_id) REFERENCES connections_table (connection_id) ON DELETE CASCADE
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
                    ////enable foreign key enforcemnt for this connection
                    //using (SQLiteCommand sqlitcmdPragma = new SQLiteCommand("PRAGMA foreign_keys = ON;", connection))
                    //{
                    //    sqlitcmdPragma.ExecuteNonQuery();
                    //}
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
                    ////enable foreign key enforcemnt for this connection
                    //using (SQLiteCommand sqlitcmdPragma = new SQLiteCommand("PRAGMA foreign_keys = ON;", connection))
                    //{
                    //    sqlitcmdPragma.ExecuteNonQuery();
                    //}
                    //                        string sWirePairsTableCommand = @"
                    //            CREATE TABLE IF NOT EXISTS wire_pairs_table (
                    //                wire_pair_id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                    //                primary_wire_id INTEGER NOT NULL,
                    //                secondary_wire_id INTEGER NOT NULL,
                    //                CONSTRAINT fk_primary_wire FOREIGN KEY (primary_wire_id) REFERENCES wire_shapes_table(wire_id) ON DELETE CASCADE,
                    //                CONSTRAINT fk_secondary_wire FOREIGN KEY (secondary_wire_id) REFERENCES wire_shapes_table(wire_id) ON DELETE CASCADE
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

        internal static void DeleteDatabase()
        {
            try
            {
                string sFilePath = DatabaseConfig.DatabasePath;
                if (System.IO.File.Exists(sFilePath))
                {
                    System.IO.File.Delete(sFilePath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in DeleteDatabase " + ex.Message, "VisAssist");
            }
        }

        public static bool CheckForDatabaseDirectory(string sFilePath)
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
    //internal static class DatabaseConfig
    //{
    //    private static string m_databasePath;

    //    public static string DatabasePath
    //    {
    //        get
    //        {
    //            if (string.IsNullOrEmpty(m_databasePath))
    //            {
    //                //throw new InvalidOperationException("Database path has not been set.");
    //                //populate this by getting the curernt document and seeing where that document is saved
    //                Visio.Application ovApp = Globals.ThisAddIn.Application;
    //                if(ovApp.Documents.Count == 0 || ovApp.ActiveDocument == null)
    //                {
    //                    return null;// no document open 
    //                }
    //                Visio.Document ovDoc = ovApp.ActiveDocument;
    //                string sFolderPath = FileUtilities.ReturnFileStructurePath();

    //                m_databasePath = sFolderPath + "VisAssistBackEnd.db";


    //            }

    //            return m_databasePath;
    //        }
    //        set
    //        {
    //            m_databasePath = value;
    //        }
    //        ///HARDCODED TO THE DESKTOP
    //        //get
    //        //{
    //        //    // Save the database on the desktop for now
    //        //    string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

    //        //    // Put the DB in a VisAssist folder on the desktop
    //        //    return Path.Combine(desktopPath, "VisAssist", "VisAssistBackEnd.db");
    //        //}
    //    }

    //    public static string ConnectionString =>
    //        $"Data Source={DatabasePath};Version=3;";
    //}

    internal static class DatabaseConfig
    {
        private static string m_databasePath;

        public static string DatabasePath
        {
            get => m_databasePath;
            set => m_databasePath = value;
        }

        public static string ConnectionString
        {
            get
            {
                if (string.IsNullOrEmpty(m_databasePath))
                    throw new InvalidOperationException(
                        "DatabaseConfig has not been bound to an active document.");

                return $"Data Source={m_databasePath};Version=3;";
            }
        }

        /// <summary>
        /// Binds the database path to the currently active Visio document.
        /// Call this before any DB access to ensure that we are using the correct connection string...
        /// </summary>
        public static bool BindToActiveDocument()
        {
            Visio.Application app = Globals.ThisAddIn.Application;

            if (app.Documents.Count == 0 || app.ActiveDocument == null)
                return false;

            Visio.Document doc = app.ActiveDocument;

            // Unsaved document → no filesystem location yet
            if (string.IsNullOrEmpty(doc.FullName))
                return false;

            string sFolderPath = FileUtilities.ReturnFileStructurePath(doc.Path);

            sFolderPath = Path.GetDirectoryName(sFolderPath);

            DatabasePath = Path.Combine(sFolderPath, "DB", "VisAssistBackEnd.db");
            return true;
        }
    }


}
