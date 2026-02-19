using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Visio = Microsoft.Office.Interop.Visio;
using Office = Microsoft.Office.Core;
using System.Data.SQLite;
using System.IO;
using VisAssistDatabaseBackEnd.DataUtilities;
using VisAssistDatabaseBackEnd.VisioUtilities;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Microsoft.Office.Interop.Visio;
using static System.Net.Mime.MediaTypeNames;
using System.Diagnostics;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Reflection;
using System.Net;
using VisAssistDatabaseBackEnd.ShapeUtilities;
using System.Globalization;
using WindowsAPICodePack.Dialogs.Controls;

namespace VisAssistDatabaseBackEnd
{
    public partial class ThisAddIn
    {
        public VisioEventSink m_appSink;
        public List<Visio.Event> m_VisioEvents = new List<Visio.Event>();
        public List<Visio.Event> m_VisioAppEvents = new List<Visio.Event>();
        public List<DelayedEvent> m_delayedEvents = new List<DelayedEvent>();
        public List<string> m_pendingShapeIds = new List<string>();
        public List<string> m_pendingPageIds = new List<string>();
        public Dictionary<string, MateSelection> m_MatesInSelection = new Dictionary<string, MateSelection>();
        public List<string> m_MatesMated = new List<string>();
        public List<string> m_MatesDeleted = new List<string>();
        public bool m_bIsPageDuplicating = false;
        public bool m_SyncedDB = false;
        public bool m_bAskWhereToCutTo = true;
        public Visio.Selection m_ovSelection;
        public string m_sLastUndoScope = "";
        //public bool m_bIsCuttingShape = false;
        public bool m_bIsCuttingShape { get; set; }

        private void ThisAddIn_Startup(object sender, System.EventArgs e)
        {
            //add the higher level events...
            VisioEvents_Connect();

            foreach (Visio.Document doc in this.Application.Documents)
            {
                if (doc.Type == Visio.VisDocumentTypes.visTypeDrawing)
                {
                    VisAssistDatabaseBackEnd.VisioUtilities.VisioHelper.OnDocumentOpenedCreated(doc, false);


                }
            }
        }

        protected override Microsoft.Office.Core.IRibbonExtensibility CreateRibbonExtensibilityObject()
        {
            return new Ribbonxml();
        }

