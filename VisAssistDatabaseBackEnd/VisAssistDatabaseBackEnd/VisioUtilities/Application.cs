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
using VisAssistDatabaseBackEnd.ShapeUtilities;
using VisAssistDatabaseBackEnd.ShapeUtilities.Wire;
using System.Xml.Linq;
using System.Configuration;
using VisAssistDatabaseBackEnd.Forms;
using Microsoft.Office.Core;

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
                            List<string> oListPages = new List<string>();
                            oListPages.Add(ovVisioPage.Name);
                            //will need to also add all the shapes on the page back to the db...check if this gets called when copying a file...
                            //add this as a delayed event becuase if this si coming from an undo the shapes may not be on the page yet...

                            //ShapesUtilities.AddShapesToDatabase(oListPages, sProjectID, ovVisioPage.Document);
                            DelayedEvent existingEvent = Globals.ThisAddIn.m_delayedEvents.FirstOrDefault(e => e.sOperationType == "AddShapesToDatabase" && e.ovDocument == ovVisioPage.Document);

                            if (existingEvent != null)
                            {
                                // Only add page if not already in list
                                if (!existingEvent.oListPages.Contains(ovVisioPage.Name))
                                {
                                    existingEvent.oListPages.Add(ovVisioPage.Name);
                                }

                            }
                            else
                            {
                                DelayedEvent oDelayedEvent = new DelayedEvent();
                                oDelayedEvent.ovDocument = ovVisioPage.Document;
                                oDelayedEvent.sOperationType = "AddShapesToDatabase";
                                if (oDelayedEvent.oListPages == null)
                                {
                                    oDelayedEvent.oListPages = new List<string>();
                                }
                                oDelayedEvent.oListPages.Add(ovVisioPage.Name);

                                Globals.ThisAddIn.m_delayedEvents.Add(oDelayedEvent);
                            }

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

                            DelayedEvent existingEvent = Globals.ThisAddIn.m_delayedEvents.FirstOrDefault(e => e.sOperationType == "AddShapesToDatabase" && e.ovDocument == ovVisioPage.Document);

                            if (existingEvent != null)
                            {
                                // Only add page if not already in list
                                if (!existingEvent.oListPages.Contains(ovVisioPage.Name))
                                {
                                    existingEvent.oListPages.Add(ovVisioPage.Name);
                                }

                            }
                            else
                            {
                                DelayedEvent oDelayedEvent = new DelayedEvent();
                                oDelayedEvent.ovDocument = ovVisioPage.Document;
                                oDelayedEvent.sOperationType = "AddShapesToDatabase";
                                if (oDelayedEvent.oListPages == null)
                                {
                                    oDelayedEvent.oListPages = new List<string>();
                                }
                                oDelayedEvent.oListPages.Add(ovVisioPage.Name);

                                Globals.ThisAddIn.m_delayedEvents.Add(oDelayedEvent);
                            }


                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in OnPageAdded " + ex.Message, "VisAssist");
            }

        }

        internal static void OnPageDuplicated(Dictionary<string, Visio.Page> oDictPagesToDuplicate)
        {
            try
            {
                Dictionary<string, Visio.Shape> oDictWires = new Dictionary<string, Visio.Shape>();
                string sProjectID = "";
                string sFileID = "";
                string sNewPageID = "";
                foreach (Visio.Page ovPageToDuplicate in oDictPagesToDuplicate.Values)
                {

                    sProjectID = ovPageToDuplicate.Document.DocumentSheet.Cells["User.ProjectID"].get_ResultStr(0);
                    sFileID = ovPageToDuplicate.Document.DocumentSheet.Cells["User.FileID"].get_ResultStr(0);
                    sNewPageID = PageUtilities.GeneratePageID(sProjectID, sFileID, ovPageToDuplicate.Name, DateTime.Now);
                    //if we are not doing a redo/undo...otherwise the formula will be correct...
                    if (!Globals.ThisAddIn.Application.IsUndoingOrRedoing)
                    {
                        ovPageToDuplicate.Application.EventsEnabled = 0;
                        ovPageToDuplicate.PageSheet.Cells["User.PageID"].Formula = VisioUtilities.Application.FormatStringForVisio(sNewPageID);
                        ovPageToDuplicate.Application.EventsEnabled = -1;
                    }

                    foreach (Visio.Shape ovShape in ovPageToDuplicate.Shapes)
                    {
                        if (ovShape.CellExists["User.Class", 0] == -1)
                        {
                            string sClass = ovShape.Cells["User.Class"].get_ResultStr(0);
                            if (sClass == "SmartWire")
                            {
                                string sKey = ovShape.ID + "|" + ovShape.ContainingPage.Name;
                                oDictWires.Add(sKey, ovShape);
                            }
                        }
                    }

                }
                WireUtilities.CheckForWirePairsOnPageDuplicated(oDictWires);
                List<string> oListWirePairIDUpdated = new List<string>();
                foreach (Visio.Page ovPageToDuplicate in oDictPagesToDuplicate.Values)
                {

                    if (!Globals.ThisAddIn.Application.IsUndoingOrRedoing)
                    {
                        //need to update the shapes ids as well...
                        foreach (Visio.Shape ovShape in ovPageToDuplicate.Shapes)
                        {
                            if (ovShape.CellExists["User.Class", 0] == -1)
                            {
                                string sClass = ovShape.Cells["User.Class"].get_ResultStr(0);
                                if (sClass != "SmartWire")
                                {

                                    //i don't want to update the wires ids right away because I need to do a little bit of more research on them...
                                    //this is one of our shapes..
                                    //ovShape.Cells["User.PageID"].Formula = VisioUtilities.Application.FormatStringForVisio(sNewPageID);
                                    string sNewShapeID = ShapesUtilities.GenerateShapeID(sProjectID, sFileID, sNewPageID, ovShape.Name, DateTime.Now);
                                    ovShape.Cells["User.ShapeID"].Formula = VisioUtilities.Application.FormatStringForVisio(sNewShapeID);



                                    //add the shapes to m_pendingShapeIDs (so that onshapeadded doesn't get fired for them...)
                                    string sKey = ovShape.ID + "|" + ovShape.ContainingPage.Name;
                                    Globals.ThisAddIn.m_pendingShapeIds.Add(sKey); //probaobly don't need to do this becuase now i am looking if we are duplicating a page in OnShapeAdded...
                                }
                                else
                                {
                                    //gather all the wires on the page to be duplicated...

                                }

                            }
                        }
                    }
                    //add a delayed event that will switch the duplicate bool to be false..
                    DelayedEvent oDelayedEvent = new DelayedEvent();
                    oDelayedEvent.ovDocument = ovPageToDuplicate.Document;
                    oDelayedEvent.sOperationType = "TurnOffDuplicateBool";
                    Globals.ThisAddIn.m_delayedEvents.Add(oDelayedEvent);


                    //before we call OnPageAdded and add all the shapes to the db we need to do something with the wires...i need to determine which wires are paired with which wires...
                    //check the oDictWires to see what kind of pairs we have...
                    //i will create a new collection based on the pairs that i have in oDictWires


                    VisAssistDatabaseBackEnd.VisioUtilities.Application.OnPageAdded(ovPageToDuplicate);

                    //add a delayed event for ondocumentchanged so that the page index are all correct...
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in OnPageDuplicated " + ex.Message, "VisAssist");

            }
            finally
            {
                Globals.ThisAddIn.Application.EventsEnabled = -1;
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
                            WireUtilities.UpdateWiresInDatabase(ovPage);
                            sLastPageID = ovPage.PageSheet.Cells["User.PageID"].get_ResultStr(0);
                        }
                    }
                    else
                    {
                        PageUtilities.UpdatePageInDatabase(ovPage, sProjectID);
                        WireUtilities.UpdateWiresInDatabase(ovPage);
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
                        //before we deelte the page from the database we need to see if we need to delete any wire mates that don't live on the page we are deleting...
                        WireUtilities.CheckForWireMateOnPageDelete(ovPage, sPageID);

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
                            //I THINK THIS IS DEAD CODE BECAUSE WE HANDLE UNDO DIFFERENT WHEN DELETING PAGES..2/12/2026
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
                    //I THINK THIS IS DEAD CODE BECAUSE WE HANDLE UNDO DIFFERENT WHEN DELETING PAGES..2/12/2026
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
        internal static void OnShapeAdded(Visio.Shape ovShape, Visio.Selection ovSelection, ref Dictionary<string, Visio.Shape> oDictWiresComingFromRedo)
        {
            try
            {


                if (ovShape.CellExists["User.Class", 0] == -1)
                {
                    //this is one of our shapes...
                    string sVisAssistFolderPath = FileUtilities.GetFolderPath(ovShape.ContainingPage.Document);
                    DatabaseConfig.BindToActiveDocument(sVisAssistFolderPath);
                    string sProjectID = ovShape.ContainingPage.Document.DocumentSheet.Cells["User.ProjectID"].get_ResultStr(0);
                    List<string> lstWires = new List<string>();
                    string sClass = ovShape.Cells["User.Class"].get_ResultStr(0);
                    switch (sClass)
                    {
                        case "NewWire":
                        case "SmartWire":
                            {
                                if (Globals.ThisAddIn.Application.IsUndoingOrRedoing)
                                {
                                    //we are doing an undo/redo

                                    string sShapeID = ovShape.Cells["User.ShapeID"].get_ResultStr(0);

                                    //we need to make sure the sShapeID exists in the table to update..
                                    bool bDoesRecordExist = DatabaseUtilities.DoesRecordExist(DatabaseUtilities.SqlTables.WireShapesTable.sWireShapeTable, sShapeID);
                                    if (bDoesRecordExist)
                                    {
                                        //this is from undoing a ctrl x...that is why it still lives in the db
                                        WireUtilities.UpdateWireInDatabase(ovShape, false);
                                    }
                                    else
                                    {
                                        //this is part of an undo/redo and the record doesn't exist which means it is not in the db..
                                        //check how many shapes we are adding (we should be pairing these wires together and if there is an odd number drop antoher wire?
                                        oDictWiresComingFromRedo.Add(sShapeID, ovShape);

                                    }





                                }
                                else
                                {
                                    //TEMPORARILY ADD THE USER.WIREPAIRID 
                                    if (ovShape.CellExists["User.WirePairID", 0] == 0)
                                    {
                                        ovShape.AddNamedRow((short)Visio.VisSectionIndices.visSectionUser, "WirePairID", 0);
                                    }
                                    string sShapeID = ovShape.Cells["User.ShapeID"].get_ResultStr(0);
                                    bool bNewWire = true;
                                    if(ovShape.Application.CurrentScope == 1024)
                                    {
                                        //this is a duplicate... we don't want to increase the color or number...
                                        bNewWire = false;
                                    }
                                    WireUtilities.AddWire(ovShape, ref lstWires, bNewWire); //lstWires here should be empty we utiltize this when we use addwiretodatabase during the sync...
                                }

                                break;
                            }
                        case "TerminalBlock":
                            {
                                TerminalBlockUtilities.AddTerminalBlockToDatabase(ovShape);
                                break;
                            }
                        case "ADC End Device":
                            {
                                EndDeviceUtilities.AddWiringEndDeviceToDatabase(ovShape);
                                break;
                            }
                    }

                    //we should add a delayed event to clear the clipbaord after adding shapes...
                    DelayedEvent oDelayedEvent = new DelayedEvent();
                    oDelayedEvent.sOperationType = "TurnOfCutShapesBool";
                    oDelayedEvent.ovDocument = ovShape.Document;
                    Globals.ThisAddIn.m_delayedEvents.Add(oDelayedEvent);


                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in OnShapeAdded " + ex.Message, "VisAssist");
            }
        }

       
        internal static List<string> OnShapeDeleted(string sShapeID, string sWireRole, Visio.Document ovDocument, string sClass)
        {
            //when we are in this method we know we are not doing an undo or redo..
            List<string> lstShapesRemoved = new List<string>();
            try
            {

                //this is one of our shapes...
                string sVisAssistFolderPath = FileUtilities.GetFolderPath(ovDocument);
                DatabaseConfig.BindToActiveDocument(sVisAssistFolderPath);
                string sProjectID = ovDocument.DocumentSheet.Cells["User.ProjectID"].get_ResultStr(0);




                switch (sClass)
                {
                    case "NewWire":
                    case "SmartWire":
                        {
                            if (Globals.ThisAddIn.Application.IsUndoingOrRedoing)
                            {
                                List<string> lstShapes = ShapesUtilities.PopulateShapesInDocument(ovDocument, sClass);
                                //need to clean up the db based on the shapes on this page left...

                                lstShapesRemoved = DatabaseUtilities.CheckShapeExistenceInVisio(DatabaseUtilities.SqlTables.WireShapesTable.sWireShapeTable, ovDocument, ref lstShapes);
                            }
                            else
                            {
                                //we also need to go and delete th secondary wire (wherever it lives...)
                                //gather the seconary information before deleting it from the db..

                                string sMateID = "";
                                string sMatePageID;
                                string sWirePairID = WireUtilities.GetColumnInfoInWireShapesTableFromDatabase("WirePairID", sShapeID);

                                if (sWirePairID != "")
                                {
                                    //if the wirepairid is empty we alrady deleted it...
                                   
                                    switch (sWireRole)
                                    {
                                        case "P":
                                            {

                                                sMateID = WireUtilities.GetColumnInfoInWirePairsTableFromDatabase("SecondaryWireID", sWirePairID);
                                                break;
                                            }
                                        case "S":
                                            {
                                                sMateID = WireUtilities.GetColumnInfoInWirePairsTableFromDatabase("PrimaryWireID", sWirePairID);
                                                break;
                                            }
                                    }

                                    //get the page id the wire mate lives on..
                                    sMatePageID = WireUtilities.GetColumnInfoInWireShapesTableFromDatabase("PageID", sMateID);

                                    //WireUtilities.DeleteWireFromDatabaseUsingShape(ovShape);
                                    WireUtilities.DeleteWireFromDatabase(sShapeID);

                                    //check if the mates is in the selection already scheduled to be deleted...
                                    bool bDeleteMateShape = true;
                                    if (!Globals.ThisAddIn.m_lstWireIDs.Contains(sMateID))
                                    {

                                        string sPageIndex = PageUtilities.GetColumnInfoInPagesTableFromDatabase("PageIndex", sMatePageID);
                                        int iPageIndex = Convert.ToInt32(sPageIndex);
                                        Visio.Page ovMatePage = ovDocument.Pages[iPageIndex];
                                        foreach (Visio.Shape ovMateShape in ovMatePage.Shapes)
                                        {
                                            if (ovMateShape.CellExists["User.ShapeID", 0] == -1)
                                            {
                                                if (ovMateShape.Cells["User.ShapeID"].get_ResultStr(0) == sMateID)
                                                {
                                                    ovDocument.Application.EventsEnabled = 0;
                                                    ovMateShape.Delete();
                                                    ovDocument.Application.EventsEnabled = -1;
                                                }
                                            }
                                        }
                                    }

                                }

                              
                            }


                            break;
                        }
                    case "TerminalBlock":
                        {
                            
                             TerminalBlockUtilities.DeleteTerminalBlockFromDatabase(sShapeID);
                            

                            break;
                        }
                    case "ADC End Device":
                        {
                            
                             EndDeviceUtilities.DeleteEndDeviceFromDatabase(sShapeID);
                            

                            break;
                        }



                }

                return lstShapesRemoved;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in OnShapeDeleted " + ex.Message, "VisAssist");
                return lstShapesRemoved;
            }
            finally
            {
                ovDocument.Application.EventsEnabled = -1;
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
                                TerminalBlockUtilities.UpdateTerminalBlockInDatabase(ovShape);

                                break;
                            }
                        case "SmartWire":
                            {
                                WireUtilities.UpdateWireInDatabase(ovShape, false);
                                break;
                            }
                        case "ADC End Device":
                            {
                                EndDeviceUtilities.UpdateEndDeviceInDatabase(ovShape);
                                break;
                            }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in CellChanged " + ex.Message, "VisAssist");
            }


            //the pinx or piny was not the cell that changed so it was not a movement but an update in the shapes cell...

        }
        internal static void TextChanged(Visio.Shape ovShape)
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
                                TerminalBlockUtilities.UpdateTerminalBlockInDatabase(ovShape);
                                break;
                            }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in TextChanged " + ex.Message, "VisAssist");
            }
        }
        internal static void OnWireShapeCut(Visio.Shape ovShape)
        {
            //we notice the user cut and paste a wire..we need to update in db where this wire exists as well as its grid location in the mates wire...
            WireUtilities.UpdateWireInDatabase(ovShape, false);
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
                    string sProjectID = ovDocument.DocumentSheet.Cells["User.ProjectID"].get_ResultStr(0);
                    string sPageID = oThisDelayedEvent.sPageID;

                    // Visio.Cell pageCell = ovPage.PageSheet.CellsU["User.PageID"];
                    try
                    {
                        string sPageName = ovPage.Name;
                    }
                    catch
                    {
                        return;
                    }
                    string sCurrentPageID = ovPage.PageSheet.Cells["User.PageID"].get_ResultStr(0);
                    //update the sPageID in the db to be sCurrentPageID
                    //update the entry record in pages_table where sPageID and update the pageID to sCurrentPageID

                    PageUtilities.UpdatePageIDInDatabase(sPageID, sCurrentPageID, ovPage);

                    PageUtilities.UpdatePageIndexInDB(sCurrentPageID, ovPage.Index);
                }

                if (oThisDelayedEvent.sOperationType == "TurnOffDuplicateBool")
                {
                    Globals.ThisAddIn.m_bIsPageDuplicating = false;
                }

                if (oThisDelayedEvent.sOperationType == "TurnOfCutShapesBool")
                {
                    Globals.ThisAddIn.m_bIsCuttingShape = false;
                    Clipboard.Clear();
                }

                if (oThisDelayedEvent.sOperationType == "TurnOffSyncDBBool")
                {
                    Globals.ThisAddIn.m_SyncedDB = false;
                }

                if (oThisDelayedEvent.sOperationType == "AddWiresToDB")
                {
                    //there are wires we need to readd to the db...
                    //i have a dictionary of shapes and they are paired with each other so if i have four shapes 1 and 2 are paire adn 3 and 4 are paired 
                    //loop through the dictionary of shapes to find these pairs and then add them to the db based on the info in the visio shape
                    Dictionary<string, Visio.Shape> oDictShapes = oThisDelayedEvent.oDictOfShapes;
                    List<Visio.Shape> lstShapes = oDictShapes.Values.ToList();
                    Visio.Document ovDocument = oThisDelayedEvent.ovDocument;
                    Dictionary<string, List<Visio.Shape>> oDictWirePairs = new Dictionary<string, List<Visio.Shape>>();
                    foreach (Visio.Shape shape in lstShapes)
                    {
                        try
                        {
                            string sWirePairID = shape.CellsU["User.WirePairID"].ResultStr[Visio.VisUnitCodes.visUnitsString];

                            if (!oDictWirePairs.ContainsKey(sWirePairID))
                            {
                                oDictWirePairs[sWirePairID] = new List<Visio.Shape>();
                            }

                            oDictWirePairs[sWirePairID].Add(shape);
                        }
                        catch
                        {
                            Console.WriteLine($"Shape {shape.NameID} does not have User.WirePairID.");
                        }
                    }



                    // Loop through in steps of 2
                    foreach (KeyValuePair<string, List<Visio.Shape>> kvp in oDictWirePairs)
                    {
                        string pairId = kvp.Key;
                        List<Visio.Shape> shapes = kvp.Value;

                        if (shapes.Count != 2)
                        {
                            Console.WriteLine($"Warning: WirePairID '{pairId}' has {shapes.Count} shapes (expected 2).");
                            continue;
                        }

                        Visio.Shape ovShape1 = shapes[0];
                        Visio.Shape ovShape2 = shapes[1];

                        string sWirePairID = ovShape1.Cells["User.WirePairID"].get_ResultStr(0);
                        MultipleRecordUpdates mruShape1 = WireUtilities.BuildWireShapeInfo(ovShape1, sWirePairID, false);
                        MultipleRecordUpdates mruShape2 = WireUtilities.BuildWireShapeInfo(ovShape2, sWirePairID, false);
                        string sProjectID = ovDocument.DocumentSheet.Cells["User.ProjectID"].get_ResultStr(0);
                        string sFileID = ovDocument.DocumentSheet.Cells["User.FileID"].get_ResultStr(0);
                        string sPageID = mruShape1.ruRecords[0].odictColumnValues["PageID"];



                        //string sWirePairID = WireUtilities.GenerateWirePairID(sProjectID, sFileID, sPageID, ovShape1.Name, ovShape2.Name, DateTime.Now);
                        //mruShape1.ruRecords[0].odictColumnValues["WirePairID"] = sWirePairID;
                        //mruShape2.ruRecords[0].odictColumnValues["WirePairID"] = sWirePairID;

                        //determine which shape is the primary or secondary..
                        string sWireRole = mruShape1.ruRecords[0].odictColumnValues["WireRole"];
                        if (sWireRole == "P")
                        {
                            WireUtilities.AddWireToDatabase(mruShape1, mruShape2);
                        }
                        else
                        {
                            WireUtilities.AddWireToDatabase(mruShape2, mruShape1);
                        }


                    }


                    //once we are done we need to clear the dictionary...
                    Globals.ThisAddIn.oDictWiresComingFromRedo.Clear();
                }

                if (oThisDelayedEvent.sOperationType == "CheckPageExistence")
                {
                    Visio.Document ovDocument = oThisDelayedEvent.ovDocument;
                    string sVisAssistFolderPath = FileUtilities.GetFolderPath(ovDocument);
                    DatabaseConfig.BindToActiveDocument(sVisAssistFolderPath);
                    //we are doing a redo/undo that is causing a deletion of a page, however the pageid may not be the updated one because it reverts back to what is what duplicated from...
                    DatabaseUtilities.CheckPageExistence(ovDocument, sVisAssistFolderPath);
                }

                if (oThisDelayedEvent.sOperationType == "CheckShapeExistence")
                {
                    Visio.Document ovDocument = oThisDelayedEvent.ovDocument;
                    Visio.Page ovPage = oThisDelayedEvent.ovPage;

                    string sVisAssistFolderPath = FileUtilities.GetFolderPath(ovDocument);
                    DatabaseUtilities.CheckShapeExistence(ovDocument, sVisAssistFolderPath);
                }

                if (oThisDelayedEvent.sOperationType == "AddShapesToDatabase")
                {
                    Visio.Document ovDocument = oThisDelayedEvent.ovDocument;
                    string sProjectID = ovDocument.DocumentSheet.Cells["User.ProjectID"].get_ResultStr(0);
                    List<string> oListPages = oThisDelayedEvent.oListPages;

                    ShapesUtilities.AddShapesToDatabase(oListPages, sProjectID, ovDocument);
                }


                //THESE SHOULD NEVER BE ADDED....
                //if (oThisDelayedEvent.sOperationType == "Undo")
                //{
                //    Globals.ThisAddIn.Application.Undo();
                //    Globals.ThisAddIn.m_sLastUndoScope = "";
                //}
                //if (oThisDelayedEvent.sOperationType == "Redo")
                //{
                //    Globals.ThisAddIn.Application.Redo();
                //    Visio.Document ovDocument = oThisDelayedEvent.ovDocument;
                //    //check to see if the all the shapes in the db exist in the visio file and if they don't this is because we are redoing a cut and paste and need to subscribe to redo twice otherwise redoing once is fine...
                //    bool bRedoTwice = DatabaseUtilities.CheckShapeExistenceInVisioForRedoing(DatabaseUtilities.SqlTables.WireShapesTable.sWireShapeTable, ovDocument);
                //    if (bRedoTwice)
                //    {
                //        Globals.ThisAddIn.Application.Redo();
                //    }

                //    Globals.ThisAddIn.m_sLastUndoScope = "Cut and Paste Action";

                //    //will need to sync db with visio file
                //    Visio.Document ovDoc = oThisDelayedEvent.ovDocument;
                //    string sVisAssistFolderPath = FileUtilities.GetFolderPath(ovDoc);
                //    DatabaseUtilities.SyncDBWithFile(ovDoc, sVisAssistFolderPath);
                //}

                if (oThisDelayedEvent.sOperationType == "CheckShapeExistenceAfterUndoDelete")
                {
                    Visio.Document ovDocument = oThisDelayedEvent.ovDocument;
                    // bool bRedo = DatabaseUtilities.CheckShapeExistenceInVisioForRedoing(DatabaseUtilities.SqlTables.WireShapesTable.sWireShapeTable, ovDocument);
                    //check the amount of wires in the table (if we have an uneven amount we want to redo..)
                    string sFileID = ovDocument.DocumentSheet.Cells["User.FileID"].get_ResultStr(0);
                    List<string> lstWiresInDB = DatabaseUtilities.GetTableRecordCountForSpecificFile(DatabaseUtilities.SqlTables.WireShapesTable.sWireShapeTable, sFileID);
                    int iNumberOfRecords = lstWiresInDB.Count;
                    //get all the shapeids fromt he inumberofRecords... and check 
                    int iWiresInDoc = 0;
                    List<string> lstWiresInVisio = new List<string>();
                    foreach (Visio.Page ovPage in ovDocument.Pages)
                    {
                        if (ovPage.PageSheet.CellExists["User.PageID", 0] == -1)
                        {
                            foreach (Visio.Shape ovShape in ovPage.Shapes)
                            {
                                if (ovShape.CellExists["User.ShapeID", 0] == -1)
                                {
                                    if (ovShape.Cells["User.Class"].get_ResultStr(0) == "SmartWire")
                                    {
                                        iWiresInDoc++;
                                        lstWiresInVisio.Add(ovShape.Cells["User.WirePairID"].get_ResultStr(0));
                                    }
                                }
                            }
                        }
                    }
                   


                    //will need to sync db with visio file
                    Visio.Document ovDoc = oThisDelayedEvent.ovDocument;
                    string sVisAssistFolderPath = FileUtilities.GetFolderPath(ovDoc);
                    //DatabaseUtilities.SyncDBWithFile(ovDoc, sVisAssistFolderPath);
                    //get a list of wires in the doc
                    List<string> lstWiresInDoc = new List<string>();
                    foreach (Visio.Page ovPage in ovDoc.Pages)
                    {
                        if (ovPage.PageSheet.CellExists["User.PageID", 0] == -1)
                        {
                            foreach (Visio.Shape ovShape in ovPage.Shapes)
                            {
                                if (ovShape.CellExists["User.ShapeID", 0] == -1)
                                {
                                    if (ovShape.Cells["User.Class"].get_ResultStr(0) == "SmartWire")
                                    {
                                        lstWiresInDB.Add(ovShape.Cells["User.ShapeID"].get_ResultStr(0));
                                    }
                                }
                            }
                        }
                    }
                    DatabaseUtilities.SyncDBWithFile(ovDoc, sVisAssistFolderPath);
                    
                }

                if (oThisDelayedEvent.sOperationType == "UndoCut")
                {

                    //I think this is dead code...
                    Visio.Document ovDoc = oThisDelayedEvent.ovDocument;

                    ovDoc.Application.Undo();

                    //also need to recognize that if they do a redo this would be a cut...

                    //Visio.Window ovWin = ovDoc.Application.ActiveWindow;

                    //Visio.Page ovCurrentPage = ovDoc.Application.ActivePage;
                    ////select everything that is in our delayedevent list of shapes
                    //foreach(string sID in oThisDelayedEvent.lstShapes)
                    //{
                    //    Visio.Shape ovShape = ovCurrentPage.Shapes[sID];
                    //    ovWin.Select(ovShape, (short)Visio.VisSelectArgs.visSelect);
                    //}

                    Globals.ThisAddIn.m_bSuppressEvents = false;
                }


                if(oThisDelayedEvent.sOperationType == "MoveShapes")
                {

                    string sAction = "MoveShapes";
                    PagesForm oNewForm = new PagesForm();
                    oNewForm.Display(sAction);
                    oNewForm.Show();
                }





            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in ProcessThisDelayedEvent " + ex.Message, "VisAssist");
            }
            finally
            {

            }

        }
        internal static void OnVisioIsIdle(Visio.Application subject)
        {
            Globals.ThisAddIn.m_pendingShapeIds.Clear();
            Globals.ThisAddIn.m_pendingPageIds.Clear();
            Globals.ThisAddIn.m_MatesInSelection.Clear();
            Globals.ThisAddIn.m_ovSelection = null;
            Globals.ThisAddIn.m_MatesMated.Clear();
            Globals.ThisAddIn.m_lstWireIDs.Clear();
            Globals.ThisAddIn.m_lstPagesinProcessofDeleting.Clear();
            int iNumberOfDelayedEvents = Globals.ThisAddIn.m_delayedEvents.Count;

            if (iNumberOfDelayedEvents > 0)
            {


                try
                {


                    for (int ithEvent = iNumberOfDelayedEvents; ithEvent > 0; ithEvent--)
                    {
                        DelayedEvent thisDelayedEvent = Globals.ThisAddIn.m_delayedEvents[ithEvent - 1];

                        //make sure we process CheckShapeExistence last...
                        if (thisDelayedEvent.sOperationType != "CheckShapeExistence")
                        {
                            ProcessThisDelayedEvent(thisDelayedEvent);
                            Globals.ThisAddIn.m_delayedEvents.Remove(thisDelayedEvent);
                        }

                    }
                    iNumberOfDelayedEvents = Globals.ThisAddIn.m_delayedEvents.Count;
                    //now process the CheckShapeExistence delayed evetn
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
        internal static void PerformAction(string[] additionalArgs, object subject)
        {
            if (additionalArgs == null || additionalArgs.Length == 0)
            {
                // handle empty array case
                Console.WriteLine("No arguments provided.");
                return;
            }

            // Get the last string
            string sLastArg = additionalArgs[additionalArgs.Length - 1];

            sLastArg = ExtractAfterEquals(sLastArg);

            switch (sLastArg)
            {
                case "JumpToMate":
                    {
                        Visio.Application ovApplication = (Visio.Application)subject;
                        Visio.Selection ovSelction = ovApplication.ActiveWindow.Selection;
                        Visio.Shape ovShape = ovSelction[1];
                        if (ovShape.CellExists["User.Class", 0] == -1)
                        {
                            if (ovShape.Cells["User.Class"].get_ResultStr(0) == "SmartWire")
                            {
                                WireUtilities.JumpToMate(ovShape);
                            }
                        }

                        break;
                    }
                case "MoveShapes":
                    {
                        //move selection of shapes

                        string sAction = "MoveShapes";
                        PagesForm oNewForm = new PagesForm();
                        oNewForm.Display(sAction);
                        oNewForm.Show();
                        break;
                    }

            }

            // continue with your logic...
        }




        //HELPER METHODS
        static public bool GetVisioObjectsFromAddonString(
           string eventInfoFromVisio,
           Visio.Application visioApplication,
           out Visio.Document visioDocument,
           out Visio.Page visioPage,
           out Visio.Shape visioShape,
           out string appArgValue,
           out string actionArgValue,
           out string sPartNumber,
           out string sManufacturer,
           out string[] additionalArgs)
        {
            if (visioApplication == null)
            {
                throw new ArgumentNullException("visioApplication");
            }

            // initialize the out parameters
            visioDocument = null;
            visioPage = null;
            visioShape = null;
            appArgValue = null;
            actionArgValue = null;
            additionalArgs = null;
            sPartNumber = "";

            // call the basic version first to get the visio object
            GetObjectsFromAddonString(
                eventInfoFromVisio,
                visioApplication,
                out visioDocument,
                out visioPage,
                out visioShape,
                out sPartNumber,
                out sManufacturer);

            // split the info
            if (eventInfoFromVisio.StartsWith("/"))
            {
                eventInfoFromVisio = eventInfoFromVisio.Substring(1);
            }
            additionalArgs = eventInfoFromVisio.Split('/');

            // put the leading / back into the arguments.  This is mainly for compatibility
            // purposes since we have existing code that expects it.
            for (int arrayIndex = 0; arrayIndex < additionalArgs.Length; arrayIndex++)
            {
                additionalArgs.SetValue("/" + additionalArgs.GetValue(arrayIndex).ToString().Trim(), arrayIndex);
            }

            // find the argument values
            foreach (string arg in additionalArgs)
            {
                if (arg.StartsWith("/app="))
                {
                    // application arg found
                    appArgValue = arg.Replace("/app=", string.Empty);
                }
                if (arg.StartsWith("/partNumber="))
                {
                    // action arg found
                    sPartNumber = arg.Replace("/partNumber=", string.Empty);
                }

                if (arg.StartsWith("/mfg="))
                {
                    // action arg found
                    sManufacturer = arg.Replace("/mfg=", string.Empty);
                }

                if (arg.StartsWith("/action="))
                {
                    // action arg found
                    actionArgValue = arg.Replace("/action=", string.Empty);
                }
            }

            // default return
            return true;
        }

        static public bool GetObjectsFromAddonString(
            string eventInfoFromVisio,
            Visio.Application visioApplication,
            out Visio.Document visioDocument,
            out Visio.Page visioPage,
            out Visio.Shape visioShape,
            out string sPartNumber,
            out string sManufacturer)
        {
            if (visioApplication == null)
            {
                throw new ArgumentNullException("visioApplication");
            }

            // intialize the out parameters values
            visioDocument = null;
            visioPage = null;
            visioShape = null;
            sPartNumber = "";
            sManufacturer = "";


            // initialize locals
            string docArgValue = null;
            string pageArgValue = null;
            string shapeArgValue = null;
            string shapeUArgValue = null;

            if (eventInfoFromVisio.Length == 0)
            {
                // return failure
                return false;
            }

            // init return value
            bool retVal = false;

            try
            {
                // split the info
                if (eventInfoFromVisio.StartsWith("/"))
                {
                    eventInfoFromVisio = eventInfoFromVisio.Substring(1);
                }
                string[] cmdLineArguments = eventInfoFromVisio.Split('/');

                // put the leading / back into the arguments.  This is mainly for compatibility
                // purposes since we have existing code that expects it.
                for (int arrayIndex = 0; arrayIndex < cmdLineArguments.Length; arrayIndex++)
                {
                    cmdLineArguments.SetValue("/" + cmdLineArguments.GetValue(arrayIndex).ToString().Trim(), arrayIndex);
                }

                // find the argument values
                if (cmdLineArguments != null &&
                    cmdLineArguments.GetLength(0) > 0)
                {
                    // we have at least one argument, see what they are
                    foreach (string arg in cmdLineArguments)
                    {
                        if (arg.StartsWith("/doc="))
                        {
                            // doc arg found
                            docArgValue = arg.Replace("/doc=", string.Empty);
                        }
                        if (arg.StartsWith("/page="))
                        {
                            // page arg found
                            pageArgValue = arg.Replace("/page=", string.Empty);
                        }
                        if (arg.StartsWith("/shapeu"))
                        {
                            // shapeu arg found
                            shapeUArgValue = arg.Replace("/shapeu=", string.Empty);
                        }
                        if (arg.StartsWith("/shape="))
                        {
                            // shape arg found
                            shapeArgValue = arg.Replace("/shape=", string.Empty);
                        }

                        if (arg.StartsWith("/partNumber="))
                        {
                            // shape arg found
                            shapeArgValue = arg.Replace("/partNumber=", string.Empty);
                            sPartNumber = shapeArgValue;
                        }

                        if (arg.StartsWith("/mfg="))
                        {
                            // shape arg found
                            shapeArgValue = arg.Replace("/mfg=", string.Empty);
                            sManufacturer = shapeArgValue;
                        }

                    }
                }

                // if shapeU arg is found then it becomes the shapeArgValue so that the appropriate shape is found
                if (shapeUArgValue != null &&
                    shapeUArgValue.Length > 0)
                {
                    shapeArgValue = shapeUArgValue;
                }

                // get the objects based on the arguments
                if (docArgValue != null &&
                    docArgValue.Length > 0 &&
                    visioApplication.Documents.Count > 0)
                {
                    // this is an index value
                    double doubleValue;
                    bool isNumber = double.TryParse(
                        docArgValue,
                        System.Globalization.NumberStyles.Any,
                        null,
                        out doubleValue);

                    if (isNumber)
                    {
                        int docIndex = Int32.Parse(docArgValue, System.Globalization.CultureInfo.InvariantCulture);
                        visioDocument = visioApplication.Documents[docIndex];
                    }
                }

                if (pageArgValue != null &&
                    pageArgValue.Length > 0 &&
                    visioDocument != null &&
                    visioDocument.Pages.Count > 0)
                {
                    // this is an index value
                    double doubleValue;
                    bool isNumber = double.TryParse(
                        docArgValue,
                        System.Globalization.NumberStyles.Any,
                        null,
                        out doubleValue);

                    if (isNumber)
                    {
                        int pageIndex = Int32.Parse(pageArgValue, System.Globalization.CultureInfo.InvariantCulture);
                        visioPage = visioDocument.Pages[pageIndex];
                    }
                }

                if (shapeArgValue != null &&
                    shapeArgValue.Length > 0 &&
                    visioPage != null &&
                    visioPage.Shapes.Count > 0)
                {
                    visioShape = visioPage.Shapes.get_ItemU(shapeArgValue);
                }

                // set success
                retVal = true;
            }
            catch (System.Runtime.InteropServices.COMException)
            {
                // here we will ignore any exceptions because what most likely is that cause is we are asking for
                // a visio object that no longer exists
                retVal = false;
            }

            // default return
            return retVal;
        }


     


        static public void NavigateTo(
           Visio.Window visioWindow,
           Visio.Shape visioShape)
        {
            NavigateTo(visioWindow, visioShape, false, false);
        }

        static public void NavigateTo(
           Visio.Window visioWindow,
           Visio.Shape visioShape,
           bool scrollToView,
           bool zoomToFit)
        {
            if (visioWindow == null)
            {
                throw new ArgumentNullException("visioWindow");
            }

            if (visioShape == null)
            {
                throw new ArgumentNullException("visioShape");
            }

            if (visioShape.Application.ShowChanges)
            {
                // switch to the view to the page containing the target shape
                if (visioWindow.Page != visioShape.ContainingPage)
                {
                    visioWindow.Page = visioShape.ContainingPage.Name;
                }

                // check the type
                if (!visioShape.Type.Equals((short)Visio.VisShapeTypes.visTypePage))
                {
                    if (visioShape.ContainingShape != null)
                    {
                        // select the subshape
                        visioWindow.Select(
                            visioShape,
                            (short)Visio.VisSelectArgs.visDeselectAll + (short)Visio.VisSelectArgs.visSubSelect);
                    }
                    else
                    {
                        // select the shape
                        visioWindow.Select(
                            visioShape,
                            (short)Visio.VisSelectArgs.visDeselectAll + (short)Visio.VisSelectArgs.visSelect);
                    }

                    if (zoomToFit)
                    {
                        double left;
                        double right;
                        double top;
                        double bottom;
                        double width;
                        double height;

                        visioShape.BoundingBox(
                            (short)Visio.VisBoundingBoxArgs.visBBoxUprightWH,
                            out left,
                            out bottom,
                            out right,
                            out top);

                        width = Math.Abs(right - left);
                        height = Math.Abs(bottom - top);

                        double xPos = 0;
                        double yPos = 0;

                        visioShape.XYToPage(width / 2, height / 2, out xPos, out yPos);

                        if (width != 0 && height != 0)
                        {
                            visioWindow.SetViewRect(xPos, yPos, width * 4, height * 4);
                        }
                    }

                    if (scrollToView)
                    {
                        // get the pinx and piny of the shape
                        double width;
                        double height;
                        double xPos;
                        double yPos;

                        // calc window view position for shape on the page

                        width = visioShape.Cells["Width"].ResultIU;
                        //StringManipulator.GetCellValue(
                        //    visioShape,
                        //    "Width",
                        //    out width);
                        height = visioShape.Cells["Height"].ResultIU;
                        //StringManipulator.GetCellValue(
                        //    visioShape,
                        //    "Height",
                        //    out height);

                        visioShape.XYToPage(width / 2, height / 2, out xPos, out yPos);

                        // center selection in the window
                        visioWindow.ScrollViewTo(xPos, yPos);
                    }
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
        internal static bool IsShapeValid(Visio.Shape ovShape)
        {
            try
            {
                string sTest = ovShape.Name;
                return true;
            }
            catch
            {
                return false;
            }
        }

       

        internal static string ExtractAfterEquals(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            int index = input.IndexOf('=');
            if (index < 0 || index == input.Length - 1)
                return string.Empty; // '=' not found or nothing after '='

            return input.Substring(index + 1);
        }

        internal static void OnBeforeDocumentSave(Visio.Document ovDoc)
        {
            try
            {


                //before the doc is saved check to see if the document has the user cells for the next wire number and the next wire color
                //add the cells if needed and populate them with the values from the db..
                if (ovDoc.DocumentSheet.CellExists["User.NextWireColor", 0] == 0)
                {
                    //Adding these two together so if I have one I have the other...
                    ovDoc.DocumentSheet.AddNamedRow((short)Visio.VisSectionIndices.visSectionUser, "NextWireColor", 0);
                    ovDoc.DocumentSheet.AddNamedRow((short)Visio.VisSectionIndices.visSectionUser, "NextWireNumber", 0);
                }

                string sVisAssistFolderPath = FileUtilities.GetFolderPath(ovDoc);
                //now populate those cells based on the db..
                DatabaseConfig.BindToActiveDocument(sVisAssistFolderPath);
                string sFileID = ovDoc.DocumentSheet.Cells["User.FileID"].get_ResultStr(0);
                string sNextWireColor = FileUtilities.GetColumnInfoInFilesTableFromDatabase("NextWireColor", sFileID);
                string sNextWireNumber = FileUtilities.GetColumnInfoInFilesTableFromDatabase("NextWireNumber", sFileID);

                ovDoc.DocumentSheet.Cells["User.NextWireColor"].Formula = VisioUtilities.Application.FormatStringForVisio(sNextWireColor);
                ovDoc.DocumentSheet.Cells["User.NextWireNumber"].Formula = VisioUtilities.Application.FormatStringForVisio(sNextWireNumber);
            }
            catch(Exception ex)
            {
                MessageBox.Show("Error in OnBeforeDocumentSave " + ex.Message, "VisAssist");
            }

        }
    }




    public struct MateSelection
    {
        public string sMateID { get; set; }
        public string sShapeID { get; set; }
        public Visio.Shape ovShape { get; set; }
        public Visio.Shape ovMateShape { get; set; }
        public string sWirePairID { get; set; }

        public MateSelection(string sMateID, string sShapeID, Visio.Shape ovShape, Visio.Shape ovMateShape, string sWirePairID)
        {
            this.sMateID = sMateID;
            this.sShapeID = sShapeID;
            this.ovShape = ovShape;
            this.ovMateShape = ovMateShape;
            this.sWirePairID = sWirePairID;
        }
    }

}
