using Microsoft.VisualStudio.Tools.Applications.Deployment;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Permissions;
using System.Text;
using System.Windows.Forms;
using VisAssistDatabaseBackEnd.Forms;
using WindowsAPICodePack.Dialogs;
using static VisAssistDatabaseBackEnd.DataUtilities.DataProcessingUtilities;
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


        //SEEDING
        internal static void AddProjectInfoSeeding()
        {
            //use the seed data and push that to the database
            //thhis adds the project info seed data
            //first check if the database file exists and then can continue 
            bool bFolderAlreadyExists = ConnectionsUtilities.CheckForDatabaseDirectory(DatabaseConfig.DatabasePath);

            if (bFolderAlreadyExists)
            {
                bool bDatabaseFileExists = System.IO.File.Exists(DatabaseConfig.DatabasePath);
                if (bDatabaseFileExists)
                {
                    bool bTableExists = DoesTableExist(DataProcessingUtilities.SqlTables.ProjectTable.sProjectTable);
                    if (bTableExists)
                    {
                        //only add the data if the project_table exists...
                        DatabaseSeeding.SeedProjects();
                    }
                    else
                    {
                        MessageBox.Show("Please add the database first: the table " + DataProcessingUtilities.SqlTables.ProjectTable.sProjectTable + " does not exist");
                    }
                }
                else
                {
                    MessageBox.Show("Please add the database first: " + DatabaseConfig.DatabasePath + " path does not exist");
                }
            }
            else
            {
                MessageBox.Show("Please add the directory first: " + DatabaseConfig.DatabasePath);
            }


        }//SEEDING




        //CRUD Actions
        internal static void AddProjectInfo(ProjectPropertiesForm projectPropertiesForm, Visio.Document ovDoc)
        {
            //string sProjectTableName = "project_table";


            bool bFolderAlreadyExists = ConnectionsUtilities.CheckForDatabaseDirectory(DatabaseConfig.DatabasePath);
            if (bFolderAlreadyExists)
            {
                bool bDataBaseFileExists = System.IO.File.Exists(DatabaseConfig.DatabasePath);
                if (bDataBaseFileExists)
                {
                    bool bTableExists = DoesTableExist(DataProcessingUtilities.SqlTables.ProjectTable.sProjectTable);

                    if (bTableExists)
                    {
                        //the table exists let's go add the project
                        bool bDoesProjectExist = DataProcessingUtilities.DoesTableHaveAnyRecords(DataProcessingUtilities.SqlTables.ProjectTable.sProjectTable);
                        if (!bDoesProjectExist)
                        {
                            //there is no record in the project_Table yet so let's go add it...
                            //we have the data the user wants to add in the projectPropertiesForm
                            m_dictProjectInfoToCompare.Clear(); //clear this before populating it in GatherProjectPropertiesInfo
                            ProjectUtilities.GatherProjectPropertiesInfo(projectPropertiesForm, ovDoc);


                            //if (m_dictProjectInfoToUpdate.Count > 0)
                            if (m_mruRecordsToCompare.ruRecords.Count > 0)
                            {

                                DataProcessingUtilities.BuildInsertSqlForMultipleRecords(DataProcessingUtilities.SqlTables.ProjectTable.sProjectTable, m_mruRecordsToCompare);
                                //DataProcessingUtilities.BuildInsertSqlForRecordDictionary(sTable, m_dictProjectInfoToUpdate);

                                ProjectUtilities.GetProjectInfoFromDatabase(); //go and grab the data from the database to populate the m_dictProjectInfoBase

                            }
                        }
                    }
                }
            }
        }

        //takes the information off the properties form to addd a new project
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
                    string sDelete = "DELETE FROM " + DataProcessingUtilities.SqlTables.ProjectTable.sProjectTable + ";";
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
        internal static void UpdateProjectInfo(ProjectPropertiesForm projectPropertiesForm)
        {
            try
            {
                Visio.Document ovDoc = Globals.ThisAddIn.Application.ActiveDocument;
                if (m_mruRecordsToCompare.ruRecords != null)
                {
                    m_mruRecordsToCompare.ruRecords.Clear();
                }


                ProjectUtilities.GatherProjectPropertiesInfo(projectPropertiesForm, ovDoc);

                m_mruRecordsToUpdate = DataProcessingUtilities.CompareDataForMultipleRecords(m_mruRecordsBase, m_mruRecordsToCompare);

                if (m_mruRecordsToUpdate.ruRecords.Count > 0)
                {

                    DataProcessingUtilities.BuildUpdateSqlForMultipleRecords(DataProcessingUtilities.SqlTables.ProjectTable.sProjectTable, m_mruRecordsToUpdate);

                    ProjectUtilities.GetProjectInfoFromDatabase(); //go and grab the data from the database to populate the m_dictProjectInfoBase

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in UpdateProjectInfo " + ex.Message, "VisAssist");
            }
        }


        internal static void AddNewProject(ProjectPropertiesForm projectPropertiesForm, string sFilePath)
        {
            //this needs to create the new visio file now and then we can add the database...
            FileUtilities.AddCoverPageDocument(sFilePath);


            MultipleRecordUpdates oFileRecord = new MultipleRecordUpdates();
            //get the active docuent 
            Visio.Document ovDoc = Globals.ThisAddIn.Application.ActiveDocument;
            Visio.Page ovPage = Globals.ThisAddIn.Application.ActivePage;

            //we are adding a project for the first time create the database and the tables in it
            ConnectionsUtilities.InitializeDatabase(DatabaseConfig.DatabasePath);
            //gather the information from the properties form to fill out the project information 
            ProjectUtilities.AddProjectInfo(projectPropertiesForm, ovDoc);
            //i need a record for the file that was created in Add

            sFilePath = FileUtilities.ReturnFileStructurePath(ovDoc.Path);
            string sFileName = ovDoc.Name;
            sFilePath = sFilePath + sFileName;


            //add the file to the database: builds the file recored and runs the sql to the database, also increases the file count...
            oFileRecord = FileUtilities.AddFileToDatabase(ovDoc, sFilePath, m_mruRecordsToCompare.ruRecords[0].sId);

            FileUtilities.AddUserCellsToDocument(oFileRecord, ovDoc);

            //this just adds stuff like the version and class, not sure what else needs to go to the page level right now
            PageUtilities.AddUserCellsToPage(ovPage);

            //THIS IS A BIT DIFFERENT BECAUSE WHEN WE ADD A NEW FILE/PROJECT WE ARE ADDING A FEW PAGES...THIS IS JUST SOME SET UP THAT IS NEEDED
            //need to build up the page reocrd and run the sql to the database
            //The page has sufficient data to move forward with AddPageToDatabase
            PageUtilities.AddPageToDatabase(ovPage, "");

            //after adding the necessary user cells save the document 
            ovDoc.SaveAs(sFilePath);

            //closing and reopening so we don't have any cache problems...
            ovDoc.Close();

            //// Reopen it so Visio refreshes its internal cache
            ovDoc = ovDoc.Application.Documents.Open(sFilePath);
            //ovDoc.SaveAs(sFilePath);
            ovDoc.Saved = true;


        }

        internal static string AddProjectFileStructure()
        {
            using (CommonOpenFileDialog folderdialog = new CommonOpenFileDialog())
            {
                folderdialog.IsFolderPicker = true;
                folderdialog.Title = "Select a folder to create the VisAssist project structure";

                if (folderdialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    string sBasePath = folderdialog.FileName; // folder path

                    string sVisAssist = "VisAssist";
                    string sProjectFolderPath = Path.Combine(sBasePath, sVisAssist);

                    // If VisAssist already exists, append -1, -2, -3, etc.
                    int iCounter = 1;
                    while (Directory.Exists(sProjectFolderPath))
                    {
                        sProjectFolderPath = Path.Combine(sBasePath, $"{sVisAssist}-{iCounter}");
                        iCounter++;
                    }

                    // Create the unique project folder
                    Directory.CreateDirectory(sProjectFolderPath);

                    string sClassAFilePath = Path.Combine(sProjectFolderPath, "Dwg - Cover Pages.vsdx");

                    //now we need to create a hidden folder that will contain the database.. put it in sProjectFolderPath -name it Dwg - sProjectName DB...
                  
                    string sDbFolderPath = Path.Combine(sProjectFolderPath, "DB");

                    // Create the folder
                    Directory.CreateDirectory(sDbFolderPath);

                    // Make it hidden
                    File.SetAttributes(sDbFolderPath, File.GetAttributes(sDbFolderPath) | FileAttributes.Hidden);

                    // Path to the database inside the hidden folder
                    DatabaseConfig.DatabasePath = Path.Combine(sDbFolderPath, "VisAssistBackEnd.db");

                    return sClassAFilePath;
                }
            }
            return null;

        }

        internal static string GetProjectName()
        {
            using (NameForm oForm = new NameForm())
            {
                oForm.ControlBox = false;
                oForm.Text = "Project Name";
                oForm.PromptText = "Project Name";
                if (oForm.ShowDialog() == DialogResult.OK)
                {
                    return oForm.sName;
                }
            }
            return null;
        }
       
        //Helper Functions

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
                    //we have m_lstProjectInfo 
                    //get the txtId textbox on the projectpropertiesform
                    //if (m_dictProjectInfoBase.Count > 0)
                    //{
                    //if(m_dictProjectInfoBase["Id"] != null)
                    //{
                    //    projectPropertiesForm.txtID.Text = m_dictProjectInfoBase["Id"].ToString();
                    //}
                    //else
                    //{
                    //    projectPropertiesForm.txtID.Text = "1"; //this will be the first and only record in the project
                    //}
                    //prefill the id with 1 because this will be our first project (this would not be on the form for the user to touch or mess with...)
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
                string sSql = @"SELECT * FROM " + DataProcessingUtilities.SqlTables.ProjectTable.sProjectTable + " LIMIT 1";

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

                                    if (sColumnName.Equals(DataProcessingUtilities.SqlTables.ProjectTable.sProjectTablePK, StringComparison.OrdinalIgnoreCase))
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

                                    if (sColumnName.Equals(DataProcessingUtilities.SqlTables.ProjectTable.sProjectTablePK, StringComparison.OrdinalIgnoreCase))
                                        continue;

                                    odictColumnValues[sColumnName] = null;
                                }
                            }
                        }
                    }
                }

                // Build RecordUpdate
                RecordUpdate ru = new RecordUpdate();
                ru.sPrimaryKeyColumn = DataProcessingUtilities.SqlTables.ProjectTable.sProjectTablePK;
                ru.sId = sId;
                ru.odictColumnValues = odictColumnValues;

                lstRecords.Add(ru);

                // Store in MultipleRecordUpdates
                m_mruRecordsBase = new MultipleRecordUpdates(lstRecords);




                //RECORDS USING DICTIONARY
                //m_dictProjectInfoBase.Clear();//this should be cleared because you build this when the user presses update
                //m_dictProjectInfoToUpdate.Clear();//this should be cleared because you build this when the user presses update

                //string sSQl = @"SELECT * FROM project_table WHERE Id = 1";
                ////logging statement placeholder
                //using (SQLiteConnection sqliteconConnection = new SQLiteConnection(Connection))
                //{
                //    //logging statement placeholder
                //    sqliteconConnection.Open();
                //    using (SQLiteCommand sqlitecmdCommand = new SQLiteCommand(sSQl, sqliteconConnection))
                //    {
                //        //logging here
                //        //execute the query and read the result
                //        using (SQLiteDataReader sqlitereadReader = sqlitecmdCommand.ExecuteReader())
                //        {
                //            if (sqlitereadReader.Read())
                //            {
                //                string sRowData = "";
                //                string sColumnName = "";
                //                for (int i = 0; i < sqlitereadReader.FieldCount; i++)
                //                {
                //                    sColumnName = sqlitereadReader.GetName(i).ToString(); // column name
                //                    sRowData = sqlitereadReader.GetValue(i).ToString(); //actual value we care about
                //                    m_dictProjectInfoBase.Add(sColumnName, sRowData); //build up the dictionary so the column is the key and the value is the value in the cell...
                //                    //logging statement placeholder
                //                }

                //            }
                //            else
                //            {
                //                //there are no records in the project_Table
                //                for(int i = 0; i <sqlitereadReader.FieldCount; i++)
                //                {
                //                    string sColumnName = sqlitereadReader.GetName(i);
                //                    m_dictProjectInfoBase.Add(sColumnName, null);
                //                }
                //            }


                //        }
                //    }
                //}


            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in GetProjectInfoFromDatabase " + ex.Message, "ViAssist");
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


        //this just goes through each text box on the form and builds up a dictionary based on the values on the form currently (so that we can compare with the values in the db)
        private static void GatherProjectPropertiesInfo(ProjectPropertiesForm projectPropertiesForm, Visio.Document ovDoc)
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
                    string sDirectoryPath = FileUtilities.ReturnFileStructurePath(ovDoc.Path);
                    //the created date doesn't exist yet...
                    DateTime dtCreatedDate = DateTime.Now;
                    oDictToUpdate["CreatedDate"] = dtCreatedDate.ToString("yyyy-MM-dd HH:mm:ss");
                    sProjectID = ProjectUtilities.GenerateProjectID(sDirectoryPath, dtCreatedDate, m_dictProjectInfoToCompare["ProjectName"]);
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




        //increases or decreases the projects file count by one
        internal static void AdjustFileCount(string sAdjustment)
        {
            //sAdjustment will either be Increase or Decrease

            ProjectUtilities.GetProjectInfoFromDatabase();

            List<RecordUpdate> lstUpdatedRecords = new List<RecordUpdate>();

            foreach (RecordUpdate ruRecord in m_mruRecordsBase.ruRecords)
            {
                // Clone the column values dictionary so we don't mutate the original
                Dictionary<string, string> oDictColumnValues = new Dictionary<string, string>(ruRecord.odictColumnValues);

                // Get current FileCount
                int iFileCount = 0;
                if (oDictColumnValues.TryGetValue("FileCount", out string sFileCount))
                {
                    // Parse safely
                    int.TryParse(sFileCount, out iFileCount);
                }

                // increase or decrease based on sAdjustment 
                if (sAdjustment == "Increase")
                {
                    iFileCount++;
                }
                else
                {
                    iFileCount--;
                }


                // Update the dictionary
                oDictColumnValues["FileCount"] = iFileCount.ToString();

                // Create a new RecordUpdate with the updated FileCount
                RecordUpdate ruUpdated = new RecordUpdate();
                ruUpdated.sPrimaryKeyColumn = ruRecord.sPrimaryKeyColumn;
                ruUpdated.sId = ruRecord.sId;
                ruUpdated.odictColumnValues = oDictColumnValues;

                // Add to the list
                lstUpdatedRecords.Add(ruUpdated);
            }
            m_mruRecordsBase = new MultipleRecordUpdates(lstUpdatedRecords);

            //increase the value in FileCount for the project_table in the database...
            DataProcessingUtilities.BuildUpdateSqlForMultipleRecords(DataProcessingUtilities.SqlTables.ProjectTable.sProjectTable, m_mruRecordsBase);
        }

        internal static string GenerateProjectID(string sDirectoryPath, DateTime createdDate, string sProjectName)
        {
            //project: sDirectoryPath + "Dwg - Cover Pages" + project name and created date
            //file: projectID + filepath + created date
            //page: ProjectID + FileID + page name + created date

            string input = sDirectoryPath + "Dwg - Cover Pages.vsdx" + sProjectName + createdDate.ToString("yyyy-MM-dd HH:mm:ss"); // formatted
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

        internal static bool ClearProjectID(MultipleRecordUpdates mruRecords)
        {
            foreach (RecordUpdate ruUpdated in mruRecords.ruRecords)
            {
                Visio.Application ovApp = Globals.ThisAddIn.Application;
                string sFilePath = ruUpdated.odictColumnValues["FilePath"];

                Visio.Document ovDoc = FileUtilities.IsVisioFileOpen(ovApp, sFilePath);
                if (ovDoc == null)
                {
                    //the document is not open in the current instance of visio-check to see if it is open/locked 
                    bool bFileLocked = FileUtilities.IsFileLocked(sFilePath);
                    if (bFileLocked)
                    {
                        //the file is open in another instance...we want to close it and re open it in our instance of visio i think we need to tell them they need to close it before we disassociate it...
                        ///otherwise we might run into cached docuemnts errors...
                        return false;
                    }
                    else
                    {

                        ovDoc = ovApp.Documents.Open(sFilePath);
                        ovDoc.DocumentSheet.Cells["User.ProjectID"].Formula = "\"\"";
                        ovDoc.SaveAs(sFilePath);
                        ovDoc.Close();
                        return true;
                    }
                }
                else
                {
                    //the document is already openend 
                    ovDoc.DocumentSheet.Cells["User.ProjectID"].Formula = "\"\"";
                    ovDoc.SaveAs(sFilePath);
                }



            }
            return true;
        }

        internal static void DeleteProject()
        {
            // i want to delete the project entireley so i will see if i can delete the folder (if everything in it is closed...)
            //open a folder dialog box and have the user point to the folder they want to delete
            //try to delete it-if we can't catch the exception and tell the user hey you need to close all the files in that project before i delete the project...
            using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "Select the VisAssist project folder to delete";

                if (folderDialog.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(folderDialog.SelectedPath))
                {
                    string sProjectFolderPath = folderDialog.SelectedPath;

                    try
                    {
                        bool bAllFilesUnlocked = true;
                        foreach(string sFilePath in Directory.GetFiles(sProjectFolderPath, "*", SearchOption.AllDirectories))
                        {
                           bool bIsFileLocked = FileUtilities.IsFileLocked(sFilePath);
                            if(bIsFileLocked)
                            {
                                bAllFilesUnlocked = false;
                                break;
                            }
                        }
                        //if this files name is VisAssistBackEnd.db delete this last if we were succesfully in deleting the other projects
                        // Attempt to delete entire project folder
                        if(bAllFilesUnlocked)
                        {
                            Directory.Delete(sProjectFolderPath, true);
                            MessageBox.Show("Project deleted successfully.", "VisAssist", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            //a file in the folder is locked...
                            MessageBox.Show("Unable to delete the project folder.\n\n" +
                            "Please make sure all Visio documents and related files in this project are closed, then try again.",
                            "VisAssist",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning
                        );
                        }
                        

                        
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
            }


        }
    }
}

