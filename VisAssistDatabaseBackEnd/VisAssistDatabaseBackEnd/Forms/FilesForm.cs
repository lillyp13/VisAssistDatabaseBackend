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
    public partial class FilesForm : Form
    {
        public FilesForm()
        {
            InitializeComponent();
        }

        private void FilesForm_Load(object sender, EventArgs e)
        {

        }

        public Dictionary<string, string> m_dictFiles = new Dictionary<string, string>();
        public string m_sSource;
        internal void Display(string sSource)
        {
            FileUtilities.PopulateFilesForm(this);
            m_sSource = sSource;
            //m_dictFiles = oDictFiles;
            //dgvFiles.Rows.Clear();
            ////populate the dgvFiles with the file names in the oDictFiles 
            //foreach (string sFileName in oDictFiles.Keys)
            //{
            //    dgvFiles.Rows.Add(sFileName);
            //}
        }

        private void btnOpen_Click(object sender, EventArgs e)
        {
            //if the user selected a row in the dgv open that file...
            if (m_sSource == "Copy")
            {
                if (dgvFiles.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Please select a file to copy.", "VisAssist");
                    return;
                }
                DataGridViewRow dgvSelectedRow = dgvFiles.SelectedRows[0];
                string sFileName = dgvSelectedRow.Cells[0].Value?.ToString();

                //we are not opening the file we are going to copy it
                string sDocName = FileUtilities.CopyFile(sFileName);
                if (sDocName == "")
                {
                    MessageBox.Show("Could not copy file " + sFileName, "VisAssist");
                }
                else
                {
                    MessageBox.Show("Successfully copied the file " + sDocName + ".", "VisAssist");
                }

            }
            else
            {
                if (dgvFiles.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Please select a file to open.", "VisAssist");
                    return;
                }
                DataGridViewRow dgvSelectedRow = dgvFiles.SelectedRows[0];
                string sFileName = dgvSelectedRow.Cells[0].Value?.ToString();

                FileUtilities.OpenFile(sFileName, m_sSource);
                
            }

            this.Close();

        }

        private void dgvFiles_DoubleClick(object sender, EventArgs e)
        {
            //the user double clicked on a row open that file...
        }
    }
}
