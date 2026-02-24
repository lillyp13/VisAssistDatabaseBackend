using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using VisAssistDatabaseBackEnd.DataUtilities;
using VisAssistDatabaseBackEnd.VisioUtilities;
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
            if (m_sAction == "Duplicate")
            {
                this.Close();
            }
            else
            {
                if (m_sAction == "Move")
                {
                    //we need to paste the shapes back to where they were...
                    Visio.Page ovPage = Globals.ThisAddIn.Application.ActivePage;
                    ovPage.Paste();
                }
            }
        }

        private void PagesForm_Load(object sender, EventArgs e)
        {

        }
        string m_sAction = "";
        public void Display(string sAction)
        {
            //add all the page names to the the dgvPages (we will be automatically adding the current page that we are duplicating..)
            PageUtilities.PopulatePagesForm(this);
            m_sAction = sAction;
            if (m_sAction == "Duplicate")
            {
                btnDuplicate.Text = "Duplicate";
            }
            else
            {

                if (m_sAction == "MoveShapes")
                {
                    btnDuplicate.Text = "Move";
                }
            }
        }

        private void btnDuplicate_Click(object sender, EventArgs e)
        {
            //check the source if we are moving or duplicating...
            if (m_sAction == "Duplicate")
            {
                //select all the pages that the user is going to duplicate....
                PageUtilities.GatherPagesToDuplicate(this);
            }
            else
            {
                if (m_sAction == "MoveShapes")
                {
                    //get the current selection cut it and paste it to the page the user decided
                    PageUtilities.CutAndPasteShapes(this);
                }

            }

            this.Dispose();

            this.Close();

        }
    }
}
