using Microsoft.Office.Interop.Visio;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using VisAssistDatabaseBackEnd.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Visio = Microsoft.Office.Interop.Visio;

namespace VisAssistDatabaseBackEnd.DataUtilities
{
    internal class PageUtilities
    {
        string sPageName;
        int iPageID;
        int iProjectID;
        int iFileID;
        int iPageIndex;
        DateTime dtCreatedDate;
        DateTime dtLastModifiedDate;
        string sVersion;
        string sClass;
        string sOrientation;
        string sScale;
        //static SQLiteConnection Connection = ConnectionsUtilities.Connection;


        string sPageNumber; //for pageformat...


        public static MultipleRecordUpdates m_mruRecordsBase = new MultipleRecordUpdates();
        public static MultipleRecordUpdates m_mruRecordsToCompare = new MultipleRecordUpdates();
        public static MultipleRecordUpdates m_mruRecordsToUpdate = new MultipleRecordUpdates();


        //CRUD Actions
        internal static void DeletePage(PagesForm pagesForm)
        {
            //get the selected row in the filePropertiesForm.dgvFileData to determine which file to delete

            // Get the selected row
            DataGridViewSelectedRowCollection colSelectedRows = pagesForm.dgvPages.SelectedRows;
            if (colSelectedRows == null || colSelectedRows.Count == 0)
            {
                MessageBox.Show("Please select at least one page to delete.");
                return;
            }

            // Build a list of RecordUpdate objects for each selected row
            List<RecordUpdate> lstRecordsToDelete = new List<RecordUpdate>();
            foreach (DataGridViewRow dgvRow in colSelectedRows)
            {
                int iFileID = Convert.ToInt32(dgvRow.Cells["PageID"].Value);

                RecordUpdate ruRecord = new RecordUpdate();
                ruRecord.sPrimaryKeyColumn = "PageID";
                ruRecord.iId = iFileID;

                lstRecordsToDelete.Add(ruRecord);
            }

            MultipleRecordUpdates mruRecordUpdates = new MultipleRecordUpdates(lstRecordsToDelete);

            // Call delete
            DataProcessingUtilities.BuildDeleteSqlForMultipleRecords(DataProcessingUtilities.SqlTables.sPagesTable, mruRecordUpdates);

            foreach (DataGridViewRow dgvRow in colSelectedRows)
            {
                pagesForm.dgvPages.Rows.Remove(dgvRow);
            }
        }
        internal static void DeleteAllPages()
        {
            //delete all the records in the pages_table
            using (SQLiteConnection sqliteConnection = new SQLiteConnection(DatabaseConfig.ConnectionString))
            {
                sqliteConnection.Open();
                //enable foreign key enforcemnt for this connection
                using (SQLiteCommand sqlitcmdPragma = new SQLiteCommand("PRAGMA foreign_keys = ON;", sqliteConnection))
                {
                    sqlitcmdPragma.ExecuteNonQuery();
                }
                // string sDelete = "DELETE FROM pages_table;";

                string sDelete = "DELETE FROM " + DataProcessingUtilities.SqlTables.sPagesTable + ";";
                using (SQLiteCommand cmd = new SQLiteCommand(sDelete, sqliteConnection))
                {
                    cmd.ExecuteNonQuery();
                }

                //reset the auto-increment counter 
                string sReset = "DELETE FROM sqlite_sequence WHERE name = 'pages_table';"; ///will need to do shapes too...
                using (SQLiteCommand cmd = new SQLiteCommand(sReset, sqliteConnection))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }
        internal static void UpdatePage(PagesForm pagesForm, bool bAllPages, int iFileID)
        {
            if (m_mruRecordsToCompare.ruRecords != null)
            {
                m_mruRecordsToCompare.ruRecords.Clear();
            }

            bool bIsNull = false;
            List<RecordUpdate> lstRecordUpdate = new List<RecordUpdate>();
            foreach (DataGridViewRow dgvRow in pagesForm.dgvPages.Rows)
            {
                Dictionary<string, string> oDictColumnValues = new Dictionary<string, string>();

                int iPrimaryKeyValue = 0;



                for (int i = 0; i <= pagesForm.dgvPages.Columns.Count - 1; i++)
                {
                    DataGridViewColumn dgvColumn = pagesForm.dgvPages.Columns[i];
                    string sColumnName = dgvColumn.Name;
                    if (dgvRow.Cells[i].Value != null)
                    {
                        string sValue = dgvRow.Cells[i].Value.ToString();
                        string sKey = dgvColumn.Name;

                        if (sColumnName != DataProcessingUtilities.SqlTables.sPagesTablePK)
                        {
                            oDictColumnValues.Add(sColumnName, sValue); //this is not the primary key
                        }
                    }
                    else
                    {
                        bIsNull = true;
                    }
                }


                //create a recordupdate for this row only if it is not null
                if (!bIsNull)
                {
                    RecordUpdate ruRecordUpdate = new RecordUpdate();
                    ruRecordUpdate.sPrimaryKeyColumn = DataProcessingUtilities.SqlTables.sPagesTablePK;
                    ruRecordUpdate.iId = iPrimaryKeyValue;
                    ruRecordUpdate.odictColumnValues = oDictColumnValues;

                    lstRecordUpdate.Add(ruRecordUpdate);
                }

            }

            //wrap all the records into a multiple recorsupdates object
            m_mruRecordsToCompare = new MultipleRecordUpdates(lstRecordUpdate);

            m_mruRecordsToUpdate = DataProcessingUtilities.CompareDataForMultipleRecords(m_mruRecordsBase, m_mruRecordsToCompare);


            if (m_mruRecordsToUpdate.ruRecords.Count > 0)
            {
                //there is something to update

                DataProcessingUtilities.BuildUpdateSqlForMultipleRecords(DataProcessingUtilities.SqlTables.sPagesTable, m_mruRecordsToUpdate);
                if (bAllPages)
                {
                    //get the pages for all the files
                    PageUtilities.GetAllPages();
                }
                else
                {
                    //get the pages for a specific file
                    PageUtilities.GetPagesForSpecificFile(iFileID);
                }

            }
        }

        internal static void AddPage()
        {

        }
        internal static void AddSeedPage() //SEED
        {
            //make sure there is a file in the files_table and a project in the project_table
           
            bool bDoesTableExist = DataProcessingUtilities.DoesParentTableHaveRecord(DataProcessingUtilities.SqlTables.sPagesTable);
            if (bDoesTableExist)
            {
                DatabaseSeeding.SeedPages();
            }
            else
            {
                MessageBox.Show("Please add a record to the files_Table.");
            }
            
        }
       



        
        
        //Helper Functions
        internal static void OpenPagesForm()
        {
            try
            {
                PagesForm oNewForm = new PagesForm();
                oNewForm.Display();
                oNewForm.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in OpenPagesForm " + ex.Message, "VisAssist");
            }
        }
        internal static void PopulatePagesForm(PagesForm pagesForm)
        {
            //we have m_mruRecordsBase that contains each page go through it and populate the datagridview...
            try
            {
                // Clear existing rows first
                pagesForm.dgvPages.Rows.Clear();

                if (m_mruRecordsBase.ruRecords == null || m_mruRecordsBase.ruRecords.Count == 0)
                {
                    MessageBox.Show("There are no pages for this file.");
                    return; //nothing to populate
                }


                // Loop through each record
                foreach (RecordUpdate ruRecord in m_mruRecordsBase.ruRecords)
                {
                    // Create a new row
                    DataGridViewRow dgvRow = new DataGridViewRow();
                    dgvRow.CreateCells(pagesForm.dgvPages);

                    // Populate each cell by matching column names
                    foreach (DataGridViewColumn dgvCol in pagesForm.dgvPages.Columns)
                    {
                        string sColName = dgvCol.Name;

                        if (ruRecord.odictColumnValues.ContainsKey(sColName))
                        {
                            dgvRow.Cells[dgvCol.Index].Value = ruRecord.odictColumnValues[sColName];
                        }
                        else
                        {
                            dgvRow.Cells[dgvCol.Index].Value = null; // or string.Empty if preferred
                        }
                    }

                    // Add the row to the DataGridView
                    pagesForm.dgvPages.Rows.Add(dgvRow);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in PopulatePagesForm: " + ex.Message, "VisAssist");
            }
        }


        internal static void GetPagesForSpecificFile(int iFileID)
        {
            try
            {
                if(m_mruRecordsBase.ruRecords != null)
                {
                    m_mruRecordsBase.ruRecords.Clear();
                }
               
                List<RecordUpdate> lstRecords = new List<RecordUpdate>();

                // string sSql = @"SELECT * FROM pages_table WHERE FileID = @FileID";
                string sSql = @"SELECt * FROM " + DataProcessingUtilities.SqlTables.sPagesTable + " WHERE FileID = @FileID";

                using (SQLiteConnection sqliteconConnection = new SQLiteConnection(DatabaseConfig.ConnectionString))
                {
                    sqliteconConnection.Open();
                    using (SQLiteCommand sqlitecmdCommand = new SQLiteCommand(sSql, sqliteconConnection))
                    {
                        // add parameter to avoid SQL injection
                        sqlitecmdCommand.Parameters.AddWithValue("@FileID", iFileID);

                        using (SQLiteDataReader sqlitereadReader = sqlitecmdCommand.ExecuteReader())
                        {
                            while (sqlitereadReader.Read())
                            {
                                Dictionary<string, string> odictColumnValues = new Dictionary<string, string>();
                                int iID = 0;

                                for (int i = 0; i < sqlitereadReader.FieldCount; i++)
                                {
                                    string sColumnName = sqlitereadReader.GetName(i);
                                    string sValue = sqlitereadReader.IsDBNull(i) ? string.Empty : sqlitereadReader.GetValue(i).ToString();
                                    odictColumnValues.Add(sColumnName, sValue);

                                    if (sColumnName == DataProcessingUtilities.SqlTables.sPagesTablePK)
                                    {
                                        iID = Convert.ToInt32(sqlitereadReader.GetValue(i));
                                    }
                                        
                                }

                                RecordUpdate ruRecordUpdate = new RecordUpdate();
                                ruRecordUpdate.sPrimaryKeyColumn = DataProcessingUtilities.SqlTables.sPagesTablePK;
                                ruRecordUpdate.iId = iID;
                                ruRecordUpdate.odictColumnValues = odictColumnValues;


                                lstRecords.Add(ruRecordUpdate);
                            }
                        }
                    }
                }

                m_mruRecordsBase = new MultipleRecordUpdates(lstRecords);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in GetPagesForFile: " + ex.Message, "VisAssist");
                
            }
        }
        internal static void GetAllPages()
        {
            //get all the pages in the pages_table
            try
            {
                
                List<RecordUpdate> lstRecords = new List<RecordUpdate>();

                // Fetch all pages, no WHERE clause
                //string sSql = @"SELECT * FROM pages_table";
                string sSql = @"SELECT * FROM " + DataProcessingUtilities.SqlTables.sPagesTable;

                using (SQLiteConnection sqliteconConnection = new SQLiteConnection(DatabaseConfig.ConnectionString))
                {
                    sqliteconConnection.Open();
                    using (SQLiteCommand sqlitecmdCommand = new SQLiteCommand(sSql, sqliteconConnection))
                    {
                        // No parameter needed anymore

                        using (SQLiteDataReader sqlitereadReader = sqlitecmdCommand.ExecuteReader())
                        {
                            while (sqlitereadReader.Read())
                            {
                                Dictionary<string, string> odictColumnValues = new Dictionary<string, string>();
                                int iID = 0;

                                for (int i = 0; i < sqlitereadReader.FieldCount; i++)
                                {
                                    string sColumnName = sqlitereadReader.GetName(i);
                                    string sValue = sqlitereadReader.IsDBNull(i) ? string.Empty : sqlitereadReader.GetValue(i).ToString();
                                    odictColumnValues.Add(sColumnName, sValue);

                                    if (sColumnName == DataProcessingUtilities.SqlTables.sPagesTablePK)
                                    {
                                        iID = Convert.ToInt32(sqlitereadReader.GetValue(i));
                                    }
                                        
                                }

                                RecordUpdate ruRecordUpdate = new RecordUpdate();
                                ruRecordUpdate.sPrimaryKeyColumn = DataProcessingUtilities.SqlTables.sPagesTablePK;
                                ruRecordUpdate.iId = iID;
                                ruRecordUpdate.odictColumnValues = odictColumnValues;

                                lstRecords.Add(ruRecordUpdate);
                            }
                        }
                    }
                }

                m_mruRecordsBase = new MultipleRecordUpdates(lstRecords);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in GetPagesForFile: " + ex.Message, "VisAssist");
            }

        }

        internal static MultipleRecordUpdates BuildPageInformation(Visio.Page ovPage)
        {

            //PageName
            //ProjectID
            //FileID
            //PageIndex
            //CreatedDate
            //LastModifiedDate
            //Version
            //Class
            //Orientation
            //Scale

            string sPageName = ovPage.Name;
            string sProjectID = ovPage.Document.DocumentSheet.Cells["User.ProjectID"].ResultIU.ToString();
            string sFileID = ovPage.Document.DocumentSheet.Cells["User.FileID"].ResultIU.ToString();
            int iPageIndex = ovPage.Index;
            //get created date from a user cell?
            //for now it will the current date 
            string sCreatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            string sLastModifiedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            //get version and class also from user cells
            string sVersion = ovPage.PageSheet.Cells["User.Version"].get_ResultStr(0);
            string sClass = ovPage.PageSheet.Cells["User.Class"].get_ResultStr(0);


            //get the orientation and scale based on the attributes.. for now i might cheapen this process
            int iPageWidth = Convert.ToInt32(ovPage.PageSheet.Cells["PageWidth"].ResultIU);
            int iPageHeight = Convert.ToInt32(ovPage.PageSheet.Cells["PageHeight"].ResultIU);
            string sOrientation = "";
            int iScale = Convert.ToInt32(ovPage.PageSheet.Cells["PageScale"].ResultIU);
            string sScale = iScale.ToString();
            if(iPageWidth > iPageHeight)
            {
                //the width is larger than the height this is horizontal
                sOrientation = "Horizontal";
            }
            else
            {
                //the width is smaller than the height this is vertical 
                sOrientation = "Vertical";
            }

            


            Dictionary<string, string> oDictFileValues = new Dictionary<string, string>();
            oDictFileValues.Add("PageName", sPageName);
            oDictFileValues.Add("ProjectID", sProjectID);
            oDictFileValues.Add("FileID", sFileID);
            oDictFileValues.Add("CreatedDate", sCreatedDate);
            oDictFileValues.Add("LastModifiedDate", sLastModifiedDate);
            oDictFileValues.Add("Version", sVersion);
            oDictFileValues.Add("Class", sClass);
            oDictFileValues.Add("Orientation", sOrientation);
            oDictFileValues.Add("Scale", sScale);

            RecordUpdate ruFileRecord = new RecordUpdate();
            ruFileRecord.sPrimaryKeyColumn = DataProcessingUtilities.SqlTables.sFilesTablePK;
            ruFileRecord.iId = DataProcessingUtilities.GetNextIdForTable(DataProcessingUtilities.SqlTables.sFilesTable);
            ruFileRecord.odictColumnValues = oDictFileValues;

            return new MultipleRecordUpdates(new List<RecordUpdate> { ruFileRecord });
        }
    }

}
