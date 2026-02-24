using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using VisAssistDatabaseBackEnd.Forms;
using VisAssistDatabaseBackEnd.Project_Manifest;
using VisAssistDatabaseBackEnd.ShapeUtilities;
using WindowsAPICodePack.Dialogs;
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

        public static Dictionary<string, string> m_dictProjectFiles = new Dictionary<string, string>();
        public static Dictionary<string, string> m_dictFilesOutsideProjectFolder = new Dictionary<string, string>();






        //CRUD ACTIONS/DATABASE WORK
        internal static void AddNewFile()
        {
            try
            {


                MultipleRecordUpdates oFileRecord = new MultipleRecordUpdates();
                //create a new visio file (it will either be classified as a class a or b depedning on which one the user wants...
                string sClass = ""; //this is dependent on which kind of file th user wants to add, but i believe in most cases this will be used to add a new secondary file to a project...
                                    //it is possible that the user wants to add a Master file 
                                    //for the class for now i am going to see if the current doc's file name contains Cover Pages and if it does we are creating a type b off of a type a so we are going to close the document 
                                    //but if we were creating a type b off of a type b we aren't going to close the current docuemnt....
                                    //check to see if our current document is assigned to a project before we continue....
                                    //we also need to account for when the user decides to create a class a or class b file so we also know which kind of document we should be creating...
                Visio.Document ovCurrentDocument = Globals.ThisAddIn.Application.ActiveDocument;
                if (ovCurrentDocument.Name.Contains("Cover Pages"))
                {
                    //we are creating a type b off of a type a
                    sClass = "Close"; //we will be using the current instance of visio and closing the document 
                }
                else
                {
                    sClass = "Open"; //we will be opening a new isntance of visio
                }

                Visio.Document ovDoc = FileUtilities.AddVisioDocument(sClass);

                if (ovDoc != null)
                {
                    Visio.Page ovPage = ovDoc.Pages[1]; //get the first page...
                    string sFilePath = ReturnFileStructurePath(ovDoc.Path);
                    string sFileName = ovDoc.Name;
                    sFilePath = sFilePath + sFileName;

                    //need to get the projectID of the db we want to add to
                    ProjectUtilities.GetProjectInfoFromDatabase();
                    string sProjectID = ProjectUtilities.m_mruRecordsBase.ruRecords[0].sId;

                    oFileRecord = AddFileToDatabase(ovDoc, sFilePath, sProjectID);
                    AddUserCellsToDocument(oFileRecord, ovDoc);
                    //we are adding a regular file...
                    ovDoc.DocumentSheet.Cells["User.Class"].Formula = "\"" + "File" + "\"";
                    //increase the filecount for the project
                    //get the project id from the document 




                    PageUtilities.AddUserCellsToPage(ovPage);
                    //The page contains the necessary info to move forward with AddPageToDatabase
                    PageUtilities.AddPageToDatabase(ovPage, "", "Visio");

                    FileUtilities.AdjustFileCountInDB(ovDoc);

                    //need to attach the doucment level events to the doucment we just created and opened...
                    VisAssistDatabaseBackEnd.VisioUtilities.VisioHelper.OnDocumentOpened(ovDoc, false);

                    ovDoc.SaveAs(sFilePath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in AddNewFile " + ex.Message, "VisAssist");
            }

        }

        internal static MultipleRecordUpdates AddFileToDatabase(Visio.Document ovDoc, string sFilePath, string sProjectID)
        {
            //builds up the file information based on the visio object and runs the sql to add to the db
            MultipleRecordUpdates oFileRecord = new MultipleRecordUpdates();
            try
            {

                oFileRecord = FileUtilities.BuildFileInformation(ovDoc, sFilePath, sProjectID);
                if (oFileRecord.ruRecords != null)
                {
                    DatabaseUtilities.BuildInsertSqlForMultipleRecords(DatabaseUtilities.SqlTables.FilesTable.sFilesTable, oFileRecord);


                    return oFileRecord;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in AddFileToDatabase " + ex.Message, "VisAssist");
            }
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
            try

            {

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

                        if (sColumnName != DatabaseUtilities.SqlTables.FilesTable.sFilesTablePK)
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
                    ruRecordUpdate.sPrimaryKeyColumn = DatabaseUtilities.SqlTables.FilesTable.sFilesTablePK;
                    ruRecordUpdate.sId = sPrimaryKeyValue;
                    ruRecordUpdate.odictColumnValues = oDictColumnValues;

                    lstRecordUpdate.Add(ruRecordUpdate);
                }

                //wrap all the records into a multiple recorsupdates object
                m_mruRecordsToCompare = new MultipleRecordUpdates(lstRecordUpdate);

                //compare the two record sets and build a new record set based on only the changes
                m_mruRecordsToUpdate = DatabaseUtilities.CompareDataForMultipleRecords(m_mruRecordsBase, m_mruRecordsToCompare);


                if (m_mruRecordsToUpdate.ruRecords.Count > 0)
                {
                    //there is a change
                    //build the update sql for the files_table
                    DatabaseUtilities.BuildUpdateSqlForMultipleRecords(DatabaseUtilities.SqlTables.FilesTable.sFilesTable, m_mruRecordsToUpdate);
                    //reset the base record set
                    FileUtilities.GetFileDataFromDatabase(filePropertiesForm);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in UpdateFile " + ex.Message, "VisAssist");
            }
        }
        internal static void DeleteFile(FilePropertiesForm filePropertiesForm)
        {
            //get the selected row in the filePropertiesForm.dgvFileData to determine which file to delete
            try
            {
                MultipleRecordUpdates mruRecords = GatherDeletionData(filePropertiesForm);

                DataGridViewRow dgvFirstRow = filePropertiesForm.dgvFileData.Rows[0];
                string sProjectID = dgvFirstRow.Cells["ProjectID"].Value.ToString();

                int iRecordCount = DatabaseUtilities.GetTableRecordCount(DatabaseUtilities.SqlTables.FilesTable.sFilesTable);

                if (iRecordCount > 1)
                {
                    if (mruRecords.ruRecords != null)
                    {
                        //go and actually delete the visio file itself 
                        foreach (RecordUpdate ruRecordUpdate in mruRecords.ruRecords)
                        {
                            string sFilePath = ruRecordUpdate.odictColumnValues["FilePath"];

                            //make sure the file to delete is not being used...
                            Visio.Document ovDoc = IsVisioFileOpen(Globals.ThisAddIn.Application, sFilePath);

                            if (ovDoc == null)
                            {
                                bool bIsFileLocked = IsFileLocked(sFilePath);
                                if (!bIsFileLocked)
                                {
                                    //the file is not locked we can safely delete it...
                                    if (System.IO.File.Exists(sFilePath))
                                    {
                                        System.IO.File.Delete(sFilePath);
                                        DeleteFileFromDatabase(mruRecords);

                                    }

                                }
                                else
                                {
                                    //the file is open in a different instance of visio 
                                    MessageBox.Show("Cannot delete this file as it is currently open.", "VisAssist");
                                }
                            }
                            else
                            {
                                //the file is currently open in our instance of visio
                                MessageBox.Show("Cannot delete this file as it is currently open.", "VisAssist");
                            }


                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in DeleteFile " + ex.Message, "VisAssist");
            }


        }

        internal static bool DeleteFileFromDatabase(MultipleRecordUpdates mruRecords)
        {
            bool bDisasociatedFile = true;
            try
            {

                Visio.Document ovDoc = Globals.ThisAddIn.Application.ActiveDocument;
                // Get the selected row

                // Disassociate the file meaning delete the record from the database
                DatabaseUtilities.BuildDeleteSqlForMultipleRecords(DatabaseUtilities.SqlTables.FilesTable.sFilesTable, mruRecords);

                FileUtilities.AdjustFileCountInDB(ovDoc);

                return bDisasociatedFile;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in DisassociateFile " + ex.Message, "VisAssist");
            }
            return bDisasociatedFile;
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
                string sDelete = "DELETE FROM " + DatabaseUtilities.SqlTables.FilesTable.sFilesTable + ";";

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

                string sSqlUpdate = "UPDATE " + DatabaseUtilities.SqlTables.ProjectTable.sProjectTable + " SET FileCount = 0 WHERE Id = @ProjectID";

                using (SQLiteCommand cmd = new SQLiteCommand(sSqlUpdate, sqliteconConnection))
                {
                    cmd.Parameters.AddWithValue("@ProjectID", 1); // set project id as 1...
                    cmd.ExecuteNonQuery();
                }
            }

        }
        internal static void UpdateFileName(string sFileName)
        {
            try
            {

                //save the current document using the sFileName
                Visio.Document ovDoc = Globals.ThisAddIn.Application.ActiveDocument;


                string sFileStructure = ReturnFileStructurePath(ovDoc.Path);
                string sFilePath = Path.Combine(sFileStructure, sFileName);

                string sOldFilePath = Path.Combine(sFileStructure, ovDoc.Name);



                ovDoc.SaveAs(sFilePath);
                //delete the old file...

                //close the docuemnt 
                ovDoc.Close();
                //then reopen the doc 
                //delete the old filepath
                if (System.IO.File.Exists(sOldFilePath))
                {
                    System.IO.File.Delete(sOldFilePath);
                }

                ovDoc = Globals.ThisAddIn.Application.Documents.Open(sFilePath);


                string sProjectID = ovDoc.DocumentSheet.Cells["User.ProjectID"].get_ResultStr(0);

                //update the file name in the database...
                //build up a mru to send to the build update..
                MultipleRecordUpdates mruRecord = BuildFileInformation(ovDoc, sFilePath, sProjectID);
                if (mruRecord.ruRecords != null)
                {
                    DatabaseUtilities.BuildUpdateSqlForMultipleRecords(DatabaseUtilities.SqlTables.FilesTable.sFilesTable, mruRecord);
                }


            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in UpdateFileName " + ex.Message, "VisAssist");
            }

        }



        //GATHERING DATA
       
        internal static MultipleRecordUpdates BuildFileInformation(Visio.Document ovDoc, string sFilePath, string sProjectGuid)
        {
            //this should build a multiple record update of the file...
            //we have the projectID from the project we just added, file name is in the file path, we have the filepath, created date and last modified date should be todays date, version should be 1, class should be VisAssistDocument, and the reset we can leave empty...
            //get the active document 

            //we are passing in the filepath because the docuemnt could be a temp doc if it is open in a different visio instance...
            RecordUpdate ruFileRecord = new RecordUpdate();
            MultipleRecordUpdates mruRecord = new MultipleRecordUpdates();
            try
            {

                string sFileName = Path.GetFileName(sFilePath);



                Dictionary<string, string> oDictFileValues = new Dictionary<string, string>();
                //oDictFileValues.Add("ProjectID", "1");
                oDictFileValues.Add("FileName", sFileName);
                oDictFileValues.Add("FilePath", sFilePath);
                //oDictFileValues.Add("CreatedDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                oDictFileValues.Add("LastModifiedDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                oDictFileValues.Add("Version", "1.0.0");
                oDictFileValues.Add("Class", "VisAssistDocument");
                


                ruFileRecord.sPrimaryKeyColumn = DatabaseUtilities.SqlTables.FilesTable.sFilesTablePK;
                string sProjectID = "";
                if (ovDoc.DocumentSheet.CellExists["User.ProjectID", 0] == -1)
                {

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
                    oDictFileValues["CreatedDate"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); //we are creating this for the first time
                }

                //add the NextWireColor and the NextWireenumber (get rid of Wire Prefix..)
                oDictFileValues.Add("NextWireNumber", "1");


                ruFileRecord.sId = sID;
                ruFileRecord.odictColumnValues = oDictFileValues;

                mruRecord = new MultipleRecordUpdates(new List<RecordUpdate> { ruFileRecord });
                return mruRecord;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in BuildFileInformation " + ex.Message, "VisAssist");
            }
            return mruRecord;

        }


        //SQL HELPERS
        internal static void AdjustFileCountInDB(Visio.Document ovDoc)
        {
            //sAdjustment will either be Increase or Decrease
            try
            {
                //Visio.Document ovDoc = Globals.ThisAddIn.Application.ActiveDocument;
                if (ovDoc != null)
                {
                    string sProjectID = ovDoc.DocumentSheet.Cells["User.ProjectID"].get_ResultStr(0);

                    using (SQLiteConnection sqliteConnection = new SQLiteConnection(DatabaseConfig.ConnectionString))
                    {
                        sqliteConnection.Open();

                        // 1️⃣ Get the number of files for this project
                        string sCountSql = "SELECT COUNT(*) FROM " + DatabaseUtilities.SqlTables.FilesTable.sFilesTable + " WHERE ProjectID = @ProjectID";
                        int iFileCount = 0;

                        using (SQLiteCommand countCmd = new SQLiteCommand(sCountSql, sqliteConnection))
                        {
                            countCmd.Parameters.AddWithValue("@ProjectID", sProjectID);
                            iFileCount = Convert.ToInt32(countCmd.ExecuteScalar());
                        }

                        // 2️⃣ Update the FileCount in project_table
                        string sUpdateSql = "UPDATE project_table SET FileCount = @FileCount WHERE ProjectId = @ProjectID";

                        using (SQLiteCommand updateCmd = new SQLiteCommand(sUpdateSql, sqliteConnection))
                        {
                            updateCmd.Parameters.AddWithValue("@FileCount", iFileCount);
                            updateCmd.Parameters.AddWithValue("@ProjectID", sProjectID);

                            updateCmd.ExecuteNonQuery();
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in AdjustFileCount " + ex.Message, "VisAssist");
            }
        }





        //VISIO DOCUMENTS
        internal static void AddLaunchFile(Visio.Document ovDoc, string sProjectID, string sVisAssistFolderPath)
        {
            try
            {


                //sFolder should be at the Begining of our VisAssist project
                //we also need to create the launchfile...create a new visio file and add the ProjectID to the docuemntshapesheet.
                Visio.Document ovLaunchDoc = ovDoc.Application.Documents.Add("");
                ovLaunchDoc.DocumentSheet.AddNamedRow((short)Visio.VisSectionIndices.visSectionUser, "ProjectID", 0);
                ovLaunchDoc.DocumentSheet.Cells["User.ProjectID"].Formula = "\"" + sProjectID + "\"";
                //add a user Cell for the class to be Launch
                ovLaunchDoc.DocumentSheet.AddNamedRow((short)Visio.VisSectionIndices.visSectionUser, "Class", 0);
                ovLaunchDoc.DocumentSheet.Cells["User.Class"].Formula = "\"" + "Launch" + "\"";


                string sLaunchFilePath = Path.Combine(sVisAssistFolderPath, "LaunchFile.vsdx");
                ovLaunchDoc.SaveAs(sLaunchFilePath);
                ovLaunchDoc.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in AddLaunchFile " + ex.Message, "VisAssist");
            }

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


            //now we want to pop open the nameform to ask the user what to name the new file then add it to the current project therefore we know wehre to put it...
            try
            {

                string sFileName = FileUtilities.GetFileNameFromForm("");

                if (sFileName != null && sFileName != "")
                {
                    //check to make sure that this file name doesn't already exist in the project...
                    string sFileNameToCheck = "Dwg - " + sFileName + ".vsdx";
                    bool bFileNameExists = FileUtilities.CheckIfFileNameExistInDB(sFileNameToCheck);

                    if (!bFileNameExists)
                    {


                        Visio.Document ovDoc = Globals.ThisAddIn.Application.ActiveDocument;

                        //get the path of where to save the new file 
                        //use the current docuemtn and get that file structure, then add the Dwg - sFileName.vsdx and save 
                        string sFileStructure = FileUtilities.ReturnFileStructurePath(ovDoc.Path);
                        string sFilePath = Path.Combine(sFileStructure, "Dwg - " + sFileName + ".vsdx");
                        //add a file in a new instance of visio

                        Visio.Application ovApp;
                        Visio.Document ovNewDoc;
                        if (sClass == "Open")
                        {
                            //create a new instance of visio...
                            ovApp = new Visio.Application();
                            ovApp.Visible = true; // make it visible
                        }
                        else
                        {
                            //we are using our current isntance of visio 
                            ovApp = Globals.ThisAddIn.Application;

                        }
                        ovNewDoc = ovApp.Documents.Add("");

                        // Anchor it to disk immediately and cleanly
                        const short visSaveAsNoPrompt = 0x40;
                        const short visSaveAsDontList = 0x200;
                        ovNewDoc.SaveAsEx(sFilePath, (short)(visSaveAsNoPrompt | visSaveAsDontList));


                        ovDoc.Save();
                        if (sClass == "Close")
                        {
                            ovDoc.Close();
                        }


                        return ovNewDoc;
                    }
                    else
                    {
                        MessageBox.Show("The file name: " + sFileName + " already exists in this project.");
                        return null;
                    }

                }
                return null;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in AddVisioDocument " + ex.Message, "VisAssist");
            }
            return null;

        }

        /// <summary>
        /// this adds the Cover Page Document visio file...
        /// </summary>
        /// <param name="sFilePath"></param>
        internal static Visio.Document AddCoverPageDocument(string sFilePath)
        {
            try
            {
                //this creates the cover page documents and calls is Dwg - Cover Pages.vsdx and saves it to the folder path...
                Visio.Application ovVisioApp;
                //if there are no documents open in our current instance of visio then open the new proejct in that but otherwise open a new instance of visio 
                if (Globals.ThisAddIn.Application.Documents.Count > 0) //-may need to account for stencils being open so instead of this would have to look for .vsdx drawings open...
                {
                    //open a new instance of visio 
                    ovVisioApp = new Visio.Application();
                }
                else
                {
                    ovVisioApp = Globals.ThisAddIn.Application;
                }

                Visio.Document ovDoc = ovVisioApp.Documents.Add("");

                //save it, close it and reopen so that the file doesn't end up in a dirty state
                //we won't need to do this once we add the templates because we do a file.copy and then open the new file...
                //we want to design wehre user chooses the template and we'll grab it from access (i think)
                ovDoc.SaveAs(sFilePath);

                ovDoc.Close();

                ovDoc = ovVisioApp.Documents.Open(sFilePath);

                //get the file name and set that to the database path...
                string sDirectoryPath = Path.GetDirectoryName(sFilePath); //get the path before the the file name
                sDirectoryPath = Path.GetDirectoryName(sDirectoryPath); //get the path before the hidden Project Files folder
                DatabaseConfig.DatabasePath = Path.Combine(sDirectoryPath, "DB", "VisAssistBackEnd.db");

                return ovDoc;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in AddCoverPageDocument " + ex.Message, "VisAssist");
                return null;
            }
        }

        internal static void AddUserCellsToDocument(MultipleRecordUpdates oFileRecord, Visio.Document ovDoc)
        {
            try
            {
                //Visio.Document ovDoc = Globals.ThisAddIn.Application.ActiveDocument;
                ovDoc.DocumentSheet.AddNamedRow((short)Visio.VisSectionIndices.visSectionUser, "ProjectID", 0);
                ovDoc.DocumentSheet.Cells["User.ProjectID"].Formula = "\"" + oFileRecord.ruRecords[0].odictColumnValues["ProjectID"] + "\"";

                ovDoc.DocumentSheet.AddNamedRow((short)Visio.VisSectionIndices.visSectionUser, "FileID", 0);
                //add the fileid from the record we just added to this cell..
                ovDoc.DocumentSheet.Cells["User.FileID"].Formula = "\"" + oFileRecord.ruRecords[0].sId + "\"";

                ovDoc.DocumentSheet.AddNamedRow((short)Visio.VisSectionIndices.visSectionUser, "CreatedDate", 0);
                ovDoc.DocumentSheet.Cells["User.CreatedDate"].Formula = "\"" + oFileRecord.ruRecords[0].odictColumnValues["CreatedDate"] + "\"";

                //add the class for the document (could be Launch, Cover Page, or File)..suject to change...
                ovDoc.DocumentSheet.AddNamedRow((short)Visio.VisSectionIndices.visSectionUser, "Class", 0);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in AddUserCellsToDocument " + ex.Message, "VisAssist");
            }
        }


        internal static void OpenFile(string sFileName, string sSource)
        {
            try
            {
                Visio.Document ovCurrentDoc = Globals.ThisAddIn.Application.ActiveDocument;
                if (ovCurrentDoc != null)
                {
                    string sCurrentDocName = ovCurrentDoc.Name;
                    if (sCurrentDocName == sFileName)
                    {
                        //they pressed the document they are currently on to open 
                        MessageBox.Show("You chose the current file that is open please pick a different file.", "VisAssist");
                        return;
                    }
                }
                //use the sFileName to get the file path from m_dictFiles..
                string sFilePath = m_dictProjectFiles[sFileName];

                if (System.IO.File.Exists(sFilePath))
                {
                    //open the file 
                    Visio.Application ovApp = Globals.ThisAddIn.Application;
                    Visio.Document ovDoc = null;


                    //we need to get the projectid from the database
                    //get the database path from the filepath...
                    string sProjectFolderPath = Path.GetDirectoryName(sFilePath).TrimEnd(Path.DirectorySeparatorChar);
                    string sVisAssistFolderPath = Path.GetDirectoryName(sProjectFolderPath).TrimEnd(Path.DirectorySeparatorChar);


                    //before we get the projectID from the db we need to bind the doucment to the db...
                    DatabaseConfig.BindToActiveDocument(sVisAssistFolderPath);
                    string sProjectID = ProjectUtilities.GetColumnInfoInProjectTableFromDatabase("ProjectID");


                    switch (sSource)
                    {
                        case "Launch":
                            {
                                //WILL NEED TO CHECK TO SEE IF THE FILE IS ALREADY IN USE...
                                ovDoc = IsVisioFileOpen(ovApp, sFilePath);
                                if (ovDoc != null)
                                {
                                    //the file is open already in our instance of visio so bring it forward..
                                    foreach (Visio.Window ovWindow in ovApp.Windows)
                                    {
                                        if (ovWindow.Document == ovDoc)
                                        {

                                            ovWindow.Activate();
                                            break;
                                        }
                                    }
                                }
                                else
                                {
                                    //our current application doens't have this file open
                                    //check to see if the file is used by a different application
                                    bool bIsFileLocked = IsFileLocked(sFilePath);
                                    if (bIsFileLocked)
                                    {
                                        //the file is open in another instance of visio sorry we can't open this 
                                        MessageBox.Show("Sorry this file is used by another application and cannot be opened at this time.", "VisAssist");
                                        //need to close the launch doc...
                                        ovCurrentDoc.Close();
                                        return;
                                    }
                                    else
                                    {
                                        //the file is  not opened and is not locked...
                                        ovDoc = ovApp.Documents.Open(sFilePath);
                                        //we are coming from the ribbon button open file from the launch file...
                                        // coming from the launch file so we want to close it...
                                        //loop through the documents in ovApp for the launchfile for this filepath...
                                        string sProjectFilePath = Path.GetDirectoryName(sFilePath).TrimEnd(Path.DirectorySeparatorChar);
                                        string sLaunchFilePath = Path.GetDirectoryName(sProjectFilePath.TrimEnd(Path.DirectorySeparatorChar)); //get the folder path before the hidden Project Files folder
                                        sLaunchFilePath = Path.Combine(sLaunchFilePath, "LaunchFile.vsdx");


                                        //close the launchfile doc
                                        //may also just be ovCurrentDoc...
                                        ovCurrentDoc.Close();
                                        //foreach (Visio.Document ovDocToCheck in ovApp.Documents)
                                        //{
                                        //    string sDocToCheckPath = FileUtilities.ReturnFileStructurePath(ovDocToCheck.Path);
                                        //    sDocToCheckPath = Path.Combine(sDocToCheckPath, ovDocToCheck.Name);

                                        //    if (sDocToCheckPath == sLaunchFilePath)
                                        //    {
                                        //        //this is the file we want to close
                                        //        ovDocToCheck.Save();
                                        //        ovDocToCheck.Close();
                                        //    }
                                        //}
                                    }
                                }

                                break;
                            }
                        case "Project":
                            {
                                ovDoc = IsVisioFileOpen(ovApp, sFilePath);
                                if (ovDoc != null)
                                {
                                    //the file is open alread in our instance of visio so bring it forward..
                                }
                                else
                                {
                                    //our current application doens't have this file open
                                    //check to see if the file is used by a different application
                                    bool bIsFileLocked = IsFileLocked(sFilePath);
                                    if (bIsFileLocked)
                                    {
                                        //the file is locked
                                        MessageBox.Show("Sorry this file is used by another application and cannot be opened at this time.", "VisAssist");
                                        return;
                                    }
                                    else
                                    {
                                        //we want to open the file in a new project if there is already a doucment open...
                                        if (Globals.ThisAddIn.Application.Documents.Count > 0) //may need to account for stencils being open...
                                        {
                                            Visio.Application ovNewApp = new Visio.Application();
                                            ovNewApp.Visible = true;

                                            //WILL NEED TO CHECK TO SEE IF THE FILE IS ALREADY IN USE...
                                            ovDoc = ovNewApp.Documents.Open(sFilePath);
                                        }
                                        else
                                        {
                                            ovDoc = ovApp.Documents.Open(sFilePath);
                                        }

                                    }

                                }




                                break;
                            }
                        case "File":
                            {
                                //WILL NEED TO CHECK TO SEE IF THE FILE IS ALREADY IN USE...
                                //we are coming from the ribbon button open file not from the launch file
                                //need to open a new instance of visio and open the file 

                                bool bIsFileLocked = IsFileLocked(sFilePath);
                                if (bIsFileLocked)
                                {
                                    //the file is locked
                                    MessageBox.Show("Sorry this file is used by another application and cannot be opened at this time.", "VisAssist");

                                    return;
                                }
                                else
                                {
                                    Visio.Application ovNewApp = new Visio.Application();
                                    ovNewApp.Visible = true;

                                    //WILL NEED TO CHECK TO SEE IF THE FILE IS ALREADY IN USE...
                                    ovDoc = ovNewApp.Documents.Open(sFilePath);
                                }

                                break;
                            }

                    }
                    sVisAssistFolderPath = FileUtilities.GetFolderPath(ovDoc);
                    DatabaseConfig.BindToActiveDocument(sVisAssistFolderPath);

                    //need to attach the doucment level events to the doucment we just created and opened...
                    VisAssistDatabaseBackEnd.VisioUtilities.VisioHelper.OnDocumentOpened(ovDoc, false);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in OpenFile " + ex.Message, "VisAssist");
            }
        }

        private static void UpdateFileIDs(Visio.Document ovDoc, string sDestFilePath, string sProjectID)
        {
            try
            {
                //DocumentLevel
                //i need to go through and upate the fileID and the page ids and shapes ids...
                string sNewFileID = GenerateFileID(sProjectID, sDestFilePath, DateTime.Now);
                if (ovDoc.DocumentSheet.CellExists["User.FileID", 0] == 0)
                {
                    ovDoc.DocumentSheet.AddNamedRow((short)Visio.VisSectionIndices.visSectionUser, "FileID", 0);

                }
                ovDoc.DocumentSheet.Cells["User.FileID"].Formula = VisioUtilities.Application.FormatStringForVisio(sNewFileID);
                if (ovDoc.DocumentSheet.CellExists["User.ProjectID", 0] == 0)
                {
                    ovDoc.DocumentSheet.AddNamedRow((short)Visio.VisSectionIndices.visSectionUser, "ProjectID", 0);

                }
                ovDoc.DocumentSheet.Cells["User.ProjectID"].Formula = VisioUtilities.Application.FormatStringForVisio(sProjectID);


                Dictionary<string, string> oDictWirePairIDs = new Dictionary<string, string>();
                foreach (Visio.Page ovPage in ovDoc.Pages)
                {
                    UpdatePageAndShapeIDs(ovPage, sProjectID, sNewFileID, ref oDictWirePairIDs);
                }




                //save the document with the new ids..
                ovDoc.Save();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in UpdateIDs " + ex.Message, "VisAssist");
            }
        }

        internal static void UpdatePageAndShapeIDs(Visio.Page ovPage, string sProjectID, string sNewFileID, ref  Dictionary<string, string> oDictWirePairIDs)
        {
            //Page Level
            try
            {
                string sNewPageID = PageUtilities.GeneratePageID(sProjectID, sNewFileID, ovPage.Name, DateTime.Now);
                if (ovPage.PageSheet.CellExists["User.PageID", 0] == 0)
                {
                    ovPage.PageSheet.AddNamedRow((short)Visio.VisSectionIndices.visSectionUser, "PageID", 0);

                }
                ovPage.PageSheet.Cells["User.PageID"].FormulaForceU = VisioUtilities.Application.FormatStringForVisio(sNewPageID);

                //Shape Level
                foreach (Visio.Shape ovShape in ovPage.Shapes)
                {
                    if (ovShape.CellExists["User.Class", 0] == -1)
                    {
                        //this is one of our shapes
                        string sNewShapeID = ShapesUtilities.GenerateShapeID(sProjectID, sNewFileID, sNewPageID, ovShape.Name, DateTime.Now);

                        ovShape.Cells["User.ShapeID"].FormulaForceU = VisioUtilities.Application.FormatStringForVisio(sNewShapeID);

                        //i also need to update the WirePairID...and then update it for its mate too...
                        string sCurrentWirePairID = ovShape.Cells["User.WirePairID"].get_ResultStr(0);
                        string sNewWirePairID = "";
                        //check to see if we have already update the wirepairid for this mate/pair...
                        if(oDictWirePairIDs.ContainsKey(sCurrentWirePairID))
                        {
                            sNewWirePairID = oDictWirePairIDs[sCurrentWirePairID];

                        }
                        else
                        {
                            //we haven't update this mate/pair yet
                            sNewWirePairID = WireUtilities.GenerateNewWirePairID(sProjectID, sNewFileID, sNewPageID, sNewShapeID, DateTime.Now);
                            oDictWirePairIDs.Add(sCurrentWirePairID, sNewWirePairID);
                        }
                        
                        ovShape.Cells["User.WirePairID"].FormulaForceU = VisioUtilities.Application.FormatStringForVisio(sNewWirePairID);


                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in UpdatePageAndShapeIDs " + ex.Message, "VisAssist");
            }
        }





        //COPY FILES
        internal static void WhichFileToCopy()
        {
            try
            {
                Visio.Application ovApp = Globals.ThisAddIn.Application;
                Visio.Document ovDoc = Globals.ThisAddIn.Application.ActiveDocument;

                //
                //string sFolderPath = ReturnFileStructurePath(ovDoc.Path);

                using (CommonOpenFileDialog folderdialog = new CommonOpenFileDialog())
                {
                    folderdialog.IsFolderPicker = true;
                    folderdialog.Title = "Select a VisAssist project";

                    if (folderdialog.ShowDialog() == CommonFileDialogResult.Ok)
                    {
                        string sVisAssistFolderPath = folderdialog.FileName;

                        bool bHasNecessaryFolders = FileUtilities.CheckIfSubFoldersExist(sVisAssistFolderPath);
                        //Open the FilesForm based on the project the user wants to find a file to copy...

                        if (bHasNecessaryFolders)
                        {


                            //make sure the folder/files the user wants to copy is apart of a good stable visassist project
                            ProjectManifest.CheckForManifestIntegrity(sVisAssistFolderPath);

                            //we are good we have the DB and the Project Files folder
                            bool bDBExists = FileUtilities.DoesDBFileExist(sVisAssistFolderPath);

                            if (bDBExists)
                            {

                                FileUtilities.PopulateProjectFilesDictionaryBasedOnDirectory(sVisAssistFolderPath);

                                //we also want to confirm that they 

                                //will need to add the launch file later if it didn't exist...once we've opened a file
                                FileUtilities.OpenFileForm("Copy"); //false we are not coming from the launch file...
                                //get the current doc again and save it...
                                ovDoc = Globals.ThisAddIn.Application.ActiveDocument;
                                ovDoc.Save();
                            }
                            folderdialog.Dispose();
                        }
                        else
                        {
                            //this is not a proper folder
                            MessageBox.Show("This is not a VisAssist folder.", "VisAssist");
                            WhichFileToCopy();
                        }


                    }
                    folderdialog.Dispose();
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in WhichFileToAssociate " + ex.Message, "VisAssist");
            }

        }

        internal static void CopyFile(FilesForm filesForm)
        {
            Visio.Document ovCurrentDoc = Globals.ThisAddIn.Application.ActiveDocument;
            string sVisAssistFolderPathOriginal = GetFolderPath(ovCurrentDoc);
            string sProjectFolderPathOriginal = Path.Combine(sVisAssistFolderPathOriginal, "Project Files");
            DataGridViewRow dgvSelectedRow = filesForm.dgvFiles.SelectedRows[0];
            string sOldFileName = dgvSelectedRow.Cells[0].Value?.ToString();

            string sExtractedFileName = FileUtilities.ExtractNameFromVisioFile(sOldFileName);

            string sNewFileName = FileUtilities.GetFileNameFromForm(sExtractedFileName);


            if (sNewFileName != "" && sNewFileName != null)
            {
                //need to add the Dwg - and the .vsdx on the file name
                //we are not opening the file we are going to copy it
                sNewFileName = FileUtilities.FormatFileName(sNewFileName);

                string sFilePath = m_dictProjectFiles[sOldFileName];

                // string sFolderPathOfOtherFile = Path.GetDirectoryName(sFilePath).TrimEnd(Path.DirectorySeparatorChar);

                string sDocName = OpenFilesToCopy(sFilePath, sNewFileName, sProjectFolderPathOriginal);
                if (sDocName == "")
                {
                    MessageBox.Show("Could not copy file " + sOldFileName, "VisAssist");
                }
                else
                {
                    MessageBox.Show("Successfully copied the file " + sDocName + ".", "VisAssist");
                }
            }
        }
        /// <summary>
        /// this checks to see how we need to open the document to copy and takes different steps to output a temporary file that will be updated and copied in AddCopiedFile
        /// </summary>
        /// <param name="sFilePath"></param>
        /// <param name="sFolderPathOfOtherFile"></param>
        /// <returns></returns>
        internal static string OpenFilesToCopy(string sFilePath, string sNewFileName, string sProjectFolderPathOriginal)
        {
            string sDocName = "";
            try
            {


                //RIGHT NOW I AM ONLY ASSOCIATING FILES THAT HAVE INFORMATION (ie have been in a database -they have the User.FileID... )
                Visio.Application ovApp = Globals.ThisAddIn.Application;
                string sFileName = Path.GetFileName(sFilePath);
                Visio.Document ovDoc = null;
                Visio.Document ovCurrentDoc = ovApp.ActiveDocument;
                string sFullFilePath = ReturnFileStructurePath(sFilePath);
                string sCurrentFilePath = ovApp.ActiveDocument.Path;
                sCurrentFilePath = ReturnFileStructurePath(ovApp.ActiveDocument.Path);
                sCurrentFilePath = Path.Combine(sCurrentFilePath, ovApp.ActiveDocument.Name);


                // Check if file is already open in THIS Visio instance
                ovDoc = IsVisioFileOpen(ovApp, sFullFilePath);
                //bool bCloseDocument = false;
                string sTempFilePath = "";
                string sTempFolder = "";
                string sTempFileName = "";
                // bool bDeleteTempFilePath = false;
                //string sDestFilePath = "";
                string sProjectID = ovCurrentDoc.DocumentSheet.Cells["User.ProjectID"].get_ResultStr(0);
                try
                {
                    if (ovDoc == null)

                    //the doc is null so that means it is not open in our current instance of visio
                    {
                        //if the doucment is null that means the file is not open yetce of visio that is not the current instance
                        if (!IsFileLocked(sFilePath))
                        {
                            //the visio file is not locked and not open in our current instance so we can safely copy from


                            //open the specified sFilePath given by the user
                            //before we opne the temporary doc we want to turn off events...
                            Globals.ThisAddIn.Application.EventsEnabled = 0;
                            Visio.Document ovDocToCopy = Globals.ThisAddIn.Application.Documents.OpenEx(sFilePath, (short)(Visio.VisOpenSaveArgs.visOpenHidden | Visio.VisOpenSaveArgs.visOpenRW));
                            //create the temporary file that we will add the new IDs into and move into the correct file structure
                            sTempFolder = Path.GetTempPath();
                            sTempFileName = ovDocToCopy.Name;
                            sTempFilePath = Path.Combine(sTempFolder, sTempFileName);
                            //make a copy from the file the user specified to the temporary space
                            System.IO.File.Copy(sFilePath, sTempFilePath, true);
                            //open the temporary document
                            Visio.Document ovTempDoc = Globals.ThisAddIn.Application.Documents.OpenEx(sTempFilePath, (short)Visio.VisOpenSaveArgs.visOpenRW);
                            //save and close the sFilePath that the use specified
                            ovDocToCopy.Save();
                            ovDocToCopy.Close();


                            //copy the file
                            sDocName = AddCopiedFile(sProjectFolderPathOriginal, sFilePath, sProjectID, ovTempDoc, sTempFilePath, sNewFileName);
                            //save and close the temp file now that we are done with it we can also trash it (we already used it to make a copy in copyfile)
                            ovTempDoc.Save();
                            ovTempDoc.Close();
                            System.IO.File.Delete(sTempFilePath);

                            //turn events back on 
                            Globals.ThisAddIn.Application.EventsEnabled = -1;
                        }
                        else
                        {
                            //the file is locked because it is open in another instance of visio
                            DialogResult result = MessageBox.Show("This document is open. Are all the edits saved to the file?", "VisAssist", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                            if (result == DialogResult.No)
                            {
                                return "";
                            }
                            else
                            {

                                sTempFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + "_" + sFileName);
                                System.IO.File.Copy(sFilePath, sTempFilePath, true);

                                //turn events off before opening the temp docs...
                                Globals.ThisAddIn.Application.EventsEnabled = 0;
                                //open the temporary doc instead of the sFilePath given by the user
                                Visio.Document ovDocToCopy = Globals.ThisAddIn.Application.Documents.OpenEx(sTempFilePath, (short)(Visio.VisOpenSaveArgs.visOpenHidden | Visio.VisOpenSaveArgs.visOpenRW));

                                Visio.Document ovNewDoc = null;
                                //create the temporary file that we will add the new IDs into and move into the correct file structure
                                sTempFolder = Path.GetTempPath();
                                sTempFileName = ovDocToCopy.Name;
                                sTempFilePath = Path.Combine(sTempFolder, sTempFileName);

                                //open the temporary document
                                Visio.Document ovTempDoc = Globals.ThisAddIn.Application.Documents.OpenEx(sTempFilePath, (short)(Visio.VisOpenSaveArgs.visOpenHidden | Visio.VisOpenSaveArgs.visOpenRW));

                                //the file is open in a different instance of visio so we need to make a copy of the file and associate the copied file...
                                //bAssociatedFile = AssociateFileOpenInDifferentVisioInstance(sDestFilePath, sFolderPath, sFileName, sFilePath, sTempFilePath);
                                sDocName = AddCopiedFile(sProjectFolderPathOriginal, sTempFilePath, sProjectID, ovTempDoc, sTempFilePath, sNewFileName);

                                ovTempDoc.Save();
                                ovTempDoc.Close();
                                System.IO.File.Delete(sTempFilePath);
                                ovCurrentDoc.Save();

                                //turn events back on 
                                Globals.ThisAddIn.Application.EventsEnabled = -1;
                            }
                        }


                    }
                    else
                    {
                        //the file is currently open in our isntance of visio, save the file first and then continue
                        ovDoc.Save();

                        //the ovDoc is not null so it is open in our current instance of visio
                        //create the temporary folder
                        sTempFolder = Path.GetTempPath();
                        sTempFileName = ovDoc.Name;
                        sTempFilePath = Path.Combine(sTempFolder, sTempFileName);
                        //make a copy to the temporary folder
                        System.IO.File.Copy(sFilePath, sTempFilePath, true);
                        //open the temporary document
                        //turn evnets off before opening the temp file...
                        Globals.ThisAddIn.Application.EventsEnabled = 0;
                        Visio.Document ovTempDoc = Globals.ThisAddIn.Application.Documents.OpenEx(sTempFilePath, (short)(Visio.VisOpenSaveArgs.visOpenHidden | Visio.VisOpenSaveArgs.visOpenRW));

                        //copy the file
                        sDocName = AddCopiedFile(sProjectFolderPathOriginal, sFilePath, sProjectID, ovTempDoc, sTempFilePath, sNewFileName); //we are associating a file that is already open

                        //save, close, and delete the temp file...
                        ovTempDoc.Save();
                        ovTempDoc.Close();
                        System.IO.File.Delete(sTempFilePath);// bAssociatedFile = AssociateFileOpenInOurVisioInstanceNew(ovDoc, sFolderPath, sFileName, sFilePath, sProjectID);

                        //turn events back on 
                        Globals.ThisAddIn.Application.EventsEnabled = -1;

                    }



                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error in OpenFilesToAssociate " + ex.Message, "VisAssist");
                }
                return sDocName;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in OpenFilesToAssociate " + ex.Message, "VisAssist");
            }
            finally
            {
                //turn events back on 
                Globals.ThisAddIn.Application.EventsEnabled = -1;
            }
            return sDocName;
        }

        /// <summary>
        /// this adds the new file to the database with the new ids as well as saves it to the correct file structure...
        /// </summary>
        /// <param name="sProjectFolderPath"></param>
        /// <param name="sFileName"></param>
        /// <param name="sFilePath"></param>
        /// <param name="sProjectID"></param>
        /// <param name="ovTempDoc"></param>
        /// <param name="sTempFilePath"></param>
        /// <returns></returns>
        private static string AddCopiedFile(string sProjectFolderPath, string sFilePath, string sProjectID, Visio.Document ovTempDoc, string sTempFilePath, string sNewFileName)
        {
            try
            {
                Visio.Document ovNewDoc = null;


                string sVisAssistFolderPath = Path.GetDirectoryName(sProjectFolderPath).TrimEnd(Path.DirectorySeparatorChar);
                DatabaseConfig.BindToActiveDocument(sVisAssistFolderPath);
                //close the original document to copyp because we are going to copy the ovTempDoc instead...
                string sDestFilePath = Path.Combine(sProjectFolderPath, sNewFileName);


                if (ovTempDoc != null)
                {

                    //update all the ids in the visio document
                    UpdateFileIDs(ovTempDoc, sDestFilePath, sProjectID);

                    //add the visio file to the database based on the new IDs/information in the document
                    MultipleRecordUpdates mruRecords = AddFileToDatabase(ovTempDoc, sDestFilePath, sProjectID);

                    //add the pages...
                    foreach (Visio.Page ovPage in ovTempDoc.Pages)
                    {
                        PageUtilities.AddPageToDatabase(ovPage, sProjectID, "Visio");

                        foreach (Visio.Shape ovShape in ovPage.Shapes)
                        {
                            if (ovShape.CellExists["User.Class", 0] == -1)
                            {
                                //this is one of our shapes 
                                string sClass = ovShape.Cells["User.Class"].get_ResultStr(0);
                                switch (sClass)
                                {
                                    case "TerminalBlock":
                                        {
                                            TerminalBlockUtilities.AddTerminalBlockToDatabase(ovShape);
                                            break;
                                        }
                                    case "SmartWire":
                                        {
                                            break;
                                        }
                                }
                            }
                        }
                    }

                    string sUniqueFilePath = "";
                    //get a uniquefilepath (in case there is a file named the same thing
                    sUniqueFilePath = GetUniqueFilePath(sDestFilePath);
                    if (sDestFilePath != sUniqueFilePath)
                    {
                        //if we needed to update the file name we also need to update the file name in the database...
                        string sUniqueFileName = Path.GetFileName(sUniqueFilePath);
                        mruRecords.ruRecords[0].odictColumnValues["FileName"] = sUniqueFileName;
                        mruRecords.ruRecords[0].odictColumnValues["FilePath"] = sUniqueFilePath;

                        if (mruRecords.ruRecords != null)
                        {
                            //run the update sql with the new proper information
                            DatabaseUtilities.BuildUpdateSqlForMultipleRecords(DatabaseUtilities.SqlTables.FilesTable.sFilesTable, mruRecords);
                        }
                    }

                    //make the final copy from the temporary file location to the destination file structure with the new name...
                    System.IO.File.Copy(sTempFilePath, sUniqueFilePath, true);

                    //open the document that we just made a copy of --not sure why or if we need to open it, we adjust the temp doc and then make a copy of that to the correct file structure...
                    ovNewDoc = Globals.ThisAddIn.Application.Documents.OpenEx(sUniqueFilePath, (short)Visio.VisOpenSaveArgs.visOpenHidden);

                    string sDocName = ovNewDoc.Name;
                    //save and close the document
                    ovNewDoc.SaveAs(sUniqueFilePath);
                    ovNewDoc.Close();


                    return sDocName;
                }

            }
            catch (Exception ex)
            {

                MessageBox.Show("Error in AssociateFile " + ex.Message, "VisAssist");
                return "";
            }
            return "";

        }



        //FORMS
        internal static void OpenFilePropertiesForm()
        {
            FilePropertiesForm oNewForm = new FilePropertiesForm();
            oNewForm.Display();
            oNewForm.ShowDialog();
        }
        internal static void OpenFileForm(string sSource)
        {
            FilesForm oNewFilesForm = new FilesForm();
            oNewFilesForm.Display(sSource);
            oNewFilesForm.ShowDialog();
        }


        internal static void PopulateFilePropertiesForm(FilePropertiesForm filePropertiesForm)
        {


            try
            {


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
            catch (Exception ex)
            {
                MessageBox.Show("Error in PopulateFilePropertiesForm " + ex.Message, "VisAssist");
            }

        }
        internal static void PopulateFilesForm(FilesForm filesForm)
        {
            filesForm.dgvFiles.Rows.Clear();
            //populate the dgvFiles with the file names in the oDictFiles 
            foreach (string sFileName in m_dictProjectFiles.Keys)
            {
                filesForm.dgvFiles.Rows.Add(sFileName);
            }
        }

        internal static string GetFileNameFromForm(string sCurrentName)
        {
            using (NameForm oForm = new NameForm())
            {
                oForm.Text = "File Name";
                oForm.PromptText = "File Name";
                oForm.txtName.Text = sCurrentName;

                if (oForm.ShowDialog() == DialogResult.OK)
                {
                    string sTrimmedName = oForm.sName?.Trim();
                    return sTrimmedName;
                }
            }
            return null;
        }

        internal static MultipleRecordUpdates GatherDeletionData(FilePropertiesForm filePropertiesForm)
        {
            MultipleRecordUpdates mruRecords = new MultipleRecordUpdates();
            try
            {

                DataGridViewSelectedRowCollection colSelectedRows = filePropertiesForm.dgvFileData.SelectedRows;
                // Build a list of RecordUpdate objects for each selected row
                List<RecordUpdate> lstRecordsToDelete = new List<RecordUpdate>();
                Dictionary<string, string> oDictColumnValues = new Dictionary<string, string>();
                foreach (DataGridViewRow dgvRow in colSelectedRows)
                {
                    string sFileID = dgvRow.Cells["FileID"].Value.ToString();
                    string sFilePath = dgvRow.Cells["FilePath"].Value.ToString();
                    oDictColumnValues.Add("FilePath", sFilePath);

                    RecordUpdate ruRecord = new RecordUpdate();
                    ruRecord.sPrimaryKeyColumn = DatabaseUtilities.SqlTables.FilesTable.sFilesTablePK;
                    ruRecord.sId = sFileID;
                    ruRecord.odictColumnValues = oDictColumnValues;

                    lstRecordsToDelete.Add(ruRecord);
                }

                mruRecords = new MultipleRecordUpdates(lstRecordsToDelete);

                return mruRecords;

            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in GatherDeletionData " + ex.Message, "VisAssist");
            }
            return mruRecords;
        }







        //CHECKS
        internal static bool CheckThatFilesExistInFolder()
        {

            //use m_mruRecordsBase and check all the records file path to make sure the file exists where it should 
            bool bCleanBaseRecords = false;
            try
            {


                List<RecordUpdate> lstFilesToDelete = new List<RecordUpdate>();
                foreach (RecordUpdate ruRecord in m_mruRecordsBase.ruRecords)
                {
                    string sFilePath = ruRecord.odictColumnValues["FilePath"].ToString();

                    if (!System.IO.File.Exists(sFilePath))
                    {
                        RecordUpdate ruRecordToDelete = new RecordUpdate();
                        ruRecordToDelete.sPrimaryKeyColumn = ruRecord.sPrimaryKeyColumn;
                        ruRecordToDelete.sId = ruRecord.sId;
                        ruRecordToDelete.odictColumnValues = ruRecord.odictColumnValues;


                        lstFilesToDelete.Add(ruRecordToDelete);
                    }
                }
                MultipleRecordUpdates mruRecordsToDeleted = new MultipleRecordUpdates(lstFilesToDelete);

                if (mruRecordsToDeleted.ruRecords.Count > 0)
                {
                    bCleanBaseRecords = true;
                    //we are going to disassociate the file..
                    DatabaseUtilities.BuildDeleteSqlForMultipleRecords(DatabaseUtilities.SqlTables.FilesTable.sFilesTable, mruRecordsToDeleted);

                    //we need to clean up our m_mruRecords again..
                    Visio.Document ovDoc = Globals.ThisAddIn.Application.ActiveDocument;
                    FileUtilities.AdjustFileCountInDB(ovDoc);

                    string sMessage = "The following files could not be found:\n\n" + string.Join("\n", lstFilesToDelete.Select(r => r.odictColumnValues["FilePath"])) + "\n\nThese files will be dissociated from the database";


                    MessageBox.Show(sMessage, "VisAssist");
                    return bCleanBaseRecords;
                }



                return bCleanBaseRecords;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in CheckThatFilesExistInFolder " + ex.Message, "VisAssist");
            }
            return bCleanBaseRecords;
        }

        internal static List<string> CheckThatFileExistsInDatabase()
        {
            //get the relative path after VisAssist and check if that file path exists in the db
            //if that path doesn't exist that means this file is not associated with the project...
            List<string> oListFilesDontExist = new List<string>();
            try
            {


                Visio.Document ovDoc = Globals.ThisAddIn.Application.ActiveDocument;
                if (ovDoc != null)
                {
                    string sVisAssistFolderPath = FileUtilities.GetFolderPath(ovDoc);


                    PopulateProjectFilesDictionaryBasedOnDirectory(sVisAssistFolderPath); //populates m_dictFiles

                    using (SQLiteConnection sqliteconConnection = new SQLiteConnection(DatabaseConfig.ConnectionString))
                    {
                        sqliteconConnection.Open();

                        string sSql = @"SELECT 1
                    FROM files_table
                    WHERE FileName = @FileName COLLATE NOCASE
                    LIMIT 1";

                        using (SQLiteCommand sqlitecmdCommand = new SQLiteCommand(sSql, sqliteconConnection))
                        {
                            foreach (KeyValuePair<string, string> kvp in m_dictProjectFiles)
                            {
                                string sFileName = kvp.Key;      //file name
                                string sFilePath = kvp.Value;    // full path 

                                sqlitecmdCommand.Parameters.Clear();
                                sqlitecmdCommand.Parameters.AddWithValue("@FileName", sFileName);

                                using (SQLiteDataReader reader = sqlitecmdCommand.ExecuteReader())
                                {
                                    bool bExists = reader.Read();

                                    if (!bExists)
                                    {
                                        // file does not exist in DB
                                        oListFilesDontExist.Add(sFileName);
                                    }
                                }
                            }
                        }


                    }
                    return oListFilesDontExist;
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in CheckThatFileExistsInDatabase " + ex.Message, "VisAssist");
            }
            return null;
        }



        internal static void CheckForLaunchFile(string sFolderPath)
        {
            //sFolderPath should be where the VisAssist Project starts...

            try
            {
                Visio.Document ovCurrentDocument = Globals.ThisAddIn.Application.ActiveDocument;
                if (ovCurrentDocument != null)
                {
                    //string sFolderPath = GetFolderPath(ovCurrentDocument);
                    string sProjectID = ovCurrentDocument.DocumentSheet.Cells["User.ProjectID"].get_ResultStr(0);

                    //check if the launch file for this project exists...
                    PopulateFilesOutsideProjectFilesFolderDictionaryBasedOnDirectory(sFolderPath);
                    if (m_dictFilesOutsideProjectFolder.Count == 1)
                    {
                        //a launch file exists, make sure it has the correct project id...
                        //open the launch file and check the projectID to make sure it matches sProjectID otherwise delete it and add the correct launch file
                        FileUtilities.CheckIfLaunchFileBelongsToProject(sProjectID, ovCurrentDocument);
                    }
                    else
                    {
                        //there is either no launch file or too many launch files
                        if (m_dictFilesOutsideProjectFolder.Count == 0)
                        {
                            //there is no launch file, go ahead and create it for the document 
                            FileUtilities.AddLaunchFile(ovCurrentDocument, sProjectID, sFolderPath);
                        }
                        else
                        {
                            if (m_dictFilesOutsideProjectFolder.Count > 1)
                            {
                                //we have too many launch files, need to delete them all and create a new one, (but make sure we can delete the old ones..
                                bool bOkToDeleteLaunchFiles = FileUtilities.CanWeDeleteAllLaunchFiles();
                                if (bOkToDeleteLaunchFiles)
                                {
                                    //we successfully have none of the launch files open now, so let's delete them all
                                    FileUtilities.DeleteAllLaunchFiles(); //go ahead and delete all the launch files in the oDictLaunchFiles
                                                                          //now add the correct launch file
                                    FileUtilities.AddLaunchFile(ovCurrentDocument, sProjectID, sFolderPath);
                                }

                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in CheckForLaunchFile " + ex.Message, "VisAssist");
            }
        }

        internal static bool CheckIfSubFoldersExist(string sFolderPath)
        {
            //the sFolderPath should point to the beginning of our VisAssist Project
            //ex: the path should end in VisAssist or VisAssist -1....
            try
            {
                string[] sSubFolders = Directory.GetDirectories(sFolderPath);
                string[] sSubFolderNames = sSubFolders.Select(f => Path.GetFileName(f)).ToArray();
                //there should be two subFolders: DB and ProjectFiles confirm this...
                bool bHasDBSubFolder = sSubFolderNames.Contains("DB", StringComparer.OrdinalIgnoreCase);
                bool bHasProjectFilesSubFolder = sSubFolderNames.Contains("Project Files", StringComparer.OrdinalIgnoreCase);

                if (bHasDBSubFolder && bHasProjectFilesSubFolder)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in DoesSubFoldersExist " + ex.Message, "VisAssist");
            }
            return false;
        }


        internal static bool CheckIfFileNameExistInDB(string sFileName)
        {
            bool bFileNameExists = false;
            try
            {
                Visio.Document ovDoc = Globals.ThisAddIn.Application.ActiveDocument;
                //check the DB if this sFileName has already been taken...


                using (SQLiteConnection sqliteconConnection = new SQLiteConnection(DatabaseConfig.ConnectionString))
                {
                    sqliteconConnection.Open();

                    string sSql = @"SELECT 1 
                    FROM files_table 
                    WHERE FileName = @FileName COLLATE NOCASE
                    LIMIT 1";

                    using (SQLiteCommand sqlitecmdCommand = new SQLiteCommand(sSql, sqliteconConnection))
                    {
                        sqlitecmdCommand.Parameters.AddWithValue("@FileName", sFileName);

                        using (SQLiteDataReader reader = sqlitecmdCommand.ExecuteReader())
                        {
                            bFileNameExists = reader.Read();
                            return bFileNameExists;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in DoesFileNameExist " + ex.Message, "VisAssist");
            }
            return bFileNameExists;
        }







        //HELPER FUNCTIONS 
        internal static bool IsFileLocked(string filePath)
        {
            try
            {
                using (FileStream stream = System.IO.File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
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

        internal static bool DoesDBFileExist(string sFolderPath)
        {
            try
            {
                //Visio.Document ovDoc = Globals.ThisAddIn.Application.ActiveDocument;
                // if (ovDoc != null)
                //{
                // string sFolderPath = ReturnFileStructurePath(ovDoc.Path);

                string sDBPath = Path.Combine(sFolderPath, "DB", "VisAssistBackEnd.db");

                if (System.IO.File.Exists(sDBPath))
                {
                    return true;
                }
                else
                {
                    return false;
                }


                // }
                //return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in DoesDBFileExist " + ex.Message, "VisAssist");
            }
            return false;
        }

        internal static bool IsFileAssignedToProject(Visio.Document ovDoc)
        {
            try
            {


                //check if the document has a User.ProjectID and if it is blank this means it is an orphan file (it has been disassociated from a project)
                string sProjectID = ovDoc.DocumentSheet.Cells["User.ProjectID"].get_ResultStr(0);

                if (sProjectID != "")
                {
                    return true; //it is assigned to a project
                }
                else
                {
                    return false; //it is not assigned to a project the projectid is a blank string
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in IsFileAssignedToProject " + ex.Message, "VisAssist");
            }
            return false;
        }
        internal static List<string> GetFileNamesInProject()
        {
            List<string> lstFileNames = new List<string>();
            try
            {


                //gather a list of all the file names in the project
                using (SQLiteConnection sqliteconConnection = new SQLiteConnection(DatabaseConfig.ConnectionString))
                {
                    sqliteconConnection.Open();
                    string sSql = @"SELECT FileName FROM files_table";
                    using (SQLiteCommand sqlitecmdCommand = new SQLiteCommand(sSql, sqliteconConnection))
                    {
                        using (SQLiteDataReader reader = sqlitecmdCommand.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                // Assuming FileName is a TEXT column
                                lstFileNames.Add(reader.GetString(0));
                                // or: reader["FileName"].ToString()
                            }
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show("Error in GetFileNamesInProject " + ex.Message, "VisAssist");
            }
            return lstFileNames;
        }


        internal static string ExtractNameFromVisioFile(string sCurrentName)
        {
            string sPrefix = "Dwg - ";
            string sSuffix = ".vsdx";

            int iStartIndex = sPrefix.Length;
            int iLength = sCurrentName.Length - sPrefix.Length - sSuffix.Length;

            string sReturnString = sCurrentName.Substring(iStartIndex, iLength).Trim();

            return sReturnString;
        }
        //this adds our prefix ("Dwg - ") and our extension (".vsdx")
        internal static string FormatFileName(string sNewFileName)
        {

            sNewFileName = "Dwg - " + sNewFileName;

            // Ensure extension
            sNewFileName += ".vsdx";

            return sNewFileName;
        }
        /// <summary>
        /// this returns the folder path that begins our project
        /// could be VisAssist, VisAssist - 1 .. and so on..
        /// </summary>
        /// <param name="ovDoc"></param>
        /// <returns></returns>
        internal static string GetFolderPath(Visio.Document ovDoc)
        {
            try
            {
                string sFolderPath = "";
                string sClass = ovDoc.DocumentSheet.Cells["User.Class"].get_ResultStr(0);

                if (sClass == "Launch")
                {
                    //this is the launch file ..access the db folder from here...
                    sFolderPath = FileUtilities.ReturnFileStructurePath(ovDoc.Path).TrimEnd(Path.DirectorySeparatorChar);
                }
                else
                {
                    //it is either File or Cover Page we can access the folder path to the db the same way
                    sFolderPath = FileUtilities.ReturnFileStructurePath(ovDoc.Path).TrimEnd(Path.DirectorySeparatorChar);
                    sFolderPath = Path.GetDirectoryName(sFolderPath); //get the path before the Project Files
                }

                return sFolderPath;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in GetFolderPath " + ex.Message, "VisAssist");
            }
            return "";
        }

        public static string GetUniqueFilePath(string sDestFilePath)
        {
            try
            {
                string sDirectory = Path.GetDirectoryName(sDestFilePath);
                string sFileNameWithoutExtension = Path.GetFileNameWithoutExtension(sDestFilePath);
                string sExtension = Path.GetExtension(sDestFilePath);

                int iCounter = 1;
                string sUniqueFilePath = sDestFilePath;

                do
                {
                    if (System.IO.File.Exists(sUniqueFilePath))
                    {
                        sUniqueFilePath = Path.Combine(sDirectory, $"{sFileNameWithoutExtension}-{iCounter}{sExtension}");
                        iCounter++;
                    }
                    else
                    {
                        break; // found a unique name
                    }
                } while (true);

                return sUniqueFilePath;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in GetUniqueFilePath " + ex.Message, "VisAssist");

            }
            return "";
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
                string sSQl = @"SELECT * FROM " + DatabaseUtilities.SqlTables.FilesTable.sFilesTable;
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

                                    if (sColumnName != DatabaseUtilities.SqlTables.FilesTable.sFilesTablePK)
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
                                ruRecordUpdate.sPrimaryKeyColumn = DatabaseUtilities.SqlTables.FilesTable.sFilesTablePK;
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


        internal static string GetColumnInfoInFilesTableFromDatabase(string sColumnName, string sFileID)
        {
            //this is usually going to get a mates id
            try
            {
                string sSpecificPiece = "";
                //use the dbPath which is the db file and open it and get the ProjectID from the project_table
                using (SQLiteConnection sqliteconConnection = new SQLiteConnection(DatabaseConfig.ConnectionString))
                {
                    //logging here
                    sqliteconConnection.Open();
                    string sSQL = $"SELECT [{sColumnName}] FROM [files_table] WHERE [FileID] = @Id LIMIT 1";

                    using (SQLiteCommand sqlcmdCommand = new SQLiteCommand(sSQL, sqliteconConnection))
                    {
                        sqlcmdCommand.Parameters.AddWithValue("@Id", sFileID);

                        using (SQLiteDataReader sqlitereadReader = sqlcmdCommand.ExecuteReader())
                        {
                            if (sqlitereadReader.Read())
                            {
                                // Safe retrieval of value as string
                                object dbValue = sqlitereadReader[sColumnName];
                                return dbValue == DBNull.Value ? "" : dbValue.ToString();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in GetColumnInfoInWirePairsTableFromDatabase " + ex.Message, "VisAssist");
            }
            return "";
        }


        /// <summary>
        /// this folder path should be at the root VisAssist ...
        /// </summary>
        /// <param name="sFolderPath"></param>
        internal static void PopulateProjectFilesDictionaryBasedOnDirectory(string sFolderPath)
        {
            string sProjectFolderPath = Path.Combine(sFolderPath, "Project Files");

            m_dictProjectFiles.Clear();
            //the db exists 
            //gather the files 

            m_dictProjectFiles =
      Directory.GetFiles(sProjectFolderPath)
          .Where(f =>
              !Path.GetFileName(f).StartsWith("~") &&
              !Path.GetExtension(f).Equals(".~vsdx", StringComparison.OrdinalIgnoreCase)
          )
          .ToDictionary(
              f => Path.GetFileName(f), // key
              f => f                     // value
          );


        }
        ///<summary>
        /// this folder path should be at the VisAssist folder
        /// </summary>
        /// <param name="sFolderPath"></param>
        internal static void PopulateFilesOutsideProjectFilesFolderDictionaryBasedOnDirectory(string sFolderPath)
        {
            m_dictFilesOutsideProjectFolder.Clear();

            m_dictFilesOutsideProjectFolder = Directory.GetFiles(sFolderPath)
          .Where(f =>
              !Path.GetFileName(f).StartsWith("~") &&
              !Path.GetExtension(f).Equals(".~vsdx", StringComparison.OrdinalIgnoreCase)
          )
          .ToDictionary(
              f => Path.GetFileName(f), // key
              f => f                     // value
          );
        }




        //LAUNCH FILE HELPER FUNCTIONS
        private static void DeleteAllLaunchFiles()
        {
            //loop through the oDictLaunchFiles and delete each file in their based on their path 
            foreach (KeyValuePair<string, string> kvp in m_dictFilesOutsideProjectFolder)
            {
                string fileName = kvp.Key;
                string filePath = kvp.Value;

                try
                {
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to delete launch file:\n{fileName}\n\n{ex.Message}", "VisAssist");
                }
            }
        }

        private static bool CanWeDeleteAllLaunchFiles()
        {
            //the launch files may be open if they are in a weird state where the user has launched visio but havn't clicked anything on the fileForm (still on launch file)

            try
            {


                //need to make sure that each file in oDictLaunch Files is either open in our instance or closed (not locked/open in another application)
                //if the file is open in our instance of visio save and close it...
                //if all the files pass this test return true
                Visio.Application ovApp = Globals.ThisAddIn.Application;
                Visio.Document ovDoc;
                foreach (KeyValuePair<string, string> kvp in m_dictFilesOutsideProjectFolder)
                {
                    string sFileName = kvp.Key;
                    string sFilePath = kvp.Value;

                    ovDoc = IsVisioFileOpen(ovApp, sFilePath);
                    if (ovDoc != null)
                    {

                        ovDoc.Save();
                        ovDoc.Close();
                    }
                    else
                    {
                        bool bIsFileLocked = IsFileLocked(sFilePath);
                        if (bIsFileLocked)
                        {
                            return false; //one of the files is locked...
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in CanWeDeleteAllLaunchFiles " + ex.Message, "VisAssist");
            }
            return false;
        }

        private static void CheckIfLaunchFileBelongsToProject(string sProjectID, Visio.Document ovDoc)
        {
            try
            {
                //we should only have one thing in the dictionary but we want to open that launch file and check to see if the projectID matches the sProjectID
                KeyValuePair<string, string> sLaunchFileItem = m_dictFilesOutsideProjectFolder.First();
                string sFilePath = sLaunchFileItem.Value.ToString();
                //need to check to see if the launch file is already open, or if it is locked..
                Visio.Document ovLaunchDoc = IsVisioFileOpen(Globals.ThisAddIn.Application, sFilePath);
                string sVisAssistFolderPath = FileUtilities.GetFolderPath(ovDoc);
                string sLaunchFilePath = Path.Combine(sVisAssistFolderPath, "LaunchFile.vsdx");
                string sProjectIDofLaunchDoc = "";


                if (ovLaunchDoc != null)
                {
                    //this is open in our instance check it...
                    sProjectIDofLaunchDoc = ovLaunchDoc.DocumentSheet.Cells["User.ProjectID"].get_ResultStr(0);

                    if (sProjectIDofLaunchDoc != sProjectID)
                    {
                        //these don't match we need to delete it and create a new one, but it is already open in our instance so we need to close it and then delete it and create a new one...
                        ovLaunchDoc.DocumentSheet.Cells["User.ProjectID"].Formula = "\"" + sProjectID + "\"";
                        ovLaunchDoc.Save();


                    }
                    else
                    {
                        // they are equal this is the correct launch file for the project, nothing to update
                        //no need to close or save becuase it was already open just leave it open...
                    }
                }
                else
                {
                    //the launch file isn't open in our instance 
                    bool bIsFileLocked = FileUtilities.IsFileLocked(sFilePath);
                    if (bIsFileLocked)
                    {
                        //the file is locked, we cannot proceed with deleting the launch file, not sure what we should do here at this time...
                    }
                    else
                    {
                        //the file is not locked, open it check the project id and then update if need be
                        //turn off events before opening and checking the launch file..
                        Globals.ThisAddIn.Application.EventsEnabled = 0;
                        ovLaunchDoc = Globals.ThisAddIn.Application.Documents.OpenEx(sFilePath, (short)(Visio.VisOpenSaveArgs.visOpenHidden | Visio.VisOpenSaveArgs.visOpenRW));
                        if (ovLaunchDoc.DocumentSheet.CellExists["User.ProjectID", 0] == -1)
                        {

                            sProjectIDofLaunchDoc = ovLaunchDoc.DocumentSheet.Cells["User.ProjectID"].get_ResultStr(0);
                            if (sProjectIDofLaunchDoc != sProjectID)
                            {
                                ovLaunchDoc.DocumentSheet.Cells["User.ProjectID"].Formula = "\"" + sProjectID + "\"";
                                ovLaunchDoc.Save();
                            }
                        }
                        else
                        {
                            //the user.projectId cell didn't exist...
                            ovLaunchDoc.DocumentSheet.AddNamedRow((short)Visio.VisSectionIndices.visSectionUser, "ProjectID", 0);
                            ovLaunchDoc.DocumentSheet.Cells["User.ProjectID"].Formula = "\"" + sProjectID + "\"";
                            ovLaunchDoc.Save();
                        }
                        //close the doucment when done looking at the projectID
                        ovLaunchDoc.Close();
                        //turn events back on 
                        Globals.ThisAddIn.Application.EventsEnabled = -1;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in CheckIfLaunchFileBelongsToProject " + ex.Message, "VisAssist");
            }
            finally
            {
                Globals.ThisAddIn.Application.EventsEnabled = -1;
            }
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
