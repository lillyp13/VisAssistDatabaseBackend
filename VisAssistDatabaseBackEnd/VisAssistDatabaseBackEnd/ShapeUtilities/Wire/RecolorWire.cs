using Microsoft.Office.Interop.Visio;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using VisAssistDatabaseBackEnd.DataUtilities;
using VisAssistDatabaseBackEnd.Forms;
using Visio = Microsoft.Office.Interop.Visio;

namespace VisAssistDatabaseBackEnd.ShapeUtilities.Wire
{
    internal class RecolorWire
    {



        internal static string GetNextColor(string currentColor)
        {
            try
            {

                // Find the index of the current color
                int iIndex = Array.IndexOf(DatabaseUtilities.WireColorOrder, currentColor);

                if (iIndex == -1)
                    iIndex = 0; // default to first color if current not found

                // Move to next color, wrapping around
                int iNextIndex = (iIndex + 1) % DatabaseUtilities.WireColorOrder.Length;

                return DatabaseUtilities.WireColorOrder[iNextIndex];
            }
            catch(Exception ex)
            {
                MessageBox.Show("Error in GetNextColor " + ex.Message, "VisAssist");
            }
            return "";
        }

        internal static void GatherReColorInfo(ReColororRenumberWiresForm reColororRenumberWiresForm)
        {
            try
            {

                string sRange = reColororRenumberWiresForm.cboRange.Text;
                string sOrder = reColororRenumberWiresForm.cboDirection.Text;

                Dictionary<string, Visio.Shape> oDictWires = new Dictionary<string, Shape>();


                string sColor = reColororRenumberWiresForm.cboColor.Text;

                oDictWires = WireUtilities.GatherWires(sRange);


                RecolorWires(oDictWires, sOrder, sColor);


            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in RenumberOrRecolorWires " + ex.Message, "VisAssist");
            }
        }



