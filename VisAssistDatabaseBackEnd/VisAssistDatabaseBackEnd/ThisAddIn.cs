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
        public bool m_bIsPageDuplicating = false;

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

                //// Filter only custom property cells
                //System.Array filterArray = Array.CreateInstance(typeof(short), 7);
                //filterArray.SetValue((short)Visio.VisSectionIndices.visSectionProp, 0);
                //filterArray.SetValue((short)Visio.VisRowIndices.visRowFirst, 1);
                //filterArray.SetValue((short)Visio.VisCellIndices.visCustPropsValue, 2);
                //filterArray.SetValue((short)Visio.VisSectionIndices.visSectionProp, 3);
                //filterArray.SetValue((short)Visio.VisRowIndices.visRowLast, 4);
                //filterArray.SetValue((short)Visio.VisCellIndices.visCustPropsValue, 5);
                //filterArray.SetValue((short)1, 6); // true
                //evtCellModified.SetFilterSRC(ref filterArray);

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
                        string sKey = ovShape.ID + "|" + ovShape.ContainingPage.Name;
                        if (!m_pendingShapeIds.Contains(sKey))
                        {
                            VisAssistDatabaseBackEnd.VisioUtilities.Application.OnShapeAdded(ovShape);

                            m_pendingShapeIds.Add(sKey);
                        }

                        break;
                    }

                //ShapeDeleted 16448
                case (short)((short)Visio.VisEventCodes.visEvtDel + (short)Visio.VisEventCodes.visEvtShape):
                    {
                        Visio.Shape ovShape = (Visio.Shape)subject;
                        string sKey = ovShape.ID + "|" + ovShape.ContainingPage.Name;
                        if (!m_pendingShapeIds.Contains(sKey))
                        {
                            VisAssistDatabaseBackEnd.VisioUtilities.Application.OnShapeDeleted((Visio.Shape)subject);
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
                            string sVisAssistFolderPath = FileUtilities.GetFolderPath(ovDocument);
                            //we are doing a redo/undo that is causing a deletion of a page, however the pageid may not be the updated one because it reverts back to what is what duplicated from...
                            DatabaseUtilities.CheckPageExistence(ovDocument, sVisAssistFolderPath);
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
                            //if this is a duplicate then we are going to set the pagesheets formula first...
                           // ovPage.PageSheet.Cells["User.PageID"].Formula = VisioUtilities.Application.FormatStringForVisio("LillY");
                           if(m_bIsPageDuplicating)
                            {
                                //we are duplicating a page...
                                VisAssistDatabaseBackEnd.VisioUtilities.Application.OnPageDuplicated(ovPage);
                            }
                           else
                            {
                                //we are just adding a page
                                VisAssistDatabaseBackEnd.VisioUtilities.Application.OnPageAdded((Visio.Page)subject);
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

                                    if(Globals.ThisAddIn.Application.IsUndoingOrRedoing)
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
                        VisAssistDatabaseBackEnd.VisioUtilities.Application.OnPageChanged((Visio.Page)subject);
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
                        VisAssistDatabaseBackEnd.VisioUtilities.Application.OnDocumentChanged((Visio.Document)subject);
                        break;
                    }

                case (short)(short)Visio.VisEventCodes.visEvtApp + (short)Visio.VisEventCodes.visEvtMarker:
                    {

                        string markerName = (string)moreInformation;

                        if (markerName.StartsWith("PageDeleted_"))
                        {
                            string pageID = markerName.Substring("PageDeleted_".Length);

                            // Get the document (usually via subject or Globals)
                            Visio.Document doc = Globals.ThisAddIn.Application.ActiveDocument;

                            // Find the restored page
                            Visio.Page restoredPage = null;
                            foreach (Visio.Page p in doc.Pages)
                            {
                                string pID = p.PageSheet.Cells["User.PageID"].get_ResultStr(0);
                                if (pID == pageID)
                                {
                                    restoredPage = p;
                                    break;
                                }
                            }

                            if (restoredPage != null)
                            {
                                // Call your OnPageAdded logic
                                VisAssistDatabaseBackEnd.VisioUtilities.Application.OnPageAdded(restoredPage);
                            }
                        }
                        // Visio.Application ovApplication = Globals.ThisAddIn.Application;

                        // string sContextString = (string)moreInformation;

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
