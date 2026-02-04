using Microsoft.Office.Interop.Visio;
using Microsoft.Office.Tools.Ribbon;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using VisAssistDatabaseBackEnd.DataUtilities;
using VisAssistDatabaseBackEnd.Forms;
using VisAssistDatabaseBackEnd.Project_Manifest;
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
            DatabaseUtilities.InitializeDatabase("");

        }


        private void btnDeletePageInfo_Click(object sender, RibbonControlEventArgs e)
        {
            PageUtilities.DeleteAllPages();
        }


        private void btnDeleteProjectInfo_Click(object sender, RibbonControlEventArgs e)
        {

            ProjectUtilities.DeleteProject();


            //this just clears the project record from the table-user would never do this and we aren't giving them a place to do it 
            //ProjectUtilities.DeleteProjectInfo();
        }


        private void btnAddWireInfo_Click(object sender, RibbonControlEventArgs e)
        {
            // ConnectionsUtilities.AddWireInfo();
        }


        private void btnGetPageName_Click(object sender, RibbonControlEventArgs e)
        {
            //grab all the pages and put them in a datagridview 
            //for now let's build a datagridview of all the pages in just one file...
            Visio.Document ovDoc = Globals.ThisAddIn.Application.ActiveDocument;
            if (ovDoc != null)
            {

                string sVisAssistFolderPath = FileUtilities.GetFolderPath(ovDoc);
                if(sVisAssistFolderPath != "")
                {
                    DatabaseConfig.BindToActiveDocument(sVisAssistFolderPath);

                    bool bDoesDBExist = FileUtilities.DoesDBFileExist(sVisAssistFolderPath);
                    if (bDoesDBExist)
                    {
                        bool bDoesTableExist = DatabaseUtilities.DoesTableHaveAnyRecords(DatabaseUtilities.SqlTables.PagesTable.sPagesTable);
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
                    { //the db doesn't exist. orphan the file...
                        MessageBox.Show("The db file doesn't exist.", "VisAssist");
                    }
                }
                else
                {
                    MessageBox.Show("Couldn't find the correct folder path.", "VisAssist");
                }
               
            }

        }

        private void btnDeleteDatabase_Click(object sender, RibbonControlEventArgs e)
        {
            DatabaseUtilities.DeleteDatabase();
        }

        private void btnGetProjectInfo_Click(object sender, RibbonControlEventArgs e)
        {
                
            Visio.Document ovDoc = Globals.ThisAddIn.Application.ActiveDocument;
            if (ovDoc != null)
            {
                string sVisAssistFolderPath = FileUtilities.GetFolderPath(ovDoc);
                if(sVisAssistFolderPath != "")
                {
                    bool bHasNecessaryFolders = FileUtilities.CheckIfSubFoldersExist(sVisAssistFolderPath); //i dont think we need to always check this, I know when we choose to copy, or open a project (i think it is only for when we need to confirm that a folder path that was given to use is a visassist project...
                    if (bHasNecessaryFolders)
                    {
                        DatabaseConfig.BindToActiveDocument(sVisAssistFolderPath);

                        bool bDoesDBExist = FileUtilities.DoesDBFileExist(sVisAssistFolderPath);
                        if (bDoesDBExist)
                        {
                            string sAction = "Update";
                            ProjectUtilities.OpenProjectForm(sAction, "", "");
                            // ProjectUtilities.GetProjectInfo();
                        }
                        else
                        {
                            //the db doesn't exist. orphan the file...
                            MessageBox.Show("The db file doesn't exist.", "VisAssist");
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Couldn't find the correct folder path.", "VisAssist");
                }

               
            }

        }

        private void btnGetFileData_Click(object sender, RibbonControlEventArgs e)
        {
            Visio.Document ovDoc = Globals.ThisAddIn.Application.ActiveDocument;
            if (ovDoc != null)
            {
                string sVisAssistFolderPath = FileUtilities.GetFolderPath(ovDoc);
                if(sVisAssistFolderPath != "")
                {
                    DatabaseConfig.BindToActiveDocument(sVisAssistFolderPath);


                    bool bDoesDBExist = FileUtilities.DoesDBFileExist(sVisAssistFolderPath);
                    if (bDoesDBExist)
                    {
                        bool bDoesTableExist = DatabaseUtilities.DoesTableHaveAnyRecords(DatabaseUtilities.SqlTables.FilesTable.sFilesTable);
                        if (bDoesDBExist)
                        {

                            bool bIsFileAssignedToProject = FileUtilities.IsFileAssignedToProject(ovDoc);
                            if (bIsFileAssignedToProject)
                            {
                                //the file is assigned to a project 
                                FileUtilities.OpenFilePropertiesForm();
                            }
                            else
                            {
                                MessageBox.Show("This file is not assigned to a project.", "VisAssist");
                            }
                        }


                    }
                    else
                    {
                        //the db doesn't exist. orphan the file...
                        MessageBox.Show("The db file doesn't exist.", "VisAssist");
                    }
                }
                else
                {
                    MessageBox.Show("Couldn't find the correct folder path.", "VisAssist");
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

                string sProjectName = ProjectUtilities.GetProjectNameFromForm();

                if (sProjectName != null && sProjectName != "")
                {
                    string sAction = "Add";
                    ProjectUtilities.OpenProjectForm(sAction, sProjectName, sFilePath);

                    //Create Manifest - after the project form is complete we can create the manifest file...
                    string sProjectFolderPath = System.IO.Path.GetDirectoryName(sFilePath).TrimEnd(System.IO.Path.DirectorySeparatorChar); // I would like to add a more universal way to get the project folder path
                   // string sDBPath = System.IO.Path.Combine(sProjectFolderPath, "DB", "VisAssistBackEnd.db"); // I would like to add a more universal way to get the db path
                    string sProjectId = ProjectUtilities.GetColumnInfoInProjectTableFromDatabase("Id");
                    ProjectManifest.CreateManifest(sProjectName, sProjectId, sProjectFolderPath);
                }
                else
                {
                    //otherwise the user cancelled the project name...
                    //we need to delete the folder that we created because no file or project was added
                    string sDirectory = System.IO.Path.GetDirectoryName(sFilePath);
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
                string sVisAssistFolderPath = FileUtilities.GetFolderPath(ovDoc);
                if(sVisAssistFolderPath != "")
                {
                    DatabaseConfig.BindToActiveDocument(sVisAssistFolderPath);

                    bool bDoesDBExist = FileUtilities.DoesDBFileExist(sVisAssistFolderPath);
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
                        MessageBox.Show("The db file doesn't exist.", "VisAssist");
                    }
                }
                else
                {
                    MessageBox.Show("Couldn't find the correct folder path.", "VisAssist");
                }
               
            }

        }

        private void btnDeleteFile_Click(object sender, RibbonControlEventArgs e)
        {
            Visio.Document ovDoc = Globals.ThisAddIn.Application.ActiveDocument;
            if (ovDoc != null)
            {
                string sVisAssistFolderPath = FileUtilities.GetFolderPath(ovDoc);
                if(sVisAssistFolderPath != "")
                {
                    DatabaseConfig.BindToActiveDocument(sVisAssistFolderPath);

                    bool bDoesDBExist = FileUtilities.DoesDBFileExist(sVisAssistFolderPath);
                    if (bDoesDBExist)
                    {

                        bool bIsFileAssignedToProject = FileUtilities.IsFileAssignedToProject(ovDoc);
                        if (bIsFileAssignedToProject)
                        {
                            FileUtilities.OpenFilePropertiesForm();
                        }
                        else
                        {
                            MessageBox.Show("This file is not assigned to a project.", "VisAssist");
                        }

                    }
                    else
                    {
                        //the db doesn't exist. orphan the file...
                        MessageBox.Show("The db file doesn't exist.", "VisAssist");
                    }
                }
                else
                {
                    MessageBox.Show("Couldn't find the correct folder path.", "VisAssist");
                }
                
            }
        }

        


        private void btnCopyAnotherFile_Click(object sender, RibbonControlEventArgs e)
        {
            Visio.Document ovDoc = Globals.ThisAddIn.Application.ActiveDocument;
            if (ovDoc != null)
            {
                string sVisAssistFolderPath = FileUtilities.GetFolderPath(ovDoc);
                if(sVisAssistFolderPath != "")
                {
                    DatabaseConfig.BindToActiveDocument(sVisAssistFolderPath);

                    bool bDoesDBExist = FileUtilities.DoesDBFileExist(sVisAssistFolderPath);
                    if (bDoesDBExist)
                    {

                        FileUtilities.WhichFileToCopy();

                    }
                    else
                    {
                        //the db doesn't exist, orphan the file 

                        MessageBox.Show("The database does not exist.", "VisAssist");
                    }
                }
                else
                {
                    MessageBox.Show("Couldn't find the correct folder path.", "VisAssist");
                }
               
            }
        }

       
        private void btnChangeFileName_Click(object sender, RibbonControlEventArgs e)
        {
            Visio.Document ovDoc = Globals.ThisAddIn.Application.ActiveDocument;
            if (ovDoc != null)
            {
                string sVisAssistFolderPath = FileUtilities.GetFolderPath(ovDoc);

                if (sVisAssistFolderPath != "")
                {
                    bool bDoesDBExist = FileUtilities.DoesDBFileExist(sVisAssistFolderPath);

                    if (bDoesDBExist)
                    {
                        bool bIsFileAssignedToProject = FileUtilities.IsFileAssignedToProject(Globals.ThisAddIn.Application.ActiveDocument);
                        if (bIsFileAssignedToProject)
                        {
                           
                            DatabaseConfig.BindToActiveDocument(sVisAssistFolderPath);
                            //open the naem form witn the current visio file name and allow them to change it...
                            string sCurrentName = Globals.ThisAddIn.Application.ActiveDocument.Name;
                            //get the string that is inside "Dwg - .vsdx"...

                            sCurrentName = FileUtilities.ExtractNameFromVisioFile(sCurrentName);

                            string sFileName = FileUtilities.GetFileNameFromForm(sCurrentName);

                            if (sFileName != null && sFileName != "")
                            {
                                FileUtilities.UpdateFileName(sFileName);
                            }
                        }
                    }
                    else
                    {
                        //the db doesn't exist. orphan the file...
                        MessageBox.Show("The db file doesn't exist.", "VisAssist");
                    }
                }
                else
                {
                    MessageBox.Show("Couldn't find the folder path.", "VisAssist");
                }

            }

        }

        private void btnOpenProject_Click(object sender, RibbonControlEventArgs e)
        {
            ProjectUtilities.OpenProject();
        }

        private void btnOpenFile_Click(object sender, RibbonControlEventArgs e)
        {
            //get the folderpath from the current document 
            Visio.Document ovDoc = Globals.ThisAddIn.Application.ActiveDocument;
            if (ovDoc != null)
            {
                string sVisAssistFolderPath = FileUtilities.GetFolderPath(ovDoc);
                if (sVisAssistFolderPath != "")
                {
                    //we need to determine if we are coming from the launch file or a different file...
                    string sSource = "";
                    if (ovDoc.Name == "LaunchFile.vsdx") //this is our general launch file
                    {
                        sSource = "Launch";

                    }
                    else
                    {
                        sSource = "File";

                    }

                    FileUtilities.PopulateProjectFilesDictionaryBasedOnDirectory(sVisAssistFolderPath);
                    FileUtilities.PopulateFilesOutsideProjectFilesFolderDictionaryBasedOnDirectory(sVisAssistFolderPath);

                    FileUtilities.OpenFileForm(sSource); //true we are launching this from the launch file...

                    FileUtilities.CheckForLaunchFile(sVisAssistFolderPath);

                    
                  
                }
                else
                {
                    MessageBox.Show("Couldn't find the folder path.", "VisAssist");
                }
            }
            else
            {
                MessageBox.Show("Please open a VisAssist Project first.", "VisAssist");


                //OR WE COULD HAVE THEM OPEN THE PROJECT AND PICK THE FILE LIKE THAT...I THINK WE SHOULD
                //ProjectUtilities.OpenProject();
            }
        }
    }
}
