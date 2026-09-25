using IPMS.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPMS.Domain.ProjectManagement
{
    /// <summary>
    /// One row on the "Project Documents" tab — metadata about an uploaded
    /// PDF/DOC/DOCX file. The actual file bytes live in file storage
    /// (Infrastructure concern, not the Domain layer's job); this Entity only
    /// tracks what was uploaded, so it can be listed, versioned, and audited.
    /// </summary>
    public class UploadDocument : BatchScopedEntity<long>
    {
        public string FileName { get; private set; }
        public string FileType { get; private set; }
        public long FileSizeBytes { get; private set; }
        public int Version { get; private set; }
        public string StoragePath { get; private set; }   // where Infrastructure actually put the file

        private UploadDocument() { }

        public static UploadDocument Create(string fileName, string fileType, long fileSizeBytes, string storagePath)
            => new UploadDocument { FileName = fileName, FileType = fileType, FileSizeBytes = fileSizeBytes, StoragePath = storagePath, Version = 1 };

        /// <summary> A re-upload of the same document bumps the version rather than replacing history. </summary>
        public UploadDocument CreateNewVersion(long fileSizeBytes, string storagePath)
            => new UploadDocument { FileName = FileName, FileType = FileType, FileSizeBytes = fileSizeBytes, StoragePath = storagePath, Version = Version + 1 };
    }

}
