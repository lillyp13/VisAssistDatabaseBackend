using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

            bool bDoesTableExist = DataProcessingUtilities.DoesParentTableHaveRecord(DataProcessingUtilities.SqlTables.sFilesTable);
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
        internal static void AddFile()
        {

            //create a new visio file (it will either be classified as a class a or b depedning on which one the user wants...
            string sClass = "Secondary"; //this is dependent on which kind of file th user wants to add, but i believe in most cases this will be used to add a new secondary file to a project...
            //it is possible that the user wants to add a Master file 
            AddVisioDocument(sClass);
            MultipleRecordUpdates oFileRecord = new MultipleRecordUpdates();
            Visio.Document ovDoc = Globals.ThisAddIn.Application.ActiveDocument;

            oFileRecord = FileUtilities.BuildFileInformation();
            DataProcessingUtilities.BuildInsertSqlForMultipleRecords(DataProcessingUtilities.SqlTables.sFilesTable, oFileRecord);

            ovDoc.DocumentSheet.AddNamedRow((short)Visio.VisSectionIndices.visSectionUser, "ID", 0);
            ovDoc.DocumentSheet.Cells["User.ProjectID"].ResultIU = Convert.ToInt32(oFileRecord.ruRecords[0].odictColumnValues["ProjectID"]);

            ovDoc.DocumentSheet.AddNamedRow((short)Visio.VisSectionIndices.visSectionUser, "FileID", 0);
            //add the fileid from the record we just added to this cell..
            ovDoc.DocumentSheet.Cells["User.FileID"].Formula = oFileRecord.ruRecords[0].iId.ToString();



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

                int iPrimaryKeyValue = 0;

                for (int i = 0; i < filePropertiesForm.dgvFileData.Columns.Count; i++)
                {
                    DataGridViewColumn dgvColumn = filePropertiesForm.dgvFileData.Columns[i];
                    string sColumnName = dgvColumn.Name;
                    string sValue = dgvRow.Cells[i].Value.ToString();
                    string sKey = dgvColumn.Name;

                    if (sColumnName != DataProcessingUtilities.SqlTables.sFilesTablePK)
                    {
                        oDictColumnValues.Add(sColumnName, sValue);
                    }
                    else
                    {
                        //this is the PK
                        iPrimaryKeyValue = Convert.ToInt32(sValue);
                    }

                }

                //create a recordupdate for this row
                RecordUpdate ruRecordUpdate = new RecordUpdate();
                ruRecordUpdate.sPrimaryKeyColumn = DataProcessingUtilities.SqlTables.sFilesTablePK;
                ruRecordUpdate.iId = iPrimaryKeyValue;
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
                DataProcessingUtilities.BuildUpdateSqlForMultipleRecords(DataProcessingUtilities.SqlTables.sFilesTable, m_mruRecordsToUpdate);
                //reset the base record set
                FileUtilities.GetFileDataFromDatabase(filePropertiesForm);
            }
        }
        internal static void DeleteFile(FilePropertiesForm filePropertiesForm)
        {
            //get the selected row in the filePropertiesForm.dgvFileData to determine which file to delete

            // Get the selected row
            DataGridViewSelectedRowCollection colSelectedRows = filePropertiesForm.dgvFileData.SelectedRows;
            if (colSelectedRows == null || colSelectedRows.Count == 0)
            {
                MessageBox.Show("Please select at least one file to delete.");
                return;
            }

            // Build a list of RecordUpdate objects for each selected row
            List<RecordUpdate> lstRecordsToDelete = new List<RecordUpdate>();
            foreach (DataGridViewRow dgvRow in colSelectedRows)
            {
                int iFileID = Convert.ToInt32(dgvRow.Cells["FileID"].Value);

                RecordUpdate ruRecord = new RecordUpdate();
                ruRecord.sPrimaryKeyColumn = DataProcessingUtilities.SqlTables.sFilesTablePK;
                ruRecord.iId = iFileID;

                lstRecordsToDelete.Add(ruRecord);
            }

            MultipleRecordUpdates mru = new MultipleRecordUpdates(lstRecordsToDelete);

            // Call delete
            DataProcessingUtilities.BuildDeleteSqlForMultipleRecords(DataProcessingUtilities.SqlTables.sFilesTable, mru);

            foreach (DataGridViewRow dgvRow in colSelectedRows)
            {
                filePropertiesForm.dgvFileData.Rows.Remove(dgvRow);
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
                string sDelete = "DELETE FROM " + DataProcessingUtilities.SqlTables.sFilesTable + ";";

                using (SQLiteCommand sqlitecmdCommand = new SQLiteCommand(sDelete, sqliteConnection))
                {
                    //logging here 
                    sqlitecmdCommand.ExecuteNonQuery();

                }
                //reset the auto-increment counter, also need to delete the pages_table...
                string[] saTablesToReset = { "files_table", "pages_table" };
                foreach (string sTable in saTablesToReset)
                {
                    //reset the auto-increment counter  //need to also reset the files_table and the pages_table and all other tables....
                    string sReset = $"DELETE FROM sqlite_sequence WHERE name = '{sTable}';";
                    using (SQLiteCommand sqlitecmdCommand = new SQLiteCommand(sReset, sqliteConnection))
                    {
                        sqlitecmdCommand.ExecuteNonQuery();
                    }
                }


            }
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
                if(m_mruRecordsBase.ruRecords != null)
                {
                    m_mruRecordsBase.ruRecords.Clear();
                }


                //select all the files from the files_table
                //string sSQl = @"SELECT * FROM files_table";
                string sSQl = @"SELECT * FROM " + DataProcessingUtilities.SqlTables.sFilesTable;
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

                                int iID = 0;
                                for(int i = 0; i < sqlitereadReader.FieldCount; i++)
                                {
                                    string sColumnName = sqlitereadReader.GetName(i);
                                    string sValue = sqlitereadReader.IsDBNull(i) ? string.Empty : sqlitereadReader.GetValue(i).ToString();

                                    if(sColumnName != DataProcessingUtilities.SqlTables.sFilesTablePK)
                                    {
                                        odictColumnValues.Add(sColumnName, sValue);
                                    }
                                    else
                                    {
                                        iID = Convert.ToInt32(sqlitereadReader.GetValue(i)); //this is the PK
                                    }

                                   

                                }
                                //create a recordupdate for this specfic record (row)
                                RecordUpdate ruRecordUpdate = new RecordUpdate();
                                ruRecordUpdate.sPrimaryKeyColumn = DataProcessingUtilities.SqlTables.sFilesTablePK;
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
                    if(i == 0)
                    {
                        //this is the first row get the PK
                        dgvRow.Cells[i].Value = ruRecord.iId;
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

        internal static MultipleRecordUpdates BuildFileInformation()
        {
            //this should build a multiple record update of the file...
            //we have the projectID from the project we just added, file name is in the file path, we have the filepath, created date and last modified date should be todays date, version should be 1, class should be VisAssistDocument, and the reset we can leave empty...
            //get the active document 
            Visio.Document ovDoc = Globals.ThisAddIn.Application.ActiveDocument;
            string sFileName = ovDoc.Name;
            string sFilePath = ReturnFileStructurePath();

            sFilePath = sFilePath + sFileName;
            

            Dictionary<string, string> oDictFileValues = new Dictionary<string, string>();
            oDictFileValues.Add("ProjectID", "1");
            oDictFileValues.Add("FileName", sFileName);
            oDictFileValues.Add("FilePath", sFilePath);
            oDictFileValues.Add("CreatedDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            oDictFileValues.Add("LastModifiedDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            oDictFileValues.Add("Version", "1.0.0");
            oDictFileValues.Add("Class", "VisAssistDocument");

            RecordUpdate ruFileRecord = new RecordUpdate();
            ruFileRecord.sPrimaryKeyColumn = DataProcessingUtilities.SqlTables.sFilesTablePK;
            ruFileRecord.iId = DataProcessingUtilities.GetNextIdForTable(DataProcessingUtilities.SqlTables.sFilesTable);
            ruFileRecord.odictColumnValues = oDictFileValues;

            return new MultipleRecordUpdates(new List<RecordUpdate> { ruFileRecord });

        }
       /// <summary>
       /// this adds the visio file itself after opening a save file dialog box and saves it to where the user specifies
       /// this should be adpated to create the file based off of a template... builds the Master document
       /// I will also build another routine AddVisioSecondaryDocument that will do the same thing except will not have the cover pages....
       /// </summary>
       /// <param name="sClass"></param>
        internal static void AddVisioDocument(string sClass)
        {
            //open a save file dialog to ask user where they want to save the visio document that will be creatd 
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Title = "Save Visio Document";
                saveFileDialog.Filter = "Visio Files (*.vsdx)|*.vsdx|All Files (*.*)|*.*";
                saveFileDialog.DefaultExt = "vsdx";
                saveFileDialog.AddExtension = true;

                DialogResult result = saveFileDialog.ShowDialog();

                if (result == DialogResult.OK)
                {
                    string sFilePath = saveFileDialog.FileName;

                    // TODO: create and save the Visio document at filePath
                    //using the result create a new visio file...
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
                else
                {
                    // User cancelled the dialog
                    MessageBox.Show("Save operation cancelled.");
                }
            }
        } //this creates the  new visio file and saves it where the user specified...



        //FILE STRUCTURE HELPER FUNCTIONS
        public static string ReturnFileStructurePath()
        {
            try
            {
                // *** CHANGED: removed unused sLocalFolder and sFileStructureToReturn ***

                string sToFilePath = Globals.ThisAddIn.Application.ActiveDocument.Path;
                Visio.Document ovThisVisioDocument = Globals.ThisAddIn.Application.ActiveDocument;

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
