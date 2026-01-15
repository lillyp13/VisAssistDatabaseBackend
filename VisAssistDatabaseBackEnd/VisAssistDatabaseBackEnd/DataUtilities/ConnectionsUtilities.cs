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
                );
                ";
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
                );
                ";

                        using (SQLiteCommand cmd = new SQLiteCommand(sPageTableCommand, connection))
                        {
                            cmd.ExecuteNonQuery();

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
                    MessageBox.Show("Database file already exists.");
                }
            }
            else
            {
                MessageBox.Show("Database directory and database exist");
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


        //given two identically built dictionaries (meaning they have the same number of records and the same keys) see which values need to be updated...
        public static Dictionary<string,string> CompareData(Dictionary<string, string> oDictBase, Dictionary<string,string> oDictCompare)
        {
            
            Dictionary<string,string> oDictInfoToUpdate = new Dictionary<string,string>();

            int ithItem = 0; 
            foreach (KeyValuePair<string, string> sBaseItem in oDictBase)
            {
                ithItem++;

                string sKey = sBaseItem.Key; //get the primary key
                string sBaseValue = sBaseItem.Value; // get the original data from the db


                string sNewValue = oDictCompare[sKey]; //get the "new" value from the form

                //check if these values differ
                if (sBaseValue != sNewValue)
                {
                    //the values are different so we are going to update this value in the db
                    oDictInfoToUpdate.Add(sKey, sNewValue);
                }
                else
                {
                    //if this is the first value these should never be different because it is the primary key
                    if(ithItem == 1)
                    {
                        //we want to add the primary key so we know where we will be updating
                        oDictInfoToUpdate.Add(sKey, sBaseValue);
                    }
                }

            }

            if(oDictInfoToUpdate.Count == 1)
            {
                oDictInfoToUpdate.Clear(); //there were no changes and this record was only the primary key...
            }

            return oDictInfoToUpdate;


        }


        /// <summary>
        ///  given the table name and a dictionary of what to update (this includes all the primary keys and the key values) build an update sql statement 
        ///  this assumes that in the dictionary there is only one ID (for the where clause) (this function does not update multiple records 
        ///  
        /// Updates ONE record 
        /// </summary>
        /// <param name="sTableName"></param>
        /// <param name="oDictInfoToUpdate"></param>

        internal static void BuildUpdateSqlForOneRecord(string sTableName, Dictionary<string, string> oDictInfoToUpdate)
        {

            string sID = ""; // primary key column
            int iID = 0;     // primary key value

            string sSqlUpdate = "UPDATE " + sTableName + " SET ";
            int index = 0;

            // open connection first so we can add parameters as we go
            using (SQLiteConnection connection = new SQLiteConnection(DatabaseConfig.ConnectionString))
            {
                connection.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(connection))
                {
                    bool bSkipFirst = false;

                    //loop through the dictionary of items to update and build up the sql based on the key (colum name)
                    foreach (KeyValuePair<string, string> sItem in oDictInfoToUpdate)
                    {
                        if (!bSkipFirst)
                        {
                            //this is the first entry which means it is the primary key so get that column name and value to determine which entry in the table to update...
                            bSkipFirst = true;

                            // first entry is primary key
                            sID = sItem.Key; //this is our primary key
                            iID = int.Parse(sItem.Value); //this is our key value
                            continue;
                        }

                        // build the SQL
                        sSqlUpdate += sItem.Key + " = @" + sItem.Key;

                        if (index < oDictInfoToUpdate.Count - 2) // minus 2 because first is skipped then add a comma so we can add another change.. otherwise this was the last update so we don't need a comma at the end of it
                        {
                            sSqlUpdate += ", ";
                        }


                        index++;

                        // add parameter immediately
                        cmd.Parameters.AddWithValue("@" + sItem.Key, sItem.Value); //automatically match sql placeholder with the dictionary value 
                    }

                    // add WHERE clause and its parameter
                    sSqlUpdate += " WHERE " + sID + " = @" + sID;
                    cmd.Parameters.AddWithValue("@" + sID, iID);

                    // assign SQL to the command
                    cmd.CommandText = sSqlUpdate;

                    // execute the update
                    cmd.ExecuteNonQuery();
                }
            }
        }




        //// Create updates for multiple records--you'd have to build this up based on what is changing....
        //var record1 = new RecordUpdate(1, new Dictionary<string, string>{{ "JobCity", "New York" },{ "ReviewedBy", "Alice" }});

        //var record2 = new RecordUpdate(2, new Dictionary<string, string>{{ "JobCity", "Los Angeles" },{ "ReviewedBy", "Bob" }});

        //var updates = new MultipleRecordUpdates(new List<RecordUpdate> { record1, record2 });

        //// Execute update
        //BuildUpdateSqlForMultipleRecords("project_table", updates);

        /// <summary>
        /// given a table name and a struct of updates build up an update sql statement based on multiple changes to multiple records...
        /// this assumes that the primary key is the same for all the upates...maybe thinnk about grouping the mruRecords based on their id and then running BuildUPdateSqlForMultipleRecords for each group that we have...
        /// </summary>
        /// <param name="sTableName"></param>
        /// <param name="updates"></param>
        internal static void BuildUpdateSqlForMultipleRecords(string sTableName, MultipleRecordUpdates mruRecords)
        {
            //collect all the unique column names that need to be updated across all the records
            HashSet<string> hsAllColumns = new HashSet<string>();
            foreach (RecordUpdate rRecord in mruRecords.Records)
            {
                foreach (string scol in rRecord.ColumnValues.Keys)
                {
                    hsAllColumns.Add(scol);
                }
            }
            string sSqlUpdate = $"UPDATE {sTableName} SET ";

            using (SQLiteConnection sqlConnection = new SQLiteConnection(DatabaseConfig.ConnectionString))
            {
                sqlConnection.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(sqlConnection))
                {
                    int colIndex = 0;
                    //loop thorugh each column that needs to be updated
                    foreach (string sCol in hsAllColumns)
                    {
                        // Build CASE expression for each column (different values for different records in a single UPDATE)
                        sSqlUpdate += $"{sCol} = CASE Id ";
                        //now loop through all the records to set the value for this column
                        foreach (RecordUpdate rRecord in mruRecords.Records)
                        {
                            if (rRecord.ColumnValues.ContainsKey(sCol))
                            {
                                //add the when and parameters based on the values in the record
                                string sParanName = $"@{sCol}_{rRecord.iId}";
                                sSqlUpdate += $"WHEN {rRecord.iId} THEN {sParanName} ";
                                cmd.Parameters.AddWithValue(sParanName, rRecord.ColumnValues[sCol]);
                            }
                        }

                        //close the case statement for this specific column
                        sSqlUpdate += "END";

                        //add a comma if this is not the last column
                        if (colIndex < hsAllColumns.Count - 1)
                        {
                            sSqlUpdate += ", ";
                        }
                            

                        colIndex++;
                    }

                    string sWhereColumn = mruRecords.Records[0].sPrimaryKeyColumn; // assumes all records use the same primary key column 

                    // Add WHERE clause to only update relevant Ids
                    sSqlUpdate += " WHERE " + sWhereColumn + " IN (" + string.Join(",", mruRecords.Records.Select(r => r.iId)) + ")";

                    cmd.CommandText = sSqlUpdate;
                    cmd.ExecuteNonQuery();
                }
            }



        }
    }

    public struct RecordUpdate
    {
        public string sPrimaryKeyColumn;
        public int iId; // Primary key value
        public Dictionary<string, string> ColumnValues; // Columns to update

        public RecordUpdate(string sPrimaryKeyColumn, int iId, Dictionary<string, string> columnValues)
        {
            this.sPrimaryKeyColumn = sPrimaryKeyColumn;
            this.iId = iId;
            this.ColumnValues = columnValues;
        }
    }

    public struct MultipleRecordUpdates
    {
        public List<RecordUpdate> Records;

        public MultipleRecordUpdates(List<RecordUpdate> records)
        {
            Records = records;
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
