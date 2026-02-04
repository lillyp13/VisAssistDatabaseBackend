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

namespace VisAssistDatabaseBackEnd.VisioUtilities
{
    internal class Application
    {


        //PAGE EVENTS
        internal static void OnPageAdded(Visio.Page ovVisioPage)
        {
            try
            {
                string sVisAssistFolderPath = FileUtilities.GetFolderPath(ovVisioPage.Document);
                DatabaseConfig.BindToActiveDocument(sVisAssistFolderPath);
                string sProjectID = ovVisioPage.Document.DocumentSheet.Cells["User.ProjectID"].get_ResultStr(0);

                //add page user cells...
                bool bAlreadyAdded = PageUtilities.AddUserCellsToPage(ovVisioPage);
                if (!bAlreadyAdded)
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

                PageUtilities.DeletePage(ovPage, sProjectID);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in OnPageDeleted " + ex.Message, "VisAssist");
            }
        }
    }
}
