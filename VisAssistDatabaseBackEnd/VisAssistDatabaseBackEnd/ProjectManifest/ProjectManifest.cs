using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace VisAssistDatabaseBackEnd.Project_Manifest
{
    internal class ProjectManifest
    {
        static string sApplicationName = "VisAssist";
        static string sVersion = "1.0.0";
        static string sCreatedBy = "VisAssist Application";
        static string sProjectName;
        static string sProjectId;
        static string sImportantNote = "This manifest is auto-generated. Do not modify or delete otherwise, the application will become unstable and system errors will occur.";
        static string sProjectPath; //path to the VisAssist project directory
        static string sManifestDirectoryName = ".visassist";
        static string sManifestFileName = "visassist.json";

        private ProjectManifest()
        {

        }

        public static void CreateManifest(string projectName, string projectId, string projectPath)
        {
            //Logging added here to track manifest creation issues

            try
            {
                sProjectName = projectName;
                sProjectPath = projectPath;
                sProjectId = projectId;

                string sManifestDirectoryPath = Path.Combine(projectPath, sManifestDirectoryName);
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

        public static void CheckForManifestIntegrity(string projectName, string projectId, string projectPath)
        {
            //Logging added here

            try
            {
                bool bManifestValid = ManifestValidations(projectPath);
                if (!bManifestValid)
                {
                    DeleteManifestDirectory(projectPath);
                    CreateManifest(projectName, projectId, projectPath);
                }

                //Logging: Manifest integrity check passed
            }
            catch (Exception ex)
            {
                //Logging: Manifest integrity check failed

                throw new Exception("Failed while checking manifest integrity: " + ex.Message);
            }

        }

        public static bool ManifestValidations(string projectPath)
        {
            //Logging added here

            string sManifestDirectoryPath = Path.Combine(projectPath, sManifestDirectoryName);
            bool bManifestDirectoryExists = ManifestDirectoryExists(projectPath);
            bool bManifestFileExists = ManifestFileExists(sManifestDirectoryPath);
            Dictionary<string, string> dictManifestData = ReadManifestFile(projectPath);
            bool bIsValidVisAssistProject = IsVisAssistProject(dictManifestData);

            try
            {
                if (bManifestDirectoryExists)
                {
                    if (bManifestFileExists)
                    {
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

        public static bool ManifestDirectoryExists(string projectPath)
        {
            //Logging added here

            string sManifestDirectoryPath = Path.Combine(projectPath, sManifestDirectoryName);
            return Directory.Exists(sManifestDirectoryPath);
        }

        public static bool ManifestFileExists(string manifestDirectoryPath)
        {
            //Logging added here

            string sManifestFilePath = Path.Combine(manifestDirectoryPath, sManifestFileName);
            return File.Exists(sManifestFilePath);
        }

        public static bool IsVisAssistProject(Dictionary<string, string> manifestData)
        {
            //Logging added here

            try
            {
                if (manifestData == null || manifestData.Count == 0) return false;
                if (manifestData.ContainsKey("ApplicationName") && manifestData["ApplicationName"] == sApplicationName)
                {
                    if (manifestData.ContainsKey("ProjectId") && !string.IsNullOrEmpty(manifestData["ProjectId"]))
                        //Logging: Manifest corresponds to a valid VisAssist project
                        return true;
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

        public static Dictionary<string, string> ReadManifestFile(string projectFolderPath)
        {
            // Using the project folder path here so that this method can be called from anywhere

            //Logging added here

            try
            {
                string sManifestFilePath = Path.Combine(projectFolderPath, sManifestDirectoryName, sManifestFileName);
                string jsonString = File.ReadAllText(sManifestFilePath);
                var data = JsonConvert.DeserializeObject<dynamic>(jsonString);

                //return a ProjectManifest dictionary populated with the data from the JSON file
                Dictionary<string, string> manifestData = new Dictionary<string, string>();
                foreach (var item in data)
                {
                    manifestData.Add(item.Name, item.Value.ToString());
                }

                //Logging: Manifest file read successfully

                return manifestData;
            }
            catch (Exception ex)
            {
                //Logging: Failed to read manifest file
                throw new Exception("Failed to read manifest file: " + ex.Message);
            }
        }

        private static void ProtectManifestDirectory(string manifestDirectoryPath)
        {
            //Logging added here

            try
            {
                //set the directory as hidden
                File.SetAttributes(manifestDirectoryPath, File.GetAttributes(manifestDirectoryPath) | FileAttributes.Hidden);

                //Logging: Manifest directory protected
            }
            catch (Exception ex)
            {
                //Logging: Failed to protect manifest directory
                throw new Exception("Failed to protect manifest directory: " + ex.Message);
            }
        }

        public static void DeleteManifestDirectory(string projectPath)
        {
            //Logging added here

            try
            {
                string sManifestDirectoryPath = Path.Combine(projectPath, sManifestDirectoryName);
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

        private static void WriteJsonFile(string manifestFilePath)
        {
            //Logging added here

            try
            {
                var data = new
                {
                    ApplicationName = sApplicationName,
                    Version = sVersion,
                    ProjectId = sProjectId,
                    CreatedOn = DateTime.Parse(DateTime.Now.ToString()),
                    CreatedBy = sCreatedBy,
                    ImportantNote = sImportantNote
                };

                string jsonString = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(manifestFilePath, jsonString);

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
