using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VisAssistDatabaseBackEnd.DataUtilities
{
    internal class PageUtilities
    {
        string sPageName;
        int iPageID;
        int iProjectID;
        int iFileID;
        int iPageIndex;
        DateTime dtCreatedDate;
        DateTime dtLastModifiedDate;
        string sVersion;
        string sClass;
        string sOrientation;
        string sScale;
        static SQLiteConnection Connection = ConnectionsUtilities.Connection;


        string sPageNumber; //for pageformat...




        //Page Actions
        internal static void AddPage()
        {
            DatabaseSeeding.SeedPages();
        }
        internal static void UpdatePage()
        {
            DatabaseSeeding.UpdatePageInfoWithSeedData();
        }
        internal static void DeletePage()
        {
            //delete all the records in the pages_table
            using (SQLiteConnection sqliteConnection = new SQLiteConnection(Connection))
            {
                sqliteConnection.Open();
                string sDelete = "DELETE FROM pages_table;";

                using (SQLiteCommand cmd = new SQLiteCommand(sDelete, sqliteConnection))
                {
                    cmd.ExecuteNonQuery();
                }

                //reset the auto-increment counter
                string sReset = "DELETE FROM sqlite_sequence WHERE name = 'pages_table';";
                using (SQLiteCommand cmd = new SQLiteCommand(sReset, sqliteConnection))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }
        internal static void GetPageName()
        {
            string sPageName = DatabaseSeeding.GetPageNameWithSeedData();
            MessageBox.Show("Got the page " + sPageName);

        }
    }

}
