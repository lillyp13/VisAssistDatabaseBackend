using Microsoft.Office.Interop.Visio;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using VisAssistDatabaseBackEnd.DataUtilities;
using VisAssistDatabaseBackEnd.VisioUtilities;
using Visio = Microsoft.Office.Interop.Visio;

namespace VisAssistDatabaseBackEnd.ShapeUtilities.Wire
{
    public class WireUtilities
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
            finally
            {
                //make sure events are on
                Globals.ThisAddIn.Application.EventsEnabled = -1;
            }


        }
        internal static void DeleteWireFromDatabase(string sShapeID)
        {
            try
            {
                MultipleRecordUpdates oWireRecord = new MultipleRecordUpdates();
                oWireRecord.ruRecords = new List<RecordUpdate>();
                RecordUpdate ru = new RecordUpdate();
                ru.sId = sShapeID;
                ru.sPrimaryKeyColumn = DatabaseUtilities.SqlTables.WireShapesTable.sWireShapeTablePK;
                oWireRecord.ruRecords.Add(ru);

                //before deleting it in the DB we should also delete the secondary wire off of the page (or the primary-whatever is the opposite..)
                DatabaseUtilities.BuildDeleteSqlForMultipleRecords(DatabaseUtilities.SqlTables.WireShapesTable.sWireShapeTable, oWireRecord);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in DeleteWireFromDatabase " + ex.Message, "VisAssist");
            }
            finally
            {
                //make sure events are on
                Globals.ThisAddIn.Application.EventsEnabled = -1;
            }
        }

        internal static void UpdateWireInDatabase(Visio.Shape ovShape, bool bMateInSelection)
        {
            try
            {
                if (ovShape.CellExists["User.WirePairID", 0] == -1)
                {
                    string sShapeID = ovShape.Cells["User.ShapeID"].get_ResultStr(0);
                    string sWirePairID = ovShape.Cells["User.WirePairID"].get_ResultStr(0);
                  
                    //this gets' called when the user moves a shape...
                    MultipleRecordUpdates oWireInfo = BuildWireShapeInfo(ovShape, sWirePairID, false);
                    DatabaseUtilities.BuildUpdateSqlForMultipleRecords(DatabaseUtilities.SqlTables.WireShapesTable.sWireShapeTable, oWireInfo);

                    //we also want to update the mates information and the features in visio (if the user changed the priamry wires # of conductors we need to make the same thing happen to its mate
                    if (!Globals.ThisAddIn.Application.IsUndoingOrRedoing)
                    {
                        WireUtilities.MatchWireFeatures(ovShape, sWirePairID);
                        UpdateWireGridLocation(ovShape, oWireInfo, bMateInSelection);
                    }

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in UpdateWireInDatabase " + ex.Message, "VisAssist");
            }
            finally
            {
                //make sure events are on
                Globals.ThisAddIn.Application.EventsEnabled = -1;
            }
        }




        //VISIO ACTIONS

        internal static void AddWire(Visio.Shape ovShape, ref List<string> olstWires, bool bNewWire)
        {
            try
            {
                //this determines what kind of wire shape we need to drop, builds the information and adds to the database...
                string sWireRole = ovShape.Cells["User.WireRole"].get_ResultStr(0);
                Visio.Shape ovPrimaryWire = null;
                Visio.Shape ovSecondaryWire = null;
                MultipleRecordUpdates oPrimaryWireRecord;
                MultipleRecordUpdates oSecondaryWireRecord;
                string sKey = ovShape.ID + "|" + ovShape.ContainingPage.Name;
                if (!Globals.ThisAddIn.m_olstMatesMated.Contains(sKey))
                {

                    if (sWireRole == "P")
                    {
                        //ovShape is a primary wire
                        //BEFORE WE BLINDLY DROP A NEW WIRE FOR THIS SHAPE CHECK IF IT IS IN OUR SELECTION TO BE DROPPED...
                        if (Globals.ThisAddIn.m_odictMatesInSelection.ContainsKey(sKey))
                        {
                            //get the mate shape ..

                            ovPrimaryWire = ovShape;
                            ovSecondaryWire = Globals.ThisAddIn.m_odictMatesInSelection[sKey].ovMateShape;

                            string sMateKey = ovSecondaryWire.ID + "|" + ovSecondaryWire.ContainingPage.Name;
                            Globals.ThisAddIn.m_olstMatesMated.Add(sKey);
                            Globals.ThisAddIn.m_olstMatesMated.Add(sMateKey);
                        }
                        else
                        {
                            //add normally the mate is not in our selection
                            ovPrimaryWire = ovShape;
                            ovSecondaryWire = AddOtherWire(ovShape, "Secondary");
                        }
                        oPrimaryWireRecord = AddWireShapeInfo(ovPrimaryWire, ovSecondaryWire);
                        //need to add the wirepairid to the otherwire...
                        ovShape.Application.EventsEnabled = 0;
                        ovSecondaryWire.Cells["User.WirePairID"].Formula = VisioUtilities.Application.FormatStringForVisio(oPrimaryWireRecord.ruRecords[0].odictColumnValues["WirePairID"]);
                        ovShape.Application.EventsEnabled = -1;
                        oSecondaryWireRecord = BuildWireShapeInfo(ovSecondaryWire, oPrimaryWireRecord.ruRecords[0].odictColumnValues["WirePairID"], true);

                        olstWires.Add(oPrimaryWireRecord.ruRecords[0].sId);
                        olstWires.Add(oSecondaryWireRecord.ruRecords[0].sId);

                    }
                    else
                    {
                        //the ovShape is a secondary wire
                        if (Globals.ThisAddIn.m_odictMatesInSelection.ContainsKey(sKey))
                        {
                            ovSecondaryWire = ovShape;
                            ovPrimaryWire = Globals.ThisAddIn.m_odictMatesInSelection[sKey].ovMateShape;
                            string sMateKey = ovPrimaryWire.ID + "|" + ovPrimaryWire.ContainingPage.Name;
                            Globals.ThisAddIn.m_olstMatesMated.Add(sKey);
                            Globals.ThisAddIn.m_olstMatesMated.Add(sMateKey);
                        }
                        else
                        {
                            //add normally the mate is not in our selection
                            ovSecondaryWire = ovShape;
                            ovPrimaryWire = AddOtherWire(ovSecondaryWire, "Primary");
                        }
                        oSecondaryWireRecord = AddWireShapeInfo(ovSecondaryWire, ovSecondaryWire);
                        //need to add the wirepairid to the otherwire...
                        ovShape.Application.EventsEnabled = 0;
                        ovPrimaryWire.Cells["User.WirePairID"].Formula = VisioUtilities.Application.FormatStringForVisio(oSecondaryWireRecord.ruRecords[0].odictColumnValues["WirePairID"]);
                        ovShape.Application.EventsEnabled = -1;
                        oPrimaryWireRecord = BuildWireShapeInfo(ovPrimaryWire, oSecondaryWireRecord.ruRecords[0].odictColumnValues["WirePairID"], true);

                        olstWires.Add(oPrimaryWireRecord.ruRecords[0].sId);
                        olstWires.Add(oSecondaryWireRecord.ruRecords[0].sId);

                    }

                    bool bDoesRecordExist = DatabaseUtilities.DoesRecordExist(DatabaseUtilities.SqlTables.WireShapesTable.sWireShapeTable, oPrimaryWireRecord.ruRecords[0].sId);

                    if (!bDoesRecordExist)
                    {
                        if (bNewWire)
                        {
                            string sFileID = ovShape.Document.DocumentSheet.Cells["User.FileID"].get_ResultStr(0);
                            string sWirePrefix = "";
                            string sRGBColor = "";

                            //need to add the next.. WirePrefix and the wire color to the db 
                            string sNextWireNumber = FileUtilities.GetColumnInfoInFilesTableFromDatabase("NextWireNumber", sFileID);
                            //update the wire with sNextWireNumber...
                            ovShape.Application.EventsEnabled = 0;
                            sWirePrefix = "W-" + sNextWireNumber;
                            ovPrimaryWire.Cells["Prop.WireLabel"].Formula = VisioUtilities.Application.FormatStringForVisio(sWirePrefix);
                            ovSecondaryWire.Cells["Prop.WireLabel"].Formula = VisioUtilities.Application.FormatStringForVisio(sWirePrefix);
                            ovShape.Application.EventsEnabled = -1;

                            sRGBColor = RecolorWire.GetAndUpdateNextWireColorFromDB(sFileID);
                            //set the color to the wires we dropped...
                            ovShape.Application.EventsEnabled = 0;
                            ovPrimaryWire.Cells["User.WireColor"].Formula = VisioUtilities.Application.FormatStringForVisio(sRGBColor);
                            ovSecondaryWire.Cells["User.WireColor"].Formula = VisioUtilities.Application.FormatStringForVisio(sRGBColor);
                            ovShape.Application.EventsEnabled = -1;

                            //need to update the nextwirenumber and nextwirecolor in the file table for this file...
                            RenumberWire.IncreaseNextWireNumber(sFileID);

                            oPrimaryWireRecord.ruRecords[0].odictColumnValues["WireLabel"] = sWirePrefix;
                            oSecondaryWireRecord.ruRecords[0].odictColumnValues["WireLabel"] = sWirePrefix;
                            oPrimaryWireRecord.ruRecords[0].odictColumnValues["Color"] = sRGBColor;
                            oSecondaryWireRecord.ruRecords[0].odictColumnValues["Color"] = sRGBColor;
                        }
                        else
                        {
                            //we are not increasing the  number of the color but we do need to match the color and number on the mate we created...
                            string sWireNumber = "";
                            string sWireColor = "";
                            if (sWireRole == "P")
                            {
                                //the shape we duplicated was the primary...so we want to use the priamry's color and number...
                                sWireNumber = ovPrimaryWire.Cells["Prop.WireLabel"].get_ResultStr(0);
                                sWireColor = ovPrimaryWire.Cells["User.WireColor"].get_ResultStr(0);
                                ovShape.Application.EventsEnabled = 0;
                                ovSecondaryWire.Cells["Prop.WireLabel"].Formula = VisioUtilities.Application.FormatStringForVisio(sWireNumber);
                                ovSecondaryWire.Cells["User.WireColor"].Formula = VisioUtilities.Application.FormatStringForVisio(sWireColor);
                                ovShape.Application.EventsEnabled = -1;

                            }
                            else
                            {
                                //the shape we duplicated was the secondary...so we want to use the secondarys color and number...
                                sWireNumber = ovSecondaryWire.Cells["Prop.WireLabel"].get_ResultStr(0);
                                sWireColor = ovSecondaryWire.Cells["User.WireColor"].get_ResultStr(0);
                                ovShape.Application.EventsEnabled = 0;
                                ovPrimaryWire.Cells["Prop.WireLabel"].Formula = VisioUtilities.Application.FormatStringForVisio(sWireNumber);
                                ovPrimaryWire.Cells["User.WireColor"].Formula = VisioUtilities.Application.FormatStringForVisio(sWireColor);
                                ovShape.Application.EventsEnabled = -1;

                            }
                            oPrimaryWireRecord.ruRecords[0].odictColumnValues["WireLabel"] = sWireNumber;
                            oSecondaryWireRecord.ruRecords[0].odictColumnValues["WireLabel"] = sWireNumber;
                            oPrimaryWireRecord.ruRecords[0].odictColumnValues["Color"] = sWireColor;
                            oSecondaryWireRecord.ruRecords[0].odictColumnValues["Color"] = sWireColor;
                        }
                        AddWireToDatabase(oPrimaryWireRecord, oSecondaryWireRecord);
                        ////now we want to add the wire pairs grid location to the correct wire (primary should point to secondary, secondary should point to primary..)
                        AddWireGridLocation(oPrimaryWireRecord, oSecondaryWireRecord, ovPrimaryWire, ovSecondaryWire);

                    }
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in AddWire " + ex.Message, "VisAssist");
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
                sMateID = WireMateUtilities.GetMateID(sWirePairID, sWireRole);
                
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
                                           WireMateUtilities.UpdateMatesFeatures(ovShapeToCheck, ovShape, sWirePairID);
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
            finally
            {
                //make sure events are on
                Globals.ThisAddIn.Application.EventsEnabled = -1;
            }
        }

        internal static Visio.Shape AddOtherWire(Visio.Shape ovWire, string sWireRoleToCreate)
        {
            //this actually drops the new wire shape on the page, whether or not it is a secondary or primary...
            try
            {
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
        public static void UpdateWireGridLocation(Visio.Shape ovShape, MultipleRecordUpdates oWireInfo, bool bMateInSelection)
        {
            //this updates the user cell in the shape to show where the mate lives...
            try
            {
                //need to get the mate shape...
                Visio.Document ovDocument = ovShape.Document;
                string sRole = oWireInfo.ruRecords[0].odictColumnValues["WireRole"];
                string sGridLocation = oWireInfo.ruRecords[0].odictColumnValues["GridLocation"];
                string sShapeID;
                string sWirePairID;
                string sMatesID = "";
                string sPageID;
                string sPageIDToCheck;
                string sShapeIDToCheck;

                sShapeID = oWireInfo.ruRecords[0].sId;
                sWirePairID = GetColumnInfoInWireShapesTableFromDatabase("WirePairID", sShapeID);
                sMatesID = WireMateUtilities.GetMateID(sWirePairID, sRole);

                //before getting the pageID if bMateInSelection is true that means we are also moving the mate and the pageID hasn't gotten updated in the db to the new page yet...
                if (bMateInSelection)
                {
                    sPageID = ovShape.ContainingPage.PageSheet.Cells["User.PageID"].get_ResultStr(0);

                }
                else
                {
                    //ok now we have the mate's id get the page it is on in order to find the shape itself in visio..
                    sPageID = GetColumnInfoInWireShapesTableFromDatabase("PageID", sMatesID);
                }


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
            finally
            {
                //make sure events are on
                Globals.ThisAddIn.Application.EventsEnabled = -1;
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

        internal static Dictionary<string, string> GatherWireInformation(Visio.Shape ovMainWire, string sPageID, string sWirePairID)
        {
            Dictionary<string, string> odictFileValues = new Dictionary<string, string>();
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

                odictFileValues.Add("PageID", sPageID);
                odictFileValues.Add("WirePairID", sWirePairID);
                odictFileValues.Add("ConnectionID", "");
                string sWireName = ovMainWire.ID.ToString();
                odictFileValues.Add("WireName", sWireName);
                odictFileValues.Add("WireRole", sWireRole);
                odictFileValues.Add("Version", sVersion);
                odictFileValues.Add("Class", sClass);
                odictFileValues.Add("WireLabel", sWireLabel);
                odictFileValues.Add("Color", sColor);
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

                odictFileValues.Add("XLocation", dPageX.ToString());
                odictFileValues.Add("YLocation", dPageY.ToString());
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
                odictFileValues.Add("AutoLabeling", iAutoLabel.ToString()); //integer...
                odictFileValues.Add("ConductorCount", iNumberOfConductors.ToString());
                odictFileValues.Add("Conductor1Label", sConductor1);
                odictFileValues.Add("Conductor2Label", sConductor2);
                odictFileValues.Add("Conductor3Label", sConductor3);
                odictFileValues.Add("Conductor4Label", sConductor4);
                odictFileValues.Add("Conductor5Label", sConductor5);
                odictFileValues.Add("Conductor6Label", sConductor6);
                odictFileValues.Add("Conductor7Label", sConductor7);
                odictFileValues.Add("Conductor8Label", sConductor8);
                odictFileValues.Add("Conductor9Label", sConductor9);
                odictFileValues.Add("Conductor10Label", sConductor10);
                odictFileValues.Add("GridLocation", sGridLocation);
                odictFileValues.Add("Shield", sShield);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in GatherWireInformation " + ex.Message, "VisAssist");
            }
            finally
            {
                //make sure events are on
                Globals.ThisAddIn.Application.EventsEnabled = -1;
            }
            return odictFileValues;
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
                    string sPageID = ovMainWire.ContainingPage.PageSheet.Cells["User.PageID"].get_ResultStr(0);
                    
                    string sWireRole = ovMainWire.Cells["User.WireRole"].get_ResultStr(0);

                    Dictionary<string, string> odictFileValues = GatherWireInformation(ovMainWire, sPageID, sWirePairID);


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
                   
                    odictFileValues["WirePairID"] = sWirePairID;

                    RecordUpdate ruFileRecord = new RecordUpdate();
                    ruFileRecord.sPrimaryKeyColumn = DatabaseUtilities.SqlTables.WireShapesTable.sWireShapeTablePK;
                    ruFileRecord.sId = sID;
                    ruFileRecord.odictColumnValues = odictFileValues;

                    return new MultipleRecordUpdates(new List<RecordUpdate> { ruFileRecord });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in BuildWireShapeInfo " + ex.Message, "VisAssist");
            }
            finally
            {
                //make sure events are on
                Globals.ThisAddIn.Application.EventsEnabled = -1;
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

                string sPageID = ovMainWire.ContainingPage.PageSheet.Cells["User.PageID"].get_ResultStr(0);

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
                Dictionary<string, string> odictFileValues = GatherWireInformation(ovMainWire, sPageID, sWirePairID);


                RecordUpdate ruFileRecord = new RecordUpdate();
                ruFileRecord.sPrimaryKeyColumn = DatabaseUtilities.SqlTables.WireShapesTable.sWireShapeTablePK;
                ruFileRecord.sId = sID;
                ruFileRecord.odictColumnValues = odictFileValues;

                return new MultipleRecordUpdates(new List<RecordUpdate> { ruFileRecord });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in BuildWireShapeInfo " + ex.Message, "VisAssist");
            }
            finally
            {
                //make sure events are on
                Globals.ThisAddIn.Application.EventsEnabled = -1;
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

                Dictionary<string, string> odictFileValues = new Dictionary<string, string>();
                odictFileValues.Add("PrimaryWireID", sPrimaryWireID);
                odictFileValues.Add("SecondaryWireID", sSecondaryWireID);

                RecordUpdate ruFileRecord = new RecordUpdate();
                ruFileRecord.sPrimaryKeyColumn = DatabaseUtilities.SqlTables.WirePairsTable.sWirePairsTablePK;
                ruFileRecord.sId = sWirePairID;
                ruFileRecord.odictColumnValues = odictFileValues;

                return new MultipleRecordUpdates(new List<RecordUpdate> { ruFileRecord });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in BuildWirePairInfo " + ex.Message, "VisAssist");
            }
            finally
            {
                //make sure events are on
                Globals.ThisAddIn.Application.EventsEnabled = -1;
            }
            return new MultipleRecordUpdates();
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
                            UpdateWireInDatabase(ovShape, false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in UpdateWiresInDatabase " + ex.Message, "VisAssist");
            }
            finally
            {
                //make sure events are on
                Globals.ThisAddIn.Application.EventsEnabled = -1;
            }
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

        internal static string GenerateNewWirePairID(string sProjectID, string sFileID, string sPageID, string sWireID, DateTime now)
        {
            string input = sProjectID + sFileID + sPageID + sWireID + now.ToString("yyyy-MM-dd HH:mm:ss"); // formatted
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
        internal static Dictionary<string, MateSelection> GatherWires(string sRange)
        {
            Dictionary<string, MateSelection> odictWires = new Dictionary<string, MateSelection>();
            try
            {
                Visio.Document ovDocument = Globals.ThisAddIn.Application.ActiveDocument;

                switch (sRange)
                {
                    case "All Pages":
                        {
                            foreach (Visio.Page ovPage in ovDocument.Pages)
                            {
                                if (ovPage.PageSheet.CellExists["User.PageID", 0] == -1)
                                {
                                    foreach (Visio.Shape ovShape in ovPage.Shapes)
                                    {
                                        if (ovShape.CellExists["User.Class", 0] == -1 && ovShape.Cells["User.Class"].get_ResultStr(0) == "SmartWire")
                                        {
                                            string sShapeID = ovShape.Cells["User.ShapeID"].get_ResultStr(0);
                                            string sWireRole = ovShape.Cells["User.WireRole"].get_ResultStr(0);
                                            string sWirePairID = ovShape.Cells["User.WirePairID"].get_ResultStr(0);
                                            MateSelection oMateSelection = new MateSelection();
                                            oMateSelection.sShapeID = sShapeID;
                                            oMateSelection.sWirePairID = sWirePairID;
                                            oMateSelection.sWireRole = sWireRole;
                                            odictWires.Add(sShapeID, oMateSelection);
                                        }
                                    }
                                }
                            }


                            break;
                        }
                    case "Current Selection":
                        {
                            Visio.Selection ovSelection = ovDocument.Application.ActiveWindow.Selection;

                            //gather the wires in the selection
                            foreach (Visio.Shape ovShape in ovSelection)
                            {
                                if (ovShape.CellExists["User.Class", 0] == -1 && ovShape.Cells["User.Class"].get_ResultStr(0) == "SmartWire")
                                {
                                    //we have a wire in our selection add it
                                    string sShapeID = ovShape.Cells["User.ShapeID"].get_ResultStr(0);
                                    string sWireRole = ovShape.Cells["User.WireRole"].get_ResultStr(0);
                                    string sWirePairID = ovShape.Cells["User.WirePairID"].get_ResultStr(0);
                                    MateSelection oMateSelection = new MateSelection();
                                    oMateSelection.sShapeID = sShapeID;
                                    oMateSelection.sWirePairID = sWirePairID;
                                    oMateSelection.sWireRole = sWireRole;
                                    odictWires.Add(sShapeID, oMateSelection);
                                }
                            }

                            //we have a dictionary with all the wires in the current selection but we need to see if we need to add the mates...
                            WireMateUtilities.AddWireMatesToSelection(ref odictWires, ovDocument);
                            break;
                        }
                    case "Current Page":
                        {
                            Visio.Page ovCurrentPage = ovDocument.Application.ActivePage;
                            //gather the wires on the page
                            foreach (Visio.Shape ovShape in ovCurrentPage.Shapes)
                            {
                                if (ovShape.CellExists["User.Class", 0] == -1 && ovShape.Cells["User.Class"].get_ResultStr(0) == "SmartWire")
                                {
                                    string sShapeID = ovShape.Cells["User.ShapeID"].get_ResultStr(0);
                                    string sWireRole = ovShape.Cells["User.WireRole"].get_ResultStr(0);
                                    string sWirePairID = ovShape.Cells["User.WirePairID"].get_ResultStr(0);
                                    MateSelection oMateSelection = new MateSelection();
                                    oMateSelection.sShapeID = sShapeID;
                                    oMateSelection.sWirePairID = sWirePairID;
                                    oMateSelection.sWireRole = sWireRole;
                                    odictWires.Add(sShapeID, oMateSelection);
                                }
                            }

                            //we have a dictionary with all the wires in the current page but we need to see if we need to add the mates...
                            WireMateUtilities.AddWireMatesToSelection(ref odictWires, ovDocument);
                            break;
                        }
                }

                return odictWires;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in GatherRecolorWireInfo " + ex.Message, "VisAssist");
            }
            finally
            {
                //make sure events are on
                Globals.ThisAddIn.Application.EventsEnabled = -1;
            }
            return odictWires;
        }
        //HELPER SQL FUNCTIONS
        internal static string GetColumnInfoInWirePairsTableFromDatabase(string sColumnName, string sWirePairID)
        {
            try
            {
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
            
            try
            {
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

        internal static List<string> GetOrderedShapeIDsByYLocation(Dictionary<string, MateSelection> odictWires, string sFileID, string sLocation, string sDirection)
        {
            List<string> olstOrderedIDs = new List<string>();

            using (SQLiteConnection sqliteconConnection = new SQLiteConnection(DatabaseConfig.ConnectionString))
            {
                sqliteconConnection.Open();

                // Build parameter list dynamically
                List<string> paramNames = new List<string>();
                int i = 0;

                foreach (string shapeID in odictWires.Keys)
                {
                    paramNames.Add($"@id{i}");
                    i++;
                }
                string sInClause = string.Join(",", paramNames);

                string sSql = $@"SELECT ws.ShapeID, p.PageIndex, {sLocation} FROM pages_table p INNER JOIN wire_shapes_table ws ON ws.PageID = p.PageID WHERE ws.ShapeID IN ({sInClause}) AND p.FileID = @FileID ORDER BY p.PageIndex ASC, {sLocation} {sDirection};";


                using (SQLiteCommand cmd = new SQLiteCommand(sSql, sqliteconConnection))
                {
                    i = 0;
                    foreach (string sShapeID in odictWires.Keys)
                    {
                        cmd.Parameters.AddWithValue($"@id{i}", sShapeID);
                        i++;
                    }
                    cmd.Parameters.AddWithValue("@FileID", sFileID);
                    using (SQLiteDataReader sqlliteReader = cmd.ExecuteReader())
                    {
                        while (sqlliteReader.Read())
                        {
                            olstOrderedIDs.Add(sqlliteReader.GetString(0));
                        }
                    }
                }
            }

            return olstOrderedIDs;
        }


       
    }
}
