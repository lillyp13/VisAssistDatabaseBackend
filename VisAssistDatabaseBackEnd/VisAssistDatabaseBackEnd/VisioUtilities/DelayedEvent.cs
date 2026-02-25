using System.Collections.Generic;
using Visio = Microsoft.Office.Interop.Visio;

namespace VisAssistDatabaseBackEnd.VisioUtilities
{
    public class DelayedEvent
    {
        public Visio.Document ovDocument;
        public Visio.Page ovPage; //ithink i can switch out this and use sPageName and the document to find the correct page...
        public string sPageName;
        public string sShapeName;
        public Visio.Selection ovSelection; //we need this to move shapes
        public string sOperationType;
        public int iPageIndex;
        public string sNewValue;
        public string sPageID;
        public List<string> oListPages;
        public Dictionary<string, Visio.Shape> odictOfShapes; //we still need this to pair wires together...(specifically undoing or redoing...)
        public List<string> olstShapes;

        public DelayedEvent() { }


    }
}
