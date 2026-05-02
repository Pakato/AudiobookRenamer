using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ATL;
using ATL.CatalogDataReaders;
using AudioBookManager.Core.Telemetry;
using Goodreads.Scraper.Models;
using Polly;
using Polly.Timeout;
using Serilog;
using TagLib;
using ThreadState = System.Threading.ThreadState;

namespace AudioBookManager.Core.Interface
{
    public abstract class BookItem
    {
        internal BookItem(string path)
        {
            ErrorStack = new List<Exception>();
        }

        public string Path { get; set; }
        public int BookNumber { get; set; }
        public string BookTitle { get; set; }
        public string Artist { get; set; }
        public int Bitrate { get; set; }
        public string Album { get; set; }
        public int Year { get; set; }

        /// <summary>
        /// Metadata scraped from Goodreads using the web scraper.
        /// </summary>
        public AudiobookMetadata ScrapedMetadata { get; set; }

        public List<Exception> ErrorStack { get; set; }
        public bool isNewBook = false;



        public string BookScore
        {
            get
            {
                if (ScrapedMetadata != null && ScrapedMetadata.Rating.HasValue)
                {
                    return ScrapedMetadata.Rating.Value.ToString("0.00");
                }

                return string.Empty;
            }
        }

        public bool Any()
        {
            return !string.IsNullOrEmpty(Path);
        }

        public abstract IEnumerable<string> SourceFiles { get; }

        protected abstract Task HandleFiles(string bookFolder);

        protected abstract Task CleanFiles();


        protected async Task<bool> HandleTagFile(string filePath, int track)
        {
            using var activity = AudioBookTelemetry.ActivitySource.StartActivity("BookItem.HandleTagFile");
            activity?.SetTag("file.path", filePath);
            activity?.SetTag("track.number", track);
            activity?.SetTag("book.title", BookTitle);

            bool resultt = false;
            Debugger.Log(1, "FileTagging", $"Tagging Arquivo {filePath} {Environment.NewLine}");
            Log.Debug("Tagging arquivo: {FilePath}, Track: {Track}", filePath, track);
            try
            {


                Task task = Task<bool>.Run(() =>
                {
                    try
                    {
                        using (TagLib.File file = TagLib.File.Create(filePath, ReadStyle.None))
                        {
                            file.Tag.Album = StringHelper.ToTitleCase(Album, TitleCase.All);
                            file.Tag.AlbumArtists = new string[]
                                {StringHelper.ToTitleCase(Artist, TitleCase.All)};
                            file.Tag.Disc = (uint)BookNumber;
                            file.Tag.Title = StringHelper.ToTitleCase(BookTitle, TitleCase.All);
                            file.Tag.TitleSort = StringHelper.ToTitleCase(BookTitle, TitleCase.All);
                            file.Tag.Artists = new string[] { StringHelper.ToTitleCase(Artist, TitleCase.All) };
                            file.Tag.Track = (uint)track;
                            file.Tag.TrackCount = 0;
                            file.Tag.Genres = new string[] { "AudioBook" };
                            if (Year > 0)
                                file.Tag.Year = (uint)Year;
                            file.Save();
                            resultt = true;
                        }

                        return true;
                    }
                    catch (Exception e)
                    {
                        BookCollection.CurrentConnection.OnLogEventHandler($"Erro ao criar tagging: {filePath}");
                        Debugger.Log(1, "FileTagging", $"Tagging Arquivo - Erro {filePath} - {e.Message} {Environment.NewLine}");
                        resultt = false;
                        return false;
                    }
                });

                try
                {
                    task.ConfigureAwait(false).GetAwaiter().GetResult();
                }
                catch (Exception e)
                {
                    Debugger.Log(1, "FileTagging",
                        $"Tagging Arquivo - OLD Method Execute {filePath} - {e.Message} {Environment.NewLine}");

                }

                if (!resultt)
                {
                    Task taskk = Task<bool>.Run(() =>
                    {
                        try
                        {
                            Track file = new Track(filePath);
                            file.Album = StringHelper.ToTitleCase(Album, TitleCase.All);
                            file.AlbumArtist = StringHelper.ToTitleCase(Artist, TitleCase.All);
                            file.DiscNumber = BookNumber;
                            file.Title = StringHelper.ToTitleCase(BookTitle, TitleCase.All);
                            file.Artist = StringHelper.ToTitleCase(Artist, TitleCase.All);
                            file.TrackNumber = track;
                            file.Genre = "AudioBook";
                            if (Year > 0)
                                file.Year = Year;
                            file.Save();
                            resultt = true;
                        }
                        catch (Exception e)
                        {
                            BookCollection.CurrentConnection.OnLogEventHandler($"Erro ao criar tagging backup: {filePath}");
                            Debugger.Log(1, "FileTagging", $"Tagging Arquivo - Erro {filePath} - {e.Message} {Environment.NewLine}");
                            resultt = false;
                        }
                    });
                    try
                    {
                        taskk.Wait(TimeSpan.FromMinutes(1).Milliseconds);
                    }
                    catch (Exception e)
                    {
                        Debugger.Log(1, "FileTagging",
                            $"Tagging Arquivo - Method Execute {filePath} - {e.Message} {Environment.NewLine}");
                    }
                }


                Debugger.Log(1, "FileTagging", $"Tagging Arquivo - Sucesso {filePath} {Environment.NewLine}");
                if (resultt)
                {
                    AudioBookTelemetry.FilesTagged.Add(1);
                    Log.Debug("Tagging bem sucedido: {FilePath}", filePath);
                }
                else
                {
                    AudioBookTelemetry.FileTagErrors.Add(1);
                    Log.Warning("Tagging falhou (fallback usado): {FilePath}", filePath);
                }
            }
            catch (Polly.Timeout.TimeoutRejectedException ex)
            {
                AudioBookTelemetry.FileTagErrors.Add(1);
                activity?.SetStatus(ActivityStatusCode.Error, "Timeout");
                Log.Warning(ex, "Timeout ao processar tagging: {FilePath}", filePath);
                Debugger.Log(1, "FileTagging",
                    $"Tagging Arquivo - OLD Method Execute {filePath} {Environment.NewLine}");
            }
            catch (Exception e)
            {
                AudioBookTelemetry.FileTagErrors.Add(1);
                activity?.SetStatus(ActivityStatusCode.Error, e.Message);
                Log.Error(e, "Erro ao processar tagging: {FilePath}", filePath);
                Debugger.Log(1, "FileTagging", $"Tagging Arquivo - Insucesso {filePath} {Environment.NewLine}");
                ErrorStack.Add(e);
            }
            return resultt;
        }

