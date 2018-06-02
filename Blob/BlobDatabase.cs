using System;
using System.IO;
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
        readonly Tree<string, uint> primaryIndex;
        readonly RecordStorage blobRecords;
        readonly BlobSerializer blobSerializer = new BlobSerializer();

        private object SyncRoot = new Object();

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

            // Construct the RecordStorage that use to store main blob data
            this.blobRecords = new RecordStorage(new BlockStorage(this.mainDatabaseFile, 4096, 48));

            // Construct the primary and secondary indexes 
            this.primaryIndex = new Tree<string, uint>(
                new TreeDiskNodeManager<string, uint>(
                    new StringSerializer(),
                    new TreeUIntSerializer(),
                    new RecordStorage(new BlockStorage(this.primaryIndexFile, 4096))
                ),
                false
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

            lock (SyncRoot)
            {
                // Do not add same Blob twice
                var entry = this.primaryIndex.Get(blob.Id);
                if (entry != null)
                    return;

                // Serialize the blob and insert it
                var recordId = this.blobRecords.Create(this.blobSerializer.Serialize(blob));

                // Primary index
                this.primaryIndex.Insert(blob.Id, recordId);
            }
        }

        /// <summary>
        /// Find a blob by its unique id
        /// </summary>
        public BlobModel Find(string blobId)
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

