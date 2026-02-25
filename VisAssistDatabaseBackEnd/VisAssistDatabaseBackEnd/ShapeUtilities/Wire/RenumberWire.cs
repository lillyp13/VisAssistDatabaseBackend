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

                Dictionary<string, Visio.Shape> oDictWires = new Dictionary<string, Shape>();


                string sPrefix = reColororRenumberWiresForm.txtPrefix.Text;
                string sNumber = reColororRenumberWiresForm.txtNumber.Text;

                oDictWires = WireUtilities.GatherWires(sRange);
                int iNumber = Convert.ToInt32(sNumber);
                RenumberWires(oDictWires, sOrder, sPrefix, iNumber);



            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in RenumberOrRecolorWires " + ex.Message, "VisAssist");
            }
        }


       

        internal static void RenumberWires(Dictionary<string, Shape> oDictWires, string sOrder, string sPrefix, int iNumber)
        {
            try
            {


                int iUndoScope = Globals.ThisAddIn.Application.BeginUndoScope("Renumber Wires");
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

                    string sWireLabel = sPrefix + "-" + iNumber;
                    Visio.Shape ovShape = pair.Value;
                    string sKey = ovShape.Name + "|" + ovShape.ContainingPage.Name;
                    if (!oDictWiresProcessed.ContainsKey(sKey))
                    {
                        // update the WireLabel based on the number and prefix given...
                        ovShape.Application.EventsEnabled = 0;
                        ovShape.Cells["Prop.WireLabel"].Formula = VisioUtilities.Application.FormatStringForVisio(sWireLabel);
                        ovShape.Application.EventsEnabled = -1;

                        //now we need to go update the mates wire label as well..
                        string sMateID = WireUtilities.GetMateID(ovShape);

                        Visio.Shape ovMateShape = oDictSortedShapes[sMateID];

                        ovMateShape.Application.EventsEnabled = 0;
                        ovMateShape.Cells["Prop.WireLabel"].Formula = VisioUtilities.Application.FormatStringForVisio(sWireLabel);
                        ovMateShape.Application.EventsEnabled = -1;


                        string sMateKey = ovMateShape.Name + "|" + ovMateShape.ContainingPage.Name;
                        oDictWiresProcessed.Add(sKey, ovShape);
                        oDictWiresProcessed.Add(sMateKey, ovShape);


                        //need to update in the db
                        WireUtilities.UpdateWireInDatabase(ovShape, false);
                        WireUtilities.UpdateWireInDatabase(ovMateShape, false);

                        iNumber++;

                    }
                }

                SetNextWireNumber(sFileID, iNumber.ToString());

              

                Globals.ThisAddIn.Application.EndUndoScope(iUndoScope, true);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in RenumberWires " + ex.Message, "VisAssist");
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
