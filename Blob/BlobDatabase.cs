using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using TinyBlockStorage.Core;

namespace TinyBlockStorage.Blob
{
    /// <summary>
    /// Then, define our database
    /// </summary>
    public class BlobDatabase : IDisposable
    {
        readonly Stream mainDatabaseFile;
        readonly Stream primaryIndexFile;
        readonly Stream secondaryIndexFile;
        readonly Tree<Guid, uint> primaryIndex;
        readonly Tree<Tuple<string, int>, uint> secondaryIndex;
        readonly RecordStorage blobRecords;
        readonly BlobSerializer blobSerializer = new BlobSerializer();

        /// <summary>
        /// </summary>
        /// <param name="pathToBlobDb">Path to blob db.</param>
        public BlobDatabase(string pathToBlobDb)
        {
            if (pathToBlobDb == null)
                throw new ArgumentNullException("pathToBlobDb");

            // As soon as BlobDatabase is constructed, open the steam to talk to the underlying files
            this.mainDatabaseFile = new FileStream(pathToBlobDb, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 4096);
            this.primaryIndexFile = new FileStream(pathToBlobDb + ".pidx", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 4096);
            this.secondaryIndexFile = new FileStream(pathToBlobDb + ".sidx", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 4096);

            // Construct the RecordStorage that use to store main blob data
            this.blobRecords = new RecordStorage(new BlockStorage(this.mainDatabaseFile, 4096, 48));

            // Construct the primary and secondary indexes 
            this.primaryIndex = new Tree<Guid, uint>(
                new TreeDiskNodeManager<Guid, uint>(
                    new GuidSerializer(),
                    new TreeUIntSerializer(),
                    new RecordStorage(new BlockStorage(this.primaryIndexFile, 4096))
                ),
                false
            );

            this.secondaryIndex = new Tree<Tuple<string, int>, uint>(
                new TreeDiskNodeManager<Tuple<string, int>, uint>(
                    new StringIntSerializer(),
                    new TreeUIntSerializer(),
                    new RecordStorage(new BlockStorage(this.secondaryIndexFile, 4096))
                ),
                true
            );
        }

        /// <summary>
        /// Update given blob
        /// </summary>
        public void Update(BlobModel blob)
        {
            if (disposed)
            {
                throw new ObjectDisposedException("BlobDatabase");
            }

            throw new NotImplementedException();
        }

        /// <summary>
        /// Insert a new blob entry into our blob database
        /// </summary>
        public void Insert(BlobModel blob)
        {
            if (disposed)
            {
                throw new ObjectDisposedException("BlobDatabase");
            }

            // Serialize the blob and insert it
            var recordId = this.blobRecords.Create(this.blobSerializer.Serialize(blob));

            // Primary index
            this.primaryIndex.Insert(blob.Id, recordId);

            // Secondary index
            this.secondaryIndex.Insert(new Tuple<string, int>(blob.FileName, blob.LastEditedUnixTimeSeconds), recordId);
        }

        /// <summary>
        /// Find a blob by its unique id
        /// </summary>
        public BlobModel Find(Guid blobId)
        {
            if (disposed)
            {
                throw new ObjectDisposedException("BlobDatabase");
            }

            // Look in the primary index for this blob
            var entry = this.primaryIndex.Get(blobId);
            if (entry == null)
            {
                return null;
            }

            return this.blobSerializer.Deserializer(this.blobRecords.Find(entry.Item2));
        }

        /// <summary>
        /// Find all blobs that beints to given fileName and lastEditedUnixTime
        /// </summary>
        public IEnumerable<BlobModel> FindBy(string fileName, int lastEditedUnixTime)
        {
            var comparer = Comparer<Tuple<string, int>>.Default;
            var searchKey = new Tuple<string, int>(fileName, lastEditedUnixTime);

            // Use the secondary index to find this blob
            foreach (var entry in this.secondaryIndex.LargerThanOrEqualTo(searchKey))
            {
                // As soon as we reached larger key than the key given by client, stop
                if (comparer.Compare(entry.Item1, searchKey) > 0)
                {
                    break;
                }

                // Still in range, yield return
                yield return this.blobSerializer.Deserializer(this.blobRecords.Find(entry.Item2));
            }
        }

        /// <summary>
        /// Delete specified blob from our database
        /// </summary>
        public void Delete(BlobModel blob)
        {
            throw new NotImplementedException();
        }

        #region Dispose
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        bool disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !disposed)
            {
                this.mainDatabaseFile.Dispose();
                this.secondaryIndexFile.Dispose();
                this.primaryIndexFile.Dispose();
                this.disposed = true;
            }
        }

        ~BlobDatabase()
        {
            Dispose(false);
        }
        #endregion
    }
}

