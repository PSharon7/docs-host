using System;
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

        public static string GetBlobUriWithSasToken(string blobUri)
        {
            string sharedAccessSignature = storageAccount.GetSharedAccessSignature(
                new SharedAccessAccountPolicy
                {
                    Permissions = SharedAccessAccountPermissions.Read,
                    SharedAccessExpiryTime = DateTime.UtcNow.AddHours(1),
                    SharedAccessStartTime = DateTime.UtcNow.AddMinutes(-10)
                });

            blobUri += sharedAccessSignature;
            return blobUri;
        }
    }
}
