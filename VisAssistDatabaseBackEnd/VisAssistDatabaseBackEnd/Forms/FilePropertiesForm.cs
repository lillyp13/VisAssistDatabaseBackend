using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
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
            FileUtilities.PopulateFilePropertiesForm(this);
        }

        private void btnAddFile_Click(object sender, EventArgs e)
        {
            //this just adds a practice add new file
            //FileUtilities.AddFile(this);
            
        }

        private void btnUpdateFile_Click(object sender, EventArgs e)
        {
            FileUtilities.UpdateFile(this);
        }

        private void btnDeleteFile_Click(object sender, EventArgs e)
        {
            FileUtilities.DeleteFile(this);
        }
    }
}
