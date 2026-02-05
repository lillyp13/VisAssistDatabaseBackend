using Microsoft.Office.Core;
using Microsoft.Office.Interop.Visio;
using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
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
            //this builds the information and then runs the sql to add the wire shape to the wire_shapes_table
            MultipleRecordUpdates oWireRecord = BuildWireShapeInfo(ovShape);
            //check if the record already exists (i think this event is firing twice possibly)
            bool bDoesRecordExist = DatabaseUtilities.DoesRecordExist(DatabaseUtilities.SqlTables.WireShapesTable.sWireShapeTable, oWireRecord.ruRecords[0].sId);

            if(!bDoesRecordExist)
            {
                DatabaseUtilities.BuildInsertSqlForMultipleRecords(DatabaseUtilities.SqlTables.WireShapesTable.sWireShapeTable, oWireRecord);

                //will also need to add it to the wire_pairs_table....
                //this gets triggered when the user drops a wire on the page, do we drop another one which would be our secondary wire...?
                MultipleRecordUpdates oWirePairRecord = BuildWirePairInfo(ovShape);
            }
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
        private static MultipleRecordUpdates BuildWireShapeInfo(Visio.Shape ovShape)
        {

            Visio.Document ovDoc = ovShape.ContainingPage.Document;
            string sProjectID = ovDoc.DocumentSheet.Cells["User.ProjectID"].get_ResultStr(0);
            string sFileID = ovDoc.DocumentSheet.Cells["User.FileID"].get_ResultStr(0);
            string sPageID = ovShape.ContainingPage.PageSheet.Cells["User.PageID"].get_ResultStr(0);

            string sWireRole = ovShape.Cells["User.WireRole"].get_ResultStr(0);

            string sVersion = ovShape.Cells["User.Version"].get_ResultStr(0);
            string sClass = ovShape.Cells["User.Class"].get_ResultStr(0);
            string sColor = ovShape.Cells["User.WireColor"].get_ResultStr(0);

            int iNumberOfConductors = (int)ovShape.Cells["Prop.NumberOfConductors"].ResultIU;
            string sConductor1 = ovShape.Cells["User.Conductor1AutoLabel"].get_ResultStr(0);
            string sConductor2 = ovShape.Cells["User.Conductor2AutoLabel"].get_ResultStr(0);
            string sConductor3 = ovShape.Cells["User.Conductor3AutoLabel"].get_ResultStr(0);
            string sConductor4 = ovShape.Cells["User.Conductor4AutoLabel"].get_ResultStr(0);
            string sConductor5 = ovShape.Cells["User.Conductor5AutoLabel"].get_ResultStr(0);
            string sConductor6 = ovShape.Cells["User.Conductor6AutoLabel"].get_ResultStr(0);
            string sConductor7 = ovShape.Cells["User.Conductor7AutoLabel"].get_ResultStr(0);
            string sConductor8 = ovShape.Cells["User.Conductor8AutoLabel"].get_ResultStr(0);
            string sConductor9 = ovShape.Cells["User.Conductor9AutoLabel"].get_ResultStr(0);
            string sConductor10 = ovShape.Cells["User.Conductor10AutoLabel"].get_ResultStr(0);

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



            Dictionary<string, string> oDictFileValues = new Dictionary<string, string>();
            oDictFileValues.Add("ProjectID", sProjectID);
            oDictFileValues.Add("FileID", sFileID);
            oDictFileValues.Add("PageID", sPageID);

            //add the WirePairID
            oDictFileValues.Add("WirePairID", "");
            //add SystemID
            oDictFileValues.Add("SystemID", "");
            //add the ConnectionID
            oDictFileValues.Add("ConnectionID", "");

            oDictFileValues.Add("WireRole", sWireRole);
            //add the tag? is this from user cells?
            oDictFileValues.Add("Tag", "");

            oDictFileValues.Add("Version", sVersion);
            oDictFileValues.Add("Class", sClass);

            //add wire lable
            oDictFileValues.Add("WireLabel", "");


            oDictFileValues.Add("Color", sColor);
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
            oDictFileValues.Add("ShowShield", "");
            //add the sheildtop
            oDictFileValues.Add("ShieldTop", "");
            //add the shieldbottom
            oDictFileValues.Add("ShieldBottom", "");



            RecordUpdate ruFileRecord = new RecordUpdate();
            ruFileRecord.sPrimaryKeyColumn = DatabaseUtilities.SqlTables.WireShapesTable.sWireShapeTablePK;
            ruFileRecord.sId = sID;
            ruFileRecord.odictColumnValues = oDictFileValues;

            return new MultipleRecordUpdates(new List<RecordUpdate> { ruFileRecord });
        }
        private static MultipleRecordUpdates BuildWirePairInfo(Visio.Shape ovShape)
        {
            //not complete yet...
            return new MultipleRecordUpdates();
        }

        //Terminal Block
        private static MultipleRecordUpdates BuildTerminalBlockInfo(Visio.Shape ovShape)
        {

            Visio.Document ovDoc = ovShape.ContainingPage.Document;
            string sProjectID = ovDoc.DocumentSheet.Cells["User.ProjectID"].get_ResultStr(0);
            string sFileID = ovDoc.DocumentSheet.Cells["User.FileID"].get_ResultStr(0);
            string sPageID = ovShape.ContainingPage.PageSheet.Cells["User.PageID"].get_ResultStr(0);

            string sColor = ovShape.Cells["Prop.Color"].get_ResultStr(0);
            string sShapeText = ovShape.Cells["Prop.ShapeText"].get_ResultStr(0);


            Dictionary<string, string> oDictFileValues = new Dictionary<string, string>();
            oDictFileValues.Add("ProjectID", sProjectID);
            oDictFileValues.Add("FileID", sFileID);
            oDictFileValues.Add("PageID", sPageID);
            oDictFileValues.Add("Color", sColor);
            oDictFileValues.Add("ShapeText", sShapeText);

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
            ruFileRecord.sPrimaryKeyColumn = DatabaseUtilities.SqlTables.TerminalBlocksTable.sTerminalBlockTablePK;
            ruFileRecord.sId = sID;
            ruFileRecord.odictColumnValues = oDictFileValues;

            return new MultipleRecordUpdates(new List<RecordUpdate> { ruFileRecord });
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
            MultipleRecordUpdates oWireRecord = BuildWireShapeInfo(ovShape);
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





    }
}
