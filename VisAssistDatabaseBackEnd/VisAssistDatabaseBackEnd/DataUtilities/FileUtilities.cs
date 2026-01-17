using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using VisAssistDatabaseBackEnd.Forms;



namespace VisAssistDatabaseBackEnd.DataUtilities
{
    internal class FileUtilities
    {
        int iFileID;
        int iProjectID;
        int iRevisionID;
        string sFileName;
        string sFilePath;
        DateTime dtCreatedDate;
        DateTime dtLastModifiedDate;
        string sVersion;
        string sClass;
        string sDrawingType;
        string sWirePrefix;
        bool bIgnoreWireColor;
        bool bAllowDuplicateTags;
        bool bShowPointData;
        static SQLiteConnection Connection = ConnectionsUtilities.Connection;



        string sFileNumber; //for pageformat and fileformat...





        public static Dictionary<string, string> m_dictFileDataInfoBase = new Dictionary<string, string>();  //key is the column name
        public static Dictionary<string, string> m_dictFileDataInfoToCompare = new Dictionary<string, string>();
        public static Dictionary<string, string> m_dictFileDataInfoToUpdate = new Dictionary<string, string>();
        public static MultipleRecordUpdates m_mruRecordsBase = new MultipleRecordUpdates();
        public static MultipleRecordUpdates m_mruRecordsToCompare = new MultipleRecordUpdates();
        public static MultipleRecordUpdates m_mruRecordsToUpdate = new MultipleRecordUpdates();

        //File Actions
        internal static void AddFirstFile()
        {
            DatabaseSeeding.SeedFiles();
        }
        internal static void DeleteAllFiles()
        {
            //delete all the records in the files_table
            using (SQLiteConnection connection = new SQLiteConnection(Connection))
            {
                connection.Open();
                string sDelete = "DELETE FROM files_table;";

                new SQLiteCommand(sDelete, connection).ExecuteNonQuery();

                //reset the auto-increment counter
                string sReset = "DELETE FROM sqlite_sequence WHERE name = 'files_table';";
                new SQLiteCommand(sReset, connection).ExecuteNonQuery();
            }
        }

        internal static void OpenFileForm()
        {
            FilePropertiesForm oNewForm = new FilePropertiesForm();
            oNewForm.Display();
            oNewForm.ShowDialog();
        }

        internal static void GetFileDataFromDatabase(FilePropertiesForm filePropertiesForm)
        {
            try
            {
                //logging statement placeholder
                //m_dictFileDataInfoBase.Clear(); 
                if(m_mruRecordsBase.ruRecords != null)
                {
                    m_mruRecordsBase.ruRecords.Clear();
                }
                

                //select all the files from the files_table
                string sSQl = @"SELECT * FROM files_table";
                List<RecordUpdate> lstRecords = new List<RecordUpdate>();
                string sPrimaryKeyColumn = "FileID";
                //logging statement placeholder
                using (SQLiteConnection sqliteconConnection = new SQLiteConnection(Connection))
                {
                    //logging statement placeholder
                    sqliteconConnection.Open();
                    using (SQLiteCommand sqlitecmdCommand = new SQLiteCommand(sSQl, sqliteconConnection))
                    {
                        //logging here
                        //execute the query and read the result
                        using (SQLiteDataReader sqlitereadReader = sqlitecmdCommand.ExecuteReader())
                        {
                            while (sqlitereadReader.Read())
                            {
                                Dictionary<string, string> odictColumnValues = new Dictionary<string, string>();

                                int iID = 0;
                                for(int i = 0; i < sqlitereadReader.FieldCount; i++)
                                {
                                    string sColumnName = sqlitereadReader.GetName(i);
                                    string sValue = sqlitereadReader.IsDBNull(i) ? string.Empty : sqlitereadReader.GetValue(i).ToString();
                                    odictColumnValues.Add(sColumnName, sValue);

                                    if(sColumnName == sPrimaryKeyColumn)
                                    {
                                        iID = Convert.ToInt32(sqlitereadReader.GetValue(i));
                                    }
                                   


                                    //string sRowData = "";
                                    //string sColumnName = "";
                                    //for (int i = 0; i < sqlitereadReader.FieldCount; i++)
                                    //{
                                    //    sColumnName = sqlitereadReader.GetName(i).ToString(); // column name
                                    //    sRowData = sqlitereadReader.GetValue(i).ToString(); //actual value we care about

                                    //    m_dictFileDataInfoBase.Add(sColumnName, sRowData); //build up the dictionary so the column is the key and the value is the value in the cell...
                                    //                                                       //logging statement placeholder
                                    //}
                                }
                                //create a recordupdate for this specfic record (row)
                                RecordUpdate ruRecordUpdate = new RecordUpdate();
                                ruRecordUpdate.sPrimaryKeyColumn = sPrimaryKeyColumn;
                                ruRecordUpdate.iId = iID;
                                ruRecordUpdate.odictColumnValues = odictColumnValues;

                                lstRecords.Add(ruRecordUpdate);
                                

                            }


                        } 
                    }
                }

                //warp everything in a multiple record updates struct
                m_mruRecordsBase = new MultipleRecordUpdates(lstRecords);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in GetProjectInfoFromDatabase " + ex.Message, "ViAssist");
            }
        }

