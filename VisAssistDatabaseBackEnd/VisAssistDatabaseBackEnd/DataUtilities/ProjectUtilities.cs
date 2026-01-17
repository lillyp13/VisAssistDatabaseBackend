using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using Microsoft.Office.Interop.Visio;
using static VisAssistDatabaseBackEnd.DataUtilities.ConnectionsUtilities;
using static VisAssistDatabaseBackEnd.DataUtilities.DataProcessingUtilities;
using System.Diagnostics;
using VisAssistDatabaseBackEnd.Forms;
using System.Windows.Forms;
using System.Security.Cryptography;

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
        static SQLiteConnection Connection = ConnectionsUtilities.Connection;

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
        public static Dictionary<string,string> m_dictProjectInfoToCompare = new Dictionary<string, string>();
        public static Dictionary<string, string> m_dictProjectInfoToUpdate = new Dictionary<string, string>();
        //Project Actions
        internal static void AddProjectInfo()
        {
            //use the seed data and push that to the database
            //thhis adds the project info seed data
            string sProjectTableName = "project_table";
            //first check if the database file exists and then can continue 
            bool bFolderAlreadyExists = ConnectionsUtilities.CheckForDatabaseDirectory();

            if (bFolderAlreadyExists)
            {
                bool bDatabaseFileExists = System.IO.File.Exists(DatabaseConfig.DatabasePath);
                if (bDatabaseFileExists)
                {
                    bool bTableExists = DoesTableExist(sProjectTableName);
                    if (bTableExists)
                    {
                        //only add the data if the project_table exists...
                        DatabaseSeeding.SeedProjects();
                    }
                    else
                    {
                        MessageBox.Show("Please add the database first: the table " + sProjectTableName + " does not exist");
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
                    
            
        }
        


        internal static void DeleteProjectInfo()
        {
            try
            {
                //delete all the records in the project_table
                using (SQLiteConnection sqliteConnection = new SQLiteConnection(Connection))
                {
                    sqliteConnection.Open();
                    string sDelete = "DELETE FROM project_table;";

                    using (SQLiteCommand cmd = new SQLiteCommand(sDelete, sqliteConnection))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    //reset the auto-increment counter
                    string sReset = "DELETE FROM sqlite_sequence WHERE name = 'project_table';";
                    using (SQLiteCommand cmd = new SQLiteCommand(sReset, sqliteConnection))
                    {
                        cmd.ExecuteNonQuery();
                    }
                        
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show("Error in DeleteProjectInfo " + ex.Message, "VisAssist");
            }
        }

        internal static void PopulatePropertiesForm(ProjectPropertiesForm projectPropertiesForm)
        {
            try
            {


                m_dictProjectInfoToCompare.Clear();
                //we have m_lstProjectInfo 
                //get the txtId textbox on the projectpropertiesform
                if (m_dictProjectInfoBase.Count > 0)
                {
                    projectPropertiesForm.txtID.Text = m_dictProjectInfoBase["Id"].ToString();
                    projectPropertiesForm.txtProjectName.Text = m_dictProjectInfoBase["ProjectName"].ToString();
                    projectPropertiesForm.txtCustomerName.Text = m_dictProjectInfoBase["CustomerName"].ToString();
                    projectPropertiesForm.txtCreatedDate.Text = m_dictProjectInfoBase["CreatedDate"].ToString();
                    projectPropertiesForm.txtModifiedDate.Text = m_dictProjectInfoBase["ModifiedDate"].ToString();
                    projectPropertiesForm.txtJobName.Text = m_dictProjectInfoBase["JobName"].ToString();
                    projectPropertiesForm.txtJobNumber.Text = m_dictProjectInfoBase["JobNumber"].ToString();
                    projectPropertiesForm.txtJobCity.Text = m_dictProjectInfoBase["JobCity"].ToString();
                    projectPropertiesForm.txtJobState.Text = m_dictProjectInfoBase["JobState"].ToString();
                    projectPropertiesForm.txtJobStreetAddress1.Text = m_dictProjectInfoBase["JobStreetAddress1"].ToString();
                    projectPropertiesForm.txtJobStreetAddress2.Text = m_dictProjectInfoBase["JobStreetAddress2"].ToString();
                    projectPropertiesForm.txtJobZipCode.Text = m_dictProjectInfoBase["JobZipCode"].ToString();
                    projectPropertiesForm.txtControlContractorName.Text = m_dictProjectInfoBase["ControlContractorName"].ToString();
                    projectPropertiesForm.txtControlContractorCity.Text = m_dictProjectInfoBase["ControlContractorCity"].ToString();
                    projectPropertiesForm.txtControlContractorState.Text = m_dictProjectInfoBase["ControlContractorState"].ToString();
                    projectPropertiesForm.txtControlContractorStreetAddress1.Text = m_dictProjectInfoBase["ControlContractorStreetAddress1"].ToString();
                    projectPropertiesForm.txtControlContractorStreetAddress2.Text = m_dictProjectInfoBase["ControlContractorStreetAddress2"].ToString();
                    projectPropertiesForm.txtControlContractorZipCode.Text = m_dictProjectInfoBase["ControlContractorZipCode"].ToString();
                    projectPropertiesForm.txtControlContractorPhone.Text = m_dictProjectInfoBase["ControlContractorPhone"].ToString();
                    projectPropertiesForm.txtControlContractorEmail.Text = m_dictProjectInfoBase["ControlContractorEmail"].ToString();
                    projectPropertiesForm.txtMechanicalEngineer.Text = m_dictProjectInfoBase["MechanicalEngineer"].ToString();
                    projectPropertiesForm.txtMechanicalContractor.Text = m_dictProjectInfoBase["MechanicalContractor"].ToString();
                    projectPropertiesForm.txtDesignedBy.Text = m_dictProjectInfoBase["DesignedBy"].ToString();
                    projectPropertiesForm.txtReviewBy.Text = m_dictProjectInfoBase["ReviewedBy"].ToString();
                    projectPropertiesForm.txtFileCount.Text = m_dictProjectInfoBase["FileCount"].ToString();
                }
                else
                {
                    MessageBox.Show("There are no records in the project_table");
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show("Error in PopulatePropertiesForm " + ex.Message, "VisAssist");
            }
            
        }

        internal static void GetProjectInfoFromDatabase()
        {
            try
            {
                //logging statement placeholder

                m_dictProjectInfoBase.Clear();//this should be cleared because you build this when the user presses update
                m_dictProjectInfoToUpdate.Clear();//this should be cleared because you build this when the user presses update

                string sSQl = @"SELECT * FROM project_table WHERE Id = 1";
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
                                string sRowData = "";
                                string sColumnName = "";
                                for (int i = 0; i < sqlitereadReader.FieldCount; i++)
                                {
                                    sColumnName = sqlitereadReader.GetName(i).ToString(); // column name
                                    sRowData = sqlitereadReader.GetValue(i).ToString(); //actual value we care about
                                    m_dictProjectInfoBase.Add(sColumnName, sRowData); //build up the dictionary so the column is the key and the value is the value in the cell...
                                    //logging statement placeholder
                                }

                            }


                        }
                    }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show("Error in GetProjectInfoFromDatabase " + ex.Message, "ViAssist");
            }
            
        }

        internal static void DeleteDatabase()
        {
            try
            {
                string sFilePath = DatabaseConfig.DatabasePath;
                if (System.IO.File.Exists(sFilePath))
                {
                    System.IO.File.Delete(sFilePath);
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show("Error in DeleteDatabase " + ex.Message, "VisAssist");
            }
        }

        internal static void OpenProjectForm()
        {
            try
            {
                ProjectPropertiesForm oNewForm = new ProjectPropertiesForm();
                oNewForm.Display();
                oNewForm.ShowDialog();
            }
            catch(Exception ex)
            {
                MessageBox.Show("Error in OpenProjectForm " + ex.Message, "VisAssist");
            }
        }


        //this just goes through each text box on the form and builds up a dictionary based on the values on the form currently (so that we can compare with the values in the db)
        private static void GatherProjectPropertiesInfo(ProjectPropertiesForm projectPropertiesForm)
        {
            try
            {
                string sID = projectPropertiesForm.txtID.Text.TrimEnd();
                m_dictProjectInfoToCompare.Add("Id", sID);

                string sProjectName = projectPropertiesForm.txtProjectName.Text.TrimEnd();
                m_dictProjectInfoToCompare.Add("ProjectName", sProjectName);

                string sCustomerName = projectPropertiesForm.txtCustomerName.Text.TrimEnd();
                m_dictProjectInfoToCompare.Add("CustomerName", sCustomerName);

                string sCreatedDate = projectPropertiesForm.txtCreatedDate.Text.TrimEnd();
                m_dictProjectInfoToCompare.Add("CreatedDate", sCreatedDate);

                string sModifiedDate = projectPropertiesForm.txtModifiedDate.Text.TrimEnd();
                m_dictProjectInfoToCompare.Add("ModifiedDate", sModifiedDate);

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
            }
            catch(Exception ex)
            {
                MessageBox.Show("Error in GatherProjectPropertiesInfo " + ex.Message, "VisAssist");
            }
        }



        internal static void UpdateProjectInfo(ProjectPropertiesForm projectPropertiesForm)
        {
            try
            {
                m_dictProjectInfoToCompare.Clear(); //clear this before populating it in GatherProjectPropertiesInfo
                ProjectUtilities.GatherProjectPropertiesInfo(projectPropertiesForm);

                m_dictProjectInfoToUpdate = DataProcessingUtilities.CompareDataDictionaries(m_dictProjectInfoBase, m_dictProjectInfoToCompare);


                if (m_dictProjectInfoToUpdate.Count > 0)
                {
                    string sTable = "project_table";

                    DataProcessingUtilities.BuildUpdateSqlForRecordDictionary(sTable, m_dictProjectInfoToUpdate, "UPDATE");

                    ProjectUtilities.GetProjectInfoFromDatabase(); //go and grab the data from the database to populate the m_dictProjectInfoBase

                }
            }
            catch(Exception ex)
            {
                MessageBox.Show("Error in UpdateProjectInfo " + ex.Message, "VisAssist");
            }
        }
    }
}

