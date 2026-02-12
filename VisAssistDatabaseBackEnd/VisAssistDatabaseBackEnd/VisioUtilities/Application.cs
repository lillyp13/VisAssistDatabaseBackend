using Microsoft.Office.Interop.Visio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Visio = Microsoft.Office.Interop.Visio;
using VisAssistDatabaseBackEnd.DataUtilities;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace VisAssistDatabaseBackEnd.VisioUtilities
{
    internal class Application
    {

        //PAGE LEVEL EVENTS
        internal static void OnPageAdded(Visio.Page ovVisioPage)
        {
            try
            {
                string sVisAssistFolderPath = FileUtilities.GetFolderPath(ovVisioPage.Document);
                DatabaseConfig.BindToActiveDocument(sVisAssistFolderPath);
                string sProjectID = ovVisioPage.Document.DocumentSheet.Cells["User.ProjectID"].get_ResultStr(0);

                //add page user cells...
                bool bAdded = PageUtilities.AddUserCellsToPage(ovVisioPage);
                if (bAdded)
                {
                    //we haven't added the page yet...
                    PageUtilities.AddPageToDatabase(ovVisioPage, sProjectID, "Visio");
                }
                else
                {
                    //check to see if this is an undo/ by seeing if the pages ID exists in the db
                    string sPageID = ovVisioPage.PageSheet.Cells["User.PageID"].get_ResultStr(0);
                    if (sPageID != "")
                    {
                        bool bDoesRecordExist = DatabaseUtilities.DoesRecordExist(DatabaseUtilities.SqlTables.PagesTable.sPagesTable, sPageID);
                        if (!bDoesRecordExist)
                        {
                            //need to add the page to the database...
                            PageUtilities.AddPageToDatabase(ovVisioPage, sProjectID, "Visio");
                            //will need to also add all the shapes on the page back to the db...
                            ShapesUtilities.AddShapesToDatabase(ovVisioPage, sProjectID);
                        }
                        else
                        {
                            //I THINK THIS IS DEAD CODE NOW BECAUSE WE ARE UPDATING THE ID BEFORE THIS...

                            //the user has duplicated a page using visio...
                            //we need to assign this page a new id and add it to the db as well as upate the ids of the shapes on the page...
                            string sFileID = ovVisioPage.Document.DocumentSheet.Cells["User.FileID"].get_ResultStr(0);



                            //i need to do this after as a processdelayed event i think....
                            // FileUtilities.UpdatePageAndShapeIDs(ovVisioPage, sProjectID, sFileID);
                            //now that all the ids are updated, we need to add the page and the shpaes...
                            PageUtilities.AddPageToDatabase(ovVisioPage, sProjectID, "New");
                            //ShapesUtilities.AddShapesToDatabase(ovVisioPage, sProjectID);

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in OnPageAdded " + ex.Message, "VisAssist");
            }

        }

        internal static void OnPageDuplicated(Visio.Page ovPage)
        {
            try
            {
                string sProjectID = ovPage.Document.DocumentSheet.Cells["User.ProjectID"].get_ResultStr(0);
                string sFileID = ovPage.Document.DocumentSheet.Cells["User.FileID"].get_ResultStr(0);

                string sNewPageID = PageUtilities.GeneratePageID(sProjectID, sFileID, ovPage.Name, DateTime.Now);
                //if we are not doing a redo/undo...otherwise the formula will be correct...
                if(!Globals.ThisAddIn.Application.IsUndoingOrRedoing)
                {
                    ovPage.PageSheet.Cells["User.PageID"].Formula = VisioUtilities.Application.FormatStringForVisio(sNewPageID);
                }
               


                //need to update the shapes ids as well...
                foreach (Visio.Shape ovShape in ovPage.Shapes)
                {
                    if (ovShape.CellExists["User.Class", 0] == -1)
                    {
                        //this is one of our shapes..
                        ovShape.Cells["User.PageID"].Formula = VisioUtilities.Application.FormatStringForVisio(sNewPageID);
                        string sNewShapeID = ShapesUtilities.GenerateShapeID(sProjectID, sFileID, sNewPageID, ovShape.Name, DateTime.Now);
                        ovShape.Cells["User.ShapeID"].Formula = VisioUtilities.Application.FormatStringForVisio(sNewShapeID);

                        //add the shapes to m_pendingShapeIDs (so that onshapeadded doesn't get fired for them...)
                        string sKey = ovShape.ID + "|" + ovShape.ContainingPage.Name;
                        Globals.ThisAddIn.m_pendingShapeIds.Add(sKey);
                    }
                }
                //add a delayed event that will switch the duplicate bool to be false..
                DelayedEvent oDelayedEvent = new DelayedEvent();
                oDelayedEvent.ovDocument = ovPage.Document;
                oDelayedEvent.sOperationType = "TurnOffDuplicateBool";
                Globals.ThisAddIn.m_delayedEvents.Add(oDelayedEvent);


                VisAssistDatabaseBackEnd.VisioUtilities.Application.OnPageAdded(ovPage);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in OnPageDuplicated " + ex.Message, "VisAssist");
            }
        }
        internal static void OnPageChanged(Visio.Page ovVisioPage)
        {
            try
            {


                string sVisAssistFolderPath = FileUtilities.GetFolderPath(ovVisioPage.Document);
                DatabaseConfig.BindToActiveDocument(sVisAssistFolderPath);
                string sProjectID = ovVisioPage.Document.DocumentSheet.Cells["User.ProjectID"].get_ResultStr(0);

                PageUtilities.UpdatePageInDatabase(ovVisioPage, sProjectID);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in OnPageChanged " + ex.Message, "VisAssist");
            }
        }

        internal static void OnDocumentChanged(Visio.Document ovDocument)
        {
            //this is for page index changed: user has dragged the page and changed the DOCUMENT order
            //it will probably also be used for when a shape has moved (grid location stuff...)
            try
            {
                //is the only time this gets called when the user changes the page index by dragging pages around?
                //should we just update each page in the document?
                string sProjectID = ovDocument.DocumentSheet.Cells["User.ProjectID"].get_ResultStr(0);
                string sLastPageID = "";
                foreach (Visio.Page ovPage in ovDocument.Pages)
                {
                    //before we go upate the page in the database, we want to check if the pageID is the same as the last pages PageID 
                    //this would mean we are doing a duplicate page..
                    string sPageID = ovPage.PageSheet.Cells["User.PageID"].get_ResultStr(0);

                    if (sLastPageID != "")
                    {
                        //need to see if this is a duplicate...
                        if (sLastPageID != sPageID)
                        {
                            PageUtilities.UpdatePageInDatabase(ovPage, sProjectID);
                            sLastPageID = ovPage.PageSheet.Cells["User.PageID"].get_ResultStr(0);
                        }
                    }
                    else
                    {
                        PageUtilities.UpdatePageInDatabase(ovPage, sProjectID);
                        sLastPageID = ovPage.PageSheet.Cells["User.PageID"].get_ResultStr(0);
                    }

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in OnDocumentChanged " + ex.Message, "VisAssist");
            }
        }

        internal static void OnPageDeleted(Visio.Page ovPage)
        {
            try
            {

                //user is deleting the page
                string sProjectID = ovPage.Document.DocumentSheet.Cells["User.ProjectID"].get_ResultStr(0);

                if (ovPage.PageSheet.CellExists["User.PageID", 0] == -1)
                {

                    string sPageID = ovPage.PageSheet.Cells["User.PageID"].get_ResultStr(0);


                    //check if the pageID has already been removed from the db...
                    bool bRecordExists = DatabaseUtilities.DoesRecordExist(DatabaseUtilities.SqlTables.PagesTable.sPagesTable, sPageID);

                    if (bRecordExists)
                    {
                        //we need to see if the id matches the pages name-we could have duplicated a page and then want to undo it so the page would have the same id as the orignal duplicated one...
                        string sPageNameInDB = PageUtilities.GetColumnInfoInPagesTableFromDatabase("PageName", sPageID);
                        if (sPageNameInDB == ovPage.Name)
                        {
                            PageUtilities.DeletePageInDatabase(ovPage, sProjectID);

                            //have a delayed event that will call ondocumentchanged...
                            DelayedEvent oDelayedEvent = new DelayedEvent();
                            oDelayedEvent.sOperationType = "OnDocumentChanged";
                            oDelayedEvent.ovDocument = ovPage.Document;
                            Globals.ThisAddIn.m_delayedEvents.Add(oDelayedEvent);
                        }
                        else
                        {
                            //this is from an undo after adding a page by duplicating it (therefore it has a PageID but the wrong pageID...)
                            string sVisAssistFolderPath = FileUtilities.GetFolderPath(ovPage.Document);
                            //populate the list of pages in the document 
                            List<string> lstPages = PageUtilities.PopulateVisioPageList(ovPage.Document);

                            DatabaseUtilities.CheckPageExistenceInVisio(ovPage.Document, ref lstPages);
                        }

                    }
                }
                else
                {
                    //this if from an undo of adding a page, where we don't have the User.PageID..
                    string sVisAssistFolderPath = FileUtilities.GetFolderPath(ovPage.Document);
                    //populate the list of pages in the document 
                    List<string> lstPages = PageUtilities.PopulateVisioPageList(ovPage.Document);

                    DatabaseUtilities.CheckPageExistenceInVisio(ovPage.Document, ref lstPages);
                }


            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in OnPageDeleted " + ex.Message, "VisAssist");
            }
        }


        //SHAPE LEVEL EVENTS
        internal static void OnShapeAdded(Visio.Shape ovShape)
        {
            if (ovShape.CellExists["User.Class", 0] == -1)
            {
                //this is one of our shapes...
                string sVisAssistFolderPath = FileUtilities.GetFolderPath(ovShape.ContainingPage.Document);
                DatabaseConfig.BindToActiveDocument(sVisAssistFolderPath);
                string sProjectID = ovShape.ContainingPage.Document.DocumentSheet.Cells["User.ProjectID"].get_ResultStr(0);

                string sClass = ovShape.Cells["User.Class"].get_ResultStr(0);
                switch (sClass)
                {
                    case "NewWire":
                    case "SmartWire":
                        {
                            ShapesUtilities.AddWireToDatabase(ovShape);
                            break;
                        }
                    case "TerminalBlock":
                        {
                            ShapesUtilities.AddTerminalBlockToDatabase(ovShape);
                            break;
                        }
                    case "ADC End Device":
                        {
                            ShapesUtilities.AddWiringEndDeviceToDatabase(ovShape);
                            break;
                        }
                }


            }
        }
        internal static void OnShapeDeleted(Visio.Shape ovShape)
        {

            if (ovShape.CellExists["User.Class", 0] == -1)
            {
                //this is one of our shapes...
                string sVisAssistFolderPath = FileUtilities.GetFolderPath(ovShape.ContainingPage.Document);
                DatabaseConfig.BindToActiveDocument(sVisAssistFolderPath);
                string sProjectID = ovShape.ContainingPage.Document.DocumentSheet.Cells["User.ProjectID"].get_ResultStr(0);



                string sClass = ovShape.Cells["User.Class"].get_ResultStr(0);
                switch (sClass)
                {
                    case "NewWire":
                    case "SmartWire":
                        {
                            if (Globals.ThisAddIn.Application.IsUndoingOrRedoing)
                            {
                                List<string> lstShapes = ShapesUtilities.PopulateShapesInDocument(ovShape.ContainingPage.Document, sClass);
                                //need to clean up the db based on the shapes on this page left...
                                DatabaseUtilities.CheckShapeExistenceInVisio(DatabaseUtilities.SqlTables.WireShapesTable.sWireShapeTable, ovShape.Document, ref lstShapes);
                            }
                            else
                            {
                                ShapesUtilities.DeleteWireFromDatabase(ovShape);
                            }


                            break;
                        }
                    case "TerminalBlock":
                        {
                            if (Globals.ThisAddIn.Application.IsUndoingOrRedoing)
                            {
                                List<string> lstShapes = ShapesUtilities.PopulateShapesInDocument(ovShape.ContainingPage.Document, sClass);
                                //need to clean up the db based on the shapes on this page left...
                                DatabaseUtilities.CheckShapeExistenceInVisio(DatabaseUtilities.SqlTables.TerminalBlocksTable.sTerminalBlockTable, ovShape.Document, ref lstShapes);

                            }
                            else
                            {
                                ShapesUtilities.DeleteTerminalBlockFromDatabase(ovShape);
                            }

                            break;
                        }
                    case "ADC End Device":
                        {
                            if (Globals.ThisAddIn.Application.IsUndoingOrRedoing)
                            {
                                List<string> lstShapes = ShapesUtilities.PopulateShapesInDocument(ovShape.ContainingPage.Document, sClass);

                                //need to clean up the db based on the shapes on this page left...
                                DatabaseUtilities.CheckShapeExistenceInVisio(DatabaseUtilities.SqlTables.WiringEndDevice.sWiringEndDeviceTable, ovShape.Document, ref lstShapes);

                            }
                            else
                            {
                                ShapesUtilities.DeleteEndDeviceFromDatabase(ovShape);
                            }

                            break;
                        }
                }


            }
        }
        internal static void CellChanged(Visio.Cell ovCell)
        {
            try
            {

                //check what cell was changed...
                //if it was the x or y recalculate the grid location...

                Visio.Shape ovShape = ovCell.Shape;
                string sClass = "";
                if (ovShape.CellExists["User.Class", 0] == -1)
                {
                    //this is one of our shapes
                    //this is one of our shapes
                    sClass = ovShape.Cells["User.Class"].get_ResultStr(0);

                    switch (sClass)
                    {
                        case "TerminalBlock":
                            {
                                ShapesUtilities.UpdateTerminalBlockInDatabase(ovShape);

                                break;
                            }
                        case "SmartWire":
                            {
                                break;
                            }
                        case "ADC End Device":
                            {
                                ShapesUtilities.UpdateEndDeviceInDatabase(ovShape);
                                break;
                            }
                    }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show("Error in CellChanged " + ex.Message, "VisAssist");
            }


            //the pinx or piny was not the cell that changed so it was not a movement but an update in the shapes cell...

        }
        internal static void TextChanged(Shape ovShape)
        {
            try
            {
                //we need this because the text is no longer stored in a cell...
                string sClass = "";
                if (ovShape.CellExists["User.Class", 0] == -1)
                {
                    //this is one of our shapes
                    //this is one of our shapes
                    sClass = ovShape.Cells["User.Class"].get_ResultStr(0);
                    switch (sClass)
                    {
                        case "TerminalBlock":
                            {
                                ShapesUtilities.UpdateTerminalBlockInDatabase(ovShape);
                                break;
                            }
                    }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show("Error in TextChanged " + ex.Message, "VisAssist");
            }
        }


        //DOCUMENT LEVEL EVENTS
        internal static void ProcessThisDelayedEvent(DelayedEvent oThisDelayedEvent)
        {

            try
            {
                if (oThisDelayedEvent.sOperationType == "OnDocumentChanged")
                {
                    Visio.Document ovDocument = oThisDelayedEvent.ovDocument;
                    OnDocumentChanged(ovDocument);
                }

                //i think this delayed event is dead code now...
                if (oThisDelayedEvent.sOperationType == "UpdateIDs")
                {
                    //we want to update page and shape ids for the given page...
                    Visio.Document ovDocument = oThisDelayedEvent.ovDocument;
                    Visio.Page ovPage = oThisDelayedEvent.ovPage;

                    string sPageID = oThisDelayedEvent.sPageID;
                    
                    //Visio.Cell pageCell = ovPage.PageSheet.CellsU["User.PageID"];
                    //if (pageCell != null)
                    //{
                    //    pageCell.FormulaU = VisioUtilities.Application.FormatStringForVisio(sPageID);
                    //}
                    // try
                    // {

                    int undoScope = ovPage.Application.BeginUndoScope("Update PageID");
                    // Update all shapes' User.PageID without adding undo steps
                    foreach (Visio.Shape ovShape in ovPage.Shapes)
                    {
                        if (ovShape.CellExists["User.PageID", 0] == -1)
                        {
                            ovShape.Cells["User.PageID"].FormulaU = VisioUtilities.Application.FormatStringForVisio(sPageID);
                        }
                        
                    }
                    // }
                    // finally
                    // {
                    // false = do NOT add this scope to the undo stack
                    ovPage.Application.EndUndoScope(undoScope, false);
                    // }

                }

                if(oThisDelayedEvent.sOperationType == "TurnOffDuplicateBool")
                {
                    Globals.ThisAddIn.m_bIsPageDuplicating = false;
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in ProcessThisDelayedEvent " + ex.Message, "VisAssist");
            }

        }

        internal static void OnVisioIsIdle(Visio.Application subject)
        {
            Globals.ThisAddIn.m_pendingShapeIds.Clear();
            Globals.ThisAddIn.m_pendingPageIds.Clear();
            int iNumberOfDelayedEvents = Globals.ThisAddIn.m_delayedEvents.Count;

            if (iNumberOfDelayedEvents > 0)
            {


                try
                {

                    for (int ithEvent = iNumberOfDelayedEvents; ithEvent > 0; ithEvent--)
                    {
                        DelayedEvent thisDelayedEvent = Globals.ThisAddIn.m_delayedEvents[ithEvent - 1];
                        ProcessThisDelayedEvent(thisDelayedEvent);
                        Globals.ThisAddIn.m_delayedEvents.Remove(thisDelayedEvent);
                    }

                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error in OnVisioIsIdle " + ex.Message, "VisAssist");
                }
            }
        }

        //HELPER FUNCTION

        /// <summary>
        /// This function is used to properly format a string for use in a Visio cell or a SQL string.
        /// Note: This version pads " characters to each end of the string.
        static public string FormatStringForVisio(
            string sInputString)
        {
            // return result
            const string SINGLE_QUOTE = "\"";
            const string DOUBLE_QUOTES = "\"\"";

            if (string.IsNullOrEmpty(sInputString))
                return "\"\"";   // Visio empty string literal

            // replace each " char with double "" chars
            sInputString = sInputString.Replace(SINGLE_QUOTE, DOUBLE_QUOTES);
            sInputString = "\"" + sInputString + "\"";

            // return result
            return sInputString;
        }

    }
}
