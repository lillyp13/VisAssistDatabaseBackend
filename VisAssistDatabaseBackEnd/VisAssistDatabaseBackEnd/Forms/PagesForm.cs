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
    public partial class PagesForm : Form
    {
        public PagesForm()
        {
            InitializeComponent();
        }

        private void PagesForm_Load(object sender, EventArgs e)
        {

        }

        internal void Display()
        {

        }
        public int m_iFileID = 0;
        public bool m_bAllPages;
        private void btnGetPages_Click(object sender, EventArgs e)
        {
            m_bAllPages = false;
            //based on the fileid in the txtfileid retrieve all the pages associated with that file...
            if (txtFileID.Text != "")
            {


                m_iFileID = Convert.ToInt32(txtFileID.Text);

                PageUtilities.GetPagesForSpecificFile(m_iFileID);
                PageUtilities.PopulatePagesForm(this);
            }
            else
            {
                MessageBox.Show("Please select a File ID.");
            }
        }

        private void btnUpdatePages_Click(object sender, EventArgs e)
        {
            //based on if we are doing pages for a specific file or not we need to reset the baserecord set
            PageUtilities.UpdatePage(this, m_bAllPages, m_iFileID);
        }

        private void btnGetAllPages_Click(object sender, EventArgs e)
        {
            m_bAllPages = true;
            //get all the pages for every file in the pages_table
            PageUtilities.GetAllPages();
            PageUtilities.PopulatePagesForm(this);
        }

        private void btnDeletePage_Click(object sender, EventArgs e)
        {
            //delete the page that is selected
            PageUtilities.DeletePage(this);
        }
    }
}
