using v5_async;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId}: start working");

        var vehicles = new List<Vehicle>();
        for (int i = 0; i < 1000; i++)
        {
            vehicles.Add(new Vehicle
            {
                Id = i,
                Name = $"Vehicle {i}",
                TechnicalInspectionDate = DateTime.Now.AddYears(-i % 3) 
            });
        }

        var progress = new Progress<string>(message => Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId}: {message}"));
        var streamService = new StreamService<Vehicle>();
        var memStream = new MemoryStream();

        var writeTask = streamService.WriteToStreamAsync(memStream, vehicles, progress);
        Thread.Sleep(200); 

        var copyTask = streamService.CopyFromStreamAsync(memStream, "output.json", progress);

        Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId}: Threads 1 and 2 started");

        await writeTask;
        await copyTask;

        var statistics = await streamService.GetStatisticsAsync("output.json", v => v.TechnicalInspectionDate.Year == DateTime.Now.Year);
        Console.WriteLine($"Statistics: {statistics}");

        Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId}: Stopped execution");
    }
}
