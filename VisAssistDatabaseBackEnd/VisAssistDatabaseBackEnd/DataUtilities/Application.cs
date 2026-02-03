using Microsoft.Office.Interop.Visio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Visio = Microsoft.Office.Interop.Visio;

namespace VisAssistDatabaseBackEnd.DataUtilities
{
    internal class Application
    {

        internal static void OnPageAdded(Visio.Page ovVisioPage)
        {
            string sFolderPath = FileUtilities.GetFolderPath(ovVisioPage.Document);
            DatabaseConfig.BindToActiveDocument(sFolderPath);
            string sProjectID = ovVisioPage.Document.DocumentSheet.Cells["User.ProjectID"].get_ResultStr(0);

            //add page user cells...
            bool bAlreadyAdded = PageUtilities.AddUserCellsToPage(ovVisioPage);
            if (!bAlreadyAdded)
            {
                //we haven't added the page yet...
                PageUtilities.AddPageToDatabase(ovVisioPage, sProjectID);
            }

        }

        internal static void OnPageChanged(Visio.Page ovVisioPage)
        {
            string sFolderPath = FileUtilities.GetFolderPath(ovVisioPage.Document);
            DatabaseConfig.BindToActiveDocument(sFolderPath);
            string sProjectID = ovVisioPage.Document.DocumentSheet.Cells["User.ProjectID"].get_ResultStr(0);

            PageUtilities.UpdatePageInDatabase(ovVisioPage, sProjectID);
        }

        internal static void OnVisioIsIdle(Visio.Application thisApp)
        {
            //CheckPageOrder(thisApp.ActiveDocument);
        }

        internal static void OnDocumentChanged(Visio.Document ovDocument)
        {
            //the document has changed, i believe this is the order of the pages that is changing...
            //is the only time this gets called when the user changes the page index by dragging pages around?
            //should we just update each page in the document?
            string sProjectID = ovDocument.DocumentSheet.Cells["User.ProjectID"].get_ResultStr(0);
            foreach (Visio.Page ovPage in ovDocument.Pages)
            {
                PageUtilities.UpdatePageInDatabase(ovPage, sProjectID);
            }
        }

        internal static void OnPageDeleted(Visio.Page ovPage)
        {
            //user is deleting the page
            string sProjectID = ovPage.Document.DocumentSheet.Cells["User.ProjectID"].get_ResultStr(0);

            PageUtilities.DeletePage(ovPage, sProjectID);
        }
    }
}
