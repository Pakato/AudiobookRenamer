using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AudioBookManager.Core.Interface;
using AudioBookManager.Core.Telemetry;
using Goodreads.Scraper;
using Goodreads.Scraper.Configuration;
using Goodreads.Scraper.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Serilog;
using File = System.IO.File;

namespace AudioBookManager.Core
{
    public class BookCollection
    {
        public List<BookItem> Books { get; set; }
        public event LogEvent LogEventHandler;
        public string FoundArtist { get; set; }
        public string FoundAlbum { get; set; }
        public delegate void LogEvent(string logText);

        internal static BookCollection CurrentConnection;
        internal BookCollection()
        {
            Books = new List<BookItem>();
            OnLogEventHandler($"Criando novo processamento");
            FoundArtist = string.Empty;
            FoundAlbum = string.Empty;
        }

        public static BookCollection Create()
        {
            CurrentConnection = new BookCollection();
            return CurrentConnection;
        }

        public async Task CreateBaseDirectory(string rootPath)
        {
            using var activity = AudioBookTelemetry.ActivitySource.StartActivity("BookCollection.CreateBaseDirectory");
            activity?.SetTag("book.count", Books.Count);
            activity?.SetTag("root.path", rootPath);
            AudioBookTelemetry.ActiveOperations.Add(1);
            var sw = Stopwatch.StartNew();

            OnLogEventHandler($"Iniciando processamento");
            Log.Information("Iniciando processamento de {BookCount} livros em {RootPath}", Books.Count, rootPath);

            string newName = $"{StringHelper.ToTitleCase(ReturnArtist(true), TitleCase.All)}";
            string baseDirectory = Path.Combine(rootPath, newName);
            if (!Directory.Exists(Path.Combine(rootPath, newName)))
                Directory.CreateDirectory(baseDirectory);
            List<Task> tasks = new List<Task>();
            foreach (var bookItem in Books)
            {
                OnLogEventHandler($"Processando Livro {bookItem.BookTitle}");
                Log.Debug("Processando livro: {BookTitle}", bookItem.BookTitle);
                tasks.Add(bookItem.GenerateFolder(baseDirectory).ContinueWith(e =>
                {
                    AudioBookTelemetry.BooksProcessed.Add(1);
                    OnLogEventHandler($"Livro Processado {bookItem.BookTitle}");
                }));
            }

            await Task.WhenAll(tasks).ContinueWith(e =>
            {
                OnLogEventHandler($"Processamento finalizado");
            });

            sw.Stop();
            AudioBookTelemetry.BookProcessingDuration.Record(sw.Elapsed.TotalMilliseconds);
            AudioBookTelemetry.ActiveOperations.Add(-1);
            activity?.SetTag("duration.ms", sw.Elapsed.TotalMilliseconds);
            Log.Information("Processamento finalizado em {DurationMs}ms", sw.Elapsed.TotalMilliseconds);
        }

        public void AddBook(string path, bool isNewBook = false)
        {
            using var activity = AudioBookTelemetry.ActivitySource.StartActivity("BookCollection.AddBook");
            activity?.SetTag("book.path", path);
            activity?.SetTag("book.is_new", isNewBook);

            if (File.Exists(path))
            {
                var book = new BookFile(path);
                if (book.Any())
                {
                    book.isNewBook = isNewBook;
                    Books.Add(book);
                    AudioBookTelemetry.BooksLoaded.Add(1);
                    Log.Information("Livro adicionado: {BookTitle} em {Path}", book.BookTitle, book.Path);
                    OnLogEventHandler($"Adicionando Livro {book.BookTitle} - {book.Path}");
                }
            }
            else
            {
                var book = new BookFolder(path);
                if (book.Any())
                {
                    book.isNewBook = isNewBook;
                    Books.Add(book);
                    AudioBookTelemetry.BooksLoaded.Add(1);
                    Log.Information("Livro adicionado: {BookTitle} em {Path}", book.BookTitle, book.Path);
                    OnLogEventHandler($"Adicionando Livro {book.BookTitle} - {book.Path}");
                }
                foreach (var dir in Directory.GetDirectories(path))
                {
                    var bookdir = new BookFolder(dir);
                    if (bookdir.Any())
                    {
                        Books.Add(bookdir);
                        AudioBookTelemetry.BooksLoaded.Add(1);
                        OnLogEventHandler($"Adicionando Livro {bookdir.BookTitle} - {book.Path}");
                    }
                }

            }
        }

        public string ReturnArtist(bool forceRefresh = false)
        {
            if (!string.IsNullOrEmpty(FoundArtist) && !forceRefresh)
                return FoundArtist;
            var nameGroup = Books.Select(item => item.Artist).GroupBy(x => x);
            var maxCount = nameGroup.Max(g => g.Count());
            if (maxCount == 1)
                return nameGroup.OrderBy(x => x?.Key?.Length).First().Key;
            var mostCommons = nameGroup.Where(x => x.Count() == maxCount).Select(x => x.Key).First();
            return mostCommons;
        }