        public const short visEvtAdd = -0x8000;
        internal void StartSinksForDoc(Visio.Document ovDocument)
        {
            try
            {
                // Ensure a single sink instance
                if (m_appSink == null)
                {
                    m_appSink = new VisioEventSink(OnVisioEvent);
                }

                // Get document-level and application-level event lists
                Visio.EventList docEventList = ovDocument.EventList;
                Visio.EventList appEventList = this.Application.EventList;

                // ---- Document-level events ----

                short pageAddedEventCode = unchecked((short)((short)Visio.VisEventCodes.visEvtAdd | (short)Visio.VisEventCodes.visEvtPage));

                // Hook the event on the document's Pages collection
                Visio.Event pageAddedEvent = ovDocument.EventList.AddAdvise(
                    pageAddedEventCode,
                    m_appSink,
                    string.Empty,
                    string.Empty
                );

                // Add to your list to prevent GC
                m_VisioEvents.Add(pageAddedEvent);


                //// Shape Added
                m_VisioEvents.Add(docEventList.AddAdvise(
                     (short)((short)visEvtAdd + (short)Visio.VisEventCodes.visEvtShape),
                    m_appSink,
                    string.Empty,
                    string.Empty));


                ////text changed
                /// //// Shape Added
                m_VisioEvents.Add(
                docEventList.AddAdvise(
                (short)Visio.VisEventCodes.visEvtCodeShapeExitTextEdit,
                m_appSink,
                string.Empty,
                string.Empty));



                // Shape Deleted
                m_VisioEvents.Add(docEventList.AddAdvise(
                   (short)((short)Visio.VisEventCodes.visEvtDel + (short)Visio.VisEventCodes.visEvtShape),
                    m_appSink,
                    string.Empty,
                    string.Empty));


                // Page Changed
                Visio.Event pagechangedAddedEvent = ovDocument.EventList.AddAdvise(
                     (short)((short)Visio.VisEventCodes.visEvtMod + (short)Visio.VisEventCodes.visEvtPage),
                    m_appSink,
                    string.Empty,
                    string.Empty
                    );

                m_VisioEvents.Add(pagechangedAddedEvent);


                short pageModifiedEventCode = unchecked((short)((short)Visio.VisEventCodes.visEvtMod | (short)Visio.VisEventCodes.visEvtPage));

                Visio.Event pageModifiedEvent = docEventList.AddAdvise(
                    pageModifiedEventCode,
                    m_appSink,
                    string.Empty,
                    string.Empty
                );

                m_VisioEvents.Add(pageModifiedEvent);


                ////Event Marker
                //m_VisioEvents.Add(
                //docEventList.AddAdvise(
                //(short)Visio.VisEventCodes.visEvtMarker, // just marker
                //m_appSink,
                //"",  // moreInformation string
                //""   // target object, usually empty for document-level marker
                //));


                // Page Added
                //m_appEvents.Add(docEventList.AddAdvise(
                //(short)((short)visEvtAdd + (short)Visio.VisEventCodes.visEvtPage),
                //m_appSink,
                //string.Empty,
                //string.Empty));
                // ✅ Correct
                //m_VisioEvents.Add(docEventList.AddAdvise(
                //    (short)((short)visEvtAdd | (short)Visio.VisEventCodes.visEvtPage),
                //    m_appSink,
                //    string.Empty,
                //    string.Empty));

                // Correct PageAdded event

                // Check if we already registered this event with this sink
                //Visio.Event evt3 = docEventList.AddAdvise(
                //   (short)((short)Visio.VisEventCodes.visEvtPage + (short)VisioEvent.visEvtAdd),
                //   m_appSink,
                //   string.Empty,
                //   string.Empty);
                //m_VisioEvents.Add(evt3);



                // Cell Changed possibly for movement as well as information change....
                var evtCellModified = docEventList.AddAdvise(
                    (short)((short)Visio.VisEventCodes.visEvtCell + (short)Visio.VisEventCodes.visEvtMod),
                    m_appSink,
                    string.Empty,
                    string.Empty);
                m_VisioEvents.Add(evtCellModified);


                // Hook CellChanged event




                //// Connections Added
                //m_appEvents.Add(docEventList.AddAdvise(
                //    (short)(visEvtAdd + (short)Visio.VisEventCodes.visEvtConnect),
                //    m_appSink,
                //    string.Empty,
                //    string.Empty));

                //// Connections Deleted
                //m_appEvents.Add(docEventList.AddAdvise(
                //    (short)((short)Visio.VisEventCodes.visEvtDel + (short)Visio.VisEventCodes.visEvtConnect),
                //    m_appSink,
                //    string.Empty,
                //    string.Empty));

                //// Shape Link Added
                //m_appEvents.Add(docEventList.AddAdvise(
                //    (short)Visio.VisEventCodes.visEvtShapeLinkAdded,
                //    m_appSink,
                //    string.Empty,
                //    string.Empty));

                //// Document Deleted (Before Close)
                //m_appEvents.Add(docEventList.AddAdvise(
                //     (short)((short)Visio.VisEventCodes.visEvtDel + (short)Visio.VisEventCodes.visEvtDoc),
                //    m_appSink,
                //    string.Empty,
                //    string.Empty));




                // Page Deleted
                m_VisioEvents.Add(docEventList.AddAdvise(
                   (short)((short)Visio.VisEventCodes.visEvtDel + (short)Visio.VisEventCodes.visEvtPage),
                    m_appSink,
                    string.Empty,
                    string.Empty));

                //// Query Cancel Page Delete
                //m_appEvents.Add(docEventList.AddAdvise(
                //    (short)Visio.VisEventCodes.visEvtCodeQueryCancelPageDel,
                //    m_appSink,
                //    string.Empty,
                //    string.Empty));

                //// Document Modified
                m_VisioEvents.Add(docEventList.AddAdvise(
                    (short)(short)Visio.VisEventCodes.visEvtDoc + (short)Visio.VisEventCodes.visEvtMod,
                    m_appSink,
                    string.Empty,
                    string.Empty));

                //// ---- Application-level events ----

                //// Visio Is Idle
                m_VisioAppEvents.Add(appEventList.AddAdvise(
                    (short)(short)Visio.VisEventCodes.visEvtApp + (short)Visio.VisEventCodes.visEvtIdle,
                    m_appSink,
                    string.Empty,
                    string.Empty));

                //// No Events Pending
                //m_appEvents.Add(appEventList.AddAdvise(
                //    (short)(short)Visio.VisEventCodes.visEvtApp + (short)Visio.VisEventCodes.visEvtNonePending,
                //    m_appSink,
                //    string.Empty,
                //    string.Empty));



            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in StartSinksForDoc: " + ex.Message, "VisAssist");
            }
        }



