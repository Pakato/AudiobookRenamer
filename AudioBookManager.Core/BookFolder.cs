using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ATL;
using AudioBookManager.Core.Interface;
using AudioBookManager.Core.Telemetry;
using Serilog;

namespace AudioBookManager.Core
{
    public class BookFolder : BookItem
    {
        public List<string> AudioFiles { get; set; }
        public List<string> OtherFiles { get; set; }
        public List<string> CueFiles { get; set; }
        public BookFolder(string path) : base(path)
        {
            List<string> audioFiles = new List<string>();
            List<string> otherFiles = new List<string>();
            List<string> cueFiles = new List<string>();
            var file = Directory.GetFiles(path).ToList();
            audioFiles.AddRange(file.Where(CheckMusic));
            audioFiles.Sort(new NumericComparer<string>());
            otherFiles.AddRange(file.Where(CheckOther));
            cueFiles.AddRange(file.Where(CheckCue));
            if (audioFiles.Any())
            {
                try
                {
                    bool error = false;
                    try
                    {
                        Track tfile = new Track(audioFiles.First());
                        Album = tfile.Album;
                        Artist = tfile.Artist;
                        BookTitle = tfile.Title;
                        Bitrate = tfile.Bitrate;
                        BookNumber = tfile.DiscNumber ?? 0;
                        if (string.IsNullOrEmpty(Album) || string.IsNullOrEmpty(Artist))
                            error = true;
                    }
                    catch (Exception e)
                    {
                        error = true;
                    }

                    if (error)
                    {
                        try
                        {
                            using (var tgfile = TagLib.File.Create(audioFiles.First()))
                            {
                                Album = tgfile.Tag.Album;
                                Artist = tgfile.Tag.FirstArtist;
                                BookTitle = tgfile.Tag.Title;
                                Bitrate = tgfile.Properties.AudioBitrate;
                                BookNumber = (int)tgfile.Tag.Disc;
                            }
                        }
                        catch (Exception e)
                        {

                        }
                    }
                    //ATL.Settings

                    Path = path;
                }
                catch (Exception e)
                {
                    BookCollection.CurrentConnection.OnLogEventHandler($"Erro ao carregar tags do livro no caminho: {path}");
                }
                AudioFiles = audioFiles;
                OtherFiles = otherFiles;
                CueFiles = cueFiles;
                Path = path;
            }
        }

        protected override async Task HandleFiles(string bookFolder)
        {
            using var activity = AudioBookTelemetry.ActivitySource.StartActivity("BookFolder.HandleFiles");
            activity?.SetTag("book.title", BookTitle);
            activity?.SetTag("audio.file_count", AudioFiles.Count);
            activity?.SetTag("other.file_count", OtherFiles.Count);
            activity?.SetTag("cue.file_count", CueFiles.Count);
            Log.Debug("Processando pasta: {BookFolder}, Arquivos: {FileCount}", bookFolder, AudioFiles.Count);

            ErrorStack = new List<Exception>();
            var tasks = new List<Task>();
            int i = 0;
            AudioFiles.ForEach(audioFile => tasks.Add(HandleAudio(bookFolder, audioFile, i++).ContinueWith(e =>
            {
                if (e.Exception != null || !e.Result)
                    BookCollection.CurrentConnection.OnLogEventHandler($"Erro ao processar o arquivo: {audioFile}");
            })));
            CueFiles.ForEach(cueFiles => tasks.Add(HandleCue(bookFolder, cueFiles).ContinueWith(e =>
            {
                if (e.Exception != null || !e.Result)
                    BookCollection.CurrentConnection.OnLogEventHandler($"Erro ao processar o arquivo: {cueFiles}");
            })));
            OtherFiles.ForEach(otherFile => tasks.Add(HandleOthers(bookFolder, otherFile).ContinueWith(e =>
            {
                if (e.Exception != null || !e.Result)
                    BookCollection.CurrentConnection.OnLogEventHandler($"Erro ao processar o arquivo: {otherFile}");
            })));
            await Task.WhenAll(tasks).ContinueWith(e =>
            {
                if (tasks.Count(g => g.Exception != null) > 0)
                {
                    BookCollection.CurrentConnection.OnLogEventHandler($"Ocorreu erros ao processar a pasta: {bookFolder}");
                }
            });
        }

