using Microsoft.Office.Core;
using Microsoft.Office.Interop.Visio;
using Microsoft.VisualStudio.Tools.Applications.Runtime;
using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Data.SQLite;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Visio = Microsoft.Office.Interop.Visio;

namespace VisAssistDatabaseBackEnd.DataUtilities
{
    internal class ShapesUtilities
    {

        //ADDING

        //Wire
        internal static void AddWireToDatabase(Visio.Shape ovShape)
        {
            //drop another wire
            Visio.Shape ovSecondaryWire = AddSecondaryWire(ovShape);

            //this builds the information and then runs the sql to add the wire shape to the wire_shapes_table
            
            MultipleRecordUpdates oPrimaryWireRecord = AddWireShapeInfo(ovShape, ovSecondaryWire);
            MultipleRecordUpdates oSecondaryWireRecord = BuildWireShapeInfo(ovSecondaryWire, oPrimaryWireRecord.ruRecords[0].odictColumnValues["WirePairID"]);
            //check if the record already exists (i think this event is firing twice possibly)
            bool bDoesRecordExist = DatabaseUtilities.DoesRecordExist(DatabaseUtilities.SqlTables.WireShapesTable.sWireShapeTable, oPrimaryWireRecord.ruRecords[0].sId);

            if (!bDoesRecordExist)
            {

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
        }
        private static Visio.Shape AddSecondaryWire(Visio.Shape ovPrimaryWire)
        {
            try
            {
                //drop a new wire shape that will be the secondary wire...
                //get the TestStencil.vssx ...
                Visio.Application ovApp = ovPrimaryWire.Application;
                string sProjectID = ovPrimaryWire.Document.DocumentSheet.Cells["User.ProjectID"].get_ResultStr(0);
                string sFileID = ovPrimaryWire.Document.DocumentSheet.Cells["User.FileID"].get_ResultStr(0);
                string sPageID = ovPrimaryWire.Cells["User.PageID"].get_ResultStr(0);


                Visio.Document ovStencilDoc = null;

                foreach (Visio.Document ovDoc in ovApp.Documents)
                {
                    if (ovDoc.Type == Visio.VisDocumentTypes.visTypeStencil && ovDoc.Name.Equals("TestStencil.vssx", StringComparison.OrdinalIgnoreCase))
                    {
                        ovStencilDoc = ovDoc;
                        break;
                    }
                }

                Visio.Master ovWireMaster = ovStencilDoc.Masters["SmartWire"];

                //turn off events before dropping it
                ovPrimaryWire.Document.Application.EventsEnabled = 0;
                Visio.Shape ovSecondaryWire = ovPrimaryWire.ContainingPage.Drop(ovWireMaster, 5, 5);
                //now we have dropped the shape
                //change the role to S
                ovSecondaryWire.Cells["User.WireRole"].Formula = VisioUtilities.Application.FormatStringForVisio("S");
                string sShapeID = GenerateShapeID(sProjectID, sFileID, sPageID, ovSecondaryWire.Name, DateTime.Now);
                ovSecondaryWire.Cells["User.ShapeID"].Formula = VisioUtilities.Application.FormatStringForVisio(sShapeID);

                ovSecondaryWire.Document.Application.EventsEnabled = -1;

                return ovSecondaryWire;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in BuildWirePairInfo " + ex.Message, "VisAssist");
            }
            finally
            {
                ovPrimaryWire.Document.Application.EventsEnabled = -1;
            }
            return null;
        }
        private static MultipleRecordUpdates AddWireShapeInfo(Visio.Shape ovMainWire, Visio.Shape ovSecondaryWire)
        {
            //this is when we are adding a wire for the first time
            try
            {


                Visio.Document ovDoc = ovMainWire.ContainingPage.Document;
                string sProjectID = ovDoc.DocumentSheet.Cells["User.ProjectID"].get_ResultStr(0);
                string sFileID = ovDoc.DocumentSheet.Cells["User.FileID"].get_ResultStr(0);

                //this si the first time we are addding this shape to the db..
                string sPageID = ovMainWire.ContainingPage.PageSheet.Cells["User.PageID"].get_ResultStr(0);
                //turn off events before adding the sPageID to the shape..
                ovMainWire.Application.EventsEnabled = 0;
                ovMainWire.Cells["User.PageID"].Formula = VisioUtilities.Application.FormatStringForVisio(sPageID);
                ovMainWire.Application.EventsEnabled = -1;


                string sWireRole = ovMainWire.Cells["User.WireRole"].get_ResultStr(0);

                string sVersion = ovMainWire.Cells["User.Version"].get_ResultStr(0);
                string sClass = ovMainWire.Cells["User.Class"].get_ResultStr(0);
                string sColor = ovMainWire.Cells["User.WireColor"].get_ResultStr(0);

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

                string sID = "";
                string sWirePairID = "";
                if (ovMainWire.CellExists["User.ShapeID", 0] == -1)
                {

                    sID = GenerateShapeID(sProjectID, sFileID, sPageID, ovMainWire.Name, DateTime.Now);
                    //turn off events firs
                    ovMainWire.Application.EventsEnabled = 0;
                    ovMainWire.Cells["User.ShapeID"].Formula = "\"" + sID + "\"";
                    ovMainWire.Application.EventsEnabled = -1;

                    //we need to also generate the WirePairID

                    sWirePairID = GenerateWirePairID(sProjectID, sFileID, sPageID, ovMainWire.Name, ovSecondaryWire.Name, DateTime.Now);

                }



                Dictionary<string, string> oDictFileValues = new Dictionary<string, string>();
                //oDictFileValues.Add("ProjectID", sProjectID);
                //oDictFileValues.Add("FileID", sFileID);
                oDictFileValues.Add("PageID", sPageID);

                //add the WirePairID

                oDictFileValues.Add("WirePairID", sWirePairID);
                //add the ConnectionID
                oDictFileValues.Add("ConnectionID", "");

                oDictFileValues.Add("WireRole", sWireRole);


                oDictFileValues.Add("Version", sVersion);
                oDictFileValues.Add("Class", sClass);

                //add wire lable
                oDictFileValues.Add("WireLabel", "");


                oDictFileValues.Add("Color", sColor);

                //get the x and y location...
                double dPinX = ovMainWire.Cells["PinX"].ResultIU;
                double dPinY = ovMainWire.Cells["PinY"].ResultIU;
                double dPageX;
                double dPageY;
                ovMainWire.XYToPage(dPinX, dPinY, out dPageX, out dPageY);

                int iPageX = (int)dPageX;
                int iPageY = (int)dPageY;

                //add the x location 
                oDictFileValues.Add("XLocation", "");
                //add the y location 
                oDictFileValues.Add("YLocation", "");
                //add the autolabelling
                oDictFileValues.Add("AutoLabeling", "0"); //integer...
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

                //add the show shield
                oDictFileValues.Add("Shield", "");

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

        //Terminal Block
        internal static void AddTerminalBlockToDatabase(Visio.Shape ovShape)
        {
            MultipleRecordUpdates oTerminalRecord = BuildTerminalBlockInfo(ovShape);
            bool bDoesRecordExist = DatabaseUtilities.DoesRecordExist(DatabaseUtilities.SqlTables.TerminalBlocksTable.sTerminalBlockTable, oTerminalRecord.ruRecords[0].sId);

            if (!bDoesRecordExist)
            {
                DatabaseUtilities.BuildInsertSqlForMultipleRecords(DatabaseUtilities.SqlTables.TerminalBlocksTable.sTerminalBlockTable, oTerminalRecord);
            }

        }

        //End Device
        internal static void AddWiringEndDeviceToDatabase(Visio.Shape ovShape)
        {
            MultipleRecordUpdates oWiringEndDeviceRecord = BuildWiringEndDeviceInfo(ovShape);
            bool bDoesRecordExist = DatabaseUtilities.DoesRecordExist(DatabaseUtilities.SqlTables.WiringEndDevice.sWiringEndDeviceTable, oWiringEndDeviceRecord.ruRecords[0].sId);

            if (!bDoesRecordExist)
            {
                DatabaseUtilities.BuildInsertSqlForMultipleRecords(DatabaseUtilities.SqlTables.WiringEndDevice.sWiringEndDeviceTable, oWiringEndDeviceRecord);

            }


        }


        //BUIlDING INFORMATION

        //Wire
        private static MultipleRecordUpdates BuildWireShapeInfo(Visio.Shape ovMainWire, string sWirePairID)
        {
           //this builds the wire info based on the values in the shape itself
           //this is more of an update or gather not an add...
            try
            {


                Visio.Document ovDoc = ovMainWire.ContainingPage.Document;
                string sProjectID = ovDoc.DocumentSheet.Cells["User.ProjectID"].get_ResultStr(0);
                string sFileID = ovDoc.DocumentSheet.Cells["User.FileID"].get_ResultStr(0);
                string sPageID = ovMainWire.Cells["User.PageID"].get_ResultStr(0);
               

                string sWireRole = ovMainWire.Cells["User.WireRole"].get_ResultStr(0);

                string sVersion = ovMainWire.Cells["User.Version"].get_ResultStr(0);
                string sClass = ovMainWire.Cells["User.Class"].get_ResultStr(0);
                string sColor = ovMainWire.Cells["User.WireColor"].get_ResultStr(0);

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

                string sID = "";
                if (ovMainWire.CellExists["User.ShapeID", 0] == -1)
                {
                    sID = ovMainWire.Cells["User.ShapeID"].get_ResultStr(0);
                   
                }
                
                if(sWirePairID == "")
                {
                    //we didn't pass in the sWirePairID which means this exists in the db and this build is about updating something or deleting soemthing
                    //get the WirePairID from the db for this sID
                    sWirePairID = GetColumnInfoInWireShapesTableFromDatabase(DatabaseUtilities.SqlTables.WireShapesTable.sWireShapeTable, sID);


                }
                


                Dictionary<string, string> oDictFileValues = new Dictionary<string, string>();
                //oDictFileValues.Add("ProjectID", sProjectID);
                //oDictFileValues.Add("FileID", sFileID);
                oDictFileValues.Add("PageID", sPageID);

                //add the WirePairID

                oDictFileValues.Add("WirePairID", sWirePairID);
                //add the ConnectionID
                oDictFileValues.Add("ConnectionID", "");

                oDictFileValues.Add("WireRole", sWireRole);


                oDictFileValues.Add("Version", sVersion);
                oDictFileValues.Add("Class", sClass);

                //add wire lable
                oDictFileValues.Add("WireLabel", "");


                oDictFileValues.Add("Color", sColor);

                //get the x and y location...
                double dPinX = ovMainWire.Cells["PinX"].ResultIU;
                double dPinY = ovMainWire.Cells["PinY"].ResultIU;
                double dPageX;
                double dPageY;
                ovMainWire.XYToPage(dPinX, dPinY, out dPageX, out dPageY);

                int iPageX = (int)dPageX;
                int iPageY = (int)dPageY;

                //add the x location 
                oDictFileValues.Add("XLocation", "");
                //add the y location 
                oDictFileValues.Add("YLocation", "");
                //add the autolabelling
                oDictFileValues.Add("AutoLabeling", "0"); //integer...
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

                //add the show shield
                oDictFileValues.Add("Shield", "");




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

        private static MultipleRecordUpdates BuildWirePairInfo(MultipleRecordUpdates oPrimaryWireRecord, MultipleRecordUpdates oSecondaryWireRecord)
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

        //Terminal Block

        private static MultipleRecordUpdates BuildTerminalBlockInfo(Visio.Shape ovShape)
        {

            Visio.Document ovDoc = ovShape.ContainingPage.Document;
            try
            {
                string sProjectID = ovDoc.DocumentSheet.Cells["User.ProjectID"].get_ResultStr(0);
                string sFileID = ovDoc.DocumentSheet.Cells["User.FileID"].get_ResultStr(0);
                string sPageID = ovShape.Cells["User.PageID"].get_ResultStr(0);
                if (sPageID == "")
                {
                    //we are adding the shape for the first time so let's apply the page id from the current page 
                    sPageID = ovShape.ContainingPage.PageSheet.Cells["User.PageID"].get_ResultStr(0);
                    //turn off events before adding the pageid to the shape..
                    ovDoc.Application.EventsEnabled = 0;
                    ovShape.Cells["User.PageID"].Formula = "\"" + sPageID + "\"";
                    ovDoc.Application.EventsEnabled = -1;
                }

                string sColor = ovShape.Cells["Prop.Color"].get_ResultStr(0);
                string sShapeText = ovShape.Text;

                //get the x and y location...
                double dPinX = ovShape.Cells["PinX"].ResultIU;
                double dPinY = ovShape.Cells["PinY"].ResultIU;
                double dPageX;
                double dPageY;
                ovShape.XYToPage(dPinX, dPinY, out dPageX, out dPageY);

                int iPageX = (int)dPageX;
                int iPageY = (int)dPageY;

                Dictionary<string, string> oDictFileValues = new Dictionary<string, string>();

                oDictFileValues.Add("PageID", sPageID);
                oDictFileValues.Add("Color", sColor);
                oDictFileValues.Add("ShapeText", sShapeText);
                oDictFileValues.Add("XLocation", iPageX.ToString());
                oDictFileValues.Add("YLocation", iPageY.ToString());

                string sID = "";
                if (ovShape.CellExists["User.ShapeID", 0] == -1)
                {
                    sID = ovShape.Cells["User.ShapeID"].get_ResultStr(0);
                    if (sID == "")
                    {
                        sID = GenerateShapeID(sProjectID, sFileID, sPageID, ovShape.Name, DateTime.Now);
                        ovShape.Cells["User.ShapeID"].Formula = "\"" + sID + "\"";
                    }
                }


                //hardcode the vert and horizontal markers...for now but i need this information from the pages...
                //assuming instead of grabbing it this way i'll want to grab the page information from the database, 
                //which part of the location should be in the DB, X and Y? the actual (1,1F)? what should we really be keeping in db

                int iPageIndex = ovShape.ContainingPage.Index;
                string sHorizontalMarkers = "8;7;6;5;4;3;2;1"; //will need to get this from the page
                string sVertMarkers = "A;B;C;D;E;F;G;H";//will need to get this from the page based on vertical/horizontal/pagescale...
                double dPageWidth = ovShape.ContainingPage.PageSheet.Cells["PageWidth"].ResultIU;
                double dPageHeight = ovShape.ContainingPage.PageSheet.Cells["PageHeight"].ResultIU;
                double dLeft = ovShape.CellsU["PinX"].ResultIU - (ovShape.CellsU["Width"].ResultIU / 2);
                double dTop = ovShape.CellsU["PinY"].ResultIU - (ovShape.CellsU["Height"].ResultIU / 2);

                double dWidth = ovShape.CellsU["Width"].ResultIU;
                double dHeight = ovShape.CellsU["Height"].ResultIU;

                
                string sGridLocation = GetShapeGridLocation(dLeft, dTop, dWidth, dHeight, dPageWidth, dPageHeight, sVertMarkers, sHorizontalMarkers, iPageIndex);



                //add the user.gridlocation temporarily
                if (ovShape.CellExists["User.GridLocation", 0] == -1)
                {
                    //turn off events before adding the gridlocation to the shape..
                    ovDoc.Application.EventsEnabled = 0;
                    ovShape.Cells["User.GridLocation"].Formula = $"\"{sGridLocation}\"";
                    ovDoc.Application.EventsEnabled = -1;

                }
                else
                {
                    //turn off events before adding the gridlocation to the shape..
                    ovDoc.Application.EventsEnabled = 0;
                    ovShape.AddNamedRow((short)Visio.VisSectionIndices.visSectionUser, "GridLocation", 0);
                    ovShape.Cells["User.GridLocation"].Formula = $"\"{sGridLocation}\"";
                    ovDoc.Application.EventsEnabled = -1;
                }


                RecordUpdate ruFileRecord = new RecordUpdate();
                ruFileRecord.sPrimaryKeyColumn = DatabaseUtilities.SqlTables.TerminalBlocksTable.sTerminalBlockTablePK;
                ruFileRecord.sId = sID;
                ruFileRecord.odictColumnValues = oDictFileValues;

                return new MultipleRecordUpdates(new List<RecordUpdate> { ruFileRecord });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in BuildTerminalBlockInfo " + ex.Message, "VisAssist");
            }
            finally
            {
                ovDoc.Application.EventsEnabled = -1;
            }
            return new MultipleRecordUpdates();


        }

        //End Device
        private static MultipleRecordUpdates BuildWiringEndDeviceInfo(Visio.Shape ovShape)
        {

            Visio.Document ovDoc = ovShape.ContainingPage.Document;
            string sProjectID = ovDoc.DocumentSheet.Cells["User.ProjectID"].get_ResultStr(0);
            string sFileID = ovDoc.DocumentSheet.Cells["User.FileID"].get_ResultStr(0);
            string sPageID = ovShape.ContainingPage.PageSheet.Cells["User.PageID"].get_ResultStr(0);

            int iTermCount = (int)ovShape.Cells["Prop.TermCount"].ResultIU;
            string sTag = ovShape.Cells["Prop.Tag"].get_ResultStr(0);


            Dictionary<string, string> oDictFileValues = new Dictionary<string, string>();
            oDictFileValues.Add("ProjectID", sProjectID);
            oDictFileValues.Add("FileID", sFileID);
            oDictFileValues.Add("PageID", sPageID);
            oDictFileValues.Add("TermCount", iTermCount.ToString());
            oDictFileValues.Add("Tag", sTag);

            string sID = "";
            if (ovShape.CellExists["User.ID", 0] == -1)
            {
                sID = ovShape.Cells["User.ID"].get_ResultStr(0);
            }
            else
            {
                ovShape.AddNamedRow((short)Visio.VisSectionIndices.visSectionUser, "ID", 0);
                sID = GenerateShapeID(sProjectID, sFileID, sPageID, ovShape.Name, DateTime.Now);
                ovShape.Cells["User.ID"].Formula = "\"" + sID + "\"";
            }


            RecordUpdate ruFileRecord = new RecordUpdate();
            ruFileRecord.sPrimaryKeyColumn = DatabaseUtilities.SqlTables.WiringEndDevice.sWiringEndDeviceTablePK;
            ruFileRecord.sId = sID;
            ruFileRecord.odictColumnValues = oDictFileValues;

            return new MultipleRecordUpdates(new List<RecordUpdate> { ruFileRecord });
        }



        //DELETING
        internal static void DeleteWireFromDatabase(Visio.Shape ovShape)
        {
            MultipleRecordUpdates oWireRecord = BuildWireShapeInfo(ovShape, "");

            //before deleting it in the DB we should also delete the secondary wire off of the page (or the primary-whatever is the opposite..)
            DatabaseUtilities.BuildDeleteSqlForMultipleRecords(DatabaseUtilities.SqlTables.WireShapesTable.sWireShapeTable, oWireRecord);
        }

        internal static void DeleteTerminalBlockFromDatabase(Visio.Shape ovShape)
        {
            MultipleRecordUpdates oTerminalBlockRecord = BuildTerminalBlockInfo(ovShape);
            DatabaseUtilities.BuildDeleteSqlForMultipleRecords(DatabaseUtilities.SqlTables.TerminalBlocksTable.sTerminalBlockTable, oTerminalBlockRecord);
        }

        internal static void DeleteEndDeviceFromDatabase(Visio.Shape ovShape)
        {
            MultipleRecordUpdates oEndDeviceRecord = BuildWiringEndDeviceInfo(ovShape);
            DatabaseUtilities.BuildDeleteSqlForMultipleRecords(DatabaseUtilities.SqlTables.WiringEndDevice.sWiringEndDeviceTable, oEndDeviceRecord);
        }


        //UPDATING
        internal static void UpdateTerminalBlockInDatabase(Visio.Shape ovShape)
        {
            //this gets' called when the user moves a shape...
            MultipleRecordUpdates oTerminalBlockRecord = BuildTerminalBlockInfo(ovShape);
            DatabaseUtilities.BuildUpdateSqlForMultipleRecords(DatabaseUtilities.SqlTables.TerminalBlocksTable.sTerminalBlockTable, oTerminalBlockRecord);

        }






        //UTILITIES
        private static string GenerateShapeID(string sProjectID, string sFileID, string sPageID, string sShapeName, DateTime now)
        {
            string input = sProjectID + sFileID + sPageID + sShapeName + now.ToString("yyyy-MM-dd HH:mm:ss"); // formatted
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

        private static string GenerateWirePairID(string sProjectID, string sFileID, string sPageID, string sPrimaryWire, string sSecondaryWire, DateTime now)
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


        public static string GetShapeGridLocation(
    double dLeft,
    double dTop,
    double dWidth,
    double dHeight,
    double dPageWidth,
    double dPageHeight,
    string sVertMarkersPrompt,
    string sHorzMarkersPrompt,
    int iPageIndex)
        {

            //THIS IS SUBJECT TO CHANGE, BUT I BELEIVE THIS MAY BE SOME OF OUR FOUNDATION USING THE VERT/HORZ MARKERS BASED ON THE PAGE SCALE...IN ORDER TO GET CORRECT GRID LOCATION
            string[] saVertMarkers = sVertMarkersPrompt.Split(';');
            string[] saHorzMarkers = sHorzMarkersPrompt.Split(';');

            if (saVertMarkers.Length == 0 || saHorzMarkers.Length == 0)
                throw new ArgumentException("Invalid grid markers");

            // Shape center
            double dCenterX = dLeft + (dWidth / 2.0);
            double dCenterY = dTop + (dHeight / 2.0);

            // ---- X GRID (horizontal markers) ----
            double dXRatio = dCenterX / dPageWidth;
            int iXIndex = (int)Math.Floor(dXRatio * saHorzMarkers.Length);
            iXIndex = Math.Max(0, Math.Min(saHorzMarkers.Length - 1, iXIndex));

            string sXMarker = saHorzMarkers[iXIndex];

            // ---- Y GRID (vertical markers) ----
            double dYRatio = dCenterY / dPageHeight;
            int iYIndex = (int)Math.Floor(dYRatio * saVertMarkers.Length);
            iYIndex = Math.Max(0, Math.Min(saVertMarkers.Length - 1, iYIndex));

            string sYMarker = saVertMarkers[iYIndex];

            // Final format: (PageIndex, X Y)
            return $"({iPageIndex}, {sXMarker}{sYMarker})";
        }

        internal static string GetColumnInfoInWireShapesTableFromDatabase(string sColumnName, string sID)
        {
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


    }
}