        internal static void PopulateFilePropertiesForm(FilePropertiesForm filePropertiesForm)
        {

            ////GIVEN ONE RECORD
            ////given the m_dictFileDataInfoBase populate the dgvFileData first item in the dictionary should line up with the first column....
            ////add an empty row 
            //DataGridViewRow dgvFirstRow = new DataGridViewRow();
            //dgvFirstRow.CreateCells(filePropertiesForm.dgvFileData); //clears existing cells and sets template accoridng to fileproperitesForm.dgvFileData
            //int iColIndex = 0;
            //foreach (KeyValuePair<string, string> sBaseItem in m_dictFileDataInfoBase)
            //{

            //    dgvFirstRow.Cells[iColIndex].Value = sBaseItem.Value;
            //    iColIndex++;


            //}

            ////add the row to the datagridview 
            //filePropertiesForm.dgvFileData.Rows.Add(dgvFirstRow);
            //END OF ONE RECORD


            // Clear existing rows first
            filePropertiesForm.dgvFileData.Rows.Clear();

            // Loop through each record (each DB row)
            foreach (RecordUpdate ruRecord in m_mruRecordsBase.ruRecords)
            {
                // Create a new row based on the DataGridView's columns
                DataGridViewRow dgvRow = new DataGridViewRow();
                dgvRow.CreateCells(filePropertiesForm.dgvFileData);

                // Fill cells by matching column names
                for (int i = 0; i < filePropertiesForm.dgvFileData.Columns.Count; i++)
                {
                    string sColumnName = filePropertiesForm.dgvFileData.Columns[i].Name;

                    if (ruRecord.odictColumnValues.ContainsKey(sColumnName))
                    {
                        dgvRow.Cells[i].Value = ruRecord.odictColumnValues[sColumnName];
                    }
                }

                // Add the populated row
                filePropertiesForm.dgvFileData.Rows.Add(dgvRow);
            }


        }

        internal static void AddFile(FilePropertiesForm filePropertiesForm)
        {
            //add a new row to datagridview 
            Dictionary<string, string> oDictFileToAdd = new Dictionary<string, string>();
            //this is to simulate the user addding a file to a project
            //write the data that you know...the user pressed a SIMILAR button to add new project, add new file->creates a new visio document and adds it to the database (for now the data should be the same as the first file)
            //file properties change in a few different places...file name change, drawing type, ignorewirecolor, allowduplicates, showpointdata...
            //in the new row add the exact data that is in the first row except increase the number for ifileID and make sure that the created date and the modified date are the current date as the process is happening
            DataGridViewRow dgvFirstRow = filePropertiesForm.dgvFileData.Rows[0];

            //create a new row 
            DataGridViewRow dgvNewRow = new DataGridViewRow();
            dgvNewRow.CreateCells(filePropertiesForm.dgvFileData);

            for(int i = 0; i < dgvFirstRow.Cells.Count; i++)
            {
                string sColumnName = filePropertiesForm.dgvFileData.Columns[i].Name;
                switch(sColumnName)
                {
                    case "FileID":
                        {
                            int iFirstFileID = Convert.ToInt32(dgvFirstRow.Cells[i].Value); //will need to get the last file id to make this more general but for now let's start with one...
                            dgvNewRow.Cells[i].Value = iFirstFileID + 1;
                            break;
                        }
                    case "CreatedDate":
                    case "LastModifiedDate":
                        {
                            dgvNewRow.Cells[i].Value = DateTime.Now.ToString("yyyy-MM-dd");
                            break;
                        }
                    default:
                        {
                            //copy this information over
                            dgvNewRow.Cells[i].Value = dgvFirstRow.Cells[i].Value;
                            break;
                        }

                }
                oDictFileToAdd.Add(sColumnName, dgvFirstRow.Cells[i].Value.ToString());



            }

            int iInsertIndex = filePropertiesForm.dgvFileData.Rows.Count - 1;
            //add the new row to the datagridviewrow
            filePropertiesForm.dgvFileData.Rows.Insert(iInsertIndex, dgvNewRow);

            //now we need to write this to sql and add a new entry in the files_table
            string sTable = "files_table";
            DataProcessingUtilities.BuildUpdateSqlForRecordDictionary(sTable, oDictFileToAdd, "INSERT INTO");
        }

