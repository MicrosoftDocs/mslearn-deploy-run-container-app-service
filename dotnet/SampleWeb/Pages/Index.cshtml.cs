using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using Azure;
using Microsoft.Azure.Cosmos;
using Microsoft.AspNetCore.Mvc;

namespace SampleWeb.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        private readonly string storageAddress = Environment.GetEnvironmentVariable("CONTENT_STORAGE");
        private readonly string storageContainerName = "mycontainer01";
        private readonly string storageBlobName = "date.txt";

        private readonly string cosmosEndpoint = Environment.GetEnvironmentVariable("COSMOS_ENDPOINT");
        private readonly string cosmosDatabaseName = "mycosmosdb01";
        private readonly string cosmosContainerName = "date";

        private BlobServiceClient blobServiceClient;
        private BlobContainerClient blobContainerClient;
        private BlobClient blobClient;

        private CosmosClient cosmosClient;
        private Database cosmosDatabase;
        private Container cosmosContainer;

        // The log to show in the web page
        public string LogMessage { get; set; }

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public async Task<IActionResult> OnGetStorageAsync()
        {
            try
            {
                blobServiceClient = new BlobServiceClient(
                    new Uri(storageAddress),
                    new ManagedIdentityCredential("6f60f59c-ca19-4715-a853-b822e9b97946")
                );

                // Create blobContainerClient if not exists
                blobContainerClient = blobServiceClient.GetBlobContainerClient(storageContainerName);
                if (!await blobContainerClient.ExistsAsync())
                {
                    blobContainerClient = await blobServiceClient.CreateBlobContainerAsync(storageContainerName);
                }

                // Create BlobClient if not exists
                blobClient = blobContainerClient.GetBlobClient(storageBlobName);
                if (!await blobClient.ExistsAsync())
                {
                    await blobClient.UploadAsync(new MemoryStream());
                }

                // Write log to blob
                string log = $"[{DateTime.Now}] This is a log message from {storageAddress}.\n";
                await blobClient.UploadAsync(BinaryData.FromString(log), overwrite: true);

                // Read log from blob
                using (var stream = new MemoryStream())
                {
                    await blobClient.DownloadToAsync(stream);
                    LogMessage = System.Text.Encoding.UTF8.GetString(stream.ToArray());
                }
            }
            catch (Exception ex)
            {
                LogMessage = "Sorry, something went wrong. Please try again later.\n";
                LogMessage += ex.Message;
            }
            return Content(LogMessage);
        }

        public async Task<IActionResult> OnGetCosmosAsync()
        {
            try
            {
                cosmosClient = new CosmosClient(
                    cosmosEndpoint,
                    new ManagedIdentityCredential()
                );

                // Create a database asynchronously if it doesn't already exist
                cosmosDatabase = await cosmosClient.CreateDatabaseIfNotExistsAsync(
                    id: cosmosDatabaseName
                );

                // Create a container asynchronously if it doesn't already exist
                cosmosContainer = await cosmosDatabase.CreateContainerIfNotExistsAsync(
                    id: cosmosContainerName,
                    partitionKeyPath: "/Category",
                    throughput: 400
                );

                // Generate a random product Id
                string productId = Guid.NewGuid().ToString();

                // Create an item
                Product product = new Product
                {
                    Id = productId,
                    Category = "Current Date",
                    Date = $"[{DateTime.Now}] This is a log message from {cosmosEndpoint}\n."
                };

                // Insert to cosmosContainer
                await cosmosContainer.CreateItemAsync(product, new PartitionKey(product.Category));

                // Read from cosmosContainer
                Product result = await cosmosContainer.ReadItemAsync<Product>(productId, new PartitionKey(product.Category));

                LogMessage = $"Created and read a new product: {result.Category}, {result.Date}\n";
            }
            catch (Exception ex)
            {
                LogMessage = "Sorry, something went wrong. Please try again later.\n";
                LogMessage += ex.Message;
            }
            return Content(LogMessage);
        }
    }

    public class Product
    {
        public string Id { get; set; }
        public string Category { get; set; }
        public string Date { get; set; }
    }
}
