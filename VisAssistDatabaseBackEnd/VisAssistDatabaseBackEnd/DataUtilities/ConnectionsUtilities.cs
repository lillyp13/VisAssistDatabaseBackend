using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                    //Database file doesn't exist, so create it
                    SQLiteConnection.CreateFile(DatabaseConfig.DatabasePath);

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
                        }
                    }

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
                        );";
                        using (SQLiteCommand cmd = new SQLiteCommand(sFileTableCommand, connection))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }

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
                            );";

                        using (SQLiteCommand cmd = new SQLiteCommand(sPageTableCommand, connection))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Database file already exists.");
                }
            }
            else
            {
                MessageBox.Show("Database directory and database exist.");
            }
        }

        private static bool CheckForDatabaseDirectory()
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
