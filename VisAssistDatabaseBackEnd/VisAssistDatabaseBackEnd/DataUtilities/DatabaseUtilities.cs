using Microsoft.Office.Interop.Visio;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Lifetime;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Web;
using System.Windows.Forms;
using VisAssistDatabaseBackEnd.ShapeUtilities;
using VisAssistDatabaseBackEnd.VisioUtilities;
using static System.Net.Mime.MediaTypeNames;
using static System.Net.WebRequestMethods;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;
using Visio = Microsoft.Office.Interop.Visio;

namespace VisAssistDatabaseBackEnd.DataUtilities
{
    internal class DatabaseUtilities
    {

        //Sql Table Struct
        public struct SqlTables
        {
            public struct ProjectTable
            {
                public const string sProjectTable = "project_table";
                public const string sProjectTablePK = "ProjectID";
                //don't know if i'll ever need to saProjectColumns...
                public static readonly string[] saProjectColumns = new string[]
                {
                "ProjectName", "CustomerName", "CreatedDate", "LastModifiedDate", "JobName", "JobNumber", "JobCity", "JobState", "JobStreetAddress1",
                "JobStreetAddress2", "JobZipCode", "ControlContractorName", "ControlContractorCity", "ControlContractorState", "ControlContractorStreetAddress1",
                "ControlContractorStreetAddress2", "ControlContractorZipCode", "ControlContractorPhone", "ControlContractorEmail", "MechanicalEngineer", "MechanicalContractor",
                "DesignedBy", "ReviewedBy", "FileCount"
                };
            }
            public struct FilesTable
            {
                public const string sFilesTable = "files_table";
                public const string sFilesTablePK = "FileID";
                public static readonly string[] saFileColumns = new string[]
            {
                "ProjectID", "RevisionID", "FileName", "FilePath", "CreatedDate",
                "LastModifiedDate", "Version", "Class", "DrawingType", "WirePrefix",
                "IgnoreWireColor", "AllowDuplicateTags", "ShowPointData"
            };
            }
            public struct PagesTable
            {
                public const string sPagesTable = "pages_table";
                public const string sPagesTablePK = "PageID";

                public static readonly string[] saPagesColumns = new string[]
                {
                "PageName", "ProjectID", "FileID", "PageIndex", "CreatedDate", "LastModifiedDate", "Version", "Class", "Orientation", "Scale"
                };
            }

            public struct WireShapesTable
            {
                public const string sWireShapeTable = "wire_shapes_table";
                public const string sWireShapeTablePK = "ShapeID";

                public static readonly string[] saWireShapesColumns = new string[]
              {
                "ProjectID", "FileID", "PageID", "WirePairID", "SystemID", "ConnectionID", "WirePairRole", "Tag", "Version",
                "Class", "WireLabel", "Color", "XLocation", "YLocation", "AutoLabeling",
                "ConductorCount", "Conductor1Label", "Conductor2Label", "Conductor3Label", "Conductor4Label", "Conductor5Label",
                "Conductor6Label", "Conductor7Label", "Conductor8Label", "Conductor9Label", "Conductor10Label", "ShowShield", "ShieldTop", "ShieldBottom"
              };


            }

            public struct WirePairsTable
            {
                public const string sWirePairsTable = "wire_pairs_table";
                public const string sWirePairsTablePK = "WirePairID";

                public static readonly string[] saWirePairsTableColumns
                    = new string[]
               {
                "WirePairID", "PrimaryWireID", "SecondaryWireID"
               };

            }

            public struct TerminalBlocksTable
            {
                public const string sTerminalBlockTable = "terminal_block_table";
                public const string sTerminalBlockTablePK = "ShapeID";


                public static readonly string[] saTerminalBlockColumns = new string[]
                {
                "ProjectID", "FileID", "PageID", "Color", "ShapeText"
                };
            }

            public struct WiringEndDevice
            {
                public const string sWiringEndDeviceTable = "wiring_end_device_table";
                public const string sWiringEndDeviceTablePK = "ShapeID";


                public static readonly string[] saWiringEndDeviceColumns = new string[]
                {
                "ProjectID", "FileID", "PageID", "TermCount", "Tag"
                };
            }

            public static readonly Dictionary<string, (string PrimaryKey, string[] Columns)> odictTableInfo = new Dictionary<string, (string PrimaryKey, string[] Columns)>()
            {
                { ProjectTable.sProjectTable, (ProjectTable.sProjectTablePK, ProjectTable.saProjectColumns) },
                { FilesTable.sFilesTable, (FilesTable.sFilesTablePK, FilesTable.saFileColumns) },
                { PagesTable.sPagesTable, (PagesTable.sPagesTablePK, PagesTable.saPagesColumns) },
                { WireShapesTable.sWireShapeTable, (WireShapesTable.sWireShapeTablePK, WireShapesTable.saWireShapesColumns) },
                { TerminalBlocksTable.sTerminalBlockTable, (TerminalBlocksTable.sTerminalBlockTablePK, TerminalBlocksTable.saTerminalBlockColumns) },
                { WiringEndDevice.sWiringEndDeviceTable, (WiringEndDevice.sWiringEndDeviceTablePK, WiringEndDevice.saWiringEndDeviceColumns) },
                { WirePairsTable.sWirePairsTable, (WirePairsTable.sWirePairsTablePK, WirePairsTable.saWirePairsTableColumns) }

            };



            public static bool TryGetPrimaryKey(string sTableName, out string sPK)
            {
                sPK = null;

                if (string.IsNullOrWhiteSpace(sTableName))
                    return false;

                if (odictTableInfo.TryGetValue(sTableName, out (string PrimaryKey, string[] Columns) oTableInfo))
                {
                    sPK = oTableInfo.PrimaryKey;
                    return true;
                }

                return false;
            }

        }

        public static readonly Dictionary<string, string> ColorMap =
     new Dictionary<string, string>
 {
    { "Yellow", "RGB(255,255,170)" },
    { "Red", "RGB(255,164,164)" },
    { "Green", "RGB(198,230,162)" },
    { "Blue", "RGB(136,196,255)" },
    { "Pink", "RGB(255,193,255)" },
    { "Orange", "RGB(255,224,132)" },
    { "Purple", "RGB(200,188,214)" },
     {"Teal", "RGB(166,255,255)" }
 };
        // Ordered color sequence
        public static readonly string[] WireColorOrder =
        {
    "Yellow",
    "Red",
    "Green",
    "Blue",
    "Pink",
    "Orange",
    "Purple",
    "Teal"
};


        internal static string GetPrimaryKey(string tableName)
        {
            switch (tableName)
            {
                case "files_table": return SqlTables.FilesTable.sFilesTablePK;
                case "pages_table": return SqlTables.PagesTable.sPagesTablePK;
                case "wire_shapes_table": return SqlTables.WireShapesTable.sWireShapeTablePK;
                case "terminal_block_table": return SqlTables.TerminalBlocksTable.sTerminalBlockTablePK;
                case "wiring_end_device_table": return SqlTables.WiringEndDevice.sWiringEndDeviceTablePK;
                case "wire_pairs_table": return SqlTables.WirePairsTable.sWirePairsTablePK;

                default: return "ProjectID"; // fallback
            }
        }




