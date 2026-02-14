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
using Visio = Microsoft.Office.Interop.Visio;

namespace VisAssistDatabaseBackEnd.Forms
{
    public partial class PagesForm : Form
    {
        public PagesForm()
        {
            InitializeComponent();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {

        }

        private void PagesForm_Load(object sender, EventArgs e)
        {

        }

        public void Display()
        {
            //add all the page names to the the dgvPages (we will be automatically adding the current page that we are duplicating..)
            PageUtilities.PopulatePagesForm(this);
          
        }

        private void btnDuplicate_Click(object sender, EventArgs e)
        {
            //select all the pages that the user is going to duplicate....
            PageUtilities.GatherPagesToDuplicate(this);
        }
    }
}
