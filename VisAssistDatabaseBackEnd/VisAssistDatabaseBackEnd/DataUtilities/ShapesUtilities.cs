using Microsoft.Office.Core;
using Microsoft.Office.Interop.Visio;
using Microsoft.VisualStudio.Tools.Applications.Runtime;
using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Data.SQLite;
using System.Drawing;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
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


        public static MultipleRecordUpdates m_mruRecordsBase = new MultipleRecordUpdates();
        public static MultipleRecordUpdates m_mruRecordsToCompare = new MultipleRecordUpdates();
        public static MultipleRecordUpdates m_mruRecordsToUpdate = new MultipleRecordUpdates();

        //ADDING

        //Wire
        internal static void AddWire(Visio.Shape ovShape, ref List<string> lstWires)
        {
            try
            {

                //check to see if the wireshape we are dropping (ovShape) is a primary or secondary wire..
                string sWireRole = ovShape.Cells["User.WireRole"].get_ResultStr(0);
                Visio.Shape ovPrimaryWire;
                Visio.Shape ovSecondaryWire;
                MultipleRecordUpdates oPrimaryWireRecord;
                MultipleRecordUpdates oSecondaryWireRecord;
                if (sWireRole == "P")
                {
                    //ovShape is a primary wire
                    //drop another wire
                    ovPrimaryWire = ovShape;
                    ovSecondaryWire = AddOtherWire(ovShape, "Secondary");
                    oPrimaryWireRecord = AddWireShapeInfo(ovPrimaryWire, ovSecondaryWire);
                    oSecondaryWireRecord = BuildWireShapeInfo(ovSecondaryWire, oPrimaryWireRecord.ruRecords[0].odictColumnValues["WirePairID"]);

                    lstWires.Add(oPrimaryWireRecord.ruRecords[0].sId);
                    lstWires.Add(oSecondaryWireRecord.ruRecords[0].sId);
                }
                else
                {
                    //the user copied a secondary wire we should create a priamry wire...
                    //ovShape is a secondary wire
                    ovSecondaryWire = ovShape;
                    ovPrimaryWire = AddOtherWire(ovSecondaryWire, "Primary");

                    oSecondaryWireRecord = AddWireShapeInfo(ovSecondaryWire, ovSecondaryWire);
                    oPrimaryWireRecord = BuildWireShapeInfo(ovPrimaryWire, oSecondaryWireRecord.ruRecords[0].odictColumnValues["WirePairID"]);

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
                    AddWireToDatabase(oPrimaryWireRecord, oSecondaryWireRecord);
                    ////this will insert the primary wire to the db
                    //DatabaseUtilities.BuildInsertSqlForMultipleRecords(DatabaseUtilities.SqlTables.WireShapesTable.sWireShapeTable, oPrimaryWireRecord);
                    //DatabaseUtilities.BuildInsertSqlForMultipleRecords(DatabaseUtilities.SqlTables.WireShapesTable.sWireShapeTable, oSecondaryWireRecord);
                    ////will also need to add it to the wire_pairs_table....
                    ////and also add the ovSeconaryWire to the db...
                    ////now we need to add the primary wire id and the secondary wire id as well as the wirepairid to the wire_pairs_table
                    //MultipleRecordUpdates oWirePairRecord = BuildWirePairInfo(oPrimaryWireRecord, oSecondaryWireRecord);

                    //if (oWirePairRecord.ruRecords != null)
                    //{
                    //    DatabaseUtilities.BuildInsertSqlForMultipleRecords(DatabaseUtilities.SqlTables.WirePairsTable.sWirePairsTable, oWirePairRecord);
                    //}

                    ////now we want to add the wire pairs grid location to the correct wire (primary should point to secondary, secondary should point to primary..)
                    AddWireGridLocation(oPrimaryWireRecord, oSecondaryWireRecord, ovPrimaryWire, ovSecondaryWire);

                }

                
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in AddWireToDatabase " + ex.Message, "VisAssist");
            }
        }

        internal static void AddWireToDatabase(MultipleRecordUpdates oPrimaryWireRecord, MultipleRecordUpdates oSecondaryWireRecord)
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

        public static void AddWireGridLocation(MultipleRecordUpdates oPrimaryWireRecord, MultipleRecordUpdates oSecondaryWireRecord, Visio.Shape ovShape, Visio.Shape ovSecondaryWire)
        {
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

        public static void UpdateWireGridLocation(Visio.Shape ovShape, MultipleRecordUpdates oWireInfo)
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

        internal static string GetColumnInfoInWirePairsTableFromDatabase(string sColumnName, string sWirePairID)
        {
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

        internal static Visio.Shape AddOtherWire(Visio.Shape ovWire, string sWireRoleToCreate)
        {
            try
            {
                //drop a new wire shape that will be the secondary wire...
                //get the TestStencil.vssx ...
                Visio.Application ovApp = ovWire.Application;
                string sProjectID = ovWire.Document.DocumentSheet.Cells["User.ProjectID"].get_ResultStr(0);
                string sFileID = ovWire.Document.DocumentSheet.Cells["User.FileID"].get_ResultStr(0);
                string sPageID = ovWire.Cells["User.PageID"].get_ResultStr(0);


                Visio.Document ovStencilDoc = null;

                foreach (Visio.Document ovDoc in ovApp.Documents)
                {
                    if (ovDoc.Type == Visio.VisDocumentTypes.visTypeStencil && ovDoc.Name.Equals("TestStencil.vssx", StringComparison.OrdinalIgnoreCase))
                    {
                        ovStencilDoc = ovDoc;
                        break;
                    }
                }

                if(ovStencilDoc == null)
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
                if(sWireRoleToCreate == "Secondary")
                {
                    ovOtherWire.Cells["User.WireRole"].Formula = VisioUtilities.Application.FormatStringForVisio("S");
                }
                else
                {
                    ovOtherWire.Cells["User.WireRole"].Formula = VisioUtilities.Application.FormatStringForVisio("P");
                }
                
                string sShapeID = GenerateShapeID(sProjectID, sFileID, sPageID, ovOtherWire.Name, DateTime.Now);
                ovOtherWire.Cells["User.ShapeID"].Formula = VisioUtilities.Application.FormatStringForVisio(sShapeID);
                ovOtherWire.Cells["User.PageID"].Formula = VisioUtilities.Application.FormatStringForVisio(sPageID);

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
        internal static MultipleRecordUpdates AddWireShapeInfo(Visio.Shape ovMainWire, Visio.Shape ovSecondaryWire)
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



                //string sWireRole = ovMainWire.Cells["User.WireRole"].get_ResultStr(0);

                //string sVersion = ovMainWire.Cells["User.Version"].get_ResultStr(0);
                //string sClass = ovMainWire.Cells["User.Class"].get_ResultStr(0);
                //string sColor = ovMainWire.Cells["User.WireColor"].get_ResultStr(0);

                //int iNumberOfConductors = (int)ovMainWire.Cells["Prop.NumberOfConductors"].ResultIU;
                //string sConductor1 = ovMainWire.Cells["User.Conductor1AutoLabel"].get_ResultStr(0);
                //string sConductor2 = ovMainWire.Cells["User.Conductor2AutoLabel"].get_ResultStr(0);
                //string sConductor3 = ovMainWire.Cells["User.Conductor3AutoLabel"].get_ResultStr(0);
                //string sConductor4 = ovMainWire.Cells["User.Conductor4AutoLabel"].get_ResultStr(0);
                //string sConductor5 = ovMainWire.Cells["User.Conductor5AutoLabel"].get_ResultStr(0);
                //string sConductor6 = ovMainWire.Cells["User.Conductor6AutoLabel"].get_ResultStr(0);
                //string sConductor7 = ovMainWire.Cells["User.Conductor7AutoLabel"].get_ResultStr(0);
                //string sConductor8 = ovMainWire.Cells["User.Conductor8AutoLabel"].get_ResultStr(0);
                //string sConductor9 = ovMainWire.Cells["User.Conductor9AutoLabel"].get_ResultStr(0);
                //string sConductor10 = ovMainWire.Cells["User.Conductor10AutoLabel"].get_ResultStr(0);

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
                Dictionary<string, string> oDictFileValues = GatherWireInformation(ovMainWire, sPageID, sWirePairID);


                //Dictionary<string, string> oDictFileValues = new Dictionary<string, string>();
                ////oDictFileValues.Add("ProjectID", sProjectID);
                ////oDictFileValues.Add("FileID", sFileID);
                //oDictFileValues.Add("PageID", sPageID);

                ////add the WirePairID

                //oDictFileValues.Add("WirePairID", sWirePairID);
                ////add the ConnectionID
                //oDictFileValues.Add("ConnectionID", "");

                //oDictFileValues.Add("WireRole", sWireRole);


                //oDictFileValues.Add("Version", sVersion);
                //oDictFileValues.Add("Class", sClass);

                ////add wire lable
                //oDictFileValues.Add("WireLabel", "");


                //oDictFileValues.Add("Color", sColor);

                ////get the x and y location...
                //double dPinX = ovMainWire.Cells["PinX"].ResultIU;
                //double dPinY = ovMainWire.Cells["PinY"].ResultIU;
                //double dPageX;
                //double dPageY;
                //ovMainWire.XYToPage(dPinX, dPinY, out dPageX, out dPageY);

                //int iPageX = (int)dPageX;
                //int iPageY = (int)dPageY;

                ////add the x location 
                //oDictFileValues.Add("XLocation", "");
                ////add the y location 
                //oDictFileValues.Add("YLocation", "");
                ////add the autolabelling
                //oDictFileValues.Add("AutoLabeling", "0"); //integer...
                //oDictFileValues.Add("ConductorCount", iNumberOfConductors.ToString());



                //oDictFileValues.Add("Conductor1Label", sConductor1);
                //oDictFileValues.Add("Conductor2Label", sConductor2);
                //oDictFileValues.Add("Conductor3Label", sConductor3);
                //oDictFileValues.Add("Conductor4Label", sConductor4);
                //oDictFileValues.Add("Conductor5Label", sConductor5);
                //oDictFileValues.Add("Conductor6Label", sConductor6);
                //oDictFileValues.Add("Conductor7Label", sConductor7);
                //oDictFileValues.Add("Conductor8Label", sConductor8);
                //oDictFileValues.Add("Conductor9Label", sConductor9);
                //oDictFileValues.Add("Conductor10Label", sConductor10);

                ////add the show shield
                //oDictFileValues.Add("Shield", "");

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

                string sGridLocation = GetWireGridLocation(dPageX, dPageY, dPageWidth, dPageHeight, sVertMarkers, sHorizontalMarkers, iPageIndex);




                //add the x location 
                oDictFileValues.Add("XLocation", dPageX.ToString());
                //add the y location 
                oDictFileValues.Add("YLocation", dPageY.ToString());
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





        //Terminal Block
        internal static void AddTerminalBlockToDatabase(Visio.Shape ovShape)
        {
            try
            {
                MultipleRecordUpdates oTerminalRecord = BuildTerminalBlockInfo(ovShape);
                bool bDoesRecordExist = DatabaseUtilities.DoesRecordExist(DatabaseUtilities.SqlTables.TerminalBlocksTable.sTerminalBlockTable, oTerminalRecord.ruRecords[0].sId);

                if (!bDoesRecordExist)
                {
                    DatabaseUtilities.BuildInsertSqlForMultipleRecords(DatabaseUtilities.SqlTables.TerminalBlocksTable.sTerminalBlockTable, oTerminalRecord);
                }
                else
                {
                    //the record already exists this is a copy of a shape...
                    if (Globals.ThisAddIn.m_delayedEvents.Count > 0)
                    {
                        string sPageID = Globals.ThisAddIn.m_delayedEvents[0].sPageID;

                        oTerminalRecord = AddTerminalBlockInfo(ovShape, sPageID);
                        DatabaseUtilities.BuildInsertSqlForMultipleRecords(DatabaseUtilities.SqlTables.TerminalBlocksTable.sTerminalBlockTable, oTerminalRecord);
                    }
                    else
                    {
                        //the uer pressed redo a second or third time....
                        string sPageID = ovShape.ContainingPage.PageSheet.Cells["User.PageID"].get_ResultStr(0);
                        oTerminalRecord = AddTerminalBlockInfo(ovShape, sPageID);
                        DatabaseUtilities.BuildInsertSqlForMultipleRecords(DatabaseUtilities.SqlTables.TerminalBlocksTable.sTerminalBlockTable, oTerminalRecord);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in AddTerminalBlockToDatabase " + ex.Message, "VisAssist");
            }

        }

        //End Device
        internal static void AddWiringEndDeviceToDatabase(Visio.Shape ovShape)
        {
            try
            {
                MultipleRecordUpdates oWiringEndDeviceRecord = BuildWiringEndDeviceInfo(ovShape);
                bool bDoesRecordExist = DatabaseUtilities.DoesRecordExist(DatabaseUtilities.SqlTables.WiringEndDevice.sWiringEndDeviceTable, oWiringEndDeviceRecord.ruRecords[0].sId);

                if (!bDoesRecordExist)
                {
                    DatabaseUtilities.BuildInsertSqlForMultipleRecords(DatabaseUtilities.SqlTables.WiringEndDevice.sWiringEndDeviceTable, oWiringEndDeviceRecord);

                }
                else
                {
                    //    //the record already exists this is a copy of a shape..
                    //    oWiringEndDeviceRecord = AddWiringEndDeviceInfo(ovShape);
                    //    DatabaseUtilities.BuildInsertSqlForMultipleRecords(DatabaseUtilities.SqlTables.WiringEndDevice.sWiringEndDeviceTable, oWiringEndDeviceRecord);
                    if (Globals.ThisAddIn.m_delayedEvents.Count > 0)
                    {
                        string sPageID = Globals.ThisAddIn.m_delayedEvents[0].sPageID;

                        oWiringEndDeviceRecord = AddWiringEndDeviceInfo(ovShape, sPageID);
                        DatabaseUtilities.BuildInsertSqlForMultipleRecords(DatabaseUtilities.SqlTables.WiringEndDevice.sWiringEndDeviceTable, oWiringEndDeviceRecord);
                    }
                    else
                    {
                        //the uer pressed redo a second or third time....
                        string sPageID = ovShape.ContainingPage.PageSheet.Cells["User.PageID"].get_ResultStr(0);
                        oWiringEndDeviceRecord = AddWiringEndDeviceInfo(ovShape, sPageID);
                        DatabaseUtilities.BuildInsertSqlForMultipleRecords(DatabaseUtilities.SqlTables.WiringEndDevice.sWiringEndDeviceTable, oWiringEndDeviceRecord);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in AddwiringEndDeviceToDatabase " + ex.Message, "VisAssist");
            }


        }


        //BUIlDING INFORMATION

        //Wire
        internal static MultipleRecordUpdates BuildWireShapeInfo(Visio.Shape ovMainWire, string sWirePairID)
        {
            //this builds the wire info based on the values in the shape itself
            //this is more of an update or gather not an add...
            try
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
                //string sVersion = ovMainWire.Cells["User.Version"].get_ResultStr(0);
                //string sClass = ovMainWire.Cells["User.Class"].get_ResultStr(0);
                //string sColor = ovMainWire.Cells["User.WireColor"].get_ResultStr(0);

                //int iNumberOfConductors = (int)ovMainWire.Cells["Prop.NumberOfConductors"].ResultIU;
                //string sConductor1 = ovMainWire.Cells["User.Conductor1AutoLabel"].get_ResultStr(0);
                //string sConductor2 = ovMainWire.Cells["User.Conductor2AutoLabel"].get_ResultStr(0);
                //string sConductor3 = ovMainWire.Cells["User.Conductor3AutoLabel"].get_ResultStr(0);
                //string sConductor4 = ovMainWire.Cells["User.Conductor4AutoLabel"].get_ResultStr(0);
                //string sConductor5 = ovMainWire.Cells["User.Conductor5AutoLabel"].get_ResultStr(0);
                //string sConductor6 = ovMainWire.Cells["User.Conductor6AutoLabel"].get_ResultStr(0);
                //string sConductor7 = ovMainWire.Cells["User.Conductor7AutoLabel"].get_ResultStr(0);
                //string sConductor8 = ovMainWire.Cells["User.Conductor8AutoLabel"].get_ResultStr(0);
                //string sConductor9 = ovMainWire.Cells["User.Conductor9AutoLabel"].get_ResultStr(0);
                //string sConductor10 = ovMainWire.Cells["User.Conductor10AutoLabel"].get_ResultStr(0);

                string sID = "";
                if (ovMainWire.CellExists["User.ShapeID", 0] == -1)
                {
                    sID = ovMainWire.Cells["User.ShapeID"].get_ResultStr(0);

                }

                if (sWirePairID == "")
                {
                    //we didn't pass in the sWirePairID which means this exists in the db and this build is about updating something or deleting soemthing
                    //get the WirePairID from the db for this sID
                    sWirePairID = GetColumnInfoInWireShapesTableFromDatabase("WirePairID", sID);
                }

                oDictFileValues["WirePairID"] = sWirePairID;


                //Dictionary<string, string> oDictFileValues = new Dictionary<string, string>();
                ////oDictFileValues.Add("ProjectID", sProjectID);
                ////oDictFileValues.Add("FileID", sFileID);
                //oDictFileValues.Add("PageID", sPageID);

                ////add the WirePairID

                //oDictFileValues.Add("WirePairID", sWirePairID);
                ////add the ConnectionID
                //oDictFileValues.Add("ConnectionID", "");

                //oDictFileValues.Add("WireRole", sWireRole);


                //oDictFileValues.Add("Version", sVersion);
                //oDictFileValues.Add("Class", sClass);

                ////add wire lable
                //oDictFileValues.Add("WireLabel", "");


                //oDictFileValues.Add("Color", sColor);

                ////get the x and y location...
                //double dPinX = ovMainWire.Cells["PinX"].ResultIU;
                //double dPinY = ovMainWire.Cells["PinY"].ResultIU;
                //double dPageX;
                //double dPageY;
                //ovMainWire.XYToPage(dPinX, dPinY, out dPageX, out dPageY);

                //int iPageX = (int)dPageX;
                //int iPageY = (int)dPageY;

                ////add the x location 
                //oDictFileValues.Add("XLocation", "");
                ////add the y location 
                //oDictFileValues.Add("YLocation", "");
                ////add the autolabelling
                //oDictFileValues.Add("AutoLabeling", "0"); //integer...
                //oDictFileValues.Add("ConductorCount", iNumberOfConductors.ToString());



                //oDictFileValues.Add("Conductor1Label", sConductor1);
                //oDictFileValues.Add("Conductor2Label", sConductor2);
                //oDictFileValues.Add("Conductor3Label", sConductor3);
                //oDictFileValues.Add("Conductor4Label", sConductor4);
                //oDictFileValues.Add("Conductor5Label", sConductor5);
                //oDictFileValues.Add("Conductor6Label", sConductor6);
                //oDictFileValues.Add("Conductor7Label", sConductor7);
                //oDictFileValues.Add("Conductor8Label", sConductor8);
                //oDictFileValues.Add("Conductor9Label", sConductor9);
                //oDictFileValues.Add("Conductor10Label", sConductor10);

                ////add the show shield
                //oDictFileValues.Add("Shield", "");




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

        //Terminal Block

        internal static MultipleRecordUpdates BuildTerminalBlockInfo(Visio.Shape ovShape)
        {
            //this is building the information based on the info already in the shape, however, if there is no shape id that means 
            //we are dropping this shape for the first time..

            Visio.Document ovDoc = ovShape.ContainingPage.Document;
            try
            {
                string sProjectID = ovDoc.DocumentSheet.Cells["User.ProjectID"].get_ResultStr(0);
                string sFileID = ovDoc.DocumentSheet.Cells["User.FileID"].get_ResultStr(0);
                string sPageID = ovShape.Cells["User.PageID"].get_ResultStr(0);

                //check to see if the sPageID and the sPageIDOnPage are the same...
                string sPageIDofPage = ovShape.ContainingPage.PageSheet.Cells["User.PageID"].get_ResultStr(0);

                if (sPageID == "")
                {
                    //we are adding the shape for the first time so let's apply the page id from the current page 

                    //turn off events before adding the pageid to the shape..
                    ovDoc.Application.EventsEnabled = 0;
                    ovShape.Cells["User.PageID"].Formula = "\"" + sPageIDofPage + "\"";
                    ovDoc.Application.EventsEnabled = -1;
                    sPageID = sPageIDofPage;
                }
                else
                {
                    if (sPageID == sPageIDofPage)
                    {
                        //this is correct
                    }
                    else
                    {
                        //the shape pages id and page id don't match this is a cut or copy to another page...
                        sPageID = sPageIDofPage;
                        //turn off events befroe updating the shapes pageid
                        ovDoc.Application.EventsEnabled = 0;
                        ovShape.Cells["User.PageID"].Formula = VisioUtilities.Application.FormatStringForVisio(sPageID);
                        ovDoc.Application.EventsEnabled = -1;
                    }
                }

                Dictionary<string, string> oDictFileValues = GatherTerminalBlockInformation(ovShape, sPageID);


                string sID = "";
                if (ovShape.CellExists["User.ShapeID", 0] == -1)
                {
                    sID = ovShape.Cells["User.ShapeID"].get_ResultStr(0);
                    if (sID == "")
                    {
                        //this could be empty if we are adding it for the first time...
                        sID = GenerateShapeID(sProjectID, sFileID, sPageID, ovShape.Name, DateTime.Now);
                        //turn off events before adding to the shape
                        ovDoc.Application.EventsEnabled = 0;
                        ovShape.Cells["User.ShapeID"].Formula = "\"" + sID + "\"";
                        ovDoc.Application.EventsEnabled = -1;
                    }
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
        internal static MultipleRecordUpdates AddTerminalBlockInfo(Visio.Shape ovShape, string sPageID)
        {
            //this is when we are adding a new terminal block, could be adding it new or adding a new one from a copy 
            Visio.Document ovDoc = ovShape.ContainingPage.Document;
            try
            {
                string sProjectID = ovDoc.DocumentSheet.Cells["User.ProjectID"].get_ResultStr(0);
                string sFileID = ovDoc.DocumentSheet.Cells["User.FileID"].get_ResultStr(0);



                if (!Globals.ThisAddIn.Application.IsUndoingOrRedoing)
                {
                    //turn off events befroe updating the shapes pageid
                    ovDoc.Application.EventsEnabled = 0;
                    ovShape.Cells["User.PageID"].Formula = VisioUtilities.Application.FormatStringForVisio(sPageID);
                    ovDoc.Application.EventsEnabled = -1;

                }


                Dictionary<string, string> oDictFileValues = GatherTerminalBlockInformation(ovShape, sPageID);


                string sID = "";
                if (ovShape.CellExists["User.ShapeID", 0] == -1)
                {

                    sID = GenerateShapeID(sProjectID, sFileID, sPageID, ovShape.Name, DateTime.Now);
                    //turn off events before adding to the shape
                    ovDoc.Application.EventsEnabled = 0;
                    ovShape.Cells["User.ShapeID"].Formula = "\"" + sID + "\"";
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

        private static Dictionary<string, string> GatherTerminalBlockInformation(Visio.Shape ovShape, string sPageID)
        {
            Dictionary<string, string> oDictFileValues = new Dictionary<string, string>();
            try
            {
                Visio.Document ovDoc = ovShape.Document;

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



                oDictFileValues.Add("PageID", sPageID);
                oDictFileValues.Add("Color", sColor);
                oDictFileValues.Add("ShapeText", sShapeText);
                oDictFileValues.Add("XLocation", iPageX.ToString());
                oDictFileValues.Add("YLocation", iPageY.ToString());


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
                    if (!Globals.ThisAddIn.Application.IsUndoingOrRedoing)
                    {
                        //turn off events before adding the gridlocation to the shape..
                        ovDoc.Application.EventsEnabled = 0;
                        ovShape.Cells["User.GridLocation"].Formula = $"\"{sGridLocation}\"";
                        ovDoc.Application.EventsEnabled = -1;
                    }


                }
                else
                {
                    //turn off events before adding the gridlocation to the shape..
                    ovDoc.Application.EventsEnabled = 0;
                    ovShape.AddNamedRow((short)Visio.VisSectionIndices.visSectionUser, "GridLocation", 0);
                    ovShape.Cells["User.GridLocation"].Formula = $"\"{sGridLocation}\"";
                    ovDoc.Application.EventsEnabled = -1;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in GatherTerminalBlockInformation " + ex.Message, "VisAssist");
            }
            return oDictFileValues;

        }

        //End Device
        internal static MultipleRecordUpdates BuildWiringEndDeviceInfo(Visio.Shape ovShape)
        {
            RecordUpdate ruFileRecord = new RecordUpdate();
            try
            {
                Visio.Document ovDoc = ovShape.ContainingPage.Document;
                string sProjectID = ovDoc.DocumentSheet.Cells["User.ProjectID"].get_ResultStr(0);
                string sFileID = ovDoc.DocumentSheet.Cells["User.FileID"].get_ResultStr(0);
                string sPageIDFromShape = ovShape.Cells["User.PageID"].get_ResultStr(0);

                string sPageIDFromPage = ovShape.ContainingPage.PageSheet.Cells["User.PageID"].get_ResultStr(0);
                if (sPageIDFromShape == "")
                {
                    //we are adding the shape for the first time so let's apply the page id from the current page 
                    if (!Globals.ThisAddIn.Application.IsUndoingOrRedoing)
                    {
                        //turn off events before adding the pageid to the shape..
                        ovDoc.Application.EventsEnabled = 0;
                        ovShape.Cells["User.PageID"].Formula = "\"" + sPageIDFromPage + "\"";
                        ovDoc.Application.EventsEnabled = -1;
                        sPageIDFromShape = sPageIDFromPage;
                    }
                }
                else
                {
                    if (sPageIDFromShape == sPageIDFromPage)
                    {
                        //this is correct
                    }
                    else
                    {
                        if (!Globals.ThisAddIn.Application.IsUndoingOrRedoing)
                        {
                            //the shape pages id and page id don't match this is a cut or copy to another page...
                            sPageIDFromShape = sPageIDFromPage;
                            //turn off events befroe updating the shapes pageid
                            ovDoc.Application.EventsEnabled = 0;
                            ovShape.Cells["User.PageID"].Formula = VisioUtilities.Application.FormatStringForVisio(sPageIDFromShape);
                            ovDoc.Application.EventsEnabled = -1;
                        }
                    }
                }

                Dictionary<string, string> oDictFileValues = GatherEndDeviceInformation(ovShape, sPageIDFromShape);


                string sID = "";
                if (ovShape.CellExists["User.ShapeID", 0] == -1)
                {
                    sID = ovShape.Cells["User.ShapeID"].get_ResultStr(0);
                    if (sID == "")
                    {
                        //this could be empty if we are adding it for the first time...
                        sID = GenerateShapeID(sProjectID, sFileID, sPageIDFromShape, ovShape.Name, DateTime.Now);
                        //turn off events before adding to the shape
                        ovDoc.Application.EventsEnabled = 0;
                        ovShape.Cells["User.ShapeID"].Formula = "\"" + sID + "\"";
                        ovDoc.Application.EventsEnabled = -1;
                    }
                }




                ruFileRecord.sPrimaryKeyColumn = DatabaseUtilities.SqlTables.WiringEndDevice.sWiringEndDeviceTablePK;
                ruFileRecord.sId = sID;
                ruFileRecord.odictColumnValues = oDictFileValues;


            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in BuildWiringEndDeviceInfo " + ex.Message, "VisAssist");
            }
            return new MultipleRecordUpdates(new List<RecordUpdate> { ruFileRecord });
        }

        internal static MultipleRecordUpdates AddWiringEndDeviceInfo(Visio.Shape ovShape, string sPageID)
        {
            //this is when we are adding a new terminal block, could be adding it new or adding a new one from a copy 
            Visio.Document ovDoc = ovShape.ContainingPage.Document;
            try
            {
                string sProjectID = ovDoc.DocumentSheet.Cells["User.ProjectID"].get_ResultStr(0);
                string sFileID = ovDoc.DocumentSheet.Cells["User.FileID"].get_ResultStr(0);


                ////check to see if the sPageID and the sPageIDOnPage are the same...
                //string sPageID = ovShape.ContainingPage.PageSheet.Cells["User.PageID"].get_ResultStr(0);


                //turn off events befroe updating the shapes pageid
                if (!Globals.ThisAddIn.Application.IsUndoingOrRedoing)
                {
                    ovDoc.Application.EventsEnabled = 0;
                    ovShape.Cells["User.PageID"].Formula = VisioUtilities.Application.FormatStringForVisio(sPageID);
                    ovDoc.Application.EventsEnabled = -1;

                }

                Dictionary<string, string> oDictFileValues = GatherEndDeviceInformation(ovShape, sPageID);


                string sID = "";
                if (ovShape.CellExists["User.ShapeID", 0] == -1)
                {
                    sID = GenerateShapeID(sProjectID, sFileID, sPageID, ovShape.Name, DateTime.Now);
                    //turn off events before adding to the shape
                    ovDoc.Application.EventsEnabled = 0;
                    ovShape.Cells["User.ShapeID"].Formula = "\"" + sID + "\"";
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

        private static Dictionary<string, string> GatherEndDeviceInformation(Visio.Shape ovShape, string sPageID)
        {
            Dictionary<string, string> oDictFileValues = new Dictionary<string, string>();
            try
            {
                Visio.Document ovDoc = ovShape.Document;

                int iTermCount = (int)ovShape.Cells["Prop.TermCount"].ResultIU;
                string sTerm1 = ovShape.Cells["Prop.term1"].get_ResultStr(0);
                string sTerm2 = ovShape.Cells["Prop.term2"].get_ResultStr(0);
                string sTerm3 = ovShape.Cells["Prop.term3"].get_ResultStr(0);
                string sTerm4 = ovShape.Cells["Prop.term4"].get_ResultStr(0);
                string sTerm5 = ovShape.Cells["Prop.term5"].get_ResultStr(0);
                string sTerm6 = ovShape.Cells["Prop.term6"].get_ResultStr(0);
                string sTerm7 = ovShape.Cells["Prop.term7"].get_ResultStr(0);
                string sTerm8 = ovShape.Cells["Prop.term8"].get_ResultStr(0);
                string sTerm9 = ovShape.Cells["Prop.term9"].get_ResultStr(0);
                string sTerm10 = ovShape.Cells["Prop.term10"].get_ResultStr(0);

                string sTag = ovShape.Cells["Prop.Tag"].get_ResultStr(0);

                //get the x and y location...
                double dPinX = ovShape.Cells["PinX"].ResultIU;
                double dPinY = ovShape.Cells["PinY"].ResultIU;
                double dPageX;
                double dPageY;
                ovShape.XYToPage(dPinX, dPinY, out dPageX, out dPageY);

                int iPageX = (int)dPageX;
                int iPageY = (int)dPageY;


                oDictFileValues.Add("PageID", sPageID);
                oDictFileValues.Add("TermCount", iTermCount.ToString());
                oDictFileValues.Add("Term1", sTerm1);
                oDictFileValues.Add("Term2", sTerm2);
                oDictFileValues.Add("Term3", sTerm3);
                oDictFileValues.Add("Term4", sTerm4);
                oDictFileValues.Add("Term5", sTerm5);
                oDictFileValues.Add("Term6", sTerm6);
                oDictFileValues.Add("Term7", sTerm7);
                oDictFileValues.Add("Term8", sTerm8);
                oDictFileValues.Add("Term9", sTerm9);
                oDictFileValues.Add("Term10", sTerm10);
                oDictFileValues.Add("Tag", sTag);
                oDictFileValues.Add("XLocation", iPageX.ToString());
                oDictFileValues.Add("YLocation", iPageY.ToString());


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
                    if (!Globals.ThisAddIn.Application.IsUndoingOrRedoing)
                    {
                        //turn off events before adding the gridlocation to the shape..
                        ovDoc.Application.EventsEnabled = 0;
                        ovShape.Cells["User.GridLocation"].Formula = $"\"{sGridLocation}\"";
                        ovDoc.Application.EventsEnabled = -1;
                    }

                }
                else
                {
                    //turn off events before adding the gridlocation to the shape..
                    ovDoc.Application.EventsEnabled = 0;
                    ovShape.AddNamedRow((short)Visio.VisSectionIndices.visSectionUser, "GridLocation", 0);
                    ovShape.Cells["User.GridLocation"].Formula = $"\"{sGridLocation}\"";
                    ovDoc.Application.EventsEnabled = -1;
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in GatherEndDeviceInformation " + ex.Message, "VisAssist");
            }
            return oDictFileValues;
        }


        //DELETING

        //Wire
        internal static void DeleteWireFromDatabase(Visio.Shape ovShape)
        {
            try
            {


                MultipleRecordUpdates oWireRecord = BuildWireShapeInfo(ovShape, "");

                //before deleting it in the DB we should also delete the secondary wire off of the page (or the primary-whatever is the opposite..)
                DatabaseUtilities.BuildDeleteSqlForMultipleRecords(DatabaseUtilities.SqlTables.WireShapesTable.sWireShapeTable, oWireRecord);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in DeleteWireFromDatabase " + ex.Message, "VisAssist");
            }
        }
        //Terminal Block
        internal static void DeleteTerminalBlockFromDatabase(Visio.Shape ovShape)
        {
            try
            {

                MultipleRecordUpdates oTerminalBlockRecord = BuildTerminalBlockInfo(ovShape);
                DatabaseUtilities.BuildDeleteSqlForMultipleRecords(DatabaseUtilities.SqlTables.TerminalBlocksTable.sTerminalBlockTable, oTerminalBlockRecord);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in DeleteTerminalBlockFromDatabase " + ex.Message, "VisAssist");
            }
        }
        //End Device
        internal static void DeleteEndDeviceFromDatabase(Visio.Shape ovShape)
        {
            try
            {
                MultipleRecordUpdates oEndDeviceRecord = BuildWiringEndDeviceInfo(ovShape);
                DatabaseUtilities.BuildDeleteSqlForMultipleRecords(DatabaseUtilities.SqlTables.WiringEndDevice.sWiringEndDeviceTable, oEndDeviceRecord);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in DeleteEndDeviceFromDatabase " + ex.Message, "VisAssist");
            }
        }


        //UPDATING

        //Terminal Block 
        internal static void UpdateTerminalBlockInDatabase(Visio.Shape ovShape)
        {
            try
            {
                //if the shape has a ShapeID then this will be an update if not we are acutally in the middle of an undo and we want to remove the shape from the db
                string sShapeID = ovShape.Cells["User.ShapeID"].get_ResultStr(0);
                if (sShapeID == "")
                {
                    string sVisAssistFolderPath = FileUtilities.GetFolderPath(ovShape.Document);
                    //this is an undo/delete...
                    DatabaseUtilities.CheckShapeExistence(ovShape.Document, sVisAssistFolderPath);
                }
                else
                {
                    //this gets' called when the user moves a shape, update text...
                    //let's check to see if the record exists in the db, if it does continue as normal if it doesn't this is a redo of an undo...
                    bool bRecordExists = DatabaseUtilities.DoesRecordExist(DatabaseUtilities.SqlTables.TerminalBlocksTable.sTerminalBlockTable, sShapeID);
                    if (bRecordExists)
                    {
                        MultipleRecordUpdates oTerminalBlockRecord = BuildTerminalBlockInfo(ovShape);
                        DatabaseUtilities.BuildUpdateSqlForMultipleRecords(DatabaseUtilities.SqlTables.TerminalBlocksTable.sTerminalBlockTable, oTerminalBlockRecord);
                    }
                    else
                    {
                        //we need to readd the record to the db...
                        MultipleRecordUpdates oTerminalBlockRecord = BuildTerminalBlockInfo(ovShape); //calling build instead of add because there is already a shapeId...
                        DatabaseUtilities.BuildInsertSqlForMultipleRecords(DatabaseUtilities.SqlTables.TerminalBlocksTable.sTerminalBlockTable, oTerminalBlockRecord);
                    }

                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in UpdateTerminalBlockInDatabase " + ex.Message, "VisAssist");
            }
        }

        //End Device
        internal static void UpdateEndDeviceInDatabase(Visio.Shape ovShape)
        {
            try
            {
                //this gets' called when the user moves a shape...
                MultipleRecordUpdates oEndDeviceInfo = BuildWiringEndDeviceInfo(ovShape);
                DatabaseUtilities.BuildUpdateSqlForMultipleRecords(DatabaseUtilities.SqlTables.WiringEndDevice.sWiringEndDeviceTable, oEndDeviceInfo);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in UpdateEndDeviceInDatabase " + ex.Message, "VisAssist");
            }
        }





        //UTILITIES
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

        internal static void AddShapesToDatabase(Visio.Page ovVisioPage, string sProjectID)
        {
            try
            {
                //gets called when we are adding a page that already has shapes on it and need to add the shapes to the db...
                //this gets called when the user is bringing back a page from an undo event...
                foreach (Visio.Shape ovShape in ovVisioPage.Shapes)
                {
                    if (ovShape.CellExists["User.Class", 0] == -1)
                    {
                        //this is one of our shapes..
                        string sClass = ovShape.Cells["User.Class"].get_ResultStr(0);
                        switch (sClass)
                        {
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
                            case "SmartWire":
                                {
                                    break;
                                }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in AddShapesToDatabase " + ex.Message, "VisAssist");
            }
        }

        internal static List<string> PopulateShapesInDocument(Visio.Document ovDocument, string sClassToGather)
        {
            //this populates a list of the shapes (given a class) on a given page
            List<string> lstShapesToReturn = new List<string>();
            try
            {

                string sID = "";
                foreach (Visio.Page ovPage in ovDocument.Pages)
                {
                    if (ovPage.PageSheet.CellExists["User.PageClass", 0] == -1)
                    {
                        //based on sClass populate a list of shapes on ther page 
                        foreach (Visio.Shape ovShape in ovPage.Shapes)
                        {
                            if (ovShape.CellExists["User.Class", 0] == -1)
                            {
                                string sClass = ovShape.Cells["User.Class"].get_ResultStr(0);

                                switch (sClassToGather)
                                {
                                    case "TerminalBlock":
                                        {
                                            //we want to gather terminal blocks
                                            if (sClass == "TerminalBlock")
                                            {
                                                sID = ovShape.Cells["User.ShapeID"].get_ResultStr(0);

                                                lstShapesToReturn.Add(sID);
                                            }

                                            break;
                                        }
                                    case "SmartWire":
                                        {
                                            if (sClass == "SmartWire")
                                            {
                                                sID = ovShape.Cells["User.ShapeID"].get_ResultStr(0);

                                                lstShapesToReturn.Add(sID);
                                            }
                                            break;
                                        }
                                    case "ADC End Device":
                                        {
                                            if (sClass == "ADC End Device")
                                            {
                                                sID = ovShape.Cells["User.ShapeID"].get_ResultStr(0);

                                                lstShapesToReturn.Add(sID);
                                            }
                                            break;
                                        }
                                }
                            }
                        }
                    }
                }
                return lstShapesToReturn;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in PopulateShapesOnPage " + ex.Message, "VisAssist");
            }
            return lstShapesToReturn;
        }
        internal static string GenerateShapeID(string sProjectID, string sFileID, string sPageID, string sShapeName, DateTime now)
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

        internal static void GetShapesInTable(string sTableName, Visio.Document ovDocument)
        {
            try
            {
                //Visio.Document ovDoc = Globals.ThisAddIn.Application.ActiveDocument;

                string sFileID = ovDocument.DocumentSheet.Cells["User.FileID"].get_ResultStr(0);

                if (m_mruRecordsBase.ruRecords != null)
                {
                    m_mruRecordsBase.ruRecords.Clear();
                }

                List<RecordUpdate> lstRecords = new List<RecordUpdate>();



                string sSql = $@"SELECT tb.* FROM {sTableName} tb INNER JOIN pages_table p ON tb.PageID = p.PageID WHERE p.FileID = @FileID;";


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
                                RecordUpdate ru = new RecordUpdate();
                                ru.sId = sqlitereadReader["ShapeID"].ToString();
                                ru.sPrimaryKeyColumn = "ShapeID";
                                ru.odictColumnValues = null;

                                lstRecords.Add(ru);
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


        internal static void UpdateWireInDatabase(Visio.Shape ovShape)
        {
            try
            {
                string sShapeID = ovShape.Cells["User.ShapeID"].get_ResultStr(0);
                //get the WirePairID for ovShape
                string sWirePairID = ShapesUtilities.GetColumnInfoInWireShapesTableFromDatabase("WirePairID", sShapeID);
                //this gets' called when the user moves a shape...
                MultipleRecordUpdates oWireInfo = BuildWireShapeInfo(ovShape, sWirePairID);
                DatabaseUtilities.BuildUpdateSqlForMultipleRecords(DatabaseUtilities.SqlTables.WireShapesTable.sWireShapeTable, oWireInfo);

                //this needs to update this wires mates location...
                UpdateWireGridLocation(ovShape, oWireInfo);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in UpdateWireInDatabase " + ex.Message, "VisAssist");
            }
        }
    }
}
