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
using VisAssistDatabaseBackEnd.Forms;
using VisAssistDatabaseBackEnd.ShapeUtilities;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Visio = Microsoft.Office.Interop.Visio;

namespace VisAssistDatabaseBackEnd.ShapeUtilities
{
    internal class ShapesUtilities
    { 


        public static MultipleRecordUpdates m_mruRecordsBase = new MultipleRecordUpdates();
        public static MultipleRecordUpdates m_mruRecordsToCompare = new MultipleRecordUpdates();
        public static MultipleRecordUpdates m_mruRecordsToUpdate = new MultipleRecordUpdates();

        
      

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
                                    //THESE ARE ADDED BEOFRE THIS FUNCTION GETS CALLED SO NOTHING TO DO HERE...
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

        internal static void CutShapes(Selection ovSelection)
        {
            string sAction = "Move";
            PagesForm oNewForm = new PagesForm();
            oNewForm.Display(sAction);
            oNewForm.Show();
        }
    }
}
