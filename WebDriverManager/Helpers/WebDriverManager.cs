﻿namespace WebDriverManager.Helpers
{
    using NLog;
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.IO.Compression;
    using System.Net;

    public static class WebDriverManager
    {
        private static readonly Logger HLog = LogManager.GetCurrentClassLogger();

        private static string DesticationFolder { get; set; }

        private static string DesticationZip { get; set; }

        private static string DesticationFile { get; set; }

        private static string Zip { get; set; }

        private static bool IsNew { get; set; }

        /// <summary>
        /// Build browser driver download URL from mock using config parameters
        /// </summary>
        /// <param name="baseUrl">URL mock</param>
        /// <param name="release">Browser driver release number</param>
        /// <param name="version">Browser driver version number</param>
        /// <param name="architecture">Target browser driver architecture</param>
        /// <returns>Browser driver download URL</returns>
        private static string BuildUrl(string baseUrl, string release, string version, string architecture)
        {
            try
            {
                return baseUrl
                    .Replace("<release>", release)
                    .Replace("<version>", version)
                    .Replace("<architecture>", architecture);
            }
            catch (Exception ex)
            {
                HLog.Error(ex, "Error occurred during building browser driver archive download URL");
                throw new WebDriverManagerException(
                    "Error occurred during building browser driver archive download URL", ex);
            }
        }

        /// <summary>
        /// Prepare destination folder based on path
        /// </summary>
        /// <param name="path">Default or custom destination folder path</param>
        private static void PrepareCatalogs(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
            }
            catch (Exception ex)
            {
                HLog.Error(ex, "Error occurred during browser driver catalog preparation");
                throw new WebDriverManagerException("Error occurred during browser driver catalog preparation", ex);
            }
        }

        /// <summary>
        /// Get destination browser driver archive name based on browser driver download URL
        /// </summary>
        /// <param name="hreflink">Browser driver download URL</param>
        /// <returns>Destination browser driver archive file name</returns>
        private static string ZipFileName(string hreflink)
        {
            try
            {
                return Path.GetFileName(hreflink);
            }
            catch (Exception ex)
            {
                HLog.Error(ex, "Error occurred during getting browser driver archive name");
                throw new WebDriverManagerException("Error occurred during getting browser driver archive name", ex);
            }
        }

        /// <summary>
        /// Download browser driver archive
        /// </summary>
        /// <param name="config">Specific browser driver config</param>
        public static void Download(WebDriverManagerConfig config)
        {
            using (var webClient = new WebClient())
            {
                var url = BuildUrl(config.Url, config.Release, config.Version, config.Architecture);
                Zip = ZipFileName(url);
                var bin = Path.GetFileNameWithoutExtension(config.Binary);
                if (bin != null)
                    DesticationFolder = Path.Combine(config.Destication, bin, config.Version, config.Architecture);
                DesticationZip = Path.Combine(DesticationFolder, Zip);
                DesticationFile = Path.Combine(DesticationFolder, config.Binary);
                try
                {
                    if (File.Exists(DesticationFile)) return;
                    PrepareCatalogs(DesticationFolder);
                    webClient.DownloadFile(url, DesticationZip);
                }
                catch (Exception ex)
                {
                    HLog.Error(ex, "Error occurred during browser driver archive downloading");
                    throw new WebDriverManagerException("Error occurred during browser driver archive downloading", ex);
                }
            }
        }

        /// <summary>
        /// Extract browser driver archive
        /// </summary>
        /// <param name="config">Specific browser driver config </param>
        public static void Unzip(WebDriverManagerConfig config)
        {
            try
            {
                DesticationFile = Path.Combine(DesticationFolder, config.Binary);

                if (!File.Exists(DesticationFile))
                {
                    using (var zip = ZipFile.Open(DesticationZip, ZipArchiveMode.Read))
                    {
                        foreach (var entry in zip.Entries)
                        {
                            if (entry.Name == config.Binary)
                            {
                                entry.ExtractToFile(DesticationFile);
                            }
                        }
                    }
                    IsNew = true;
                }
                else
                    IsNew = false;
            }
            catch (Exception ex)
            {
                HLog.Error(ex, "Error occurred during browser driver archive extracting");
                throw new WebDriverManagerException("Error occurred during browser driver archive extracting", ex);
            }
        }

        /// <summary>
        /// Delete extracted browser driver archive
        /// </summary>
        public static void Clean()
        {
            try
            {
                if (File.Exists(DesticationZip))
                    File.Delete(DesticationZip);
            }
            catch (Exception ex)
            {
                HLog.Error(ex, "Error occurred during deleting extracted browser driver archive");
                throw new WebDriverManagerException("Error occurred during deleting extracted browser driver archive",
                    ex);
            }
        }

        /// <summary>
        /// Add browser driver environment variable
        /// </summary>
        /// <param name="variable">Environment variable</param>
        public static void AddEnvironmentVariable(string variable)
        {
            try
            {
                var variableValue = Environment.GetEnvironmentVariable(variable);
                if (variableValue == null || !variableValue.Equals(DesticationFolder))
                    Environment.SetEnvironmentVariable(variable, DesticationFolder, EnvironmentVariableTarget.Machine);
            }
            catch (Exception ex)
            {
                HLog.Error(ex, "Error occurred during adding(updating) browser driver environment variable");
                throw new WebDriverManagerException(
                    "Error occurred during adding(updating) browser driver environment variable", ex);
            }
        }

        /// <summary>
        /// Update browser driver environment variable if it's already exist and different from current
        /// </summary>
        /// <param name="variable">Environment variable</param>
        // TODO : Temporary disable this functionality because of wrong path override
        public static void UpdatePath(string variable)
        {
            try
            {
//                const string name = "PATH";
//                var pathVariable = Environment.GetEnvironmentVariable(name);
//                var newPathVariable = pathVariable + (pathVariable != null && pathVariable.EndsWith(";") ? string.Empty : ";") + $@"%{variable}%";
//                if (pathVariable != null && !pathVariable.Contains(DesticationFolder) && !pathVariable.Contains(variable))
//                    Environment.SetEnvironmentVariable(newPathVariable, name, EnvironmentVariableTarget.Machine);
            }
            catch (Exception ex)
            {
                HLog.Error(ex, "Error occurred during updating PATH environment variable");
                throw new WebDriverManagerException("Error occurred during updating PATH environment variable", ex);
            }
        }

        /// <summary>
        /// Install application from file
        /// </summary>
        /// <param name="command">Installation command</param>
        public static void Install(string command)
        {
            try
            {
                if (File.Exists(DesticationFile) && IsNew)
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        UseShellExecute = false,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        FileName = DesticationFile,
                        Arguments = command
                    };
                    Process process = new Process
                    {
                        StartInfo = startInfo
                    };
                    process.Start();
                    process.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                HLog.Error(ex,
                    $"Error occurred during application installation from file '{DesticationFile}' using command '{command}'");
                throw new WebDriverManagerException(
                    $"Error occurred during application installation from file '{DesticationFile}' using command '{command}'",
                    ex);
            }
        }
    }
}