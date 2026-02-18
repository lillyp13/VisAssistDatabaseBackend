using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using VisAssistDatabaseBackEnd.DataUtilities;
using Visio = Microsoft.Office.Interop.Visio;

namespace VisAssistDatabaseBackEnd.ShapeUtilities
{
    internal class TerminalBlockUtilities
    {

        //CRUD ACTIONS  
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
                  
                    UpdateTerminalBlockInDatabase(ovShape);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in AddTerminalBlockToDatabase " + ex.Message, "VisAssist");
            }

        }
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


        //HELPER FUNCTIONS 

        internal static MultipleRecordUpdates BuildTerminalBlockInfo(Visio.Shape ovShape)
        {
            //this is building the information based on the info already in the shape, however, if there is no shape id that means 
            //we are dropping this shape for the first time..

            Visio.Document ovDoc = ovShape.ContainingPage.Document;
            try
            {
                string sProjectID = ovDoc.DocumentSheet.Cells["User.ProjectID"].get_ResultStr(0);
                string sFileID = ovDoc.DocumentSheet.Cells["User.FileID"].get_ResultStr(0);
                string sPageID = ovShape.ContainingPage.PageSheet.Cells["User.PageID"].get_ResultStr(0);

                Dictionary<string, string> oDictFileValues = GatherTerminalBlockInformation(ovShape, sPageID);


                string sID = "";
                if (ovShape.CellExists["User.ShapeID", 0] == -1)
                {
                    sID = ovShape.Cells["User.ShapeID"].get_ResultStr(0);
                    if (sID == "")
                    {
                        //this could be empty if we are adding it for the first time...
                        sID = ShapesUtilities.GenerateShapeID(sProjectID, sFileID, sPageID, ovShape.Name, DateTime.Now);
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
                    //ovDoc.Application.EventsEnabled = 0;
                    //ovShape.Cells["User.PageID"].Formula = VisioUtilities.Application.FormatStringForVisio(sPageID);
                    //ovDoc.Application.EventsEnabled = -1;

                }


                Dictionary<string, string> oDictFileValues = GatherTerminalBlockInformation(ovShape, sPageID);


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

        internal static Dictionary<string, string> GatherTerminalBlockInformation(Visio.Shape ovShape, string sPageID)
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
                MessageBox.Show("Error in GatherTerminalBlockInformation " + ex.Message, "VisAssist");
            }
            return oDictFileValues;

        }

    }
}