        protected override async Task CleanFiles()
        {
            try
            {
                AudioFiles.ForEach(e => File.Delete(e));
                CueFiles.ForEach(e => File.Delete(e));
                OtherFiles.ForEach(e => File.Delete(e));
                var file = Directory.GetFiles(Path).ToList();
                var count = file.Where(CheckMusic).Count() + file.Where(CheckOther).Count() +
                            file.Where(CheckCue).Count();
                if (count == 0)
                    Directory.Delete(Path, true);
            }
            catch (Exception e)
            {
                ErrorStack.Add(e);
            }
        }

        private async Task<bool> HandleAudio(string bookFolder, string audioFile, int i)
        {
            var newFilePath = $"{StringHelper.ToTitleCase(BookTitle, TitleCase.All)} - " +
                              $"{(i + 1).ToString().PadLeft(2, '0')}" +
                              $"{System.IO.Path.GetExtension(audioFile)}";
            var newAudioPath = System.IO.Path.Combine(bookFolder, newFilePath);

            if (!System.IO.File.Exists(newAudioPath))
            {
                var tempFilePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
                    $"{Guid.NewGuid()}_{System.IO.Path.GetFileName(newFilePath)}");

                try
                {
                    Debugger.Log(1, "FileCopy", $"Copiando Arquivo (para temp) {newFilePath} {Environment.NewLine}");
                    using (var sourceStream = new FileStream(audioFile, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
                    using (var tempStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite, 4096, true))
                    {
                        await sourceStream.CopyToAsync(tempStream).ConfigureAwait(false);
                    }
                    await HandleTagFile(tempFilePath, i + 1);
                    Debugger.Log(1, "FileCopy", $"Copiando Arquivo (para destino) {newFilePath} {Environment.NewLine}");
                    using (var tempSourceStream = new FileStream(tempFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
                    using (var destinationStream = new FileStream(newAudioPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.ReadWrite, 4096, true))
                    {
                        await tempSourceStream.CopyToAsync(destinationStream).ConfigureAwait(false);
                    }

                    Debugger.Log(1, "FileCopy", $"Copiando Arquivo - Sucesso {newFilePath} {Environment.NewLine}");
                    
                }
                catch (Exception e)
                {
                    ErrorStack.Add(e);
                    return false;
                }
                finally
                {
                    try
                    {
                        if (System.IO.File.Exists(tempFilePath))
                        {
                            System.IO.File.Delete(tempFilePath);
                        }
                    }
                    catch (Exception cleanupException)
                    {
                        ErrorStack.Add(cleanupException);
                    }
                }
            }
            return true;
        }

        private async Task<bool> HandleOthers(string bookFolder, string otherFile)
        {
            var newFilePath = $"{StringHelper.ToTitleCase(BookTitle, TitleCase.All)} - " +
                              $"{(1).ToString().PadLeft(2, '0')}" +
                              $"{System.IO.Path.GetExtension(otherFile)}";
            var newPath = System.IO.Path.Combine(bookFolder, newFilePath);

            if (CheckImage(otherFile))
            {
                if (!System.IO.File.Exists(newPath))
                {
                    try
                    {
                        using (FileStream file = new FileStream(otherFile, FileMode.Open, FileAccess.Read,
                            FileShare.Read, 4096,
                            FileOptions.Asynchronous))
                        {
                            using (FileStream fileTo = new FileStream(newPath, FileMode.CreateNew, FileAccess.Write,
                                FileShare.ReadWrite, 4096,
                                FileOptions.Asynchronous))
                            {
                                Task copy = file.CopyToAsync(fileTo);
                                if (copy != null)
                                {
                                    await copy.ConfigureAwait(false);
                                    if (copy?.Exception != null)
                                    {
                                        ErrorStack.Add(copy.Exception);
                                        return false;
                                    }
                                }
                            }
                        }

                        //File.Copy(otherFile, newPath);
                        //File.Delete(otherFile);
                    }
                    catch (Exception e)
                    {
                        ErrorStack.Add(e);
                        return false;
                    }
                }
            }
            else
            {
                if (!System.IO.File.Exists(
                    System.IO.Path.Combine(bookFolder, newPath)))
                {
                    try
                    {
                        using (FileStream file = new FileStream(otherFile, FileMode.Open, FileAccess.Read, FileShare.Read, 4096,
                            FileOptions.Asynchronous))
                        using (FileStream fileTo = new FileStream(newPath, FileMode.CreateNew, FileAccess.Write,
                            FileShare.ReadWrite, 4096,
                            FileOptions.Asynchronous))
                        {
                            Task copy = file.CopyToAsync(fileTo);
                            copy.ConfigureAwait(false).GetAwaiter().GetResult();
                            if (copy != null)
                            {
                                if (copy?.Exception != null)
                                {
                                    ErrorStack.Add(copy.Exception);
                                    return false;
                                }
                            }
                        }

                        //File.Copy(otherFile, newPath);
                        //File.Delete(otherFile);
                    }
                    catch (Exception e)
                    {
                        ErrorStack.Add(e);
                        return false;
                    }
                }
            }

            return true;
        }

        private async Task<bool> HandleCue(string bookFolder, string cueFiles)
        {
            var newFilePath = $"{StringHelper.ToTitleCase(BookTitle, TitleCase.All)} - " +
                              $"{(1).ToString().PadLeft(2, '0')}" +
                              $"{System.IO.Path.GetExtension(cueFiles)}";
            var newFilePathFile = $"{StringHelper.ToTitleCase(BookTitle, TitleCase.All)} - " +
                                  $"{(1).ToString().PadLeft(2, '0')}" +
                                  $"{System.IO.Path.GetExtension(AudioFiles.First())}";
            var newPath = System.IO.Path.Combine(bookFolder, newFilePath);

            if (!System.IO.File.Exists(newPath))
            {
                try
                {
                    using (FileStream file = new FileStream(cueFiles, FileMode.Open, FileAccess.Read, FileShare.Read, 4096,
                        FileOptions.Asynchronous))
                    using (FileStream fileTo = new FileStream(newPath, FileMode.CreateNew, FileAccess.ReadWrite,
                        FileShare.ReadWrite, 4096,
                        FileOptions.Asynchronous))
                    {
                        Task copy = file.CopyToAsync(fileTo);
                        copy.ConfigureAwait(false).GetAwaiter().GetResult();
                        if (copy?.Exception != null)
                        {
                            ErrorStack.Add(copy.Exception);
                            return false;
                        }
                    }
                    await HandleCueConvert(newPath, newFilePathFile);
                    //File.Copy(cueFiles, newPath);
                    //await HandleCueConvert(newPath, newFilePathFile);
                    //File.Delete(cueFiles);
                }
                catch (Exception e)
                {
                    ErrorStack.Add(e);
                    return false;
                }
            }
            return true;

        }



        private Func<string, bool> CheckMusic = (s) => s.EndsWith("mp3", StringComparison.InvariantCultureIgnoreCase) || s.EndsWith("m4b", StringComparison.InvariantCultureIgnoreCase) || s.EndsWith("m4a", StringComparison.InvariantCultureIgnoreCase) || s.EndsWith("mp4", StringComparison.InvariantCultureIgnoreCase) || s.EndsWith("opus", StringComparison.InvariantCultureIgnoreCase);

        private Func<string, bool> CheckOther = (s) => s.EndsWith("xml", StringComparison.InvariantCultureIgnoreCase) || s.EndsWith("dat", StringComparison.InvariantCultureIgnoreCase) || s.EndsWith("jpg", StringComparison.InvariantCultureIgnoreCase) || s.EndsWith("gif", StringComparison.InvariantCultureIgnoreCase) || s.EndsWith("png", StringComparison.InvariantCultureIgnoreCase) || s.EndsWith("dat", StringComparison.InvariantCultureIgnoreCase);

        private Func<string, bool> CheckImage = (s) => s.EndsWith("jpg", StringComparison.InvariantCultureIgnoreCase) || s.EndsWith("gif", StringComparison.InvariantCultureIgnoreCase) || s.EndsWith("png", StringComparison.InvariantCultureIgnoreCase);

        private Func<string, bool> CheckCue = (s) => s.EndsWith("cue", StringComparison.InvariantCultureIgnoreCase);
    }
}
