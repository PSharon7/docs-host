using System.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace docs.host
{
    public static class BlobAccessor
    {
        private readonly static string s_storageConnectionString = ConfigurationManager.AppSettings["blob_connection"];
        private readonly static string s_containerName = ConfigurationManager.AppSettings["blob_container"];

        public static CloudStorageAccount storageAccount;
        public static CloudBlobContainer cloudBlobContainer;

        public static void Initialize()
        {
            storageAccount = CloudStorageAccount.Parse(s_storageConnectionString);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            cloudBlobContainer = blobClient.GetContainerReference(s_containerName);
        }

        public static string GetContainerName()
        {
            return s_containerName;
        }
    }
}
