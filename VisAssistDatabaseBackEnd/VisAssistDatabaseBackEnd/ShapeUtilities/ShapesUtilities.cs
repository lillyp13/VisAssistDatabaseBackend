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
using VisAssistDatabaseBackEnd.DataUtilities;
using VisAssistDatabaseBackEnd.Forms;
using VisAssistDatabaseBackEnd.ShapeUtilities;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Visio = Microsoft.Office.Interop.Visio;
using VisAssistDatabaseBackEnd.ShapeUtilities.Wire;

namespace VisAssistDatabaseBackEnd.ShapeUtilities
{
    internal class ShapesUtilities
    {


        public static MultipleRecordUpdates m_mruRecordsBase = new MultipleRecordUpdates();
        public static MultipleRecordUpdates m_mruRecordsToCompare = new MultipleRecordUpdates();
        public static MultipleRecordUpdates m_mruRecordsToUpdate = new MultipleRecordUpdates();
        private static PagesForm m_PagesForm;



        internal static void AddShapesToDatabase(List<string> oListVisioPages, string sProjectID, Visio.Document ovDocument)
        {
            try
            {
                //gets called when we are adding a page that already has shapes on it and need to add the shapes to the db...
                //this gets called when the user is bringing back a page from an undo event...
                Dictionary<string, Visio.Shape> oDictWires = new Dictionary<string, Visio.Shape>();
                foreach (Visio.Page ovPage in ovDocument.Pages)
                {
                    if (oListVisioPages.Contains(ovPage.Name))
                    {
                        foreach (Visio.Shape ovShape in ovPage.Shapes)
                        {
                            if (ovShape.CellExists["User.Class", 0] == -1)
                            {
                                //this is one of our shapes..
                                string sClass = ovShape.Cells["User.Class"].get_ResultStr(0);
                                switch (sClass)
                                {
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
                                    case "SmartWire":
                                        {
                                            //gather a list of the wires to add...
                                            string sKey = ovShape.Cells["User.WirePairID"].get_ResultStr(0) + "|" + ovShape.ID + "|" + ovShape.ContainingPage.Name;

                                            oDictWires.Add(sKey, ovShape);
                                            break;
                                        }
                                }
                            }
                        }

                    }
                }



                //now we have a full list of wires and we need to pair them based on their wirepairids..
                //the wire pair id is the the first part of the key in the dictionary (before the first pipe |)
                //use that to find the other matching wirepairid in the dictionary...
                // Group shapes by WirePairID
                Dictionary<string, List<Visio.Shape>> odictWirePairs = new Dictionary<string, List<Visio.Shape>>();

                foreach (KeyValuePair<string, Visio.Shape> BaseItem in oDictWires)
                {
                    string sKey = BaseItem.Key;
                    Visio.Shape shape = BaseItem.Value;

                    // Extract WirePairID (everything before the first '|')
                    string sWirePairID = sKey.Split('|')[0];

                    if (!odictWirePairs.ContainsKey(sWirePairID))
                    {
                        odictWirePairs[sWirePairID] = new List<Visio.Shape>();
                    }


                    odictWirePairs[sWirePairID].Add(shape);
                }

                foreach (KeyValuePair<string, List<Visio.Shape>> BaseItem in odictWirePairs)
                {
                    string wirePairID = BaseItem.Key;
                    List<Visio.Shape> pairShapes = BaseItem.Value;

                    if (pairShapes.Count == 2)
                    {
                        Visio.Shape wireA = pairShapes[0];
                        Visio.Shape wireB = pairShapes[1];

                        MultipleRecordUpdates oPrimaryRecord = new MultipleRecordUpdates();
                        MultipleRecordUpdates oSecondaryRecord = new MultipleRecordUpdates();
                        //determine which is the primary...
                        if (wireA.Cells["User.WireRole"].get_ResultStr(0) == "P")
                        {
                            //wireA is the priamry
                            oPrimaryRecord = WireUtilities.BuildWireShapeInfo(wireA, wirePairID, false);
                            oSecondaryRecord = WireUtilities.BuildWireShapeInfo(wireB, wirePairID, false);

                        }
                        else
                        {
                            //wireA is the secondary
                            oPrimaryRecord = WireUtilities.BuildWireShapeInfo(wireB, wirePairID, false);
                            oSecondaryRecord = WireUtilities.BuildWireShapeInfo(wireA, wirePairID, false);
                        }

                        //only add them to the db if they don't exist yet..
                        bool bDoesWireRecordExist = DatabaseUtilities.DoesRecordExist(DatabaseUtilities.SqlTables.WireShapesTable.sWireShapeTable, oPrimaryRecord.ruRecords[0].sId);
                        if (!bDoesWireRecordExist)
                        {
                            WireUtilities.AddWireToDatabase(oPrimaryRecord, oSecondaryRecord);
                        }

                    }
                    else
                    {
                        //the mates shape is coming back on a different page...
                        //use the sWirePairID to find the wire elsewhere in the document 
                        foreach (Visio.Page ovPage in ovDocument.Pages)
                        {
                            if (ovPage.PageSheet.CellExists["User.PageID", 0] == -1)
                            {
                                foreach (Visio.Shape ovShape in ovPage.Shapes)
                                {
                                    if (ovShape.CellExists["User.Class", 0] == -1 && ovShape.Cells["User.Class"].get_ResultStr(0) == "SmartWire")
                                    {
                                        string sWirePairIDToCheck = ovShape.Cells["User.WirePairID"].get_ResultStr(0);
                                        if (sWirePairIDToCheck == wirePairID)
                                        {
                                            //make sure we didn't find the shape we already have...

                                            Visio.Shape wireA = pairShapes[0];
                                            string sKey = wireA.Name + "|" + wireA.ContainingPage.Name;
                                            string sKeyToCheck = ovShape.Name + "|" + ovShape.ContainingPage.Name;
                                            if (sKey != sKeyToCheck)
                                            {
                                                Visio.Shape wireB = ovShape;
                                                //this is the wire's mate...
                                                MultipleRecordUpdates oPrimaryRecord = new MultipleRecordUpdates();
                                                MultipleRecordUpdates oSecondaryRecord = new MultipleRecordUpdates();
                                                //determine which is the primary...
                                                if (wireA.Cells["User.WireRole"].get_ResultStr(0) == "P")
                                                {
                                                    //wireA is the priamry
                                                    oPrimaryRecord = WireUtilities.BuildWireShapeInfo(wireA, wirePairID, false);
                                                    oSecondaryRecord = WireUtilities.BuildWireShapeInfo(wireB, wirePairID, false);

                                                }
                                                else
                                                {
                                                    //wireA is the secondary
                                                    oPrimaryRecord = WireUtilities.BuildWireShapeInfo(wireB, wirePairID, false);
                                                    oSecondaryRecord = WireUtilities.BuildWireShapeInfo(wireA, wirePairID, false);
                                                }

                                                //only add them to the db if they don't exist yet..
                                                bool bDoesWireRecordExist = DatabaseUtilities.DoesRecordExist(DatabaseUtilities.SqlTables.WireShapesTable.sWireShapeTable, oPrimaryRecord.ruRecords[0].sId);
                                                if (!bDoesWireRecordExist)
                                                {
                                                    WireUtilities.AddWireToDatabase(oPrimaryRecord, oSecondaryRecord);
                                                }
                                                break;
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


        internal static void GetShapesInTable(string sTableName, Visio.Document ovDocument)
        {
            try
            {
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
                        sqlitecmdCommand.Parameters.AddWithValue("@FileID", sFileID);

                        using (SQLiteDataReader sqlitereadReader = sqlitecmdCommand.ExecuteReader())
                        {
                            while (sqlitereadReader.Read())
                            {
                                RecordUpdate ru = new RecordUpdate();
                                ru.sId = sqlitereadReader["ShapeID"].ToString();
                                ru.sPrimaryKeyColumn = "ShapeID";

                                // Create dictionary for all columns except ShapeID, values as string
                                Dictionary<string, string> oDictColumns = new Dictionary<string, string>();
                                for (int i = 0; i < sqlitereadReader.FieldCount; i++)
                                {
                                    string columnName = sqlitereadReader.GetName(i);
                                    if (columnName == "ShapeID") continue; // skip ShapeID

                                    object value = sqlitereadReader.GetValue(i);
                                    oDictColumns[columnName] = value == DBNull.Value ? "" : value.ToString();
                                }

                                ru.odictColumnValues = oDictColumns;
                                lstRecords.Add(ru);
                            }
                        }
                    }
                }

                m_mruRecordsBase = new MultipleRecordUpdates(lstRecords);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in GetShapesInTable: {ex.Message}", "VisAssist");
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




    }
}
