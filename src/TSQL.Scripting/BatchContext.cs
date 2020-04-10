using OneCSharp.Metadata.Model;
using OneCSharp.Metadata.Services;
using System;

namespace OneCSharp.TSQL.Scripting
{
    internal interface IBatchContext
    {
        InfoBase InfoBase { get; set; }
    }
    internal sealed class BatchContext : IBatchContext
    {
        private InfoBase _infobase = null;
        private IMetadataService Metadata { get; }
        public BatchContext(IMetadataService metadata)
        {
            Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        }
        public InfoBase InfoBase
        {
            get { return _infobase; }
            set
            {
                _infobase = value;
            }
        }
    }
}