        public void VisioEvents_Connect()
        {
            var app = this.Application;
            var eventList = app.EventList;

            m_appSink = new VisioEventSink(OnVisioEvent);

            ////event marker
            m_VisioAppEvents.Add(
                eventList.AddAdvise(
                    (short)(Visio.VisEventCodes.visEvtApp | Visio.VisEventCodes.visEvtMarker),
                    m_appSink,
                    "",
                    "")
            );

            //query cancel
            m_VisioAppEvents.Add(
                eventList.AddAdvise(
                    (short)Visio.VisEventCodes.visEvtCodeQueryCancelSelDel,
                    m_appSink,
                    "",
                    "")
            );
            //this is the docuemnt opened
            m_VisioAppEvents.Add(
                eventList.AddAdvise(
                    (short)Visio.VisEventCodes.visEvtCodeDocOpen,
                    m_appSink,
                    "",
                    "")
            );


            //after modal
            m_VisioAppEvents.Add(eventList.AddAdvise(
              (short)((short)Visio.VisEventCodes.visEvtApp + (short)Visio.VisEventCodes.visEvtAfterModal),
               m_appSink,
               string.Empty,
               string.Empty));


        }



        private static volatile bool m_visioIsIdle = false;
        public Dictionary<string, Visio.Shape> oDictWiresComingFromRedo = new Dictionary<string, Shape>();
        private bool OnVisioEvent(
    short eventCode,
    object source,
    int eventID,
    int eventSequenceNumber,
    object subject,
    object moreInformation)
        {
            //var code = (Visio.VisEventCodes)eventCode;

            //if ((code & Visio.VisEventCodes.visEvtMarker) != 0)
            //{
            //    // Marker hit
            //    return true;
            //}

            //if (code == Visio.VisEventCodes.visEvtCodeDocOpen)
            //{
            //    var doc = subject as Visio.Document;
            //    if (doc != null)
            //    {
            //        // visio is already open and we are now going to opne a doucment...
            //        VisAssistDatabaseBackEnd.DataUtilities.VisioHelper.OnDocumentOpenedCreated(doc, false);
            //    }
            //    // Document opened
            //    return true;
            //}

            //return true;


            bool bCancelEvent = false;

