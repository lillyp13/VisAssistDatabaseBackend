using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Windows.Forms;
using VisAssistDatabaseBackEnd.DataUtilities;
using VisAssistDatabaseBackEnd.Forms;
using VisAssistDatabaseBackEnd.VisioUtilities;
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

                Dictionary<string, MateSelection> odictWires = new Dictionary<string, MateSelection>();


                string sColor = reColororRenumberWiresForm.cboColor.Text;

                odictWires = WireUtilities.GatherWires(sRange);


                RecolorWires(odictWires, sOrder, sColor);


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

        internal static void RecolorWires(Dictionary<string, MateSelection> odictWires, string sOrder, string sColor)
        {
            try
            {


                int iUndoScope = Globals.ThisAddIn.Application.BeginUndoScope("Recolor Wires");
                List<string> olstWiresProcessed = new List<string>();
                List<string> olstSortedWireIDs = new List<string>();
                Visio.Document ovDocument = Globals.ThisAddIn.Application.ActiveDocument;
                string sFileID = ovDocument.DocumentSheet.Cells["User.FileID"].get_ResultStr(0);
              


                switch (sOrder)
                {
                    case "Top-Bottom":
                        {
                            olstSortedWireIDs = WireUtilities.GetOrderedShapeIDsByYLocation(odictWires, sFileID, "YLocation", "DESC");
                            break;
                        }
                    case "Bottom-Top":
                        {
                            olstSortedWireIDs = WireUtilities.GetOrderedShapeIDsByYLocation(odictWires, sFileID, "YLocation", "ASC");
                            break;
                        }
                    case "Left-Right":
                        {
                            olstSortedWireIDs = WireUtilities.GetOrderedShapeIDsByYLocation(odictWires, sFileID, "XLocation", "ASC");
                            break;
                        }
                    case "Right-Left":
                        {
                            olstSortedWireIDs = WireUtilities.GetOrderedShapeIDsByYLocation(odictWires, sFileID, "XLocation", "DESC");
                            break;
                        }
                }



                foreach (string sShapeID in olstSortedWireIDs)
                {
                    
                    if (!olstWiresProcessed.Contains(sShapeID))
                    {
                        //update the wirelabel..
                        //get the shape to update...
                        string sPageID = WireUtilities.GetColumnInfoInWireShapesTableFromDatabase("PageID", sShapeID);
                        //now get the pageindex from the pages_table
                        string sPageIndex = PageUtilities.GetColumnInfoInPagesTableFromDatabase("PageIndex", sPageID);
                        int iPageIndex = Convert.ToInt32(sPageIndex);
                        Visio.Page ovPage = ovDocument.Pages[iPageIndex];
                        foreach (Visio.Shape ovShape in ovPage.Shapes)
                        {
                            if (ovShape.CellExists["User.Class", 0] == -1 && ovShape.Cells["User.Class"].get_ResultStr(0) == "SmartWire")
                            {
                                if (ovShape.Cells["User.ShapeID"].get_ResultStr(0) == sShapeID)
                                {
                                    string sRGBFormula = DatabaseUtilities.ColorMap[sColor];
                                    //this is our shape
                                    ovShape.Application.EventsEnabled = 0;
                                    ovShape.Cells["User.WireColor"].Formula = VisioUtilities.Application.FormatStringForVisio(sRGBFormula);
                                    ovShape.Application.EventsEnabled = -1;

                                    WireUtilities.UpdateWireInDatabase(ovShape, false);

                                    olstWiresProcessed.Add(sShapeID);
                                    //now we need to update the mate shape now...
                                    string sMateID = WireMateUtilities.GetMateID(odictWires[sShapeID].sWirePairID, odictWires[sShapeID].sWireRole);
                                    string sMatePageID = WireUtilities.GetColumnInfoInWireShapesTableFromDatabase("PageID", sMateID);
                                    string sMatePageIndex = PageUtilities.GetColumnInfoInPagesTableFromDatabase("PageIndex", sMatePageID);
                                    int iMatePageIndex = Convert.ToInt32(sMatePageIndex);

                                    Visio.Page ovMatePage = ovDocument.Pages[iMatePageIndex];
                                    foreach (Visio.Shape ovMateShape in ovMatePage.Shapes)
                                    {
                                        if (ovMateShape.CellExists["User.Class", 0] == -1 && ovMateShape.Cells["User.Class"].get_ResultStr(0) == "SmartWire")
                                        {
                                            if (ovMateShape.Cells["User.ShapeID"].get_ResultStr(0) == sMateID)
                                            {
                                                //this is the mate shape
                                                ovMateShape.Application.EventsEnabled = 0;
                                                ovMateShape.Cells["User.WireColor"].Formula = VisioUtilities.Application.FormatStringForVisio(sRGBFormula);
                                                ovMateShape.Application.EventsEnabled = -1;


                                                WireUtilities.UpdateWireInDatabase(ovMateShape, false);

                                                olstWiresProcessed.Add(sMateID);
                                                break;
                                            }
                                        }
                                    }
                                    sColor = GetNextColor(sColor);
                                    break;

                                }
                            }
                        }
                    }

                }

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
