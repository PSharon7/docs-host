using System.Configuration;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace docs.host
{
    public static class BlobAccessor
    {
        private readonly static string s_storageConnectionString = Config.Get("blob_connection");
        private readonly static string s_containerName = Config.Get("blob_container");

        public static CloudStorageAccount storageAccount;
        public static CloudBlobContainer cloudBlobContainer;

        public static async Task Initialize()
        {
            storageAccount = CloudStorageAccount.Parse(s_storageConnectionString);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            cloudBlobContainer = blobClient.GetContainerReference(s_containerName);
            await cloudBlobContainer.CreateIfNotExistsAsync();
        }

        public static string GetContainerName()
        {
            return s_containerName;
        }
    }
}