        internal static string GetAndUpdateNextWireColorFromDB(string sFileID)
        {
            string sRGBFormula = "";
            try
            {
                // int iCurrentIndex = GetNextWireColorIndexFromDB(sFileID);

                int iCurrentIndex = 0;
                string sColor = FileUtilities.GetColumnInfoInFilesTableFromDatabase("NextWireColor", sFileID);

                if (string.IsNullOrWhiteSpace(sColor))
                {
                    iCurrentIndex = 0;
                }


                iCurrentIndex = Array.IndexOf(DatabaseUtilities.WireColorOrder, sColor);

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
        internal static void ResetWireColor(string sFileID)
        {
            try
            {
                SetNextWireColor(sFileID, DatabaseUtilities.ColorMap["Yellow"]);
            }
            catch(Exception ex)
            {
                MessageBox.Show("Error in ResetWireColor " + ex.Message, "VisAssist");
            }
           
        }

        internal static void RecolorWires(Dictionary<string, Visio.Shape> oDictWires, string sOrder, string sColor)
        {
            try
            {


                int iUndoScope = Globals.ThisAddIn.Application.BeginUndoScope("Recolor Wires");
                Dictionary<string, Visio.Shape> oDictWiresProcessed = new Dictionary<string, Shape>();
                Visio.Document ovDocument = Globals.ThisAddIn.Application.ActiveDocument;
                string sFileID = ovDocument.DocumentSheet.Cells["User.FileID"].get_ResultStr(0);
                Dictionary<string, Visio.Shape> oDictSortedShapes = new Dictionary<string, Shape>();
                switch (sOrder)
                {
                    case "Top-Bottom":
                        {
                            oDictSortedShapes = oDictWires.OrderByDescending(pair => pair.Value.Cells["PinY"].ResultIU).ToDictionary(pair => pair.Key, pair => pair.Value);

                            break;
                        }
                    case "Bottom-Top":
                        {
                            oDictSortedShapes = oDictWires.OrderBy(pair => pair.Value.Cells["PinY"].ResultIU).ToDictionary(pair => pair.Key, pair => pair.Value);
                            break;
                        }
                    case "Left-Right":
                        {
                            oDictSortedShapes = oDictWires.OrderBy(pair => pair.Value.Cells["PinX"].ResultIU).ToDictionary(pair => pair.Key, pair => pair.Value);
                            break;
                        }
                    case "Right-Left":
                        {
                            oDictSortedShapes = oDictWires.OrderByDescending(pair => pair.Value.Cells["PinX"].ResultIU).ToDictionary(pair => pair.Key, pair => pair.Value);
                            break;
                        }
                }

                foreach (KeyValuePair<string, Visio.Shape> pair in oDictSortedShapes)
                {
                    Visio.Shape ovShape = pair.Value;
                    string sKey = ovShape.Name + "|" + ovShape.ContainingPage.Name;
                    if (!oDictWiresProcessed.ContainsKey(sKey))
                    {
                        // Get the RGB formula from the ColorMap
                        string sRGBFormula = DatabaseUtilities.ColorMap[sColor];
                        ovShape.Application.EventsEnabled = 0;
                        ovShape.Cells["User.WireColor"].Formula = VisioUtilities.Application.FormatStringForVisio(sRGBFormula);
                        ovShape.Application.EventsEnabled = -1;
                       
                        string sMateID = WireUtilities.GetMateID(ovShape);

                     

                        //Visio.Shape ovMateShape = FindWireByShapeID(sMateID, ovDocument, sKey);
                        //get the mate from sortedDict...
                        Visio.Shape ovMateShape = oDictSortedShapes[sMateID];
                        ovMateShape.Application.EventsEnabled = 0;
                        ovMateShape.Cells["User.WireColor"].Formula = VisioUtilities.Application.FormatStringForVisio(sRGBFormula);
                        ovMateShape.Application.EventsEnabled = -1;
                        //ok now we want to update these wires in the database...

                        WireUtilities.UpdateWireInDatabase(ovShape, false);
                        WireUtilities.UpdateWireInDatabase(ovMateShape, false);


                        string sMateKey = ovMateShape.Name + "|" + ovMateShape.ContainingPage.Name;
                        oDictWiresProcessed.Add(sKey, ovShape);
                        oDictWiresProcessed.Add(sMateKey, ovShape);

                        //we will also need to increase the wire color in the db and for the next wire pair...
                        //set sColor to be the next color in the colormap based on what it is right now...
                        sColor = GetNextColor(sColor);
                    }
                }

                sColor = GetNextColor(sColor);
                //now set the nextwirecolor in the db...
                SetNextWireColor(sFileID, sColor);

                Globals.ThisAddIn.Application.EndUndoScope(iUndoScope, true);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in RecolorWires " + ex.Message, "VisAssist");
            }
        }

        //SQL
        internal static void SetNextWireColor(string sFileID, string sNextWireColor)
        {
            try
            {

                using (SQLiteConnection sqliteconConnection = new SQLiteConnection(DatabaseConfig.ConnectionString))
                {
                    sqliteconConnection.Open();
                    string sSql = @"UPDATE files_table SET NextWireColor = @NextWireColor WHERE FileID = @FileID";

                    using (SQLiteCommand sqlitecmdCommand = new SQLiteCommand(sSql, sqliteconConnection))
                    {
                        sqlitecmdCommand.Parameters.AddWithValue("@NextWireColor", sNextWireColor);
                        sqlitecmdCommand.Parameters.AddWithValue("@FileID", sFileID);

                        sqlitecmdCommand.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in SetNextWireColor " + ex.Message, "VisAssist");
            }
        }
        internal static void UpdateNextWireColor(string sFileID, int iNextIndex)
        {
            try
            {
                string sNextColorName = DatabaseUtilities.WireColorOrder[iNextIndex];

                using (SQLiteConnection conn = new SQLiteConnection(DatabaseConfig.ConnectionString))
                {
                    conn.Open();

                    string sql = "UPDATE files_table SET NextWireColor = @Color WHERE FileID = @Id";

                    using (SQLiteCommand cmd = new SQLiteCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Color", sNextColorName);
                        cmd.Parameters.AddWithValue("@Id", sFileID);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in UpdateNextWireColor " + ex.Message, "VisAssist");
            }
        }


    }
}
