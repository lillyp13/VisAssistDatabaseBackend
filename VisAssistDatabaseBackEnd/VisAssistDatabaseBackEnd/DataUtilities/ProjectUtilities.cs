using Microsoft.Office.Interop.Visio;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using VisAssistDatabaseBackEnd.Forms;
using VisAssistDatabaseBackEnd.Project_Manifest;
using WindowsAPICodePack.Dialogs;
using static VisAssistDatabaseBackEnd.DataUtilities.DatabaseUtilities;
using Visio = Microsoft.Office.Interop.Visio;

namespace VisAssistDatabaseBackEnd.DataUtilities
{
    internal class ProjectUtilities
    {
        // Project fields
        string sProjectName;
        DateTime dtCreatedDate; // creating the project information
        DateTime dtModifiedDate; // changing project information
        string sCustomerName;
        string sJobName;
        string sJobNumber;
        string sJobCity;
        string sJobState;
        string sJobStreetAddress1;
        string sJobStreetAddress2;
        string sJobZipCode;
        string sControlContractorName;
        string sControlContractorCity;
        string sControlContractorState;
        string sControlContractorStreetAddress1;
        string sControlContractorStreetAddress2;
        string sControlContractorZipCode;
        string sControlContractorPhone;
        string sControlContractorEmail;
        string sMechanicalEngineer;
        string sMechanicalContractor;
        string sDesignedBy;
        string sReviewedBy;
        int iFileCount;
        //static SQLiteConnection Connection = ConnectionsUtilities.Connection;

        string sFileNumberFormat;
        string sPageNumberFormat;

        // Constructor to initialize the object
        public ProjectUtilities(
            string projectName,
            string customerName)
        {
            sProjectName = projectName;
            sCustomerName = customerName;
            dtCreatedDate = DateTime.Now;
            dtModifiedDate = DateTime.Now;
            // You can initialize other fields as needed
        }

        public static Dictionary<string, string> m_dictProjectInfoBase = new Dictionary<string, string>();  //key is the column name
        public static Dictionary<string, string> m_dictProjectInfoToCompare = new Dictionary<string, string>();
        public static Dictionary<string, string> m_dictProjectInfoToUpdate = new Dictionary<string, string>();
        public static MultipleRecordUpdates m_mruRecordsBase = new MultipleRecordUpdates();
        public static MultipleRecordUpdates m_mruRecordsToCompare = new MultipleRecordUpdates();
        public static MultipleRecordUpdates m_mruRecordsToUpdate = new MultipleRecordUpdates();






