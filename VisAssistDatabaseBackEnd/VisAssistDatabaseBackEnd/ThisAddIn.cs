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
using VisAssistDatabaseBackEnd.ShapeUtilities.Wire;
using System.Globalization;
using WindowsAPICodePack.Dialogs.Controls;
using Microsoft.Office.Tools;

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
        public bool m_bIsPageDuplicating = false;
        public bool m_SyncedDB = false;
        public bool m_bAskWhereToCutTo = true;
        public Visio.Selection m_ovSelection;
        public string m_sLastUndoScope = "";
        public List<string> m_lstWireIDs = new List<string>();
        public List<string> m_lstWirePairIDs = new List<string>();
        public List<string> m_lstPagesinProcessofDeleting = new List<string>();
        public bool m_bSuppressEvents = false;
        //public bool m_bIsCuttingShape = false;
        public bool m_bIsCuttingShape { get; set; }

        private void ThisAddIn_Startup(object sender, System.EventArgs e)
        {
            //add the higher level events...
            //Visio.UIObject visioAppMenusUI = Application.CustomMenus;
            //CustomizeAccelSet(visioAppMenusUI, Visio.VisUIObjSets.visUIObjSetDrawing);
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
        // Store a reference to your UIObject
        private Visio.UIObject myCustomCutCommand;
        private static void CustomizeDrawingMenu(Visio.UIObject visioUIObject, Visio.VisUIObjSets visUIObjSetDrawing)
        {
            //I'm leaving this here so that if we do want to ever add something to the rma this is already built in...
            if (visioUIObject == null)
            {
                return;
            }
            try
            {
                Visio.MenuSet ovThisMenuSet = visioUIObject.MenuSets[10];

                //this adds to all shapes...
                Microsoft.Office.Interop.Visio.MenuItem ovThisMenuItem = (Microsoft.Office.Interop.Visio.MenuItem)ovThisMenuSet.Menus[0].MenuItems.AddAt(0);
                ovThisMenuItem.Caption = "VisAssist Cut Shapes";
                ovThisMenuItem.ActionText = "VisAssist Cut Shapes";
                ovThisMenuItem.AddOnName = "QueueMarkerEvent";
                ovThisMenuItem.AddOnArgs = "/action=MoveShapes";
                //"/app=VisAssist /action=move_these_shapes";
                ovThisMenuItem.FaceID = 125;


            }
            catch (Exception ex)
            {

            }
        }

        internal void StartSinksForDoc(Visio.Document ovDocument)
        {
            try
            {

                Visio.UIObject visioAppMenusUI = Application.BuiltInMenus;

                CustomizeAccelSet(visioAppMenusUI, Visio.VisUIObjSets.visUIObjSetDrawing);

                //CustomizeDrawingMenu(visioAppMenusUI, Visio.VisUIObjSets.visUIObjSetDrawing);
                Application.SetCustomMenus(visioAppMenusUI);
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


                //querycancelselectiondelete
                short eventCode = (short)Visio.VisEventCodes.visEvtCodeQueryCancelSelDel;

                Visio.Event deleteEvent = ovDocument.EventList.AddAdvise(
                    eventCode,
                    m_appSink,
                    "",
                    ""
                );

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

                // before document close
                //m_VisioEvents.Add(docEventList.AddAdvise(
                //     (short)((short)Visio.VisEventCodes.visEvtDel + (short)Visio.VisEventCodes.visEvtDoc),
                //    m_appSink,
                //    string.Empty,
                //    string.Empty));


                //before document save
                m_VisioEvents.Add(docEventList.AddAdvise(
                     (short)((short)Visio.VisEventCodes.visEvtCodeBefDocSave),
                    m_appSink,
                    string.Empty,
                    string.Empty));

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


            bool bCancelEvent = false;


            switch (eventCode)
            {


                //ShapeAdded -32704
                case (short)((short)visEvtAdd + (short)Visio.VisEventCodes.visEvtShape):
                    {
                        if (!m_bSuppressEvents)
                        {
                            if (!m_bIsPageDuplicating)
                            {
                                Visio.Shape ovShape = (Visio.Shape)subject;
                                if (ovShape != null)
                                {
                                    if (ovShape.ID != 0)
                                    {
                                        //if the shape id is 0 that means we deleted it...
                                        string sKey = ovShape.ID + "|" + ovShape.ContainingPage.Name;
                                        Visio.Document ovDocument = ovShape.Document;
                                        string sProjectID = ovDocument.DocumentSheet.Cells["User.ProjectID"].get_ResultStr(0);
                                        string sFileID = ovDocument.DocumentSheet.Cells["User.FileID"].get_ResultStr(0);
                                        string sPageID = ovShape.ContainingPage.PageSheet.Cells["User.PageID"].get_ResultStr(0);

                                        if (m_ovSelection == null)
                                        {
                                            m_ovSelection = Application.ActiveWindow.Selection;
                                        }



                                        if (!m_pendingShapeIds.Contains(sKey))
                                        {
                                            //check if we ahve already done the action of mating the shape
                                            if (!m_MatesMated.Contains(sKey))
                                            {


                                                //check to see if the mate was in our selection (we need to delete it becuasae we have alrady dropped a new mate for the shape..)
                                                if (m_MatesInSelection.ContainsKey(sKey))
                                                {
                                                    //we need to update their ids...
                                                    MateSelection oMateSelection = m_MatesInSelection[sKey];
                                                    ovShape.Application.EventsEnabled = 0;
                                                    string sOtherNewShapeID = ShapesUtilities.GenerateShapeID(sProjectID, sFileID, sPageID, ovShape.Name, DateTime.Now);
                                                    ovShape.Cells["User.ShapeID"].Formula = VisioUtilities.Application.FormatStringForVisio(sOtherNewShapeID);
                                                    ovShape.Cells["User.WirePairID"].Formula = VisioUtilities.Application.FormatStringForVisio(oMateSelection.sWirePairID);
                                                    ovShape.Application.EventsEnabled = -1;
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
                                                            //the mate is in the selection add it to m_pendingshapeids so we don't add another wire for it
                                                            MateSelection oMateSelection = new MateSelection();
                                                            oMateSelection.ovMateShape = ovMateShape;
                                                            oMateSelection.sWirePairID = ovShape.Cells["User.WirePairID"].get_ResultStr(0);
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

                                                    //i think this is still live code, i beleive it gets called when we paste?
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
                                            }


                                            m_pendingShapeIds.Add(sKey);
                                        }
                                    }
                                }
                            }
                        }
                        break;

                    }

                case (short)((short)Visio.VisEventCodes.visEvtDel + (short)Visio.VisEventCodes.visEvtShape):
                    {
                        if (!m_bSuppressEvents)
                        {


                            Visio.Shape ovShape = (Visio.Shape)subject;
                            string sKey = ovShape.ID + "|" + ovShape.ContainingPage.Name;
                            Visio.Selection ovSelection = null;


                            //gather information from the shape so we don't have to pass it around
                            Visio.Document ovDocument = ovShape.Document;
                            string sClass = "";
                            string sShapeID = "";
                            string sWireRole = "";
                            if (ovShape.CellExists["User.Class", 0] == -1)
                            {
                                sClass = ovShape.Cells["User.Class"].get_ResultStr(0);
                            }
                            if (ovShape.CellExists["User.ShapeID", 0] == -1)
                            {
                                sShapeID = ovShape.Cells["User.ShapeID"].get_ResultStr(0);
                            }
                            if (sClass == "SmartWire")
                            {
                                sWireRole = ovShape.Cells["User.WireRole"].get_ResultStr(0);
                            }

                            //get the selection in beforeshapedeleted...
                            if (m_ovSelection == null)
                            {
                                m_ovSelection = Application.ActiveWindow.Selection;
                            }
                            //add a list of all the shapeids in the selection

                            //check the selection for wires...
                            bool bSelectionContainsWires = false;

                            if (!m_pendingShapeIds.Contains(sKey))
                            {
                                //check if we are doing an undo
                                if (!Application.IsUndoingOrRedoing)
                                {
                                    if (m_lstWireIDs.Count == 0)
                                    {
                                        foreach (Visio.Shape ovShapeToCheck in m_ovSelection)
                                        {
                                            if (ovShapeToCheck.CellExists["User.Class", 0] == -1)
                                            {
                                                if (ovShapeToCheck.Cells["User.Class"].get_ResultStr(0) == "SmartWire")
                                                {
                                                    //there is a wire in the selection 
                                                    bSelectionContainsWires = true;
                                                    string sWireID = ovShapeToCheck.Cells["User.ShapeID"].get_ResultStr(0);
                                                    m_lstWireIDs.Add(sWireID);
                                                    m_lstWirePairIDs.Add(ovShape.Cells["User.WirePairID"].get_ResultStr(0));
                                                }
                                            }
                                        }
                                    }
                                    //we are not undoing, check if this is a cut
                                    if (Clipboard.ContainsData(DataFormats.EnhancedMetafile))
                                    {
                                        m_bIsCuttingShape = true;
                                        bool bDoesClipboardContainWire = ClipboardContainsWireShape();
                                        if (bDoesClipboardContainWire)
                                        {
                                            //there is a wire in this movement
                                            //pull open the form for the user to choose where to paste the selection...
                                            foreach (Visio.Shape ovShapeToCheck in m_ovSelection)
                                            {
                                                //for all other shapes besides wires we want to call OnShapeDeleted first..
                                                if (ovShapeToCheck.CellExists["User.Class", 0] == -1)
                                                {
                                                    if (ovShapeToCheck.Cells["User.Class"].get_ResultStr(0) != "SmartWire")
                                                    {
                                                        VisioUtilities.Application.OnShapeDeleted(sShapeID, sWireRole, ovDocument, sClass);
                                                    }
                                                }
                                            }



                                            //THIS SHOULD ALL BE DEAD CODE NOW....
                                            //if (m_bAskWhereToCutTo)
                                            //{
                                            //    //int iUndoScope = ovSelection.Application.BeginUndoScope("Cut Shape");
                                            //    //ShapesUtilities.CutShapes(ovSelection);
                                            //    //ovSelection.Application.EndUndoScope(iUndoScope, true);


                                            //    //make the user use our move shapes.
                                            //    //ovDocument.Application.Undo();
                                            //    List<string> selectedShapeIDs = new List<string>();
                                            //    string sShapeKey = "";


                                            //    foreach (Visio.Shape shape in m_ovSelection)
                                            //    {
                                            //        sShapeKey = shape.ID.ToString();
                                            //        selectedShapeIDs.Add(sShapeKey); // Store Shape.ID (NOT ShapeID cell)
                                            //    }


                                            //    MessageBox.Show("Please use our Move Shapes Ribbon button", "VisAssist");
                                            //    bool bAlreadyQueued = Globals.ThisAddIn.m_delayedEvents.Any(e => e.ovDocument == ovDocument && e.sOperationType == "UndoCut");
                                            //    if (!bAlreadyQueued)
                                            //    {
                                            //        DelayedEvent oDelayeEvent = new DelayedEvent();
                                            //        oDelayeEvent.sOperationType = "UndoCut";
                                            //        oDelayeEvent.ovDocument = ovDocument;
                                            //        oDelayeEvent.lstShapes = selectedShapeIDs;
                                            //        Globals.ThisAddIn.m_delayedEvents.Add(oDelayeEvent);
                                            //    }
                                            //    m_bSuppressEvents = true;
                                            //    break;

                                            //}

                                        }
                                        else
                                        {
                                            //there are no wires in this cut..
                                            m_bIsCuttingShape = false;
                                            VisioUtilities.Application.OnShapeDeleted(sShapeID, sWireRole, ovDocument, sClass);
                                        }


                                    }
                                    else
                                    {
                                        //we are not doing an unod and we are not cutting
                                        VisAssistDatabaseBackEnd.VisioUtilities.Application.OnShapeDeleted(sShapeID, sWireRole, ovDocument, sClass);
                                    }
                                }



                                else
                                {
                                    //we are undoing or redoing...
                                    bool bAlreadyQueued = Globals.ThisAddIn.m_delayedEvents.Any(e => e.ovDocument == ovDocument && e.sOperationType == "CheckShapeExistenceAfterUndoDelete");
                                    if (!bAlreadyQueued)
                                    {
                                        DelayedEvent oDelayedEvent = new DelayedEvent();
                                        oDelayedEvent.ovDocument = ovDocument;
                                        oDelayedEvent.sOperationType = "CheckShapeExistenceAfterUndoDelete";
                                        Globals.ThisAddIn.m_delayedEvents.Add(oDelayedEvent);
                                    }



                                }

                                m_pendingShapeIds.Add(sKey);
                            }

                        }
                        break;
                    }



                //querycancelseldel
                case (short)Visio.VisEventCodes.visEvtCodeQueryCancelSelDel:
                    {
                        try
                        {
                            //check if the current scop eis 1023 and if it is stop this because we are doing a delete shape not cut.. (may want to just check if the scope is 1020 i think that is a cut...
                            if (Application.CurrentScope != 1023)
                            {




                                Visio.Selection ovSelection = (Visio.Selection)subject;

                                //only do this if it is not a visassist project..
                                //i thought this wouldn't get called because sinks shouldn't be set up for a non visassist doc but i think if a doc is open i sink or soemthing like that...
                                //maybe add precautions here to stop it from happening if this is a non visassist doc
                                Visio.Document ovDoc = ovSelection.Application.ActiveDocument;
                                if (ovDoc.DocumentSheet.CellExists["User.ProjectID", 0] == -1)
                                {
                                    //for now i am going to do this, but i think i want to check the manifest file...

                                    if (ovSelection == null || ovSelection.Count == 0)
                                        return false; // nothing selected, allow delete

                                    // Loop through selected shapes
                                    for (short i = 1; i <= ovSelection.Count; i++)
                                    {
                                        Visio.Shape ovShape = ovSelection[i];

                                        // Check if shape has User.Class
                                        if (ovShape.CellExists["User.Class", 0] == -1)
                                        {
                                            string userClass = ovShape.Cells["User.Class"].get_ResultStr(0);

                                            if (userClass == "SmartWire")
                                            {
                                                // Found a wire → cancel deletion
                                                //System.Windows.Forms.MessageBox.Show(
                                                //    "Please use VisAssist Move Shapes"
                                                //);

                                                DelayedEvent oDelayedEvent = new DelayedEvent();
                                                oDelayedEvent.ovDocument = ovSelection.Document;
                                                oDelayedEvent.ovSelection = ovSelection;
                                                oDelayedEvent.sOperationType = "MoveShapes";
                                                m_delayedEvents.Add(oDelayedEvent);

                                                return true; // CANCEL DELETE
                                            }
                                        }
                                    }
                                }
                            }
                            return false; // allow delete

                        }
                        catch
                        {
                            return false; // fail safe: allow delete
                        }
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
                            string sPageToDeleteFromUndo = ovPage.Name;
                            if(!m_lstPagesinProcessofDeleting.Contains(sPageToDeleteFromUndo))
                            {
                                m_lstPagesinProcessofDeleting.Add(sPageToDeleteFromUndo);
                            }
                            

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

                        try
                        {
                            string sPageName = ovPage.Name;
                            if(m_lstPagesinProcessofDeleting.Contains(sPageName))
                            {
                                //this page is actually in the process of getting deleted from an undo/redo
                                break;
                            }
                        }
                        catch
                        {
                            //page doesn't actually exist...
                            break;
                        }


                        Visio.Application ovApp = ovPage.Application;
                        //if the current scope is 2383 this is duplicate...
                        if (Application.CurrentScope != 2383)
                        {



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

                                    //THIS SHOULD BE DEAD CODE BECAUSE WE CHECK IF WE ARE DUPLICATING A PAGE EARLIER (THE CODE)...
                                    if (!Application.IsUndoingOrRedoing)
                                    {
                                        //we are duplicating a page...
                                        Dictionary<string, Visio.Page> oDictPagesToDuplicate = new Dictionary<string, Visio.Page>();
                                        oDictPagesToDuplicate.Add(ovPage.Name, ovPage);
                                        //int iUndoScope = ovPage.Application.BeginUndoScope("Duplicate");
                                        VisAssistDatabaseBackEnd.VisioUtilities.Application.OnPageDuplicated(oDictPagesToDuplicate);
                                        //ovPage.Application.EndUndoScope(iUndoScope, true);
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

                       }
                        else
                        {

                            m_bIsPageDuplicating = true;
                            //we need to turn off events and delete this page and get the page that the user wants to duplicate....
                           

                            //need to see if there are wires on the page where their mates live on a different page, if this is true we want to call whatpagestoduplicate otherwise this is just duplicating one page...
                            Dictionary<string, Visio.Page> oDictPagesToDuplicate = WireUtilities.DoesPageContainWireMates(ovPage);
                            if(oDictPagesToDuplicate.Count > 0)
                            {
                               // int iUndoScope = ovApp.BeginUndoScope("Duplicate Pages");
                                string sPageID = ovPage.PageSheet.Cells["User.PageID"].get_ResultStr(0);
                                //this is the page id that the user wanted to copy...
                                int iUndoScope = ovApp.BeginUndoScope("Duplicate Pages");
                                ovPage.Application.EventsEnabled = 0;
                                ovPage.Delete(1);
                                ovPage.Application.EventsEnabled = -1;

                                //we want to get the new page to duplicate based on the page id...
                                //ok now we want to call our duplicate...
                                Visio.Page ovPageToDuplicate = null;
                                foreach (Visio.Page ovPossiblePage in ovApp.ActiveDocument.Pages)
                                {
                                    if (ovPossiblePage.PageSheet.CellExists["User.PageID", 0] == -1)
                                    {
                                        if (ovPossiblePage.PageSheet.Cells["User.PageID"].get_ResultStr(0) == sPageID)
                                        {
                                            ovPageToDuplicate = ovPossiblePage;
                                        }
                                    }
                                }
                                if(!oDictPagesToDuplicate.ContainsKey(ovPageToDuplicate.Name))
                                {
                                    oDictPagesToDuplicate.Add(ovPageToDuplicate.Name, ovPageToDuplicate);
                                }
                               

                                PageUtilities.DuplicateMultiplePages(oDictPagesToDuplicate);
                                //PageUtilities.WhatPagesToDuplicate();
                                ovApp.EndUndoScope(iUndoScope, true);
                            }
                            else
                            {
                                int iUndoScope = ovApp.BeginUndoScope("Duplicate Page");
                                string sPageID = ovPage.PageSheet.Cells["User.PageID"].get_ResultStr(0);
                                //this is the page id that the user wanted to copy...
                                ovPage.Application.EventsEnabled = 0;
                                ovPage.Delete(1);
                                ovPage.Application.EventsEnabled = -1;

                                //we want to get the new page to duplicate based on the page id...
                                //ok now we want to call our duplicate...
                                Visio.Page ovPageToDuplicate = null;
                                foreach (Visio.Page ovPossiblePage in ovApp.ActiveDocument.Pages)
                                {
                                    if (ovPossiblePage.PageSheet.CellExists["User.PageID", 0] == -1)
                                    {
                                        if (ovPossiblePage.PageSheet.Cells["User.PageID"].get_ResultStr(0) == sPageID)
                                        {
                                            ovPageToDuplicate = ovPossiblePage;
                                        }
                                    }
                                }
                                //turn events off when duplicating the page..
                                ovApp.Application.EventsEnabled = 0;
                                Visio.Page ovNewPage = ovPageToDuplicate.Duplicate();
                                ovApp.Application.EventsEnabled = -1;
                                Dictionary<string, Visio.Page> oDictPageToDuplicate = new Dictionary<string, Visio.Page>();
                                oDictPageToDuplicate.Add(ovNewPage.Name, ovNewPage);
                                VisAssistDatabaseBackEnd.VisioUtilities.Application.OnPageDuplicated(oDictPageToDuplicate);
                                ovApp.EndUndoScope(iUndoScope, true);
                            }
                           

                            
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

                                    //if (Globals.ThisAddIn.Application.IsUndoingOrRedoing)
                                    //{
                                    //    break;
                                    //}
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

                       // VisioUtilities.Application.OnBeforeDocumentClosed((Visio.Document)subject);

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

                case (short)((short)Visio.VisEventCodes.visEvtApp + (short)Visio.VisEventCodes.visEvtIdle):
                    {
                        VisioUtilities.Application.OnVisioIsIdle((Visio.Application)subject);
                        break;
                    }

                //before document save
                case (short)(short)Visio.VisEventCodes.visEvtCodeBefDocSave:
                    {
                        VisioUtilities.Application.OnBeforeDocumentSave((Visio.Document)subject);
                        break;
                    }

                case (short)(short)Visio.VisEventCodes.visEvtDoc + (short)Visio.VisEventCodes.visEvtMod:
                    {
                        //if the current scop eis 2383 this is a duplicate page...
                        Visio.Document ovDoc = (Visio.Document)subject;
                        if (ovDoc.Application.CurrentScope != 2383)
                        {


                            if (!m_bIsPageDuplicating)
                            {
                                VisAssistDatabaseBackEnd.VisioUtilities.Application.OnDocumentChanged((Visio.Document)subject);
                            }
                        }

                        break;
                    }



                case (short)(short)Visio.VisEventCodes.visEvtApp + (short)Visio.VisEventCodes.visEvtMarker:
                    {
                        Visio.Application ovApplication = Globals.ThisAddIn.Application;
                        Visio.Document visioDocument = null;
                        Visio.Page visioPage = null;
                        Visio.Shape visioShape = null;
                        string appArgValue = null;
                        string actionArgValue = null;
                        string sPartNumber = "";
                        string sNewManufacturer = "";
                        string[] additionalArgs = null;

                        string eventInfoFromVisio = ovApplication.get_EventInfo((int)Visio.VisEventCodes.visEvtIdMostRecent);

                        if (eventInfoFromVisio != null &&
                   eventInfoFromVisio.Length > 0)
                        {


                            VisioUtilities.Application.GetVisioObjectsFromAddonString(
                       eventInfoFromVisio,
                       ovApplication,
                       out visioDocument,
                       out visioPage,
                       out visioShape,
                       out appArgValue,
                       out actionArgValue,
                       out sPartNumber,
                       out sNewManufacturer,
                       out additionalArgs);


                            VisioUtilities.Application.PerformAction(additionalArgs, subject);


                        }



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

        private static void CustomizeAccelSet(
            Visio.UIObject visioUIObject,
            Visio.VisUIObjSets uiObjSet)
        {


            if (visioUIObject == null)
            {
                return;
            }
            try
            {
                Visio.AccelTable ovThisTable = visioUIObject.AccelTables[0];

                int iNumOfAcell = ovThisTable.AccelItems.Count;
                //Ctrl+shift+C = Center  -- old one was textHAlignCenter
                //Ctrl+shift+M = Middle  -- old one was 
                //Ctrl+shift+R = Right   -- old one was TextHAlignRight
                //Ctrl+shift+L = Left    -- old one was TextHAlignLeft
                //Ctrl+shift+T = Top     --old one was TextAlignTop
                //Ctrl+shift+B = Bottom  -- old one was "SendToBack


                for (int i = iNumOfAcell - 1; i > 0; i--)
                {
                    Visio.AccelItem thisItem = ovThisTable.AccelItems[i];



                    // CTRL + SHIFT + X  (uppercase X)
                    Visio.AccelItem ctrlShiftX = ovThisTable.AccelItems.Add();
                    ctrlShiftX.Key = 88;     // X
                    ctrlShiftX.Shift = -1;   // shift
                    ctrlShiftX.Control = -1; // ctrl
                    ctrlShiftX.CmdNum = 0;
                    ctrlShiftX.AddOnName = "QueueMarkerEvent";
                    ctrlShiftX.AddOnArgs = "/action=MoveShapes";

                    //Visio.AccelItem newItem = ovThisTable.AccelItems.Add();

                    //newItem.Key = 88;      // X
                    //newItem.Shift = -1;    // Shift
                    //newItem.Control = -1;  // Ctrl

                    //newItem.CmdNum = 0;    // IMPORTANT: disable built-in command
                    //newItem.AddOnName = "QueueMarkerEvent";
                    //newItem.AddOnArgs = "/action=MoveShapes";



















                    //109 = m, 77 = M
                    //98 = b, 66 = B - change out b, and use z
                    //108 = l, 76 = L
                    //99 = c, 67 = C
                    //114 = r, 82 = R
                    //116 = t, 84 = T
                    //122 = z, 90 = Z

                    //120 = x 88 = X

                    //test for x
                    //if ((thisItem.Key == (short)120) && (thisItem.Control == -1) && (thisItem.Shift == 0))
                    //{
                    //    thisItem.CmdNum = (short)Visio.VisUICmds.visCmdDistributeVSpace;
                    //    //Ribbonxml.Instance?.btnMoveShapes_Click(null);
                    //    // var cmdID = Globals.Ribbons.GetRibbon.Tab.Controls["btnMoveShapes"].Id;
                    //    // (short)Visio.VisUICmds.visCmdDistributeVSpace;
                    //}
                    ////test for X = 88
                    //if ((thisItem.Key == (short)88) && (thisItem.Control == -1) && (thisItem.Shift == 0))
                    //{
                    //    thisItem.CmdNum = (short)Visio.VisUICmds.visCmdDistributeVSpace;            // (short)Visio.VisUICmds.visCmdAlignObjectMiddle;
                    //}



                    ////test for v = 118
                    //if ((thisItem.Key == (short)118) && (thisItem.Control == -1) && (thisItem.Shift == -1))
                    //{
                    //    thisItem.CmdNum = (short)Visio.VisUICmds.visCmdDistributeVSpace;
                    //}
                    ////test for V = 86
                    //if ((thisItem.Key == (short)86) && (thisItem.Control == -1) && (thisItem.Shift == -1))
                    //{
                    //    thisItem.CmdNum = (short)Visio.VisUICmds.visCmdDistributeVSpace;            // (short)Visio.VisUICmds.visCmdAlignObjectMiddle;
                    //}


                    ////m and M
                    //if ((thisItem.Key == (short)109) && (thisItem.Control == -1) && (thisItem.Shift == -1))
                    //{
                    //    thisItem.CmdNum = (short)Visio.VisUICmds.visCmdAlignObjectMiddle;

                    //}
                    //if ((thisItem.Key == (short)77) && (thisItem.Control == -1) && (thisItem.Shift == -1))
                    //{
                    //    thisItem.CmdNum = (short)Visio.VisUICmds.visCmdAlignObjectMiddle;

                    //}


                    ////l and L
                    //if ((thisItem.Key == (short)108) && (thisItem.Control == -1) && (thisItem.Shift == -1))
                    //{
                    //    thisItem.CmdNum = (short)Visio.VisUICmds.visCmdAlignObjectLeft;
                    //}
                    //if ((thisItem.Key == (short)76) && (thisItem.Control == -1) && (thisItem.Shift == -1))
                    //{
                    //    thisItem.CmdNum = (short)Visio.VisUICmds.visCmdAlignObjectLeft;
                    //}

                    ////c and C
                    //if ((thisItem.Key == (short)99) && (thisItem.Control == -1) && (thisItem.Shift == -1))
                    //{
                    //    thisItem.CmdNum = (short)Visio.VisUICmds.visCmdAlignObjectCenter;
                    //}
                    //if ((thisItem.Key == (short)67) && (thisItem.Control == -1) && (thisItem.Shift == -1))
                    //{
                    //    thisItem.CmdNum = (short)Visio.VisUICmds.visCmdAlignObjectCenter;
                    //}


                    ////r and R
                    //if ((thisItem.Key == (short)114) && (thisItem.Control == -1) && (thisItem.Shift == -1))
                    //{
                    //    thisItem.CmdNum = (short)Visio.VisUICmds.visCmdAlignObjectRight;
                    //}
                    //if ((thisItem.Key == (short)82) && (thisItem.Control == -1) && (thisItem.Shift == -1))
                    //{
                    //    thisItem.CmdNum = (short)Visio.VisUICmds.visCmdAlignObjectRight;
                    //}

                    ////t and T
                    //if ((thisItem.Key == (short)116) && (thisItem.Control == -1) && (thisItem.Shift == -1))
                    //{
                    //    thisItem.CmdNum = (short)Visio.VisUICmds.visCmdAlignObjectTop;
                    //}
                    //if ((thisItem.Key == (short)84) && (thisItem.Control == -1) && (thisItem.Shift == -1))
                    //{
                    //    thisItem.CmdNum = (short)Visio.VisUICmds.visCmdAlignObjectTop;
                    //}
                }



            }
            catch (Exception ex)
            {

            }
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
