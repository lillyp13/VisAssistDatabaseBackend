using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Security.Cryptography;
using System.Security.Permissions;
using System.Text;
using System.Windows.Forms;
using VisAssistDatabaseBackEnd.Forms;
using Visio = Microsoft.Office.Interop.Visio;



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
        //static SQLiteConnection Connection = ConnectionsUtilities.Connection;



        string sFileNumber; //for pageformat and fileformat...





        public static Dictionary<string, string> m_dictFileDataInfoBase = new Dictionary<string, string>();  //key is the column name
        public static Dictionary<string, string> m_dictFileDataInfoToCompare = new Dictionary<string, string>();
        public static Dictionary<string, string> m_dictFileDataInfoToUpdate = new Dictionary<string, string>();
        public static MultipleRecordUpdates m_mruRecordsBase = new MultipleRecordUpdates();
        public static MultipleRecordUpdates m_mruRecordsToCompare = new MultipleRecordUpdates();
        public static MultipleRecordUpdates m_mruRecordsToUpdate = new MultipleRecordUpdates();


        //SEEDING
        internal static void AddSeedFile()
        {
            //make sure there is a project in the project_table...

            bool bDoesTableExist = DataProcessingUtilities.DoesParentTableHaveRecord(DataProcessingUtilities.SqlTables.FilesTable.sFilesTable);
            if (bDoesTableExist)
            {
                DatabaseSeeding.SeedFiles();
            }
            else
            {
                MessageBox.Show("Please add a record to the project_Table.");
            }

        } //SEED DATA




        //CRUD Actions
        internal static void AddNewFile()
        {
            MultipleRecordUpdates oFileRecord = new MultipleRecordUpdates();
            //create a new visio file (it will either be classified as a class a or b depedning on which one the user wants...
            string sClass = "Secondary"; //this is dependent on which kind of file th user wants to add, but i believe in most cases this will be used to add a new secondary file to a project...
                                         //it is possible that the user wants to add a Master file 

           Visio.Document ovDoc = AddVisioDocument(sClass);

            Visio.Page ovPage = ovDoc.Pages[1]; //get the first page...
            string sFilePath = ReturnFileStructurePath(ovDoc.Path);
            string sFileName = ovDoc.Name;
            sFilePath = sFilePath + sFileName;

            //need to get the projectID of the db we want to add to
            ProjectUtilities.GetProjectInfoFromDatabase();
            string sProjectID = ProjectUtilities.m_mruRecordsBase.ruRecords[0].sId;

            oFileRecord = AddFileToDatabase(ovDoc, sFilePath, sProjectID);
            AddUserCellsToDocument(oFileRecord, ovDoc);

            //increase the filecount for the project
            //get the project id from the document 




            PageUtilities.AddUserCellsToPage();
            PageUtilities.AddPageToDatabase(ovPage);

            ovDoc.SaveAs(sFilePath);
        }

        internal static MultipleRecordUpdates AddFileToDatabase(Visio.Document ovDoc, string sFilePath, string sProjectID)
        {
            MultipleRecordUpdates oFileRecord = new MultipleRecordUpdates();
            oFileRecord = FileUtilities.BuildFileInformation(ovDoc, sFilePath, sProjectID);
            DataProcessingUtilities.BuildInsertSqlForMultipleRecords(DataProcessingUtilities.SqlTables.FilesTable.sFilesTable, oFileRecord);

            //increase the filecount for the proejct...
            ProjectUtilities.AdjustFileCount("Increase");

            return oFileRecord;
        }

        internal static void UpdateFile(FilePropertiesForm filePropertiesForm)
        {
            //will be ever be changing multiple files? 
            //only if we give them the space --otherwise there is no spot for them to change something on two different files...  

            //where would we need to call update file?
            //--when the file name or file path is changed, when the user changes the drawing type, wire prefix, ignroewirecolor, allow duplicate tags, show point data (some from the settings, another from the project properties form)
            //modified date? when do i update this
            //project_id will only change once we give the user the ability to associte and disassociate files with a project...
            ;
            if (m_mruRecordsToCompare.ruRecords != null)
            {
                m_mruRecordsToCompare.ruRecords.Clear();
            }



            //this will be done a little bit differently because the wire prefix, ignore wirecolor, allow duplicate tags, and show point data is from the visassist settings,
            //but the file name, file path and drawing type are from somewhere else..also revision id i think...so therefore when we update the file we will often only be looking to update one column...

            List<RecordUpdate> lstRecordUpdate = new List<RecordUpdate>();
            foreach (DataGridViewRow dgvRow in filePropertiesForm.dgvFileData.Rows)
            {
                Dictionary<string, string> oDictColumnValues = new Dictionary<string, string>();

                string sPrimaryKeyValue = "";

                for (int i = 0; i < filePropertiesForm.dgvFileData.Columns.Count; i++)
                {
                    DataGridViewColumn dgvColumn = filePropertiesForm.dgvFileData.Columns[i];
                    string sColumnName = dgvColumn.Name;
                    string sValue = dgvRow.Cells[i].Value.ToString();
                    string sKey = dgvColumn.Name;

                    if (sColumnName != DataProcessingUtilities.SqlTables.FilesTable.sFilesTablePK)
                    {
                        oDictColumnValues.Add(sColumnName, sValue);
                    }
                    else
                    {
                        //this is the PK
                        sPrimaryKeyValue = sValue;
                    }

                }

                //create a recordupdate for this row
                RecordUpdate ruRecordUpdate = new RecordUpdate();
                ruRecordUpdate.sPrimaryKeyColumn = DataProcessingUtilities.SqlTables.FilesTable.sFilesTablePK;
                ruRecordUpdate.sId = sPrimaryKeyValue;
                ruRecordUpdate.odictColumnValues = oDictColumnValues;

                lstRecordUpdate.Add(ruRecordUpdate);
            }

            //wrap all the records into a multiple recorsupdates object
            m_mruRecordsToCompare = new MultipleRecordUpdates(lstRecordUpdate);

            //compare the two record sets and build a new record set based on only the changes
            m_mruRecordsToUpdate = DataProcessingUtilities.CompareDataForMultipleRecords(m_mruRecordsBase, m_mruRecordsToCompare);


            if (m_mruRecordsToUpdate.ruRecords.Count > 0)
            {
                //there is a change
                //build the update sql for the files_table
                DataProcessingUtilities.BuildUpdateSqlForMultipleRecords(DataProcessingUtilities.SqlTables.FilesTable.sFilesTable, m_mruRecordsToUpdate);
                //reset the base record set
                FileUtilities.GetFileDataFromDatabase(filePropertiesForm);
            }
        }
        internal static void DeleteFile(FilePropertiesForm filePropertiesForm)
        {
            //get the selected row in the filePropertiesForm.dgvFileData to determine which file to delete

            MultipleRecordUpdates mruRecords = DisassociateFile(filePropertiesForm);

            //go and actually delete the visio file itself 
            foreach (RecordUpdate ruRecordUpdate in mruRecords.ruRecords)
            {
                string sFilePath = ruRecordUpdate.odictColumnValues["FilePath"];

                //the file does need to be closed, if it is open we need to make a delayed event to delete it...i think
                if (File.Exists(sFilePath))
                {
                    File.Delete(sFilePath);
                }
                ProjectUtilities.AdjustFileCount("Decrease");
            }
        }
        internal static void DeleteAllFiles()
        {
            //delete all the records in the files_table
            using (SQLiteConnection sqliteConnection = new SQLiteConnection(DatabaseConfig.ConnectionString))
            {
                sqliteConnection.Open();

                //enable foreign key enforcemnt for this connection
                using (SQLiteCommand sqlitcmdPragma = new SQLiteCommand("PRAGMA foreign_keys = ON;", sqliteConnection))
                {
                    sqlitcmdPragma.ExecuteNonQuery();
                }

                // string sDelete = "DELETE FROM files_table;";
                string sDelete = "DELETE FROM " + DataProcessingUtilities.SqlTables.FilesTable.sFilesTable + ";";

                using (SQLiteCommand sqlitecmdCommand = new SQLiteCommand(sDelete, sqliteConnection))
                {
                    //logging here 
                    sqlitecmdCommand.ExecuteNonQuery();

                }



            }

            //need to also clear the filecount in the project properites 
            //set the FileCount to be 0 in the project_table where the id = 1
            using (SQLiteConnection sqliteconConnection = new SQLiteConnection(DatabaseConfig.ConnectionString))
            {
                sqliteconConnection.Open();

                string sSqlUpdate = "UPDATE " + DataProcessingUtilities.SqlTables.ProjectTable.sProjectTable + " SET FileCount = 0 WHERE Id = @ProjectID";

                using (SQLiteCommand cmd = new SQLiteCommand(sSqlUpdate, sqliteconConnection))
                {
                    cmd.Parameters.AddWithValue("@ProjectID", 1); // set project id as 1...
                    cmd.ExecuteNonQuery();
                }
            }

        }






        internal static MultipleRecordUpdates DisassociateFile(FilePropertiesForm filePropertiesForm)
        {
            // Get the selected row
            MultipleRecordUpdates mruRecords = new MultipleRecordUpdates();
            DataGridViewSelectedRowCollection colSelectedRows = filePropertiesForm.dgvFileData.SelectedRows;
            if (colSelectedRows == null || colSelectedRows.Count == 0)
            {
                MessageBox.Show("Please select at least one file to delete.");
                return mruRecords;
            }

            // Build a list of RecordUpdate objects for each selected row
            List<RecordUpdate> lstRecordsToDelete = new List<RecordUpdate>();
            Dictionary<string, string> oDictColumnValues = new Dictionary<string, string>();
            foreach (DataGridViewRow dgvRow in colSelectedRows)
            {
                string sFileID = dgvRow.Cells["FileID"].Value.ToString();
                string sFilePath = dgvRow.Cells["FilePath"].Value.ToString();
                oDictColumnValues.Add("FilePath", sFilePath);

                RecordUpdate ruRecord = new RecordUpdate();
                ruRecord.sPrimaryKeyColumn = DataProcessingUtilities.SqlTables.FilesTable.sFilesTablePK;
                ruRecord.sId = sFileID;
                ruRecord.odictColumnValues = oDictColumnValues;

                lstRecordsToDelete.Add(ruRecord);
            }

            mruRecords = new MultipleRecordUpdates(lstRecordsToDelete);
            //based on the file path of the file to disassociate, open it and make clear the projectID
            bool bClearedProjectID = ProjectUtilities.ClearProjectID(mruRecords);
            if (bClearedProjectID)
            {
                // Disassociate by deleting the record in the database
                DataProcessingUtilities.BuildDeleteSqlForMultipleRecords(DataProcessingUtilities.SqlTables.FilesTable.sFilesTable, mruRecords);

                ProjectUtilities.AdjustFileCount("Decrease");

                foreach (DataGridViewRow dgvRow in colSelectedRows)
                {
                    filePropertiesForm.dgvFileData.Rows.Remove(dgvRow);
                }
            }
            else
            {
                //we are unable to disassociate the file because the file is open in a different instance of visio...
                MessageBox.Show("Please close the file: " + mruRecords.ruRecords[0].odictColumnValues["FilePath"] + " in order to disassociate.");
            }



            return mruRecords;
        }







        //Helper Functions
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
                if (m_mruRecordsBase.ruRecords != null)
                {
                    m_mruRecordsBase.ruRecords.Clear();
                }


                //select all the files from the files_table
                //string sSQl = @"SELECT * FROM files_table";
                string sSQl = @"SELECT * FROM " + DataProcessingUtilities.SqlTables.FilesTable.sFilesTable;
                List<RecordUpdate> lstRecords = new List<RecordUpdate>();

                //logging statement placeholder
                using (SQLiteConnection sqliteconConnection = new SQLiteConnection(DatabaseConfig.ConnectionString))
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

                                string sID = "";
                                for (int i = 0; i < sqlitereadReader.FieldCount; i++)
                                {
                                    string sColumnName = sqlitereadReader.GetName(i);
                                    string sValue = sqlitereadReader.IsDBNull(i) ? string.Empty : sqlitereadReader.GetValue(i).ToString();

                                    if (sColumnName != DataProcessingUtilities.SqlTables.FilesTable.sFilesTablePK)
                                    {
                                        odictColumnValues.Add(sColumnName, sValue);
                                    }
                                    else
                                    {
                                        sID = sqlitereadReader.GetValue(i).ToString(); //this is the PK
                                    }



                                }
                                //create a recordupdate for this specfic record (row)
                                RecordUpdate ruRecordUpdate = new RecordUpdate();
                                ruRecordUpdate.sPrimaryKeyColumn = DataProcessingUtilities.SqlTables.FilesTable.sFilesTablePK;
                                ruRecordUpdate.sId = sID;
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
                    if (i == 0)
                    {
                        //this is the first row get the PK
                        dgvRow.Cells[i].Value = ruRecord.sId;
                    }
                    else
                    {
                        string sColumnName = filePropertiesForm.dgvFileData.Columns[i].Name;

                        if (ruRecord.odictColumnValues.ContainsKey(sColumnName))
                        {
                            dgvRow.Cells[i].Value = ruRecord.odictColumnValues[sColumnName];
                        }
                    }

                }

                // Add the populated row
                filePropertiesForm.dgvFileData.Rows.Add(dgvRow);
            }


        }

        internal static MultipleRecordUpdates BuildFileInformation(Visio.Document ovDoc, string sFilePath, string sProjectGuid)
        {
            //this should build a multiple record update of the file...
            //we have the projectID from the project we just added, file name is in the file path, we have the filepath, created date and last modified date should be todays date, version should be 1, class should be VisAssistDocument, and the reset we can leave empty...
            //get the active document 

            //we are passing in the filepath because the docuemnt could be a temp doc if it is open in a different visio instance...


            string sFileName = Path.GetFileName(sFilePath);



            Dictionary<string, string> oDictFileValues = new Dictionary<string, string>();
            //oDictFileValues.Add("ProjectID", "1");
            oDictFileValues.Add("FileName", sFileName);
            oDictFileValues.Add("FilePath", sFilePath);
            //oDictFileValues.Add("CreatedDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            oDictFileValues.Add("LastModifiedDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            oDictFileValues.Add("Version", "1.0.0");
            oDictFileValues.Add("Class", "VisAssistDocument");

            RecordUpdate ruFileRecord = new RecordUpdate();
            ruFileRecord.sPrimaryKeyColumn = DataProcessingUtilities.SqlTables.FilesTable.sFilesTablePK;
            string sProjectID = "";
            if (ovDoc.DocumentSheet.CellExists["User.ProjectID", 0] == -1)
            {
                //sProjectID = ovDoc.DocumentSheet.Cells["User.ProjectID"].get_ResultStr(0);
                //if (sProjectID == "")
                //{
                //    //this is an orphaned file...
                //    sProjectID = sProjectGuid;
                //}
                //else
                //{
                //    //this file belonged to a different project so we are going to need to update it to the new one...
                //    sProjectID = sProjectGuid;
                //}
                sProjectID = sProjectGuid;
                oDictFileValues.Add("ProjectID", sProjectID);
            }
            else
            {
                sProjectID = sProjectGuid; //we are creating the file and project right now and we haven't added the user cerlls yet
                oDictFileValues.Add("ProjectID", sProjectID);
            }

            //check to see if the document has a User.FileID guid... and take that if it does...
            string sID = "";
            if (ovDoc.DocumentSheet.CellExists["User.FileID", 0] == -1)
            {
                sID = ovDoc.DocumentSheet.Cells["User.FileID"].get_ResultStr(0);
                oDictFileValues["CreatedDate"] = ovDoc.DocumentSheet.Cells["User.CreatedDate"].get_ResultStr(0);
            }
            else
            {
                sID = GenerateFileID(sProjectID, sFilePath, DateTime.Now);
                oDictFileValues["CreatedDate"] = DateTime.Now.ToString(); //we are creating this for the first time
            }



            ruFileRecord.sId = sID;
            ruFileRecord.odictColumnValues = oDictFileValues;

            return new MultipleRecordUpdates(new List<RecordUpdate> { ruFileRecord });

        }
        /// <summary>
        /// this adds the visio file itself after opening a save file dialog box and saves it to where the user specifies
        /// this should be adpated to create the file based off of a template... builds the Master document
        /// I will also build another routine AddVisioSecondaryDocument that will do the same thing except will not have the cover pages....
        /// </summary>
        /// <param name="sClass"></param>
        internal static Visio.Document AddVisioDocument(string sClass)
        {
            //this is for when we are adding a new visio document/file...

            //open a save file dialog to ask user where they want to save the visio document that will be creatd 
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Title = "Save Visio Document";
                saveFileDialog.Filter = "Visio Files (*.vsdx)|*.vsdx|All Files (*.*)|*.*";
                saveFileDialog.DefaultExt = "vsdx";
                saveFileDialog.AddExtension = true;

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string sFilePath = saveFileDialog.FileName;

                    Visio.Application ovVisioApp = Globals.ThisAddIn.Application;

                    // Create new document
                    Visio.Document ovDoc = ovVisioApp.Documents.Add("");

                    // Anchor it to disk immediately and cleanly
                    const short visSaveAsNoPrompt = 0x40;
                    const short visSaveAsDontList = 0x200;

                    ovDoc.SaveAsEx(sFilePath,(short)(visSaveAsNoPrompt | visSaveAsDontList));

                    // Set DB path
                    string sDirectoryPath = Path.GetDirectoryName(sFilePath);
                    DatabaseConfig.DatabasePath = Path.Combine(sDirectoryPath, "VisAssistBackEnd.db");

                    // Close cover pages doc if open
                    string sFilePathOfCoverPages = Path.Combine(sDirectoryPath, "Dwg - Cover Pages.vsdx");

                    foreach (Visio.Document ovDocToCheck in ovVisioApp.Documents)
                    {
                        string fullPath = Path.Combine(sDirectoryPath, ovDocToCheck.Name);

                        if (fullPath.Equals(sFilePathOfCoverPages, StringComparison.OrdinalIgnoreCase))
                        {
                            if (!ovDocToCheck.Saved)
                            {
                                ovDocToCheck.Save();
                            }
                            ovDocToCheck.Close();
                            break;
                        }
                    }

                    return ovDoc; // ← IMPORTANT: return the live document
                }
                else
                {
                    MessageBox.Show("Save operation cancelled.");
                    return null;
                }
            }
        } //this creates the  new visio file and saves it where the user specified...

        internal static void AddCoverPageDocument(string sFilePath)
        {
            //this creates the cover page documents and calls is Dwg - Cover Pages.vsdx and saves it to the folder path...
            Visio.Application ovVisioApp = Globals.ThisAddIn.Application;
            Visio.Document ovDoc = ovVisioApp.Documents.Add("");

            //save it, close it and reopen so that the file doesn't end up in a dirty state
            //we won't need to do this once we add the templates because we do a file.copy and then open the new file...
            //we want to design wehre user chooses the template and we'll grab it from access (i think)
            ovDoc.SaveAs(sFilePath);

            ovDoc.Close();

            ovDoc = ovVisioApp.Documents.Open(sFilePath);

            //get the file name and set that to the database path...
            string sDirectoryPath = Path.GetDirectoryName(sFilePath);
            DatabaseConfig.DatabasePath = Path.Combine(sDirectoryPath, "VisAssistBackEnd.db");
        }


        internal static void AddUserCellsToDocument(MultipleRecordUpdates oFileRecord, Visio.Document ovDoc)
        {
            //Visio.Document ovDoc = Globals.ThisAddIn.Application.ActiveDocument;
            ovDoc.DocumentSheet.AddNamedRow((short)Visio.VisSectionIndices.visSectionUser, "ProjectID", 0);
            ovDoc.DocumentSheet.Cells["User.ProjectID"].Formula = "\"" + oFileRecord.ruRecords[0].odictColumnValues["ProjectID"] + "\"";

            ovDoc.DocumentSheet.AddNamedRow((short)Visio.VisSectionIndices.visSectionUser, "FileID", 0);
            //add the fileid from the record we just added to this cell..
            ovDoc.DocumentSheet.Cells["User.FileID"].Formula = "\"" + oFileRecord.ruRecords[0].sId + "\"";

            ovDoc.DocumentSheet.AddNamedRow((short)Visio.VisSectionIndices.visSectionUser, "CreatedDate", 0);
            ovDoc.DocumentSheet.Cells["User.CreatedDate"].Formula = "\"" + oFileRecord.ruRecords[0].odictColumnValues["CreatedDate"] + "\"";
        }

        internal static bool CheckThatFilesExistInFolder()
        {
            //use m_mruRecordsBase and check all the records file path to make sure the file exists where it should 
            bool bCleanBaseRecords = false;
            List<RecordUpdate> lstFilesToDisassociate = new List<RecordUpdate>();
            foreach (RecordUpdate ruRecord in m_mruRecordsBase.ruRecords)
            {
                string sFilePath = ruRecord.odictColumnValues["FilePath"].ToString();

                if (!File.Exists(sFilePath))
                {
                    RecordUpdate ruRecordToDelete = new RecordUpdate();
                    ruRecordToDelete.sPrimaryKeyColumn = ruRecord.sPrimaryKeyColumn;
                    ruRecordToDelete.sId = ruRecord.sId;
                    ruRecordToDelete.odictColumnValues = ruRecord.odictColumnValues;


                    lstFilesToDisassociate.Add(ruRecordToDelete);
                }
            }
            MultipleRecordUpdates mruRecordsToDisassociate = new MultipleRecordUpdates(lstFilesToDisassociate);

            if (mruRecordsToDisassociate.ruRecords.Count > 0)
            {
                bCleanBaseRecords = true;
                //we are going to disassociate the file..
                DataProcessingUtilities.BuildDeleteSqlForMultipleRecords(DataProcessingUtilities.SqlTables.FilesTable.sFilesTable, mruRecordsToDisassociate);

                //we need to clean up our m_mruRecords again..


                string sMessage = "The following files could not be found:\n\n" + string.Join("\n", lstFilesToDisassociate.Select(r => r.odictColumnValues["FilePath"])) + "\n\nThese files will be dissociated from the database";


                MessageBox.Show(sMessage, "VisAssist");
                return bCleanBaseRecords;
            }
            return bCleanBaseRecords;
        }





        internal static void WhichFileToAssociate()
        {
            Visio.Application ovApp = Globals.ThisAddIn.Application;
            Visio.Document ovDoc = Globals.ThisAddIn.Application.ActiveDocument;

            // For example, assume your database path is stored in User-defined cell:
            string sFolderPath = ReturnFileStructurePath(ovDoc.Path);


            // 2️⃣ Open File Dialog to pick the other database
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {

                openFileDialog.Title = "Select the file to associate with the current document";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string sFilePath = openFileDialog.FileName;

                    string sFileName = Path.GetFileName(sFilePath);
                    string sDirectory = Path.GetDirectoryName(sFilePath);
                    //open the file in visio first if it is not already open...going to have to do this for each document?????
                    //Visio.Document ovOtherDoc = ovApp.Documents.Open(sFolderPath + sFileName);
                    // 3️⃣ Call your merge/associate function
                    AssociateFile(sFilePath, sFolderPath);

                    MessageBox.Show("Databases successfully associated!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private static void AssociateFile(string sFilePath, string sFolderPath)
        {

            //RIGHT NOW I AM ONLY ASSOCIATING FILES THAT HAVE INFORMATION (ie have been in a database -they have the User.FileID... )
            Visio.Application ovApp = Globals.ThisAddIn.Application;
            string sFileName = Path.GetFileName(sFilePath);
            Visio.Document ovDoc = null;
            string sFullFilePath = ReturnFileStructurePath(sFilePath);
            // Check if file is already open in THIS Visio instance
            ovDoc = IsVisioFileOpen(ovApp, sFullFilePath);
            bool bCloseDocument = false;
            string sTempFilePath = null;
            bool bDeleteTempFilePath = false;
            string sDestFilePath = "";

            try
            {
                if (ovDoc == null)
                {
                    //if the doucment is null that means the file is not open yet
                    bCloseDocument = true;
                    //check to see if it is locked meaning it could possibly be open in an instance of visio that is not the current instance
                    if (!IsFileLocked(sFilePath))
                    {
                        //if the file is not locked(opened anywhere else) we can safelty open it
                        ovDoc = ovApp.Documents.OpenEx(sFilePath, (short)(Visio.VisOpenSaveArgs.visOpenHidden | Visio.VisOpenSaveArgs.visOpenRO));
                    }
                    else
                    {
                        //the file is locked (open in another instance of visio so we will make a copy of the file in a temp folder and open that file to associate...
                        bDeleteTempFilePath = true;
                        // Swallow the exception silently, create a temp copy instead
                        sTempFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + "_" + sFileName);
                        File.Copy(sFilePath, sTempFilePath, true);


                        ovDoc = ovApp.Documents.OpenEx(sTempFilePath, (short)(Visio.VisOpenSaveArgs.visOpenHidden | Visio.VisOpenSaveArgs.visOpenRO));

                    }


                }

                // Process the Visio document
                if (ovDoc != null)
                {
                    //need to get the projectID of the db we want to add to
                    ProjectUtilities.GetProjectInfoFromDatabase();
                    string sProjectID = ProjectUtilities.m_mruRecordsBase.ruRecords[0].sId;


                    AddFileToDatabase(ovDoc, sFilePath, sProjectID);
                    ovDoc.DocumentSheet.Cells["User.ProjectID"].Formula = "\"" + sProjectID + "\"";

                    foreach (Visio.Page ovPage in ovDoc.Pages)
                    {
                        PageUtilities.AddPageToDatabase(ovPage);
                    }
                    //will also need to put the work to add all the shapes on the page in the database....
                    // Copy the file to the new folder
                    

                    sDestFilePath = Path.Combine(sFolderPath, sFileName);
                    if (!bDeleteTempFilePath)
                    {
                        //we can copy from the given path...
                        //need to see if the file already exists in the location 
                        if (sFilePath != sDestFilePath)
                        {
                            File.Copy(sFilePath, sDestFilePath, true); //not the same path
                        }

                    }
                    else
                    {
                        //we need to copy from the temporary file...
                        File.Copy(sTempFilePath, sDestFilePath, true);
                    }


                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in AssociateFile " + ex.Message, "VisAssist");
            }
            finally
            {
                //delete/close the files that we need to based on if we opened it or the user opened it...
                if (bCloseDocument && ovDoc != null)
                {

                    ovDoc.SaveAs(sDestFilePath);
                    ovDoc.Close();
                }

                if (bDeleteTempFilePath)
                {
                    File.Delete(sTempFilePath);
                }
            }
        }

        internal static bool IsFileLocked(string filePath)
        {
            try
            {
                using (FileStream stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    return false; // file is not locked
                }
            }
            catch (IOException)
            {
                return true; // file is locked/open in a different instance...
            }
        }



        internal static Visio.Document IsVisioFileOpen(Visio.Application ovApp, string filePath)
        {
            string targetPath = Path.GetFullPath(filePath);

            foreach (Visio.Document doc in ovApp.Documents)
            {
                try
                {
                    string sDocNameToCheck = ReturnFileStructurePath(doc.Path);
                    sDocNameToCheck = Path.Combine(sDocNameToCheck, doc.Name);
                    if (!string.IsNullOrEmpty(sDocNameToCheck) &&
                        string.Equals(Path.GetFullPath(sDocNameToCheck), targetPath, StringComparison.OrdinalIgnoreCase))
                    {
                        return doc; // document is open
                    }
                }
                catch
                {
                    // Some system docs (like stencils) may throw exceptions; ignore them
                }
            }

            return null;
        }

        internal static string GenerateFileID(string sProjectID, string filePath, DateTime createdDate)
        {
            //project: sDirectoryPath + "Dwg - Cover Pages" + project name and created date
            //file: projectID + filepath + created date
            //page: ProjectID + FileID + page name + created date

            string input = sProjectID + filePath + createdDate.ToString("yyyy-MM-dd HH:mm:ss"); // formatted
            using (SHA256 sha = SHA256.Create())
            {
                byte[] bytehashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
                StringBuilder sb = new StringBuilder();
                foreach (byte b in bytehashBytes)
                {
                    sb.Append(b.ToString("x2")); // hex
                }

                return sb.ToString();
            }
        }



        internal static bool DoesDBFileExist()
        {
            Visio.Document ovDoc = Globals.ThisAddIn.Application.ActiveDocument;
            if (ovDoc != null)
            {
                string sFolderPath = ReturnFileStructurePath(ovDoc.Path);

                string sDBPath = Path.Combine(sFolderPath, "VisAssistBackEnd.db");

                if (File.Exists(sDBPath))
                {
                    return true;
                }
                else
                {
                    return false;
                }


            }
            return false;
        }











        //FILE STRUCTURE HELPER FUNCTIONS
        public static string ReturnFileStructurePath(string sToFilePath)
        {
            try
            {
                // *** CHANGED: removed unused sLocalFolder and sFileStructureToReturn ***

                // string sToFilePath = Globals.ThisAddIn.Application.ActiveDocument.Path;
                //Visio.Document ovThisVisioDocument = Globals.ThisAddIn.Application.ActiveDocument;

                //now if we are given a url (by having http in it) we need to get the tofilepath another way 
                if (sToFilePath.Contains("https://"))
                {

                    if (sToFilePath.IndexOf("d.docs.live.net", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        // This resolves https://d.docs.live.net/<CID>/...
                        sToFilePath = ResolveOnedriveCloudUrlToLocal(sToFilePath);

                    }
                    // --- OneDrive BUSINESS / SharePoint ---
                    else if (sToFilePath.IndexOf(".sharepoint.com", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        // This resolves https://tenant-my.sharepoint.com/...
                        sToFilePath = ResolveOneDriveBusinessPath(sToFilePath);

                    }


                    //string sOneDrivePath = ResolveOnedriveCloudUrlToLocal(sToFilePath);
                    //sToFilePath = sOneDrivePath;
                }
                else
                {

                }
                // Fallback: if we can't resolve, just return what Visio gave us
                return sToFilePath;
            }
            catch (Exception ex) // *** CHANGED: added catch + logging + null return ***
            {

                return null;
            }
        }

        public static string ResolveOneDriveBusinessPath(string cloudUrl)
        {
            if (string.IsNullOrEmpty(cloudUrl) || !cloudUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return null;

            const string baseKeyPath = @"Software\Microsoft\OneDrive\Accounts";

            using (RegistryKey accountsKey = Registry.CurrentUser.OpenSubKey(baseKeyPath))
            {
                if (accountsKey == null)
                    return null;

                foreach (string subKeyName in accountsKey.GetSubKeyNames())
                {
                    if (!subKeyName.StartsWith("Business", StringComparison.OrdinalIgnoreCase))
                        continue;

                    using (RegistryKey accountKey = accountsKey.OpenSubKey(subKeyName))
                    {
                        if (accountKey == null)
                            continue;

                        string serviceUri = accountKey.GetValue("ServiceEndpointUri") as string;
                        string localRoot = accountKey.GetValue("MountPoint") as string
                                        ?? accountKey.GetValue("UserFolder") as string;

                        if (string.IsNullOrEmpty(serviceUri) || string.IsNullOrEmpty(localRoot))
                            continue;

                        serviceUri = serviceUri.TrimEnd('/');
                        if (serviceUri.EndsWith("_api", StringComparison.OrdinalIgnoreCase))
                        {
                            serviceUri = serviceUri.Substring(0, serviceUri.Length - "_api".Length);
                            serviceUri = serviceUri.TrimEnd('/');
                        }
                        // Check if the cloud URL starts with the service endpoint
                        if (cloudUrl.StartsWith(serviceUri, StringComparison.OrdinalIgnoreCase))
                        {
                            //add\Documents to the serviceUri so that we don't add that to the path if it truly isn't located there
                            serviceUri = serviceUri + "/Documents";
                            // Compute relative path after the service endpoint
                            string relativePath = cloudUrl.Substring(serviceUri.Length).TrimStart('/');

                            // Convert URL separators to Windows path separators
                            string localPath = System.IO.Path.Combine(localRoot, relativePath.Replace("/", "\\"));

                            return localPath;
                        }
                    }
                }
            }

            return null;
        }

        public static string ResolveOnedriveCloudUrlToLocal(string visioPath)
        {
            // Not a cloud path
            if (!visioPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return visioPath;

            string cid = GetCidFromVisioUrl(visioPath);
            if (cid == null)
                return visioPath;

            string localRoot = FindLocalOneDrivePathForCid(cid);
            if (localRoot == null)
                return visioPath;  // Could not map → return original

            string relative = GetRelativeOneDrivePath(visioPath);

            string localPath = System.IO.Path.Combine(localRoot, relative.Replace("/", "\\"));

            return localPath;
        }
        private static string GetCidFromVisioUrl(string url)
        {
            const string marker = "d.docs.live.net/";
            int idx = url.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return null;

            idx += marker.Length;
            int endIdx = url.IndexOf("/", idx);
            if (endIdx < 0) return null;

            return url.Substring(idx, endIdx - idx);
        }

        private static string GetRelativeOneDrivePath(string fullUrl)
        {
            const string marker = "d.docs.live.net/";
            int idx = fullUrl.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return null;

            idx += marker.Length;

            // Find the slash after the CID
            int firstSlash = fullUrl.IndexOf("/", idx);
            if (firstSlash < 0) return null;

            return fullUrl.Substring(firstSlash + 1); // e.g., "Documents/VisAssist/..."
        }

        private static string FindLocalOneDrivePathForCid(string cid)
        {
            const string baseKeyPath = @"Software\Microsoft\OneDrive\Accounts";

            using (RegistryKey accountsKey = Registry.CurrentUser.OpenSubKey(baseKeyPath))
            {
                if (accountsKey == null)
                    return null;

                foreach (string subKeyName in accountsKey.GetSubKeyNames())
                {
                    using (RegistryKey accountKey = accountsKey.OpenSubKey(subKeyName))
                    {
                        if (accountKey == null)
                            continue;

                        // Read CID from registry
                        string cidOnDisk = accountKey.GetValue("CID") as string;
                        if (cidOnDisk == null)
                            continue;

                        if (!cidOnDisk.Equals(cid, StringComparison.OrdinalIgnoreCase))
                            continue;  // Not the matching account

                        // Found the correct OneDrive account
                        string localPath = accountKey.GetValue("UserFolder") as string;
                        if (localPath != null && Directory.Exists(localPath))
                            return localPath;
                    }
                }
            }

            return null;
        }


    }
}
