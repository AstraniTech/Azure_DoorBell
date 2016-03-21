using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Microsoft.WindowsAzure.Mobile.Service;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;

namespace iotpocService.Controllers
{
    public class RequestSASController : ApiController
    {
        public ApiServices Services { get; set; }

        // GET api/RequestSAS
        public async System.Threading.Tasks.Task<PhotoResponse> Get()
        {
            string storageAccountName;
            string storageAccountKey;
            string containerName;
            string sasQueryString="";
            string imageUri="";

            // Try to get the Azure storage account token from app settings.  
            if (!(Services.Settings.TryGetValue("STORAGE_ACCOUNT_NAME", out storageAccountName) |
            Services.Settings.TryGetValue("STORAGE_ACCOUNT_ACCESS_KEY", out storageAccountKey) | Services.Settings.TryGetValue("CONTAINER_NAME", out containerName)))
            {
                Services.Log.Error("Could not retrieve storage account settings.");
            }

            // Set the URI for the Blob Storage service.
            Uri blobEndpoint = new Uri(string.Format("https://{0}.blob.core.windows.net", storageAccountName));

            // Create the BLOB service client.
            CloudBlobClient blobClient = new CloudBlobClient(blobEndpoint,
                new StorageCredentials(storageAccountName, storageAccountKey));
            PhotoResponse photoResponse = null;
            if (Services.Settings.TryGetValue("CONTAINER_NAME", out containerName))
            {
                // Set the BLOB store container name on the item, which must be lowercase.
                containerName = containerName.ToLower();

                // Create a container, if it doesn't already exist.
                CloudBlobContainer container = blobClient.GetContainerReference(containerName);
                await container.CreateIfNotExistsAsync();

                // Create a shared access permission policy. 
                BlobContainerPermissions containerPermissions = new BlobContainerPermissions();

                // Enable anonymous read access to BLOBs.
                containerPermissions.PublicAccess = BlobContainerPublicAccessType.Blob;
                container.SetPermissions(containerPermissions);

                // Define a policy that gives write access to the container for 5 minutes.                                   
                SharedAccessBlobPolicy sasPolicy = new SharedAccessBlobPolicy()
                {
                    SharedAccessStartTime = DateTime.UtcNow,
                    SharedAccessExpiryTime = DateTime.UtcNow.AddMinutes(5),
                    Permissions = SharedAccessBlobPermissions.Write
                };

                // Get the SAS as a string.
                sasQueryString = container.GetSharedAccessSignature(sasPolicy);
                Services.Log.Info("sasQueryString: "+ sasQueryString);
                // Set the URL used to store the image.
                var blobName = genRandNum() + ".jpg";
                imageUri = string.Format("{0}{1}/{2}", blobEndpoint.ToString(),
                    containerName, blobName);
                Services.Log.Info("imageUri: "+ imageUri);
                photoResponse = new PhotoResponse();
                photoResponse.sasUrl = sasQueryString;
                photoResponse.photoId = imageUri;
                photoResponse.expiry = sasPolicy.SharedAccessExpiryTime.ToString();
                photoResponse.ContainerName = containerName;
                photoResponse.ResourceName = blobName;

            }

            if(photoResponse!=null)
            {
                
                return photoResponse;
            }
            else
            {
                Services.Log.Info("Failed to generate SAS");

                return photoResponse;
            }            
            
        }

        private int genRandNum()
        {
            var rand = new Random();
            return (rand.Next(1,90000)) + 10000;
        }
    }

    public class PhotoResponse
    {
        public string sasUrl { get; set; }
        public string photoId { get; set; }
        public string expiry { get; set; }
        public string ResourceName { get; set; }
        public string ContainerName { get; set; }

    }
}
