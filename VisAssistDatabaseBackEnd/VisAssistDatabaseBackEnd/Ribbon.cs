using Microsoft.Office.Tools.Ribbon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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
            //this is the seed data
            ProjectUtilities.AddProjectInfo();
        }

        private void btnAddFileInfo_Click(object sender, RibbonControlEventArgs e)
        {
            //this would get triggered basically right after add project info because when we create a new project and press ok we create a new file...
            //idea that there would always be one file associated with the project UNLESS the user goes and disassociates all files in a project (but when initially creating a project there should always be at least one file)
            FileUtilities.AddFirstFile();
        }

        private void btnAddPageInfo_Click(object sender, RibbonControlEventArgs e)
        {
            PageUtilities.AddPage();
        }

        private void btnDeletePageInfo_Click(object sender, RibbonControlEventArgs e)
        {
            PageUtilities.DeleteAllPages();
        }

        private void btnDeleteFileInfo_Click(object sender, RibbonControlEventArgs e)
        {
            FileUtilities.DeleteAllFiles();
        }

        private void btnDeleteProjectInfo_Click(object sender, RibbonControlEventArgs e)
        {
            ProjectUtilities.DeleteProjectInfo();
        }

        private void btnModifyProjectInfo_Click(object sender, RibbonControlEventArgs e)
        {
            //ProjectUtilities.BuildUpdateSql();
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
           // PageUtilities.UpdatePage();
        }

        private void btnGetPageName_Click(object sender, RibbonControlEventArgs e)
        {
            //grab all the pages and put them in a datagridview 
            //for now let's build a datagridview of all the pages in just one file...
            bool bDoesTableExist = DataProcessingUtilities.DoesTableHaveAnyRecords("pages_Table");
            PageUtilities.OpenPagesForm();
        }

        private void btnDeleteDatabase_Click(object sender, RibbonControlEventArgs e)
        {
            ProjectUtilities.DeleteDatabase();
        }

        private void btnGetProjectInfo_Click(object sender, RibbonControlEventArgs e)
        {
            bool bDoesTableExist = DataProcessingUtilities.DoesTableHaveAnyRecords("project_Table"); 
            ProjectUtilities.OpenProjectForm(bDoesTableExist);
           // ProjectUtilities.GetProjectInfo();
        }

        private void btnGetFileData_Click(object sender, RibbonControlEventArgs e)
        {
            bool bDoesTableExist = DataProcessingUtilities.DoesTableHaveAnyRecords("files_Table");
            FileUtilities.OpenFileForm();
        }

        private void btnAddProjectWithVisio_Click(object sender, RibbonControlEventArgs e)
        {
            ConnectionsUtilities.InitializeDatabase();
            ProjectUtilities.AddProject();
           
            
                ProjectUtilities.OpenProjectForm(false);

           

        }
    }
}
