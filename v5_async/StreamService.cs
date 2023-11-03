using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace v5_async
{
    public class StreamService<T>
    {
        private SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

        public async Task WriteToStreamAsync(Stream stream, IEnumerable<T> data, IProgress<string> progress)
        {
            await semaphoreSlim.WaitAsync();
            try
            {
                progress.Report($"Thread {Thread.CurrentThread.ManagedThreadId}: Start writing to stream");
                
                stream.Position = 0;
                
                await Task.Delay(new Random().Next(3000, 5000));

                var serializedData = JsonSerializer.SerializeToUtf8Bytes(data);
                
                var bytesWritten = 0;
                while (bytesWritten < serializedData.Length)
                {
                    var chunkSize = Math.Min(1024, serializedData.Length - bytesWritten);
                    await stream.WriteAsync(serializedData, bytesWritten, chunkSize);
                    
                    bytesWritten += chunkSize;
                    progress.Report($"Thread {Thread.CurrentThread.ManagedThreadId}: writing to stream, finished {100 * bytesWritten / serializedData.Length}%");
                    
                    await Task.Delay(50);
                }

                progress.Report($"Thread {Thread.CurrentThread.ManagedThreadId}: Writing to stream completed");
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        public async Task CopyFromStreamAsync(Stream stream, string filename, IProgress<string> progress)
        {
            await semaphoreSlim.WaitAsync();
            try
            {
                progress.Report($"Thread {Thread.CurrentThread.ManagedThreadId}: Start copying from stream to file");
                
                stream.Position = 0;

                using (var fileStream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
                {
                    var buffer = new byte[1024];
                    
                    var totalBytesRead = 0;
                    int bytesRead;
                    
                    while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                        
                        totalBytesRead += bytesRead;
                        progress.Report($"Thread {Thread.CurrentThread.ManagedThreadId}: copying from stream to file, copied {totalBytesRead} bytes");
                    }
                }

                progress.Report($"Thread {Thread.CurrentThread.ManagedThreadId}: copying finished");
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        public async Task<int> GetStatisticsAsync(string fileName, Func<T, bool> filter)
        {
            int count = 0;

            await using (var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true))
            {
                var data = await JsonSerializer.DeserializeAsync<IEnumerable<T>>(stream);
                
                if (data != null)
                {
                    foreach (var item in data)
                    {
                        if (filter(item))
                        {
                            count++;
                        }
                    }
                }
            }
            
            return count;
        }
    }
}
