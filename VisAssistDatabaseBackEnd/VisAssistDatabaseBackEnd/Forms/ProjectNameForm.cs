using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VisAssistDatabaseBackEnd.Forms
{
    public partial class ProjectNameForm : Form
    {
        public ProjectNameForm()
        {
            InitializeComponent();
        }

        public string sProjectName => txtProjectName.Text.Trim();
        public void Display()
        {

        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            if(txtProjectName.Text.Length > 0)
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show("Please pick a project name.", "VisAssist");
            }
        }

        private void ProjectNameForm_Load(object sender, EventArgs e)
        {

        }
    }
}
