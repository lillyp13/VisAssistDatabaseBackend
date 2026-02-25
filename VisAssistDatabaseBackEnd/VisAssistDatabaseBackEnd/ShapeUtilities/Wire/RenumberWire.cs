using Microsoft.Office.Interop.Visio;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using VisAssistDatabaseBackEnd.DataUtilities;
using VisAssistDatabaseBackEnd.ShapeUtilities;
using VisAssistDatabaseBackEnd.Forms;
using Visio = Microsoft.Office.Interop.Visio;
using System.Drawing;
using VisAssistDatabaseBackEnd.VisioUtilities;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static VisAssistDatabaseBackEnd.DataUtilities.DatabaseUtilities.SqlTables;
using System.Reflection;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Runtime.Remoting.Messaging;

namespace VisAssistDatabaseBackEnd.ShapeUtilities.Wire
{
    internal class RenumberWire
    {






        internal static void GatherRenumberInfo(ReColororRenumberWiresForm reColororRenumberWiresForm)
        {
            try
            {

                string sRange = reColororRenumberWiresForm.cboRange.Text;
                string sOrder = reColororRenumberWiresForm.cboDirection.Text;

                Dictionary<string, MateSelection> oDictWires = new Dictionary<string, MateSelection>();


                string sPrefix = reColororRenumberWiresForm.txtPrefix.Text;
                string sNumber = reColororRenumberWiresForm.txtNumber.Text;
                if(sNumber.Trim() == "")
                {
                    MessageBox.Show("Please pick the starting number.", "VisAssist");
                }

                if (int.TryParse(sNumber.Trim(), out int iNumber))
                {
                   
                }
                else
                {
                    MessageBox.Show("Please enter a valid integer number.", "VisAssist");
                }
               
                oDictWires = WireUtilities.GatherWires(sRange);
               

                RenumberWires(oDictWires, sOrder, sPrefix, iNumber);



            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in RenumberOrRecolorWires " + ex.Message, "VisAssist");
            }
            finally
            {
                //make sure events are on
                Globals.ThisAddIn.Application.EventsEnabled = -1;
            }
        }


       

