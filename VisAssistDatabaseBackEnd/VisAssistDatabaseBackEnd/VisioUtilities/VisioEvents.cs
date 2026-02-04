

using Microsoft.Office.Interop.Visio;
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using VisAssistDatabaseBackEnd.Project_Manifest;
using Visio = Microsoft.Office.Interop.Visio;
using VisAssistDatabaseBackEnd.DataUtilities;

namespace VisAssistDatabaseBackEnd.VisioUtilities
{

    /// <summary>A COM-visible Visio event handler.</summary>
    /// <remarks>
    /// This class provides a COM-visible handler for Visio events.  
    /// This is used in cases where Visio events can't be accessed directly
    /// with an AddHandler call.
    /// </remarks>
    [System.Runtime.InteropServices.ComVisible(true)]
    public class VisioEventSink : Visio.IVisEventProc
    {

        #region delegates

        public delegate bool VisioEventDelegate(
            short eventCode,
            object source,
            int eventID,
            int eventSequenceNumber,
            object subject,
            object moreInformation);

        private VisioEventDelegate m_Callback;

        #endregion

        #region construction / destruction

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="callback"></param>
        public VisioEventSink(
            VisioEventDelegate callback)
        {
            m_Callback = callback;
        }

        #endregion

        #region methods

        /// <summary>
        /// The event-notification function from Visio.
        /// </summary>
        /// <param name="eventCode"></param>
        /// <param name="source"></param>
        /// <param name="eventID"></param>
        /// <param name="eventSequenceNumber"></param>
        /// <param name="subject"></param>
        /// <param name="moreInformation"></param>
        /// <returns></returns>
        public object VisEventProc(
                short eventCode,
                object source,
                int eventID,
                int eventSequenceNumber,
                object subject,
                object moreInformation)
        {
            return m_Callback?.Invoke(
                eventCode,
                source,
                eventID,
                eventSequenceNumber,
                subject,
                moreInformation) ?? true;
        }

        private VisioEventSink m_VisioEventSink = null;
        private VisioEventSink m_VisioAppEventSink = null;
        // public Application m_application;


        #endregion
    }


    /// <summary>
    /// Class with static methods for use during Visio event-handling.
    /// </summary>
    internal sealed partial class VisioEvents
    {
        #region constants

        public const short visEvtAdd = -0x8000;

        #endregion

        #region GetVisioObjectFromAddonString

