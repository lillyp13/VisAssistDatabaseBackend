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
using VisAssistDatabaseBackEnd.ShapeUtilities;
using VisAssistDatabaseBackEnd.ShapeUtilities.Wire;
using VisAssistDatabaseBackEnd.VisioUtilities;
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
            RecordUpdate ruFileRecord = new RecordUpdate();
            try
            {


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

                
                ruFileRecord.sPrimaryKeyColumn = DatabaseUtilities.SqlTables.PagesTable.sPagesTablePK;
                ruFileRecord.sId = sPageID;
                ruFileRecord.odictColumnValues = oDictFileValues;
                
            }
            catch(Exception ex)
            {
                MessageBox.Show("Error in BuildPageInfoBasedOnVisioPage " + ex.Message, "VisAssist");
            }
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

        internal static void PasteShapesOnPageUserSpecified(PagesForm pagesForm)
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
            foreach (Visio.Page ovPage in ovDocument.Pages)
            {
                if (ovPage.Name == sPageNameToMoveTo)
                {
                    string sNewPageID = ovPage.PageSheet.Cells["User.PageID"].get_ResultStr(0);
                    //this is the page we want to paste what is in our clipboard...
                    int iUndoScope = ovApp.BeginUndoScope("Cut and Paste Action");
                    Globals.ThisAddIn.m_sLastUndoScope = "Cut and Paste Action";
                    ovPage.Paste();
                    ovApp.EndUndoScope(iUndoScope, true);


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


                //before calling DuplicateMultiplePages we need to check the dictionary of pages they chose for any wires on a page they didn't pick...
               
                Dictionary<string, Visio.Page> oDictPagesToAskUser = new Dictionary<string, Visio.Page>();
                foreach(Visio.Page ovPage in oDictPagesToDuplicate.Values)
                {
                    Dictionary<string, Visio.Page> oDictOtherPages = WireUtilities.DoesPageContainWireMates(ovPage);
                    //check if there are any pages in oDictOtherPages that doesn't exist in oDictPagesToDuplicate
                    foreach (KeyValuePair<string, Visio.Page> kvPage in oDictOtherPages)
                    {
                        if (!oDictPagesToDuplicate.ContainsKey(kvPage.Key))
                        {
                            if (!oDictPagesToAskUser.ContainsKey(kvPage.Key))
                            {
                                oDictPagesToAskUser.Add(kvPage.Key, kvPage.Value);
                            }
                        }
                    }
                }
                if(oDictPagesToAskUser.Count > 0)
                {
                    //ask the user if they want to include these pages or not
                    DialogResult result = MessageBox.Show("There are additional related pages that contain wire mates.\n\n" + "Do you want to include these pages in the duplication?", "VisAssist",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                    if(result == DialogResult.Yes)
                    {
                        //add these pages to the dictionary to duplicate
                        foreach (KeyValuePair<string, Visio.Page> kvPage in oDictPagesToAskUser)
                        {
                            if (!oDictPagesToDuplicate.ContainsKey(kvPage.Key))
                            {
                                oDictPagesToDuplicate.Add(kvPage.Key, kvPage.Value);
                            }
                        }
                    }
                }

                int iUndoScope = ovDocument.Application.BeginUndoScope("Duplicate");
                PageUtilities.DuplicateMultiplePages(oDictPagesToDuplicate);
                ovDocument.Application.EndUndoScope(iUndoScope, true);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in GatherPagesToDuplicate " + ex.Message, "VisAssist");
            }
        }

        internal static void DuplicateMultiplePages(Dictionary<string, Visio.Page> oDictPages)
        {
            Visio.Document ovDocument = Globals.ThisAddIn.Application.ActiveDocument;
           // int iUndoScope = ovDocument.Application.BeginUndoScope("Duplicate Pages");
            try
            {


                //we have a list of the pages we want to duplicate...
                //for each visio page in the dictioanry call the visio duplicate action
                //turn events off first 


                Visio.Application ovApp = ovDocument.Application;
                ////get the active page?
                Visio.Page ovPage = ovDocument.Application.ActivePage;
                string sPageID = ovPage.PageSheet.Cells["User.PageID"].get_ResultStr(0);
             


                Dictionary<string, Visio.Page> oDictPagesToDuplicate = new Dictionary<string, Visio.Page>();
                ovDocument.Application.EventsEnabled = 0;
                //// 1️⃣ Find highest numeric page name
                foreach (Visio.Page ovPageToAdd in oDictPages.Values.OrderBy(p => p.Index))
                {
                    Visio.Page ovNewPage = ovPageToAdd.Duplicate();

                    //    // Move to end
                    ovNewPage.Index = ovDocument.Pages.Count;
                    //    string sNewName = ovPage.Name + "-Duplicated";

                    //    ovNewPage.Name = sNewName;

                    oDictPagesToDuplicate.Add(ovNewPage.Name, ovNewPage);
                }
                ovDocument.Application.EventsEnabled = -1;

                VisioUtilities.Application.OnPageDuplicated(oDictPagesToDuplicate);

            }
            catch(Exception ex)
            {
                MessageBox.Show("Error in DuplicateMultiplePages " + ex.Message, "VisAssist");
            }
            //ovDocument.Application.EndUndoScope(iUndoScope, true);
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

        
        internal static void UpdatePageIDInDatabase(string sOldPageID, string sCurrentPageID, Visio.Page ovPage)
        {
            try
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
            catch(Exception ex)
            {
                MessageBox.Show("Error in UpdatePageIDInDatabase " + ex.Message, "VisAssist");
            }
        }

        

        internal static void FirstStepInStressTest(int iPageNumber, int iWireNumber)
        {
            try
            {


                Visio.Document ovCurrentDoc = Globals.ThisAddIn.Application.ActiveDocument;

                Visio.Document ovStencilDoc = null;

                // Find the stencil containing the SmartWire master
                foreach (Visio.Document ovDoc in Globals.ThisAddIn.Application.Documents)
                {
                    if (ovDoc.Type == Visio.VisDocumentTypes.visTypeStencil &&
                        ovDoc.Name.Equals("TestStencil.vssx", StringComparison.OrdinalIgnoreCase))
                    {
                        ovStencilDoc = ovDoc;
                        break;
                    }
                }

                if (ovStencilDoc == null)
                {
                    MessageBox.Show("Stencil 'TestStencil.vssx' not found.", "VisAssist");
                    return;
                }

                // Get the SmartWire master
                Visio.Master ovWireMaster = ovStencilDoc.Masters["SmartWire"];



                if (ovCurrentDoc != null)
                {
                    // Add 100 Visio pages
                    for (int ithPage = 1; ithPage <= iPageNumber; ithPage++)
                    {
                        Visio.Page ovPage = ovCurrentDoc.Pages.Add();

                        double dX = 1.0;

                        // Start at the top of the page
                        double dY = ovPage.PageSheet.CellsU["PageHeight"].ResultIU - 0.5;

                        // Drop 10 wires on the page
                        for (int i = 0; i < iWireNumber; i++)
                        {
                            Visio.Shape ovShape = ovPage.Drop(ovWireMaster, dX, dY);

                            // Move Y down by the shape's actual height + small gap
                            double dHeight = ovShape.Cells["Height"].ResultIU;
                            dY -= dHeight + 0.2;
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show("Error in FirstStepInStressTest " + ex.Message, "VisAssist");
            }

        }

        internal static void SecondStepInStressTest(int numberOfPages, int wiresPerPage)
        {
            try
            {

                //add 50 pages and then go through all the pages and move all the secondary wires in the document to these 50 pages (there should be 1000 secondary wires so split them accordingly..)
                Visio.Document ovCurrentDoc = Globals.ThisAddIn.Application.ActiveDocument;
                Globals.ThisAddIn.m_bAskWhereToCutTo = false;

                if (ovCurrentDoc == null)
                    return;

                //const int numberOfPages = 50;
                //const int wiresPerPage = 20; // 1000 wires / 50 pages

                List<Visio.Shape> allSecondaryWires = new List<Visio.Shape>();

                // Collect all secondary wires in the document
                foreach (Visio.Page page in ovCurrentDoc.Pages)
                {
                    foreach (Visio.Shape ovShape in page.Shapes)
                    {
                        if (ovShape.CellExists["User.Class", 0] == -1)
                        {
                            if (ovShape.Cells["User.Class"].get_ResultStr(0) == "SmartWire")
                            {
                                //grab the seconary wire..
                                if (ovShape.Cells["User.WireRole"].get_ResultStr(0) == "S")
                                {
                                    allSecondaryWires.Add(ovShape);
                                }
                            }
                        }

                    }
                }

                if (allSecondaryWires.Count == 0)
                {
                    MessageBox.Show("No secondary wires found in the document.", "VisAssist");
                    return;
                }

                // Add 50 new pages
                List<Visio.Page> newPages = new List<Visio.Page>();
                for (int i = 0; i < numberOfPages; i++)
                {
                    Visio.Page newPage = ovCurrentDoc.Pages.Add();
                    newPages.Add(newPage);
                }

                int wireIndex = 0;

                foreach (Visio.Page page in newPages)
                {
                    double dX = 1.0;
                    double dY = page.PageSheet.CellsU["PageHeight"].ResultIU - 0.5;

                    for (int i = 0; i < wiresPerPage && wireIndex < allSecondaryWires.Count; i++)
                    {
                        Visio.Shape wire = allSecondaryWires[wireIndex];
                        wireIndex++;

                        // Cut the wire from its original page
                        Visio.Page originalPage = wire.ContainingPage;
                        Visio.Selection sel = originalPage.CreateSelection(
                            Visio.VisSelectionTypes.visSelTypeEmpty, 0, 0);
                        sel.Select(wire, (short)Visio.VisSelectArgs.visSelect);
                        sel.Cut();

                        // Paste onto the new page

                        page.Paste();

                    }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show("Error in SecondStepInStressTest " + ex.Message, "VisAssist");
            }
        }

        internal static void CutAndPasteShapes(PagesForm pagesForm)
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
            //get the current page so we can return to it...
            Visio.Page ovCurrentPage = ovApp.ActivePage;

            foreach (Visio.Page ovPage in ovDocument.Pages)
            {
                if (ovPage.Name == sPageNameToMoveTo)
                {
                    string sNewPageID = ovPage.PageSheet.Cells["User.PageID"].get_ResultStr(0);
                    //this is the page we want to paste what is in our clipboard...
                    int iUndoScope = ovApp.BeginUndoScope("Cut and Paste Action");
                    Globals.ThisAddIn.m_sLastUndoScope = "Cut and Paste Action";
                    //before we paste we need to make the cut of the current selection...
                    Visio.Selection ovSelection = ovApp.ActiveWindow.Selection;
                    ovApp.EventsEnabled = 0;
                    ovSelection.Cut();
                    ovPage.Paste();
                    ovApp.EventsEnabled = -1;
                    //need to update in db...
                    //get the selection we just pasted..
                    ovApp.ActiveWindow.Page = ovPage;
                    ovSelection = ovApp.ActiveWindow.Selection;
                    foreach (Visio.Shape ovShape in ovSelection)
                    {
                        if (ovShape.CellExists["User.Class", 0] == -1)
                        {
                            if (ovShape.Cells["User.Class"].get_ResultStr(0) == "SmartWire")
                            {
                                //check to see if the mate is in the selection becuase otherwise calling UpdateWireInDatabase and getting UpdateWireGridLocation won't fully update because the shape isn't updated in the database yet...
                                string sWirePairID = ovShape.Cells["User.WirePairID"].get_ResultStr(0);
                                bool bMateInSelection = false;
                                foreach(Visio.Shape ovMateShape in ovSelection)
                                {
                                    if(ovMateShape.Name != ovShape.Name)
                                    {
                                        if (ovMateShape.Cells["User.WirePairID"].get_ResultStr(0) == sWirePairID)
                                        {
                                            //this is the mate shape 
                                            bMateInSelection = true;
                                        }
                                    }
                                }

                                WireUtilities.UpdateWireInDatabase(ovShape, bMateInSelection);
                            }
                        }
                        
                    }

                    ovApp.ActiveWindow.Page = ovCurrentPage;


                    ovApp.EndUndoScope(iUndoScope, true);


                }
            }
        }

        internal static void OtherPagesToDuplicate(Visio.Page ovPageToDuplicate)
        {
            //gather all the pages to duplicate (if there is a wire on ovPageToDuplicate and its mate is on a different page add the mates page..)
            try
            {
                foreach(Visio.Shape ovShape in ovPageToDuplicate.Shapes)
                {
                    if (ovShape.CellExists["User.Class",0] == -1)
                    {
                        if (ovShape.Cells["User.Class"].get_ResultStr(0) == "SmartWire")
                        {

                        }
                    }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show("Error in OtherpagesToDuplicate " + ex.Message, "VisAssist");
            }
        }

        internal static void UpdatePageIndexInDB(string sPageID, int iPageIndex)
        {
            try
            {

                //update the entry record in pages_table where sPageID and update the pageID to sCurrentPageID

                using (SQLiteConnection sqliteconConnection = new SQLiteConnection(DatabaseConfig.ConnectionString))
                {
                    sqliteconConnection.Open();

                    string sSql = @"UPDATE pages_table SET PageIndex = @NewPageIndex WHERE PageID = @PageID";


                    using (SQLiteCommand sqlitecmdCommand = new SQLiteCommand(sSql, sqliteconConnection))
                    {
                        sqlitecmdCommand.Parameters.AddWithValue("@NewPageIndex", iPageIndex);
                        sqlitecmdCommand.Parameters.AddWithValue("@PageID", sPageID);

                        sqlitecmdCommand.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in UpdatePageIndexInDB " + ex.Message, "VisAssist");
            }
        }
    }

}
