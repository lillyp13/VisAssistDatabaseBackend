using Microsoft.Office.Tools.Ribbon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisAssistDatabaseBackEnd.DataUtilities;

namespace VisAssistDatabaseBackEnd
{
    public partial class Ribbon
    {
        private void Ribbon_Load(object sender, RibbonUIEventArgs e)
        {

        }

        private void btnAddDatabase_Click(object sender, RibbonControlEventArgs e)
        {
            //open and initialize the database
            ConnectionsUtilities.InitializeDatabase();

        }

        private void btnProjectInfo_Click(object sender, RibbonControlEventArgs e)
        {
            ConnectionsUtilities.AddProjectInfo();
        }

        private void btnAddFileInfo_Click(object sender, RibbonControlEventArgs e)
        {
            ConnectionsUtilities.AddFile();
        }

        private void btnAddPageInfo_Click(object sender, RibbonControlEventArgs e)
        {
            ConnectionsUtilities.AddPage();
        }

        private void btnDeletePageInfo_Click(object sender, RibbonControlEventArgs e)
        {
            ConnectionsUtilities.DeletePage();
        }

        private void btnDeleteFileInfo_Click(object sender, RibbonControlEventArgs e)
        {
            ConnectionsUtilities.DeleteFile();
        }

        private void btnDeleteProjectInfo_Click(object sender, RibbonControlEventArgs e)
        {
            ConnectionsUtilities.DeleteProjectInfo();
        }

        private void btnModifyProjectInfo_Click(object sender, RibbonControlEventArgs e)
        {
            ConnectionsUtilities.UpdateProjectInfo();
        }

        private void btnModifyFile_Click(object sender, RibbonControlEventArgs e)
        {

        }

        private void btnAddWireInfo_Click(object sender, RibbonControlEventArgs e)
        {
           // ConnectionsUtilities.AddWireInfo();
        }

        private void btnModifyPage_Click(object sender, RibbonControlEventArgs e)
        {
            ConnectionsUtilities.UpdatePage();
        }

        private void btnGetPageName_Click(object sender, RibbonControlEventArgs e)
        {
            //we want to retrieve a page name based on the PageID
            //for now we will hardcode a random page...
            ConnectionsUtilities.GetPageName();
        }
    }
}
