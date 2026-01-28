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
                else
                {
                    //the db doesn't exist 
                    FileUtilities.OrphanFile(sFolderPath);
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
                else
                {
                    //the db doesn't exist 
                    FileUtilities.OrphanFile(sFolderPath);
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
                else
                {
                    FileUtilities.OrphanFile(sFolderPath);

                   
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
                            //the file is assigned to a project 
                            FileUtilities.OpenFileForm();
                        }
                        else
                        {
                            MessageBox.Show("This file is not assigned to a project.", "VisAssist");
                        }
                    }
                    

                }
                else
                {
                    //the db doesn't exist ... i think we should orphan the file, we may want to do more than orphan it...by clearing file id and page id and so on...
                    FileUtilities.OrphanFile(sFolderPath);
                }
            }

        }

        private void btnAddProjectWithVisio_Click(object sender, RibbonControlEventArgs e)
        {
            //this creates the visio document
            //string sClass = "Master"; //i think this would always creating the Master File
           

            string sFilePath = ProjectUtilities.AddProjectFileStructure();

            if (sFilePath != null)
            {

                string sProjectName = ProjectUtilities.GetProjectName();

                if (sProjectName != null && sProjectName != "")
                {
                    string sAction = "Add";
                    ProjectUtilities.OpenProjectForm(sAction, sProjectName, sFilePath);
                }
                else
                {
                    //otherwise the user cancelled the project name...
                    //we need to delete the folder that we created because no file or project was added
                    string sDirectory = Path.GetDirectoryName(sFilePath);
                    if (Directory.Exists(sDirectory))
                    {
                        Directory.Delete(sDirectory, true); //delete recursively...
                    }
                }
                
            }
            //otherwise the user cancelled when picking a place to save the project to..



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
                else
                {
                    //the db doesn't exist. orphan the file...
                    FileUtilities.OrphanFile(sFolderPath);
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
                else
                {
                    //the db doesn't exist 
                    FileUtilities.OrphanFile(sFolderPath);
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
                else
                {
                    //the db doesn't exist 
                    FileUtilities.OrphanFile(sFolderPath);
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
                else
                {
                    //the db doesn't exist, orphan the file 
                    FileUtilities.OrphanFile(sFolderPath);
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
                //the file is orphaned because there is no projectId in the user cell...

                FileUtilities.AssociateOrphanedFiles(ovDoc);


            }
            else
            {
                //there is a projectID in the user cell
                //this could live in a file structure that contains a db, doesn't contains a db, or contains the db that the file was copied from ...
                //check to see if the file has a db in the correct file structure and if it doesn't then go ahead and orphan the file and recall this, but if there is a db then don't orphan the file...
                bool bDoesDBExist = FileUtilities.DoesDBFileExist();
                if(bDoesDBExist)
                {
                    //the file has a project id and it lives in a file structure that contains a db this is not an orphaned file...
                    //we want to use that db that we just found to see if there is already a file in that project db with the file id we are trying to add
                    //if that file id doesn't exist: normal message box about how this is not an orphaned file...
                    //if the file id does exist: this file exists already or this file is a copy of a file that already exists in the project...i think we want to check to see which case it is
                    //if the file id exists and it is this file then we are okay
                    //but if the file id exists and it is not this file then this means we have found a duplicate visio documents with matching fileids trying to be a part of the same project...
                    //check to see if we have duplicate file id 

                    //the db exists
                    MessageBox.Show("This is not an orphaned file.");
                }
                else
                {
                    //the db doesn't exist let's orphan this file
                    string sFolderPath = FileUtilities.ReturnFileStructurePath(ovDoc.Path);
                  
                    FileUtilities.OrphanFile(sFolderPath);
                    //call associateorphanedfiles...now that we have an oprhaned file
                    FileUtilities.AssociateOrphanedFiles(ovDoc);
                }
                
            }

        }

        private void btnChangeFileName_Click(object sender, RibbonControlEventArgs e)
        {
            Visio.Document ovDoc = Globals.ThisAddIn.Application.ActiveDocument;
            if (ovDoc != null)
            {
                string sFolderPath = FileUtilities.ReturnFileStructurePath(ovDoc.Path);
                bool bDoesDBExist = FileUtilities.DoesDBFileExist();

                if (bDoesDBExist)
                {
                    bool bIsFileAssignedToProject = FileUtilities.IsFileAssignedToProject(Globals.ThisAddIn.Application.ActiveDocument);
                    if (bIsFileAssignedToProject)
                    {
                        DatabaseConfig.BindToActiveDocument(sFolderPath);
                        //open the naem form witn the current visio file name and allow them to change it...
                        string sCurrentName = Globals.ThisAddIn.Application.ActiveDocument.Name;
                        //get the string that is inside "Dwg - .vsdx"...

                        sCurrentName = FileUtilities.ExtractNameFromVisioFile(sCurrentName);

                        string sFileName = FileUtilities.GetFileName(sCurrentName);

                        if (sFileName != null && sFileName != "")
                        {
                            FileUtilities.UpdateFileName(sFileName);
                        }
                    }
                }
                else
                {
                    //the db doesn't exist orphan the file ...
                    FileUtilities.OrphanFile(sFolderPath);
                }

            }

        }
    }
}
