using System;
using System.Collections.Generic;
using System.Windows.Forms;
using VisAssistDatabaseBackEnd.DataUtilities;
using Visio = Microsoft.Office.Interop.Visio;

namespace VisAssistDatabaseBackEnd.ShapeUtilities
{
    internal class EndDeviceUtilities
    {

        //CRUD ACTIONS
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
                    //this might need some work but end devices in general need a bit of work i think...
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

        internal static void DeleteEndDeviceFromDatabase(string sShapeID)
        {
            try
            {

                MultipleRecordUpdates oEndDeviceRecord = new MultipleRecordUpdates();
                oEndDeviceRecord.ruRecords = new List<RecordUpdate>();
                RecordUpdate ru = new RecordUpdate();
                ru.sId = sShapeID;
                ru.sPrimaryKeyColumn = DatabaseUtilities.SqlTables.WiringEndDevice.sWiringEndDeviceTablePK;
                oEndDeviceRecord.ruRecords.Add(ru);

                //before deleting it in the DB we should also delete the secondary wire off of the page (or the primary-whatever is the opposite..)
                DatabaseUtilities.BuildDeleteSqlForMultipleRecords(DatabaseUtilities.SqlTables.WiringEndDevice.sWiringEndDeviceTable, oEndDeviceRecord);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in DeleteEndDeviceFromDatabase " + ex.Message, "VisAssist");
            }
        }

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



        //HELPER FUNCTIONS
        internal static MultipleRecordUpdates BuildWiringEndDeviceInfo(Visio.Shape ovShape)
        {
            RecordUpdate ruFileRecord = new RecordUpdate();
            try
            {
                Visio.Document ovDoc = ovShape.ContainingPage.Document;
                string sProjectID = ovDoc.DocumentSheet.Cells["User.ProjectID"].get_ResultStr(0);
                string sFileID = ovDoc.DocumentSheet.Cells["User.FileID"].get_ResultStr(0);
                string sPageIDFromShape = ovShape.ContainingPage.PageSheet.Cells["User.PageID"].get_ResultStr(0);


                Dictionary<string, string> odictFileValues = GatherEndDeviceInformation(ovShape, sPageIDFromShape);


                string sID = "";
                if (ovShape.CellExists["User.ShapeID", 0] == -1)
                {
                    sID = ovShape.Cells["User.ShapeID"].get_ResultStr(0);
                    if (sID == "")
                    {
                        //this could be empty if we are adding it for the first time...
                        sID = ShapesUtilities.GenerateShapeID(sProjectID, sFileID, sPageIDFromShape, ovShape.Name, DateTime.Now);
                        //turn off events before adding to the shape
                        ovDoc.Application.EventsEnabled = 0;
                        ovShape.Cells["User.ShapeID"].Formula = "\"" + sID + "\"";
                        ovDoc.Application.EventsEnabled = -1;
                    }
                }




                ruFileRecord.sPrimaryKeyColumn = DatabaseUtilities.SqlTables.WiringEndDevice.sWiringEndDeviceTablePK;
                ruFileRecord.sId = sID;
                ruFileRecord.odictColumnValues = odictFileValues;


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



                Dictionary<string, string> odictFileValues = GatherEndDeviceInformation(ovShape, sPageID);


                string sID = "";
                if (ovShape.CellExists["User.ShapeID", 0] == -1)
                {
                    sID = ShapesUtilities.GenerateShapeID(sProjectID, sFileID, sPageID, ovShape.Name, DateTime.Now);
                    //turn off events before adding to the shape
                    ovDoc.Application.EventsEnabled = 0;
                    ovShape.Cells["User.ShapeID"].Formula = "\"" + sID + "\"";
                    ovDoc.Application.EventsEnabled = -1;

                }


                RecordUpdate ruFileRecord = new RecordUpdate();
                ruFileRecord.sPrimaryKeyColumn = DatabaseUtilities.SqlTables.TerminalBlocksTable.sTerminalBlockTablePK;
                ruFileRecord.sId = sID;
                ruFileRecord.odictColumnValues = odictFileValues;

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
            Dictionary<string, string> odictFileValues = new Dictionary<string, string>();
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


                odictFileValues.Add("PageID", sPageID);
                odictFileValues.Add("TermCount", iTermCount.ToString());
                odictFileValues.Add("Term1", sTerm1);
                odictFileValues.Add("Term2", sTerm2);
                odictFileValues.Add("Term3", sTerm3);
                odictFileValues.Add("Term4", sTerm4);
                odictFileValues.Add("Term5", sTerm5);
                odictFileValues.Add("Term6", sTerm6);
                odictFileValues.Add("Term7", sTerm7);
                odictFileValues.Add("Term8", sTerm8);
                odictFileValues.Add("Term9", sTerm9);
                odictFileValues.Add("Term10", sTerm10);
                odictFileValues.Add("Tag", sTag);
                odictFileValues.Add("XLocation", iPageX.ToString());
                odictFileValues.Add("YLocation", iPageY.ToString());


                int iPageIndex = ovShape.ContainingPage.Index;
                string sHorizontalMarkers = "8;7;6;5;4;3;2;1"; //will need to get this from the page
                string sVertMarkers = "A;B;C;D;E;F;G;H";//will need to get this from the page based on vertical/horizontal/pagescale...
                double dPageWidth = ovShape.ContainingPage.PageSheet.Cells["PageWidth"].ResultIU;
                double dPageHeight = ovShape.ContainingPage.PageSheet.Cells["PageHeight"].ResultIU;
                double dLeft = ovShape.CellsU["PinX"].ResultIU - (ovShape.CellsU["Width"].ResultIU / 2);
                double dTop = ovShape.CellsU["PinY"].ResultIU - (ovShape.CellsU["Height"].ResultIU / 2);

                double dWidth = ovShape.CellsU["Width"].ResultIU;
                double dHeight = ovShape.CellsU["Height"].ResultIU;


                string sGridLocation = ShapesUtilities.GetShapeGridLocation(dLeft, dTop, dWidth, dHeight, dPageWidth, dPageHeight, sVertMarkers, sHorizontalMarkers, iPageIndex);



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
            return odictFileValues;
        }

    }
}
