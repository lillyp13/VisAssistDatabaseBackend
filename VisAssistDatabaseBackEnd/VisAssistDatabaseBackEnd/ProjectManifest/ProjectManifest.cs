using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using VisAssistDatabaseBackEnd.DataUtilities;

namespace VisAssistDatabaseBackEnd.Project_Manifest
{
    internal class ProjectManifest
    {
        static string sApplicationName = "VisAssist";
        static string sVersion = "1.0.0";
        static string sCreatedBy = "VisAssist Application";
        static string sProjectName;
        static string sProjectID;
        static string sImportantNote = "This manifest is auto-generated. Do not modify or delete otherwise, the application will become unstable and system errors will occur.";
        static string sProjectPath; //path to the VisAssist project directory
        static string sManifestDirectoryName = ".visassist";
        static string sManifestFileName = "visassist.json";

        private ProjectManifest()
        {

        }

        public static void CreateManifest(string sProjectName, string sProjectID, string sProjectPath)
        {
            //Logging added here to track manifest creation issues

            try
            {
                ProjectManifest.sProjectName = sProjectName;
                ProjectManifest.sProjectPath = sProjectPath;
                ProjectManifest.sProjectID = sProjectID;

                string sManifestDirectoryPath = Path.Combine(sProjectPath, sManifestDirectoryName);
                Directory.CreateDirectory(sManifestDirectoryPath);
                ProtectManifestDirectory(sManifestDirectoryPath);
                CreateManifestFile();

                //Logging: Manifest created successfully
            }
            catch (Exception ex)
            {
                //Logging: Manifest creation failed

                throw new Exception("Failed to create manifest directory: " + ex.Message);
            }
        }

        public static void CheckForManifestIntegrity(string sProjectPath)
        {
            //Logging added here

            try
            {
                DatabaseConfig.BindToActiveDocument(sProjectPath);
                string sProjectName = ProjectUtilities.GetColumnInfoInProjectTableFromDatabase("ProjectName");
                string sProjectID = ProjectUtilities.GetColumnInfoInProjectTableFromDatabase("ProjectID");

                bool bManifestValid = ManifestValidations(sProjectPath);
                if (!bManifestValid)
                {
                    bool bManifestDirectoryExists = ManifestDirectoryExists(sProjectPath);
                    if(bManifestDirectoryExists)
                    {
                        DeleteManifestDirectory(sProjectPath);
                    }
                    
                    CreateManifest(sProjectName, sProjectID, sProjectPath);
                }

                //Logging: Manifest integrity check passed
            }
            catch (Exception ex)
            {
                //Logging: Manifest integrity check failed

                throw new Exception("Failed while checking manifest integrity: " + ex.Message);
            }

        }

        public static bool ManifestValidations(string sProjectPath)
        {
            //Logging added here

            string sManifestDirectoryPath = Path.Combine(sProjectPath, sManifestDirectoryName);
            bool bManifestDirectoryExists = ManifestDirectoryExists(sProjectPath);

            try
            {
                if (bManifestDirectoryExists)
                {
                    bool bManifestFileExists = ManifestFileExists(sManifestDirectoryPath);

                    if (bManifestFileExists)
                    {
                        //we can only read manifestfile if the file exts....using the file to check if it is a visassist project but if they don't exist we need to abort earleir....
                        Dictionary<string, string> oDictManifestData = ReadManifestFile(sProjectPath);
                        bool bIsValidVisAssistProject = IsVisAssistProject(oDictManifestData);

                        if (bIsValidVisAssistProject)
                        {
                            //the manifest file exists and is valid, so we can return

                            //Logging: Manifest structure is valid

                            return true;
                        }

                        //Logging: Manifest file is invalid

                        return false;
                    }

                    //Logging: Manifest file does not exist

                    return false;
                }

                //Logging: Manifest directory does not exist

                return false;
            }
            catch (Exception ex)
            {
                //Logging: Manifest structure validation failed

                throw new Exception("Failed while validating Manifest structure." + ex.Message);
            }
        }

        public static bool ManifestDirectoryExists(string sProjectPath)
        {
            //Logging added here

            string sManifestDirectoryPath = Path.Combine(sProjectPath, sManifestDirectoryName);
            return Directory.Exists(sManifestDirectoryPath);
        }

        public static bool ManifestFileExists(string sManifestDirectoryPath)
        {
            //Logging added here

            string sManifestFilePath = Path.Combine(sManifestDirectoryPath, sManifestFileName);
            return File.Exists(sManifestFilePath);
        }

