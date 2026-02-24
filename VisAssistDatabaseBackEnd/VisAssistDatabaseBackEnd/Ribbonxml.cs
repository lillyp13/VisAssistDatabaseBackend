using Microsoft.Office.Core;
using Microsoft.Office.Tools.Ribbon;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using VisAssistDatabaseBackEnd.DataUtilities;
using VisAssistDatabaseBackEnd.Forms;
using VisAssistDatabaseBackEnd.ShapeUtilities;
using Office = Microsoft.Office.Core;
using Visio = Microsoft.Office.Interop.Visio;

// TODO:  Follow these steps to enable the Ribbon (XML) item:

// 1: Copy the following code block into the ThisAddin, ThisWorkbook, or ThisDocument class.

//  protected override Microsoft.Office.Core.IRibbonExtensibility CreateRibbonExtensibilityObject()
//  {
//      return new Ribbonxml();
//  }

// 2. Create callback methods in the "Ribbon Callbacks" region of this class to handle user
//    actions, such as clicking a button. Note: if you have exported this Ribbon from the Ribbon designer,
//    move your code from the event handlers to the callback methods and modify the code to work with the
//    Ribbon extensibility (RibbonX) programming model.

// 3. Assign attributes to the control tags in the Ribbon XML file to identify the appropriate callback methods in your code.  

// For more information, see the Ribbon XML documentation in the Visual Studio Tools for Office Help.


namespace VisAssistDatabaseBackEnd
{
    [ComVisible(true)]
    public class Ribbonxml : Office.IRibbonExtensibility
    {
        private Office.IRibbonUI ribbon;
        public static Ribbonxml Instance { get; private set; }
        public Ribbonxml()
        {
            Instance = this;
        }

        public void Ribbon_Load(Office.IRibbonUI ribbonUI)
        {
            this.ribbon = ribbonUI;
        }
        #region IRibbonExtensibility Members

        public string GetCustomUI(string ribbonID)
        {
            return GetResourceText("VisAssistDatabaseBackEnd.Ribbonxml.xml");
        }

        #endregion

        #region Ribbon Callbacks
        //Create callback methods here. For more information about adding callback methods, visit https://go.microsoft.com/fwlink/?LinkID=271226

        #endregion

        #region Helpers

        private static string GetResourceText(string resourceName)
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            string[] resourceNames = asm.GetManifestResourceNames();
            for (int i = 0; i < resourceNames.Length; ++i)
            {
                if (string.Compare(resourceName, resourceNames[i], StringComparison.OrdinalIgnoreCase) == 0)
                {
                    using (StreamReader resourceReader = new StreamReader(asm.GetManifestResourceStream(resourceNames[i])))
                    {
                        if (resourceReader != null)
                        {
                            return resourceReader.ReadToEnd();
                        }
                    }
                }
            }
            return null;
        }


        public void btnAddDatabase_Click(Office.IRibbonControl control)
        {
            //open and initialize the database
            DatabaseUtilities.InitializeDatabase("");

        }

        public void btnDeletePageInfo_Click(Office.IRibbonControl control)
        {
            //PageUtilities.DeleteAllPages();
        }

        public bool GetDuplicatePageEnabled(Office.IRibbonControl control)
        {
            return false;
        }

        public void OnCustomDuplicateSinglePage(Office.IRibbonControl control)
        {
            MessageBox.Show("The " + control.Id + " control has been clicked.");
        }

        public void OnCustomDuplicateMultiplePages(Office.IRibbonControl control)
        {
            MessageBox.Show("The " + control.Id + " control has been clicked.");

            PageUtilities.WhatPagesToDuplicate();
        }

        public void btnDeleteProjectInfo_Click(Office.IRibbonControl control)
        {

            ProjectUtilities.DeleteProject();


            //this just clears the project record from the table-user would never do this and we aren't giving them a place to do it 
            //ProjectUtilities.DeleteProjectInfo();
        }


        public void btnAddWireInfo_Click(Office.IRibbonControl control)
        {
            // ConnectionsUtilities.AddWireInfo();
        }


