using Microsoft.Office.Tools.Ribbon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using VisAssistDatabaseBackEnd.DataUtilities;
using VisAssistDatabaseBackEnd.Forms;

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
            ConnectionsUtilities.InitializeDatabase("");

        }

        private void btnProjectInfo_Click(object sender, RibbonControlEventArgs e)
        {
            //this is the seed data
            //ProjectUtilities.AddProjectInfoSeeding();
        }

        private void btnAddFileInfo_Click(object sender, RibbonControlEventArgs e)
        {
            //this would get triggered basically right after add project info because when we create a new project and press ok we create a new file...
            //idea that there would always be one file associated with the project UNLESS the user goes and disassociates all files in a project (but when initially creating a project there should always be at least one file)
            //FileUtilities.AddSeedFile();
        }

        private void btnAddPageInfo_Click(object sender, RibbonControlEventArgs e)
        {
            DatabaseConfig.BindToActiveDocument();

            bool bDoesDBExist = FileUtilities.DoesDBFileExist();
            if(bDoesDBExist)
            {
                PageUtilities.AddSeedPage();
            }
            

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
            
            ProjectUtilities.DeleteProject();


            //this just clears the project record from the table-user would never do this and we aren't giving them a place to do it 
            //ProjectUtilities.DeleteProjectInfo();
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
            DatabaseConfig.BindToActiveDocument();

            bool bDoesDBExist = FileUtilities.DoesDBFileExist();
            if (bDoesDBExist)
            {
                bool bDoesTableExist = DataProcessingUtilities.DoesTableHaveAnyRecords(DataProcessingUtilities.SqlTables.PagesTable.sPagesTable);
                if (bDoesTableExist)
                {
                    PageUtilities.OpenPagesForm();
                }
            }

        }

        private void btnDeleteDatabase_Click(object sender, RibbonControlEventArgs e)
        {
            ConnectionsUtilities.DeleteDatabase();
        }

        private void btnGetProjectInfo_Click(object sender, RibbonControlEventArgs e)
        {
            DatabaseConfig.BindToActiveDocument();

            bool bDoesDBExist = FileUtilities.DoesDBFileExist();
            if (bDoesDBExist)
            {
                string sAction = "Update";
                ProjectUtilities.OpenProjectForm(sAction, "", "");
                // ProjectUtilities.GetProjectInfo();
            }
            
        }

        private void btnGetFileData_Click(object sender, RibbonControlEventArgs e)
        {
            DatabaseConfig.BindToActiveDocument();


            bool bDoesDBExist = FileUtilities.DoesDBFileExist();
            if (bDoesDBExist)
            {
                bool bDoesTableExist = DataProcessingUtilities.DoesTableHaveAnyRecords(DataProcessingUtilities.SqlTables.FilesTable.sFilesTable);
                if (bDoesDBExist)
                {
                    FileUtilities.OpenFileForm();
                }

            }

        }

        private void btnAddProjectWithVisio_Click(object sender, RibbonControlEventArgs e)
        {
            //this creates the visio document
            //string sClass = "Master"; //i think this would always creating the Master File
            //FileUtilities.AddVisioDocument(sClass);

            string sFilePath = ProjectUtilities.AddProjectFileStructure();

            string sProjectName = ProjectUtilities.GetProjectName();


            string sAction = "Add";
            ProjectUtilities.OpenProjectForm(sAction, sProjectName, sFilePath);





        }

        private void btnAddFile_Click(object sender, RibbonControlEventArgs e)
        {
            //this will create the class b file and add it to an existing project
            //could either add the file to the existing doc's project
            //or could add a file to an existing project if the user points to save the file somewhere else...
            DatabaseConfig.BindToActiveDocument();

            bool bDoesDBExist = FileUtilities.DoesDBFileExist();
            if (bDoesDBExist)
            {
                FileUtilities.AddNewFile();
            }

        }

        private void btnDeleteFile_Click(object sender, RibbonControlEventArgs e)
        {
            DatabaseConfig.BindToActiveDocument();

            bool bDoesDBExist = FileUtilities.DoesDBFileExist();
            if (bDoesDBExist)
            {
                FileUtilities.OpenFileForm();
            }
        }

        private void btnDisAssociateFile_Click(object sender, RibbonControlEventArgs e)
        {
            DatabaseConfig.BindToActiveDocument();

            bool bDoesDBExist = FileUtilities.DoesDBFileExist();
            if (bDoesDBExist)
            {
                FileUtilities.OpenFileForm();
            }
        }



        private void btnAssociateFile_Click(object sender, RibbonControlEventArgs e)
        {
            DatabaseConfig.BindToActiveDocument();

            bool bDoesDBExist = FileUtilities.DoesDBFileExist();
            if (bDoesDBExist)
            {
                FileUtilities.WhichFileToAssociate();
            }
        }
    }
}
