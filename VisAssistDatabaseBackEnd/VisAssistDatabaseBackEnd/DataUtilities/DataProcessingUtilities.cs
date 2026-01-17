using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SQLite;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VisAssistDatabaseBackEnd.DataUtilities
{
    internal class DataProcessingUtilities
    {

        //given two identically built dictionaries (meaning they have the same number of records and the same keys) see which values need to be updated...
        public static Dictionary<string, string> CompareDataDictionaries(Dictionary<string, string> oDictBase, Dictionary<string, string> oDictCompare)
        {
            Dictionary<string, string> oDictInfoToUpdate = new Dictionary<string, string>();
            try
            {
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
                        if (ithItem == 1)
                        {
                            //we want to add the primary key so we know where we will be updating
                            oDictInfoToUpdate.Add(sKey, sBaseValue);
                        }
                    }

                }

                if (oDictInfoToUpdate.Count == 1)
                {
                    oDictInfoToUpdate.Clear(); //there were no changes and this record was only the primary key...
                }

                return oDictInfoToUpdate;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in CompareData " + ex.Message, "VisAssist");
            }
            return oDictInfoToUpdate;

        }



        /// <summary>
        /// given two identically built MutlipleRecordUpdates see which values have changed for each record
        /// </summary>
        /// <param name="ruBaseRecords"></param>
        /// <param name="ruCompareRecords"></param>
        /// <returns></returns>
        internal static MultipleRecordUpdates ComapreDataForMultipleRecords(MultipleRecordUpdates mruRecordsBase, MultipleRecordUpdates mruRecordsToCompare)
        {
            List<RecordUpdate> ruRecordsToUpdate = new List<RecordUpdate>();

            try
            {
                foreach (RecordUpdate ruBase in mruRecordsBase.ruRecords)
                {
                    // find matching record by primary key value

                    RecordUpdate ruCompare = new RecordUpdate();
                    foreach(RecordUpdate ruUpdate in mruRecordsToCompare.ruRecords)
                    {
                        if(ruUpdate.iId == ruBase.iId && ruUpdate.sPrimaryKeyColumn == ruBase.sPrimaryKeyColumn)
                        {
                            //we found the matching record in the multiplerecords udpate  in the mruRecordsToCompare that matches the record in the mruRecordsBase
                            ruCompare = ruUpdate;
                            break;
                        }
                    }
                    

                    Dictionary<string, string> odictChanges = new Dictionary<string, string>();

                    // always include primary key first
                    odictChanges.Add(ruBase.sPrimaryKeyColumn,ruBase.iId.ToString());

                    foreach (KeyValuePair<string, string> sBaseItem in ruBase.odictColumnValues)
                    {
                        string sColumnName = sBaseItem.Key;
                        string sBaseValue = sBaseItem.Value;

                        if (!ruCompare.odictColumnValues.ContainsKey(sColumnName))
                            continue;

                        string sCompareValue = ruCompare.odictColumnValues[sColumnName];

                        // value changed
                        if (sBaseValue != sCompareValue)
                        {
                            odictChanges.Add(sColumnName, sCompareValue);
                        }
                    }

                    // if only primary key exists, nothing changed
                    if (odictChanges.Count > 1)
                    {
                        RecordUpdate ruUpdate = new RecordUpdate();
                        ruUpdate.sPrimaryKeyColumn = ruBase.sPrimaryKeyColumn;
                        ruUpdate.iId = ruBase.iId;
                        ruUpdate.odictColumnValues = odictChanges;

                        ruRecordsToUpdate.Add(ruUpdate);
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








        /// <summary>
        ///  given the table name and a dictionary of what to update (this includes all the primary keys and the key values) build an update sql statement 
        ///  this assumes that in the dictionary there is only one ID (for the where clause) (this function does not update multiple records 
        ///  
        /// 
        /// i want to build a buildsqlforonerecord that actually takes one record, right now this is not using any records....
        /// Updates ONE record 
        /// </summary>
        /// <param name="sTableName"></param>
        /// <param name="oDictInfoToUpdate"></param>

        internal static void BuildUpdateSqlForRecordDictionary(string sTableName, Dictionary<string, string> oDictInfoToUpdate, string sAction)
        {
            try
            {
                string sID = ""; // primary key column
                int iID = 0;     // primary key value

                //string sSqlUpdate = sAction + " " + sTableName + " SET ";
                int iIndex = 0;
                string sSql = "";

                //logging here
                // open connection first so we can add parameters as we go
                using (SQLiteConnection sqliteconConnection = new SQLiteConnection(DatabaseConfig.ConnectionString))
                {
                    //logging here
                    sqliteconConnection.Open();

                    using (SQLiteCommand sqlitecmdCommand = new SQLiteCommand(sqliteconConnection))
                    {
                        //logging here
                        bool bSkipFirst = false;

                        switch (sAction)
                        {
                            case "UPDATE":
                                {

                                    sSql = "UPDATE " + sTableName + " SET ";
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
                                        sSql += sItem.Key + " = @" + sItem.Key;

                                        if (iIndex < oDictInfoToUpdate.Count - 2) // minus 2 because first is skipped then add a comma so we can add another change.. otherwise this was the last update so we don't need a comma at the end of it
                                        {
                                            sSql += ", ";
                                        }


                                        iIndex++;

                                        // add parameter immediately
                                        sqlitecmdCommand.Parameters.AddWithValue("@" + sItem.Key, sItem.Value); //automatically match sql placeholder with the dictionary value 
                                    }

                                    // add WHERE clause and its parameter
                                    sSql += " WHERE " + sID + " = @" + sID;
                                    sqlitecmdCommand.Parameters.AddWithValue("@" + sID, iID);

                                    // assign SQL to the command
                                    sqlitecmdCommand.CommandText = sSql;
                                    break;
                                }

                            case "INSERT":
                                {
                                    break;
                                }
                            case "DELETE":
                                {
                                    // Start the DELETE statement
                                    sSql = "DELETE FROM " + sTableName;

                                    // Only the first item in the dictionary (assumed primary key) is needed for WHERE

                                    foreach (KeyValuePair<string, string> sItem in oDictInfoToUpdate)
                                    {
                                        if (!bSkipFirst)
                                        {
                                            bSkipFirst = true;

                                            // first entry is primary key
                                            sID = sItem.Key;              // column name of primary key
                                            iID = int.Parse(sItem.Value); // value of primary key

                                            // add WHERE clause for primary key
                                            sSql += " WHERE " + sID + " = @" + sID;

                                            // add parameter
                                            sqlitecmdCommand.Parameters.AddWithValue("@" + sID, iID);

                                            break; // no need to process other columns
                                        }
                                    }

                                    // assign SQL to the command
                                    sqlitecmdCommand.CommandText = sSql;
                                    break;
                                }

                        }





                        // execute the update
                        sqlitecmdCommand.ExecuteNonQuery();
                        //logging here
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in BuildUpdateSqlForOneRecord " + ex.Message, "VisAssist");
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
        /// 
        /// </summary>
        /// <param name="sTableName"></param>
        /// <param name="updates"></param>
        internal static void BuildUpdateSqlForMultipleRecords(string sTableName, MultipleRecordUpdates mruRecords)
        {
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
                                    string sParameterName = $"@{sCol}_{rRecord.iId}";
                                    sSqlUpdate += $"WHEN {rRecord.iId} THEN {sParameterName} ";
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
                        sSqlUpdate += $" WHERE {sWhereColumn} IN ({string.Join(",", mruRecords.ruRecords.Select(r => r.iId))})";

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


        //checks to make sure that the given table exists in the database (not sure if this is even needed but it is a safeguard...)
        internal static bool DoesTableExist(string sTableName)
        {
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


    }


    public struct RecordUpdate
    {
        public string sPrimaryKeyColumn;
        public int iId; // Primary key value
        public Dictionary<string, string> odictColumnValues; // Columns to update

        public RecordUpdate(string sPrimaryKeyColumn, int iId, Dictionary<string, string> odictColumnValues)
        {
            this.sPrimaryKeyColumn = sPrimaryKeyColumn;
            this.iId = iId;
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

}
