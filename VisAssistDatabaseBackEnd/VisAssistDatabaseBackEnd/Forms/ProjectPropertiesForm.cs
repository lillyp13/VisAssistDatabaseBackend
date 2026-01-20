using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using VisAssistDatabaseBackEnd.DataUtilities;

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
        public void Display(bool bDoesProjectExist)
        {
           if(bDoesProjectExist)
            {
                //the project already exists
                ProjectUtilities.GetProjectInfoFromDatabase();
                ProjectUtilities.PopulatePropertiesForm(this);
            }
           else
            {
                //the project doesn't exist so we will be adding a new project to the db...

            }
            
            
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            //take a snapshot of what the project properties are now and then compare them with the original dictionary
            ProjectUtilities.UpdateProjectInfo(this);
        }
    }
}
