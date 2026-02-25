using Microsoft.Office.Interop.Visio;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using VisAssistDatabaseBackEnd.DataUtilities;
using VisAssistDatabaseBackEnd.VisioUtilities;
using Visio = Microsoft.Office.Interop.Visio;

namespace VisAssistDatabaseBackEnd.ShapeUtilities.Wire
{
    internal class WireMateUtilities
    {

        internal static void AddWireMatesToSelection(ref Dictionary<string, MateSelection> odictWires, Visio.Document ovDocument)
        {
            //ok now we have all the wires in the selection in our dictionary but we need to see if we need to add mate shapes...
            try
            {


                Queue<string> queWiresToProcess = new Queue<string>(odictWires.Keys);

                // Prevent infinite loops
                HashSet<string> processedWireIDs = new HashSet<string>();

                while (queWiresToProcess.Count > 0)
                {
                    string sCurrentWireID = queWiresToProcess.Dequeue();


                    // Skip if already processed
                    if (!processedWireIDs.Add(sCurrentWireID))
                        continue;

                    MateSelection currentSelection = odictWires[sCurrentWireID];
                    string sWireRole = currentSelection.sWireRole;
                    string sWirePairID = currentSelection.sWirePairID;


                    string sMateID = GetMateID(sWirePairID, sWireRole);

                    if (!odictWires.ContainsKey(sMateID))
                    {
                        MateSelection newSelection = new MateSelection();
                        newSelection.sShapeID = sMateID;
                        newSelection.sWirePairID = sWirePairID;
                        newSelection.sWireRole = sWireRole;


                        odictWires.Add(sMateID, newSelection);

                        queWiresToProcess.Enqueue(sMateID);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in AddWireMatesToSelection " + ex.Message, "VisAssist");
            }
            finally
            {
                //make sure events are on
                Globals.ThisAddIn.Application.EventsEnabled = -1;
            }
        }


        internal static string GetMateID(string sWirePairID, string sWireRole)
        {
            string sMateID = "";
            try
            {

                switch (sWireRole)
                {
                    case "P":
                        {
                            //the shape we have is a primary we want to get the secondary wire id..
                            sMateID = WireUtilities.GetColumnInfoInWirePairsTableFromDatabase("SecondaryWireID", sWirePairID);
                            break;
                        }
                    case "S":
                        {
                            //the shape we have is a secondary we want to get the primary wire id...
                            sMateID = WireUtilities.GetColumnInfoInWirePairsTableFromDatabase("PrimaryWireID", sWirePairID);
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in GetMateID " + ex.Message, "VisAssist");
            }
            finally
            {
                //make sure events are on
                Globals.ThisAddIn.Application.EventsEnabled = -1;
            }
            return sMateID;
        }

        //ACtIONS
        internal static void UpdateMatesFeatures(Visio.Shape ovMateShape, Visio.Shape ovShape, string sWirePairID)
        {
            try
            {
                //need to match the # of conductors, color, wire label, auto labelling, and all the conductor labels...
                string sPageID = ovShape.ContainingPage.PageSheet.Cells["User.PageID"].get_ResultStr(0);
                Dictionary<string, string> odictWireInfo = WireUtilities.GatherWireInformation(ovShape, sPageID, sWirePairID);

                //based on the info in odictWireInfo update ovShapeToUpdate with the values..

                //go through the dicionaty and pull out the following values:
                string sColor = odictWireInfo["Color"];

                string sWireLabel = odictWireInfo["WireLabel"];


                int iNumberOfConductors = Convert.ToInt32(odictWireInfo["ConductorCount"]);
                int iAutoLabel = Convert.ToInt32(odictWireInfo["AutoLabeling"]);
                string sConductor1 = odictWireInfo["Conductor1Label"];
                string sConductor2 = odictWireInfo["Conductor2Label"];
                string sConductor3 = odictWireInfo["Conductor3Label"];
                string sConductor4 = odictWireInfo["Conductor4Label"];
                string sConductor5 = odictWireInfo["Conductor5Label"];
                string sConductor6 = odictWireInfo["Conductor6Label"];
                string sConductor7 = odictWireInfo["Conductor7Label"];
                string sConductor8 = odictWireInfo["Conductor8Label"];
                string sConductor9 = odictWireInfo["Conductor9Label"];
                string sConductor10 = odictWireInfo["Conductor10Label"];

                //only do this if the values are different...
                double dCurrentNumberofConductors = ovMateShape.Cells["Prop.NumberOfConductors"].ResultIU;
                if (Convert.ToInt32(dCurrentNumberofConductors) != iNumberOfConductors)
                {
                    ovMateShape.Cells["Prop.NumberOfConductors"].ResultIU = iNumberOfConductors;
                }
                string sCurrentWireLabel = ovMateShape.Cells["Prop.WireLabel"].get_ResultStr(0);
                if (sCurrentWireLabel != sWireLabel)
                {
                    ovMateShape.Cells["Prop.WireLabel"].Formula = VisioUtilities.Application.FormatStringForVisio(sWireLabel);
                }


            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in UpdateMatesFeatures " + ex.Message, "VisAssist");
            }
            finally
            {
                //make sure events are on
                Globals.ThisAddIn.Application.EventsEnabled = -1;
            }

        }
        internal static void JumpToMate(Shape ovShape)
        {
            try
            {

                //we are given a visio wire shape and we need to determine what/where is the mate wire and then navigate to that shape...
                string sShapeID = ovShape.Cells["User.ShapeID"].get_ResultStr(0);
                string sWirePairID = ovShape.Cells["User.WirePairID"].get_ResultStr(0);
                string sWireRole = ovShape.Cells["User.WireRole"].get_ResultStr(0);
                string sMateID = GetMateID(sWirePairID, sWireRole);


                //ok now we have the mates ID lets get the page id
                string sMatePageID = WireUtilities.GetColumnInfoInWireShapesTableFromDatabase("PageID", sMateID);
                //use the index to get the page in the document and then double check the page id...
                //get the page index
                string sIndex = PageUtilities.GetColumnInfoInPagesTableFromDatabase("PageIndex", sMatePageID);
                int iIndex = Convert.ToInt32(sIndex);
                //get the visio page from the index instead of looping thorugh the pages
                Visio.Page ovPage = ovShape.Document.Pages[iIndex];

                //this is the page the mate lives on
                foreach (Visio.Shape ovMateShape in ovPage.Shapes)
                {
                    if (ovMateShape.CellExists["User.ShapeID", 0] == -1)
                    {
                        if (ovMateShape.Cells["User.ShapeID"].get_ResultStr(0) == sMateID)
                        {
                            //this is the mate shape navigate the user here...
                            VisioUtilities.Application.NavigateTo(Globals.ThisAddIn.Application.ActiveWindow, ovMateShape);
                            Globals.ThisAddIn.Application.ActiveWindow.CenterViewOnShape(ovMateShape, Visio.VisCenterViewFlags.visCenterViewDefault);
                        }
                    }
                }


            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in JumpToMate " + ex.Message, "VisAssist");
            }
            finally
            {
                //make sure events are on
                Globals.ThisAddIn.Application.EventsEnabled = -1;
            }


        }

        //CHECKS

        //I'm thinking of refactoring a few of these methods so that instead of passing the com object I pass the ids...
        internal static void IsWirePairsOnPageDuplicated(Dictionary<string, Shape> odictWires)
        {
            try
            {
                string sProjectID;
                string sFileID;
                string sPageID;
                string sFirstKey;
                string sSecondKey;
                List<string> oListWiresProcessed = new List<string>();

                foreach (Visio.Shape ovShape in odictWires.Values)
                {
                    sProjectID = ovShape.Document.DocumentSheet.Cells["User.ProjectID"].get_ResultStr(0);
                    sFileID = ovShape.Document.DocumentSheet.Cells["User.FileID"].get_ResultStr(0);
                    sPageID = ovShape.ContainingPage.PageSheet.Cells["User.PageID"].get_ResultStr(0); //this was updated in OnPageDuplicated..
                    sFirstKey = ovShape.ID + "|" + ovShape.ContainingPage.Name;
                    if (!oListWiresProcessed.Contains(sFirstKey))
                    {


                        string sShapeID = ovShape.Cells["User.ShapeID"].get_ResultStr(0);
                        //based on the sShapeID grab the WirePairID to get the Mates ID...
                        string sWirePairID = WireUtilities.GetColumnInfoInWireShapesTableFromDatabase("WirePairID", sShapeID);
                        //now get the mates id...
                        string sWireRole = ovShape.Cells["User.WireRole"].get_ResultStr(0);
                        string sMateID = GetMateID(sWirePairID, sWireRole);


                        bool bMateInSelection = false;
                        //ok now we need to see if this mateid exists in the odictWires
                        foreach (Visio.Shape ovShapeToCheck in odictWires.Values)
                        {
                            string sShapeIDToCheck = ovShapeToCheck.Cells["User.ShapeID"].get_ResultStr(0);
                            if (sShapeIDToCheck == sMateID)
                            {
                                //the mate is in the selection and we should pair these wires together
                                bMateInSelection = true;
                                //let's update the ids for these shapes and add them to the db..

                                string sNewShapeIDFirstShape = ShapesUtilities.GenerateShapeID(sProjectID, sFileID, sPageID, ovShape.Name, DateTime.Now);
                                string sMatesPageID = ovShapeToCheck.ContainingPage.PageSheet.Cells["User.PageID"].get_ResultStr(0);
                                string sNewShapeIDSecondShape = ShapesUtilities.GenerateShapeID(sProjectID, sFileID, sMatesPageID, ovShapeToCheck.Name, DateTime.Now);
                                //update the ids in the shapes..
                                ovShape.Application.EventsEnabled = 0;
                                ovShape.Cells["User.ShapeID"].Formula = VisioUtilities.Application.FormatStringForVisio(sNewShapeIDFirstShape);
                                ovShapeToCheck.Cells["User.ShapeID"].Formula = VisioUtilities.Application.FormatStringForVisio(sNewShapeIDSecondShape);
                                ovShape.Application.EventsEnabled = -1;

                                string sNewWirePairID = WireUtilities.GenerateWirePairID(sProjectID, sFileID, sPageID, ovShape.Name, ovShapeToCheck.Name, DateTime.Now);
                                ovShape.Application.EventsEnabled = 0;
                                ovShape.Cells["User.WirePairID"].Formula = VisioUtilities.Application.FormatStringForVisio(sNewWirePairID);
                                ovShapeToCheck.Cells["User.WirePairID"].Formula = VisioUtilities.Application.FormatStringForVisio(sNewWirePairID);
                                ovShape.Application.EventsEnabled = -1;


                                //now we need to add these wires to the db..
                                MultipleRecordUpdates mruPrimaryRecord = new MultipleRecordUpdates();
                                MultipleRecordUpdates mruSecondaryRecord = new MultipleRecordUpdates();
                                switch (sWireRole)
                                {
                                    case "P":
                                        {
                                            mruPrimaryRecord = WireUtilities.BuildWireShapeInfo(ovShape, sNewWirePairID, false);
                                            mruSecondaryRecord = WireUtilities.BuildWireShapeInfo(ovShapeToCheck, sNewWirePairID, false);

                                            break;
                                        }
                                    case "S":
                                        {
                                            mruSecondaryRecord = WireUtilities.BuildWireShapeInfo(ovShape, sNewWirePairID, false);
                                            mruPrimaryRecord = WireUtilities.BuildWireShapeInfo(ovShapeToCheck, sNewWirePairID, false);

                                            break;
                                        }
                                }

                                sSecondKey = ovShapeToCheck.ID + "|" + ovShapeToCheck.ContainingPage.Name;
                                WireUtilities.AddWireToDatabase(mruPrimaryRecord, mruSecondaryRecord);

                                //after adding the shapes to the database we need to update the gridlocation
                                switch (sWireRole)
                                {
                                    case "P":
                                        {
                                            WireUtilities.UpdateWireGridLocation(ovShape, mruPrimaryRecord, false);
                                            WireUtilities.UpdateWireGridLocation(ovShapeToCheck, mruSecondaryRecord, false);
                                            break;
                                        }
                                    case "S":
                                        {
                                            WireUtilities.UpdateWireGridLocation(ovShape, mruSecondaryRecord, false);
                                            WireUtilities.UpdateWireGridLocation(ovShapeToCheck, mruPrimaryRecord, false);
                                            break;
                                        }
                                }
                                oListWiresProcessed.Add(sFirstKey);
                                oListWiresProcessed.Add(sSecondKey);



                            }
                        }

                        if (!bMateInSelection)
                        {
                            //the mate is not on the page we are adding...we are going to drop another wire on the page and mate it with this wire...
                            string sWireRoleToCreate = "";
                            bool bShapeIsPrimary = true;
                            if (sWireRole == "P")
                            {
                                bShapeIsPrimary = true;
                                sWireRoleToCreate = "Secondary";
                            }
                            else
                            {
                                if (sWireRole == "S")
                                {
                                    bShapeIsPrimary = false;
                                    sWireRoleToCreate = "Primary";
                                }
                            }

                            //add a new ShapeID to our current shape
                            string sNewShapeID = ShapesUtilities.GenerateShapeID(sProjectID, sFileID, sPageID, ovShape.Name, DateTime.Now);
                            ovShape.Application.EventsEnabled = 0;
                            ovShape.Cells["User.ShapeID"].Formula = VisioUtilities.Application.FormatStringForVisio(sNewShapeID);
                            ovShape.Application.EventsEnabled = -1;
                            //create a new WirePairID for these wires...
                            Visio.Shape ovOtherWire = WireUtilities.AddOtherWire(ovShape, sWireRoleToCreate);
                            string sNewWirePairId = "";
                            if (bShapeIsPrimary)
                            {
                                sNewWirePairId = WireUtilities.GenerateWirePairID(sProjectID, sFileID, sPageID, ovShape.Name, ovOtherWire.Name, DateTime.Now);
                            }
                            else
                            {
                                sNewWirePairId = WireUtilities.GenerateWirePairID(sProjectID, sFileID, sPageID, ovOtherWire.Name, ovShape.Name, DateTime.Now);
                            }


                            //update the wirePairId for the wires...
                            ovShape.Application.EventsEnabled = 0;
                            ovShape.Cells["User.WirePairID"].Formula = VisioUtilities.Application.FormatStringForVisio(sNewWirePairId);
                            ovOtherWire.Cells["User.WirePairID"].Formula = VisioUtilities.Application.FormatStringForVisio(sNewWirePairId);
                            ovShape.Application.EventsEnabled = -1;


                            //WE NO LONGER WANT TO INCREASE THE NUMBER OR COLOR WHEN DUPLICATING....
                            //we want to get the wire label and the color from ovShape and apply it to ovOtherWire
                            string sWireLabel = ovShape.Cells["Prop.WireLabel"].get_ResultStr(0);
                            string sWireColor = ovShape.Cells["User.WireColor"].get_ResultStr(0);
                            ovShape.Application.EventsEnabled = 0;
                            ovOtherWire.Cells["Prop.WireLabel"].Formula = VisioUtilities.Application.FormatStringForVisio(sWireLabel);
                            ovOtherWire.Cells["User.WireColor"].Formula = VisioUtilities.Application.FormatStringForVisio(sWireColor);
                            ovShape.Application.EventsEnabled = -1;



                            //now add the wires to the db
                            MultipleRecordUpdates mruPrimaryRecord = new MultipleRecordUpdates();
                            MultipleRecordUpdates mruSecondaryRecord = new MultipleRecordUpdates();
                            if (bShapeIsPrimary)
                            {
                                mruPrimaryRecord = WireUtilities.BuildWireShapeInfo(ovShape, sNewWirePairId, false);
                                mruSecondaryRecord = WireUtilities.BuildWireShapeInfo(ovOtherWire, sNewWirePairId, false);
                            }
                            else
                            {
                                mruPrimaryRecord = WireUtilities.BuildWireShapeInfo(ovOtherWire, sNewWirePairId, false);
                                mruSecondaryRecord = WireUtilities.BuildWireShapeInfo(ovShape, sNewWirePairId, false);
                            }
                            WireUtilities.AddWireToDatabase(mruPrimaryRecord, mruSecondaryRecord);
                            switch (sWireRole)
                            {
                                case "P":
                                    {
                                        WireUtilities.UpdateWireGridLocation(ovShape, mruPrimaryRecord, false);
                                        WireUtilities.UpdateWireGridLocation(ovOtherWire, mruSecondaryRecord, false);
                                        break;
                                    }
                                case "S":
                                    {
                                        WireUtilities.UpdateWireGridLocation(ovShape, mruSecondaryRecord, false);
                                        WireUtilities.UpdateWireGridLocation(ovOtherWire, mruPrimaryRecord, false);
                                        break;
                                    }
                            }


                        }
                    }

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in CheckForWirePairs " + ex.Message, "VisAssist");
            }
            finally
            {
                //make sure events are on
                Globals.ThisAddIn.Application.EventsEnabled = -1;
            }
        }

        internal static void IsWireMateOnPageDelete(Visio.Page ovPage, string sPageID)
        {
            //this gets called when a user deletes a page and we need to see if there are wires on the page and if their mates are on the same page or a different page
            //if the mate is on a different page we need to manually delete it...
            try
            {
                foreach (Visio.Shape ovShape in ovPage.Shapes)
                {
                    if (ovShape.CellExists["User.Class", 0] == -1)
                    {
                        string sClass = ovShape.Cells["User.Class"].get_ResultStr(0);
                        if (sClass == "SmartWire")
                        {
                            //check if its mate is on a different page...
                            string sShapeID = ovShape.Cells["User.ShapeID"].get_ResultStr(0);
                            //use the shapeid to get the wirepairid and therefore the other wires id...
                            string sWirePairID = WireUtilities.GetColumnInfoInWireShapesTableFromDatabase("WirePairID", sShapeID);
                            string sWireRole = ovShape.Cells["User.WireRole"].get_ResultStr(0);
                            string sMateID = GetMateID(sWirePairID, sWireRole);


                            //now use the sMateID to find what page it is on...
                            string sMatesPageID = WireUtilities.GetColumnInfoInWireShapesTableFromDatabase("PageID", sMateID);

                            if (sMatesPageID != sPageID)
                            {
                                //only do this if the mate lives on a differnet page than we are deleting..

                                foreach (Visio.Page ovMatesPage in ovPage.Document.Pages)
                                {
                                    if (ovMatesPage.PageSheet.CellExists["User.PageID", 0] == -1)
                                    {
                                        string sPageIDToCheck = ovMatesPage.PageSheet.Cells["User.PageID"].get_ResultStr(0);
                                        if (sPageIDToCheck == sMatesPageID)
                                        {
                                            //this is the page the mate lives on..
                                            foreach (Visio.Shape ovShapeToCheck in ovMatesPage.Shapes)
                                            {
                                                if (ovShapeToCheck.CellExists["User.Class", 0] == -1)
                                                {
                                                    if (ovShapeToCheck.Cells["User.Class"].get_ResultStr(0) == "SmartWire")
                                                    {
                                                        string sShapeIDToCheck = ovShapeToCheck.Cells["User.ShapeID"].get_ResultStr(0);
                                                        if (sShapeIDToCheck == sMateID)
                                                        {
                                                            //this is the mate we need to delete...
                                                            ovPage.Application.EventsEnabled = 0;
                                                            ovShapeToCheck.Delete();
                                                            ovPage.Application.EventsEnabled = -1;
                                                        }
                                                    }


                                                }
                                            }
                                        }
                                    }
                                }
                            }

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in CheckForWireMateOnPageDelete " + ex.Message, "VisAssist");
            }
            finally
            {
                ovPage.Application.EventsEnabled = -1;
            }
        }
        internal static string IsMateInSelection(Selection ovSelection, Shape ovShape, out Visio.Shape ovMateShape)
        {
            //check if the ovShape mate is in the selection and if it is return the key ( string sKey = ovShape.ID + "|" + ovShape.ContainingPage.Name;)
            string sKey = "";
            ovMateShape = null;
            try
            {
                string sWireRole = "";
                if (ovShape.CellExists["User.ShapeID", 0] == -1)
                {
                    if (ovShape.CellExists["User.WirePairID", 0] == -1)
                    {


                        string sWirePairID = ovShape.Cells["User.WirePairID"].get_ResultStr(0);

                        //now we have the mates id check each shape in the selection for this id
                        foreach (Visio.Shape ovShapeToCheck in ovSelection)
                        {
                            if (ovShapeToCheck.Name != ovShape.Name)
                            {
                                if (ovShapeToCheck.CellExists["User.WirePairID", 0] == -1)
                                {
                                    if (ovShapeToCheck.Cells["User.WirePairID"].get_ResultStr(0) == sWirePairID)
                                    {
                                        //the mate is in the selection add it to m_pendingshapeids so we don't drop another wire pair for this..
                                        sKey = ovShapeToCheck.ID + "|" + ovShapeToCheck.ContainingPage.Name;
                                        ovMateShape = ovShapeToCheck;
                                        //Globals.ThisAddIn.m_pendingShapeIds.Add(sKey);
                                    }
                                }
                            }
                        }
                    }

                }
                return sKey;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in IsMateInSelection " + ex.Message, "VisAssist");
            }
            finally
            {
                //make sure events are on
                Globals.ThisAddIn.Application.EventsEnabled = -1;
            }
            return sKey;
        }
        internal static Dictionary<string, Visio.Page> DoesPageContainWireMates(Visio.Page ovPage)
        {
            //we are given a page we need to see if there is a wire on the page and if there is we need to check to see if the mate also lives on this page...
            //this should return a list of all the pages that the mates live on...
            Dictionary<string, Visio.Page> odictPagesToDuplicate = new Dictionary<string, Visio.Page>();
            try
            {
                if (ovPage.PageSheet.CellExists["User.PageID", 0] == -1)
                {
                    foreach (Visio.Shape ovShape in ovPage.Shapes)
                    {
                        if (ovShape.CellExists["User.Class", 0] == -1 && ovShape.Cells["User.Class"].get_ResultStr(0) == "SmartWire")
                        {

                            //there is a wire on the page, we need to check to see if the mate is also on this page...
                            WireMateUtilities.IsWireMateOnSamePage(ovShape, ref odictPagesToDuplicate);


                        }
                    }


                    //now that we have gone through the original page that the user clicked on to duplicate, I need to check those pages for mates and so on (following any daisy chain basically...)
                    Queue<Visio.Page> quePagesToProcess = new Queue<Visio.Page>(odictPagesToDuplicate.Values);

                    while (quePagesToProcess.Count > 0)
                    {
                        Visio.Page ovCurrentPage = quePagesToProcess.Dequeue();

                        foreach (Visio.Shape ovShape in ovCurrentPage.Shapes)
                        {
                            if (ovShape.CellExists["User.Class", 0] == -1 && ovShape.Cells["User.Class"].get_ResultStr(0) == "SmartWire")
                            {
                                int iBeforeCount = odictPagesToDuplicate.Count;

                                WireMateUtilities.IsWireMateOnSamePage(ovShape, ref odictPagesToDuplicate);

                                // If dictionary grew, enqueue the newly added pages
                                if (odictPagesToDuplicate.Count > iBeforeCount)
                                {
                                    foreach (Visio.Page ovNextPage in odictPagesToDuplicate.Values)
                                    {
                                        if (!quePagesToProcess.Contains(ovNextPage))
                                        {
                                            quePagesToProcess.Enqueue(ovNextPage);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }


            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in DoesPageContainWireMates " + ex.Message, "VisAssist");
            }
            finally
            {
                //make sure events are on
                Globals.ThisAddIn.Application.EventsEnabled = -1;
            }
            return odictPagesToDuplicate;
        }

        private static Dictionary<string, Visio.Page> IsWireMateOnSamePage(Shape ovShape, ref Dictionary<string, Visio.Page> odictPagesToDuplicate)
        {
            try
            {
                string sPageID = ovShape.ContainingPage.PageSheet.Cells["User.PageID"].get_ResultStr(0);

                string sWirePairID = ovShape.Cells["User.WirePairID"].get_ResultStr(0);
                string sWireRole = ovShape.Cells["User.WireRole"].get_ResultStr(0);
                string sMateID = GetMateID(sWirePairID, sWireRole);
                string sMatePageID = "";

                sMatePageID = WireUtilities.GetColumnInfoInWireShapesTableFromDatabase("PageID", sMateID);
                if (sMatePageID != sPageID)
                {
                    string sPageName = PageUtilities.GetColumnInfoInPagesTableFromDatabase("PageName", sMatePageID);
                    //the mates are not on the same page...

                    foreach (Visio.Page ovPossiblePage in ovShape.Document.Pages)
                    {
                        if (ovPossiblePage.Name == sPageName)
                        {
                            if (!odictPagesToDuplicate.ContainsKey(ovPossiblePage.Name))
                            {
                                odictPagesToDuplicate.Add(ovPossiblePage.Name, ovPossiblePage);
                            }

                        }
                    }
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in IsWireMateOnSamePage " + ex.Message, "VisAssist");
            }
            finally
            {
                //make sure events are on
                Globals.ThisAddIn.Application.EventsEnabled = -1;
            }
            return odictPagesToDuplicate;
        }

    }
}
