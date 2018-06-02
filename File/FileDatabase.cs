using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using TinyBlockStorage.Core;

namespace TinyBlockStorage.File
{
    /// <summary>
    /// Then, define our database
    /// </summary>
    public class FileDatabase : IDisposable
    {
        readonly Stream mainDatabaseFile;
        readonly Stream primaryIndexFile;
        readonly Stream secondaryIndexFile;
        readonly Tree<Guid, uint> primaryIndex;
        readonly Tree<string, uint> secondaryIndex;
        readonly RecordStorage blobRecords;
        readonly FileSerializer blobSerializer = new FileSerializer();

        /// <summary>
        /// </summary>
        /// <param name="pathToFileDb">Path to blob db.</param>
        public FileDatabase(string pathToFileDb)
        {
            if (pathToFileDb == null)
                throw new ArgumentNullException("pathToFileDb");

            // As soon as FileDatabase is constructed, open the steam to talk to the underlying files
            this.mainDatabaseFile = new FileStream(pathToFileDb, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 4096);
            this.primaryIndexFile = new FileStream(pathToFileDb + ".pidx", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 4096);
            this.secondaryIndexFile = new FileStream(pathToFileDb + ".sidx", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 4096);

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

            this.secondaryIndex = new Tree<string, uint>(
                new TreeDiskNodeManager<string, uint>(
                    new StringSerializer(),
                    new TreeUIntSerializer(),
                    new RecordStorage(new BlockStorage(this.secondaryIndexFile, 4096))
                ),
                true
            );
        }

        /// <summary>
        /// Update given blob
        /// </summary>
        public void Update(FileModel blob)
        {
            if (disposed)
            {
                throw new ObjectDisposedException("FileDatabase");
            }

            throw new NotImplementedException();
        }

        /// <summary>
        /// Insert a new blob entry into our blob database
        /// </summary>
        public void Insert(FileModel blob)
        {
            if (disposed)
            {
                throw new ObjectDisposedException("FileDatabase");
            }

            // Serialize the blob and insert it
            var recordId = this.blobRecords.Create(this.blobSerializer.Serialize(blob));

            // Primary index
            this.primaryIndex.Insert(blob.Id, recordId);

            // Secondary index
            this.secondaryIndex.Insert(blob.FileName, recordId);
        }

        /// <summary>
        /// Find a blob by its unique id
        /// </summary>
        public FileModel Find(Guid blobId)
        {
            if (disposed)
            {
                throw new ObjectDisposedException("FileDatabase");
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
        /// Find all blobs that beints to given fileName
        /// </summary>
        public IEnumerable<FileModel> FindBy(string fileName)
        {
            var comparer = Comparer<string>.Default;

            // Use the secondary index to find this blob
            foreach (var entry in this.secondaryIndex.LargerThanOrEqualTo(fileName))
            {
                // As soon as we reached larger key than the key given by client, stop
                if (comparer.Compare(entry.Item1, fileName) > 0)
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
        public void Delete(FileModel blob)
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

        ~FileDatabase()
        {
            Dispose(false);
        }
        #endregion
    }
}

