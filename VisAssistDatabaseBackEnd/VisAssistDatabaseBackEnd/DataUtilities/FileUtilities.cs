using Microsoft.Win32;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Security.Cryptography;
using System.Security.Permissions;
using System.Text;
using System.Windows.Forms;
using VisAssistDatabaseBackEnd.Forms;
using WindowsAPICodePack.Dialogs;
using static System.Net.WebRequestMethods;
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

                Visio.Document ovDoc = AddVisioDocument(sClass);

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

                    //increase the filecount for the project
                    //get the project id from the document 




                    PageUtilities.AddUserCellsToPage(ovPage);
                    //The page contains the necessary info to move forward with AddPageToDatabase
                    PageUtilities.AddPageToDatabase(ovPage, "");

                    FileUtilities.AdjustFileCount(ovDoc);

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
            MultipleRecordUpdates oFileRecord = new MultipleRecordUpdates();
            try
            {

                oFileRecord = FileUtilities.BuildFileInformation(ovDoc, sFilePath, sProjectID);
                if (oFileRecord.ruRecords != null)
                {
                    DataProcessingUtilities.BuildInsertSqlForMultipleRecords(DataProcessingUtilities.SqlTables.FilesTable.sFilesTable, oFileRecord);

                    //increase the filecount for the proejct...
                   

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


                //MultipleRecordUpdates mruRecords = DisassociateFile(filePropertiesForm);
                MultipleRecordUpdates mruRecords = GatherDisassociationData(filePropertiesForm);

                DataGridViewRow dgvFirstRow = filePropertiesForm.dgvFileData.Rows[0];
                string sProjectID = dgvFirstRow.Cells["ProjectID"].Value.ToString();

                int iRecordCount = DataProcessingUtilities.GetTableRecordCount(DataProcessingUtilities.SqlTables.FilesTable.sFilesTable);

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
                                        DisassociateFile(mruRecords);
                                        FileUtilities.AdjustFileCount(ovDoc);
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





        internal static void WhichFileToAssociate()
        {
            try
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
                        bool bAssociatedFile = OpenFilesToAssociate(sFilePath, sFolderPath);
                        if (bAssociatedFile)
                        {
                            //save the current file
                            ovDoc.Save();
                            MessageBox.Show("Databases successfully associated!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            MessageBox.Show("You chose a document already associated with this project, please pick a different document.", "VisAssist");
                            WhichFileToAssociate();
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in WhichFileToAssociate " + ex.Message, "VisAssist");
            }
        }

        internal static bool OpenFilesToAssociate(string sFilePath, string sFolderPath)
        {
            bool bAssociatedFile = false;
            try
            {


                //RIGHT NOW I AM ONLY ASSOCIATING FILES THAT HAVE INFORMATION (ie have been in a database -they have the User.FileID... )
                Visio.Application ovApp = Globals.ThisAddIn.Application;
                string sFileName = Path.GetFileName(sFilePath);
                Visio.Document ovDoc = null;
                string sFullFilePath = ReturnFileStructurePath(sFilePath);
                string sCurrentFilePath = ovApp.ActiveDocument.Path;
                sCurrentFilePath = ReturnFileStructurePath(ovApp.ActiveDocument.Path);
                sCurrentFilePath = Path.Combine(sCurrentFilePath, ovApp.ActiveDocument.Name);

                //check to make sure the user didn't accidentally click associate the same file that we are currently on (they might have just fat fingered it)
                if (sCurrentFilePath == sFullFilePath)
                {
                    return bAssociatedFile;
                }

                


                //also need to check to see if the file the user picked already exists in the current docs project
                //i think this will require us to open the document and look at the file id and see if that file id already exists in the project...
                //i think the file name/path would be unreliable...

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

                        //check to see if it is locked meaning it could possibly be open in an instance of visio that is not the current instance
                        if (!IsFileLocked(sFilePath))
                        {

                            bCloseDocument = true;
                            //if the file is not locked(opened anywhere else) we can safelty open it
                            // ovDoc = ovApp.Documents.OpenEx(sFilePath, (short)(Visio.VisOpenSaveArgs.visOpenHidden | Visio.VisOpenSaveArgs.visOpenRO));
                            //the file is not open in our instance and not open in a different instance
                           bAssociatedFile = AssociateFileNotOpened(sFolderPath, sFileName, sFilePath);
                        }
                        else
                        {
                            //the file is locked (open in another instance of visio so we will make a copy of the file in a temp folder and open that file to associate...
                            bDeleteTempFilePath = true;
                            // Swallow the exception silently, create a temp copy instead
                            sTempFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + "_" + sFileName);
                            System.IO.File.Copy(sFilePath, sTempFilePath, true);

                            //open the temporary doc

                            //the file is open in a different instance of visio so we need to make a copy of the file and associate the copied file...
                           bAssociatedFile = AssociateFileOpenInDifferentVisioInstance(sDestFilePath, sFolderPath, sFileName, sFilePath, sTempFilePath);

                            Visio.Document ovCurrentDoc = ovApp.ActiveDocument;
                            ovCurrentDoc.Save();

                        }


                    }
                    else
                    {
                        //the doc is not null therefore is was open in our current instance of visio
                      bAssociatedFile = AssociateFileOpenInOurVisioInstance(ovDoc, sFolderPath, sFileName, sFilePath);
                    }



                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error in OpenFilesToAssociate " + ex.Message, "VisAssist");
                }
                return bAssociatedFile;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in OpenFilesToAssociate " + ex.Message, "VisAssist");
            }
            return false;
        }

        private static bool AssociateFileOpenInDifferentVisioInstance(string sDestFilePath, string sFolderPath, string sFileName, string sFilePath, string sTempFilePath)
        {
            Visio.Document ovDoc = Globals.ThisAddIn.Application.Documents.OpenEx(sTempFilePath, (short)(Visio.VisOpenSaveArgs.visOpenHidden | Visio.VisOpenSaveArgs.visOpenRW));
            try
            {
                if (ovDoc != null)
                {
                    //need to get the projectID of the db we want to add to
                    ProjectUtilities.GetProjectInfoFromDatabase();
                    string sProjectID = ProjectUtilities.m_mruRecordsBase.ruRecords[0].sId;


                    //before we add it to the database we need to check to see if it already exists...
                    string sFileID = ovDoc.DocumentSheet.Cells["User.FileID"].get_ResultStr(0);
                    bool bDoesRecordExist = DataProcessingUtilities.DoesRecordExist(DataProcessingUtilities.SqlTables.FilesTable.sFilesTable, sFileID);

                    if (bDoesRecordExist)
                    {
                        //this file/record already exists in the project

                        return false;
                    }

                    sDestFilePath = Path.Combine(sFolderPath, sFileName);

                    MultipleRecordUpdates mruRecords = AddFileToDatabase(ovDoc, sDestFilePath, sProjectID);
                    //ovDoc.DocumentSheet.Cells["User.ProjectID"].Formula = "\"" + sProjectID + "\"";

                    foreach (Visio.Page ovPage in ovDoc.Pages)
                    {
                        //this does NOT have sufficient data to move forward with AddPageToDatbase
                        //we need to pass in the correct project id (the file id and page id and everything downstream will stay the same)

                        PageUtilities.AddPageToDatabase(ovPage, sProjectID);
                    }
                    //will also need to put the work to add all the shapes on the page in the database....
                    // Copy the file to the new folder

                    string sUniqueFilePath = "";

                    //before we make a copy make sure that the sDesfilePath doesn't already exist and if it does we need to increment - 1 and so on...
                    //we need to copy from the temporary file...
                    sUniqueFilePath = GetUniqueFilePath(sDestFilePath);
                    if (sDestFilePath != sUniqueFilePath)
                    {
                        //we needed to upgrade the filename/filepath we need to update it in the database...
                        string sUniqueFileName = Path.GetFileName(sUniqueFilePath);
                        mruRecords.ruRecords[0].odictColumnValues["FileName"] = sUniqueFileName;
                        mruRecords.ruRecords[0].odictColumnValues["FilePath"] = sUniqueFilePath;

                        if (mruRecords.ruRecords != null)
                        {
                            DataProcessingUtilities.BuildUpdateSqlForMultipleRecords(DataProcessingUtilities.SqlTables.FilesTable.sFilesTable, mruRecords);
                        }
                    }
                    System.IO.File.Copy(sTempFilePath, sUniqueFilePath, true);



                    //need to open the file in the destination file path to add the project ID

                    //only open the file if we made a copy...
                    // ovNewDoc = Globals.ThisAddIn.Application.Documents.OpenEx(sUniqueFilePath, (short)Visio.VisOpenSaveArgs.visOpenHidden);

                    ovDoc.DocumentSheet.Cells["User.ProjectID"].FormulaU = "\"" + sProjectID + "\"";

                    
                    ovDoc.SaveAs(sUniqueFilePath);
                    ovDoc.Close(); 



                    return true;


                }
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in AssociateFile " + ex.Message, "VisAssist");
            }
            return false;
        }

        private static bool AssociateFileOpenInOurVisioInstance(Visio.Document ovDoc, string sFolderPath, string sFileName, string sFilePath)
        {
            try
            {
                Visio.Document ovNewDoc;
                if (ovDoc != null)
                {
                    //need to get the projectID of the db we want to add to
                    ProjectUtilities.GetProjectInfoFromDatabase();
                    string sProjectID = ProjectUtilities.m_mruRecordsBase.ruRecords[0].sId;


                    //before we add it to the database we need to check to see if it already exists...
                    string sFileID = ovDoc.DocumentSheet.Cells["User.FileID"].get_ResultStr(0);
                    bool bDoesRecordExist = DataProcessingUtilities.DoesRecordExist(DataProcessingUtilities.SqlTables.FilesTable.sFilesTable, sFileID);

                    if (bDoesRecordExist)
                    {
                        //this file/record already exists in the project

                        return false;
                    }

                    string sDestFilePath = Path.Combine(sFolderPath, sFileName);

                    MultipleRecordUpdates mruRecords = AddFileToDatabase(ovDoc, sDestFilePath, sProjectID);
                    //ovDoc.DocumentSheet.Cells["User.ProjectID"].Formula = "\"" + sProjectID + "\"";

                    foreach (Visio.Page ovPage in ovDoc.Pages)
                    {
                        //this does NOT have sufficient data to move forward with AddPageToDatbase
                        //we need to pass in the correct project id (the file id and page id and everything downstream will stay the same)

                        PageUtilities.AddPageToDatabase(ovPage, sProjectID);
                    }
                    //will also need to put the work to add all the shapes on the page in the database....
                    // Copy the file to the new folder

                    string sUniqueFilePath = "";
                    bool bCopiedDoc = false;

                    //we can copy from the given path...
                    // need to see if the file already exists in the location
                    if (sFilePath != sDestFilePath)
                    {
                        bCopiedDoc = true; //the file is not in the same location as the destination so we are going to make a copy of the file...
                        //before we make a copy make sure that the sDesfilePath doesn't already exist and if it does we need to increment - 1 and so on...
                        sUniqueFilePath = GetUniqueFilePath(sDestFilePath);
                        if (sDestFilePath != sUniqueFilePath)
                        {
                            //we needed to upgrade the filename/filepath we need to update it in the database...
                            string sUniqueFileName = Path.GetFileName(sUniqueFilePath);
                            mruRecords.ruRecords[0].odictColumnValues["FileName"] = sUniqueFileName;
                            mruRecords.ruRecords[0].odictColumnValues["FilePath"] = sUniqueFilePath;

                            if (mruRecords.ruRecords != null)
                            {
                                DataProcessingUtilities.BuildUpdateSqlForMultipleRecords(DataProcessingUtilities.SqlTables.FilesTable.sFilesTable, mruRecords);
                            }
                        }
                        System.IO.File.Copy(sFilePath, sUniqueFilePath, true); //not the same path

                        //the file path and the destination path are the same so we don't need to make a copy 
                        ovNewDoc = Globals.ThisAddIn.Application.Documents.OpenEx(sUniqueFilePath, (short)Visio.VisOpenSaveArgs.visOpenHidden);
                    }
                    else
                    {

                        ovNewDoc = ovDoc; //the doc that we want to edit it the one we just opened before this method...


                    }


                    //ovNewDoc.DocumentSheet.Cells["User.ProjectID"].FormulaU = "\"" + sProjectID + "\"";
                    Visio.Cell ovCell = ovNewDoc.DocumentSheet.Cells["User.ProjectID"];
                    ovCell.FormulaU = "\"" + sProjectID + "\"";


                    if (bCopiedDoc)
                    {
                        //    //we copied the doc so we want to save to the uniquefilepath
                        ovNewDoc.SaveAs(sUniqueFilePath);
                        ovNewDoc.Close(); //only close the document if we opened it...
                    }
                    else
                    {

                        ovDoc.Save(); //the document is already in the file location and was not already open...so save the file to the current filepath...
                                      // ovDoc.Close();
                    }


                    return true;


                }
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in AssociateFile " + ex.Message, "VisAssist");
            }
            return false;
        }

        private static bool AssociateFileNotOpened(string sFolderPath, string sFileName, string sFilePath)
        {
            //the visio file is not open at all
            //will need to check to see if the destination path is the same (do we make a copy of the file or not?)
            try
            {
                Visio.Document ovDoc = Globals.ThisAddIn.Application.Documents.OpenEx(sFilePath, (short)(Visio.VisOpenSaveArgs.visOpenHidden | Visio.VisOpenSaveArgs.visOpenRW));
                Visio.Document ovNewDoc = null;
                if (ovDoc != null)
                {
                    //need to get the projectID of the db we want to add to
                    ProjectUtilities.GetProjectInfoFromDatabase();
                    string sProjectID = ProjectUtilities.m_mruRecordsBase.ruRecords[0].sId;


                    //before we add it to the database we need to check to see if it already exists...
                    string sFileID = ovDoc.DocumentSheet.Cells["User.FileID"].get_ResultStr(0);
                    bool bDoesRecordExist = DataProcessingUtilities.DoesRecordExist(DataProcessingUtilities.SqlTables.FilesTable.sFilesTable, sFileID);

                    if (bDoesRecordExist)
                    {
                        //this file/record already exists in the project

                        return false;
                    }

                    string sDestFilePath = Path.Combine(sFolderPath, sFileName);

                    MultipleRecordUpdates mruRecords = AddFileToDatabase(ovDoc, sDestFilePath, sProjectID);
                    //ovDoc.DocumentSheet.Cells["User.ProjectID"].Formula = "\"" + sProjectID + "\"";

                    foreach (Visio.Page ovPage in ovDoc.Pages)
                    {
                        //this does NOT have sufficient data to move forward with AddPageToDatbase
                        //we need to pass in the correct project id (the file id and page id and everything downstream will stay the same)

                        PageUtilities.AddPageToDatabase(ovPage, sProjectID);
                    }
                    //will also need to put the work to add all the shapes on the page in the database....
                    // Copy the file to the new folder

                    string sUniqueFilePath = "";
                    bool bCopiedDoc = false;

                    //we can copy from the given path...
                    //need to see if the file already exists in the location 
                    if (sFilePath != sDestFilePath)
                    {
                        bCopiedDoc = true; //the file is not in the same location as the destination so we are going to make a copy of the file...
                        //before we make a copy make sure that the sDesfilePath doesn't already exist and if it does we need to increment - 1 and so on...
                        sUniqueFilePath = GetUniqueFilePath(sDestFilePath);
                        if (sDestFilePath != sUniqueFilePath)
                        {
                            //we needed to upgrade the filename/filepath we need to update it in the database...
                            string sUniqueFileName = Path.GetFileName(sUniqueFilePath);
                            mruRecords.ruRecords[0].odictColumnValues["FileName"] = sUniqueFileName;
                            mruRecords.ruRecords[0].odictColumnValues["FilePath"] = sUniqueFilePath;

                            if (mruRecords.ruRecords != null)
                            {
                                DataProcessingUtilities.BuildUpdateSqlForMultipleRecords(DataProcessingUtilities.SqlTables.FilesTable.sFilesTable, mruRecords);
                            }
                        }
                        System.IO.File.Copy(sFilePath, sUniqueFilePath, true); //not the same path

                        //the file path and the destination path are the same so we don't need to make a copy 
                        ovNewDoc = Globals.ThisAddIn.Application.Documents.OpenEx(sUniqueFilePath, (short)Visio.VisOpenSaveArgs.visOpenHidden);
                    }
                    else
                    {

                        ovNewDoc = ovDoc; //the doc that we want to edit it the one we just opened before this method...


                    }


                    //ovNewDoc.DocumentSheet.Cells["User.ProjectID"].FormulaU = "\"" + sProjectID + "\"";
                    Visio.Cell ovCell = ovNewDoc.DocumentSheet.Cells["User.ProjectID"];
                    ovCell.FormulaU = "\"" + sProjectID + "\"";


                    if (bCopiedDoc)
                    {
                        //we copied the doc so we want to save to the uniquefilepath
                        ovNewDoc.SaveAs(sUniqueFilePath);
                        ovNewDoc.Close(); //only close the document if we opened it...
                    }
                    else
                    {

                        ovDoc.Save(); //the document is already in the file location and was not already open...so save the file to the current filepath...
                        ovDoc.Close();
                    }

                    // ovDoc.Close();

                    return true;


                }
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in AssociateFile " + ex.Message, "VisAssist");
            }
            return false;
        }

        

        public static string GetUniqueFilePath(string sDestFilePath)
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

        internal static bool DisassociateFile(MultipleRecordUpdates mruRecords)
        {
            bool bDisasociatedFile = true;
            try
            {

                Visio.Document ovDoc = Globals.ThisAddIn.Application.ActiveDocument;
                // Get the selected row

                //based on the file path of the file to disassociate, open it and make clear the projectID
                bool bClearedProjectID = ProjectUtilities.ClearProjectID(mruRecords);
                if (bClearedProjectID)
                {
                    // Disassociate by deleting the record in the database
                    DataProcessingUtilities.BuildDeleteSqlForMultipleRecords(DataProcessingUtilities.SqlTables.FilesTable.sFilesTable, mruRecords);

                    FileUtilities.AdjustFileCount(ovDoc);


                }
                else
                {
                    //we are unable to disassociate the file because the file is open in a different instance of visio...
                    MessageBox.Show("Please close the file: " + mruRecords.ruRecords[0].odictColumnValues["FilePath"] + " in order to disassociate.");
                    bDisasociatedFile = false;
                }
                return bDisasociatedFile;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in DisassociateFile " + ex.Message, "VisAssist");
            }
            return bDisasociatedFile;
        }

        internal static MultipleRecordUpdates GatherDisassociationData(FilePropertiesForm filePropertiesForm)
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
                    ruRecord.sPrimaryKeyColumn = DataProcessingUtilities.SqlTables.FilesTable.sFilesTablePK;
                    ruRecord.sId = sFileID;
                    ruRecord.odictColumnValues = oDictColumnValues;

                    lstRecordsToDelete.Add(ruRecord);
                }

                mruRecords = new MultipleRecordUpdates(lstRecordsToDelete);

                return mruRecords;

            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in GatherDisassociationData " + ex.Message, "VisAssist");
            }
            return mruRecords;
        }

        /// <summary>
        /// this takes a file and makes it an orphan file by clearing out the ProjectID
        /// </summary>
        /// <param name="sFolderPath"></param>
        internal static void OrphanFile(string sFolderPath)
        {
            try
            {
                Visio.Document ovDoc = Globals.ThisAddIn.Application.ActiveDocument;
                string sProjectID = ovDoc.DocumentSheet.Cells["User.ProjectID"].get_ResultStr(0);
                Dictionary<string, string> oDictColumnValues = new Dictionary<string, string>();
                string sFilePath = Path.Combine(sFolderPath, ovDoc.Name);
                oDictColumnValues.Add("FilePath", sFilePath);
                //the database doesn't exist so let's clear the all the ids in this file, this should now be an orphaned file (doesn't have a project id but has the other ids)
                RecordUpdate record = new RecordUpdate();
                record.sPrimaryKeyColumn = DataProcessingUtilities.SqlTables.FilesTable.sFilesTablePK;
                record.sId = sProjectID;
                record.odictColumnValues = oDictColumnValues;

                MultipleRecordUpdates mruRecords = new MultipleRecordUpdates(new List<RecordUpdate> { record });
                ProjectUtilities.ClearProjectID(mruRecords);

                MessageBox.Show("This is an orphaned file, please associate it first.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in OrphanFile " + ex.Message, "VisAssist");
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


                ruFileRecord.sPrimaryKeyColumn = DataProcessingUtilities.SqlTables.FilesTable.sFilesTablePK;
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

                string sFileName = GetFileName("");

                if (sFileName != null && sFileName != "")
                {
                    //check to make sure that this file name doesn't already exist in the project...
                    string sFileNameToCheck = "Dwg - " + sFileName + ".vsdx";
                    bool bFileNameExists = FileUtilities.DoesFileNameExist(sFileNameToCheck);

                    if (!bFileNameExists)
                    {


                        Visio.Document ovDoc = Globals.ThisAddIn.Application.ActiveDocument;

                        //get the path of where to save the new file 
                        //use the current docuemtn and get that file structure, then add the Dwg - sFileName.vsdx and save 
                        string sFileStructure = ReturnFileStructurePath(ovDoc.Path);
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

        } //this creates the  new visio file and saves it where the user specified...
        internal static void AddCoverPageDocument(string sFilePath)
        {
            try
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
                DatabaseConfig.DatabasePath = Path.Combine(sDirectoryPath, "DB", "VisAssistBackEnd.db");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in AddCoverPageDocument " + ex.Message, "VisAssist");
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
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in AddUserCellsToDocument " + ex.Message, "VisAssist");
            }
        }


        private static bool DoesFileNameExist(string sFileName)
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

        internal static string GetFileName(string sCurrentName)
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

        internal static void AdjustFileCount(Visio.Document ovDoc)
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
                        string sCountSql = "SELECT COUNT(*) FROM " + DataProcessingUtilities.SqlTables.FilesTable.sFilesTable + " WHERE ProjectID = @ProjectID";
                        int iFileCount = 0;

                        using (SQLiteCommand countCmd = new SQLiteCommand(sCountSql, sqliteConnection))
                        {
                            countCmd.Parameters.AddWithValue("@ProjectID", sProjectID);
                            iFileCount = Convert.ToInt32(countCmd.ExecuteScalar());
                        }

                        // 2️⃣ Update the FileCount in project_table
                        string sUpdateSql = "UPDATE project_table SET FileCount = @FileCount WHERE Id = @ProjectID";

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

        internal static bool CheckThatFilesExistInFolder()
        {

            //use m_mruRecordsBase and check all the records file path to make sure the file exists where it should 
            bool bCleanBaseRecords = false;
            try
            {


                List<RecordUpdate> lstFilesToDisassociate = new List<RecordUpdate>();
                foreach (RecordUpdate ruRecord in m_mruRecordsBase.ruRecords)
                {
                    string sFilePath = ruRecord.odictColumnValues["FilePath"].ToString();

                    if (!System.IO.File.Exists(sFilePath))
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
                    Visio.Document ovDoc = Globals.ThisAddIn.Application.ActiveDocument;
                    FileUtilities.AdjustFileCount(ovDoc);

                    string sMessage = "The following files could not be found:\n\n" + string.Join("\n", lstFilesToDisassociate.Select(r => r.odictColumnValues["FilePath"])) + "\n\nThese files will be dissociated from the database";


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
                    string sFileStructure = FileUtilities.ReturnFileStructurePath(ovDoc.Path);

                    string sDBPath = Path.Combine(sFileStructure, ovDoc.Name);

                    //loop thorugh each file in the sFileStructure folder check if the file name exists in the db...
                    //string[] sFiles = Directory.GetFiles(sFileStructure);
                    string[] sFiles = Directory.GetFiles(sFileStructure)
    .Where(f => !Path.GetFileName(f).StartsWith("~") &&
                !Path.GetExtension(f).Equals(".~vsdx", StringComparison.OrdinalIgnoreCase)).ToArray();

                    using (SQLiteConnection sqliteconConnection = new SQLiteConnection(DatabaseConfig.ConnectionString))
                    {
                        sqliteconConnection.Open();

                        string sSql = @"SELECT 1
                    FROM files_table
                    WHERE FileName = @FileName COLLATE NOCASE
                    LIMIT 1";

                        using (SQLiteCommand sqlitecmdCommand = new SQLiteCommand(sSql, sqliteconConnection))
                        {
                            foreach (string sFileNames in sFiles)
                            {
                                string fileName = Path.GetFileName(sFileNames);

                                sqlitecmdCommand.Parameters.Clear();
                                sqlitecmdCommand.Parameters.AddWithValue("@FileName", fileName);

                                using (SQLiteDataReader reader = sqlitecmdCommand.ExecuteReader())
                                {
                                    bool bExists = reader.Read(); // true if a record exists
                                    if (!bExists)
                                    {
                                        //this file doesn't exist
                                        oListFilesDontExist.Add(fileName);
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

                if (System.IO.File.Exists(sOldFilePath))
                {
                    System.IO.File.Delete(sOldFilePath);
                }


                string sProjectID = ovDoc.DocumentSheet.Cells["User.ProjectID"].get_ResultStr(0);

                //update the file name in the database...
                //build up a mru to send to the build update..
                MultipleRecordUpdates mruRecord = BuildFileInformation(ovDoc, sFilePath, sProjectID);
                if (mruRecord.ruRecords != null)
                {
                    DataProcessingUtilities.BuildUpdateSqlForMultipleRecords(DataProcessingUtilities.SqlTables.FilesTable.sFilesTable, mruRecord);
                }


            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in UpdateFileName " + ex.Message, "VisAssist");
            }

        }




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

        internal static bool DoesDBFileExist()
        {
            try
            {
                Visio.Document ovDoc = Globals.ThisAddIn.Application.ActiveDocument;
                if (ovDoc != null)
                {
                    string sFolderPath = ReturnFileStructurePath(ovDoc.Path);

                    string sDBPath = Path.Combine(sFolderPath, "DB", "VisAssistBackEnd.db");

                    if (System.IO.File.Exists(sDBPath))
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

        internal static void AssociateOrphanedFiles(Visio.Document ovDoc)
        {
            try
            {
                string sDBPath = FileUtilities.WhichProjectToAssociateOrphanedFile();

                if (sDBPath != null && sDBPath != "")
                {
                    //from DBPath get the path of the new file: 
                    string sFileStructure = Path.GetDirectoryName(sDBPath);
                    //get the file name of the curreent unassigned docuemnt 

                    string sFileName = ovDoc.Name;

                    string sDestinationFilePath = Path.Combine(sFileStructure, sFileName);

                    string sFilePath = FileUtilities.ReturnFileStructurePath(ovDoc.Path);
                    //string sFolderPath = Path.GetDirectoryName(sFilePath);
                    string sFilePathToCopy = Path.Combine(sFilePath, sFileName);


                    //need to bind the database the the document that is the target...
                    DatabaseConfig.BindToActiveDocument(sFileStructure);
                   bool bAssociatedFile = FileUtilities.AssociateOrphanedFile(ovDoc, sDestinationFilePath, sFileStructure, sFileName, sFilePathToCopy);
                    // FileUtilities.AssociateFile(ovDoc, sDestinationFilePath, sFileStructure, sFileName, false, sFilePathToCopy, "");
                    if(bAssociatedFile)
                    {
                        MessageBox.Show("Databases successfully associated!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in AssociateOrphanedFiles " + ex.Message, "VisAssist");
            }
        }

        private static bool AssociateOrphanedFile(Visio.Document ovOriginalDoc, string sDestFilePath, string sFolderPath, string sFileName, string sFilePathToCopy)
        {
            try
            {

                //need to get the projectID of the db we want to add to
                Visio.Document ovNewDoc = null;
                bool bCloseOriginalFile;
                ProjectUtilities.GetProjectInfoFromDatabase();
                string sProjectID = ProjectUtilities.m_mruRecordsBase.ruRecords[0].sId;


                //before we add it to the database we need to check to see if it already exists...
                string sFileID = ovOriginalDoc.DocumentSheet.Cells["User.FileID"].get_ResultStr(0);
                bool bDoesRecordExist = DataProcessingUtilities.DoesRecordExist(DataProcessingUtilities.SqlTables.FilesTable.sFilesTable, sFileID);

                if (bDoesRecordExist)
                {
                    //there is already a record with these IDs we need to go update them all before we associte this orphan file...
                    AssociateOrphanFileWithNewIDs(ovOriginalDoc, sDestFilePath, sFolderPath, sFileName, sFilePathToCopy);
                    
                   
                    return true;

                }
                else
                {
                    //the fileID doesn't exist in the db...we can go ahead and associate the file normally.
                    AssociateFileWithCurrentIDs(ovOriginalDoc, sDestFilePath, sFolderPath, sFileName, sFilePathToCopy);

                    return true;
                }

            }
            catch(Exception ex)
            {
                MessageBox.Show("Error in AssociateOrphanedFile " + ex.Message, "VisAssist");
            }
            return false;
        }

        private static void AssociateFileWithCurrentIDs(Visio.Document ovOriginalDoc, string sDestFilePath, string sFolderPath, string sFileName, string sFilePathToCopy)
        {
            try
            {
                Visio.Document ovNewDoc = null;
                bool bCloseOriginalFile;
                string sProjectID = ProjectUtilities.m_mruRecordsBase.ruRecords[0].sId;
                sDestFilePath = Path.Combine(sFolderPath, sFileName);

                MultipleRecordUpdates mruRecords = AddFileToDatabase(ovOriginalDoc, sDestFilePath, sProjectID);
                //ovDoc.DocumentSheet.Cells["User.ProjectID"].Formula = "\"" + sProjectID + "\"";

                foreach (Visio.Page ovPage in ovOriginalDoc.Pages)
                {
                    //this does NOT have sufficient data to move forward with AddPageToDatbase
                    //we need to pass in the correct project id (the file id and page id and everything downstream will stay the same)

                    PageUtilities.AddPageToDatabase(ovPage, sProjectID);
                }
                //will also need to put the work to add all the shapes on the page in the database....
                // Copy the file to the new folder

                string sUniqueFilePath = "";

                //we can copy from the given path...
                //need to see if the file already exists in the location 
                if (sFilePathToCopy != sDestFilePath)
                {
                    bCloseOriginalFile = true;
                    //before we make a copy make sure that the sDesfilePath doesn't already exist and if it does we need to increment - 1 and so on...
                    sUniqueFilePath = GetUniqueFilePath(sDestFilePath);
                    if (sDestFilePath != sUniqueFilePath)
                    {
                        //we needed to upgrade the filename/filepath we need to update it in the database...
                        string sUniqueFileName = Path.GetFileName(sUniqueFilePath);
                        mruRecords.ruRecords[0].odictColumnValues["FileName"] = sUniqueFileName;
                        mruRecords.ruRecords[0].odictColumnValues["FilePath"] = sUniqueFilePath;

                        if (mruRecords.ruRecords != null)
                        {
                            DataProcessingUtilities.BuildUpdateSqlForMultipleRecords(DataProcessingUtilities.SqlTables.FilesTable.sFilesTable, mruRecords);
                        }
                    }
                    System.IO.File.Copy(sFilePathToCopy, sUniqueFilePath, true); //not the same path

                    //open the document that we just made a copy of 
                    ovNewDoc = Globals.ThisAddIn.Application.Documents.OpenEx(sUniqueFilePath, (short)Visio.VisOpenSaveArgs.visOpenHidden);

                    sDestFilePath = sUniqueFilePath;

                }
                else
                {
                    bCloseOriginalFile = false;
                    //our current document is the file we are changing so we don't need to copy it...
                    ovNewDoc = ovOriginalDoc;
                }



                ovNewDoc.DocumentSheet.Cells["User.ProjectID"].FormulaU = "\"" + sProjectID + "\"";

                ovNewDoc.Save();
                if (bCloseOriginalFile)
                {
                    ovNewDoc.Close();
                    ovOriginalDoc.Close();

                    //now reopen the ovNewDoc...
                    Globals.ThisAddIn.Application.Documents.Open(sDestFilePath);
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show("Error in AssociateFileWithCurrentIDs " + ex.Message, "VisAssist");
            }
        }


        private static void AssociateOrphanFileWithNewIDs(Visio.Document ovOriginalDoc, string sDestFilePath, string sFolderPath, string sFileName, string sFilePathToCopy)
        {
            try
            {


                string sTempFolder = Path.GetTempPath();
                string sTempFileName = ovOriginalDoc.Name;

                string sTempFilePath = Path.Combine(sTempFolder, sTempFileName);
                System.IO.File.Copy(ovOriginalDoc.Name, sTempFilePath, true);
                Visio.Document ovTempDoc = Globals.ThisAddIn.Application.Documents.OpenEx(sTempFilePath, (short)Visio.VisOpenSaveArgs.visOpenRW);
                bool bCloseOriginalFile;
                Visio.Document ovNewDoc = null;
                // sFilePathToCopy = tempFilePath;
                string sProjectID = ProjectUtilities.m_mruRecordsBase.ruRecords[0].sId;

                UpdateIDs(ovTempDoc, sDestFilePath, sProjectID);
                //ok now we have ovTempDoc that has the correct inforamtion inside of it
                MultipleRecordUpdates mruRecords = AddFileToDatabase(ovTempDoc, sDestFilePath, sProjectID);

                foreach (Visio.Page ovPage in ovTempDoc.Pages)
                {
                    //this does NOT have sufficient data to move forward with AddPageToDatbase
                    //we need to pass in the correct project id (the file id and page id and everything downstream will stay the same)

                    PageUtilities.AddPageToDatabase(ovPage, sProjectID);
                }
                //will also need to put the work to add all the shapes on the page in the database....
                // Copy the file to the new folder

                string sUniqueFilePath = "";
                if (sFilePathToCopy != sDestFilePath)
                {
                    bCloseOriginalFile = true;
                    //before we make a copy make sure that the sDesfilePath doesn't already exist and if it does we need to increment - 1 and so on...
                    sUniqueFilePath = GetUniqueFilePath(sDestFilePath);
                    if (sDestFilePath != sUniqueFilePath)
                    {
                        //we needed to upgrade the filename/filepath we need to update it in the database...
                        string sUniqueFileName = Path.GetFileName(sUniqueFilePath);
                        mruRecords.ruRecords[0].odictColumnValues["FileName"] = sUniqueFileName;
                        mruRecords.ruRecords[0].odictColumnValues["FilePath"] = sUniqueFilePath;

                        if (mruRecords.ruRecords != null)
                        {
                            DataProcessingUtilities.BuildUpdateSqlForMultipleRecords(DataProcessingUtilities.SqlTables.FilesTable.sFilesTable, mruRecords);
                        }
                    }
                    System.IO.File.Copy(sTempFilePath, sUniqueFilePath, true); //not the same path

                    //close the tempfile and delete it too 
                    ovTempDoc.Save();
                    ovTempDoc.Close();
                    System.IO.File.Delete(sTempFilePath);
                    //open the document that we just made a copy of 
                    ovNewDoc = Globals.ThisAddIn.Application.Documents.OpenEx(sUniqueFilePath, (short)Visio.VisOpenSaveArgs.visOpenHidden);

                    sDestFilePath = sUniqueFilePath;

                }
                else
                {
                    bCloseOriginalFile = false;
                    //our current document is the file we are changing so we don't need to copy it...
                    ovNewDoc = ovOriginalDoc;
                }


                //save and close the original doc
                ovNewDoc.Save();
                if (bCloseOriginalFile)
                {
                    ovNewDoc.Close();
                    ovOriginalDoc.Close();

                    //now reopen the ovNewDoc...
                    Globals.ThisAddIn.Application.Documents.Open(sDestFilePath);
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show("Error in AssociateOrphanFileWithNewIDs " + ex.Message, "VisAssist");
            }
        }

        internal static string WhichProjectToAssociateOrphanedFile()
        {
            using (CommonOpenFileDialog folderdialog = new CommonOpenFileDialog())
            {
                folderdialog.IsFolderPicker = true;
                folderdialog.Title = "Select the db folder of the project you want to assign the file to";

                if (folderdialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    //get the folder that the user clicked it should be the DB folder
                    string sDBPath = folderdialog.FileName;

                    return sDBPath;
                }
            }
            return "";
        }


        private static void UpdateIDs(Visio.Document ovDoc, string sDestFilePath, string sProjectID)
        {
            try
            {


                //i need to go through and upate the fileID and the page ids and shapes ids...
                string sNewFileID = GenerateFileID(sProjectID, sDestFilePath, DateTime.Now);
                ovDoc.DocumentSheet.Cells["User.FileID"].Formula = "\"" + sNewFileID + "\"";

                ovDoc.DocumentSheet.Cells["User.ProjectID"].Formula = "\"" + sProjectID + "\"";
                foreach (Visio.Page ovPage in ovDoc.Pages)
                {
                    string sNewPageID = PageUtilities.GeneratePageID(sProjectID, sNewFileID, ovPage.Name, DateTime.Now);
                    ovPage.PageSheet.Cells["User.PageID"].Formula = "\"" + sNewPageID + "\"";
                    foreach (Visio.Shape ovShape in ovPage.Shapes)
                    {
                        //update the shapes id....
                    }
                }

                //save the document with the new ids..
                ovDoc.Save();
            }
            catch(Exception ex)
            {
                MessageBox.Show("Error in UpdateIDs " + ex.Message, "VisAssist");
            }
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
