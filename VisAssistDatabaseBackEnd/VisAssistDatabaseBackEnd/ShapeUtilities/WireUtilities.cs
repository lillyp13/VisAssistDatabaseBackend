using Microsoft.Office.Interop.Visio;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using VisAssistDatabaseBackEnd.DataUtilities;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Visio = Microsoft.Office.Interop.Visio;

namespace VisAssistDatabaseBackEnd.ShapeUtilities
{
    internal class WireUtilities
    {
        //CRUD ACTIONS
        internal static void AddWireToDatabase(MultipleRecordUpdates oPrimaryWireRecord, MultipleRecordUpdates oSecondaryWireRecord)
        {
            try
            {

                //double check to make sure the record doesn't exist...
                bool bDoesRecordExist = DatabaseUtilities.DoesRecordExist(DatabaseUtilities.SqlTables.WireShapesTable.sWireShapeTable, oPrimaryWireRecord.ruRecords[0].sId);
                if (!bDoesRecordExist)
                {
                    //this is our final safeguard from adding multiples and getting an error-log it if this occurs...
                    //this will insert the primary wire to the db
                    DatabaseUtilities.BuildInsertSqlForMultipleRecords(DatabaseUtilities.SqlTables.WireShapesTable.sWireShapeTable, oPrimaryWireRecord);
                    DatabaseUtilities.BuildInsertSqlForMultipleRecords(DatabaseUtilities.SqlTables.WireShapesTable.sWireShapeTable, oSecondaryWireRecord);
                    //will also need to add it to the wire_pairs_table....
                    //and also add the ovSeconaryWire to the db...
                    //now we need to add the primary wire id and the secondary wire id as well as the wirepairid to the wire_pairs_table
                    MultipleRecordUpdates oWirePairRecord = BuildWirePairInfo(oPrimaryWireRecord, oSecondaryWireRecord);

                    if (oWirePairRecord.ruRecords != null)
                    {


                        DatabaseUtilities.BuildInsertSqlForMultipleRecords(DatabaseUtilities.SqlTables.WirePairsTable.sWirePairsTable, oWirePairRecord);
                    }
                }
                else
                {
                    //logging here because we tried to add a duplicate of a wire...
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in AddWireToDatabase " + ex.Message, "VisAssist");
            }


        }
        internal static void DeleteWireFromDatabase(Visio.Shape ovShape)
        {
            try
            {


                MultipleRecordUpdates oWireRecord = BuildWireShapeInfo(ovShape, "", false);

                //before deleting it in the DB we should also delete the secondary wire off of the page (or the primary-whatever is the opposite..)
                DatabaseUtilities.BuildDeleteSqlForMultipleRecords(DatabaseUtilities.SqlTables.WireShapesTable.sWireShapeTable, oWireRecord);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in DeleteWireFromDatabase " + ex.Message, "VisAssist");
            }
        }

        internal static void UpdateWireInDatabase(Visio.Shape ovShape)
        {
            try
            {
                string sShapeID = ovShape.Cells["User.ShapeID"].get_ResultStr(0);
                //get the WirePairID for ovShape
                string sWirePairID = WireUtilities.GetColumnInfoInWireShapesTableFromDatabase("WirePairID", sShapeID);
                //this gets' called when the user moves a shape...
                MultipleRecordUpdates oWireInfo = BuildWireShapeInfo(ovShape, sWirePairID, false);
                DatabaseUtilities.BuildUpdateSqlForMultipleRecords(DatabaseUtilities.SqlTables.WireShapesTable.sWireShapeTable, oWireInfo);

                //we also want to update the mates information and the features in visio (if the user changed the priamry wires # of conductors we need to make the same thing happen to its mate
                if (!Globals.ThisAddIn.Application.IsUndoingOrRedoing)
                {
                    WireUtilities.MatchWireFeatures(ovShape, sWirePairID);
                }


                //this needs to update this wires mates location...
                UpdateWireGridLocation(ovShape, oWireInfo);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in UpdateWireInDatabase " + ex.Message, "VisAssist");
            }
        }




        //VISIO ACTIONS

        internal static void AddWire(Visio.Shape ovShape, ref List<string> lstWires, bool bNewWire)
        {
            //this determines what kind of wire shape we need to drop, builds the information and adds to the database...
            try
            {

                //check to see if the wireshape we are dropping (ovShape) is a primary or secondary wire..
                string sWireRole = ovShape.Cells["User.WireRole"].get_ResultStr(0);
                Visio.Shape ovPrimaryWire = null;
                Visio.Shape ovSecondaryWire = null;
                MultipleRecordUpdates oPrimaryWireRecord;
                MultipleRecordUpdates oSecondaryWireRecord;
                string sKey = ovShape.ID + "|" + ovShape.ContainingPage.Name;
                if (!Globals.ThisAddIn.m_MatesMated.Contains(sKey))
                {

                    if (sWireRole == "P")
                    {
                        //ovShape is a primary wire
                        //drop another wire


                        //BEFORE WE BLINDLY DROP A NEW WIRE FOR THIS SHAPE CHECK IF IT IS IN OUR SELECTION TO BE DROPPED...
                        if (Globals.ThisAddIn.m_MatesInSelection.Count > 0)
                        {
                            //check if the mate for this shape is in the selection (therefore we dont' need to drop another wire)

                            //get the mate shape ..
                            ovPrimaryWire = ovShape;
                            ovSecondaryWire = Globals.ThisAddIn.m_MatesInSelection[sKey].ovMateShape;

                            string sMateKey = ovSecondaryWire.ID + "|" + ovSecondaryWire.ContainingPage.Name;
                            Globals.ThisAddIn.m_MatesMated.Add(sKey);
                            Globals.ThisAddIn.m_MatesMated.Add(sMateKey);
                        }
                        else
                        {
                            //add normally
                            ovPrimaryWire = ovShape;
                            ovSecondaryWire = AddOtherWire(ovShape, "Secondary");
                        }
                        oPrimaryWireRecord = AddWireShapeInfo(ovPrimaryWire, ovSecondaryWire);
                        //need to add the wirepairid to the otherwire...
                        ovShape.Application.EventsEnabled = 0;
                        ovSecondaryWire.Cells["User.WirePairID"].Formula = VisioUtilities.Application.FormatStringForVisio(oPrimaryWireRecord.ruRecords[0].odictColumnValues["WirePairID"]);
                        ovShape.Application.EventsEnabled = -1;
                        oSecondaryWireRecord = BuildWireShapeInfo(ovSecondaryWire, oPrimaryWireRecord.ruRecords[0].odictColumnValues["WirePairID"], true);

                        lstWires.Add(oPrimaryWireRecord.ruRecords[0].sId);
                        lstWires.Add(oSecondaryWireRecord.ruRecords[0].sId);

                    }
                    else
                    {
                        if (Globals.ThisAddIn.m_MatesInSelection != null)
                        {
                            ovSecondaryWire = ovShape;
                            ovPrimaryWire = Globals.ThisAddIn.m_MatesInSelection[sKey].ovMateShape;
                            string sMateKey = ovPrimaryWire.ID + "|" + ovPrimaryWire.ContainingPage.Name;
                            Globals.ThisAddIn.m_MatesMated.Add(sKey);
                            Globals.ThisAddIn.m_MatesMated.Add(sMateKey);
                        }
                        else
                        {
                            //add normally...

                            //the user copied a secondary wire we should create a priamry wire...
                            //ovShape is a secondary wire
                            ovSecondaryWire = ovShape;
                            ovPrimaryWire = AddOtherWire(ovSecondaryWire, "Primary");
                        }
                        oSecondaryWireRecord = AddWireShapeInfo(ovSecondaryWire, ovSecondaryWire);
                        //need to add the wirepairid to the otherwire...
                        ovShape.Application.EventsEnabled = 0;
                        ovPrimaryWire.Cells["User.WirePairID"].Formula = VisioUtilities.Application.FormatStringForVisio(oSecondaryWireRecord.ruRecords[0].odictColumnValues["WirePairID"]);
                        ovShape.Application.EventsEnabled = -1;
                        oPrimaryWireRecord = BuildWireShapeInfo(ovPrimaryWire, oSecondaryWireRecord.ruRecords[0].odictColumnValues["WirePairID"], true);

                        lstWires.Add(oPrimaryWireRecord.ruRecords[0].sId);
                        lstWires.Add(oSecondaryWireRecord.ruRecords[0].sId);

                    }


                    //this builds the information and then runs the sql to add the wire shape to the wire_shapes_table

                    //MultipleRecordUpdates oPrimaryWireRecord = AddWireShapeInfo(ovPrimaryWire, ovSecondaryWire);
                    //MultipleRecordUpdates oSecondaryWireRecord = BuildWireShapeInfo(ovSecondaryWire, oPrimaryWireRecord.ruRecords[0].odictColumnValues["WirePairID"]);
                    //check if the record already exists (i think this event is firing twice possibly)
                    bool bDoesRecordExist = DatabaseUtilities.DoesRecordExist(DatabaseUtilities.SqlTables.WireShapesTable.sWireShapeTable, oPrimaryWireRecord.ruRecords[0].sId);

                    if (!bDoesRecordExist)
                    {
                        string sFileID = ovShape.Document.DocumentSheet.Cells["User.FileID"].get_ResultStr(0);
                        string sWirePrefix = "";
                        string sRGBColor = "";

                        //need to add the next.. WirePrefix and the wire color to the db 
                        string sNextWireNumber = FileUtilities.GetColumnInfoInFilesTableFromDatabase("NextWireNumber", sFileID);
                        //string sNextColor = FileUtilities.GetColumnInfoInFilesTableFromDatabase("NextWireColor", sFileID);
                        //update the wire with sNextWireNumber...
                        ovShape.Application.EventsEnabled = 0;
                        sWirePrefix = "W-" + sNextWireNumber;
                        ovPrimaryWire.Cells["Prop.WireLabel"].Formula = VisioUtilities.Application.FormatStringForVisio(sWirePrefix);
                        ovSecondaryWire.Cells["Prop.WireLabel"].Formula = VisioUtilities.Application.FormatStringForVisio(sWirePrefix);
                        ovShape.Application.EventsEnabled = -1;

                        sRGBColor = GetAndUpdateNextWireColor(sFileID);
                        //set the color to the wires we dropped...
                        ovShape.Application.EventsEnabled = 0;
                        ovPrimaryWire.Cells["User.WireColor"].Formula = VisioUtilities.Application.FormatStringForVisio(sRGBColor);
                        ovSecondaryWire.Cells["User.WireColor"].Formula = VisioUtilities.Application.FormatStringForVisio(sRGBColor);
                        ovShape.Application.EventsEnabled = -1;


                        //need to update the nextwirenumber and nextwirecolor in the file table for this file...
                        IncreaseNextWireNumber(sFileID);

                        oPrimaryWireRecord.ruRecords[0].odictColumnValues["WireLabel"] = sWirePrefix;
                        oSecondaryWireRecord.ruRecords[0].odictColumnValues["WireLabel"] = sWirePrefix;
                        oPrimaryWireRecord.ruRecords[0].odictColumnValues["Color"] = sRGBColor;
                        AddWireToDatabase(oPrimaryWireRecord, oSecondaryWireRecord);
                        ////now we want to add the wire pairs grid location to the correct wire (primary should point to secondary, secondary should point to primary..)
                        AddWireGridLocation(oPrimaryWireRecord, oSecondaryWireRecord, ovPrimaryWire, ovSecondaryWire);

                    }
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in AddWireToDatabase " + ex.Message, "VisAssist");
            }
            finally
            {
                ovShape.Application.EventsEnabled = -1;
            }
        }

        private static void MatchWireFeatures(Shape ovShape, string sWirePairID)
        {
            try
            {
                Visio.Document ovDocument = ovShape.Document;
                //this will update the mates features to match ovShape...
                //get the mates shape in visio
                string sMateID = "";
                string sWireRole = ovShape.Cells["User.WireRole"].get_ResultStr(0);
                switch (sWireRole)
                {
                    case "P":
                        {
                            sMateID = GetColumnInfoInWirePairsTableFromDatabase("SecondaryWireID", sWirePairID);
                            break;
                        }
                    case "S":
                        {
                            sMateID = GetColumnInfoInWirePairsTableFromDatabase("PrimaryWireID", sWirePairID);
                            break;
                        }
                }

                string sMatePageID = GetColumnInfoInWireShapesTableFromDatabase("PageID", sMateID);
                foreach (Visio.Page ovPage in ovDocument.Pages)
                {
                    if (ovPage.PageSheet.CellExists["User.PageID", 0] == -1)
                    {
                        string sPageIDToCheck = ovPage.PageSheet.Cells["User.PageID"].get_ResultStr(0);
                        if (sPageIDToCheck == sMatePageID)
                        {
                            //this is the page the mate lives on
                            foreach (Visio.Shape ovShapeToCheck in ovPage.Shapes)
                            {
                                if (ovShapeToCheck.CellExists["User.Class", 0] == -1)
                                {
                                    if (ovShapeToCheck.Cells["User.Class"].get_ResultStr(0) == "SmartWire")
                                    {
                                        string sShapeIDToCheck = ovShapeToCheck.Cells["User.ShapeID"].get_ResultStr(0);
                                        if (sShapeIDToCheck == sMateID)
                                        {
                                            //this is the mate shape
                                            UpdateMatesFeatures(ovShapeToCheck, ovShape, sWirePairID);
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
                MessageBox.Show("Error in MatchWireFeatures " + ex.Message, "VisAssist");
            }
        }

        private static void UpdateMatesFeatures(Visio.Shape ovShapeToUpdate, Visio.Shape ovShape, string sWirePairID)
        {
            try
            {

                //need to match the # of conductors, color, wire label, auto labelling, and all the conductor labels...
                string sPageID = ovShape.ContainingPage.PageSheet.Cells["User.PageID"].get_ResultStr(0);
                Dictionary<string, string> oDictWireInfo = GatherWireInformation(ovShape, sPageID, sWirePairID);

                //based on the info in oDictWireInfo update ovShapeToUpdate with the values..

                //go through the dicionaty and pull out the following values:
                string sColor = oDictWireInfo["Color"];

                string sWireLabel = oDictWireInfo["WireLabel"];


                int iNumberOfConductors = Convert.ToInt32(oDictWireInfo["ConductorCount"]);
                int iAutoLabel = Convert.ToInt32(oDictWireInfo["AutoLabeling"]);
                string sConductor1 = oDictWireInfo["Conductor1Label"];
                string sConductor2 = oDictWireInfo["Conductor2Label"];
                string sConductor3 = oDictWireInfo["Conductor3Label"];
                string sConductor4 = oDictWireInfo["Conductor4Label"];
                string sConductor5 = oDictWireInfo["Conductor5Label"];
                string sConductor6 = oDictWireInfo["Conductor6Label"];
                string sConductor7 = oDictWireInfo["Conductor7Label"];
                string sConductor8 = oDictWireInfo["Conductor8Label"];
                string sConductor9 = oDictWireInfo["Conductor9Label"];
                string sConductor10 = oDictWireInfo["Conductor10Label"];

                ovShapeToUpdate.Cells["Prop.NumberOfConductors"].ResultIU = iNumberOfConductors;
                ovShapeToUpdate.Cells["Prop.WireLabel"].Formula = VisioUtilities.Application.FormatStringForVisio(sWireLabel);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in UpdateMatesFeatures " + ex.Message, "VisAssist");
            }

        }




        internal static Visio.Shape AddOtherWire(Visio.Shape ovWire, string sWireRoleToCreate)
        {
            //this actually drops the new wire shape on the page, whether or not it is a secondary or primary...
            try
            {
                //drop a new wire shape that will be the secondary wire...
                //get the TestStencil.vssx ...
                Visio.Application ovApp = ovWire.Application;
                string sProjectID = ovWire.Document.DocumentSheet.Cells["User.ProjectID"].get_ResultStr(0);
                string sFileID = ovWire.Document.DocumentSheet.Cells["User.FileID"].get_ResultStr(0);
                string sPageID = ovWire.ContainingPage.PageSheet.Cells["User.PageID"].get_ResultStr(0);


                Visio.Document ovStencilDoc = null;

                foreach (Visio.Document ovDoc in ovApp.Documents)
                {
                    if (ovDoc.Type == Visio.VisDocumentTypes.visTypeStencil && ovDoc.Name.Equals("TestStencil.vssx", StringComparison.OrdinalIgnoreCase))
                    {
                        ovStencilDoc = ovDoc;
                        break;
                    }
                }

                if (ovStencilDoc == null)
                {
                    MessageBox.Show("Please open the stencil.", "VisAssist");
                    return null;
                }
                Visio.Master ovWireMaster = ovStencilDoc.Masters["SmartWire"];

                //turn off events before dropping it
                ovWire.Document.Application.EventsEnabled = 0;
                Visio.Shape ovOtherWire = ovWire.ContainingPage.Drop(ovWireMaster, 5, 5);

                //TEMPOARILY ADD THE USERWIREPAIRID
                ovOtherWire.AddNamedRow((short)Visio.VisSectionIndices.visSectionUser, "WirePairID", 0);
                //now we have dropped the shape
                //change the role...
                if (sWireRoleToCreate == "Secondary")
                {
                    ovOtherWire.Cells["User.WireRole"].Formula = VisioUtilities.Application.FormatStringForVisio("S");
                }
                else
                {
                    ovOtherWire.Cells["User.WireRole"].Formula = VisioUtilities.Application.FormatStringForVisio("P");
                }

                //set the number of conductors based on the number of conductors in ovShape
                int iNumberOfConductors = Convert.ToInt32(ovWire.Cells["Prop.NumberOfConductors"].ResultIU);
                ovOtherWire.Cells["Prop.NumberOfConductors"].ResultIU = iNumberOfConductors;

                string sShapeID = ShapesUtilities.GenerateShapeID(sProjectID, sFileID, sPageID, ovOtherWire.Name, DateTime.Now);
                ovOtherWire.Cells["User.ShapeID"].Formula = VisioUtilities.Application.FormatStringForVisio(sShapeID);
                // ovOtherWire.Cells["User.PageID"].Formula = VisioUtilities.Application.FormatStringForVisio(sPageID);

                ovOtherWire.Document.Application.EventsEnabled = -1;

                return ovOtherWire;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in BuildWirePairInfo " + ex.Message, "VisAssist");
            }
            finally
            {
                ovWire.Document.Application.EventsEnabled = -1;
            }
            return null;
        }
        public static void UpdateWireGridLocation(Visio.Shape ovShape, MultipleRecordUpdates oWireInfo)
        {
            //this updates the user cell in the shape to show where the mate lives...
            try
            {
                //need to get the mate shape...
                Visio.Document ovDocument = ovShape.Document;
                string sState = oWireInfo.ruRecords[0].odictColumnValues["WireRole"];
                string sGridLocation = oWireInfo.ruRecords[0].odictColumnValues["GridLocation"];
                string sShapeID;
                string sWirePairID;
                string sMatesID = "";
                string sPageID;
                string sPageIDToCheck;
                string sShapeIDToCheck;

                sShapeID = oWireInfo.ruRecords[0].sId;
                sWirePairID = GetColumnInfoInWireShapesTableFromDatabase("WirePairID", sShapeID);
                switch (sState)
                {
                    case "P":
                        {
                            //the user moved the primary wire, we need to update the secondary wires grid location to show where the primary wire is located now...
                            //now that we have the WirePairID get the other wire...
                            sMatesID = GetColumnInfoInWirePairsTableFromDatabase("SecondaryWireID", sWirePairID);

                            break;
                        }
                    case "S":
                        {
                            sMatesID = GetColumnInfoInWirePairsTableFromDatabase("PrimaryWireID", sWirePairID);
                            break;
                        }
                }

                //ok now we have the mate's id get the page it is on in order to find the shape itself in visio..
                sPageID = GetColumnInfoInWireShapesTableFromDatabase("PageID", sMatesID);

                //ok now we have the pageid and shape id find the actual shape on the page
                foreach (Visio.Page ovPage in ovDocument.Pages)
                {
                    if (ovPage.PageSheet.CellExists["User.PageID", 0] == -1)
                    {
                        sPageIDToCheck = ovPage.PageSheet.Cells["User.PageID"].get_ResultStr(0);
                        if (sPageIDToCheck == sPageID)
                        {
                            //this is the page our shape is on..
                            foreach (Visio.Shape ovShapeToCheck in ovPage.Shapes)
                            {
                                if (ovShapeToCheck.CellExists["User.ShapeID", 0] == -1)
                                {
                                    sShapeIDToCheck = ovShapeToCheck.Cells["User.ShapeID"].get_ResultStr(0);
                                    if (sShapeIDToCheck == sMatesID)
                                    {
                                        //this is our shape to update...
                                        if (!Globals.ThisAddIn.Application.IsUndoingOrRedoing)
                                        {
                                            ovShapeToCheck.Application.EventsEnabled = 0;
                                            ovShapeToCheck.Cells["User.WireLocation"].Formula = VisioUtilities.Application.FormatStringForVisio(sGridLocation);
                                            ovShapeToCheck.Application.EventsEnabled = -1;
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
                MessageBox.Show("Error in UpdateWireGridLocation " + ex.Message, "VisAssist");
            }

        }

        public static void AddWireGridLocation(MultipleRecordUpdates oPrimaryWireRecord, MultipleRecordUpdates oSecondaryWireRecord, Visio.Shape ovShape, Visio.Shape ovSecondaryWire)
        {
            //this adds the grid location to both shapes for the first time..
            try
            {
                string sPrimaryGridLocation = oPrimaryWireRecord.ruRecords[0].odictColumnValues["GridLocation"];
                string sSecondaryGridLocation = oSecondaryWireRecord.ruRecords[0].odictColumnValues["GridLocation"];

                ovShape.Application.EventsEnabled = 0;
                ovShape.Cells["User.WireLocation"].Formula = VisioUtilities.Application.FormatStringForVisio(sSecondaryGridLocation); // add the secondary wire location to the primary wire
                ovSecondaryWire.Cells["User.WireLocation"].Formula = VisioUtilities.Application.FormatStringForVisio(sPrimaryGridLocation); //add the primary wire location to the secondary wire
                ovShape.Application.EventsEnabled = -1;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in UpdateWireGridLocation " + ex.Message, "VisAssist");
            }
            finally
            {
                ovShape.Application.EventsEnabled = -1;
            }

        }


        //HELPER BUILIDNG FUNCTIONS

        private static Dictionary<string, string> GatherWireInformation(Visio.Shape ovMainWire, string sPageID, string sWirePairID)
        {
            Dictionary<string, string> oDictFileValues = new Dictionary<string, string>();
            try
            {


                string sWireRole = ovMainWire.Cells["User.WireRole"].get_ResultStr(0);

                string sVersion = ovMainWire.Cells["User.Version"].get_ResultStr(0);
                string sClass = ovMainWire.Cells["User.Class"].get_ResultStr(0);
                string sColor = ovMainWire.Cells["User.WireColor"].get_ResultStr(0);

                string sWireLabel = ovMainWire.Cells["Prop.WireLabel"].get_ResultStr(0);
                string sShield = ovMainWire.Cells["Prop.Shield"].get_ResultStr(0);

                int iNumberOfConductors = (int)ovMainWire.Cells["Prop.NumberOfConductors"].ResultIU;
                string sConductor1 = ovMainWire.Cells["User.Conductor1AutoLabel"].get_ResultStr(0);
                string sConductor2 = ovMainWire.Cells["User.Conductor2AutoLabel"].get_ResultStr(0);
                string sConductor3 = ovMainWire.Cells["User.Conductor3AutoLabel"].get_ResultStr(0);
                string sConductor4 = ovMainWire.Cells["User.Conductor4AutoLabel"].get_ResultStr(0);
                string sConductor5 = ovMainWire.Cells["User.Conductor5AutoLabel"].get_ResultStr(0);
                string sConductor6 = ovMainWire.Cells["User.Conductor6AutoLabel"].get_ResultStr(0);
                string sConductor7 = ovMainWire.Cells["User.Conductor7AutoLabel"].get_ResultStr(0);
                string sConductor8 = ovMainWire.Cells["User.Conductor8AutoLabel"].get_ResultStr(0);
                string sConductor9 = ovMainWire.Cells["User.Conductor9AutoLabel"].get_ResultStr(0);
                string sConductor10 = ovMainWire.Cells["User.Conductor10AutoLabel"].get_ResultStr(0);



                //oDictFileValues.Add("ProjectID", sProjectID);
                //oDictFileValues.Add("FileID", sFileID);
                oDictFileValues.Add("PageID", sPageID);

                //add the WirePairID

                oDictFileValues.Add("WirePairID", sWirePairID);
                //add the ConnectionID
                oDictFileValues.Add("ConnectionID", "");
                string sWireName = ovMainWire.ID.ToString();
                //need to get the 
                oDictFileValues.Add("WireName", sWireName);

                oDictFileValues.Add("WireRole", sWireRole);


                oDictFileValues.Add("Version", sVersion);
                oDictFileValues.Add("Class", sClass);

                //add wire lable

                oDictFileValues.Add("WireLabel", sWireLabel);


                oDictFileValues.Add("Color", sColor);



                int iPageIndex = ovMainWire.ContainingPage.Index;
                string sHorizontalMarkers = "8;7;6;5;4;3;2;1"; //will need to get this from the page
                string sVertMarkers = "A;B;C;D;E;F;G;H";//will need to get this from the page based on vertical/horizontal/pagescale...
                double dPageWidth = ovMainWire.ContainingPage.PageSheet.Cells["PageWidth"].ResultIU;
                double dPageHeight = ovMainWire.ContainingPage.PageSheet.Cells["PageHeight"].ResultIU;



                double dLocalX = ovMainWire.Cells["Controls.JacketX"].ResultIU;
                double dLocalY = ovMainWire.Cells["Controls.JacketY"].ResultIU;

                // Convert to page coordinates
                double dPageX;
                double dPageY;

                ovMainWire.XYToPage(dLocalX, dLocalY, out dPageX, out dPageY);

                string sGridLocation = WireUtilities.GetWireGridLocation(dPageX, dPageY, dPageWidth, dPageHeight, sVertMarkers, sHorizontalMarkers, iPageIndex);




                //add the x location 
                oDictFileValues.Add("XLocation", dPageX.ToString());
                //add the y location 
                oDictFileValues.Add("YLocation", dPageY.ToString());
                //add the autolabelling
                //get the value of autolabelling from Prop.ConductorLabeling
                string sLabelling = ovMainWire.Cells["Prop.ConductorLabeling"].get_ResultStr(0);
                int iAutoLabel = 0;
                if (sLabelling == "Auto Labeling")
                {
                    iAutoLabel = 0;
                }
                else
                {
                    iAutoLabel = 1; //this is manual labelling...
                }
                oDictFileValues.Add("AutoLabeling", iAutoLabel.ToString()); //integer...
                oDictFileValues.Add("ConductorCount", iNumberOfConductors.ToString());



                oDictFileValues.Add("Conductor1Label", sConductor1);
                oDictFileValues.Add("Conductor2Label", sConductor2);
                oDictFileValues.Add("Conductor3Label", sConductor3);
                oDictFileValues.Add("Conductor4Label", sConductor4);
                oDictFileValues.Add("Conductor5Label", sConductor5);
                oDictFileValues.Add("Conductor6Label", sConductor6);
                oDictFileValues.Add("Conductor7Label", sConductor7);
                oDictFileValues.Add("Conductor8Label", sConductor8);
                oDictFileValues.Add("Conductor9Label", sConductor9);
                oDictFileValues.Add("Conductor10Label", sConductor10);

                oDictFileValues.Add("GridLocation", sGridLocation);
                //add the show shield
                oDictFileValues.Add("Shield", sShield);

            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in GatherWireInformation " + ex.Message, "VisAssist");
            }

            return oDictFileValues;
        }
        internal static MultipleRecordUpdates BuildWireShapeInfo(Visio.Shape ovMainWire, string sWirePairID, bool bAddNewShapeID)
        {
            //this builds the wire info based on the values in the shape itself
            //this is more of an update or gather not an add...
            try
            {
                bool bIsValidShape = VisioUtilities.Application.IsShapeValid(ovMainWire);
                if (bIsValidShape)
                {




                    Visio.Document ovDoc = ovMainWire.ContainingPage.Document;
                    string sProjectID = ovDoc.DocumentSheet.Cells["User.ProjectID"].get_ResultStr(0);
                    string sFileID = ovDoc.DocumentSheet.Cells["User.FileID"].get_ResultStr(0);
                    //string sPageID = ovMainWire.Cells["User.PageID"].get_ResultStr(0);
                    string sPageID = ""; //i think i always want to get it fromt he page...
                    if (sPageID == "")
                    {
                        //get the pageID from the page
                        sPageID = ovMainWire.ContainingPage.PageSheet.Cells["User.PageID"].get_ResultStr(0);
                    }

                    string sWireRole = ovMainWire.Cells["User.WireRole"].get_ResultStr(0);

                    Dictionary<string, string> oDictFileValues = GatherWireInformation(ovMainWire, sPageID, sWirePairID);


                    string sID = "";
                    if (ovMainWire.CellExists["User.ShapeID", 0] == -1)
                    {
                        sID = ovMainWire.Cells["User.ShapeID"].get_ResultStr(0);

                        if (bAddNewShapeID)
                        {
                            sID = ShapesUtilities.GenerateShapeID(sProjectID, sFileID, sPageID, ovMainWire.Name, DateTime.Now);
                            //add this to the shape itself 
                            //may need to check we are not undoing/redoing...
                            ovMainWire.Application.EventsEnabled = 0;
                            ovMainWire.Cells["User.ShapeID"].Formula = VisioUtilities.Application.FormatStringForVisio(sID);
                            ovMainWire.Application.EventsEnabled = -1;
                        }
                    }

                    if (sWirePairID == "")
                    {
                        //we didn't pass in the sWirePairID which means this exists in the db and this build is about updating something or deleting soemthing
                        //get the WirePairID from the db for this sID
                        sWirePairID = GetColumnInfoInWireShapesTableFromDatabase("WirePairID", sID);


                    }
                    //make this undoable..
                    //int iUndoScope = ovMainWire.Application.BeginUndoScope("Update PairID");
                    //ovMainWire.Application.EventsEnabled = 0;
                    //ovMainWire.Cells["User.WirePairID"].Formula = VisioUtilities.Application.FormatStringForVisio(sWirePairID);
                    //ovMainWire.Application.EndUndoScope(iUndoScope, false);
                    //ovMainWire.Application.EventsEnabled = -1;

                    oDictFileValues["WirePairID"] = sWirePairID;





                    RecordUpdate ruFileRecord = new RecordUpdate();
                    ruFileRecord.sPrimaryKeyColumn = DatabaseUtilities.SqlTables.WireShapesTable.sWireShapeTablePK;
                    ruFileRecord.sId = sID;
                    ruFileRecord.odictColumnValues = oDictFileValues;

                    return new MultipleRecordUpdates(new List<RecordUpdate> { ruFileRecord });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in BuildWireShapeInfo " + ex.Message, "VisAssist");
            }
            return new MultipleRecordUpdates();
        }
        internal static MultipleRecordUpdates AddWireShapeInfo(Visio.Shape ovMainWire, Visio.Shape ovSecondaryWire)
        {
            //this is when we are adding a wire for the first time to add/build the information that will go in the db
            try
            {
                Visio.Document ovDoc = ovMainWire.ContainingPage.Document;
                string sProjectID = ovDoc.DocumentSheet.Cells["User.ProjectID"].get_ResultStr(0);
                string sFileID = ovDoc.DocumentSheet.Cells["User.FileID"].get_ResultStr(0);

                //this si the first time we are addding this shape to the db..
                string sPageID = ovMainWire.ContainingPage.PageSheet.Cells["User.PageID"].get_ResultStr(0);
                //turn off events before adding the sPageID to the shape..
                //ovMainWire.Application.EventsEnabled = 0;
                //ovMainWire.Cells["User.PageID"].Formula = VisioUtilities.Application.FormatStringForVisio(sPageID);
                //ovMainWire.Application.EventsEnabled = -1;



                string sID = "";
                string sWirePairID = "";
                if (ovMainWire.CellExists["User.ShapeID", 0] == -1)
                {

                    sID = ShapesUtilities.GenerateShapeID(sProjectID, sFileID, sPageID, ovMainWire.Name, DateTime.Now);
                    //turn off events firs
                    ovMainWire.Application.EventsEnabled = 0;
                    ovMainWire.Cells["User.ShapeID"].Formula = "\"" + sID + "\"";
                    ovMainWire.Application.EventsEnabled = -1;

                    //we need to also generate the WirePairID

                    sWirePairID = WireUtilities.GenerateWirePairID(sProjectID, sFileID, sPageID, ovMainWire.Name, ovSecondaryWire.Name, DateTime.Now);
                    ovMainWire.Application.EventsEnabled = 0;
                    ovMainWire.Cells["User.WirePairID"].Formula = VisioUtilities.Application.FormatStringForVisio(sWirePairID);
                    ovMainWire.Application.EventsEnabled = -1;

                }
                Dictionary<string, string> oDictFileValues = GatherWireInformation(ovMainWire, sPageID, sWirePairID);



                RecordUpdate ruFileRecord = new RecordUpdate();
                ruFileRecord.sPrimaryKeyColumn = DatabaseUtilities.SqlTables.WireShapesTable.sWireShapeTablePK;
                ruFileRecord.sId = sID;
                ruFileRecord.odictColumnValues = oDictFileValues;

                return new MultipleRecordUpdates(new List<RecordUpdate> { ruFileRecord });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in BuildWireShapeInfo " + ex.Message, "VisAssist");
            }
            return new MultipleRecordUpdates();
        }

        internal static MultipleRecordUpdates BuildWirePairInfo(MultipleRecordUpdates oPrimaryWireRecord, MultipleRecordUpdates oSecondaryWireRecord)
        {
            try
            {
                string sPrimaryWireID = oPrimaryWireRecord.ruRecords[0].sId;
                string sSecondaryWireID = oSecondaryWireRecord.ruRecords[0].sId;
                string sWirePairID = oPrimaryWireRecord.ruRecords[0].odictColumnValues["WirePairID"];


                Dictionary<string, string> oDictFileValues = new Dictionary<string, string>();
                oDictFileValues.Add("PrimaryWireID", sPrimaryWireID);
                oDictFileValues.Add("SecondaryWireID", sSecondaryWireID);


                RecordUpdate ruFileRecord = new RecordUpdate();
                ruFileRecord.sPrimaryKeyColumn = DatabaseUtilities.SqlTables.WirePairsTable.sWirePairsTablePK;
                ruFileRecord.sId = sWirePairID;
                ruFileRecord.odictColumnValues = oDictFileValues;

                return new MultipleRecordUpdates(new List<RecordUpdate> { ruFileRecord });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in BuildWirePairInfo " + ex.Message, "VisAssist");
            }
            return new MultipleRecordUpdates();
        }

        public static string GetWireGridLocation(
   double dPointX,
   double dPointY,
   double dPageWidth,
   double dPageHeight,
   string sVertMarkersPrompt,
   string sHorzMarkersPrompt,
   int iPageIndex)
        {
            string[] saVertMarkers = sVertMarkersPrompt.Split(';');
            string[] saHorzMarkers = sHorzMarkersPrompt.Split(';');

            if (saVertMarkers.Length == 0 || saHorzMarkers.Length == 0)
                throw new ArgumentException("Invalid grid markers");

            // ---- X GRID ----
            double dXRatio = dPointX / dPageWidth;
            int iXIndex = (int)Math.Floor(dXRatio * saHorzMarkers.Length);
            iXIndex = Math.Max(0, Math.Min(saHorzMarkers.Length - 1, iXIndex));

            string sXMarker = saHorzMarkers[iXIndex];

            // ---- Y GRID ----
            double dYRatio = dPointY / dPageHeight;
            int iYIndex = (int)Math.Floor(dYRatio * saVertMarkers.Length);
            iYIndex = Math.Max(0, Math.Min(saVertMarkers.Length - 1, iYIndex));

            string sYMarker = saVertMarkers[iYIndex];

            return $"({iPageIndex}, {sXMarker}{sYMarker})";
        }

        internal static string GenerateWirePairID(string sProjectID, string sFileID, string sPageID, string sPrimaryWire, string sSecondaryWire, DateTime now)
        {
            string input = sProjectID + sFileID + sPageID + sPrimaryWire + sSecondaryWire + now.ToString("yyyy-MM-dd HH:mm:ss"); // formatted
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
        private static string GetAndUpdateNextWireColor(string sFileID)
        {
            string sRGBFormula = "";
            try
            {
                int iCurrentIndex = GetNextWireColorIndex(sFileID);

                // Safety check (if DB somehow has bad value)
                if (iCurrentIndex < 0 || iCurrentIndex >= DatabaseUtilities.WireColorOrder.Length)
                    iCurrentIndex = 0;

                string sColorName = DatabaseUtilities.WireColorOrder[iCurrentIndex];

                // This is the RGB formula you will apply to Visio
                sRGBFormula = DatabaseUtilities.ColorMap[sColorName];

                int iNextIndex = (iCurrentIndex + 1) % DatabaseUtilities.WireColorOrder.Length;

                UpdateNextWireColor(sFileID, iNextIndex);

            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in GetAndUpdateNextWireColor " + ex.Message, "VisAssit");
            }
            return sRGBFormula;
        }


        //HELPER SQL FUNCTIONS
        internal static string GetColumnInfoInWirePairsTableFromDatabase(string sColumnName, string sWirePairID)
        {
            //this is usually going to get a mates id
            try
            {
                string sSpecificPiece = "";
                //use the dbPath which is the db file and open it and get the ProjectID from the project_table
                using (SQLiteConnection sqliteconConnection = new SQLiteConnection(DatabaseConfig.ConnectionString))
                {
                    //logging here
                    sqliteconConnection.Open();
                    string sSQL = $"SELECT [{sColumnName}] FROM [wire_pairs_table] WHERE [WirePairID] = @Id LIMIT 1";

                    using (SQLiteCommand sqlcmdCommand = new SQLiteCommand(sSQL, sqliteconConnection))
                    {
                        sqlcmdCommand.Parameters.AddWithValue("@Id", sWirePairID);

                        using (SQLiteDataReader sqlitereadReader = sqlcmdCommand.ExecuteReader())
                        {
                            if (sqlitereadReader.Read())
                            {
                                // Safe retrieval of value as string
                                object dbValue = sqlitereadReader[sColumnName];
                                return dbValue == DBNull.Value ? "" : dbValue.ToString();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in GetColumnInfoInWirePairsTableFromDatabase " + ex.Message, "VisAssist");
            }
            return "";
        }
        internal static string GetColumnInfoInWireShapesTableFromDatabase(string sColumnName, string sID)
        {
            //this usually goes to gather the WirePairID
            try
            {
                string sSpecificPiece = "";
                //use the dbPath which is the db file and open it and get the ProjectID from the project_table
                using (SQLiteConnection sqliteconConnection = new SQLiteConnection(DatabaseConfig.ConnectionString))
                {
                    //logging here
                    sqliteconConnection.Open();
                    string sSQL = $"SELECT [{sColumnName}] FROM [wire_shapes_table] WHERE [ShapeID] = @Id LIMIT 1";

                    using (SQLiteCommand sqlcmdCommand = new SQLiteCommand(sSQL, sqliteconConnection))
                    {
                        sqlcmdCommand.Parameters.AddWithValue("@Id", sID);

                        using (SQLiteDataReader sqlitereadReader = sqlcmdCommand.ExecuteReader())
                        {
                            if (sqlitereadReader.Read())
                            {
                                // Safe retrieval of value as string
                                object dbValue = sqlitereadReader[sColumnName];
                                return dbValue == DBNull.Value ? "" : dbValue.ToString();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in GetColumnInfoInProjectTableFromDatabase " + ex.Message, "VisAssist");
            }
            return "";
        }
        private static void IncreaseNextWireNumber(string sFileID)
        {
            try
            {


                using (SQLiteConnection sqliteconConnection = new SQLiteConnection(DatabaseConfig.ConnectionString))
                {
                    sqliteconConnection.Open();
                    string sSql = @"UPDATE files_table SET NextWireNumber = CAST(CAST(NextWireNumber AS INT) + 1 AS VARCHAR(50)) WHERE FileID = @FileID";
                    using (SQLiteCommand sqlitecmdCommand = new SQLiteCommand(sSql, sqliteconConnection))
                    {
                        sqlitecmdCommand.Parameters.AddWithValue("@FileID", sFileID);
                        sqlitecmdCommand.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in IncreaseNextWireNumber " + ex.Message, "VisAssist");
            }
        }
        private static void UpdateNextWireColor(string fileID, int nextIndex)
        {
            try
            {
                string sNextColorName = DatabaseUtilities.WireColorOrder[nextIndex];

                using (SQLiteConnection conn = new SQLiteConnection(DatabaseConfig.ConnectionString))
                {
                    conn.Open();

                    string sql = "UPDATE files_table SET NextWireColor = @Color WHERE FileID = @Id";

                    using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Color", sNextColorName);
                        cmd.Parameters.AddWithValue("@Id", fileID);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in UpdateNextWireColor " + ex.Message, "VisAssist");
            }
        }
        internal static void ResetWireColorAndNumber()
        {
            //we want to reset the color to be the first color (yellow) and reset the next wire number to 1...
            //get the current FileID..
            try
            {
                Visio.Document ovDocument = Globals.ThisAddIn.Application.ActiveDocument;
                if (ovDocument != null)
                {
                    //may want to check if this is one of our projects
                    if (ovDocument.DocumentSheet.CellExists["User.FileID", 0] == -1)
                    {
                        string sFileID = ovDocument.DocumentSheet.Cells["User.FileID"].get_ResultStr(0);
                        //now go set the NextWireColor to yellow and the NextWireNumber to 1 for this sFileID...
                        WireUtilities.UpdateNextWireColor(sFileID, 0); //reset to yellow...
                        WireUtilities.ResetWireNumber(sFileID);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in ResetWireColorAndNumber " + ex.Message, "VisAssist");
            }
        }
        private static int GetNextWireColorIndex(string sFileID)
        {
            int iIndex = 0;
            try
            {
                string sColor = FileUtilities.GetColumnInfoInFilesTableFromDatabase("NextWireColor", sFileID);

                if (string.IsNullOrWhiteSpace(sColor))
                    return 0; // default to first color

                iIndex = Array.IndexOf(DatabaseUtilities.WireColorOrder, sColor);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in GetNextWireColorIndex " + ex.Message, "VisAssist");
            }
            return iIndex >= 0 ? iIndex : 0; // fallback safety
        }

        private static void ResetWireNumber(string sFileID)
        {
            try
            {
                using (SQLiteConnection sqliteconConnection = new SQLiteConnection(DatabaseConfig.ConnectionString))
                {
                    sqliteconConnection.Open();
                    string sSql = @"UPDATE files_table SET NextWireNumber = 1 WHERE FileID = @FileID";
                    using (SQLiteCommand sqlitecmdCommand = new SQLiteCommand(sSql, sqliteconConnection))
                    {
                        sqlitecmdCommand.Parameters.AddWithValue("@FileID", sFileID);
                        sqlitecmdCommand.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in ResetWireNumber " + ex.Message, "VisAssist");
            }
        }
        internal static void CheckForWirePairsOnPageDuplicated(Dictionary<string, Shape> oDictWires)
        {
            //given the dicitonary oDictWires loop through it and check to see if its mate is also in the dictionary
            try
            {
                string sProjectID;
                string sFileID;
                string sPageID;
                string sFirstKey;
                string sSecondKey;
                List<string> oListWiresProcessed = new List<string>();

                foreach (Visio.Shape ovShape in oDictWires.Values)
                {
                    sProjectID = ovShape.Document.DocumentSheet.Cells["User.ProjectID"].get_ResultStr(0);
                    sFileID = ovShape.Document.DocumentSheet.Cells["User.FileID"].get_ResultStr(0);
                    sPageID = ovShape.ContainingPage.PageSheet.Cells["User.PageID"].get_ResultStr(0); //this was updated in OnPageDuplicated..
                    sFirstKey = ovShape.ID + "|" + ovShape.ContainingPage.Name;
                    if (!oListWiresProcessed.Contains(sFirstKey))
                    {


                        string sShapeID = ovShape.Cells["User.ShapeID"].get_ResultStr(0);
                        //based on the sShapeID grab the WirePairID to get the Mates ID...
                        string sWirePairID = GetColumnInfoInWireShapesTableFromDatabase("WirePairID", sShapeID);
                        //now get the mates id...
                        string sWireRole = ovShape.Cells["User.WireRole"].get_ResultStr(0);
                        string sMateID = "";
                        switch (sWireRole)
                        {
                            case "P":
                                {
                                    sMateID = GetColumnInfoInWirePairsTableFromDatabase("SecondaryWireID", sWirePairID);
                                    break;
                                }
                            case "S":
                                {
                                    sMateID = GetColumnInfoInWirePairsTableFromDatabase("PrimaryWireID", sWirePairID);
                                    break;
                                }
                        }

                        bool bMateInSelection = false;
                        //ok now we need to see if this mateid exists in the oDictWires
                        foreach (Visio.Shape ovShapeToCheck in oDictWires.Values)
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

                                string sNewWirePairID = GenerateWirePairID(sProjectID, sFileID, sPageID, ovShape.Name, ovShapeToCheck.Name, DateTime.Now);
                                ovShape.Application.EventsEnabled = 0;
                                ovShape.Cells["User.WirePairID"].Formula = VisioUtilities.Application.FormatStringForVisio(sNewWirePairID);
                                ovShapeToCheck.Cells["User.WirePairID"].Formula = VisioUtilities.Application.FormatStringForVisio(sNewWirePairID);
                                ovShape.Application.EventsEnabled = -1;


                                //we want to update the wire color and the number...
                                string sRGBFormula = GetAndUpdateNextWireColor(sFileID);
                                string sWireNumber = FileUtilities.GetColumnInfoInFilesTableFromDatabase("NextWireNumber", sFileID);
                                string sWirePrefix = "W-" + sWireNumber;
                                IncreaseNextWireNumber(sFileID);

                                //set the color and wire to the shapes...
                                ovShape.Application.EventsEnabled = 0;
                                ovShape.Cells["User.WireColor"].Formula = VisioUtilities.Application.FormatStringForVisio(sRGBFormula);
                                ovShapeToCheck.Cells["User.WireColor"].Formula = VisioUtilities.Application.FormatStringForVisio(sRGBFormula);
                                ovShape.Cells["Prop.WireLabel"].Formula = VisioUtilities.Application.FormatStringForVisio(sWirePrefix);
                                ovShapeToCheck.Cells["Prop.WireLabel"].Formula = VisioUtilities.Application.FormatStringForVisio(sWirePrefix);
                                ovShape.Application.EventsEnabled = -1;

                                //now we need to add these wires to the db..
                                MultipleRecordUpdates mruPrimaryRecord = new MultipleRecordUpdates();
                                MultipleRecordUpdates mruSecondaryRecord = new MultipleRecordUpdates();
                                switch (sWireRole)
                                {
                                    case "P":
                                        {
                                            mruPrimaryRecord = BuildWireShapeInfo(ovShape, sNewWirePairID, false);
                                            mruSecondaryRecord = BuildWireShapeInfo(ovShapeToCheck, sNewWirePairID, false);

                                            break;
                                        }
                                    case "S":
                                        {
                                            mruSecondaryRecord = BuildWireShapeInfo(ovShape, sNewWirePairID, false);
                                            mruPrimaryRecord = BuildWireShapeInfo(ovShapeToCheck, sNewWirePairID, false);

                                            break;
                                        }
                                }

                                sSecondKey = ovShapeToCheck.ID + "|" + ovShapeToCheck.ContainingPage.Name;
                                AddWireToDatabase(mruPrimaryRecord, mruSecondaryRecord);

                                //after adding the shapes to the database we need to update the gridlocation
                                switch (sWireRole)
                                {
                                    case "P":
                                        {
                                            UpdateWireGridLocation(ovShape, mruPrimaryRecord);
                                            UpdateWireGridLocation(ovShapeToCheck, mruSecondaryRecord);
                                            break;
                                        }
                                    case "S":
                                        {
                                            UpdateWireGridLocation(ovShape, mruSecondaryRecord);
                                            UpdateWireGridLocation(ovShapeToCheck, mruPrimaryRecord);
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
                            Visio.Shape ovOtherWire = AddOtherWire(ovShape, sWireRoleToCreate);
                            string sNewWirePairId = "";
                            if (bShapeIsPrimary)
                            {
                                sNewWirePairId = GenerateWirePairID(sProjectID, sFileID, sPageID, ovShape.Name, ovOtherWire.Name, DateTime.Now);
                            }
                            else
                            {
                                sNewWirePairId = GenerateWirePairID(sProjectID, sFileID, sPageID, ovOtherWire.Name, ovShape.Name, DateTime.Now);
                            }
                            //increase their wire number and color here before adding to the db...
                            string sWireColor = GetAndUpdateNextWireColor(sFileID);
                            string sWireNumber = FileUtilities.GetColumnInfoInFilesTableFromDatabase("NextWireNumber", sFileID);
                            string sWirePrefix = "W-" + sWireNumber;
                            IncreaseNextWireNumber(sFileID);

                            //update the wirelabel and color based on the next wire information
                            ovShape.Application.EventsEnabled = 0;
                            ovShape.Cells["Prop.WireLabel"].Formula = VisioUtilities.Application.FormatStringForVisio(sWirePrefix);
                            ovOtherWire.Cells["Prop.WireLabel"].Formula = VisioUtilities.Application.FormatStringForVisio(sWirePrefix);
                            ovShape.Cells["User.WireColor"].Formula = VisioUtilities.Application.FormatStringForVisio(sWireColor);
                            ovOtherWire.Cells["User.WireColor"].Formula = VisioUtilities.Application.FormatStringForVisio(sWireColor);
                            ovShape.Application.EventsEnabled = -1;


                            //now add the wires to the db
                            MultipleRecordUpdates mruPrimaryRecord = new MultipleRecordUpdates();
                            MultipleRecordUpdates mruSecondaryRecord = new MultipleRecordUpdates();
                            if (bShapeIsPrimary)
                            {
                                mruPrimaryRecord = BuildWireShapeInfo(ovShape, sNewWirePairId, false);
                                mruSecondaryRecord = BuildWireShapeInfo(ovOtherWire, sNewWirePairId, false);
                            }
                            else
                            {
                                mruPrimaryRecord = BuildWireShapeInfo(ovOtherWire, sNewWirePairId, false);
                                mruSecondaryRecord = BuildWireShapeInfo(ovShape, sNewWirePairId, false);
                            }
                            AddWireToDatabase(mruPrimaryRecord, mruSecondaryRecord);
                            switch (sWireRole)
                            {
                                case "P":
                                    {
                                        UpdateWireGridLocation(ovShape, mruPrimaryRecord);
                                        UpdateWireGridLocation(ovOtherWire, mruSecondaryRecord);
                                        break;
                                    }
                                case "S":
                                    {
                                        UpdateWireGridLocation(ovShape, mruSecondaryRecord);
                                        UpdateWireGridLocation(ovOtherWire, mruPrimaryRecord);
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
        }

        internal static void CheckForWireMateOnPageDelete(Visio.Page ovPage, string sPageID)
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
                            string sMateID = "";
                            switch (sWireRole)
                            {
                                case "P":
                                    {
                                        // we need to go find the secondary....
                                        sMateID = WireUtilities.GetColumnInfoInWirePairsTableFromDatabase("SecondaryWireID", sWirePairID);
                                        break;
                                    }
                                case "S":
                                    {
                                        //we need to go find the primary...
                                        sMateID = WireUtilities.GetColumnInfoInWirePairsTableFromDatabase("PrimaryWireID", sWirePairID);
                                        break;
                                    }
                            }

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


        internal static void UpdateWiresInDatabase(Visio.Page ovPage)
        {
            try
            {

                foreach (Visio.Shape ovShape in ovPage.Shapes)
                {
                    if (ovShape.CellExists["User.Class", 0] == -1)
                    {
                        if (ovShape.Cells["User.Class"].get_ResultStr(0) == "SmartWire")
                        {
                            UpdateWireInDatabase(ovShape);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in UpdateWiresInDatabase " + ex.Message, "VisAssist");
            }
        }

        internal static bool CheckForWireMate(string sShapeID)
        {
            //check for the mate in the document and if doesn't exist this is an undo of a cut not a shape dropped
            try
            {
                //check if this exsits in the db
                bool bDoesRecordExist = DatabaseUtilities.DoesRecordExist(DatabaseUtilities.SqlTables.WireShapesTable.sWireShapeTable, sShapeID);
                if (bDoesRecordExist)
                {
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in CheckForWireMate " + ex.Message, "VisAssist");
            }
            return false;

        }

        internal static void JumpToMate(Shape ovShape)
        {
            //we are given a visio wire shape and we need to determine what/where is the mate wire and then navigate to that shape...
            string sShapeID = ovShape.Cells["User.ShapeID"].get_ResultStr(0);
            string sWirePairID = ovShape.Cells["User.WirePairID"].get_ResultStr(0);
            string sWireRole = ovShape.Cells["User.WireRole"].get_ResultStr(0);
            string sMateID = "";
            switch (sWireRole)
            {
                case "P":
                    {
                        //the wire the use clicked on is the primary
                        sMateID = GetColumnInfoInWirePairsTableFromDatabase("SecondaryWireID", sWirePairID);
                        break;
                    }
                case "S":
                    {
                        //the wire the user clicked on is the secondary
                        sMateID = GetColumnInfoInWirePairsTableFromDatabase("PrimaryWireID", sWirePairID);
                        break;
                    }
            }


            //ok now we have the mates ID lets get the page id
            string sMatePageID = GetColumnInfoInWireShapesTableFromDatabase("PageID", sMateID);
            //use the index to get the page in the document and then double check the page id...
            //get the page index
            string sIndex = PageUtilities.GetColumnInfoInPagesTableFromDatabase("PageIndex", sMatePageID);
            int iIndex = Convert.ToInt32(sIndex);
            //get the visio page from the index instead of looping thorugh the pages
            Visio.Page ovPage = ovShape.Document.Pages[iIndex];
            //foreach (Visio.Page ovPage in ovShape.Document.Pages)
            // {
            // if (ovPage.PageSheet.CellExists["User.PageID", 0] == -1)
            // {
            // if (ovPage.PageSheet.Cells["User.PageID"].get_ResultStr(0) == sMatePageID)
            {
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
                // }
                // }
                
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
                        sWireRole = ovShape.Cells["User.WireRole"].get_ResultStr(0);
                        string sMateID = "";
                        switch (sWireRole)
                        {
                            case "P":
                                {
                                    //ovShape is the primary
                                    sMateID = GetColumnInfoInWirePairsTableFromDatabase("SecondaryWireID", sWirePairID);

                                    break;
                                }
                            case "S":
                                {
                                    //ovShape is the secondary
                                    sMateID = GetColumnInfoInWirePairsTableFromDatabase("PrimaryWireID", sWirePairID);
                                    break;
                                }
                        }

                        //now we have the mates id check each shape in the selection for this id
                        foreach (Visio.Shape ovShapeToCheck in ovSelection)
                        {
                            if (ovShapeToCheck.CellExists["User.ShapeID", 0] == -1)
                            {
                                if (ovShapeToCheck.Cells["User.ShapeID"].get_ResultStr(0) == sMateID)
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
                return sKey;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in IsMateInSelection " + ex.Message, "VisAssist");
            }
            return sKey;
        }
    }
}
