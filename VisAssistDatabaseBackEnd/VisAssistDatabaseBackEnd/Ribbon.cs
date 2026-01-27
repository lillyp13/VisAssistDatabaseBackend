using Microsoft.Office.Tools.Ribbon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using VisAssistDatabaseBackEnd.DataUtilities;
using VisAssistDatabaseBackEnd.Forms;
using Visio = Microsoft.Office.Interop.Visio;

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
            Visio.Document ovDoc = Globals.ThisAddIn.Application.ActiveDocument;
            if (ovDoc != null)
            {


                string sFolderPath = FileUtilities.ReturnFileStructurePath(ovDoc.Path);

                DatabaseConfig.BindToActiveDocument(sFolderPath);

                bool bDoesDBExist = FileUtilities.DoesDBFileExist();
                if (bDoesDBExist)
                {

                    bool bIsFileAssignedToProject = FileUtilities.IsFileAssignedToProject(ovDoc);
                    if (bIsFileAssignedToProject)
                    {


                        PageUtilities.AddSeedPage();
                    }
                    else
                    {
                        MessageBox.Show("This file is not assigned to a project.", "VisAssist");
                    }
                }
            }


        }

        private void btnDeletePageInfo_Click(object sender, RibbonControlEventArgs e)
        {
            PageUtilities.DeleteAllPages();
        }

        private void btnDeleteFileInfo_Click(object sender, RibbonControlEventArgs e)
        {
            //FileUtilities.DeleteAllFiles();
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
            Visio.Document ovDoc = Globals.ThisAddIn.Application.ActiveDocument;
            if (ovDoc != null)
            {

                string sFolderPath = FileUtilities.ReturnFileStructurePath(ovDoc.Path);
                DatabaseConfig.BindToActiveDocument(sFolderPath);

                bool bDoesDBExist = FileUtilities.DoesDBFileExist();
                if (bDoesDBExist)
                {
                    bool bDoesTableExist = DataProcessingUtilities.DoesTableHaveAnyRecords(DataProcessingUtilities.SqlTables.PagesTable.sPagesTable);
                    if (bDoesTableExist)
                    {

                        bool bIsFileAssignedToProject = FileUtilities.IsFileAssignedToProject(ovDoc);
                        if (bIsFileAssignedToProject)
                        {
                            PageUtilities.OpenPagesForm();
                        }
                        else
                        {
                            MessageBox.Show("This file is not assigned to a project.", "VisAssist");
                        }

                    }
                }
            }

        }

        private void btnDeleteDatabase_Click(object sender, RibbonControlEventArgs e)
        {
            ConnectionsUtilities.DeleteDatabase();
        }

        private void btnGetProjectInfo_Click(object sender, RibbonControlEventArgs e)
        {

            Visio.Document ovDoc = Globals.ThisAddIn.Application.ActiveDocument;
            if (ovDoc != null)
            {
                string sFolderPath = FileUtilities.ReturnFileStructurePath(ovDoc.Path);
                DatabaseConfig.BindToActiveDocument(sFolderPath);

                bool bDoesDBExist = FileUtilities.DoesDBFileExist();
                if (bDoesDBExist)
                {
                    string sAction = "Update";
                    ProjectUtilities.OpenProjectForm(sAction, "", "");
                    // ProjectUtilities.GetProjectInfo();
                }
            }

        }

        private void btnGetFileData_Click(object sender, RibbonControlEventArgs e)
        {
            Visio.Document ovDoc = Globals.ThisAddIn.Application.ActiveDocument;
            if (ovDoc != null)
            {
                string sFolderPath = FileUtilities.ReturnFileStructurePath(ovDoc.Path);

                DatabaseConfig.BindToActiveDocument(sFolderPath);


                bool bDoesDBExist = FileUtilities.DoesDBFileExist();
                if (bDoesDBExist)
                {
                    bool bDoesTableExist = DataProcessingUtilities.DoesTableHaveAnyRecords(DataProcessingUtilities.SqlTables.FilesTable.sFilesTable);
                    if (bDoesDBExist)
                    {

                        bool bIsFileAssignedToProject = FileUtilities.IsFileAssignedToProject(ovDoc);
                        if (bIsFileAssignedToProject)
                        {


                            FileUtilities.OpenFileForm();
                        }
                        else
                        {
                            MessageBox.Show("This file is not assigned to a project.", "VisAssist");
                        }
                    }

                }
            }

        }

        private void btnAddProjectWithVisio_Click(object sender, RibbonControlEventArgs e)
        {
            //this creates the visio document
            //string sClass = "Master"; //i think this would always creating the Master File
            //FileUtilities.AddVisioDocument(sClass);

            string sFilePath = ProjectUtilities.AddProjectFileStructure();

            if (sFilePath != null)
            {

                string sProjectName = ProjectUtilities.GetProjectName();


                string sAction = "Add";
                ProjectUtilities.OpenProjectForm(sAction, sProjectName, sFilePath);
            }




        }

        private void btnAddFile_Click(object sender, RibbonControlEventArgs e)
        {
            //this will create the class b file and add it to an existing project
            //could either add the file to the existing doc's project
            //or could add a file to an existing project if the user points to save the file somewhere else...
            Visio.Document ovDoc = Globals.ThisAddIn.Application.ActiveDocument;
            if (ovDoc != null)
            {
                string sFolderPath = FileUtilities.ReturnFileStructurePath(ovDoc.Path);

                DatabaseConfig.BindToActiveDocument(sFolderPath);

                bool bDoesDBExist = FileUtilities.DoesDBFileExist();
                if (bDoesDBExist)
                {

                    bool bIsFileAssignedToProject = FileUtilities.IsFileAssignedToProject(ovDoc);
                    if (bIsFileAssignedToProject)
                    {
                        FileUtilities.AddNewFile();
                    }
                    else
                    {
                        MessageBox.Show("This file is not assigned to a project.", "VisAssist");
                    }
                }
            }

        }

        private void btnDeleteFile_Click(object sender, RibbonControlEventArgs e)
        {
            Visio.Document ovDoc = Globals.ThisAddIn.Application.ActiveDocument;
            if (ovDoc != null)
            {
                string sFolderPath = FileUtilities.ReturnFileStructurePath(ovDoc.Path);

                DatabaseConfig.BindToActiveDocument(sFolderPath);

                bool bDoesDBExist = FileUtilities.DoesDBFileExist();
                if (bDoesDBExist)
                {

                    bool bIsFileAssignedToProject = FileUtilities.IsFileAssignedToProject(ovDoc);
                    if (bIsFileAssignedToProject)
                    {
                        FileUtilities.OpenFileForm();
                    }
                    else
                    {
                        MessageBox.Show("This file is not assigned to a project.", "VisAssist");
                    }

                }
            }
        }

        private void btnDisAssociateFile_Click(object sender, RibbonControlEventArgs e)
        {
            Visio.Document ovDoc = Globals.ThisAddIn.Application.ActiveDocument;
            if (ovDoc != null)
            {
                string sFolderPath = FileUtilities.ReturnFileStructurePath(ovDoc.Path);

                DatabaseConfig.BindToActiveDocument(sFolderPath);

                bool bDoesDBExist = FileUtilities.DoesDBFileExist();
                if (bDoesDBExist)
                {

                    bool bIsFileAssignedToProject = FileUtilities.IsFileAssignedToProject(ovDoc);
                    if (bIsFileAssignedToProject)
                    {
                        FileUtilities.OpenFileForm();
                    }
                    else
                    {
                        MessageBox.Show("This file is not assigned to a project.", "VisAssist");
                    }
                }
            }
        }



        private void btnAssociateFile_Click(object sender, RibbonControlEventArgs e)
        {
            Visio.Document ovDoc = Globals.ThisAddIn.Application.ActiveDocument;
            if (ovDoc != null)
            {
                string sFolderPath = FileUtilities.ReturnFileStructurePath(ovDoc.Path);

                DatabaseConfig.BindToActiveDocument(sFolderPath);

                bool bDoesDBExist = FileUtilities.DoesDBFileExist();
                if (bDoesDBExist)
                {

                    bool bIsFileAssignedToProject = FileUtilities.IsFileAssignedToProject(ovDoc);
                    if (bIsFileAssignedToProject)
                    {


                        FileUtilities.WhichFileToAssociate();
                    }
                    else
                    {
                        MessageBox.Show("This file is not assigned to a project. Use the Associate Orphaned File button", "VisAssist");
                    }
                }
            }
        }

        private void btnAssociateOrphanedFile_Click(object sender, RibbonControlEventArgs e)
        {
            //we have an orphaned file and we want to assign it to a project
            //ask the user which project to save this orphaned file to 
            //we want a folder dialog box where the user will choose the DB folder that contains the db file and where the file structure is where we want to save the new file to
            //we will need to unhide the hidden folders..
            Visio.Document ovDoc = Globals.ThisAddIn.Application.ActiveDocument;
            if (ovDoc == null)
            {
                MessageBox.Show("Please open a document.");
            }

            string sProjectID = ovDoc.DocumentSheet.Cells["User.ProjectID"].get_ResultStr(0);
            if (sProjectID == "")
            {


                string sDBPath = FileUtilities.WhichProjectToAssociateOrphanedFile();

                //from DBPath get the path of the new file: 
                string sFileStructure = Path.GetDirectoryName(sDBPath);
                //get the file name of the curreent unassigned docuemnt 

                string sFileName = ovDoc.Name;

                string sDestinationFilePath = Path.Combine(sFileStructure, sFileName);

                string sFilePath = FileUtilities.ReturnFileStructurePath(ovDoc.Path);
                //string sFolderPath = Path.GetDirectoryName(sFilePath);
                string sFilePathToCopy = Path.Combine(sFilePath, sFileName);


                //need to bind the database the the document that is the target...
                 DatabaseConfig.BindToActiveDocument(sFileStructure);
                FileUtilities.AssociateFile(ovDoc, sDestinationFilePath, sFileStructure, sFileName, false, sFilePathToCopy, "");

                MessageBox.Show("Databases successfully associated!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

            }
            else
            {
                MessageBox.Show("This is not an orphaned file.");
            }

        }

        private void btnChangeFileName_Click(object sender, RibbonControlEventArgs e)
        {
            Visio.Document ovDoc = Globals.ThisAddIn.Application.ActiveDocument;
            if (ovDoc != null)
            {
                string sFolderPath = FileUtilities.ReturnFileStructurePath(ovDoc.Path);


                bool bIsFileAssignedToProject = FileUtilities.IsFileAssignedToProject(Globals.ThisAddIn.Application.ActiveDocument);
                if (bIsFileAssignedToProject)
                {
                    DatabaseConfig.BindToActiveDocument(sFolderPath);
                    //open the naem form witn the current visio file name and allow them to change it...
                    string sCurrentName = Globals.ThisAddIn.Application.ActiveDocument.Name;
                    string sFileName = FileUtilities.GetFileName(sCurrentName);

                    if (sFileName != null)
                    {
                        FileUtilities.UpdateFileName(sFileName);
                    }
                }
            }

        }
    }
}
