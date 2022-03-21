﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.FileProviders;

namespace Umbraco.StorageProviders.AzureBlob
{
    /// <summary>
    /// Represents a virtual hierarchy of Azure Blob Storage blobs.
    /// </summary>
    /// <seealso cref="Microsoft.Extensions.FileProviders.IDirectoryContents" />
    public class AzureBlobDirectoryContents : IDirectoryContents
    {
        private readonly BlobContainerClient _containerClient;
        private readonly IList<BlobHierarchyItem> _items;

        /// <inheritdoc />
        public bool Exists { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureBlobDirectoryContents" /> class.
        /// </summary>
        /// <param name="containerClient">The container client.</param>
        /// <param name="items">The items.</param>
        /// <exception cref="System.ArgumentNullException">containerClient
        /// or
        /// items</exception>
        public AzureBlobDirectoryContents(BlobContainerClient containerClient, IList<BlobHierarchyItem> items)
        {
            _containerClient = containerClient ?? throw new ArgumentNullException(nameof(containerClient));
            _items = items ?? throw new ArgumentNullException(nameof(items));

            Exists = _items.Count > 0;
        }

        /// <inheritdoc />
        public IEnumerator<IFileInfo> GetEnumerator()
            => _items.Select<BlobHierarchyItem, IFileInfo>(x => x.IsPrefix
                    ? new AzureBlobPrefixInfo(x.Prefix)
                    : new AzureBlobItemInfo(_containerClient.GetBlobClient(x.Blob.Name), x.Blob.Properties)).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
