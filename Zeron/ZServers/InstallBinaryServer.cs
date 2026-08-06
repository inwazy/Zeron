// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System.Globalization;
using Zeron.ZCore;
using Zeron.ZCore.Type;

namespace Zeron.ZServers
{
    /// <summary>
    /// InstallBinaryServer - downloads installer binaries for InstallServer queues.
    /// </summary>
    public static class InstallBinaryServer
    {
        /// <summary>
        /// TryDownload - returns true when the local file already exists or download succeeds.
        /// </summary>
        /// <param name="queuesType"></param>
        /// <returns>Returns bool.</returns>
        public static bool TryDownload(
            InstallQueuesType? queuesType)
        {
            if (queuesType == null
                || string.IsNullOrEmpty(queuesType.RepoUrl)
                || string.IsNullOrEmpty(queuesType.FilePath))
            {
                return false;
            }

            if (File.Exists(queuesType.FilePath))
            {
                return true;
            }

            using HttpClient httpClient = new();

            try
            {
                using Task<HttpResponseMessage> httpResponse = httpClient.GetAsync(queuesType.RepoUrl);
                httpResponse.Wait();

                if (!httpResponse.IsCompletedSuccessfully)
                {
                    return false;
                }

                return TryWriteResponseToFile(queuesType.FilePath, httpResponse.Result);
            }
            catch (InvalidOperationException e)
            {
                LogDownloadError(nameof(InvalidOperationException), e);
            }
            catch (HttpRequestException e)
            {
                LogDownloadError(nameof(HttpRequestException), e);
            }
            catch (TaskCanceledException e)
            {
                LogDownloadError(nameof(TaskCanceledException), e);
            }

            return false;
        }

        /// <summary>
        /// TryWriteResponseToFile
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="response"></param>
        /// <returns>Returns bool.</returns>
        private static bool TryWriteResponseToFile(
            string filePath,
            HttpResponseMessage response)
        {
            try
            {
                string? directory = Path.GetDirectoryName(filePath);

                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using FileStream fileStream = File.Create(filePath);
                response.Content.CopyToAsync(fileStream).Wait();

                return true;
            }
            catch (UnauthorizedAccessException e)
            {
                LogDownloadError(nameof(UnauthorizedAccessException), e);
            }
            catch (ArgumentException e)
            {
                LogDownloadError(nameof(ArgumentException), e);
            }
            catch (PathTooLongException e)
            {
                LogDownloadError(nameof(PathTooLongException), e);
            }
            catch (DirectoryNotFoundException e)
            {
                LogDownloadError(nameof(DirectoryNotFoundException), e);
            }
            catch (IOException e)
            {
                LogDownloadError(nameof(IOException), e);
            }
            catch (NotSupportedException e)
            {
                LogDownloadError(nameof(NotSupportedException), e);
            }

            return false;
        }

        /// <summary>
        /// LogDownloadError
        /// </summary>
        /// <param name="exceptionName"></param>
        /// <param name="e"></param>
        /// <returns>Returns void.</returns>
        private static void LogDownloadError(
            string exceptionName,
            Exception e)
        {
            ZNLogger.Common.Error(string.Format(CultureInfo.InvariantCulture,
                "InstallBinaryServer TryDownload {0}:{1}\n{2}",
                exceptionName,
                e.Message,
                e.StackTrace));
        }
    }
}