        internal static void UpdateFile(FilePropertiesForm filePropertiesForm)
        {
            //will be ever be changing multiple files? 
            //only if we give them the space --otherwise there is no spot for them to change something on two different files...  

            //where would we need to call update file?
            //--when the file name or file path is changed, when the user changes the drawing type, wire prefix, ignroewirecolor, allow duplicate tags, show point data (some from the settings, another from the project properties form)
            //modified date? when do i update this
            //project_id will only change once we give the user the ability to associte and disassociate files with a project...
            m_dictFileDataInfoToCompare.Clear();
            if(m_mruRecordsToCompare.ruRecords != null)
            {
                m_mruRecordsToCompare.ruRecords.Clear();
            }
           


            //this will be done a little bit differently because the wire prefix, ignore wirecolor, allow duplicate tags, and show point data is from the visassist settings,
            //but the file name, file path and drawing type are from somewhere else..also revision id i think...so therefore when we update the file we will often only be looking to update one column...

            List<RecordUpdate> lstRecordUpdate = new List<RecordUpdate>();
            foreach(DataGridViewRow dgvRow in filePropertiesForm.dgvFileData.Rows)
            {
                Dictionary<string, string> oDictColumnValues = new Dictionary<string, string>();
                string sPriamryKey = "";
                int iPrimaryKeyValue = 0;

                for(int i = 0; i < filePropertiesForm.dgvFileData.Columns.Count; i++)
                {
                    DataGridViewColumn dgvColumn = filePropertiesForm.dgvFileData.Columns[i];
                    string sColumnname = dgvColumn.Name;
                    string sValue = dgvRow.Cells[i].Value.ToString();
                    string sKey = dgvColumn.Name;

                    if(sColumnname == "FileID")
                    {
                        //this is the our priamry key 
                        sPriamryKey = sColumnname;
                        iPrimaryKeyValue = Convert.ToInt32(sValue);
                    }
                    else
                    {
                        oDictColumnValues.Add(sColumnname, sValue);
                    }
                }

                //create a recordupdate for this row
                RecordUpdate ruRecordUpdate = new RecordUpdate();
                ruRecordUpdate.sPrimaryKeyColumn = sPriamryKey;
                ruRecordUpdate.iId = iPrimaryKeyValue;
                ruRecordUpdate.odictColumnValues = oDictColumnValues;

                lstRecordUpdate.Add(ruRecordUpdate);
            }

            //wrap all the records into a multiple recorsupdates object
            m_mruRecordsToCompare = new MultipleRecordUpdates(lstRecordUpdate);
            //build up the m_dictFileDataInfoToUpdate based on each value in each column in the first row of filePropertiesForm.dgvFileData
            //DataGridViewRow dgvFirstRow = filePropertiesForm.dgvFileData.Rows[0];
            //for(int i= 0; i < filePropertiesForm.dgvFileData.Columns.Count; i++)
            //{
                

            //    m_dictFileDataInfoToCompare.Add(sKey, sValue);
            //}


            //however i think we need to adjust how we are handling this because we won't compare a list of 14 to 14 always because if the user changes the drawing type does this mean
            //we will go to all the other places that contain the file data and then always be checking two identical lists? 
            //this is unlike the project properties because the project properties is all in one place
            //m_dictFileDataInfoToUpdate = DataProcessingUtilities.CompareDataDictionaries(m_dictFileDataInfoBase, m_dictFileDataInfoToCompare);

            m_mruRecordsToUpdate = DataProcessingUtilities.ComapreDataForMultipleRecords(m_mruRecordsBase, m_mruRecordsToCompare);


            if(m_mruRecordsToUpdate.ruRecords.Count > 0)
            {
                string sTable = "files_table";
                DataProcessingUtilities.BuildUpdateSqlForMultipleRecords(sTable, m_mruRecordsToUpdate);
                FileUtilities.GetFileDataFromDatabase(filePropertiesForm);
            }
            //if (m_dictFileDataInfoToUpdate.Count > 0)
            //{
            //    string sTable = "files_table";

            //    DataProcessingUtilities.BuildUpdateSqlForOneRecord(sTable, m_dictFileDataInfoToUpdate, "UPDATE");

            //    FileUtilities.GetFileDataFromDatabase(filePropertiesForm); //go and grab the data from the database to populate the m_dictProjectInfoBase

            //}
        }

        internal static void DeleteFile(FilePropertiesForm filePropertiesForm)
        {
            //get the selected row in the filePropertiesForm.dgvFileData to determine which file to delete
            DataGridViewRow dgvSelectedRow = filePropertiesForm.dgvFileData.SelectedRows[0];
            Dictionary<string, string> oDictFileToDelete = new Dictionary<string, string>();
            string sTable = "files_table";

            if(dgvSelectedRow != null)
            {
                int iFileID = Convert.ToInt32(dgvSelectedRow.Cells["FileID"].Value);

                //loop through each column in the selectec row and add the values the oDictFileToDelete
                foreach(DataGridViewCell dgvCell in dgvSelectedRow.Cells)
                {
                    string sColumnName = dgvSelectedRow.DataGridView.Columns[dgvCell.ColumnIndex].Name;
                    string sValue = dgvCell.Value.ToString();

                    oDictFileToDelete.Add(sColumnName, sValue);
                }
            }
            DataProcessingUtilities.BuildUpdateSqlForRecordDictionary(sTable, oDictFileToDelete, "DELETE");
        }
    }
}