        //INITIALIZE, DELETE, CHECK FOR DATABASE
        internal static void InitializeDatabase(string sFilePath)
        {
            try
            {


                //ensure the folder exists and if not create it
                bool bFolderAlreadyExists = CheckForDatabaseDirectory(sFilePath);

                if (bFolderAlreadyExists)
                {
                    bool bDatabaseFileExists = System.IO.File.Exists(DatabaseConfig.DatabasePath);
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
                    ProjectID TEXT PRIMARY KEY,
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
                    NextWireColor TEXT,
                    NextWireNumber TEXT,
                    IgnoreWireColor INTEGER DEFAULT 0,
                    AllowDuplicateTags INTEGER DEFAULT 0,
                    ShowPointData INTEGER DEFAULT 0,
                    FOREIGN KEY(ProjectID) REFERENCES project_table(ProjectID) ON DELETE CASCADE
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
                    FOREIGN KEY(ProjectID) REFERENCES project_table(ProjectID) ON DELETE CASCADE,
                    FOREIGN KEY(FileID) REFERENCES files_table(FileID) ON DELETE CASCADE
                );
                ";

                            using (SQLiteCommand cmd = new SQLiteCommand(sPageTableCommand, connection))
                            {
                                cmd.ExecuteNonQuery();
                                //logging here
                            }
                        }


                        //create the wire_shapes_table
                        using (SQLiteConnection connection = new SQLiteConnection(DatabaseConfig.ConnectionString))
                        {
                            connection.Open();

                            //enable foreign key enforcemnt for this connection
                            using (SQLiteCommand sqlitcmdPragma = new SQLiteCommand("PRAGMA foreign_keys = ON;", connection))
                            {
                                sqlitcmdPragma.ExecuteNonQuery();
                            }
                            string sWireTableCommand = @"
                        CREATE TABLE IF NOT EXISTS wire_shapes_table(
                            ShapeID TEXT NOT NULL,
                            PageID TEXT NOT NULL,
                            WirePairID TEXT NOT NULL,
                            ConnectionID INTEGER,
                            WireName TEXT,
                            WireRole TEXT NOT NULL,
                            Version TEXT,
                            Class TEXT,
                            WireLabel TEXT,
                            Color TEXT,
                            XLocation REAL NOT NULL,
                            YLocation REAL NOT NULL,
                            AutoLabeling INTEGER NOT NULL,
                            ConductorCount INTEGER NOT NULL,
                            Conductor1Label TEXT,
                            Conductor2Label TEXT,
                            Conductor3Label TEXT,
                            Conductor4Label TEXT,
                            Conductor5Label TEXT,
                            Conductor6Label TEXT,
                            Conductor7Label TEXT,
                            Conductor8Label TEXT,
                            Conductor9Label TEXT,
                            Conductor10Label TEXT,
                            GridLocation TEXT,
                            Shield TEXT,
                            PRIMARY KEY(ShapeID),
                            CONSTRAINT WirePairsWireShapes
                                FOREIGN KEY (WirePairID) REFERENCES wire_pairs_table (WirePairID) ON DELETE CASCADE,
                            CONSTRAINT pages_wire_shapes
                                FOREIGN KEY (PageID) REFERENCES pages_table (PageID) ON DELETE CASCADE
                            );";

                            //HAVEN'T ADDED THE CONNECTIONS TABLE YET...
                            //CONSTRAINT connections_wire_shapes
                            //    FOREIGN KEY (ConnectionID) REFERENCES connections_table (ConnectionID) ON DELETE CASCADE

                            using (SQLiteCommand cmd = new SQLiteCommand(sWireTableCommand, connection))
                            {
                                cmd.ExecuteNonQuery();

                            }
                        }


                        //create the wire_pairs_table
                        using (SQLiteConnection connection = new SQLiteConnection(DatabaseConfig.ConnectionString))
                        {
                            connection.Open();
                            //enable foreign key enforcemnt for this connection
                            using (SQLiteCommand sqlitcmdPragma = new SQLiteCommand("PRAGMA foreign_keys = ON;", connection))
                            {
                                sqlitcmdPragma.ExecuteNonQuery();
                            }
                            string sWirePairsTableCommand = @"
                                    CREATE TABLE IF NOT EXISTS wire_pairs_table (
                                        WirePairID TEXT NOT NULL PRIMARY KEY,
                                        PrimaryWireID TEXT NOT NULL,
                                        SecondaryWireID TEXT NOT NULL,
                                        CONSTRAINT fk_primary_wire FOREIGN KEY (PrimaryWireID) REFERENCES wire_shapes_table(ShapeID) ON DELETE CASCADE,
                                        CONSTRAINT fk_secondary_wire FOREIGN KEY (SecondaryWireID) REFERENCES wire_shapes_table(ShapeID) ON DELETE CASCADE
                                    );";
                            using (SQLiteCommand cmd = new SQLiteCommand(sWirePairsTableCommand, connection))
                            {
                                cmd.ExecuteNonQuery();

                            }

                        }


                        //create the terminal_block_table
                        using (SQLiteConnection connection = new SQLiteConnection(DatabaseConfig.ConnectionString))
                        {
                            connection.Open();
                            //enable foreign key enforcemnt for this connection
                            using (SQLiteCommand sqlitcmdPragma = new SQLiteCommand("PRAGMA foreign_keys = ON;", connection))
                            {
                                sqlitcmdPragma.ExecuteNonQuery();
                            }
                            string sTerminalBlockTable = @"
                                    CREATE TABLE IF NOT EXISTS terminal_block_table (
                                    ShapeID TEXT NOT NULL PRIMARY KEY,
                                    PageID TEXT NOT NULL,
                                    Color TEXT,
                                    ShapeText TEXT,
                                    XLocation REAL NOT NULL,
                                    YLocation REAL NOT NULL,
                                    FOREIGN KEY(PageID) REFERENCES pages_table(PageID) ON DELETE CASCADE
                                    );";
                            using (SQLiteCommand cmd = new SQLiteCommand(sTerminalBlockTable, connection))
                            {
                                cmd.ExecuteNonQuery();

                            }
                        }

                        //create the wiring_end_device
                        using (SQLiteConnection connection = new SQLiteConnection(DatabaseConfig.ConnectionString))
                        {
                            connection.Open();
                            //enable foreign key enforcemnt for this connection
                            using (SQLiteCommand sqlitcmdPragma = new SQLiteCommand("PRAGMA foreign_keys = ON;", connection))
                            {
                                sqlitcmdPragma.ExecuteNonQuery();
                            }
                            string sTerminalBlockTable = @"
                                    CREATE TABLE IF NOT EXISTS wiring_end_device_table (
                                    ShapeID TEXT NOT NULL PRIMARY KEY,
                                    PageID TEXT NOT NULL,
                                    TermCount INTEGER NOT NULL,
                                    Term1 TEXT,
                                    Term2 TEXT,
                                    Term3 TEXT,
                                    Term4 TEXT,
                                    Term5 TEXT,
                                    Term6 TEXT,
                                    Term7 TEXT,
                                    Term8 TEXT,
                                    Term9 TEXT,
                                    Term10 TEXT,
                                    Tag TEXT,
                                    XLocation REAL NOT NULL,
                                    YLocation REAL NOT NULL,
                                    FOREIGN KEY(PageID) REFERENCES pages_table(PageID) ON DELETE CASCADE
                                    );";
                            using (SQLiteCommand cmd = new SQLiteCommand(sTerminalBlockTable, connection))
                            {
                                cmd.ExecuteNonQuery();

                            }
                        }
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
                    MessageBox.Show("Database directory and database exist", "VisAssist");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in InitializeDatabase " + ex.Message, "VisAssist");
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
            string sFolder = System.IO.Path.GetDirectoryName(DatabaseConfig.DatabasePath);
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





        //SQL STATEMENTS
        //CRUD 
        internal static void BuildUpdateSqlForMultipleRecords(string sTableName, MultipleRecordUpdates mruRecords)
        {
            //Builds an Update statement based on the records in mruRecords for the table sTableName
            try
            {
                // Collect all the unique column names that need to be updated across all the records
                HashSet<string> hsAllColumns = new HashSet<string>();
                foreach (RecordUpdate rRecord in mruRecords.ruRecords)
                {
                    foreach (string scol in rRecord.odictColumnValues.Keys)
                    {
                        hsAllColumns.Add(scol);
                    }
                }

                // Determine the primary key column (assumes all records share the same PK)
                string sWhereColumn = mruRecords.ruRecords[0].sPrimaryKeyColumn;

                string sSqlUpdate = $"UPDATE {sTableName} SET ";

                using (SQLiteConnection sqliteconConnection = new SQLiteConnection(DatabaseConfig.ConnectionString))
                {
                    sqliteconConnection.Open();

                    using (SQLiteCommand sqlitecmdCommand = new SQLiteCommand(sqliteconConnection))
                    {
                        int iColIndex = 0;

                        // Loop through each column that needs to be updated
                        foreach (string sCol in hsAllColumns)
                        {
                            // Skip the primary key column; we don't update it
                            if (sCol.Equals(sWhereColumn, StringComparison.OrdinalIgnoreCase))
                                continue;

                            // Build CASE expression for this column
                            sSqlUpdate += $"{sCol} = CASE {sWhereColumn} ";

                            // Loop through all records to set the value for this column
                            foreach (RecordUpdate rRecord in mruRecords.ruRecords)
                            {
                                if (rRecord.odictColumnValues.ContainsKey(sCol))
                                {
                                    string sParameterName = $"@{sCol}_{rRecord.sId}";
                                    sSqlUpdate += $"WHEN '{rRecord.sId}' THEN {sParameterName} ";
                                    sqlitecmdCommand.Parameters.AddWithValue(sParameterName, rRecord.odictColumnValues[sCol]);
                                }
                            }

                            // Close the CASE statement for this column
                            sSqlUpdate += "END";

                            // Add a comma if this is not the last column (we'll trim at the end as a safety)
                            sSqlUpdate += ", ";

                            iColIndex++;
                        }

                        // Trim the trailing comma and space
                        sSqlUpdate = sSqlUpdate.TrimEnd(',', ' ');

                        // Add WHERE clause to update only the relevant records
                        sSqlUpdate += $" WHERE {sWhereColumn} IN ({string.Join(",", mruRecords.ruRecords.Select(r => $"'{r.sId}'"))})";

                        // Set command text and execute
                        sqlitecmdCommand.CommandText = sSqlUpdate;
                        sqlitecmdCommand.ExecuteNonQuery();
                    }
                }


            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in BuildUpdateSqlForMultipleRecords " + ex.Message, "VisAssist");
            }

        }


        internal static void BuildDeleteSqlForMultipleRecords(string sTableName, MultipleRecordUpdates mruRecords)
        {
            //Builds a Delete statement based on the records in mruRecords for the table sTableName
            try
            {
                // All records must share the same primary key column
                string sWhereColumn = mruRecords.ruRecords[0].sPrimaryKeyColumn;

                using (SQLiteConnection sqliteconConnection = new SQLiteConnection(DatabaseConfig.ConnectionString))
                {
                    sqliteconConnection.Open();

                    //enable foreign key enforcemnt for this connection
                    using (SQLiteCommand sqlitcmdPragma = new SQLiteCommand("PRAGMA foreign_keys = ON;", sqliteconConnection))
                    {
                        sqlitcmdPragma.ExecuteNonQuery();
                    }

                    using (SQLiteCommand sqlitecmdCommand = new SQLiteCommand(sqliteconConnection))
                    {
                        // Build parameterized IN clause using RecordUpdate.iId
                        List<string> lstParameterNames = new List<string>();

                        for (int i = 0; i < mruRecords.ruRecords.Count; i++)
                        {
                            string sParameterName = $"@id{i}";
                            lstParameterNames.Add(sParameterName);

                            sqlitecmdCommand.Parameters.AddWithValue(
                                sParameterName,
                                mruRecords.ruRecords[i].sId
                            );
                        }

                        string sSqlDelete =
                            $"DELETE FROM {sTableName} " +
                            $"WHERE {sWhereColumn} IN ({string.Join(",", lstParameterNames)})";

                        sqlitecmdCommand.CommandText = sSqlDelete;
                        sqlitecmdCommand.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in BuildDeleteSqlForMultipleRecords: " + ex.Message, "VisAssist");
            }

        }


        internal static void BuildInsertSqlForMultipleRecords(string sTableName, MultipleRecordUpdates mruRecords)
        {
            //Builds an Insert statement based on the records in mruRecords for the table sTableName
            try
            {
                using (SQLiteConnection sqliteconConnection = new SQLiteConnection(DatabaseConfig.ConnectionString))
                {
                    sqliteconConnection.Open();

                    using (SQLiteCommand sqlitecmdCommand = new SQLiteCommand(sqliteconConnection))
                    {
                        //add the PK...
                        DatabaseUtilities.SqlTables.TryGetPrimaryKey(sTableName, out string sPK);

                        foreach (RecordUpdate ruRecord in mruRecords.ruRecords)
                        {
                            if (!ruRecord.odictColumnValues.ContainsKey(sPK))
                            {
                                //add the pk column 
                                ruRecord.odictColumnValues.Add(sPK, ruRecord.sId);
                            }
                        }


                        // Collect all unique columns across all records
                        HashSet<string> hsAllColumns = new HashSet<string>();

                        foreach (RecordUpdate ruRecord in mruRecords.ruRecords)
                        {
                            foreach (string sColumn in ruRecord.odictColumnValues.Keys)
                            {
                                hsAllColumns.Add(sColumn);
                            }

                        }

                        // Build parameterized INSERT statement
                        string sSqlInsert = $"INSERT INTO {sTableName} ({string.Join(", ", hsAllColumns)}) VALUES ";

                        List<string> lstValues = new List<string>();
                        int iRecordIndex = 0;

                        foreach (RecordUpdate ruRecord in mruRecords.ruRecords)
                        {
                            List<string> lstParameterNames = new List<string>();

                            foreach (string sColumn in hsAllColumns)
                            {
                                string sParameterName = $"@{sColumn}_{iRecordIndex}";

                                // If this record has a value, use it; otherwise, NULL
                                if (ruRecord.odictColumnValues != null && ruRecord.odictColumnValues.ContainsKey(sColumn))
                                {
                                    sqlitecmdCommand.Parameters.Add(new SQLiteParameter(sParameterName, ruRecord.odictColumnValues[sColumn]));
                                }
                                else
                                {
                                    sqlitecmdCommand.Parameters.Add(new SQLiteParameter(sParameterName, DBNull.Value));
                                }

                                lstParameterNames.Add(sParameterName);
                            }

                            lstValues.Add("(" + string.Join(", ", lstParameterNames) + ")");
                            iRecordIndex++;
                        }

                        sSqlInsert += string.Join(", ", lstValues);

                        sqlitecmdCommand.CommandText = sSqlInsert;
                        sqlitecmdCommand.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in BuildInsertSqlForMultipleRecords: " + ex.Message, "VisAssist");
            }
        }


        internal static bool DoesTableExist(string sTableName)
        {
            //checks to make sure that the given table exists in the database (not sure if this is even needed but it is a safeguard...)
            //checks to see if the table exists in the db...
            try
            {
                //logging here
                using (SQLiteConnection sqliteconConnection = new SQLiteConnection(DatabaseConfig.ConnectionString))
                {
                    //logging here
                    sqliteconConnection.Open();
                    string sSQL = @"SELECT name FROM sqlite_master WHERE type = 'table' AND name = @TableName;";

                    using (SQLiteCommand sqlcmdCommand = new SQLiteCommand(sSQL, sqliteconConnection))
                    {
                        //logging here
                        sqlcmdCommand.Parameters.AddWithValue("@TableName", sTableName);

                        using (SQLiteDataReader sqlitereadReader = sqlcmdCommand.ExecuteReader())
                        {
                            return sqlitereadReader.Read();
                            //logging here
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in DoesTableExist " + ex.Message, "VisAssist");
            }
            return false;
        }

        internal static bool DoesParentTableHaveRecord(string sTableName)
        {
            try
            {
                //given a table we want to know if the parent table has at least one record...
                //Our array goes project_table, files_table, pages_table, wire_shapes_table 
                Dictionary<string, string> parentMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                parentMap.Add("files_table", "project_table");
                parentMap.Add("pages_table", "files_table");
                parentMap.Add("wire_shapes_table", "pages_table");


                // If the table has a parent
                if (parentMap.TryGetValue(sTableName, out string parentTable))
                {
                    // Check the parent table first
                    if (!DoesTableHaveAnyRecords(parentTable))
                        return false; // fail immediately if parent is empty

                    // Recurse upward to see if its parent has records
                    return DoesParentTableHaveRecord(parentTable);
                }

                // No parent (top-level table)
                return true; // nothing else to check
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in DoesParentTableHaveRecord " + ex.Message, "VisAssist");
                return false;
            }

        }

        internal static bool DoesTableHaveAnyRecords(string sTableName)
        {
            try
            {
                //check if the table has any records...
                string sql = $"SELECT 1 FROM {sTableName} LIMIT 1;";

                using (SQLiteConnection sqliteconConnection = new SQLiteConnection(DatabaseConfig.ConnectionString))
                {
                    sqliteconConnection.Open();

                    using (SQLiteCommand sqlitecmdCommand = new SQLiteCommand(sql, sqliteconConnection))
                    {
                        using (SQLiteDataReader reader = sqlitecmdCommand.ExecuteReader())
                        {
                            return reader.Read(); // true if at least one row exists
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in DoesTableHaveAnyRecords " + ex.Message, "VisAssist");
                return false;
            }
        }


        internal static int GetTableRecordCount(string sTableName)
        {
            try
            {
                //gets the number of records in a table...
                string sSql = $"SELECT COUNT(*) FROM {sTableName};";

                using (SQLiteConnection sqliteconConnection = new SQLiteConnection(DatabaseConfig.ConnectionString))
                {
                    sqliteconConnection.Open();

                    using (SQLiteCommand sqlitecmdCommand = new SQLiteCommand(sSql, sqliteconConnection))
                    {
                        return Convert.ToInt32(sqlitecmdCommand.ExecuteScalar());
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in GetTableRecordCount " + ex.Message, "VisAssist");
                return 0;
            }

        }
        //we want to know if there is at least one record given a table..
        internal static bool DoesRecordExist(string sTableName, string sID)
        {
            try
            {
                string sPk = GetPrimaryKey(sTableName);

                using (SQLiteConnection sqliteconConnection = new SQLiteConnection(DatabaseConfig.ConnectionString))
                {
                    sqliteconConnection.Open();

                    string sSQL = $@"SELECT 1 FROM {sTableName} WHERE {sPk} = @Id LIMIT 1;";

                    using (SQLiteCommand sqlcmdCommand = new SQLiteCommand(sSQL, sqliteconConnection))
                    {
                        sqlcmdCommand.Parameters.Add("@Id", DbType.String).Value = sID;

                        using (SQLiteDataReader sqlitereadReader = sqlcmdCommand.ExecuteReader())
                        {
                            return sqlitereadReader.Read();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in DoesRecordExist: " + ex.Message, "VisAssist");
                return false;
            }
        }

        internal static MultipleRecordUpdates GetRecordInformation(string sTableName, string sRecordID)
        {
            MultipleRecordUpdates mruRecordToReturn = new MultipleRecordUpdates();
            try
            {

                //based on the table get the pK and then gather all the information for the columns in that row
                string sPK = GetPrimaryKey(sTableName);

                string sSql = $@"SELECT * FROM [{sTableName}] WHERE [{sPK}] = @Id";

                using (SQLiteConnection sqliteconConnection = new SQLiteConnection(DatabaseConfig.ConnectionString))
                {
                    sqliteconConnection.Open();

                    using (SQLiteCommand sqlitecmdCommand = new SQLiteCommand(sSql, sqliteconConnection))
                    {
                        sqlitecmdCommand.Parameters.AddWithValue("@Id", sRecordID);
                        using (SQLiteDataReader sqlitereadReader = sqlitecmdCommand.ExecuteReader())
                        {
                            if (!sqlitereadReader.Read())
                                throw new Exception("Record not found");
                            Dictionary<string, string> oDictColumnValues = new Dictionary<string, string>();

                            for (int i = 0; i < sqlitereadReader.FieldCount; i++)
                            {
                                string sColumnName = sqlitereadReader.GetName(i);

                                if (sColumnName.Equals(DatabaseUtilities.SqlTables.PagesTable.sPagesTablePK, StringComparison.OrdinalIgnoreCase))
                                {
                                    continue; // PK not included in update dictionary
                                }

                                oDictColumnValues[sColumnName] = sqlitereadReader.IsDBNull(i) ? null : sqlitereadReader.GetValue(i).ToString();
                            }

                            RecordUpdate ru = new RecordUpdate();
                            ru.sPrimaryKeyColumn = sPK;
                            ru.sId = sRecordID;
                            ru.odictColumnValues = oDictColumnValues;

                            mruRecordToReturn.ruRecords = new List<RecordUpdate>
                            {
                                ru
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in GetRecordInformation " + ex.Message, "VisAssist");
                return mruRecordToReturn;
            }
            return mruRecordToReturn;
        }



        //INTERNAL DATA PROCCESSING

        /// <summary>
        /// given two identically built MutlipleRecordUpdates see which values have changed for each record
        /// </summary>
        /// <param name="ruBaseRecords"></param>
        /// <param name="ruCompareRecords"></param>
        /// <returns></returns>
        internal static MultipleRecordUpdates CompareDataForMultipleRecords(MultipleRecordUpdates mruRecordsBase, MultipleRecordUpdates mruRecordsToCompare)
        {
            List<RecordUpdate> ruRecordsToUpdate = new List<RecordUpdate>();

            try
            {
                foreach (RecordUpdate ruBase in mruRecordsBase.ruRecords)
                {
                    // find matching record by primary key value

                    RecordUpdate ruCompare = new RecordUpdate();
                    foreach (RecordUpdate ruUpdate in mruRecordsToCompare.ruRecords)
                    {
                        if (ruUpdate.sId == ruBase.sId && ruUpdate.sPrimaryKeyColumn == ruBase.sPrimaryKeyColumn)
                        {
                            //we found the matching record in the multiplerecords udpate  in the mruRecordsToCompare that matches the record in the mruRecordsBase
                            ruCompare = ruUpdate;
                            break;
                        }
                    }


                    Dictionary<string, string> odictChanges = new Dictionary<string, string>();



                    foreach (KeyValuePair<string, string> sBaseItem in ruBase.odictColumnValues)
                    {
                        string sColumnName = sBaseItem.Key;
                        string sBaseValue = sBaseItem.Value;

                        if (!ruCompare.odictColumnValues.ContainsKey(sColumnName))
                        {
                            continue;
                        }


                        string sCompareValue = ruCompare.odictColumnValues[sColumnName];

                        // value changed
                        if (sBaseValue != sCompareValue)
                        {
                            odictChanges.Add(sColumnName, sCompareValue);
                        }
                    }

                    // if only primary key exists, nothing changed
                    if (odictChanges.Count > 0)
                    {
                        //if the only thing that has "changed" is the lastmodifieddate then don't update it we didn't actually change anything
                        if (odictChanges.Count == 1)
                        {
                            //only continue if the column is not lastmodfieddate
                            if (odictChanges.ContainsKey("LastModifiedDate"))
                            {

                            }
                            else
                            {
                                RecordUpdate ruUpdate = new RecordUpdate();
                                ruUpdate.sPrimaryKeyColumn = ruBase.sPrimaryKeyColumn;
                                ruUpdate.sId = ruBase.sId;
                                ruUpdate.odictColumnValues = odictChanges;

                                ruRecordsToUpdate.Add(ruUpdate);
                            }
                        }
                        else
                        {
                            RecordUpdate ruUpdate = new RecordUpdate();
                            ruUpdate.sPrimaryKeyColumn = ruBase.sPrimaryKeyColumn;
                            ruUpdate.sId = ruBase.sId;
                            ruUpdate.odictColumnValues = odictChanges;

                            ruRecordsToUpdate.Add(ruUpdate);
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Error in CompareMultipleRecordUpdates: " + ex.Message,
                    "VisAssist");
            }

            return new MultipleRecordUpdates(ruRecordsToUpdate);
        }

        internal static void SyncDBWithFile(Visio.Document ovDocument, string sVisAssistFolderPath)
        {
            Globals.ThisAddIn.m_SyncedDB = true;
            //make sure the db is pointing towards correct location 
            DatabaseConfig.BindToActiveDocument(sVisAssistFolderPath);

            CheckPageExistence(ovDocument, sVisAssistFolderPath);

            CheckShapeExistence(ovDocument, sVisAssistFolderPath);


            //add a delayd event to turn off m_SyncedDb
            DelayedEvent oDelayedEvent = new DelayedEvent();
            oDelayedEvent.ovDocument = ovDocument;
            oDelayedEvent.sOperationType = "TurnOffSyncDBBool";
            Globals.ThisAddIn.m_delayedEvents.Add(oDelayedEvent);

        }

        internal static void CheckShapeExistence(Visio.Document ovDocument, string sVisAssistFolderPath)
        {
            try
            {


                //first part checks to see if all the shape in the visio file exist in the db and that their information is up to date...
                string sProjectID = ovDocument.DocumentSheet.Cells["User.ProjectID"].get_ResultStr(0);
                //create a new list for each shape that has its own table...
                List<string> lstTerminals = new List<string>();
                List<string> lstWires = new List<string>();
                List<string> lstEndDevices = new List<string>();
                Dictionary<string, Visio.Shape> oDictPrimaryWiresNotInDB = new Dictionary<string, Visio.Shape>();
                Dictionary<string, Visio.Shape> oDictSecondaryWiresNotInDB = new Dictionary<string, Visio.Shape>();
                foreach (Visio.Page ovPage in ovDocument.Pages)
                {
                    foreach (Visio.Shape ovShape in ovPage.Shapes)
                    {
                        string sClass = "";
                        if (ovShape.CellExists["User.Class", 0] == -1)
                        {
                            //this is one of our shapes
                            sClass = ovShape.Cells["User.Class"].get_ResultStr(0);
                            string sShapeID = ovShape.Cells["User.ShapeID"].get_ResultStr(0);

                            if (sShapeID != "")
                            {

                                bool bDoesRecordExist;
                                switch (sClass)
                                {
                                    case "TerminalBlock":
                                        {
                                            //this checks that all the visio termianl blocks have a db record entry and makes sure the information is up to date..
                                            CheckShapeExistenceInDB(DatabaseUtilities.SqlTables.TerminalBlocksTable.sTerminalBlockTable, ovShape, sShapeID, ref lstTerminals, ref oDictPrimaryWiresNotInDB, ref oDictSecondaryWiresNotInDB);



                                            break;
                                        }
                                    case "SmartWire":
                                        {
                                            CheckShapeExistenceInDB(DatabaseUtilities.SqlTables.WireShapesTable.sWireShapeTable, ovShape, sShapeID, ref lstWires, ref oDictPrimaryWiresNotInDB, ref oDictSecondaryWiresNotInDB);




                                            break;
                                        }
                                    case "ADC End Device":
                                        {
                                            CheckShapeExistenceInDB(SqlTables.WiringEndDevice.sWiringEndDeviceTable, ovShape, sShapeID, ref lstEndDevices, ref oDictPrimaryWiresNotInDB, ref oDictSecondaryWiresNotInDB);
                                            break;
                                        }
                                }
                            }


                        }

                    }
                }



                //clean up wires not in db...
                if (oDictPrimaryWiresNotInDB.Count > 0)
                {
                    //now we need to drop secondary wires for each of the primary wires in oDictPrimaryWiresNotInDB and add them to the db...
                    //foreach (Visio.Shape ovPrimaryWire in oDictPrimaryWiresNotInDB.Values)
                    //{
                    //    WireUtilities.AddWire(ovPrimaryWire, ref lstWires, true);
                    //}

                    //we are going to match the primary to its secodnary...
                    foreach (Visio.Shape ovPrimaryWire in oDictPrimaryWiresNotInDB.Values)
                    {
                        string sWirePairID = ovPrimaryWire.Cells["User.WirePairID"].get_ResultStr(0);
                        MultipleRecordUpdates mruPrimaryRecord = WireUtilities.BuildWireShapeInfo(ovPrimaryWire, sWirePairID, false);
                        //get the secondary wire in the oDictSecondaryWires based on its shapeid...
                        string sMateID = "";
                        //loop through the oDictSecondaryWiresNotInDB for the matching wirepairid...
                        foreach (Visio.Shape ovMateShape in oDictSecondaryWiresNotInDB.Values)
                        {
                            if (ovMateShape.Cells["User.WirePairID"].get_ResultStr(0) == sWirePairID)
                            {
                                sMateID = ovMateShape.Cells["User.ShapeID"].get_ResultStr(0);
                            }
                        }
                        Visio.Shape ovSecondaryWire = oDictSecondaryWiresNotInDB[sMateID];
                        MultipleRecordUpdates mruSecondaryRecord = WireUtilities.BuildWireShapeInfo(ovSecondaryWire, sWirePairID, false);

                        WireUtilities.AddWireToDatabase(mruPrimaryRecord, mruSecondaryRecord);

                        //add to lstWires so we don't delete them...
                        lstWires.Add(mruPrimaryRecord.ruRecords[0].sId);
                        lstWires.Add(mruSecondaryRecord.ruRecords[0].sId);
                    }

                }


                //now we need to check to make sure everything in the db exists in visio
                CheckShapeExistenceInVisio(SqlTables.TerminalBlocksTable.sTerminalBlockTable, ovDocument, ref lstTerminals);
                CheckShapeExistenceInVisio(SqlTables.WiringEndDevice.sWiringEndDeviceTable, ovDocument, ref lstEndDevices);
                CheckShapeExistenceInVisio(SqlTables.WireShapesTable.sWireShapeTable, ovDocument, ref lstWires);



                //this last part checks all the shape tables to make sure that there pages actually exist and if they don't delete them..
                CheckShapesPageExistence(SqlTables.TerminalBlocksTable.sTerminalBlockTable, ovDocument);
                CheckShapesPageExistence(SqlTables.WiringEndDevice.sWiringEndDeviceTable, ovDocument);
                CheckShapesPageExistence(SqlTables.WireShapesTable.sWireShapeTable, ovDocument);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in CheckShapeExistence " + ex.Message, "VisAssist");
            }
            finally
            {
                ovDocument.Application.EventsEnabled = -1;
            }
        }

        private static void CheckShapesPageExistence(string sShapeTable, Document ovDocument)
        {
            //this checks to make sure all the shapes pageId's actually exist in the db pages_table
            try
            {
                using (SQLiteConnection sqliteconConnection = new SQLiteConnection(DatabaseConfig.ConnectionString))
                {
                    sqliteconConnection.Open();
                    //enable foreign key enforcemnt for this connection
                    using (SQLiteCommand sqlitcmdPragma = new SQLiteCommand("PRAGMA foreign_keys = ON;", sqliteconConnection))
                    {
                        sqlitcmdPragma.ExecuteNonQuery();
                    }
                    string sSql = @"DELETE FROM " + sShapeTable + " WHERE PageID NOT IN (SELECT PageID FROM pages_table)";

                    using (SQLiteCommand sqlitecmdCommand = new SQLiteCommand(sSql, sqliteconConnection))
                    {
                        sqlitecmdCommand.CommandText = sSql;
                        sqlitecmdCommand.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in CheckShapesPageExistence " + ex.Message, "VisAssist");
            }
        }

        internal static List<string> CheckShapeExistenceInVisio(string sTableName, Visio.Document ovDocument, ref List<string> lstShapes)
        {
            ShapesUtilities.GetShapesInTable(sTableName, ovDocument); //this populates ShapeUtilites.m_mruRecordsBase  --not sure if i should create a new recordbase for each table/shapes (terminal blocks, wires..)

            List<string> lstShapesToRemove = new List<string>();

            string sPK = GetPrimaryKey(sTableName);
            foreach (RecordUpdate ru in ShapesUtilities.m_mruRecordsBase.ruRecords)
            {
                string sShapeID = ru.sId;
                if (lstShapes.Contains(sShapeID))
                {
                    //the shape exists in visio and in the db...
                }
                else
                {
                    //the shape exists in the db and not in visio, we want to delete it from the db
                    lstShapesToRemove.Add(sShapeID);
                    //get the pagename from the pageid...

                }
            }
            if (lstShapesToRemove.Count > 0)
            {
                //we have records in our pages table that don't actually exist-go delete them from db...
                //build a delete sql for each record...
                List<RecordUpdate> ruRecords = new List<RecordUpdate>();
                foreach (string sShapeID in lstShapesToRemove)
                {
                    RecordUpdate ru = new RecordUpdate();
                    ru.sId = sShapeID;
                    ru.sPrimaryKeyColumn = sPK;
                    ru.odictColumnValues = null;

                    ruRecords.Add(ru);
                }

                MultipleRecordUpdates mruRecordsToDelete = new MultipleRecordUpdates(ruRecords);
                BuildDeleteSqlForMultipleRecords(sTableName, mruRecordsToDelete);


                //return the list of shapes removed using the shape name and page name...

            }
            return lstShapesToRemove;
        }

        internal static void CheckShapeExistenceInDB(string sTableName, Visio.Shape ovShape, string sShapeID, ref List<string> lstShapes, ref Dictionary<string, Shape> oDictPrimaryWiresNotInDB, ref Dictionary<string, Visio.Shape> oDictSecondaryWiresNotInDB)
        {
            bool bDoesRecordExist = DoesRecordExist(sTableName, sShapeID);
            string sClass = ovShape.Cells["User.Class"].get_ResultStr(0);
            bool bDontAddTolstShapes = false;

            if (!bDoesRecordExist)
            {
                //the record doesn't exist for this visio shape-freak accident...
                switch (sClass)
                {
                    case "TerminalBlock":
                        {
                            bDontAddTolstShapes = false;
                            TerminalBlockUtilities.AddTerminalBlockToDatabase(ovShape);
                            break;
                        }
                    case "ADC End Device": //this class has changed to End Device...
                        {
                            bDontAddTolstShapes = false;
                            EndDeviceUtilities.AddWiringEndDeviceToDatabase(ovShape);
                            break;
                        }
                    case "SmartWire":
                        {
                            //we have a wire that is not in our database, which means we are missing the primary and secondary wire...
                            //gather a list of all the secondarya and primaries, we will be deleting the secondaries and producing new ones and adding them to the db...
                            string sWireRole = ovShape.Cells["User.WireRole"].get_ResultStr(0);
                            bDontAddTolstShapes = true;
                            switch (sWireRole)
                            {
                                case "P":
                                    {
                                        oDictPrimaryWiresNotInDB.Add(sShapeID, ovShape);
                                        break;
                                    }
                                case "S":
                                    {
                                        oDictSecondaryWiresNotInDB.Add(sShapeID, ovShape);
                                        ////delete the secondary wire..
                                        //ovShape.Application.EventsEnabled = 0;
                                        //ovShape.Delete();
                                        //ovShape.Application.EventsEnabled = -1;

                                        break;
                                    }
                            }
                            break;
                        }
                }

            }
            else
            {

                //the record exists, but we want to make sure that the information in the visio shape matches the db...
                MultipleRecordUpdates mruBaseFileRecord = new MultipleRecordUpdates();
                switch (sClass)
                {
                    case "TerminalBlock":
                        {
                            mruBaseFileRecord = TerminalBlockUtilities.BuildTerminalBlockInfo(ovShape);
                            break;
                        }
                    case "ADC End Device":
                        {
                            mruBaseFileRecord = EndDeviceUtilities.BuildWiringEndDeviceInfo(ovShape);
                            break;
                        }
                    case "SmartWire":
                        {
                            mruBaseFileRecord = WireUtilities.BuildWireShapeInfo(ovShape, "", false);

                            break;
                        }
                }


                //get the records information in a multioplerecordupdate and then compare that to mruBaseFile...
                MultipleRecordUpdates mruDBRecord = GetRecordInformation(sTableName, sShapeID);
                if (mruDBRecord.ruRecords != null)
                {
                    MultipleRecordUpdates mruRecordToUpdate = CompareDataForMultipleRecords(mruBaseFileRecord, mruDBRecord);
                    if (mruRecordToUpdate.ruRecords.Count > 0)
                    {
                        BuildUpdateSqlForMultipleRecords(sTableName, mruBaseFileRecord);
                    }
                }
            }

            if (!bDontAddTolstShapes)
            {
                lstShapes.Add(sShapeID);
            }

        }

        internal static void CheckPageExistence(Visio.Document ovDocument, string sVisAssistFolderPath)
        {
            try
            {
                //this first part checks to see if the all the pages in the visio file exist in the db and if their information is correct...

                string sProjectID = ovDocument.DocumentSheet.Cells["User.ProjectID"].get_ResultStr(0);
                List<string> lstPages = new List<string>();
                //we are given the root project folderpath sVisAssistFolderPath
                //and the document ovDocument
                //create a collection of the pages and the shapes to confirm they exist in the db...

                foreach (Visio.Page ovPage in ovDocument.Pages)
                {
                    if (ovPage.PageSheet.CellExists["User.PageID", 0] == -1)
                    {

                        string sPageID = ovPage.PageSheet.Cells["User.PageID"].get_ResultStr(0);
                        CheckPageExistenceInDB(ovPage, sProjectID, sPageID);
                        //add it to a collection of pages that should be in db to compare later to what is in db and clear and records that don't exist in the collection
                        lstPages.Add(sPageID);
                    }

                }




                CheckPageExistenceInVisio(ovDocument, ref lstPages);

            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in CheckPageExistence " + ex.Message, "VisAssist");
            }
        }

        internal static void CheckPageExistenceInVisio(Document ovDocument, ref List<string> lstPages)
        {
            //This checks to see if we need to delete any records from the DB
            //now check if any records exist in db that don't in lstPages then delete them from the db...
            PageUtilities.GetPagesForCurrentFile(ovDocument); //populate PageUtilities.m_mruRecordsBase
            List<string> lstPagesToRemove = new List<string>();
            foreach (RecordUpdate ru in PageUtilities.m_mruRecordsBase.ruRecords)
            {
                string sPageID = ru.sId;
                if (lstPages.Contains(sPageID))
                {
                    //the page exists in visio and in the db...
                }
                else
                {
                    //the page exists in the db and not in visio, we want to delete it from the db
                    lstPagesToRemove.Add(sPageID);
                }
            }

            if (lstPagesToRemove.Count > 0)
            {
                //we have records in our pages table that don't actually exist-go delete them from db...
                //build a delete sql for each record...
                List<RecordUpdate> ruRecords = new List<RecordUpdate>();
                foreach (string sPageID in lstPagesToRemove)
                {
                    RecordUpdate ru = new RecordUpdate();
                    ru.sId = sPageID;
                    ru.sPrimaryKeyColumn = SqlTables.PagesTable.sPagesTablePK;
                    ru.odictColumnValues = null;

                    ruRecords.Add(ru);
                }

                MultipleRecordUpdates mruRecordsToDelete = new MultipleRecordUpdates(ruRecords);
                BuildDeleteSqlForMultipleRecords(SqlTables.PagesTable.sPagesTable, mruRecordsToDelete);
            }
        }

        internal static void CheckPageExistenceInDB(Visio.Page ovPage, string sProjectID, string sPageID)
        {
            //check if that record exists in the db
            bool bDoesRecordExist = DoesRecordExist(DatabaseUtilities.SqlTables.PagesTable.sPagesTable, sPageID);
            if (!bDoesRecordExist)
            {
                //this is a freak accident add the record to the db...
                PageUtilities.AddPageToDatabase(ovPage, sProjectID, "Visio");

            }
            else
            {
                //the record exists, we want to make sure that the information in the db matches the information in the visio file
                MultipleRecordUpdates mruBaseFileRecord = PageUtilities.BuildPageInfoBasedOnVisioPage(ovPage, sProjectID);
                //get the records information in a multioplerecordupdate and then compare that to mruBaseFile...
                MultipleRecordUpdates mruDBRecord = GetRecordInformation(SqlTables.PagesTable.sPagesTable, sPageID);
                if (mruDBRecord.ruRecords != null)
                {
                    MultipleRecordUpdates mruRecordToUpdate = CompareDataForMultipleRecords(mruBaseFileRecord, mruDBRecord);
                    if (mruRecordToUpdate.ruRecords.Count > 0)
                    {
                        BuildUpdateSqlForMultipleRecords(SqlTables.PagesTable.sPagesTable, mruBaseFileRecord);
                    }
                }

            }

        }

        internal static void CreateGridString(Visio.Page ovPage, out double dPageWidth, out double dPageHeight, out string sPageGrid)
        {

            //Re-Caculate the values for the different page sizes
            //The new page will be the same size as the page that the user was on
            dPageWidth = ovPage.PageSheet.get_Cells("PageWidth").get_Result(0);
            dPageHeight = ovPage.PageSheet.get_Cells("PageHeight").get_Result(0);
            double dStartingX = .3833;
            double dStartingY = .3833;
            double dUsableWidth = dPageWidth - 2 * dStartingX;
            double dGridValue1 = dUsableWidth + dStartingX;
            sPageGrid = "";
            double dUsableHeight;


            StringBuilder sb = new StringBuilder();


            sb.Append(dUsableWidth.ToString());
            sb.Append(";");
            dUsableHeight = dPageHeight - 2 * dStartingY;
            sb.Append(dUsableHeight.ToString());
            sb.Append(";");
            sb.Append(dStartingX.ToString());
            sb.Append(";");
            sb.Append(dStartingY.ToString());

            sPageGrid = sb.ToString();

        }

        internal static List<string> CheckShapeExistenceInVisioOnDeletion(string sTableName, Document ovDocument, ref List<string> lstShapes)
        {
            ShapesUtilities.GetShapesInTable(sTableName, ovDocument); //this populates ShapeUtilites.m_mruRecordsBase  --not sure if i should create a new recordbase for each table/shapes (terminal blocks, wires..)

            List<string> lstShapesToRemove = new List<string>();

            string sPK = GetPrimaryKey(sTableName);
            foreach (RecordUpdate ru in ShapesUtilities.m_mruRecordsBase.ruRecords)
            {
                string sShapeID = ru.sId;
                if (lstShapes.Contains(sShapeID))
                {
                    //the shape exists in visio and in the db...
                }
                else
                {
                    //the shape exists in the db and not in visio, we want to delete it from the db
                    lstShapesToRemove.Add(sShapeID);
                    //get the pagename from the pageid...
                    string sPageID = ru.odictColumnValues["PageID"];
                    string sPageName = PageUtilities.GetColumnInfoInPagesTableFromDatabase("PageName", sPageID);
                    string sKey = ru.odictColumnValues["WireName"] + "|" + sPageName;
                    Globals.ThisAddIn.m_MatesDeleted.Add(sKey);
                }
            }
            if (lstShapesToRemove.Count > 0)
            {
                //we have records in our pages table that don't actually exist-go delete them from db...
                //build a delete sql for each record...
                List<RecordUpdate> ruRecords = new List<RecordUpdate>();
                foreach (string sShapeID in lstShapesToRemove)
                {
                    RecordUpdate ru = new RecordUpdate();
                    ru.sId = sShapeID;
                    ru.sPrimaryKeyColumn = sPK;
                    ru.odictColumnValues = null;

                    ruRecords.Add(ru);
                }

                MultipleRecordUpdates mruRecordsToDelete = new MultipleRecordUpdates(ruRecords);
                BuildDeleteSqlForMultipleRecords(sTableName, mruRecordsToDelete);


                //return the list of shapes removed using the shape name and page name...

            }
            return lstShapesToRemove;

        }

        internal static bool CheckShapeExistenceInVisioForRedoing(string sTableName, Visio.Document ovDocument)
        {
            ShapesUtilities.GetShapesInTable(sTableName, ovDocument); //this populates ShapeUtilites.m_mruRecordsBase  --not sure if i should create a new recordbase for each table/shapes (terminal blocks, wires..)

            bool bRedoTwice = false;
            List<string> lstWires = new List<string>();
            string sPK = GetPrimaryKey(sTableName);

            //populate all the wirs that exist in the database 
            foreach (Visio.Page ovPage in ovDocument.Pages)
            {
                if (ovPage.PageSheet.CellExists["User.PageID", 0] == -1)
                {
                    foreach (Visio.Shape ovShape in ovPage.Shapes)
                    {
                        if (ovShape.CellExists["User.ShapeID", 0] == -1)
                        {
                            if (ovShape.Cells["User.Class"].get_ResultStr(0) == "SmartWire")
                            {
                                lstWires.Add(ovShape.Cells["User.ShapeID"].get_ResultStr(0));
                            }
                        }
                    }
                }
            }
            foreach (RecordUpdate ru in ShapesUtilities.m_mruRecordsBase.ruRecords)
            {
                string sShapeID = ru.sId;
                if (lstWires.Contains(sShapeID))
                {
                    //the shape exists in visio and in the db...
                }
                else
                {
                    //the shape exists in the db and not in visio, we want to delete it from the db
                    bRedoTwice = true;
                    break;
                    

                }
            }

            return bRedoTwice;


        }
    }





}


public struct RecordUpdate
{
    public string sPrimaryKeyColumn;
    public string sId; // Primary key value
    public Dictionary<string, string> odictColumnValues; // Columns to update

    public RecordUpdate(string sPrimaryKeyColumn, string sId, Dictionary<string, string> odictColumnValues)
    {
        this.sPrimaryKeyColumn = sPrimaryKeyColumn;
        this.sId = sId;
        this.odictColumnValues = odictColumnValues;
    }
}

public struct MultipleRecordUpdates
{
    public List<RecordUpdate> ruRecords;

    public MultipleRecordUpdates(List<RecordUpdate> ruRecords)
    {
        this.ruRecords = ruRecords;
    }
}


//just to get where to save the database


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
    public static bool BindToActiveDocument(string sFolderPath)
    {
        //instead of binding the document to the active document let's give this method the document path that we want to bind it to 

        //Visio.Application app = Globals.ThisAddIn.Application;

        //if (app.Documents.Count == 0 || app.ActiveDocument == null)
        //    return false;

        //Visio.Document doc = app.ActiveDocument;

        //// Unsaved document → no filesystem location yet
        //if (string.IsNullOrEmpty(doc.FullName))
        //    return false;

        //string sFolderPath = FileUtilities.ReturnFileStructurePath(doc.Path);

        //sFolderPath = Path.GetDirectoryName(sFolderPath);

        DatabasePath = System.IO.Path.Combine(sFolderPath, "DB", "VisAssistBackEnd.db");
        return true;
    }
}





