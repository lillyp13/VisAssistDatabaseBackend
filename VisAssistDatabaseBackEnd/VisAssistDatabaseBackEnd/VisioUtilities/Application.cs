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
using System.Xml.Linq;
using System.Configuration;

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
                            List<Visio.Page> oListPages = new List<Visio.Page>();
                            oListPages.Add(ovVisioPage);
                            //will need to also add all the shapes on the page back to the db...check if this gets called when copying a file...
                            ShapesUtilities.AddShapesToDatabase(oListPages, sProjectID);
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
                                if (!existingEvent.oListPages.Contains(ovVisioPage))
                                {
                                    existingEvent.oListPages.Add(ovVisioPage);
                                }

                            }
                            else
                            {
                                DelayedEvent oDelayedEvent = new DelayedEvent();
                                oDelayedEvent.ovDocument = ovVisioPage.Document;
                                oDelayedEvent.sOperationType = "AddShapesToDatabase";
                                if (oDelayedEvent.oListPages == null)
                                {
                                    oDelayedEvent.oListPages = new List<Visio.Page>();
                                }
                                oDelayedEvent.oListPages.Add(ovVisioPage);

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
                Dictionary<string, Visio.Shape> oDictWires = new Dictionary<string, Shape>();
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
        internal static void OnShapeAdded(Visio.Shape ovShape, Visio.Selection ovSelection, ref Dictionary<string, Shape> oDictWiresComingFromRedo)
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
                                        WireUtilities.UpdateWireInDatabase(ovShape);
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
                                    if (sShapeID == "")
                                    {
                                        bNewWire = true;
                                    }
                                    else
                                    {
                                        bNewWire = false; //this is probably a copy...
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

        internal static List<string> OnShapeDeletedRedoing(Visio.Shape ovShape, Selection ovSelection)
        {
            Visio.Document ovDoc = ovShape.Document;
            List<string> lstShapesRemoved = new List<string>();

            try
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

                                    lstShapesRemoved = DatabaseUtilities.CheckShapeExistenceInVisioOnDeletion(DatabaseUtilities.SqlTables.WireShapesTable.sWireShapeTable, ovShape.Document, ref lstShapes);
                                }
                                else
                                {
                                    //we also need to go and delete th secondary wire (wherever it lives...)
                                    //gather the seconary information before deleting it from the db..
                                    string sWireRole = ovShape.Cells["User.WireRole"].get_ResultStr(0);
                                    string sShapeID = ovShape.Cells["User.ShapeID"].get_ResultStr(0);
                                    string sMateID = "";
                                    string sPageID;
                                    string sWirePairID = WireUtilities.GetColumnInfoInWireShapesTableFromDatabase("WirePairID", sShapeID);

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
                                    sPageID = WireUtilities.GetColumnInfoInWireShapesTableFromDatabase("PageID", sMateID);

                                    WireUtilities.DeleteWireFromDatabaseUsingShape(ovShape);

                                    bool bDeleteMateShape = true;
                                    //delete the secondary in visio if the user doesn't have it in the selection...
                                    foreach (Visio.Page ovPage in ovDoc.Pages)
                                    {
                                        if (ovPage.PageSheet.CellExists["User.PageID", 0] == -1)
                                        {
                                            string sPageIDToCheck = ovPage.PageSheet.Cells["User.PageID"].get_ResultStr(0);
                                            if (sPageIDToCheck == sPageID)
                                            {
                                                //this is the page the mate lives on
                                                foreach (Visio.Shape ovShapeToCheck in ovPage.Shapes)
                                                {
                                                    if (ovShapeToCheck.CellExists["User.ShapeID", 0] == -1)
                                                    {
                                                        string sShapeIDToCheck = ovShapeToCheck.Cells["User.ShapeID"].get_ResultStr(0);
                                                        if (sShapeIDToCheck == sMateID)
                                                        {

                                                            //before deleting check if this wire is in our ovSelection..
                                                            int iSelectedShapes = ovSelection.Count;
                                                            for (int ithShape = 1; ithShape <= iSelectedShapes; ithShape++)
                                                            {
                                                                Visio.Shape ovShapeInSelection = ovSelection[ithShape];
                                                                if (ovShapeInSelection.CellExists["User.ShapeID", 0] == -1)
                                                                {
                                                                    string sShapeIDToPossiblyDelete = ovShapeInSelection.Cells["User.ShapeID"].get_ResultStr(0);
                                                                    if (sShapeIDToPossiblyDelete == sMateID)
                                                                    {
                                                                        //it is already going to be deleted because the user chose to delete it..
                                                                        bDeleteMateShape = false;
                                                                    }
                                                                }
                                                            }

                                                            if (bDeleteMateShape)
                                                            {
                                                                //turn off events and then delete the shape..
                                                                ovDoc.Application.EventsEnabled = 0;
                                                                ovShapeToCheck.Delete();
                                                                ovDoc.Application.EventsEnabled = -1;
                                                            }

                                                        }
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
                                if (Globals.ThisAddIn.Application.IsUndoingOrRedoing)
                                {
                                    List<string> lstShapes = ShapesUtilities.PopulateShapesInDocument(ovShape.ContainingPage.Document, sClass);
                                    //need to clean up the db based on the shapes on this page left...
                                    DatabaseUtilities.CheckShapeExistenceInVisio(DatabaseUtilities.SqlTables.TerminalBlocksTable.sTerminalBlockTable, ovShape.Document, ref lstShapes);

                                }
                                else
                                {
                                    TerminalBlockUtilities.DeleteTerminalBlockFromDatabase(ovShape);
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
                                    EndDeviceUtilities.DeleteEndDeviceFromDatabase(ovShape);
                                }

                                break;
                            }
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
                ovDoc.Application.EventsEnabled = -1;
            }
        }
        internal static List<string> OnShapeDeleted(string sShapeID, string sWireRole, Visio.Document ovDocument, string sClass)
        {

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

                                //check if the mates is in the selection already scheduled to be deleted...

                                //delete the secondary in visio if the user doesn't have it in the selection...
                                //foreach (Visio.Page ovPage in ovDocument.Pages)
                                //{
                                //    if (ovPage.PageSheet.CellExists["User.PageID", 0] == -1)
                                //    {
                                //        string sPageIDToCheck = ovPage.PageSheet.Cells["User.PageID"].get_ResultStr(0);
                                //        if (sPageIDToCheck == sPageID)
                                //        {
                                //            //this is the page the mate lives on
                                //            foreach (Visio.Shape ovShapeToCheck in ovPage.Shapes)
                                //            {
                                //                if (ovShapeToCheck.CellExists["User.ShapeID", 0] == -1)
                                //                {
                                //                    string sShapeIDToCheck = ovShapeToCheck.Cells["User.ShapeID"].get_ResultStr(0);
                                //                    if (sShapeIDToCheck == sMateID)
                                //                    {

                                //                        //before deleting check if this wire is in our ovSelection..
                                //                        int iSelectedShapes = ovSelection.Count;
                                //                        for (int ithShape = 1; ithShape <= iSelectedShapes; ithShape++)
                                //                        {
                                //                            Visio.Shape ovShapeInSelection = ovSelection[ithShape];
                                //                            if (ovShapeInSelection.CellExists["User.ShapeID", 0] == -1)
                                //                            {
                                //                                string sShapeIDToPossiblyDelete = ovShapeInSelection.Cells["User.ShapeID"].get_ResultStr(0);
                                //                                if (sShapeIDToPossiblyDelete == sMateID)
                                //                                {
                                //                                    //it is already going to be deleted because the user chose to delete it..
                                //                                    bDeleteMateShape = false;
                                //                                }
                                //                            }
                                //                        }

                                //                        if (bDeleteMateShape)
                                //                        {
                                //                            //turn off events and then delete the shape..
                                //                            ovDoc.Application.EventsEnabled = 0;
                                //                            ovShapeToCheck.Delete();
                                //                            ovDoc.Application.EventsEnabled = -1;
                                //                        }

                                //                    }
                                //                }
                                //            }
                                //        }
                                //    }
                                //}

                            }


                            break;
                        }
                    case "TerminalBlock":
                        {
                            //if (Globals.ThisAddIn.Application.IsUndoingOrRedoing)
                            //{
                            //    List<string> lstShapes = ShapesUtilities.PopulateShapesInDocument(ovShape.ContainingPage.Document, sClass);
                            //    //need to clean up the db based on the shapes on this page left...
                            //    DatabaseUtilities.CheckShapeExistenceInVisio(DatabaseUtilities.SqlTables.TerminalBlocksTable.sTerminalBlockTable, ovShape.Document, ref lstShapes);

                            //}
                            //else
                            //{
                            // TerminalBlockUtilities.DeleteTerminalBlockFromDatabase(ovShape);
                            // }

                            break;
                        }
                    case "ADC End Device":
                        {
                            //if (Globals.ThisAddIn.Application.IsUndoingOrRedoing)
                            //{
                            //    List<string> lstShapes = ShapesUtilities.PopulateShapesInDocument(ovShape.ContainingPage.Document, sClass);

                            //    //need to clean up the db based on the shapes on this page left...
                            //    DatabaseUtilities.CheckShapeExistenceInVisio(DatabaseUtilities.SqlTables.WiringEndDevice.sWiringEndDeviceTable, ovShape.Document, ref lstShapes);

                            //}
                            //else
                            //{
                            // EndDeviceUtilities.DeleteEndDeviceFromDatabase(ovShape);
                            //}

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
                                WireUtilities.UpdateWireInDatabase(ovShape);
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
        internal static void OnWireShapeCut(Shape ovShape)
        {
            //we notice the user cut and paste a wire..we need to update in db where this wire exists as well as its grid location in the mates wire...
            WireUtilities.UpdateWireInDatabase(ovShape);
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

                    string sCurrentPageID = ovPage.PageSheet.Cells["User.PageID"].get_ResultStr(0);
                    //update the sPageID in the db to be sCurrentPageID
                    //update the entry record in pages_table where sPageID and update the pageID to sCurrentPageID

                    PageUtilities.UpdatePageIDInDatabase(sPageID, sCurrentPageID, ovPage);


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



                    // Loop through in steps of 2
                    for (int ithShape = 0; ithShape < lstShapes.Count; ithShape += 2)
                    {
                        // Make sure we don't go out of bounds if the count is odd
                        if (ithShape + 1 >= lstShapes.Count)
                        {
                            Console.WriteLine($"Warning: Shape at index {ithShape} has no pair.");
                            break;
                        }

                        Visio.Shape ovShape1 = lstShapes[ithShape];
                        Visio.Shape ovShape2 = lstShapes[ithShape + 1];

                        MultipleRecordUpdates mruShape1 = WireUtilities.BuildWireShapeInfo(ovShape1, "", false);
                        MultipleRecordUpdates mruShape2 = WireUtilities.BuildWireShapeInfo(ovShape2, "", false);
                        string sProjectID = ovDocument.DocumentSheet.Cells["User.ProjectID"].get_ResultStr(0);
                        string sFileID = ovDocument.DocumentSheet.Cells["User.FileID"].get_ResultStr(0);
                        string sPageID = mruShape1.ruRecords[0].odictColumnValues["PageID"];



                        string sWirePairID = WireUtilities.GenerateWirePairID(sProjectID, sFileID, sPageID, ovShape1.Name, ovShape2.Name, DateTime.Now);
                        mruShape1.ruRecords[0].odictColumnValues["WirePairID"] = sWirePairID;
                        mruShape2.ruRecords[0].odictColumnValues["WirePairID"] = sWirePairID;

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
                    List<Visio.Page> oListPages = oThisDelayedEvent.oListPages;

                    ShapesUtilities.AddShapesToDatabase(oListPages, sProjectID);
                }

                if (oThisDelayedEvent.sOperationType == "Undo")
                {
                    Globals.ThisAddIn.Application.Undo();
                    Globals.ThisAddIn.m_sLastUndoScope = "";
                }
                if (oThisDelayedEvent.sOperationType == "Redo")
                {
                    Globals.ThisAddIn.Application.Redo();
                    Visio.Document ovDocument = oThisDelayedEvent.ovDocument;
                    //check to see if the all the shapes in the db exist in the visio file and if they don't this is because we are redoing a cut and paste and need to subscribe to redo twice otherwise redoing once is fine...
                    bool bRedoTwice = DatabaseUtilities.CheckShapeExistenceInVisioForRedoing(DatabaseUtilities.SqlTables.WireShapesTable.sWireShapeTable, ovDocument);
                    if (bRedoTwice)
                    {
                        Globals.ThisAddIn.Application.Redo();
                    }

                    Globals.ThisAddIn.m_sLastUndoScope = "Cut and Paste Action";

                    //will need to sync db with visio file
                    Visio.Document ovDoc = oThisDelayedEvent.ovDocument;
                    string sVisAssistFolderPath = FileUtilities.GetFolderPath(ovDoc);
                    DatabaseUtilities.SyncDBWithFile(ovDoc, sVisAssistFolderPath);
                }

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
                    foreach(Visio.Page ovPage in ovDocument.Pages)
                    {
                        if (ovPage.PageSheet.CellExists["User.PageID", 0] == -1)
                        {
                            foreach(Visio.Shape ovShape in ovPage.Shapes)
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
                    //bool bRedo = false;
                    //int iDifference = Math.Abs(iNumberOfRecords - iWiresInDoc);
                    //if(iDifference % 2 == 0)
                    //{
                    //    //the difference is even
                    //    //we need to check to see if wires in doc are all paired...if 
                    //    //we want to see if the lstWiresInVisio and the lstWiresInDb 

                    //    //need to make sure that the lstWiresInVisio while even number there needs to be 2 of the same id...
                    //    var wireCounts = lstWiresInVisio.GroupBy(id => id).ToDictionary(g => g.Key, g => g.Count());

                    //    // Check if every wire ID appears exactly twice
                    //    bool allPairsValid = wireCounts.Values.All(count => count == 2);

                    //    if (allPairsValid)
                    //    {
                    //        //if there are pairs in lstWiresInDB that are not in lstWiresInVisio we will want to said bRedo to be true
                    //        bool missingInVisio = lstWiresInDB.Any(dbWireID => !lstWiresInVisio.Contains(dbWireID));
                    //        if(missingInVisio)
                    //        {
                    //            bRedo = true;
                    //        }
                    //    }
                    //    else
                    //    {
                    //        bRedo = true;
                    //    }
                    //}
                    //else
                    //{
                    //    //the difference is odd we need to do a redo...
                    //    bRedo = true;
                    //}
                    
                   
                    //if (bRedo)
                    //{

                    //    if(Globals.ThisAddIn.m_sLastUndoScope == "Cut and Paste Action")
                    //    {
                    //        Globals.ThisAddIn.Application.Undo();
                    //        Globals.ThisAddIn.m_sLastUndoScope = "Undoing Cut and Paste Action";
                    //    }
                    //    else
                    //    {
                    //        if(Globals.ThisAddIn.m_sLastUndoScope == "Undoing Cut and Paste Action")
                    //        {
                    //            Globals.ThisAddIn.Application.Redo();
                    //            Globals.ThisAddIn.m_sLastUndoScope = "Cut and Paste Action";
                    //        }
                            
                    //    }
                        
                    //}


                    //will need to sync db with visio file
                    Visio.Document ovDoc = oThisDelayedEvent.ovDocument;
                    string sVisAssistFolderPath = FileUtilities.GetFolderPath(ovDoc);
                    DatabaseUtilities.SyncDBWithFile(ovDoc, sVisAssistFolderPath);
                }

                if(oThisDelayedEvent.sOperationType == "UndoCut")
                {
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
            Globals.ThisAddIn.m_MatesDeleted.Clear();
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
