using System;
using System.Threading.Tasks;
using System.Net;
using System.IO;

namespace Url2Arc
{
    public class SourceItem : IDisposable
    {
        public static readonly Int64 DefaultUseFileBufferThreshold = 104857600;

        public String Url { get; set; } = String.Empty;

        public String FileName { get; set; } = String.Empty;

        public String GUID { get; private set; } = Guid.NewGuid().ToString("N");

        public Int64 UseFileBufferThreshold { get; set; } = DefaultUseFileBufferThreshold;

        public Boolean IsProcessed { get; private set; } = false;

        public Boolean IsFailed { get; private set; } = false;

        public Exception LastException { get; private set; } = null;

        public Stream BufferStream
        {
            get
            {
                if (this.bufferStream == null)
                {
                    return null;
                }

                return (Stream)this.bufferStream;
            }
        }

        private WebRequest request = null;
        private WebResponse response = null;

        private String bufferFilePath = null;
        private Stream bufferStream = null;

        public SourceItem(String url)
        {
            Url = url;
        }

        public SourceItem(String url, Int64 useFileBufferThreshold)
        {
            Url = url;
            UseFileBufferThreshold = useFileBufferThreshold;
        }

        public async Task<Boolean> DownloadAsync()
        {
            if (IsProcessed)
            {
                return true;
            }

            this.request = WebRequest.Create(Url);

            try
            {
                this.response = await this.request.GetResponseAsync();

                Stream writeStream = null;
                if (this.response.ContentLength > this.UseFileBufferThreshold)
                {
                    this.bufferFilePath = Path.GetTempFileName();
                    writeStream = new FileStream(this.bufferFilePath, FileMode.Create);
                }
                else
                {
                    writeStream = new MemoryStream();
                }

                try
                {
                    using (var readStream = this.response.GetResponseStream())
                    {
                        byte[] buf = new byte[4096];
                        while (true)
                        {
                            var readSize = readStream.Read(buf, 0, buf.Length);
                            if (readSize < 1)
                            {
                                break;
                            }
                            writeStream.Write(buf, 0, readSize);
                        }

                        writeStream.Flush();

                        if (writeStream is MemoryStream)
                        {
                            this.bufferStream = new MemoryStream();
                            writeStream.Position = 0;
                            writeStream.CopyTo(this.bufferStream);
                        }
                        else
                        {
                            writeStream.Dispose();
                            writeStream = null;
                            this.bufferStream = new FileStream(this.bufferFilePath, FileMode.Open);
                        }
                    }
                }
                finally
                {
                    if (writeStream != null)
                    {
                        writeStream.Dispose();
                        writeStream = null;
                    }
                }

                if (this.FileName == String.Empty)
                {
                    this.FileName = Path.GetFileName(this.response.ResponseUri.AbsolutePath);
                }

                this.IsProcessed = true;
            }
            catch (Exception e)
            {
                this.IsFailed = true;
                this.LastException = e;

                if (this.bufferStream != null)
                {
                    this.bufferStream.Dispose();
                    this.bufferStream = null;
                }

                if (this.bufferFilePath != null)
                {
                    File.Delete(this.bufferFilePath);
                    this.bufferFilePath = null;
                }
            }

            return !this.IsFailed;
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (this.bufferStream != null)
                    {
                        this.bufferStream.Dispose();
                        this.bufferStream = null;
                    }

                    if (this.bufferFilePath != null)
                    {
                        File.Delete(this.bufferFilePath);
                        bufferFilePath = null;
                    }

                    if (this.response != null)
                    {
                        this.response.Dispose();
                        this.response = null;
                    }
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
