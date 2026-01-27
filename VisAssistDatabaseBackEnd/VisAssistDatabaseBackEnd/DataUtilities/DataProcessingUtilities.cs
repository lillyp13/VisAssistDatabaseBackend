using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Visio = Microsoft.Office.Interop.Visio;

namespace VisAssistDatabaseBackEnd.DataUtilities
{
    internal class DataProcessingUtilities
    {
        //create a struct that will contain the names of the sql tables 
        public struct SqlTables
        {
            public struct ProjectTable
            {
                public const string sProjectTable = "project_table";
                public const string sProjectTablePK = "Id";
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


            public static readonly Dictionary<string, (string PrimaryKey, string[] Columns)> odictTableInfo = new Dictionary<string, (string PrimaryKey, string[] Columns)>()
            {
                { ProjectTable.sProjectTable, (ProjectTable.sProjectTablePK, ProjectTable.saProjectColumns) },
                { FilesTable.sFilesTable, (FilesTable.sFilesTablePK, FilesTable.saFileColumns) },
                { PagesTable.sPagesTable, (PagesTable.sPagesTablePK, PagesTable.saPagesColumns) }
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

        internal static string GetPrimaryKey(string tableName)
        {
            switch (tableName)
            {
                case "files_table": return DataProcessingUtilities.SqlTables.FilesTable.sFilesTablePK;
                case "pages_table": return DataProcessingUtilities.SqlTables.PagesTable.sPagesTablePK;

                default: return "Id"; // fallback
            }
        }


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

                        RecordUpdate ruUpdate = new RecordUpdate();
                        ruUpdate.sPrimaryKeyColumn = ruBase.sPrimaryKeyColumn;
                        ruUpdate.sId = ruBase.sId;
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
            try
            {
                using (SQLiteConnection sqliteconConnection = new SQLiteConnection(DatabaseConfig.ConnectionString))
                {
                    sqliteconConnection.Open();

                    using (SQLiteCommand sqlitecmdCommand = new SQLiteCommand(sqliteconConnection))
                    {

                        //if this is the files_table i need to add the pk value
                        //if (sTableName == DataProcessingUtilities.SqlTables.FilesTable.sFilesTable)
                        //{
                        //    for (int i = 0; i < mruRecords.ruRecords.Count; i++)
                        //    {
                        //        RecordUpdate ruRecord = mruRecords.ruRecords[i]; 

                        //        if(ruRecord.odictColumnValues == null)
                        //        {
                        //            ruRecord.odictColumnValues = new Dictionary<string, string>();
                        //        }

                        //        if(!ruRecord.odictColumnValues.ContainsKey(DataProcessingUtilities.SqlTables.FilesTable.sFilesTablePK))
                        //        {
                        //            ruRecord.odictColumnValues.Add(SqlTables.FilesTable.sFilesTablePK, ruRecord.sId);
                        //        }
                        //    }
                        //}
                        DataProcessingUtilities.SqlTables.TryGetPrimaryKey(sTableName, out string sPK);
                        
                        foreach(RecordUpdate ruRecord in mruRecords.ruRecords)
                        {
                            if(!ruRecord.odictColumnValues.ContainsKey(sPK))
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




        //Helper Functions
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


        //we want to know if there is at least one record given a table..
        internal static bool DoesRecordExist(string sTableName, string sID)
        {
            try
            {
                string sPk = GetPrimaryKey(sTableName);

                using (SQLiteConnection sqliteconConnection =new SQLiteConnection(DatabaseConfig.ConnectionString))
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




        /// <summary>
        /// given a table we want to know if the parent table has at least one record...
        /// Our array goes project_table, files_table, pages_table, wire_shapes_table 
        /// </summary>
        /// <param name="sTableName"></param>
        internal static bool DoesParentTableHaveRecord(string sTableName)
        {
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

        internal static bool DoesTableHaveAnyRecords(string sTableName)
        {
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


        internal static int GetTableRecordCount(string sTableName)
        {
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






}