        protected async Task HandleCueConvert(string cuePath, string newFileName)
        {
            string text = string.Empty;
            using (FileStream file = new FileStream(cuePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite, 4096,
                FileOptions.Asynchronous))
            {
                using (var sr = new StreamReader(file))
                    text = await sr.ReadToEndAsync();
                var match = System.Text.RegularExpressions.Regex.Match(text, @"FILE ""(.*)"" .*");
                if (match.Groups.Count >= 1)
                {
                    text = text.Replace(match.Groups[1].Value, newFileName);
                }
            }
            using (FileStream file = new FileStream(cuePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite, 4096,
               FileOptions.Asynchronous))
            {
                file.SetLength(0);
                using (StreamWriter writer = new StreamWriter(file))
                    await writer.WriteAsync(text);
            }
        }

        public async Task GenerateFolder(string albumFolder)
        {
            using var activity = AudioBookTelemetry.ActivitySource.StartActivity("BookItem.GenerateFolder");
            activity?.SetTag("book.title", BookTitle);
            activity?.SetTag("book.number", BookNumber);
            activity?.SetTag("album.folder", albumFolder);

            var albumSegment = StringHelper.ToTitleCase(Album, TitleCase.All).ToSafeFileName();
            var titleSegment = StringHelper.ToTitleCase(BookTitle, TitleCase.All).ToSafeFileName();
            var bookFolderSegment = $"Book {BookNumber.ToString().PadLeft(2, '0')} - {titleSegment}";
            var path = System.IO.Path.Combine(albumFolder, albumSegment, bookFolderSegment);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            await HandleFiles(path);
            if (ErrorStack.Count == 0)
            {
                //CleanFiles();
            }
            else
            {
                ReportProcessingErrors(activity, path);
                BookCollection.CurrentConnection.OnLogEventHandler($"Erro ao processar a pasta: {path}");
            }
        }

        private void ReportProcessingErrors(Activity activity, string path)
        {
            AudioBookTelemetry.BookProcessingErrors.Add(1,
                new KeyValuePair<string, object>("book.title", BookTitle),
                new KeyValuePair<string, object>("book.number", BookNumber));

            activity?.SetStatus(ActivityStatusCode.Error, $"{ErrorStack.Count} error(s) while processing folder");
            activity?.SetTag("error.count", ErrorStack.Count);
            activity?.SetTag("output.path", path);
            activity?.SetTag("book.artist", Artist);
            activity?.SetTag("book.album", Album);
            activity?.SetTag("book.year", Year);

            for (int i = 0; i < ErrorStack.Count; i++)
            {
                var ex = ErrorStack[i];
                activity?.AddException(ex, new TagList
                {
                    { "error.index", i },
                    { "book.title", BookTitle },
                    { "output.path", path }
                });
            }

            Log.Error(
                "Erro ao processar a pasta {Path} para {BookTitle} ({ErrorCount} erros). Primeiro erro: {FirstError}",
                path, BookTitle, ErrorStack.Count, ErrorStack[0].Message);
        }
    }

}
