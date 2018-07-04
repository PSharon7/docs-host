using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;

namespace docs.host
{
    public static class BlobStorage
    {
        private readonly static string storageConnectionString = ConfigurationManager.AppSettings["storageConnectionString"];
        private readonly static string containerName = ConfigurationManager.AppSettings["containerName"];

        public static CloudStorageAccount storageAccount;
        public static CloudBlobContainer cloudBlobContainer;

        public static void Initialize()
        {
            storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            cloudBlobContainer = blobClient.GetContainerReference(containerName);
        }
    }
}
