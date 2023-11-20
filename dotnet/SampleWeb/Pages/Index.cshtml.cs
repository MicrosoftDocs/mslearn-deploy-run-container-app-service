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
        private readonly string storageContainerName = "testcontainer";
        private readonly string storageBlobName = "storagetest.txt";

        private readonly string cosmosEndpoint = Environment.GetEnvironmentVariable("COSMOS_ENDPOINT");
        private readonly string cosmosDatabaseName = "testdb";
        private readonly string cosmosContainerName = "cosmostest";

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

        public void OnGet()
        {
            string domainName = Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME");
            ViewData["Host"] = domainName;
            if (string.IsNullOrEmpty(domainName))
            {
                ViewData["Site"] = "Sample Container";
                ViewData["Host"] = "local or unknown";
            }
            else if (domainName.ToLower().Contains("msha"))
            {
                ViewData["Site"] = "Dedicated MSHA";
            }
            else if (domainName.ToLower().Contains("cds"))
            {
                ViewData["Site"] = "Dedicated CDS";
            }
            else
            {
                ViewData["Site"] = "Sample Container Web";
            }

            string sku = Environment.GetEnvironmentVariable("ENVIRONMENT_VERSION");

            if (string.IsNullOrEmpty(sku))
            {
                ViewData["Sku"] = "Unknown";
            }
            else
            {
                ViewData["Sku"] = sku;
            }
        }

        public async Task<IActionResult> OnGetStorageAsync()
        {
            // ' storage blob data contributor' is required before using MSI
            try
            {
                //blobServiceClient = new BlobServiceClient(
                //    new Uri(storageAddress),
                //    new ManagedIdentityCredential("6f60f59c-ca19-4715-a853-b822e9b97946")
                //);

                DefaultAzureCredential defaultAzureCredential = new DefaultAzureCredential();
                blobServiceClient = new BlobServiceClient(
                    new Uri(storageAddress),
                    defaultAzureCredential
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
                string log = $"[{DateTime.Now}] This is a log message from {ViewData["Site"]}.\n";
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
                DefaultAzureCredential defaultAzureCredential = new DefaultAzureCredential();
                cosmosClient = new CosmosClient(
                    cosmosEndpoint,
                    defaultAzureCredential
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
                string productId = "test";

                // Create an item
                Log tempLog = new Log
                {
                    id = productId,
                    Category = cosmosEndpoint,
                    Date = $"[{DateTime.Now}] This is a log message from {Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME")}\n."
                };

                // Insert to cosmosContainer
                await cosmosContainer.UpsertItemAsync<Log>(tempLog);

                // Read from cosmosContainer
                Log result = await cosmosContainer.ReadItemAsync<Log>(productId, new PartitionKey(tempLog.Category));

                LogMessage = $"Created and read a new log: {result.Category}, {result.Date}\n";
            }
            catch (Exception ex)
            {
                LogMessage = "Sorry, something went wrong. Please try again later.\n";
                LogMessage += ex.Message;
            }
            return Content(LogMessage);
        }
    }

    public class Log
    {
        public string id { get; set; }
        public string Category { get; set; }
        public string Date { get; set; }
    }
}
