using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;

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
            /*
             We need to do a number of checks here:
            1. Check if the manifest directory exists
                    a. If it does, check if the manifest file exists
                        i. If it does, check if it's valid
                            - If it's valid, return. Validity requires checking for the required fields ProjectId & ApplicationName. ProjectId must match the provided projectId.
                            - If it's not valid, delete the manifest directory and recreate it
                        ii. If it doesn't, delete the manifest directory and recreate it
                    b. If it doesn't, create the manifest directory and the manifest file

            These checks will likely need to be broken up into separate methods for clarity and maintainability.
            Here is a possible breakdown:
                1. ManifestDirectoryExists(string projectPath): bool
                2. ManifestFileExists(string manifestDirectoryPath): bool
                3. IsManifestFileValid(string manifestFilePath): bool
             */
            try
            {
                sProjectName = projectName;
                sProjectPath = projectPath;
                sProjectId = projectId;
                bool bManifestValid = ManifestValidations(projectPath);

                if (bManifestValid)
                {
                    //the manifest file exists and is valid, so we can return
                    return;
                }

                //if we reach here, we need to create the manifest directory and file
                string manifestDirectoryPath = Path.Combine(projectPath, sManifestDirectoryName);
                Directory.CreateDirectory(manifestDirectoryPath);
                ProtectManifestDirectory(manifestDirectoryPath);
                CreateManifestFile();
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to create manifest directory: " + ex.Message);
            }
        }

        public static bool ManifestValidations(string projectPath)
        {
            string manifestDirectoryPath = Path.Combine(projectPath, sManifestDirectoryName);
            bool bManifestDirectoryExists = ManifestDirectoryExists(projectPath);
            bool bManifestFileExists = ManifestFileExists(manifestDirectoryPath);
            Dictionary<string, string> dictManifestData = ReadManifestFile(projectPath);
            bool isValidVisAssistProject = IsVisAssistProject(dictManifestData);

            try
            {
                if (bManifestDirectoryExists)
                {
                    if (bManifestFileExists)
                    {
                        if (isValidVisAssistProject)
                        {
                            //the manifest file exists and is valid, so we can return
                            return true;
                        }
                        return false;
                    }
                    return false;
                }
                return false;
            }
            catch (Exception ex)
            {
                throw new Exception("Failed while validating Manifest structure." + ex.Message);
            }
        }

        public static bool ManifestDirectoryExists(string projectPath)
        {
            string manifestDirectoryPath = Path.Combine(projectPath, sManifestDirectoryName);
            return Directory.Exists(manifestDirectoryPath);
        }

        public static bool ManifestFileExists(string manifestDirectoryPath)
        {
            string manifestFilePath = Path.Combine(manifestDirectoryPath, sManifestFileName);
            return File.Exists(manifestFilePath);
        }

        public static bool IsVisAssistProject(Dictionary<string, string> manifestData)
        {
            if (manifestData == null || manifestData.Count == 0) return false;
            if (manifestData.ContainsKey("ApplicationName") && manifestData["ApplicationName"] == sApplicationName)
            {
                if (manifestData.ContainsKey("ProjectId") && !string.IsNullOrEmpty(manifestData["ProjectId"]))
                    return true;
            }
            return false;
        }

        public static Dictionary<string, string> ReadManifestFile(string projectFolderPath)
        {
            /* 
             * Using the project folder path here so that this method can be called from anywhere
             */

            string manifestDirectoryPath = Path.Combine(projectFolderPath, sManifestDirectoryName);
            string manifestFilePath = Path.Combine(manifestDirectoryPath, sManifestFileName);
            string jsonString = File.ReadAllText(manifestFilePath);
            var data = JsonConvert.DeserializeObject<dynamic>(jsonString);

            //return a ProjectManifest dictionary populated with the data from the JSON file
            Dictionary<string, string> manifestData = new Dictionary<string, string>();
            foreach (var item in data)
            {
                manifestData.Add(item.Name, item.Value.ToString());
            }
            return manifestData;
        }

        private static void ProtectManifestDirectory(string manifestDirectoryPath)
        {
            //set the directory as hidden
            File.SetAttributes(manifestDirectoryPath, File.GetAttributes(manifestDirectoryPath) | FileAttributes.Hidden);
        }

        public static void DeleteManifestDirectory(string projectPath)
        {
            string manifestDirectoryPath = Path.Combine(projectPath, sManifestDirectoryName);
            Directory.Delete(manifestDirectoryPath, true);
        }

        public static void DeleteManifestFile(string manifestDirectoryPath)
        {
            string manifestFilePath = Path.Combine(manifestDirectoryPath, sManifestFileName);
            File.Delete(manifestFilePath);
        }

        public static void CreateManifestFile()
        {
            // Implementation for creating manifest file
            string manifestDirectoryPath = Path.Combine(sProjectPath, sManifestDirectoryName);
            string manifestFilePath = Path.Combine(manifestDirectoryPath, sManifestFileName);
            File.Create(manifestFilePath).Dispose();

            WriteJsonFile(manifestFilePath);
        }

        private static void WriteJsonFile(string manifestFilePath)
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
        }
    }
}