            switch (eventCode)
            {
                //ShapeAdded -32704
                case (short)((short)visEvtAdd + (short)Visio.VisEventCodes.visEvtShape):
                    {
                        Visio.Shape ovShape = (Visio.Shape)subject;
                        if (ovShape != null)
                        {
                            if (ovShape.ID != 0)
                            {
                                //if the shape id is 0 that means we deleted it...
                                string sKey = ovShape.ID + "|" + ovShape.ContainingPage.Name;


                                if (m_ovSelection == null)
                                {
                                    m_ovSelection = Application.ActiveWindow.Selection;
                                }



                                if (!m_pendingShapeIds.Contains(sKey))
                                {
                                    //check to see if the mate was in our selection (we need to delete it becuasae we have alrady dropped a new mate for the shape..)
                                    if (m_MatesInSelection.ContainsKey(sKey))
                                    {
                                        //we want to delete this shape...

                                        //ovShape.Application.EventsEnabled = 0;
                                        //ovShape.Delete();
                                        //ovShape.Application.EventsEnabled = -1;
                                        break;
                                    }
                                    //we only care if we are cutting a wire shape...

                                    if (!m_bIsCuttingShape)
                                    {
                                        if (!m_bIsPageDuplicating)
                                        {
                                            //check to see if mate is in this selection and therefore should add to m_pendingShapeIds because we dont' want to drop another shape for it...
                                            Visio.Shape ovMateShape = null;
                                            string sMateKey = WireUtilities.IsMateInSelection(m_ovSelection, ovShape, out ovMateShape);
                                            if (sMateKey != "")
                                            {
                                                MateSelection oMateSelection = new MateSelection();
                                                oMateSelection.sMateID = sMateKey;
                                                oMateSelection.sShapeID = sKey;
                                                oMateSelection.ovShape = ovShape;
                                                oMateSelection.ovMateShape = ovMateShape;
                                                m_MatesInSelection.Add(sKey, oMateSelection);
                                            }

                                            VisAssistDatabaseBackEnd.VisioUtilities.Application.OnShapeAdded(ovShape, m_ovSelection, ref oDictWiresComingFromRedo);
                                            if (oDictWiresComingFromRedo.Count > 0)
                                            {
                                                //will need a delayed event to add these wires back to the db
                                                bool bDelayedEventAlreadyExists = Globals.ThisAddIn.m_delayedEvents.Any(e => e.sOperationType == "AddWiresToDB");
                                                if (!bDelayedEventAlreadyExists)
                                                {
                                                    DelayedEvent oNewDelayedEvent = new DelayedEvent();
                                                    oNewDelayedEvent.sOperationType = "AddWiresToDB";
                                                    oNewDelayedEvent.ovDocument = ovShape.Document;
                                                    oNewDelayedEvent.oDictOfShapes = oDictWiresComingFromRedo;
                                                    Globals.ThisAddIn.m_delayedEvents.Add(oNewDelayedEvent);
                                                }

                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (ovShape.CellExists["User.Class", 0] == -1)
                                        {
                                            string sClass = ovShape.Cells["User.Class"].get_ResultStr(0);
                                            if (sClass == "SmartWire")
                                            {
                                                //add a delayed event that will switch the duplicate bool to be false..
                                                DelayedEvent oDelayedEvent = new DelayedEvent();
                                                oDelayedEvent.ovDocument = ovShape.Document;
                                                oDelayedEvent.sOperationType = "TurnOfCutShapesBool";
                                                Globals.ThisAddIn.m_delayedEvents.Add(oDelayedEvent);

                                                VisAssistDatabaseBackEnd.VisioUtilities.Application.OnWireShapeCut(ovShape);

                                            }
                                            else
                                            {
                                                //we are cutting/pasting a non wire shape, add it normally..

                                                VisAssistDatabaseBackEnd.VisioUtilities.Application.OnShapeAdded(ovShape, m_ovSelection, ref oDictWiresComingFromRedo);

                                            }
                                        }


                                    }



                                    m_pendingShapeIds.Add(sKey);
                                }
                            }
                        }

                        break;
                    }

                //ShapeDeleted 16448
                case (short)((short)Visio.VisEventCodes.visEvtDel + (short)Visio.VisEventCodes.visEvtShape):
                    {
                        Visio.Shape ovShape = (Visio.Shape)subject;
                        string sKey = ovShape.ID + "|" + ovShape.ContainingPage.Name;
                        Visio.Selection ovSelection = null;

                        //get the selection in beforeshapedeleted...
                        if (Application.ActiveWindow != null)
                        {
                            ovSelection = Application.ActiveWindow.Selection;
                        }

                        //check the selection for wires...
                        bool bSelectionContainsWires = false;
                        foreach (Visio.Shape ovShapeToCheck in ovSelection)
                        {
                            if (ovShape.CellExists["User.Class", 0] == -1)
                            {
                                if (ovShape.Cells["User.Class"].get_ResultStr(0) == "SmartWire")
                                {
                                    //there is a wire in the selection 
                                    bSelectionContainsWires = true;
                                }
                            }
                        }

                        if (!m_pendingShapeIds.Contains(sKey))
                        {
                            //check if we are doing an undo
                            if (!Application.IsUndoingOrRedoing)
                            {
                                //we are not undoing, check if this is a cut
                                if (Clipboard.ContainsData(DataFormats.EnhancedMetafile))
                                {
                                    m_bIsCuttingShape = true;
                                    bool bDoesClipboardContainWire = ClipboardContainsWireShape();
                                    if (bDoesClipboardContainWire)
                                    {
                                        //there is a wire in this movement
                                        //pull open the form for the user to choose where to paste the selection...
                                        foreach (Visio.Shape ovShapeToCheck in ovSelection)
                                        {
                                            //for all other shapes besides wires we want to call OnShapeDeleted first..
                                            if (ovShapeToCheck.CellExists["User.Class", 0] == -1)
                                            {
                                                if (ovShapeToCheck.Cells["User.Class"].get_ResultStr(0) != "SmartWire")
                                                {
                                                    VisioUtilities.Application.OnShapeDeleted(ovShapeToCheck, ovSelection);
                                                }
                                            }
                                        }

                                        if (m_bAskWhereToCutTo)
                                        {
                                            int iUndoScope = ovSelection.Application.BeginUndoScope("Cut Shape");
                                            ShapesUtilities.CutShapes(ovSelection);
                                            ovSelection.Application.EndUndoScope(iUndoScope, true);
                                        }

                                    }
                                    else
                                    {
                                        //there are no wires in this cut..
                                        m_bIsCuttingShape = false;
                                        VisioUtilities.Application.OnShapeDeleted(ovShape, ovSelection);
                                    }


                                }
                                else
                                {
                                    //we are not doing an unod and we are not cutting
                                    VisAssistDatabaseBackEnd.VisioUtilities.Application.OnShapeDeleted(ovShape, ovSelection);
                                }
                            }



                            else
                            {
                                if (!m_MatesDeleted.Contains(sKey))
                                {
                                    //if we haven't already processed the shape and its mate...
                                    if (m_sLastUndoScope != "")
                                    {
                                        //make sure this is not one of our stress tests
                                        if (m_sLastUndoScope != "Stress Test")
                                        {


                                            //we are doing an undo
                                            //this is part of an undo/redo event...
                                            List<string> lstShapesRemoved = VisAssistDatabaseBackEnd.VisioUtilities.Application.OnShapeDeletedRedoing((Visio.Shape)subject, ovSelection);



                                            bool bUndoEvent = false;
                                            //check if we should add this event by looking at the db and seeing if we have any wires in visio with no mate in visio 
                                            if (lstShapesRemoved.Count > 0)
                                            {
                                                //we removed wires from the db
                                                foreach (string sShapeID in lstShapesRemoved)
                                                {
                                                    //we removed a wire...
                                                    bUndoEvent = WireUtilities.CheckForWireMate(sShapeID);
                                                    if (bUndoEvent)
                                                    {
                                                        bool bUndoAlreadyScheduled = Globals.ThisAddIn.m_delayedEvents.Any(ev => ev.sOperationType == "Undo");
                                                        if (!bUndoAlreadyScheduled)
                                                        {
                                                            //add delayed event to undo the cut...
                                                            DelayedEvent oDelayedEvent = new DelayedEvent();
                                                            oDelayedEvent.sOperationType = "Undo";
                                                            Globals.ThisAddIn.m_delayedEvents.Add(oDelayedEvent);
                                                        }

                                                    }
                                                }

                                            }
                                            else
                                            {
                                                //we didn't remove any wires from the db (doing an undo/redo of a cut/move)
                                                string sShapeID = ovShape.Cells["User.ShapeID"].get_ResultStr(0);
                                                bUndoEvent = WireUtilities.CheckForWireMate(sShapeID);
                                                if (bUndoEvent)
                                                {
                                                    bool bUndoAlreadyScheduled = Globals.ThisAddIn.m_delayedEvents.Any(ev => ev.sOperationType == "Undo");
                                                    if (!bUndoAlreadyScheduled)
                                                    {
                                                        //add delayed event to undo the cut...
                                                        DelayedEvent oDelayedEvent = new DelayedEvent();
                                                        oDelayedEvent.sOperationType = "Undo";
                                                        Globals.ThisAddIn.m_delayedEvents.Add(oDelayedEvent);
                                                    }
                                                }
                                            }

                                        }
                                        //  m_sLastUndoScope = "";
                                    }
                                    else
                                    {
                                        //we are doing a redo


                                        List<string> lstShapesRemoved = VisAssistDatabaseBackEnd.VisioUtilities.Application.OnShapeDeletedRedoing((Visio.Shape)subject, ovSelection);


                                        bool bUndoEvent = false;
                                        //check if we should add this event by looking at the db and seeing if we have any wires in visio with no mate in visio 
                                        if (lstShapesRemoved.Count > 0)
                                        {
                                            //we removed wires from the db
                                            foreach (string sShapeID in lstShapesRemoved)
                                            {
                                                //we removed a wire...
                                                bUndoEvent = WireUtilities.CheckForWireMate(sShapeID);
                                                if (bUndoEvent)
                                                {
                                                   
                                                    bool bUndoAlreadyScheduled = Globals.ThisAddIn.m_delayedEvents.Any(ev => ev.sOperationType == "Redo");
                                                    if (!bUndoAlreadyScheduled)
                                                    {
                                                        //add delayed event to undo the cut...
                                                        DelayedEvent oDelayedEvent = new DelayedEvent();
                                                        oDelayedEvent.sOperationType = "Redo";
                                                        oDelayedEvent.ovDocument = Globals.ThisAddIn.Application.ActiveDocument;
                                                        Globals.ThisAddIn.m_delayedEvents.Add(oDelayedEvent);
                                                    }

                                                }
                                            }

                                        }
                                        else
                                        {
                                            //we didn't remove any wires from the db (doing an undo/redo of a cut/move)
                                            string sShapeID = ovShape.Cells["User.ShapeID"].get_ResultStr(0);
                                            bUndoEvent = WireUtilities.CheckForWireMate(sShapeID);
                                            if (bUndoEvent)
                                            {
                                                bool bRedoAlreadyScheduled = Globals.ThisAddIn.m_delayedEvents.Any(ev => ev.sOperationType == "Redo");
                                                if (!bRedoAlreadyScheduled)
                                                {
                                                    //add delayed event to undo the cut...
                                                    DelayedEvent oDelayedEvent = new DelayedEvent();
                                                    oDelayedEvent.sOperationType = "Redo";
                                                    oDelayedEvent.ovDocument = Globals.ThisAddIn.Application.ActiveDocument;
                                                    Globals.ThisAddIn.m_delayedEvents.Add(oDelayedEvent);
                                                }
                                            }
                                        }

                                        m_sLastUndoScope = "Cut and Past Action";

                                    }



                                }


                            }

                            m_pendingShapeIds.Add(sKey);
                        }

                        break;
                    }

                //PageDeleted
                case (short)((short)Visio.VisEventCodes.visEvtDel + (short)Visio.VisEventCodes.visEvtPage):
                    {
                        if (!Globals.ThisAddIn.Application.IsUndoingOrRedoing)
                        {
                            VisAssistDatabaseBackEnd.VisioUtilities.Application.OnPageDeleted((Visio.Page)subject);
                        }
                        else
                        {
                            Visio.Page ovPage = ((Visio.Page)subject);
                            Visio.Document ovDocument = ovPage.Document;
                            //this should be a delayed event because i want it to happen after visio gets rid of the pages in the undo...
                            DelayedEvent oDelayedEvent = new DelayedEvent();
                            oDelayedEvent.ovDocument = ovDocument;
                            oDelayedEvent.sOperationType = "CheckPageExistence";
                            Globals.ThisAddIn.m_delayedEvents.Add(oDelayedEvent);

                        }

                        break;
                    }

                //PageAdded -32752
                case (short)((short)VisioEvents.visEvtAdd + (short)Visio.VisEventCodes.visEvtPage):
                    {
                        Visio.Page ovPage = (Visio.Page)subject;

                        string sKey = ovPage.ID + "|" + ovPage.Name;
                        if (!m_pendingPageIds.Contains(sKey))
                        {
                            //if the page has shapes on it already this is a duplicate..
                            if (ovPage.PageSheet.CellExists["User.PageID", 0] == -1)
                            {
                                m_bIsPageDuplicating = true; //we are duplicating a page...


                            }
                            else
                            {
                                //we aren't duplicating a page
                                m_bIsPageDuplicating = false;
                            }
                            //if this is a duplicate then we are going to set the pagesheets formula first...
                            // ovPage.PageSheet.Cells["User.PageID"].Formula = VisioUtilities.Application.FormatStringForVisio("LillY");
                            if (m_bIsPageDuplicating)
                            {
                                if (!Application.IsUndoingOrRedoing)
                                {
                                    //we are duplicating a page...
                                    Dictionary<string, Visio.Page> oDictPagesToDuplicate = new Dictionary<string, Visio.Page>();
                                    oDictPagesToDuplicate.Add(ovPage.Name, ovPage);
                                    VisAssistDatabaseBackEnd.VisioUtilities.Application.OnPageDuplicated(oDictPagesToDuplicate);
                                }
                                else
                                {
                                    //we are doing a redo/undo..
                                    //need to update the pageids...

                                    VisioUtilities.Application.OnPageAdded(ovPage);
                                    //this could be a redo of pagduplicated...
                                    //add a delayed event that will switch the duplicate bool to be false..
                                    DelayedEvent oDelayedEvent = new DelayedEvent();
                                    oDelayedEvent.ovDocument = ovPage.Document;
                                    oDelayedEvent.sOperationType = "TurnOffDuplicateBool";
                                    Globals.ThisAddIn.m_delayedEvents.Add(oDelayedEvent);


                                }
                            }
                            else
                            {
                                //we are just adding a page
                                VisAssistDatabaseBackEnd.VisioUtilities.Application.OnPageAdded(ovPage);
                            }

                            m_pendingPageIds.Add(sKey);

                        }


                        break;
                    }

                //Cell Changed / modified 10240
                case (short)((short)Visio.VisEventCodes.visEvtCell + (short)Visio.VisEventCodes.visEvtMod):
                    {
                        Visio.Cell ovCell = ((Visio.Cell)subject);
                        Visio.Shape ovShape = ovCell.Shape;
                        if (ovShape != null)
                        {
                            if (ovShape.ContainingPage != null)
                            {
                                string sKey = ovShape.ID + "|" + ovShape.ContainingPage.Name;

                                if (!m_pendingShapeIds.Contains(sKey))
                                {
                                    if (m_bIsPageDuplicating)
                                    {

                                        break;
                                    }

                                    if (Globals.ThisAddIn.Application.IsUndoingOrRedoing)
                                    {
                                        break;
                                    }
                                    //we are not in the middle of adding a shape..
                                    VisAssistDatabaseBackEnd.VisioUtilities.Application.CellChanged((Visio.Cell)subject);
                                    m_pendingShapeIds.Add(sKey);
                                }
                            }
                        }

                        break;
                    }

                //Text Changed 
                case (short)Visio.VisEventCodes.visEvtCodeShapeExitTextEdit:
                    {
                        Visio.Shape ovShape = (Visio.Shape)subject;
                        VisAssistDatabaseBackEnd.VisioUtilities.Application.TextChanged(ovShape);
                        //OnShapeTextChanged(shape);
                        break;
                    }


                //page changed / modified 8208
                case (short)((short)VisEventCodes.visEvtMod + (short)Visio.VisEventCodes.visEvtPage):
                    {
                        if (!m_bIsPageDuplicating)
                        {
                            VisAssistDatabaseBackEnd.VisioUtilities.Application.OnPageChanged((Visio.Page)subject);
                        }
                        break;
                    }

                //after modal
                case (short)Visio.VisEventCodes.visEvtApp + (short)Visio.VisEventCodes.visEvtAfterModal:
                    {
                        m_bIsPageDuplicating = false;
                        break;
                    }

                //ConnectionsAdded - 32512
                case (short)(((short)visEvtAdd + (short)Visio.VisEventCodes.visEvtConnect)):
                    {

                        //OnConnectionsAddded((Visio.Connects)subject);
                        break;
                    }

                //ConnectionsDeleted 16640
                case (short)((short)Visio.VisEventCodes.visEvtDel + (short)Visio.VisEventCodes.visEvtConnect):
                    {
                        //OnConnectionsDeleted((Visio.Connects)subject);
                        break;
                    }

                //LinkAdded Event


                //BeforeDocumentClose 16386
                case (short)((short)Visio.VisEventCodes.visEvtDel + (short)Visio.VisEventCodes.visEvtDoc):
                    {

                        // OnBeforeDocumentClosed((Visio.Document)subject);

                        break;
                    }


                //QueryCancelPageDelete 500
                case (short)((short)Visio.VisEventCodes.visEvtCodeQueryCancelPageDel):
                    {
                        // bCancelEvent = OnCheckBeforePageDeleted((Visio.Page)subject);
                        //OnBeforePageDelete((Visio.Page)subject);

                        break;
                    }

                //NoEventsPending 4608
                case (short)(short)Visio.VisEventCodes.visEvtApp + (short)Visio.VisEventCodes.visEvtNonePending:
                    {
                        // OnNoEventsPending((Visio.Application)subject);
                        break;
                    }

                //IsIdle 5120
                case (short)(short)Visio.VisEventCodes.visEvtApp + (short)Visio.VisEventCodes.visEvtIdle:
                    {
                        //VisAssistDatabaseBackEnd.DataUtilities.Application.OnVisioIsIdle((Visio.Application)subject);
                        VisAssistDatabaseBackEnd.VisioUtilities.Application.OnVisioIsIdle((Visio.Application)subject);
                        break;
                    }


                case (short)(short)Visio.VisEventCodes.visEvtDoc + (short)Visio.VisEventCodes.visEvtMod:
                    {
                        if (!m_bIsPageDuplicating)
                        {
                            VisAssistDatabaseBackEnd.VisioUtilities.Application.OnDocumentChanged((Visio.Document)subject);
                        }

                        break;
                    }



                case (short)(short)Visio.VisEventCodes.visEvtApp + (short)Visio.VisEventCodes.visEvtMarker:
                    {

                        Visio.Application ovapplication = (Visio.Application)subject;

                        if (ovapplication.ActiveWindow != null && ovapplication.ActiveWindow.Selection.Count > 0)
                        {
                            Visio.Shape ovShape = ovapplication.ActiveWindow.Selection[1];
                            if (ovShape.CellExists["User.Class", 0] == -1)
                            {
                                if (ovShape.Cells["User.Class"].get_ResultStr(0) == "SmartWire")
                                {
                                    WireUtilities.JumpToMate(ovShape);
                                }
                            }
                        }
                        //Visio.Application ovApplication = Globals.ThisAddIn.Application;

                        //string sContextString = (string)moreInformation;

                        //VisAssistDatabaseBackEnd.VisioUtilities.VisioHelper.VisioApplication_MarkerEvent(ovApplication, eventSequenceNumber, sContextString);

                        break;
                    }
                case (short)Visio.VisEventCodes.visEvtCodeQueryCancelSelDel:
                    {
                        // Visio.Selection ovSelection = (Visio.Selection)subject;
                        // VisioApplication_SelectionDeleted(ovSelection);

                        break;
                    }
                case (short)Visio.VisEventCodes.visEvtCodeDocOpen:
                    {
                        var doc = subject as Visio.Document;
                        if (doc != null)
                        {
                            // visio is already open and we are now going to opne a doucment...
                            VisAssistDatabaseBackEnd.VisioUtilities.VisioHelper.OnDocumentOpenedCreated(doc, false);
                        }
                        //OnDocumentOpened((Visio.Document)subject);
                        break;
                    }

                //we don't do anything on ondocumentcreated
                //case (short)Visio.VisEventCodes.visEvtCodeDocCreate:
                //    {
                //        OnDocumentCreated((Visio.Document)subject);
                //        break;
                //    }

                default:
                    {
                        break;
                    }


            }
            return bCancelEvent;
        }

        private bool ClipboardContainsWireShape()
        {
            //turn off events for this..

            Visio.Application app = Globals.ThisAddIn.Application;
            app.EventsEnabled = 0;
            Visio.Page activePage = app.ActivePage;

            if (!Clipboard.ContainsData(DataFormats.EnhancedMetafile) &&
                !Clipboard.ContainsData(DataFormats.Text)) // adjust formats
                return false;

            int undoScope = activePage.Application.BeginUndoScope("CheckClipboardWire");



            try
            {
                activePage.Paste();
                Visio.Selection ovSelection = activePage.Application.ActiveWindow.Selection;
                foreach (Visio.Shape ovShape in ovSelection)
                {
                    // Detect wire by Master name or User cell
                    if ((ovShape.CellExistsU["User.Class", 0] == -1 && ovShape.Cells["User.Class"].get_ResultStr(0) == "SmartWire"))
                    {
                        return true;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
            finally
            {
                activePage.Application.EndUndoScope(undoScope, false);
                app.EventsEnabled = -1;
            }
        }


        private void ThisAddIn_Shutdown(object sender, System.EventArgs e)
        {
        }




        #region VSTO generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InternalStartup()
        {
            this.Startup += new System.EventHandler(ThisAddIn_Startup);
            this.Shutdown += new System.EventHandler(ThisAddIn_Shutdown);
        }

        #endregion
    }
}
