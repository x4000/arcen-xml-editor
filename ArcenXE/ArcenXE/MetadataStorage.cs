using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArcenXE.Utilities.MetadataProcessing;

namespace ArcenXE
{
    public static class MetadataStorage
    {
        public static MetadataDocument? CurrentVisMetadata = null;
        public static Dictionary<string, MetadataDocument> AllMetadatas = new Dictionary<string, MetadataDocument>();
    }
}
