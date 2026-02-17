using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Visio = Microsoft.Office.Interop.Visio;

namespace VisAssistDatabaseBackEnd.VisioUtilities
{
    public class DelayedEvent
    {
        public Visio.Document ovDocument;
        public Visio.Page ovPage;
        public string sPageName;
        public string sShapeName;
        public Visio.Shape ovShape;
        public Visio.Selection ovSelection;
        public string sOperationType;
        public int iPageIndex;
        public string sNewValue;
        public Visio.Cell ovCell;
        public Visio.Shape ovOtherShape;
        public string sPageID;
        public List<Visio.Page> oListPages;
        public Dictionary<string, Visio.Shape> oDictOfShapes;

        public DelayedEvent() { }


    }
}
