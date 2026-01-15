using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisAssistDatabaseBackEnd.DataUtilities
{
    internal class FileUtilities
    {
        int iFileID;
        int iProjectID;
        int iRevisionID;
        string sFileName;
        string sFilePath;
        DateTime dtCreatedDate;
        DateTime dtLastModifiedDate;
        string sVersion;
        string sClass;
        string sDrawingType;
        string sWirePrefix;
        bool bIgnoreWireColor;
        bool bAllowDuplicateTags;
        bool bShowPointData;
        static SQLiteConnection Connection = ConnectionsUtilities.Connection;



        string sFileNumber; //for pageformat and fileformat...



        //File Actions
        internal static void AddFile()
        {
            DatabaseSeeding.SeedFiles();
        }
        internal static void DeleteFile()
        {
            //delete all the records in the files_table
            using (SQLiteConnection connection = new SQLiteConnection(Connection))
            {
                connection.Open();
                string sDelete = "DELETE FROM files_table;";

                new SQLiteCommand(sDelete, connection).ExecuteNonQuery();

                //reset the auto-increment counter
                string sReset = "DELETE FROM sqlite_sequence WHERE name = 'files_table';";
                new SQLiteCommand(sReset, connection).ExecuteNonQuery();
            }
        }

    }
}