        public string ReturnAlbum(bool forceRefresh = false)
        {
            if (!string.IsNullOrEmpty(FoundAlbum) && !forceRefresh)
                return FoundAlbum;
            var nameGroup = Books.Select(item => item.Album).GroupBy(x => x);
            var maxCount = nameGroup.Max(g => g.Count());
            if (maxCount == 1)
                return nameGroup.OrderBy(x => x?.Key?.Length).First().Key;
            var mostCommons = nameGroup.Where(x => x.Count() == maxCount).Select(x => x.Key).First();
            return mostCommons;
        }

        public void SetArtist(string artist)
        {
            Books.ForEach(item => item.Artist = artist);
        }

        public void SetAlbum(string album)
        {
            Books.ForEach(item => item.Album = album);
        }

        public async Task LoadCurrentFolder(string rootPath, string artist, string album)
        {
            OnLogEventHandler("Procurando Livros existentes");
            string newName = $"{StringHelper.ToTitleCase(artist, TitleCase.All)}\\{StringHelper.ToTitleCase(album, TitleCase.All)}";
            string selectedPath = "";
            if (Directory.Exists(Path.Combine(rootPath, newName)))
            {
                selectedPath = Path.Combine(rootPath, newName);
            }
            else
            {
                var nonSymbolic = Regex.Replace(newName, "[^a-zA-Z0-9]", "");
                var dirs = Directory.GetDirectories(rootPath);
                selectedPath = dirs.FirstOrDefault(e => Regex.Replace(System.IO.Path.GetFileName(e), "[^a-zA-Z0-9]", "") == nonSymbolic);
            }

            await LoadCurrentFolderSelected(selectedPath);
        }

        public async Task LoadCurrentFolderSelected(string selectedPath)
        {
            using var activity = AudioBookTelemetry.ActivitySource.StartActivity("BookCollection.LoadCurrentFolderSelected");
            activity?.SetTag("selected.path", selectedPath);

            if (!string.IsNullOrEmpty(selectedPath))
            {
                OnLogEventHandler("Carregando Livros existentes");
                Log.Information("Carregando livros de {SelectedPath}", selectedPath);
                List<Task> foldersLoad = Directory.GetDirectories(selectedPath)
                    .Select(directory => HandleFolderLoad(directory)).ToList();
                Task.WaitAll(foldersLoad.ToArray());
                activity?.SetTag("folders.loaded", foldersLoad.Count);
            }
            else
            {
                OnLogEventHandler("Não foram encontrados livros");
                Log.Warning("Nenhum livro encontrado no caminho selecionado");
            }

            FoundAlbum = ReturnAlbum(true);
            FoundArtist = ReturnArtist(true);
        }

        private Task HandleFolderLoad(string directory)
        {
            if (!Books.Exists(item => item.Path == directory))
            {
                var book = new BookFolder(directory);
                if (book.Any())
                {
                    Books.Add(book);
                    OnLogEventHandler($"Adicionando Livro {book.BookTitle}");
                }
            }
            return Task.CompletedTask;
        }

        internal virtual void OnLogEventHandler(string logtext)
        {
            LogEventHandler?.Invoke($"{DateTime.Now.ToString()} {logtext}{System.Environment.NewLine}");
        }