        internal static void RenumberWires(Dictionary<string, MateSelection> oDictWires, string sOrder, string sPrefix, int iNumber)
        {
            try
            {

                int iUndoScope = Globals.ThisAddIn.Application.BeginUndoScope("Renumber Wires");
                //Dictionary<string, Visio.Shape> oDictWiresProcessed = new Dictionary<string, Shape>();
                List<string> olstWiresProcessed = new List<string>();
                Visio.Document ovDocument = Globals.ThisAddIn.Application.ActiveDocument;
                string sFileID = ovDocument.DocumentSheet.Cells["User.FileID"].get_ResultStr(0);
               // Dictionary<string, Visio.Shape> oDictSortedShapes = new Dictionary<string, Shape>();
               List<string> lstSortedWireIDs = new List<string>();
                //we have oDictWires and we want to sort them based on the sOrder 


                switch (sOrder)
                {
                    case "Top-Bottom":
                        {
                           
                            //we have a dictionary with all the shapeids we want to order
                            //sql statement to order them IDs by OrderByDescending based on the column YLocation...
                           lstSortedWireIDs = WireUtilities.GetOrderedShapeIDsByYLocation(oDictWires, sFileID, "YLocation", "DESC");
                            break;
                        }
                    case "Bottom-Top":
                        {
                           
                            lstSortedWireIDs = WireUtilities.GetOrderedShapeIDsByYLocation(oDictWires, sFileID, "YLocation", "ASC");
                            break;
                        }
                    case "Left-Right":
                        {
                           
                            lstSortedWireIDs = WireUtilities.GetOrderedShapeIDsByYLocation(oDictWires, sFileID, "XLocation", "ASC");
                            break;
                        }
                    case "Right-Left":
                        {
                           
                            lstSortedWireIDs = WireUtilities.GetOrderedShapeIDsByYLocation(oDictWires, sFileID, "XLocation", "DESC");
                            break;
                        }
                }


                foreach(string sShapeID in lstSortedWireIDs)
                {
                    string sWireLabel = "";
                    if(sPrefix == "")
                    {
                        //dont add the dash
                        sWireLabel = iNumber.ToString();
                    }
                    else
                    {
                        sWireLabel = sPrefix + "-" + iNumber;
                    }
                   
                    if(!olstWiresProcessed.Contains(sShapeID))
                    {
                        //update the wirelabel..
                        //get the shape to update...
                        string sPageID = WireUtilities.GetColumnInfoInWireShapesTableFromDatabase("PageID", sShapeID);
                        //now get the pageindex from the pages_table
                        string sPageIndex = PageUtilities.GetColumnInfoInPagesTableFromDatabase("PageIndex", sPageID);
                        int iPageIndex = Convert.ToInt32(sPageIndex);
                        Visio.Page ovPage = ovDocument.Pages[iPageIndex];
                        foreach(Visio.Shape ovShape in ovPage.Shapes)
                        {
                            if (ovShape.CellExists["User.Class", 0] == -1 && ovShape.Cells["User.Class"].get_ResultStr(0) == "SmartWire")
                            {
                                if (ovShape.Cells["User.ShapeID"].get_ResultStr(0) == sShapeID)
                                {
                                    //this is our shape
                                    ovShape.Application.EventsEnabled = 0;
                                    ovShape.Cells["Prop.WireLabel"].Formula = VisioUtilities.Application.FormatStringForVisio(sWireLabel);
                                    ovShape.Application.EventsEnabled = -1;

                                    WireUtilities.UpdateWireInDatabase(ovShape, false);

                                    olstWiresProcessed.Add(sShapeID);
                                    //now we need to update the mate shape now...
                                    string sMateID = WireUtilities.GetMateID(oDictWires[sShapeID].sWirePairID, oDictWires[sShapeID].sWireRole);
                                    string sMatePageID = WireUtilities.GetColumnInfoInWireShapesTableFromDatabase("PageID", sMateID);
                                    string sMatePageIndex = PageUtilities.GetColumnInfoInPagesTableFromDatabase("PageIndex", sMatePageID);
                                    int iMatePageIndex = Convert.ToInt32(sMatePageIndex);

                                    Visio.Page ovMatePage = ovDocument.Pages[iMatePageIndex];
                                    foreach(Visio.Shape ovMateShape in ovMatePage.Shapes)
                                    {
                                        if (ovMateShape.CellExists["User.Class", 0] == -1 && ovMateShape.Cells["User.Class"].get_ResultStr(0) == "SmartWire")
                                        {
                                            if (ovMateShape.Cells["User.ShapeID"].get_ResultStr(0) == sMateID)
                                            {
                                                //this is the mate shape
                                                ovMateShape.Application.EventsEnabled = 0;
                                                ovMateShape.Cells["Prop.WireLabel"].Formula = VisioUtilities.Application.FormatStringForVisio(sWireLabel);
                                                ovMateShape.Application.EventsEnabled = -1;


                                                WireUtilities.UpdateWireInDatabase(ovMateShape, false);

                                                olstWiresProcessed.Add(sMateID);
                                                break;
                                            }
                                        }
                                    }
                                    iNumber++;
                                    break;

                                }
                            }
                        }
                    }

                }


                SetNextWireNumber(sFileID, iNumber.ToString());



                Globals.ThisAddIn.Application.EndUndoScope(iUndoScope, true);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in RenumberWires " + ex.Message, "VisAssist");
            }
            finally
            {
                //make sure events are on
                Globals.ThisAddIn.Application.EventsEnabled = -1;
            }
        }

        //SQL
        internal static void IncreaseNextWireNumber(string sFileID)
        {
            try
            {
                using (SQLiteConnection sqliteconConnection = new SQLiteConnection(DatabaseConfig.ConnectionString))
                {
                    sqliteconConnection.Open();
                    string sSql = @"UPDATE files_table SET NextWireNumber = CAST(CAST(NextWireNumber AS INT) + 1 AS VARCHAR(50)) WHERE FileID = @FileID";
                    using (SQLiteCommand sqlitecmdCommand = new SQLiteCommand(sSql, sqliteconConnection))
                    {
                        sqlitecmdCommand.Parameters.AddWithValue("@FileID", sFileID);
                        sqlitecmdCommand.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in IncreaseNextWireNumber " + ex.Message, "VisAssist");
            }
        }
        internal static void SetNextWireNumber(string sFileID, string sNextWireNumber)
        {
            try
            {

                using (SQLiteConnection sqliteconConnection = new SQLiteConnection(DatabaseConfig.ConnectionString))
                {
                    sqliteconConnection.Open();
                    string sSql = @"UPDATE files_table SET NextWireNumber = @NextWireNumber WHERE FileID = @FileID";

                    using (SQLiteCommand sqlitecmdCommand = new SQLiteCommand(sSql, sqliteconConnection))
                    {
                        sqlitecmdCommand.Parameters.AddWithValue("@NextWireNumber", sNextWireNumber);
                        sqlitecmdCommand.Parameters.AddWithValue("@FileID", sFileID);

                        sqlitecmdCommand.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in SetNextWireNumber " + ex.Message, "VisAssist");
            }
        }
        internal static void ResetWireNumber(string sFileID)
        {
            try
            {
                SetNextWireNumber(sFileID, 1.ToString());
               
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in ResetWireNumber " + ex.Message, "VisAssist");
            }
        }

        




    }
}