        //CRUD Actions
        internal static void AddProjectInfo(ProjectPropertiesForm projectPropertiesForm, Visio.Document ovDoc)
        {
            //string sProjectTableName = "project_table";
            try
            {

                bool bFolderAlreadyExists = DatabaseUtilities.CheckForDatabaseDirectory(DatabaseConfig.DatabasePath);
                if (bFolderAlreadyExists)
                {
                    bool bDataBaseFileExists = System.IO.File.Exists(DatabaseConfig.DatabasePath);
                    if (bDataBaseFileExists)
                    {
                        bool bTableExists = DatabaseUtilities.DoesTableExist(DatabaseUtilities.SqlTables.ProjectTable.sProjectTable);

                        if (bTableExists)
                        {
                            //the table exists let's go add the project
                            bool bDoesProjectExist = DatabaseUtilities.DoesTableHaveAnyRecords(DatabaseUtilities.SqlTables.ProjectTable.sProjectTable);
                            if (!bDoesProjectExist)
                            {
                                //there is no record in the project_Table yet so let's go add it...
                                //we have the data the user wants to add in the projectPropertiesForm
                                m_dictProjectInfoToCompare.Clear(); //clear this before populating it in GatherProjectPropertiesInfo
                                ProjectUtilities.GatherProjectPropertiesInfoFromForm(projectPropertiesForm, ovDoc);


                                //if (m_dictProjectInfoToUpdate.Count > 0)
                                if (m_mruRecordsToCompare.ruRecords.Count > 0)
                                {

                                    DatabaseUtilities.BuildInsertSqlForMultipleRecords(DatabaseUtilities.SqlTables.ProjectTable.sProjectTable, m_mruRecordsToCompare);
                                    //DataProcessingUtilities.BuildInsertSqlForRecordDictionary(sTable, m_dictProjectInfoToUpdate);

                                    ProjectUtilities.GetProjectInfoFromDatabase(); //go and grab the data from the database to populate the m_dictProjectInfoBase

                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in AddProjectInfo " + ex.Message, "VisAssist");
            }
        }
        internal static void UpdateProjectInfo(ProjectPropertiesForm projectPropertiesForm)
        {
            try
            {
                Visio.Document ovDoc = Globals.ThisAddIn.Application.ActiveDocument;
                if (m_mruRecordsToCompare.ruRecords != null)
                {
                    m_mruRecordsToCompare.ruRecords.Clear();
                }


                ProjectUtilities.GatherProjectPropertiesInfoFromForm(projectPropertiesForm, ovDoc);

                m_mruRecordsToUpdate = DatabaseUtilities.CompareDataForMultipleRecords(m_mruRecordsBase, m_mruRecordsToCompare);

                if (m_mruRecordsToUpdate.ruRecords.Count > 0)
                {

                    DatabaseUtilities.BuildUpdateSqlForMultipleRecords(DatabaseUtilities.SqlTables.ProjectTable.sProjectTable, m_mruRecordsToUpdate);

                    ProjectUtilities.GetProjectInfoFromDatabase(); //go and grab the data from the database to populate the m_dictProjectInfoBase

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in UpdateProjectInfo " + ex.Message, "VisAssist");
            }
        }





        internal static void DeleteProjectInfo()
        {
            try
            {
                //delete all the records in the project_table
                using (SQLiteConnection sqliteConnection = new SQLiteConnection(DatabaseConfig.ConnectionString))
                {
                    sqliteConnection.Open();
                    //enable foreign key enforcemnt for this connection
                    using (SQLiteCommand sqlitcmdPragma = new SQLiteCommand("PRAGMA foreign_keys = ON;", sqliteConnection))
                    {
                        sqlitcmdPragma.ExecuteNonQuery();
                    }
                    // string sDelete = "DELETE FROM project_table;";
                    string sDelete = "DELETE FROM " + DatabaseUtilities.SqlTables.ProjectTable.sProjectTable + ";";
                    using (SQLiteCommand cmd = new SQLiteCommand(sDelete, sqliteConnection))
                    {
                        cmd.ExecuteNonQuery();
                    }





                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in DeleteProjectInfo " + ex.Message, "VisAssist");
            }
        }

        //HELPER FUNCTIONS

        internal static void AddNewProject(ProjectPropertiesForm projectPropertiesForm, string sFilePath)
        {
            try
            {

                string sProjectFolderPath = System.IO.Path.GetDirectoryName(sFilePath);
                string sVisAssistFolderPath = System.IO.Path.GetDirectoryName(sProjectFolderPath);
                string sFileName = System.IO.Path.GetFileName(sFilePath);
                // string sHiddenProjectFolder = Path.Combine(sVisAssistFolder, "Project Files", sFileName);
                //this needs to create the new visio file now and then we can add the database...
                Visio.Document ovDoc = FileUtilities.AddCoverPageDocument(sFilePath);

                Visio.Page ovPage = ovDoc.Pages[1];

                MultipleRecordUpdates oFileRecord = new MultipleRecordUpdates();
                //get the active docuent 
                // Visio.Document ovDoc = Globals.ThisAddIn.Application.ActiveDocument;
                // Visio.Page ovPage = Globals.ThisAddIn.Application.ActivePage;

                //we are adding a project for the first time create the database and the tables in it
                DatabaseUtilities.InitializeDatabase(DatabaseConfig.DatabasePath);
                //gather the information from the properties form to fill out the project information 
                ProjectUtilities.AddProjectInfo(projectPropertiesForm, ovDoc);

                //add the file to the database: builds the file recored and runs the sql to the database, also increases the file count...
                oFileRecord = FileUtilities.AddFileToDatabase(ovDoc, sFilePath, m_mruRecordsToCompare.ruRecords[0].sId);

                FileUtilities.AddUserCellsToDocument(oFileRecord, ovDoc);
                //because we are creating the CoverPageDocument we will populate the class with Cover Page
                ovDoc.DocumentSheet.Cells["User.Class"].Formula = "\"" + "Cover Page" + "\"";

                //this just adds stuff like the version and class, not sure what else needs to go to the page level right now
                PageUtilities.AddUserCellsToPage(ovPage);

                //THIS IS A BIT DIFFERENT BECAUSE WHEN WE ADD A NEW FILE/PROJECT WE ARE ADDING A FEW PAGES...THIS IS JUST SOME SET UP THAT IS NEEDED
                //need to build up the page reocrd and run the sql to the database
                //The page has sufficient data to move forward with AddPageToDatabase
                PageUtilities.AddPageToDatabase(ovPage, "","Visio");

                //after adding the necessary user cells save the document 
                ovDoc.SaveAs(sFilePath);

                //closing and reopening so we don't have any cache problems...
                ovDoc.Close();

                //// Reopen it so Visio refreshes its internal cache
                ovDoc = ovDoc.Application.Documents.Open(sFilePath);

                string sProjectID = m_mruRecordsToCompare.ruRecords[0].sId.ToString();

                FileUtilities.AddLaunchFile(ovDoc, sProjectID, sVisAssistFolderPath);

                //ovDoc.SaveAs(sFilePath);
                ovDoc.Saved = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in AddNewProject " + ex.Message, "VisAssist");
            }

        }

        internal static string AddProjectFileStructure()
        {
            try
            {

                //ask the user where to save the new VisAssist file structure
                using (CommonOpenFileDialog folderdialog = new CommonOpenFileDialog())
                {
                    folderdialog.IsFolderPicker = true;
                    folderdialog.Title = "Select a folder to create the VisAssist Project";

                    if (folderdialog.ShowDialog() == CommonFileDialogResult.Ok)
                    {
                        string sBasePath = folderdialog.FileName; // folder path

                        string sVisAssist = "VisAssist";
                        string sVisAssistFolderPath = System.IO.Path.Combine(sBasePath, sVisAssist);

                        // If VisAssist already exists, append -1, -2, -3...
                        int iCounter = 1;
                        while (Directory.Exists(sVisAssistFolderPath))
                        {
                            sVisAssistFolderPath = System.IO.Path.Combine(sBasePath, $"{sVisAssist}-{iCounter}");
                            iCounter++;
                        }

                        // Create the unique project folder
                        Directory.CreateDirectory(sVisAssistFolderPath);

                        //create a hidden directory for the Visio files...
                        string sProjectFolderPath = System.IO.Path.Combine(sVisAssistFolderPath, "Project Files");
                        Directory.CreateDirectory(sProjectFolderPath);
                        File.SetAttributes(sProjectFolderPath, File.GetAttributes(sProjectFolderPath) | FileAttributes.Hidden);

                        //build the visio file name using our general Cover Pages
                        string sClassAFilePath = System.IO.Path.Combine(sProjectFolderPath, "Dwg - Cover Pages.vsdx");

                        //now we need to create a hidden folder that will contain the database..
                        string sDbFolderPath = System.IO.Path.Combine(sVisAssistFolderPath, "DB");
                        Directory.CreateDirectory(sDbFolderPath);
                        File.SetAttributes(sDbFolderPath, File.GetAttributes(sDbFolderPath) | FileAttributes.Hidden);

                        // Bind to the database inside the hidden folder
                        DatabaseConfig.DatabasePath = System.IO.Path.Combine(sDbFolderPath, "VisAssistBackEnd.db");

                        folderdialog.Dispose();
                        return sClassAFilePath;
                    }
                    folderdialog.Dispose();
                }
            }
            catch (Exception ex)

            {
                MessageBox.Show("Error in AddProjectFileStructure " + ex.Message, "VisAssist");
            }
            return null;

        }

        internal static void DeleteProject()
        {
            try
            {


                // i want to delete the project entireley so i will see if i can delete the folder (if everything in it is closed...)
                //open a folder dialog box and have the user point to the folder they want to delete
                //try to delete it-if we can't catch the exception and tell the user hey you need to close all the files in that project before i delete the project...
                using (CommonOpenFileDialog folderdialog = new CommonOpenFileDialog())
                {
                    folderdialog.IsFolderPicker = true;
                    folderdialog.Title = "Select the VisAssist project folder to delete";

                    if (folderdialog.ShowDialog() == CommonFileDialogResult.Ok)
                    {
                        string sVisAssistFolderPath = folderdialog.FileName;

                        bool bHasNecessaryFolders = FileUtilities.CheckIfSubFoldersExist(sVisAssistFolderPath);

                        if (bHasNecessaryFolders)
                        {
                            ProjectManifest.CheckForManifestIntegrity(sVisAssistFolderPath);

                            try
                            {

                                bool bAllFilesUnlocked = true;


                                //check if ANY files in ANY of the Visassit subfolders are open, including the project files, the db, the launch file, json...
                                foreach (string sFilePath in Directory.GetFiles(sVisAssistFolderPath, "*", SearchOption.AllDirectories))
                                {
                                    bool bIsFileLocked = FileUtilities.IsFileLocked(sFilePath);
                                    if (bIsFileLocked)
                                    {
                                        bAllFilesUnlocked = false;
                                    }
                                }




                                //if this files name is VisAssistBackEnd.db delete this last if we were succesfully in deleting the other projects
                                // Attempt to delete entire project folder
                                if (bAllFilesUnlocked)
                                {
                                    //reset the attributes in order to successfully delete now that we know nothing is open...
                                    foreach (string sFile in Directory.GetFiles(sVisAssistFolderPath, "*", SearchOption.AllDirectories))
                                    {
                                        File.SetAttributes(sFile, FileAttributes.Normal);
                                    }


                                    foreach (string sDirectory in Directory.GetDirectories(sVisAssistFolderPath, "*", SearchOption.AllDirectories))
                                    {
                                        File.SetAttributes(sDirectory, FileAttributes.Normal);
                                    }


                                    File.SetAttributes(sVisAssistFolderPath, FileAttributes.Normal);

                                    Directory.Delete(sVisAssistFolderPath, true);
                                    MessageBox.Show("Project deleted successfully.", "VisAssist", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                }
                                else
                                {
                                    //a file in the folder is locked...
                                    MessageBox.Show("Unable to delete the project folder because the file is locked.\n\n" +
                                    "Please make sure all Visio documents and related files in this project are closed, then try again.",
                                    "VisAssist",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Warning
                                );
                                }

                                folderdialog.Dispose();


                            }

                            //add a few catches...
                            catch (IOException)
                            {
                                MessageBox.Show("Unable to delete the project folder.\n\n" +
                                    "Please make sure all Visio documents and related files in this project are closed, then try again.",
                                    "VisAssist",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Warning
                                );
                            }
                        }
                        else
                        {
                            MessageBox.Show("Please pick a VisAssist Project.", "VisAssist");
                            DeleteProject();
                        }

                    }
                    folderdialog.Dispose();
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show("Error in DeleteProject " + ex.Message, "VisAssist");
            }


        }

        internal static void GetProjectInfoFromDatabase()
        {
            try
            {
                //logging statement placeholder
                //RECORDS USING MUTLIPLE RECORD UPDATES
                List<RecordUpdate> lstRecords = new List<RecordUpdate>();


                string sId = ""; // default for "new project"
                Dictionary<string, string> odictColumnValues = new Dictionary<string, string>();

                // string sSql = @"SELECT * FROM project_table LIMIT 1";
                string sSql = @"SELECT * FROM " + DatabaseUtilities.SqlTables.ProjectTable.sProjectTable + " LIMIT 1";

                using (SQLiteConnection sqliteconConnection = new SQLiteConnection(DatabaseConfig.ConnectionString))
                {
                    sqliteconConnection.Open();

                    using (SQLiteCommand sqlitecmdCommand = new SQLiteCommand(sSql, sqliteconConnection))
                    {

                        using (SQLiteDataReader sqlitereadReader = sqlitecmdCommand.ExecuteReader())
                        {
                            if (sqlitereadReader.Read())
                            {
                                // Existing project
                                for (int i = 0; i < sqlitereadReader.FieldCount; i++)
                                {
                                    string sColumnName = sqlitereadReader.GetName(i);

                                    if (sColumnName.Equals(DatabaseUtilities.SqlTables.ProjectTable.sProjectTablePK, StringComparison.OrdinalIgnoreCase))
                                    {
                                        sId = sqlitereadReader.GetValue(i).ToString();
                                        continue; // PK not included in update dictionary
                                    }

                                    odictColumnValues[sColumnName] = sqlitereadReader.IsDBNull(i) ? null : sqlitereadReader.GetValue(i).ToString();
                                }
                            }
                            else
                            {
                                // No project exists → build empty record from schema
                                for (int i = 0; i < sqlitereadReader.FieldCount; i++)
                                {
                                    string sColumnName = sqlitereadReader.GetName(i);

                                    if (sColumnName.Equals(DatabaseUtilities.SqlTables.ProjectTable.sProjectTablePK, StringComparison.OrdinalIgnoreCase))
                                        continue;

                                    odictColumnValues[sColumnName] = null;
                                }
                            }
                        }
                    }
                }

                // Build RecordUpdate
                RecordUpdate ru = new RecordUpdate();
                ru.sPrimaryKeyColumn = DatabaseUtilities.SqlTables.ProjectTable.sProjectTablePK;
                ru.sId = sId;
                ru.odictColumnValues = odictColumnValues;

                lstRecords.Add(ru);

                // Store in MultipleRecordUpdates
                m_mruRecordsBase = new MultipleRecordUpdates(lstRecords);



            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in GetProjectInfoFromDatabase " + ex.Message, "ViAssist");
            }

        }
        internal static string GetColumnInfoInProjectTableFromDatabase(string sColumnName)
        {
            try
            {
                string sSpecificPiece = "";
                //use the dbPath which is the db file and open it and get the ProjectID from the project_table
                using (SQLiteConnection sqliteconConnection = new SQLiteConnection(DatabaseConfig.ConnectionString))
                {
                    //logging here
                    sqliteconConnection.Open();
                    string sSQL = "SELECT " + sColumnName + " FROM project_table LIMIT 1"; //get the only record in the proejct_table...

                    using (SQLiteCommand sqlcmdCommand = new SQLiteCommand(sSQL, sqliteconConnection))
                    {
                        using (SQLiteDataReader sqlitereadReader = sqlcmdCommand.ExecuteReader())
                        {
                            if (sqlitereadReader.Read())
                            {
                                sSpecificPiece = sqlitereadReader[sColumnName]?.ToString();
                                return sSpecificPiece;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in GetColumnInfoInProjectTableFromDatabase " + ex.Message, "VisAssist");
            }
            return "";
        }


        internal static string GenerateProjectID(string sVisAssistFolderPath, DateTime createdDate, string sProjectName)
        {
            //generates a unique ID for the Project
            //project: sDirectoryPath + "Dwg - Cover Pages" + project name and created date
            //file: projectID + filepath + created date
            //page: ProjectID + FileID + page name + created date

            string sProjectFolderPath = System.IO.Path.Combine(sVisAssistFolderPath, "Project Files");
            string sInput = sProjectFolderPath + "Dwg - Cover Pages.vsdx" + sProjectName + createdDate.ToString("yyyy-MM-dd HH:mm:ss"); // formatted
            using (SHA256 sha = SHA256.Create())
            {
                byte[] bytehashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(sInput));
                StringBuilder sb = new StringBuilder();
                foreach (byte b in bytehashBytes)
                {
                    sb.Append(b.ToString("x2")); // hex
                }

                return sb.ToString();
            }
        }


        //FORMS
        internal static void OpenProject()
        {
            //open a folder dialog for the user to choose a VisAssist Folder

            try
            {

                using (CommonOpenFileDialog folderdialog = new CommonOpenFileDialog())
                {
                    folderdialog.IsFolderPicker = true;
                    folderdialog.Title = "Select a folder to open the VisAssist project";

                    if (folderdialog.ShowDialog() == CommonFileDialogResult.Ok)
                    {
                        string sVisAssistFolderPath = folderdialog.FileName; // folder path
                        bool bHasNecessaryFolders = FileUtilities.CheckIfSubFoldersExist(sVisAssistFolderPath);


                        if (bHasNecessaryFolders)
                        {
                            //check the manifest...

                            ProjectManifest.CheckForManifestIntegrity(sVisAssistFolderPath);
                            //we are good we have the DB and the Project Files folder
                            bool bDBExists = FileUtilities.DoesDBFileExist(sVisAssistFolderPath);

                            if (bDBExists)
                            {

                                FileUtilities.PopulateProjectFilesDictionaryBasedOnDirectory(sVisAssistFolderPath);
                                FileUtilities.PopulateFilesOutsideProjectFilesFolderDictionaryBasedOnDirectory(sVisAssistFolderPath);
                                //will need to add the launch file later if it didn't exist...once we've opened a file
                                FileUtilities.OpenFileForm("Project");

                                FileUtilities.CheckForLaunchFile(sVisAssistFolderPath);

                            }
                            folderdialog.Dispose();
                        }
                        else
                        {
                            //this is not a proper folder
                            MessageBox.Show("This is not a VisAssist folder.", "VisAssist");
                            OpenProject();
                        }

                    }
                    folderdialog.Dispose();
                }

                //open the fileForm and populate it with the files in the project for the user to open 


            }
            catch (Exception ex)

            {
                MessageBox.Show("Error in OpenProject " + ex.Message, "VisAssist");
            }
        }
        internal static string GetProjectNameFromForm()
        {
            //gets the project name from the NameForm after asking the user what to name the Project
            using (NameForm oForm = new NameForm())
            {
                oForm.ControlBox = false;
                oForm.Text = "Project Name";
                oForm.PromptText = "Project Name";
                if (oForm.ShowDialog() == DialogResult.OK)
                {
                    string sTrimmedName = oForm.sName?.Trim();
                    return sTrimmedName;
                }
            }
            return null;
        }
        //this just goes through each text box on the form and builds up a dictionary based on the values on the form currently (so that we can compare with the values in the db)
        private static void GatherProjectPropertiesInfoFromForm(ProjectPropertiesForm projectPropertiesForm, Visio.Document ovDoc)
        {
            try
            {


                //this just creates the dictionary to compare...
                string sID = projectPropertiesForm.txtID.Text.TrimEnd();
                m_dictProjectInfoToCompare.Add("Id", sID);

                string sProjectName = projectPropertiesForm.txtProjectName.Text.TrimEnd();
                m_dictProjectInfoToCompare.Add("ProjectName", sProjectName);

                string sCustomerName = projectPropertiesForm.txtCustomerName.Text.TrimEnd();
                m_dictProjectInfoToCompare.Add("CustomerName", sCustomerName);

                string sCreatedDate = projectPropertiesForm.txtCreatedDate.Text.TrimEnd();
                m_dictProjectInfoToCompare.Add("CreatedDate", sCreatedDate);

                string sModifiedDate = projectPropertiesForm.txtLastModifiedDate.Text.TrimEnd();
                m_dictProjectInfoToCompare.Add("LastModifiedDate", sModifiedDate);

                string sJobName = projectPropertiesForm.txtJobName.Text.TrimEnd();
                m_dictProjectInfoToCompare.Add("JobName", sJobName);

                string sJobNumber = projectPropertiesForm.txtJobNumber.Text.TrimEnd();
                m_dictProjectInfoToCompare.Add("JobNumber", sJobNumber);

                string sJobCity = projectPropertiesForm.txtJobCity.Text.TrimEnd();
                m_dictProjectInfoToCompare.Add("JobCity", sJobCity);

                string sJobState = projectPropertiesForm.txtJobState.Text.TrimEnd();
                m_dictProjectInfoToCompare.Add("JobState", sJobState);

                string sJobStreetAddress1 = projectPropertiesForm.txtJobStreetAddress1.Text.TrimEnd();
                m_dictProjectInfoToCompare.Add("JobStreetAddress1", sJobStreetAddress1);

                string sJobStreetAddress2 = projectPropertiesForm.txtJobStreetAddress2.Text.TrimEnd();
                m_dictProjectInfoToCompare.Add("JobStreetAddress2", sJobStreetAddress2);

                string sJobZipCode = projectPropertiesForm.txtJobZipCode.Text.TrimEnd();
                m_dictProjectInfoToCompare.Add("JobZipCode", sJobZipCode);

                string sControlContractorName = projectPropertiesForm.txtControlContractorName.Text.TrimEnd();
                m_dictProjectInfoToCompare.Add("ControlContractorName", sControlContractorName);

                string sControlContractorCity = projectPropertiesForm.txtControlContractorCity.Text.TrimEnd();
                m_dictProjectInfoToCompare.Add("ControlContractorCity", sControlContractorCity);

                string sControlContractorState = projectPropertiesForm.txtControlContractorState.Text.TrimEnd();
                m_dictProjectInfoToCompare.Add("ControlContractorState", sControlContractorState);

                string sControlContractorStreetAdress1 = projectPropertiesForm.txtControlContractorStreetAddress1.Text.TrimEnd();
                m_dictProjectInfoToCompare.Add("ControlContractorStreetAddress1", sControlContractorStreetAdress1);

                string sControlContractorStreetAddress2 = projectPropertiesForm.txtControlContractorStreetAddress2.Text.TrimEnd();
                m_dictProjectInfoToCompare.Add("ControlContractorStreetAddress2", sControlContractorStreetAddress2);

                string sControlContractorZipCode = projectPropertiesForm.txtControlContractorZipCode.Text.TrimEnd();
                m_dictProjectInfoToCompare.Add("ControlContractorZipCode", sControlContractorZipCode);

                string sControlContractorPhone = projectPropertiesForm.txtControlContractorPhone.Text.TrimEnd();
                m_dictProjectInfoToCompare.Add("ControlContractorPhone", sControlContractorPhone);

                string sControlContractorEmail = projectPropertiesForm.txtControlContractorEmail.Text.TrimEnd();
                m_dictProjectInfoToCompare.Add("ControlContractorEmail", sControlContractorEmail);

                string sMechanicalEngineer = projectPropertiesForm.txtMechanicalEngineer.Text.TrimEnd();
                m_dictProjectInfoToCompare.Add("MechanicalEngineer", sMechanicalEngineer);

                string sMechanicalContractor = projectPropertiesForm.txtMechanicalContractor.Text.TrimEnd();
                m_dictProjectInfoToCompare.Add("MechanicalContractor", sMechanicalContractor);

                string sDesignedBy = projectPropertiesForm.txtDesignedBy.Text.TrimEnd();
                m_dictProjectInfoToCompare.Add("DesignedBy", sDesignedBy);

                string sReviwedBy = projectPropertiesForm.txtReviewBy.Text.TrimEnd();
                m_dictProjectInfoToCompare.Add("ReviewedBy", sReviwedBy);

                string sFileCount = projectPropertiesForm.txtFileCount.Text.TrimEnd();
                m_dictProjectInfoToCompare.Add("FileCount", sFileCount);


                string sPrimarykey = "Id";

                // Build column dictionary (exclude Id)
                Dictionary<string, string> oDictToUpdate = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                foreach (KeyValuePair<string, string> sBaseItem in m_dictProjectInfoToCompare)
                {
                    if (!sBaseItem.Key.Equals(sPrimarykey, StringComparison.OrdinalIgnoreCase))
                    {
                        oDictToUpdate[sBaseItem.Key] = sBaseItem.Value;
                    }

                }

                // Single project, always Id = 1
                //depending on if the project already existed we either need to get the prjoect id or create the project id
                string sProjectID = "";
                if (ovDoc.DocumentSheet.CellExists["User.ProjectID", 0] == -1)
                {
                    sProjectID = ovDoc.DocumentSheet.Cells["User.ProjectID"].get_ResultStr(0);
                }

                if (sProjectID == "")
                {
                    //we are adding a project for the first time there isn't a projectId assigned yet...

                    string sProjectFolderPath = FileUtilities.ReturnFileStructurePath(ovDoc.Path).TrimEnd(System.IO.Path.DirectorySeparatorChar);
                    string sVisAssistFolderPath = System.IO.Path.GetDirectoryName(sProjectFolderPath);
                    //the created date doesn't exist yet...
                    DateTime dtCreatedDate = DateTime.Now;
                    oDictToUpdate["CreatedDate"] = dtCreatedDate.ToString("yyyy-MM-dd HH:mm:ss");
                    sProjectID = ProjectUtilities.GenerateProjectID(sVisAssistFolderPath, dtCreatedDate, m_dictProjectInfoToCompare["ProjectName"]);
                }


                RecordUpdate record = new RecordUpdate();
                record.sPrimaryKeyColumn = sPrimarykey;
                record.sId = sProjectID;
                record.odictColumnValues = oDictToUpdate;

                m_mruRecordsToCompare = new MultipleRecordUpdates(new List<RecordUpdate> { record });

            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in GatherProjectPropertiesInfo " + ex.Message, "VisAssist");
            }
        }

        internal static void OpenProjectForm(string sAction, string sProjectName, string sFilePath)
        {
            try
            {
                ProjectPropertiesForm oNewForm = new ProjectPropertiesForm();
                oNewForm.Display(sAction, sProjectName, sFilePath);
                //oNewForm.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in OpenProjectForm " + ex.Message, "VisAssist");
            }
        }

        internal static void PopulatePropertiesForm(ProjectPropertiesForm projectPropertiesForm)
        {
            try
            {
                //THIS IS USING MULTIPLE RECORD UPDATES
                if (m_mruRecordsToCompare.ruRecords != null)
                {
                    m_mruRecordsToCompare.ruRecords.Clear();
                }

                Dictionary<string, string> odictProjectInfo = m_mruRecordsBase.ruRecords[0].odictColumnValues;
                if (m_mruRecordsBase.ruRecords.Count > 0)
                {

                    //THIS IS USING A DICTIONARY

                    m_dictProjectInfoToCompare.Clear();

                    projectPropertiesForm.txtID.Text = m_mruRecordsBase.ruRecords[0].sId;


                    if (odictProjectInfo["ProjectName"] != "")
                    {
                        projectPropertiesForm.txtProjectName.Text = odictProjectInfo["ProjectName"].ToString();

                    }
                    else
                    {
                        projectPropertiesForm.txtProjectName.Text = "";
                    }
                    if (odictProjectInfo["CustomerName"] != null)
                    {
                        projectPropertiesForm.txtCustomerName.Text = odictProjectInfo["CustomerName"].ToString();

                    }
                    else
                    {
                        projectPropertiesForm.txtCustomerName.Text = "";
                    }
                    if (odictProjectInfo["CreatedDate"] != "")
                    {
                        projectPropertiesForm.txtCreatedDate.Text = odictProjectInfo["CreatedDate"].ToString();

                    }
                    else
                    {
                        projectPropertiesForm.txtCreatedDate.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                    }
                    if (odictProjectInfo["LastModifiedDate"] != "")
                    {
                        projectPropertiesForm.txtLastModifiedDate.Text = odictProjectInfo["LastModifiedDate"].ToString();

                    }
                    else
                    {
                        projectPropertiesForm.txtLastModifiedDate.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                    }
                    if (odictProjectInfo["JobName"] != null)
                    {
                        projectPropertiesForm.txtJobName.Text = odictProjectInfo["JobName"].ToString();

                    }
                    else
                    {
                        projectPropertiesForm.txtJobName.Text = "";
                    }
                    if (odictProjectInfo["JobNumber"] != null)
                    {
                        projectPropertiesForm.txtJobNumber.Text = odictProjectInfo["JobNumber"].ToString();

                    }
                    else
                    {

                        projectPropertiesForm.txtJobNumber.Text = "";
                    }
                    if (odictProjectInfo["JobCity"] != null)
                    {
                        projectPropertiesForm.txtJobCity.Text = odictProjectInfo["JobCity"].ToString();

                    }
                    else
                    {
                        projectPropertiesForm.txtJobCity.Text = "";
                    }
                    if (odictProjectInfo["JobState"] != null)
                    {
                        projectPropertiesForm.txtJobState.Text = odictProjectInfo["JobState"].ToString();

                    }
                    else
                    {
                        projectPropertiesForm.txtJobState.Text = "";
                    }
                    if (odictProjectInfo["JobStreetAddress1"] != null)
                    {
                        projectPropertiesForm.txtJobStreetAddress1.Text = odictProjectInfo["JobStreetAddress1"].ToString();

                    }
                    else
                    {
                        projectPropertiesForm.txtJobStreetAddress1.Text = "";
                    }
                    if (odictProjectInfo["JobStreetAddress2"] != null)
                    {
                        projectPropertiesForm.txtJobStreetAddress2.Text = odictProjectInfo["JobStreetAddress2"].ToString();

                    }
                    else
                    {
                        projectPropertiesForm.txtJobStreetAddress2.Text = "";
                    }
                    if (odictProjectInfo["JobZipCode"] != null)
                    {
                        projectPropertiesForm.txtJobZipCode.Text = odictProjectInfo["JobZipCode"].ToString();

                    }
                    else
                    {
                        projectPropertiesForm.txtJobZipCode.Text = "";
                    }
                    if (odictProjectInfo["ControlContractorName"] != null)
                    {
                        projectPropertiesForm.txtControlContractorName.Text = odictProjectInfo["ControlContractorName"].ToString();

                    }
                    else
                    {
                        projectPropertiesForm.txtControlContractorName.Text = "";
                    }
                    if (odictProjectInfo["ControlContractorCity"] != null)
                    {
                        projectPropertiesForm.txtControlContractorCity.Text = odictProjectInfo["ControlContractorCity"].ToString();

                    }
                    else
                    {
                        projectPropertiesForm.txtControlContractorCity.Text = "";
                    }
                    if (odictProjectInfo["ControlContractorState"] != null)
                    {
                        projectPropertiesForm.txtControlContractorState.Text = odictProjectInfo["ControlContractorState"].ToString();

                    }
                    else
                    {
                        projectPropertiesForm.txtControlContractorState.Text = "";
                    }
                    if (odictProjectInfo["ControlContractorStreetAddress1"] != null)
                    {
                        projectPropertiesForm.txtControlContractorStreetAddress1.Text = odictProjectInfo["ControlContractorStreetAddress1"].ToString();

                    }
                    else
                    {
                        projectPropertiesForm.txtControlContractorStreetAddress1.Text = "";
                    }
                    if (odictProjectInfo["ControlContractorStreetAddress2"] != null)
                    {
                        projectPropertiesForm.txtControlContractorStreetAddress2.Text = odictProjectInfo["ControlContractorStreetAddress2"].ToString();

                    }
                    else
                    {
                        projectPropertiesForm.txtControlContractorStreetAddress2.Text = "";
                    }
                    if (odictProjectInfo["ControlContractorZipCode"] != null)
                    {
                        projectPropertiesForm.txtControlContractorZipCode.Text = odictProjectInfo["ControlContractorZipCode"].ToString();

                    }
                    else
                    {
                        projectPropertiesForm.txtControlContractorZipCode.Text = "";
                    }
                    if (odictProjectInfo["ControlContractorPhone"] != null)
                    {
                        projectPropertiesForm.txtControlContractorPhone.Text = odictProjectInfo["ControlContractorPhone"].ToString();

                    }
                    else
                    {
                        projectPropertiesForm.txtControlContractorPhone.Text = "";
                    }
                    if (odictProjectInfo["ControlContractorEmail"] != null)
                    {
                        projectPropertiesForm.txtControlContractorEmail.Text = odictProjectInfo["ControlContractorEmail"].ToString();

                    }
                    else
                    {
                        projectPropertiesForm.txtControlContractorEmail.Text = "";
                    }
                    if (odictProjectInfo["MechanicalEngineer"] != null)
                    {
                        projectPropertiesForm.txtMechanicalEngineer.Text = odictProjectInfo["MechanicalEngineer"].ToString();

                    }
                    else
                    {
                        projectPropertiesForm.txtMechanicalEngineer.Text = "";
                    }
                    if (odictProjectInfo["MechanicalContractor"] != null)
                    {
                        projectPropertiesForm.txtMechanicalContractor.Text = odictProjectInfo["MechanicalContractor"].ToString();

                    }
                    else
                    {
                        projectPropertiesForm.txtMechanicalContractor.Text = "";
                    }
                    if (odictProjectInfo["DesignedBy"] != null)
                    {
                        projectPropertiesForm.txtDesignedBy.Text = odictProjectInfo["DesignedBy"].ToString();

                    }
                    else
                    {
                        projectPropertiesForm.txtDesignedBy.Text = "";
                    }
                    if (odictProjectInfo["ReviewedBy"] != null)
                    {
                        projectPropertiesForm.txtReviewBy.Text = odictProjectInfo["ReviewedBy"].ToString();

                    }
                    else
                    {
                        projectPropertiesForm.txtReviewBy.Text = "";
                    }
                    if (odictProjectInfo["FileCount"] != null)
                    {
                        projectPropertiesForm.txtFileCount.Text = odictProjectInfo["FileCount"].ToString();
                    }
                    else
                    {
                        projectPropertiesForm.txtFileCount.Text = "1"; //this will be the first file that gets added to the project when the user adds a project...
                    }

                }
                //}
                else
                {
                    MessageBox.Show("There are no records in the project_table");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in PopulatePropertiesForm " + ex.Message, "VisAssist");
            }

        }


    }
}

