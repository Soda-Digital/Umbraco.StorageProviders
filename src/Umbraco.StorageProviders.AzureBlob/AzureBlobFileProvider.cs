﻿using System;
using System.Collections.Generic;
using System.Net;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using Umbraco.Cms.Core;
using Umbraco.Extensions;
using Umbraco.StorageProviders.AzureBlob.IO;

namespace Umbraco.StorageProviders.AzureBlob
{
    /// <summary>
    /// Represents a read-only Azure Blob Storage file provider.
    /// </summary>
    /// <seealso cref="Microsoft.Extensions.FileProviders.IFileProvider" />
    public class AzureBlobFileProvider : IFileProvider
    {
        private readonly BlobContainerClient _containerClient;
        private readonly string? _containerRootPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureBlobFileProvider" /> class.
        /// </summary>
        /// <param name="containerClient">The container client.</param>
        /// <param name="containerRootPath">The container root path.</param>
        /// <exception cref="System.ArgumentNullException">containerClient</exception>
        public AzureBlobFileProvider(BlobContainerClient containerClient, string? containerRootPath = null)
        {
            _containerClient = containerClient ?? throw new ArgumentNullException(nameof(containerClient));
            _containerRootPath = containerRootPath?.Trim(Constants.CharArrays.ForwardSlash);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureBlobFileProvider" /> class.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <exception cref="System.ArgumentNullException">options</exception>
        public AzureBlobFileProvider(AzureBlobFileSystemOptions options)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _containerClient = new BlobContainerClient(options.ConnectionString, options.ContainerName);
            _containerRootPath = options.ContainerRootPath?.Trim(Constants.CharArrays.ForwardSlash);
        }

        /// <inheritdoc />
        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            var path = GetFullPath(subpath);

            // Get all blobs and iterate to fetch all pages
            var blobs = new List<BlobHierarchyItem>();
            foreach (var item in _containerClient.GetBlobsByHierarchy(delimiter: "/", prefix: path))
            {
                blobs.Add(item);
            }

            if (blobs.Count == 0)
            {
                return NotFoundDirectoryContents.Singleton;
            }

            return new AzureBlobDirectoryContents(_containerClient, blobs);
        }

        /// <inheritdoc />
        public IFileInfo GetFileInfo(string subpath)
        {
            var path = GetFullPath(subpath);
            var blobClient = _containerClient.GetBlobClient(path);

            BlobProperties properties;
            try
            {
                properties = blobClient.GetProperties().Value;
            }
            catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.NotFound)
            {
                return new NotFoundFileInfo(AzureBlobItemInfo.ParseName(path));
            }

            return new AzureBlobItemInfo(blobClient, properties);
        }

        /// <inheritdoc />
        public IChangeToken Watch(string filter) => NullChangeToken.Singleton;

        private string GetFullPath(string subpath) => _containerRootPath + subpath.EnsureStartsWith('/');
    }
}