        public void btnGetPageName_Click(Office.IRibbonControl control)
        {
            //grab all the pages and put them in a datagridview 
            //for now let's build a datagridview of all the pages in just one file...
            Visio.Document ovDoc = Globals.ThisAddIn.Application.ActiveDocument;
            if (ovDoc != null)
            {

                string sVisAssistFolderPath = FileUtilities.GetFolderPath(ovDoc);
                if (sVisAssistFolderPath != "")
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

        public void btnDeleteDatabase_Click(Office.IRibbonControl control)
        {
            DatabaseUtilities.DeleteDatabase();
        }

        public void btnGetProjectInfo_Click(Office.IRibbonControl control)
        {

            Visio.Document ovDoc = Globals.ThisAddIn.Application.ActiveDocument;
            if (ovDoc != null)
            {
                string sVisAssistFolderPath = FileUtilities.GetFolderPath(ovDoc);
                if (sVisAssistFolderPath != "")
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

        public void btnGetFileData_Click(Office.IRibbonControl control)
        {
            Visio.Document ovDoc = Globals.ThisAddIn.Application.ActiveDocument;
            if (ovDoc != null)
            {
                string sVisAssistFolderPath = FileUtilities.GetFolderPath(ovDoc);
                if (sVisAssistFolderPath != "")
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

        public void btnAddProjectWithVisio_Click(Office.IRibbonControl control)
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

                }
                else
                {
                    //otherwise the user cancelled the project name...
                    //we need to delete the folder that we created because no file or project was added
                    string sProjectFolderPath = Path.GetDirectoryName(sFilePath).TrimEnd(Path.DirectorySeparatorChar);
                    string sVisAssistFolderPath = Path.GetDirectoryName(sProjectFolderPath).TrimEnd(Path.DirectorySeparatorChar);
                   
                    if (Directory.Exists(sVisAssistFolderPath))
                    {
                        //before deleting it i need to turn off the hidden attributes...
                        foreach(string sDir in Directory.GetDirectories(sVisAssistFolderPath, "*", SearchOption.AllDirectories))
                        {
                            File.SetAttributes(sDir, FileAttributes.Normal);
                        }
                        foreach(string sfile in Directory.GetFiles(sVisAssistFolderPath, "*", SearchOption.AllDirectories))
                        {
                            File.SetAttributes(sfile, FileAttributes.Normal);
                        }
                        File.SetAttributes(sVisAssistFolderPath, FileAttributes.Normal);
                        Directory.Delete(sVisAssistFolderPath, true); //delete recursively...
                    }
                }

            }
            //otherwise the user cancelled when picking a place to save the project to..



        }

        public void btnAddFile_Click(Office.IRibbonControl control)
        {
            //this will create the class b file and add it to an existing project
            //could either add the file to the existing doc's project
            //or could add a file to an existing project if the user points to save the file somewhere else...
            Visio.Document ovDoc = Globals.ThisAddIn.Application.ActiveDocument;
            if (ovDoc != null)
            {
                string sVisAssistFolderPath = FileUtilities.GetFolderPath(ovDoc);
                if (sVisAssistFolderPath != "")
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

        public void btnDeleteFile_Click(Office.IRibbonControl control)
        {
            Visio.Document ovDoc = Globals.ThisAddIn.Application.ActiveDocument;
            if (ovDoc != null)
            {
                string sVisAssistFolderPath = FileUtilities.GetFolderPath(ovDoc);
                if (sVisAssistFolderPath != "")
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




        public void btnCopyAnotherFile_Click(Office.IRibbonControl control)
        {
            Visio.Document ovDoc = Globals.ThisAddIn.Application.ActiveDocument;
            if (ovDoc != null)
            {
                string sVisAssistFolderPath = FileUtilities.GetFolderPath(ovDoc);
                if (sVisAssistFolderPath != "")
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


        public void btnChangeFileName_Click(Office.IRibbonControl control)
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
                                if (sFileName != sCurrentName) //make sure the file name is different than when it came in...
                                {
                                    //before we go and update the file name we need to check to make sure there isn't another file in the project with the same file name

                                    sFileName = FileUtilities.FormatFileName(sFileName);

                                    //get all the file names in this proejct 
                                    List<string> lstFileNames = FileUtilities.GetFileNamesInProject();
                                    if (!lstFileNames.Contains(sFileName, StringComparer.OrdinalIgnoreCase))
                                    {
                                        FileUtilities.UpdateFileName(sFileName);
                                    }

                                }

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

        public void btnOpenProject_Click(Office.IRibbonControl control)
        {
            ProjectUtilities.OpenProject();
        }

        public void btnOpenFile_Click(Office.IRibbonControl control)
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

        public void btnPageAndTerminals_Click(Office.IRibbonControl control)
        {
            PageUtilities.StressTest();

        }

        public void btnResetWireColorandNumber_Click(Office.IRibbonControl control)
        {
            WireUtilities.ResetWireColorAndNumber();
        }

        public void btnFirstStep_Click(Office.IRibbonControl control)
        {
            //this drops 100 pages and drops 10 wires on each page
            PageUtilities.FirstStepInStressTest(100, 10); 
        }

        public void btnSecondStep_Click(Office.IRibbonControl control)
        {
            PageUtilities.SecondStepInStressTest(50, 20);
            Globals.ThisAddIn.m_bAskWhereToCutTo = true;
        }

        public void btnSmallTestCaseSecond_Click(Office.IRibbonControl control)
        {
            int iUndoScope = Globals.ThisAddIn.Application.BeginUndoScope("Small Stress Test Step 2");
            PageUtilities.SecondStepInStressTest(5, 10);
            Globals.ThisAddIn.m_bAskWhereToCutTo = true;
            Globals.ThisAddIn.Application.EndUndoScope(iUndoScope, true);
            Globals.ThisAddIn.m_sLastUndoScope = "Stress Test";
        }

        public void btnSmallTestCaseFirst_Click(Office.IRibbonControl control)
        {
            //drop 10 pages 5 wires on each page (10 wires in total 5 pairs...)
            int iUndoScope = Globals.ThisAddIn.Application.BeginUndoScope("Small Stress Test Step 1");
            PageUtilities.FirstStepInStressTest(10, 5);
            Globals.ThisAddIn.Application.EndUndoScope(iUndoScope, true);
            Globals.ThisAddIn.m_sLastUndoScope = "Stress Test";
        }

      
        #endregion
    }
}
