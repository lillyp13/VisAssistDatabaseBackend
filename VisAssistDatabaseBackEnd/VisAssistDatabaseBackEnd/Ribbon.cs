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
            ProjectUtilities.AddProjectInfo();
        }

       

        private void btnDeleteProjectInfo_Click(object sender, RibbonControlEventArgs e)
        {
            ProjectUtilities.DeleteProjectInfo();
        }

        private void btnModifyProjectInfo_Click(object sender, RibbonControlEventArgs e)
        {
            //ConnectionsUtilities.UpdateProjectInfo();
        }
    }
}
