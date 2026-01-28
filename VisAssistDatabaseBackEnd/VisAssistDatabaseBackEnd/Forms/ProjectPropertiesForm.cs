using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using VisAssistDatabaseBackEnd.DataUtilities;
using Visio = Microsoft.Office.Interop.Visio;

namespace VisAssistDatabaseBackEnd.Forms
{
    public partial class ProjectPropertiesForm : Form
    {
        public ProjectPropertiesForm()
        {
            InitializeComponent();
        }

        private void ProjectPropertiesForm_Load(object sender, EventArgs e)
        {

        }

        public string m_sAction = "";
        public string m_sFilePath = "";
        public void Display(string sAction, string sProjectName, string sFilePath)
        {
            m_sAction = sAction;
            m_sFilePath = sFilePath;
            if(sAction == "Add")
            {
                //the database doesn't exist yet open the form 
                //ConnectionsUtilities.InitializeDatabase();
                //ProjectUtilities.AddProjectInfo(this);
                txtProjectName.Text = sProjectName;
                ShowDialog();
                
            }
            else
            {
                if(sAction == "Update")
                {
                    //the project already exists
                    Visio.Document ovDoc = Globals.ThisAddIn.Application.ActiveDocument;
                    bool bAssignedToProject = FileUtilities.IsFileAssignedToProject(ovDoc);

                    if (bAssignedToProject)
                    {
                        FileUtilities.AdjustFileCount(ovDoc);
                        ProjectUtilities.GetProjectInfoFromDatabase();
                        ProjectUtilities.PopulatePropertiesForm(this);
                        ShowDialog();
                    }
                    else
                    {
                        MessageBox.Show("This file is not associated with a project.", "VisAssist");
                    }
                }
            }
            



        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            //we either need to update or do an insert if we are adding a project for the first time 
            //take a snapshot of what the project properties are now and then compare them with the original dictionary
        
            if (m_sAction == "Update")
            {
                ProjectUtilities.UpdateProjectInfo(this);
            }
            else
            {
                if(m_sAction == "Add")
                {
                    ProjectUtilities.AddNewProject(this, m_sFilePath);
                    
                     
                    m_sAction = "Update"; //change this to update after adding the project (i think we just close the form so not sure we need to do this...)
                }
            }

           
            this.Close();
            
        }

        private void ProjectPropertiesForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            //the user is closing the form, check to see if we were going to be adding a project and if we are we need to clean up the file structrue that was created...
            if (m_sAction == "Add")
            {
                //then remove the file structure... 
                string sDirectory = Path.GetDirectoryName(m_sFilePath);
                if (Directory.Exists(sDirectory))
                {
                    Directory.Delete(sDirectory, true); //delete recursively...
                }
            }
        }
    }
}