        public static bool IsVisAssistProject(Dictionary<string, string> oDictManifestData)
        {
            //Logging added here

            try
            {
                if (oDictManifestData == null || oDictManifestData.Count == 0) return false;
                if (oDictManifestData.ContainsKey("ApplicationName") && oDictManifestData["ApplicationName"] == sApplicationName)
                {
                    if (oDictManifestData.ContainsKey("ProjectID") && !string.IsNullOrEmpty(oDictManifestData["ProjectID"]))
                    {
                        string sDbProjectId = ProjectUtilities.GetColumnInfoInProjectTableFromDatabase("ProjectID");
                        string sManifestProjectId = oDictManifestData["ProjectID"].ToString();
                        if (sDbProjectId == sManifestProjectId)
                        {
                            //Logging: Manifest ProjectId matches the Database ProjectId
                            return true;
                        }

                        //Logging: Manifest ProjectId does not match the Database ProjectId

                        return false;
                    }

                    //Logging: Manifest ProjectId is missing or invalid

                    return false;
                }

                //Logging: Manifest does not correspond to a valid VisAssist project

                return false;
            }
            catch (Exception ex)
            {
                //Logging: Failed to validate VisAssist project
                throw new Exception("Failed to validate VisAssist project from manifest data: " + ex.Message);
            }
        }

        public static Dictionary<string, string> ReadManifestFile(string sProjectFolderPath)
        {
            // Using the project folder path here so that this method can be called from anywhere

            //Logging added here

            try
            {
                string sManifestFilePath = Path.Combine(sProjectFolderPath, sManifestDirectoryName, sManifestFileName);
                string jsonString = File.ReadAllText(sManifestFilePath);
                var data = JsonConvert.DeserializeObject<dynamic>(jsonString);

                //return a ProjectManifest dictionary populated with the data from the JSON file
                Dictionary<string, string> oDictManifestData = new Dictionary<string, string>();

                if (data == null)
                {
                    //Logging: Manifest file is empty or invalid
                    return oDictManifestData;
                }

                foreach (var item in data)
                {
                    oDictManifestData.Add(item.Name, item.Value.ToString());
                }

                //Logging: Manifest file read successfully

                return oDictManifestData;
            }
            catch (Exception ex)
            {
                //Logging: Failed to read manifest file
                throw new Exception("Failed to read manifest file: " + ex.Message);
            }
        }

        private static void ProtectManifestDirectory(string sManifestDirectoryPath)
        {
            //Logging added here

            try
            {
                //set the directory as hidden
                File.SetAttributes(sManifestDirectoryPath, File.GetAttributes(sManifestDirectoryPath) | FileAttributes.Hidden);

                //Logging: Manifest directory protected
            }
            catch (Exception ex)
            {
                //Logging: Failed to protect manifest directory
                throw new Exception("Failed to protect manifest directory: " + ex.Message);
            }
        }

        public static void DeleteManifestDirectory(string sProjectPath)
        {
            //Logging added here

            try
            {
                string sManifestDirectoryPath = Path.Combine(sProjectPath, sManifestDirectoryName);
                Directory.Delete(sManifestDirectoryPath, true);

                //Logging: Manifest directory deleted
            }
            catch (Exception ex)
            {
                //Logging: Failed to delete manifest directory
                throw new Exception("Failed to delete manifest directory: " + ex.Message);
            }
        }

        public static void CreateManifestFile()
        {
            //Logging added here

            try
            {
                string sManifestFilePath = Path.Combine(sProjectPath, sManifestDirectoryName, sManifestFileName);
                File.Create(sManifestFilePath).Dispose();

                WriteJsonFile(sManifestFilePath);

                //Logging: Manifest file created successfully
            }
            catch (Exception ex)
            {
                //Logging: Failed to create manifest file
                throw new Exception("Failed to create manifest file: " + ex.Message);
            }
        }

        private static void WriteJsonFile(string sManifestFilePath)
        {
            //Logging added here

            try
            {
                var data = new
                {
                    ApplicationName = sApplicationName,
                    Version = sVersion,
                    ProjectID = sProjectID,
                    CreatedOn = DateTime.Parse(DateTime.Now.ToString()),
                    CreatedBy = sCreatedBy,
                    ImportantNote = sImportantNote
                };

                string sjsonString = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(sManifestFilePath, sjsonString);

                //Logging: JSON written to manifest file successfully
            }
            catch (Exception ex)
            {
                //Logging: Failed to write JSON to manifest file
                throw new Exception("Failed to write JSON to manifest file: " + ex.Message);
            }
        }
    }
}
