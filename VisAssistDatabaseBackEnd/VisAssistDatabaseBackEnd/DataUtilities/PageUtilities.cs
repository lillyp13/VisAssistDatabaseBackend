using Accessibility;
using Microsoft.Office.Interop.Visio;
using MS.WindowsAPICodePack.Internal;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;
using VisAssistDatabaseBackEnd.Forms;
using VisAssistDatabaseBackEnd.VisioUtilities;
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


        //CRUD ACTIONS
        
        internal static void DeletePageInDatabase(Visio.Page ovPage, string sProjectID)
        {
            try
            {
                MultipleRecordUpdates mruRecordUpdates = BuildPageInfoBasedOnVisioPage(ovPage, sProjectID);
                // Call delete
                DatabaseUtilities.BuildDeleteSqlForMultipleRecords(DatabaseUtilities.SqlTables.PagesTable.sPagesTable, mruRecordUpdates);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in DeletePage " + ex.Message, "VisAssist");
            }

        }
      
        //internal static void UpdatePage(PagesInformationForm pagesForm, bool bAllPages, string sFileID)
        //{
        //    if (m_mruRecordsToCompare.ruRecords != null)
        //    {
        //        m_mruRecordsToCompare.ruRecords.Clear();
        //    }

        //    bool bIsNull = false;
        //    List<RecordUpdate> lstRecordUpdate = new List<RecordUpdate>();
        //    foreach (DataGridViewRow dgvRow in pagesForm.dgvPages.Rows)
        //    {
        //        Dictionary<string, string> oDictColumnValues = new Dictionary<string, string>();

        //        string sPrimaryKey = "";

        //        for (int i = 0; i <= pagesForm.dgvPages.Columns.Count - 1; i++)
        //        {
        //            DataGridViewColumn dgvColumn = pagesForm.dgvPages.Columns[i];
        //            string sColumnName = dgvColumn.Name;
        //            if (dgvRow.Cells[i].Value != null)
        //            {
        //                string sValue = dgvRow.Cells[i].Value.ToString();

        //                if (sColumnName != DatabaseUtilities.SqlTables.PagesTable.sPagesTablePK)
        //                {
        //                    //check to see if this is the LastModifiedDate
        //                    //we only want to add the lastmodifieddate if something else about the page has changed, this cannot be the only value...
        //                    if (sColumnName == "LastModifiedDate")
        //                    {
        //                        oDictColumnValues.Add(sColumnName, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        //                    }
        //                    else
        //                    {
        //                        oDictColumnValues.Add(sColumnName, sValue); //this is not the primary key or the last modiifed date...
        //                    }

        //                }
        //                else
        //                {
        //                    //this is the primary key
        //                    sPrimaryKey = sValue;
        //                }
        //            }
        //            else
        //            {
        //                bIsNull = true;
        //            }
        //        }


        //        //create a recordupdate for this row only if it is not null
        //        if (!bIsNull)
        //        {
        //            RecordUpdate ruRecordUpdate = new RecordUpdate();
        //            ruRecordUpdate.sPrimaryKeyColumn = DatabaseUtilities.SqlTables.PagesTable.sPagesTablePK;
        //            ruRecordUpdate.sId = sPrimaryKey;
        //            ruRecordUpdate.odictColumnValues = oDictColumnValues;

        //            lstRecordUpdate.Add(ruRecordUpdate);
        //        }

        //    }

        //    //wrap all the records into a multiple recorsupdates object
        //    m_mruRecordsToCompare = new MultipleRecordUpdates(lstRecordUpdate);

        //    m_mruRecordsToUpdate = DatabaseUtilities.CompareDataForMultipleRecords(m_mruRecordsBase, m_mruRecordsToCompare);




        //    if (m_mruRecordsToUpdate.ruRecords.Count > 0)
        //    {
        //        //there is something to update
        //        //sync with visio this is to simulate the actual event (user changes a page name in visio and it triggers the update to db 
        //        //this method is just to keep visio and our db in sync as of today (our event handlers...)


        //        //will need to add the page name no matter what..but should only update the LastModifiedDate if something else was updated...

        //        PageUtilities.UpdateVisioPages();


        //        DatabaseUtilities.BuildUpdateSqlForMultipleRecords(DatabaseUtilities.SqlTables.PagesTable.sPagesTable, m_mruRecordsToUpdate);
        //        //I think we will also want to update the files and projects LastModifiedDate-right???


        //        if (bAllPages)
        //        {
        //            //get the pages for all the files
        //            PageUtilities.GetAllPages();
        //        }
        //        else
        //        {
        //            //get the pages for a specific file
        //            Visio.Document ovDoc = Globals.ThisAddIn.Application.ActiveDocument;
        //            PageUtilities.GetPagesForCurrentFile(ovDoc);
        //        }




        //    }
        //}


        internal static void AddPageToDatabase(Visio.Page ovPage, string sProjectID, string sSource)
        {
            try
            {
                MultipleRecordUpdates oPageRecord = new MultipleRecordUpdates();
                switch (sSource)
                {
                    case "Visio":
                        {
                            oPageRecord = PageUtilities.BuildPageInfoBasedOnVisioPage(ovPage, sProjectID);
                            break;
                        }
                    case "New":
                        {
                            oPageRecord = PageUtilities.BuildPageInfoBasedOnVisioPage(ovPage, sProjectID);
                            //give it a new id...
                            string sFileID = ovPage.Document.DocumentSheet.Cells["User.FileID"].get_ResultStr(0);

                            string sNewPageID = GeneratePageID(sProjectID, sFileID, ovPage.Name, DateTime.Now);
                            RecordUpdate ru = oPageRecord.ruRecords[0];
                            ru.sId = sNewPageID;
                            oPageRecord.ruRecords[0] = ru;
                            //update the pages User.PageID with events off..

                            // will need to create a delayed event to add the new ids to the page/shapes on the page...
                            DelayedEvent oDelayedEvent = new DelayedEvent();
                            oDelayedEvent.sOperationType = "UpdateIDs";
                            oDelayedEvent.ovDocument = ovPage.Document;
                            oDelayedEvent.ovPage = ovPage;
                            oDelayedEvent.sPageID = sNewPageID;
                            Globals.ThisAddIn.m_delayedEvents.Add(oDelayedEvent);



                            break;
                        }
                }

                DatabaseUtilities.BuildInsertSqlForMultipleRecords(DatabaseUtilities.SqlTables.PagesTable.sPagesTable, oPageRecord);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in AddPageToDatabase " + ex.Message, "VisAssist");
            }
        }


        internal static void UpdatePageInDatabase(Visio.Page ovPage, string sProjectID)
        {
            try
            {
                MultipleRecordUpdates oPageRecord = PageUtilities.BuildPageInfoBasedOnVisioPage(ovPage, sProjectID);
                DatabaseUtilities.BuildUpdateSqlForMultipleRecords(DatabaseUtilities.SqlTables.PagesTable.sPagesTable, oPageRecord);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in UpdatePageInDatabase " + ex.Message, "VisAssist");
            }

        }


        //HELPER FUNCTIONS
        internal static MultipleRecordUpdates BuildPageInfoBasedOnVisioPage(Visio.Page ovPage, string sProjectID)
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
            if (sProjectID == "")
            {
                //we have sufficient data in the page's document shapesheet, grab the project id from there
                sProjectID = ovPage.Document.DocumentSheet.Cells["User.ProjectID"].get_ResultStr(0);//this will take the old project id if we are associating...
            }


            //we are in the process of associating a file so we have a different projectID we will be adding for pages..



            string sFileID = ovPage.Document.DocumentSheet.Cells["User.FileID"].get_ResultStr(0);
            int iPageIndex = ovPage.Index;
            //get created date from a user cell?
            //for now it will the current date 


            string sLastModifiedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            //get version and class also from user cells
            string sVersion = ovPage.PageSheet.Cells["User.Version"].get_ResultStr(0);
            string sClass = ovPage.PageSheet.Cells["User.PageClass"].get_ResultStr(0);


            //get the orientation and scale based on the attributes.. for now i might cheapen this process
            int iPageWidth = Convert.ToInt32(ovPage.PageSheet.Cells["PageWidth"].ResultIU);
            int iPageHeight = Convert.ToInt32(ovPage.PageSheet.Cells["PageHeight"].ResultIU);
            string sOrientation = "";
            int iScale = Convert.ToInt32(ovPage.PageSheet.Cells["PageScale"].ResultIU);
            string sScale = iScale.ToString();
            if (iPageWidth > iPageHeight)
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
            oDictFileValues.Add("PageIndex", iPageIndex.ToString());
            // oDictFileValues.Add("CreatedDate", dtCreatedDate.ToString());

            oDictFileValues.Add("LastModifiedDate", sLastModifiedDate);
            oDictFileValues.Add("Version", sVersion);
            oDictFileValues.Add("Class", sClass);
            oDictFileValues.Add("Orientation", sOrientation);
            oDictFileValues.Add("Scale", sScale);

            string sPageID = "";
            if (ovPage.PageSheet.CellExists["User.PageID", 0] == -1)
            {
                oDictFileValues["CreatedDate"] = ovPage.Document.DocumentSheet.Cells["User.CreatedDate"].get_ResultStr(0);
                sPageID = ovPage.PageSheet.Cells["User.PageID"].get_ResultStr(0);
            }

            if (sPageID == "")
            {
                //this is us adding a page there isn't a page id yet...
                oDictFileValues["CreatedDate"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                sPageID = PageUtilities.GeneratePageID(sProjectID, sFileID, sPageName, DateTime.Now);
                ovPage.PageSheet.AddNamedRow((short)Visio.VisSectionIndices.visSectionUser, "PageID", 0);
                ovPage.PageSheet.Cells["User.PageID"].Formula = "\"" + sPageID + "\"";

            }
            else
            {

            }

            RecordUpdate ruFileRecord = new RecordUpdate();
            ruFileRecord.sPrimaryKeyColumn = DatabaseUtilities.SqlTables.PagesTable.sPagesTablePK;
            ruFileRecord.sId = sPageID;
            ruFileRecord.odictColumnValues = oDictFileValues;

            return new MultipleRecordUpdates(new List<RecordUpdate> { ruFileRecord });
        }

        internal static string GeneratePageID(string sProjectID, string sFileID, string sPageName, DateTime now)
        {
            //project: sDirectoryPath + "Dwg - Cover Pages" + project name and created date
            //file: projectID + filepath + created date
            //page: ProjectID + FileID + page name + created date

            string input = sProjectID + sFileID + sPageName + now.ToString("yyyy-MM-dd HH:mm:ss"); // formatted
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

        internal static bool AddUserCellsToPage(Visio.Page ovPage)
        {
            try
            {
                bool bCellsAdded = true;
                //Visio.Page ovPage = Globals.ThisAddIn.Application.ActivePage;
                if (ovPage.PageSheet.CellExists["User.Version", 0] == 0)
                {
                    ovPage.PageSheet.AddNamedRow((short)Visio.VisSectionIndices.visSectionUser, "Version", 0); //not quite sure what the value of this is...
                    ovPage.PageSheet.Cells["User.Version"].Formula = "\"v1\""; //might want to pull the format string for visio fromm VisAssist...
                }
                else
                {
                    bCellsAdded = false;
                }
                if (ovPage.PageSheet.CellExists["User.PageClass", 0] == 0)
                {
                    ovPage.PageSheet.AddNamedRow((short)Visio.VisSectionIndices.visSectionUser, "PageClass", 0);
                    ovPage.PageSheet.Cells["User.PageClass"].Formula = "\"Working\"";//might want to pull the format string for visio fromm VisAssist...
                }
                if (ovPage.PageSheet.CellExists["User.CreatedDate", 0] == 0)
                {
                    ovPage.PageSheet.AddNamedRow((short)Visio.VisSectionIndices.visSectionUser, "CreatedDate", 0);
                    ovPage.PageSheet.Cells["User.CreatedDate"].Formula = "\"" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\"";
                }
                return bCellsAdded;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in AddUserCellsToPage " + ex.Message, "VisAssist");
            }
            return false;

        }
        internal static List<string> PopulateVisioPageList(Visio.Document ovDocument)
        {
            List<string> lstPages = new List<string>();
            try
            {
                foreach (Visio.Page ovPage in ovDocument.Pages)
                {
                    if (ovPage.PageSheet.CellExists["User.PageID", 0] == -1)
                    {
                        string sPageID = ovPage.PageSheet.Cells["User.PageID"].get_ResultStr(0);
                        lstPages.Add(sPageID);
                    }
                }
                return lstPages;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in PopulateVisioPageList " + ex.Message, "VisAssist");
            }
            return lstPages;
        }


        //FORMS
        internal static void OpenPagesForm()
        {
            try
            {
                PagesInformationForm oNewForm = new PagesInformationForm();
                oNewForm.Display();
                oNewForm.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in OpenPagesForm " + ex.Message, "VisAssist");
            }
        }
        internal static void PopulatePagesInformationForm(PagesInformationForm pagesForm)
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
        internal static void PopulatePagesForm(PagesForm pagesForm)
        {
            try
            {
                Visio.Page ovCurrentPage = Globals.ThisAddIn.Application.ActivePage;
                Visio.Document ovDocument = ovCurrentPage.Document;
                foreach (Visio.Page ovPage in ovDocument.Pages)
                {
                    //add the page to the dgvPages
                    string sPageName = ovPage.Name;
                    if (sPageName != ovCurrentPage.Name)
                    {
                        pagesForm.dgvPages.Rows.Add(sPageName);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in PopulatePagesForm " + ex.Message, "VisAssit");
            }
        }

        internal static void PopulatePagesFormForOnePage(PagesInformationForm pagesForm)
        {
            //clear all the rows first 
            pagesForm.dgvPages.Rows.Clear();

            Visio.Page ovPage = Globals.ThisAddIn.Application.ActivePage;
            if (ovPage != null)
            {
                //get the page information from visio 
                string sProjectID = ovPage.Document.DocumentSheet.Cells["User.ProjectID"].get_ResultStr(0);

                m_mruRecordsToUpdate = BuildPageInfoBasedOnVisioPage(ovPage, sProjectID);


                foreach (RecordUpdate ruRecord in m_mruRecordsToUpdate.ruRecords)
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
                            if (sColName == "PageID")
                            {
                                dgvRow.Cells[dgvCol.Index].Value = ruRecord.sId;
                            }
                            else
                            {
                                dgvRow.Cells[dgvCol.Index].Value = null; // or string.Empty if preferred
                            }

                        }
                    }

                    // Add the row to the DataGridView
                    pagesForm.dgvPages.Rows.Add(dgvRow);
                }

            }
        }



        //GATHERING INFORMATION
        internal static void GetPagesForCurrentFile(Visio.Document ovDoc)
        {
            try
            {
                //Visio.Document ovDoc = Globals.ThisAddIn.Application.ActiveDocument;
                if (ovDoc != null)
                {
                    string sFileID = ovDoc.DocumentSheet.Cells["User.FileID"].get_ResultStr(0);
                    string sPageID = "";
                    if (m_mruRecordsBase.ruRecords != null)
                    {
                        m_mruRecordsBase.ruRecords.Clear();
                    }

                    List<RecordUpdate> lstRecords = new List<RecordUpdate>();

                    // string sSql = @"SELECT * FROM pages_table WHERE FileID = @FileID";
                    string sSql = @"SELECt * FROM " + DatabaseUtilities.SqlTables.PagesTable.sPagesTable + " WHERE FileID = @FileID";

                    using (SQLiteConnection sqliteconConnection = new SQLiteConnection(DatabaseConfig.ConnectionString))
                    {
                        sqliteconConnection.Open();
                        using (SQLiteCommand sqlitecmdCommand = new SQLiteCommand(sSql, sqliteconConnection))
                        {
                            // add parameter to avoid SQL injection
                            sqlitecmdCommand.Parameters.AddWithValue("@FileID", sFileID);

                            using (SQLiteDataReader sqlitereadReader = sqlitecmdCommand.ExecuteReader())
                            {
                                while (sqlitereadReader.Read())
                                {
                                    Dictionary<string, string> odictColumnValues = new Dictionary<string, string>();

                                    for (int i = 0; i < sqlitereadReader.FieldCount; i++)
                                    {
                                        string sColumnName = sqlitereadReader.GetName(i);
                                        string sValue = sqlitereadReader.IsDBNull(i) ? string.Empty : sqlitereadReader.GetValue(i).ToString();
                                        odictColumnValues.Add(sColumnName, sValue);

                                        if (sColumnName == DatabaseUtilities.SqlTables.PagesTable.sPagesTablePK)
                                        {
                                            sPageID = sqlitereadReader.GetValue(i).ToString();
                                        }

                                    }

                                    RecordUpdate ruRecordUpdate = new RecordUpdate();
                                    ruRecordUpdate.sPrimaryKeyColumn = DatabaseUtilities.SqlTables.PagesTable.sPagesTablePK;
                                    ruRecordUpdate.sId = sPageID;
                                    ruRecordUpdate.odictColumnValues = odictColumnValues;


                                    lstRecords.Add(ruRecordUpdate);
                                }
                            }
                        }
                    }

                    m_mruRecordsBase = new MultipleRecordUpdates(lstRecords);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in GetPagesForFile: " + ex.Message, "VisAssist");

            }
        }


        internal static string GetColumnInfoInPagesTableFromDatabase(string sColumnName, string sPageID)
        {
            try
            {
                string sSpecificPiece = "";
                //use the dbPath which is the db file and open it and get the ProjectID from the project_table
                using (SQLiteConnection sqliteconConnection = new SQLiteConnection(DatabaseConfig.ConnectionString))
                {
                    //logging here
                    sqliteconConnection.Open();
                    string sSQL = $"SELECT {sColumnName} FROM pages_table WHERE PageID = @PageID LIMIT 1";

                    using (SQLiteCommand sqlcmdCommand = new SQLiteCommand(sSQL, sqliteconConnection))
                    {
                        sqlcmdCommand.Parameters.Add("@PageID", DbType.String).Value = sPageID;

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
                MessageBox.Show("Error in GetColumnInfoInPagesTableFromDatabase " + ex.Message, "VisAssist");
            }
            return "";
        }
        internal static void GetAllPages()
        {
            //get all the pages in the pages_table
            try
            {

                List<RecordUpdate> lstRecords = new List<RecordUpdate>();

                // Fetch all pages, no WHERE clause
                //string sSql = @"SELECT * FROM pages_table";
                string sSql = @"SELECT * FROM " + DatabaseUtilities.SqlTables.PagesTable.sPagesTable;

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
                                string sPageID = "";

                                for (int i = 0; i < sqlitereadReader.FieldCount; i++)
                                {
                                    string sColumnName = sqlitereadReader.GetName(i);
                                    string sValue = sqlitereadReader.IsDBNull(i) ? string.Empty : sqlitereadReader.GetValue(i).ToString();
                                    odictColumnValues.Add(sColumnName, sValue);

                                    if (sColumnName == DatabaseUtilities.SqlTables.PagesTable.sPagesTablePK)
                                    {
                                        sPageID = sqlitereadReader.GetValue(i).ToString();
                                    }

                                }

                                RecordUpdate ruRecordUpdate = new RecordUpdate();
                                ruRecordUpdate.sPrimaryKeyColumn = DatabaseUtilities.SqlTables.PagesTable.sPagesTablePK;
                                ruRecordUpdate.sId = sPageID;
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

       




        //COPYING PAGES

        internal static void WhatPagesToDuplicate()
        {
            //ask the user which other pages the user wants to duplicate..
            string sAction = "Duplicate";
            PagesForm oNewForm = new PagesForm();
            oNewForm.Display(sAction);
            oNewForm.Show();

        }

        internal static void GatherPagesToDuplicate(PagesForm pagesForm)
        {
            try
            {
                //grabs the selected pages in dgvPages to duplicate...
                List<string> olstPagesToDuplicate = new List<string>();
                foreach (DataGridViewRow dgvRow in pagesForm.dgvPages.SelectedRows)
                {
                    if (!dgvRow.IsNewRow)
                    {
                        string sPageName = dgvRow.Cells["PageName"].Value?.ToString();
                        olstPagesToDuplicate.Add(sPageName);
                    }
                }

                //add the current page name
                string sCurrentPageName = Globals.ThisAddIn.Application.ActivePage.Name;
                olstPagesToDuplicate.Add(sCurrentPageName);

                Dictionary<string, Visio.Page> oDictPagesToDuplicate = new Dictionary<string, Visio.Page>();
                Visio.Document ovDocument = Globals.ThisAddIn.Application.ActiveDocument;
                foreach (Visio.Page ovPage in ovDocument.Pages)
                {
                    if (olstPagesToDuplicate.Contains(ovPage.Name))
                    {
                        oDictPagesToDuplicate.Add(ovPage.Name, ovPage);
                    }
                }

                PageUtilities.DuplicateMultiplePages(oDictPagesToDuplicate);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in GatherPagesToDuplicate " + ex.Message, "VisAssist");
            }
        }

        private static void DuplicateMultiplePages(Dictionary<string, Visio.Page> oDictPages)
        {
            //we have a list of the pages we want to duplicate...
            //for each visio page in the dictioanry call the visio duplicate action
            //turn events off first 
            Visio.Document ovDocument = Globals.ThisAddIn.Application.ActiveDocument;
            int iUndoScope = ovDocument.Application.BeginUndoScope("Duplicate Pages");
            Dictionary<string, Visio.Page> oDictPagesToDuplicate = new Dictionary<string, Page>();
            ovDocument.Application.EventsEnabled = 0;
            //// 1️⃣ Find highest numeric page name
            foreach (Visio.Page ovPage in oDictPages.Values.OrderBy(p => p.Index))
            {
                Visio.Page ovNewPage = ovPage.Duplicate();

            //    // Move to end
                ovNewPage.Index = ovDocument.Pages.Count;
            //    string sNewName = ovPage.Name + "-Duplicated";

            //    ovNewPage.Name = sNewName;

                oDictPagesToDuplicate.Add(ovNewPage.Name, ovNewPage);
            }
            ovDocument.Application.EventsEnabled = -1;

            VisioUtilities.Application.OnPageDuplicated(oDictPagesToDuplicate);

            ovDocument.Application.EndUndoScope(iUndoScope, true);
        }


        //OTHER
        internal static void StressTest()
        {
            //this will add 50 pages and drop ten terminal blocks on each page
            Visio.Document ovCurrentDoc = Globals.ThisAddIn.Application.ActiveDocument;

            Visio.Document ovStencilDoc = null;

            foreach (Visio.Document ovDoc in Globals.ThisAddIn.Application.Documents)
            {
                if (ovDoc.Type == Visio.VisDocumentTypes.visTypeStencil && ovDoc.Name.Equals("TestStencil.vssx", StringComparison.OrdinalIgnoreCase))
                {
                    ovStencilDoc = ovDoc;
                    break;
                }
            }

            Visio.Master ovTerminalBlockMaster = ovStencilDoc.Masters["TerminalBlock"];

            if (ovCurrentDoc != null)
            {
                //add 50 visio pages and drop 10 terminals blocks on each page

                for (int ithPage = 1; ithPage <= 50; ithPage++)
                {
                    Visio.Page ovPage = ovCurrentDoc.Pages.Add();

                    double dX = 1.0;

                    // Start at top of page
                    double dY = ovPage.PageSheet.CellsU["PageHeight"].ResultIU - 0.5;

                    for (int i = 0; i < 10; i++)
                    {
                        Visio.Shape ovShape = ovPage.Drop(ovTerminalBlockMaster, dX, dY);

                        // Move y down by the shape's actual height
                        double dHeight = ovShape.Cells["Height"].ResultIU;
                        dY -= dHeight + 0.2; // small gap
                    }
                }
            }
        }

        

        internal static List<string> PopulateShapesListBasedOnPage(Visio.Page ovPage, string sTableName)
        {
            List<string> lstResults = new List<string>();
            //gather all the shapes in the table for the given page...
            string sPageID = ovPage.PageSheet.Cells["User.PageID"].get_ResultStr(0);
            string sPk = DatabaseUtilities.GetPrimaryKey(sTableName);
            //query all the shapes in the table for sPageID...
            using (SQLiteConnection sqliteconConnection = new SQLiteConnection(DatabaseConfig.ConnectionString))
            {
                sqliteconConnection.Open();

                string sSQL = $@"SELECT {sPk} FROM {sTableName} WHERE PageID = @PageID;";

                using (SQLiteCommand sqlcmdCommand = new SQLiteCommand(sSQL, sqliteconConnection))
                {
                    sqlcmdCommand.Parameters.Add("@PageID", DbType.String).Value = sPageID;

                    using (SQLiteDataReader reader = sqlcmdCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lstResults.Add(reader.GetString(0));
                        }
                    }
                }
            }
            return lstResults;
        }

        internal static void UpdatePageIDInDatabase(string sOldPageID, string sCurrentPageID, Page ovPage)
        {
            //update the entry record in pages_table where sPageID and update the pageID to sCurrentPageID

            using (SQLiteConnection sqliteconConnection = new SQLiteConnection(DatabaseConfig.ConnectionString))
            {
                sqliteconConnection.Open();

                string sSql = @"UPDATE pages_table SET PageID = @NewPageID WHERE PageID = @OldPageID";


                using (SQLiteCommand sqlitecmdCommand = new SQLiteCommand(sSql, sqliteconConnection))
                {
                    sqlitecmdCommand.Parameters.AddWithValue("@NewPageID", sCurrentPageID);
                    sqlitecmdCommand.Parameters.AddWithValue("@OldPageID", sOldPageID);

                    sqlitecmdCommand.ExecuteNonQuery();
                }
            }
        }

        internal static void GetPageToMoveShapesTo(PagesForm pagesForm)
        {
            string sPageNameToMoveTo = "";
            foreach (DataGridViewRow dgvRow in pagesForm.dgvPages.SelectedRows)
            {
                if (!dgvRow.IsNewRow)
                {
                   sPageNameToMoveTo = dgvRow.Cells["PageName"].Value?.ToString();
                    
                }
            }

            //ok now paste what we have in our clipboard on the visio page sPageNameToMoveTo
            Visio.Application ovApp = Globals.ThisAddIn.Application;
            Visio.Document ovDocument = ovApp.ActiveDocument;
            foreach(Visio.Page ovPage in ovDocument.Pages)
            {
                if(ovPage.Name == sPageNameToMoveTo)
                {
                    string sNewPageID = ovPage.PageSheet.Cells["User.PageID"].get_ResultStr(0);
                    //this is the page we want to paste what is in our clipboard...
                   
                    ovPage.Paste();
                    Visio.Selection ovPastedShapes = ovApp.ActiveWindow.Selection;
                    //we will need to update the shapes we just pasted (most likely just the pageid...)
                    //foreach(Visio.Shape ovShape in ovPastedShapes)
                    //{
                    //    if (ovShape.CellExists["User.PageID",0] == -1)
                    //    {
                    //        ovShape.Cells["User.PageID"].Formula = VisioUtilities.Application.FormatStringForVisio(sNewPageID);
                    //    }
                    //}
                }
            }
        }
    }

}
