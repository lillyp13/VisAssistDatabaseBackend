using Microsoft.Office.Interop.Visio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Visio = Microsoft.Office.Interop.Visio;
using VisAssistDatabaseBackEnd.DataUtilities;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace VisAssistDatabaseBackEnd.VisioUtilities
{
    internal class Application
    {

        //PAGE LEVEL EVENTS
        internal static void OnPageAdded(Visio.Page ovVisioPage)
        {
            try
            {
                string sVisAssistFolderPath = FileUtilities.GetFolderPath(ovVisioPage.Document);
                DatabaseConfig.BindToActiveDocument(sVisAssistFolderPath);
                string sProjectID = ovVisioPage.Document.DocumentSheet.Cells["User.ProjectID"].get_ResultStr(0);

                //add page user cells...
                bool bAdded = PageUtilities.AddUserCellsToPage(ovVisioPage);
                if (bAdded)
                {
                    //we haven't added the page yet...
                    PageUtilities.AddPageToDatabase(ovVisioPage, sProjectID);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in OnPageAdded " + ex.Message, "VisAssist");
            }

        }

        internal static void OnPageChanged(Visio.Page ovVisioPage)
        {
            try
            {


                string sVisAssistFolderPath = FileUtilities.GetFolderPath(ovVisioPage.Document);
                DatabaseConfig.BindToActiveDocument(sVisAssistFolderPath);
                string sProjectID = ovVisioPage.Document.DocumentSheet.Cells["User.ProjectID"].get_ResultStr(0);

                PageUtilities.UpdatePageInDatabase(ovVisioPage, sProjectID);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in OnPageChanged " + ex.Message, "VisAssist");
            }
        }

        internal static void OnDocumentChanged(Visio.Document ovDocument)
        {
            //this is for page index changed: user has dragged the page and changed the DOCUMENT order
            //it will probably also be used for when a shape has moved (grid location stuff...)
            try
            {
                //is the only time this gets called when the user changes the page index by dragging pages around?
                //should we just update each page in the document?
                string sProjectID = ovDocument.DocumentSheet.Cells["User.ProjectID"].get_ResultStr(0);
                foreach (Visio.Page ovPage in ovDocument.Pages)
                {
                    PageUtilities.UpdatePageInDatabase(ovPage, sProjectID);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in OnDocumentChanged " + ex.Message, "VisAssist");
            }
        }

        internal static void OnPageDeleted(Visio.Page ovPage)
        {
            try
            {
               
                //user is deleting the page
                string sProjectID = ovPage.Document.DocumentSheet.Cells["User.ProjectID"].get_ResultStr(0);
                string sPageID = ovPage.PageSheet.Cells["User.PageID"].get_ResultStr(0);

                //check if the pageID has already been removed from the db...
                bool bRecordExists = DatabaseUtilities.DoesRecordExist(DatabaseUtilities.SqlTables.PagesTable.sPagesTable, sPageID);
                if (bRecordExists)
                {
                    PageUtilities.DeletePageInDatabase(ovPage, sProjectID);

                    //have a delayed event that will call ondocumentchanged...
                    DelayedEvent oDelayedEvent = new DelayedEvent();
                    oDelayedEvent.sOperationType = "OnDocumentChanged";
                    oDelayedEvent.ovDocument = ovPage.Document;
                    Globals.ThisAddIn.m_delayedEvents.Add(oDelayedEvent);

                    
                }


            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in OnPageDeleted " + ex.Message, "VisAssist");
            }
        }


        //SHAPE LEVEL EVENTS
        internal static void OnShapeAdded(Visio.Shape ovShape)
        {
            if (ovShape.CellExists["User.Class", 0] == -1)
            {
                //this is one of our shapes...
                string sVisAssistFolderPath = FileUtilities.GetFolderPath(ovShape.ContainingPage.Document);
                DatabaseConfig.BindToActiveDocument(sVisAssistFolderPath);
                string sProjectID = ovShape.ContainingPage.Document.DocumentSheet.Cells["User.ProjectID"].get_ResultStr(0);

                string sClass = ovShape.Cells["User.Class"].get_ResultStr(0);
                switch(sClass)
                {
                    case "NewWire":
                    case "SmartWire":
                        {
                            ShapesUtilities.AddWireToDatabase(ovShape);
                            break;
                        }
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
                }
                

            }
        }
        internal static void OnShapeDeleted(Visio.Shape ovShape)
        {

            if (ovShape.CellExists["User.Class", 0] == -1)
            {
                //this is one of our shapes...
                string sVisAssistFolderPath = FileUtilities.GetFolderPath(ovShape.ContainingPage.Document);
                DatabaseConfig.BindToActiveDocument(sVisAssistFolderPath);
                string sProjectID = ovShape.ContainingPage.Document.DocumentSheet.Cells["User.ProjectID"].get_ResultStr(0);

                string sClass = ovShape.Cells["User.Class"].get_ResultStr(0);
                switch (sClass)
                {
                    case "NewWire":
                    case "SmartWire":
                        {
                            ShapesUtilities.DeleteWireFromDatabase(ovShape);
                            break;
                        }
                    case "TerminalBlock":
                        {
                            ShapesUtilities.DeleteTerminalBlockFromDatabase(ovShape);
                            break;
                        }
                    case "ADC End Device":
                        {
                            ShapesUtilities.DeleteEndDeviceFromDatabase(ovShape);
                            break;
                        }
                }


            }
        }



        //DOCUMENT LEVEL EVENTS
        internal static void ProcessThisDelayedEvent(DelayedEvent oThisDelayedEvent)
        {

            try
            {
                if (oThisDelayedEvent.sOperationType == "OnDocumentChanged")
                {
                    Visio.Document ovDocument = oThisDelayedEvent.ovDocument;
                    OnDocumentChanged(ovDocument);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in processThisDelayedEvnet " + ex.Message, "VisAssist");
            }

        }

        internal static void OnVisioIsIdle(Visio.Application subject)
        {

            int iNumberOfDelayedEvents = Globals.ThisAddIn.m_delayedEvents.Count;

            if (iNumberOfDelayedEvents > 0)
            {


                try
                {

                    for (int ithEvent = iNumberOfDelayedEvents; ithEvent > 0; ithEvent--)
                    {
                        DelayedEvent thisDelayedEvent = Globals.ThisAddIn.m_delayedEvents[ithEvent - 1];
                        ProcessThisDelayedEvent(thisDelayedEvent);
                        Globals.ThisAddIn.m_delayedEvents.Remove(thisDelayedEvent);
                    }

                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error in OnVisioIsIdle " + ex.Message, "VisAssist");
                }
            }
        }

        internal static void VisioApplication_CellChanged(Visio.Cell ovCell)
        {
            //check what cell was changed...
            //if it was the x or y recalculate the grid location...
            string sCellName = ovCell.Name;
            if (sCellName == "PinX" || sCellName == "PinY")
            {
                //this is movement....
                //what shape is the cell apart of ?
                Visio.Shape ovShape = ovCell.Shape;
                if (ovShape.CellExists["User.Class", 0] == -1)
                {
                    //this is one of our shapes
                    string sClass = ovShape.Cells["User.Class"].get_ResultStr(0);

                    switch(sClass)
                    {
                        case "TerminalBlock":
                            {
                               ShapesUtilities.UpdateTerminalBlockInDatabase(ovShape);

                                break;
                            }
                        case "SmartWire":
                            {
                                break;
                            }
                        case "ADC End Device":
                            {
                                break;
                            }
                    }
                }
            }
        }

        //HELPER FUNCTION

        /// <summary>
        /// This function is used to properly format a string for use in a Visio cell or a SQL string.
        /// Note: This version pads " characters to each end of the string.
        static public string FormatStringForVisio(
            string sInputString)
        {
            // return result
            const string SINGLE_QUOTE = "\"";
            const string DOUBLE_QUOTES = "\"\"";

            if (string.IsNullOrEmpty(sInputString))
                return "\"\"";   // Visio empty string literal

            // replace each " char with double "" chars
            sInputString = sInputString.Replace(SINGLE_QUOTE, DOUBLE_QUOTES);
            sInputString = "\"" + sInputString + "\"";

            // return result
            return sInputString;
        }
    }
}
