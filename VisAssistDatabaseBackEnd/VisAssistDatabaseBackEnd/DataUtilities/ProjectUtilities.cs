using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using Microsoft.Office.Interop.Visio;
using static VisAssistDatabaseBackEnd.DataUtilities.ConnectionsUtilities;

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


        //Project Actions
        internal static void AddProjectInfo()
        {
            //use the seed data and push that to the database
            //this adds the project info seed data
            DatabaseSeeding.seedProjectTable();
        }
        internal static void UpdateProjectInfo()
        {
            throw new NotImplementedException();
            //DatabaseSeeding.UpdateProjectInfoWithSeedData();
        }
        internal static void DeleteProjectInfo()
        {
            //delete all the records in the project_table
            using (SQLiteConnection connection = new SQLiteConnection(Connection))
            {
                connection.Open();
                string sDelete = "DELETE FROM project_table;";

                new SQLiteCommand(sDelete, connection).ExecuteNonQuery();

                //reset the auto-increment counter
                string sReset = "DELETE FROM sqlite_sequence WHERE name = 'project_table';";
                new SQLiteCommand(sReset, connection).ExecuteNonQuery();
            }
        }



    }
}