        /// <summary>
        /// Loads book metadata from Goodreads using the Puppeteer web scraper (headless browser).
        /// This method is async and should be used instead of LoadGoodReads() as the API is discontinued.
        /// Uses PuppeteerSharp for better JavaScript support and anti-bot evasion.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task LoadGoodReadsScraperAsync(CancellationToken cancellationToken = default)
        {
            using var activity = AudioBookTelemetry.ActivitySource.StartActivity("BookCollection.LoadGoodReadsScraperAsync");
            activity?.SetTag("book.count", Books.Count);
            AudioBookTelemetry.ActiveOperations.Add(1);
            var sw = Stopwatch.StartNew();

            OnLogEventHandler("Iniciando busca no Goodreads (Puppeteer Browser)...");
            Log.Information("Iniciando scraping do Goodreads para {BookCount} livros", Books.Count);

            // Create scraper service with default settings
            var settings = new GoodreadsScraperSettings
            {
                RequestDelayMs = 2500,  // Slightly longer delay for browser-based scraping
                MaxRetries = 3,
                MaxSearchResults = 10,
                TimeoutSeconds = 60
            };

            var optionsWrapper = Options.Create(settings);
            var logger = NullLogger<GoodreadsPuppeteerScraperService>.Instance;

            await using var scraperService = new GoodreadsPuppeteerScraperService(optionsWrapper, logger);

            OnLogEventHandler("Browser headless inicializado...");

            foreach (var book in Books)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                // Skip if already has metadata
                if (book.ScrapedMetadata != null)
                {
                    OnLogEventHandler($"Livro já possui metadata: {book.BookTitle}");
                    continue;
                }

                try
                {
                    using var searchActivity = AudioBookTelemetry.ActivitySource.StartActivity("Goodreads.SearchBook");
                    var searchQuery = $"{StringHelper.ToTitleCase(book.Artist, TitleCase.All)} {StringHelper.ToTitleCase(book.BookTitle, TitleCase.All)}";
                    searchActivity?.SetTag("search.query", searchQuery);
                    searchActivity?.SetTag("book.title", book.BookTitle);
                    OnLogEventHandler($"Buscando: {searchQuery}");
                    Log.Debug("Buscando no Goodreads: {SearchQuery}", searchQuery);

                    AudioBookTelemetry.GoodreadsSearches.Add(1);
                    var searchResults = await scraperService.SearchBooksAsync(searchQuery, cancellationToken);

                    if (searchResults == null || searchResults.Count == 0)
                    {
                        AudioBookTelemetry.GoodreadsSearchMisses.Add(1);
                        searchActivity?.SetTag("search.result", "miss");
                        OnLogEventHandler($"Nenhum resultado encontrado para: {book.BookTitle}");
                        Log.Warning("Nenhum resultado no Goodreads para: {BookTitle}", book.BookTitle);
                        continue;
                    }
                    AudioBookTelemetry.GoodreadsSearchHits.Add(1);
                    searchActivity?.SetTag("search.result", "hit");
                    searchActivity?.SetTag("search.result_count", searchResults.Count);

                    // Find best match
                    GoodreadsSearchResult bestMatch = null;
                    var normalizedBookTitle = StringHelper.ToTitleCase(book.BookTitle, TitleCase.All).ToLower();

                    if (searchResults.Count == 1)
                    {
                        bestMatch = searchResults[0];
                    }
                    else
                    {
                        foreach (var result in searchResults)
                        {
                            var resultTitle = result.Title.ToLower();
                            int parenIndex = resultTitle.IndexOf("(");
                            if (parenIndex <= 0)
                                parenIndex = resultTitle.Length;

                            if (resultTitle.Substring(0, parenIndex).Contains(normalizedBookTitle))
                            {
                                bestMatch = result;
                                break;
                            }
                        }

                        // If no exact match, use first result
                        bestMatch ??= searchResults[0];
                    }

                    if (bestMatch != null && !string.IsNullOrEmpty(bestMatch.BookId))
                    {
                        using var metadataActivity = AudioBookTelemetry.ActivitySource.StartActivity("Goodreads.GetMetadata");
                        metadataActivity?.SetTag("book.id", bestMatch.BookId);
                        metadataActivity?.SetTag("book.title", bestMatch.Title);
                        OnLogEventHandler($"Obtendo detalhes para: {bestMatch.Title}");
                        var metadata = await scraperService.GetBookMetadataAsync(bestMatch.BookId, cancellationToken);

                        if (metadata != null)
                        {
                            book.ScrapedMetadata = metadata;
                            if (metadata.Year.HasValue)
                                book.Year = metadata.Year.Value;
                            if (!string.IsNullOrEmpty(metadata.PrimaryAuthor))
                                book.Artist = metadata.PrimaryAuthor;
                            if (!string.IsNullOrEmpty(metadata.Series)) {
                                book.Album = metadata.Series;
                                if (!string.IsNullOrEmpty(metadata.SeriesNumber))
                                    book.BookTitle = $"{metadata.Series} {metadata.SeriesNumber}";
                            }
                            if (!string.IsNullOrEmpty(metadata.SeriesNumber) && int.TryParse(metadata.SeriesNumber, out int bookNumber))
                                book.BookNumber = bookNumber;




                            OnLogEventHandler($"Metadata obtido: {metadata.Title} ({metadata.Year}) - Rating: {metadata.Rating}");
                            Log.Information("Metadata obtido: {Title} ({Year}) Rating: {Rating}", metadata.Title, metadata.Year, metadata.Rating);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    OnLogEventHandler("Operação cancelada pelo usuário.");
                    Log.Warning("Operação de scraping cancelada pelo usuário");
                    break;
                }
                catch (Exception ex)
                {
                    AudioBookTelemetry.GoodreadsErrors.Add(1);
                    Activity.Current?.SetStatus(ActivityStatusCode.Error, ex.Message);
                    Log.Error(ex, "Erro ao buscar metadata para {BookTitle}", book.BookTitle);
                    OnLogEventHandler($"Erro ao buscar {book.BookTitle}: {ex.Message}");
                }
            }

            sw.Stop();
            AudioBookTelemetry.GoodreadsScrapeDuration.Record(sw.Elapsed.TotalMilliseconds);
            AudioBookTelemetry.ActiveOperations.Add(-1);
            activity?.SetTag("duration.ms", sw.Elapsed.TotalMilliseconds);
            Log.Information("Busca no Goodreads finalizada em {DurationMs}ms", sw.Elapsed.TotalMilliseconds);
            OnLogEventHandler("Busca no Goodreads finalizada.");
        }
    }


}