        /// <summary>
        /// This function returns the Visio object references that are called out in the event info string of a
        /// Visio event.
        /// </summary>
        /// <param name="eventInfoFromVisio">
        ///	The event info string from Visio.
        ///	</param>
        /// <param name="visioApplication">
        ///	The Visio application object.
        ///	</param>
        /// <param name="visioDocument">
        ///	A reference to a Visio document object that will be assigned by this function.
        ///	</param>
        /// <param name="visioPage">
        ///	A reference to a Visio page object that will be assigned by this function.
        ///	</param>
        /// <param name="visioShape">
        ///	A reference to a Visio shape object that will be assigned by this function.
        ///	</param>
        static public bool GetObjectsFromAddonString(
            string eventInfoFromVisio,
            Visio.Application visioApplication,
            out Visio.Document visioDocument,
            out Visio.Page visioPage,
            out Visio.Shape visioShape,
            out string sPartNumber,
            out string sManufacturer)
        {
            if (visioApplication == null)
            {
                throw new ArgumentNullException("visioApplication");
            }

            // intialize the out parameters values
            visioDocument = null;
            visioPage = null;
            visioShape = null;
            sPartNumber = "";
            sManufacturer = "";


            // initialize locals
            string docArgValue = null;
            string pageArgValue = null;
            string shapeArgValue = null;
            string shapeUArgValue = null;

            if (eventInfoFromVisio.Length == 0)
            {
                // return failure
                return false;
            }

            // init return value
            bool retVal = false;

            try
            {
                // split the info
                if (eventInfoFromVisio.StartsWith("/"))
                {
                    eventInfoFromVisio = eventInfoFromVisio.Substring(1);
                }
                string[] cmdLineArguments = eventInfoFromVisio.Split('/');

                // put the leading / back into the arguments.  This is mainly for compatibility
                // purposes since we have existing code that expects it.
                for (int arrayIndex = 0; arrayIndex < cmdLineArguments.Length; arrayIndex++)
                {
                    cmdLineArguments.SetValue("/" + cmdLineArguments.GetValue(arrayIndex).ToString().Trim(), arrayIndex);
                }

                // find the argument values
                if (cmdLineArguments != null &&
                    cmdLineArguments.GetLength(0) > 0)
                {
                    // we have at least one argument, see what they are
                    foreach (string arg in cmdLineArguments)
                    {
                        if (arg.StartsWith("/doc="))
                        {
                            // doc arg found
                            docArgValue = arg.Replace("/doc=", string.Empty);
                        }
                        if (arg.StartsWith("/page="))
                        {
                            // page arg found
                            pageArgValue = arg.Replace("/page=", string.Empty);
                        }
                        if (arg.StartsWith("/shapeu"))
                        {
                            // shapeu arg found
                            shapeUArgValue = arg.Replace("/shapeu=", string.Empty);
                        }
                        if (arg.StartsWith("/shape="))
                        {
                            // shape arg found
                            shapeArgValue = arg.Replace("/shape=", string.Empty);
                        }

                        if (arg.StartsWith("/partNumber="))
                        {
                            // shape arg found
                            shapeArgValue = arg.Replace("/partNumber=", string.Empty);
                            sPartNumber = shapeArgValue;
                        }

                        if (arg.StartsWith("/mfg="))
                        {
                            // shape arg found
                            shapeArgValue = arg.Replace("/mfg=", string.Empty);
                            sManufacturer = shapeArgValue;
                        }

                    }
                }

                // if shapeU arg is found then it becomes the shapeArgValue so that the appropriate shape is found
                if (shapeUArgValue != null &&
                    shapeUArgValue.Length > 0)
                {
                    shapeArgValue = shapeUArgValue;
                }

                // get the objects based on the arguments
                if (docArgValue != null &&
                    docArgValue.Length > 0 &&
                    visioApplication.Documents.Count > 0)
                {
                    // this is an index value
                    double doubleValue;
                    bool isNumber = double.TryParse(
                        docArgValue,
                        System.Globalization.NumberStyles.Any,
                        null,
                        out doubleValue);

                    if (isNumber)
                    {
                        int docIndex = Int32.Parse(docArgValue, System.Globalization.CultureInfo.InvariantCulture);
                        visioDocument = visioApplication.Documents[docIndex];
                    }
                }

                if (pageArgValue != null &&
                    pageArgValue.Length > 0 &&
                    visioDocument != null &&
                    visioDocument.Pages.Count > 0)
                {
                    // this is an index value
                    double doubleValue;
                    bool isNumber = double.TryParse(
                        docArgValue,
                        System.Globalization.NumberStyles.Any,
                        null,
                        out doubleValue);

                    if (isNumber)
                    {
                        int pageIndex = Int32.Parse(pageArgValue, System.Globalization.CultureInfo.InvariantCulture);
                        visioPage = visioDocument.Pages[pageIndex];
                    }
                }

                if (shapeArgValue != null &&
                    shapeArgValue.Length > 0 &&
                    visioPage != null &&
                    visioPage.Shapes.Count > 0)
                {
                    visioShape = visioPage.Shapes.get_ItemU(shapeArgValue);
                }

                // set success
                retVal = true;
            }
            catch (System.Runtime.InteropServices.COMException)
            {
                // here we will ignore any exceptions because what most likely is that cause is we are asking for
                // a visio object that no longer exists
                retVal = false;
            }

            // default return
            return retVal;
        }

        #endregion





    }

    public static class VisioHelper
    {

        public async static void OnDocumentOpenedCreated(
          Visio.Document ovThisVisioDocument,
          bool newDrawing)
        {

            try
            {
                //going to see if this is a launch file...
                string sClass = "";
                if (ovThisVisioDocument.DocumentSheet.CellExists["User.Class", 0] == -1)
                {
                    var AddIn = Globals.ThisAddIn;
                    AddIn.StartSinksForDoc(ovThisVisioDocument);


                    //this is a VisAssist project
                    sClass = ovThisVisioDocument.DocumentSheet.Cells["User.Class"].get_ResultStr(0);
                    string sVisAssistFolderPath = FileUtilities.GetFolderPath(ovThisVisioDocument);

                    if (sClass == "Launch")
                    {
                        //we have a launch file
                        //we want to open the file form for this project...
                        
                        FileUtilities.PopulateProjectFilesDictionaryBasedOnDirectory(sVisAssistFolderPath);
                        FileUtilities.OpenFileForm("Launch");
                    }
                    
                    ProjectManifest.CheckForManifestIntegrity(sVisAssistFolderPath);

                }
                //otherwise this is not a visassist project...






            }
            catch (COMException ex)
            {
                Console.WriteLine($"COM Exception: {ex.Message}");
                //VisAssistCommon.Logging.LogMsg("OnDocumentOpenedCreated for: " + ovThisVisioDocument.Name, "Error - Delete");
            }




        }

        public async static void OnDocumentOpened(
          Visio.Document ovThisVisioDocument,
          bool newDrawing)
        {

            try
            {
                //going to see if this is a launch file...
                string sClass = "";
                if (ovThisVisioDocument.DocumentSheet.CellExists["User.Class", 0] == -1)
                {
                    var AddIn = Globals.ThisAddIn;
                    AddIn.StartSinksForDoc(ovThisVisioDocument);




                }
                //otherwise this is not a visassist project...


            }
            catch (COMException ex)
            {
                Console.WriteLine($"COM Exception: {ex.Message}");
            }





        }
    }

}

