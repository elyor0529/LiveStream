using FFmpeg.NET.Events;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LiveStream.Demo
{
    class Program
    {

        private  static async Task ExecFfm(string args)
        {
            var exePath = Path.Combine(Environment.CurrentDirectory, "ffmpeg.exe");
            var ffmpeg = new FFmpeg.NET.Engine(exePath);

            ffmpeg.Progress += OnProgress;
            ffmpeg.Data += OnData;
            ffmpeg.Error += OnError;
            ffmpeg.Complete += OnComplete;

            await ffmpeg.ExecuteAsync(args);
        }

        private static void OnProgress(object sender, ConversionProgressEventArgs e)
        {
            Console.WriteLine("[{0} => {1}]", e.Input?.FileInfo?.Name, e.Output?.FileInfo?.Name);
            Console.WriteLine("Bitrate: {0}", e.Bitrate);
            Console.WriteLine("Fps: {0}", e.Fps);
            Console.WriteLine("Frame: {0}", e.Frame);
            Console.WriteLine("ProcessedDuration: {0}", e.ProcessedDuration);
            Console.WriteLine("Size: {0} kb", e.SizeKb);
            Console.WriteLine("TotalDuration: {0}\n", e.TotalDuration);
        }

        private static void OnData(object sender, ConversionDataEventArgs e) => Console.WriteLine("[{0} => {1}]: {2}", e.Input?.FileInfo?.Name, e.Output?.FileInfo?.Name, e.Data);

        private static void OnComplete(object sender, ConversionCompleteEventArgs e) => Console.WriteLine("Completed conversion from {0} to {1}", e.Input?.FileInfo?.FullName, e.Output?.FileInfo?.FullName);

        private static void OnError(object sender, ConversionErrorEventArgs e) => Console.WriteLine("[{0} => {1}]: Error: {2}\n{3}", e.Input?.FileInfo?.Name, e.Output?.FileInfo?.Name, e.Exception?.ExitCode, e.Exception?.InnerException);

        private static void Main(string[] args)
        {
            var elapsed = new TimeSpan(0, 0, 60);
            var id = Guid.NewGuid();
            var capture = new WaveInEvent
            {
                DeviceNumber = 0,
                BufferMilliseconds = 1000,
                WaveFormat = new WaveFormat(44100, 2),
            };
            var writer = new WaveFileWriter($"record-{id}.wav", capture.WaveFormat);

            capture.DataAvailable += (s, a) =>
            {
                writer.Write(a.Buffer, 0, a.BytesRecorded);
            };

            capture.RecordingStopped += (s, a) =>
            {
                writer.Dispose();
                writer = null;
                capture.Dispose();
            };

            Task.Run(async () => await ExecFfm($"-y -f vfwcap -r 25 -t {elapsed:g} -i 0 front-{id}.mp4"));
            Task.Run(async () => await ExecFfm($"-y -f gdigrab -framerate 25 -t {elapsed:g} -i desktop desk-{id}.mkv"));

            capture.StartRecording();

            Thread.Sleep(elapsed);
            Console.WriteLine("Done!");
            capture.StopRecording();

            Environment.Exit(Environment.ExitCode);

        }

    }
}
