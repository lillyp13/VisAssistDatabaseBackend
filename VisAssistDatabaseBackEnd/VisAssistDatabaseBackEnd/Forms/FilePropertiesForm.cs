using System;
using System.Collections.Generic;
using System.Windows.Forms;
using VisAssistDatabaseBackEnd.DataUtilities;

namespace VisAssistDatabaseBackEnd.Forms
{
    public partial class FilePropertiesForm : Form
    {
        public FilePropertiesForm()
        {
            InitializeComponent();
        }

        private void FilePropertiesForm_Load(object sender, EventArgs e)
        {

        }
        public void Display()
        {
            FileUtilities.GetFileDataFromDatabase(this);
            //first we check to see if all the files in the db exist in the file structure
            bool bGetFileDataAgain = FileUtilities.CheckThatFilesExistInFolder();
            if (bGetFileDataAgain)
            {
                FileUtilities.GetFileDataFromDatabase(this);
            }

            //check the opposite, we have checked to see that all of the files in the db have a visio file, but now we want to check if the visio file has a file id and exists in the db...
            List<string> oListFilesDontExist = FileUtilities.CheckThatFileExistsInDatabase();
            if(oListFilesDontExist.Count > 0)
            {
                //message listing all the files that are in the filestrucure but are not in the database
                string sMessage = "The following files exist in the folder but are not in the database:\n\n" + string.Join("\n", oListFilesDontExist);

                MessageBox.Show(sMessage, "Missing Files", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            FileUtilities.PopulateFilePropertiesForm(this);
        }

        private void btnDeleteFile_Click(object sender, EventArgs e)
        {
            //call delete file for the specified file in the datagridivew...
            FileUtilities.DeleteFile(this);
        }
    }
}
