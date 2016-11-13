using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.IO;
using System.IO.Compression;

namespace Url2Arc
{
    public class Archiver : IDisposable
    {
        public List<SourceItem> SourceItems { get; private set; } = new List<SourceItem>();

        public List<SourceItem> SucceededSourceItems { get; private set; } = new List<SourceItem>();

        public List<SourceItem> FailedSourceItems { get; private set; } = new List<SourceItem>();

        public Boolean ThrowDownloadFailedException { get; set; } = false;

        public Int32 DownloadLimitAtOnce { get; set; } = 2;

        public Int64 UseFileBufferThreshold { get; set; } = SourceItem.DefaultUseFileBufferThreshold;

        public void AddItem(SourceItem item)
        {
            SourceItems.Add(item);
        }

        public void AddItem(String url)
        {
            AddItem(new SourceItem(url, UseFileBufferThreshold));
        }

        public void AddItems(IEnumerable<SourceItem> sourceItems)
        {
            SourceItems.AddRange(sourceItems);
        }

        public void AddItems(IEnumerable<String> sourceItems)
        {
            foreach (String sourceItem in sourceItems)
            {
                AddItem(sourceItem);
            }
        }

        public async Task CreateArchiveAsync(Stream streamToWrite)
        {
            SucceededSourceItems.Clear();
            FailedSourceItems.Clear();

            using (var zip = new ZipArchive(streamToWrite, ZipArchiveMode.Update))
            {
                var downloadAction = new ActionBlock<SourceItem>(
                    async sourceItem =>
                    {
                        if (!(await sourceItem.DownloadAsync()))
                        {
                            if (ThrowDownloadFailedException)
                            {
                                throw sourceItem.LastException;
                            }
                            FailedSourceItems.Add(sourceItem);
                        }
                        else
                        {
                            SucceededSourceItems.Add(sourceItem);
                        }
                    }
                    , new ExecutionDataflowBlockOptions
                    {
                        MaxDegreeOfParallelism = DownloadLimitAtOnce,
                    }
                );

                foreach (var sourceItem in SourceItems)
                {
                    await downloadAction.SendAsync(sourceItem);
                }

                downloadAction.Complete();
                await downloadAction.Completion;

                var names = new List<String>();
                foreach (var sourceItem in SucceededSourceItems)
                {
                    var name = sourceItem.FileName;
                    if (name == String.Empty || names.Contains(name))
                    {
                        var n = Path.GetFileNameWithoutExtension(name);
                        var e = Path.GetExtension(name);
                        name = $"{n}{Guid.NewGuid().ToString("P")}{e}";
                    }
                    names.Add(name);

                    var entry = zip.CreateEntry(name);
                    sourceItem.BufferStream.Position = 0;
                    sourceItem.BufferStream.CopyTo(entry.Open());
                }
            }
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    SucceededSourceItems.Clear();
                    FailedSourceItems.Clear();

                    foreach (var item in SourceItems)
                    {
                        item.Dispose();
                    }
                    SourceItems.Clear();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
