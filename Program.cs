using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Storage;
using Azure.ResourceManager.Storage.Models;

class Program
{
    static async Task Main(string[] args)
    {
        string subscriptionId    = "789d945d-2384-4287-ae5b-53a641f6fe8f";
        string resourceGroupName = "rg-blob-storage";
        string storageAccountName = "ksovestorage20260522";
        AzureLocation location   = AzureLocation.EastUS;
        var credential = new DefaultAzureCredential();
        var armClient  = new ArmClient(credential);
        var subscription = await armClient
            .GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{subscriptionId}"))
            .GetAsync();
        Console.WriteLine($"Using subscription: {subscription.Value.Data.DisplayName}");
        var rgCollection = subscription.Value.GetResourceGroups();
        var rgData = new ResourceGroupData(location);
        rgData.Tags.Add("environment", "dev");
        rgData.Tags.Add("purpose", "blob-storage");
        Console.WriteLine($"Creating resource group: {resourceGroupName}...");
        var rgOperation = await rgCollection.CreateOrUpdateAsync(
            WaitUntil.Completed, resourceGroupName, rgData);
        var resourceGroup = rgOperation.Value;
        Console.WriteLine($"Resource group created: {resourceGroup.Data.Id}");
        var storageCollection = resourceGroup.GetStorageAccounts();
        var storageData = new StorageAccountCreateOrUpdateContent(
            new StorageSku(StorageSkuName.StandardLrs),
            StorageKind.StorageV2,
            location)
        {
            AccessTier = StorageAccountAccessTier.Hot,
            AllowBlobPublicAccess = false,
            MinimumTlsVersion = StorageMinimumTlsVersion.Tls1_2,
            EnableHttpsTrafficOnly = true,
        };
        storageData.Tags.Add("environment", "dev");
        storageData.Tags.Add("purpose", "blob-storage");
        Console.WriteLine($"Creating storage account: {storageAccountName}...");
        var storageOperation = await storageCollection.CreateOrUpdateAsync(
            WaitUntil.Completed, storageAccountName, storageData);
        var storageAccount = storageOperation.Value;
        Console.WriteLine($"Storage account created: {storageAccount.Data.Id}");
        var blobService = storageAccount.GetBlobService();
        var containerCollection = blobService.GetBlobContainers();
        var containerOperation = await containerCollection.CreateOrUpdateAsync(
            WaitUntil.Completed,
            "my-blob-container",
            new BlobContainerData() { PublicAccess = StoragePublicAccessType.None });
        var container = containerOperation.Value;
        Console.WriteLine($"Blob container created: {container.Data.Name}");
        Console.WriteLine("\n--- Deployment Complete ---");
        Console.WriteLine($"Resource Group : {resourceGroupName}");
        Console.WriteLine($"Storage Account: {storageAccount.Data.Name}");
        Console.WriteLine($"Blob Endpoint  : {storageAccount.Data.PrimaryEndpoints.BlobUri}");
        Console.WriteLine($"Container      : {container.Data.Name}");
    }
}

