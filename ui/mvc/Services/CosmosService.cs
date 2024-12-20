﻿using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using System.Net;

namespace PhiDeidPortal.Ui.Services
{
    public class CosmosService : ICosmosService
    {
        private readonly CosmosClient _cosmosClient;
        private readonly IConfiguration _configuration;
        private readonly IConfigurationSection _cosmosConfiguration;
        private readonly string _cosmosDbName;
        private readonly string _cosmosContainerName;
        private readonly string _cosmosPartitionKey;

        public CosmosService(CosmosClient cosmosClient, IConfiguration configuration)
        {
            _cosmosClient = cosmosClient;
            _configuration = configuration;

            _cosmosConfiguration = configuration.GetSection("CosmosDb");
            _cosmosDbName = _cosmosConfiguration["DatabaseId"] ?? throw new ArgumentNullException(nameof(_cosmosDbName));
            _cosmosContainerName = _cosmosConfiguration["ContainerId"] ?? throw new ArgumentNullException(nameof(_cosmosContainerName));
            _cosmosPartitionKey = _cosmosConfiguration["PartitionKey"] ?? throw new ArgumentNullException(nameof(_cosmosPartitionKey));
        }

        public async Task<ItemResponse<MetadataRecord>> UpsertMetadataRecord(MetadataRecord record)
        {
            var cosmosDbResponse = await _cosmosClient.CreateDatabaseIfNotExistsAsync(_cosmosDbName);
            var containerProperties = new ContainerProperties
            {
                PartitionKeyPath = _cosmosPartitionKey,
                Id = _cosmosContainerName
            };

            var containerResponse = await cosmosDbResponse.Database.CreateContainerIfNotExistsAsync(containerProperties);

            ItemResponse<MetadataRecord> recordResponse = await containerResponse.Container.UpsertItemAsync<MetadataRecord>(
            item: record,
                new PartitionKey(record.Uri)
                );

            return recordResponse;
        }

        public MetadataRecord? GetMetadataRecord(string docId)
        {
            var docRecord = _cosmosClient
                .GetDatabase(_cosmosDbName)
                .GetContainer(_cosmosContainerName)
                .GetItemLinqQueryable<MetadataRecord>(true)
                .Where(d => d.id == docId)
                .FirstOrDefault();

            return docRecord;
        }

        public MetadataRecord? GetMetadataRecordByUri(string uri)
        {
            var docRecord = _cosmosClient
                .GetDatabase(_cosmosDbName)
                .GetContainer(_cosmosContainerName)
                .GetItemLinqQueryable<MetadataRecord>(true)
                .Where(d => d.Uri == uri)
                .FirstOrDefault();

            return docRecord;
        }

        public MetadataRecord? GetMetadataRecordByAuthorAndUri(string author, string uri)
        {
            var docRecord = _cosmosClient
                .GetDatabase(_cosmosDbName)
                .GetContainer(_cosmosContainerName)
                .GetItemLinqQueryable<MetadataRecord>(true)
                .Where(d => (d.Author == author || d.Author == "N/A") && d.Uri == uri)
                .FirstOrDefault();

            return docRecord;
        }

        public async Task<ServiceResponse> DeleteMetadataRecord(MetadataRecord document)
        {
            var response = await _cosmosClient
                .GetDatabase(_cosmosDbName)
                .GetContainer(_cosmosContainerName)
                .DeleteItemAsync<MetadataRecord>(document.id, new PartitionKey(document.Uri));

            return new ServiceResponse() { IsSuccess = response.StatusCode == HttpStatusCode.NoContent, Code = response.StatusCode };
        }

        public async Task<ItemResponse<MetadataRecord>> UpdateMetadataRecord(MetadataRecord document)
        {
            var cosmosDbResponse = await _cosmosClient.CreateDatabaseIfNotExistsAsync(_cosmosDbName);
            var containerProperties = new ContainerProperties
            {
                PartitionKeyPath = _cosmosPartitionKey,
                Id = _cosmosContainerName
            };

            var containerResponse = await cosmosDbResponse.Database.CreateContainerIfNotExistsAsync(containerProperties);
            var recordResponse = await containerResponse.Container.UpsertItemAsync<MetadataRecord>(
            item: document,
                new PartitionKey(document.Uri)
                );

            return recordResponse;
        }
        
    }
}
