using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AudioBookManager.Core.Interface;
using Goodreads.Scraper;
using Goodreads.Scraper.Configuration;
using Goodreads.Scraper.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
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
            OnLogEventHandler($"Iniciando processamento");
            string newName = $"{StringHelper.ToTitleCase(ReturnArtist(true), TitleCase.All)}";
            string baseDirectory = Path.Combine(rootPath, newName);
            if (!Directory.Exists(Path.Combine(rootPath, newName)))
                Directory.CreateDirectory(baseDirectory);
            List<Task> tasks = new List<Task>();
            foreach (var bookItem in Books)
            {
                OnLogEventHandler($"Processando Livro {bookItem.BookTitle}");
                tasks.Add(bookItem.GenerateFolder(baseDirectory).ContinueWith(e =>
                {
                    OnLogEventHandler($"Livro Processado {bookItem.BookTitle}");
                }));
            }

            await Task.WhenAll(tasks).ContinueWith(e =>
            {
                OnLogEventHandler($"Processamento finalizado");
            });
        }

        public void AddBook(string path, bool isNewBook = false)
        {
            if (File.Exists(path))
            {
                var book = new BookFile(path);
                if (book.Any())
                {
                    book.isNewBook = isNewBook;
                    Books.Add(book);
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
                    OnLogEventHandler($"Adicionando Livro {book.BookTitle} - {book.Path}");
                }
                foreach (var dir in Directory.GetDirectories(path))
                {
                    var bookdir = new BookFolder(dir);
                    if (bookdir.Any())
                    {
                        Books.Add(bookdir);
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
            if (!string.IsNullOrEmpty(selectedPath))
            {
                OnLogEventHandler("Carregando Livros existentes");
                List<Task> foldersLoad = Directory.GetDirectories(selectedPath)
                    .Select(directory => HandleFolderLoad(directory)).ToList();
                Task.WaitAll(foldersLoad.ToArray());
            }
            else
            {
                OnLogEventHandler("Não foram encontrados livros");
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
            OnLogEventHandler("Iniciando busca no Goodreads (Puppeteer Browser)...");

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
                    var searchQuery = $"{StringHelper.ToTitleCase(book.Artist, TitleCase.All)} {StringHelper.ToTitleCase(book.BookTitle, TitleCase.All)}";
                    OnLogEventHandler($"Buscando: {searchQuery}");

                    var searchResults = await scraperService.SearchBooksAsync(searchQuery, cancellationToken);

                    if (searchResults == null || searchResults.Count == 0)
                    {
                        OnLogEventHandler($"Nenhum resultado encontrado para: {book.BookTitle}");
                        continue;
                    }

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
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    OnLogEventHandler("Operação cancelada pelo usuário.");
                    break;
                }
                catch (Exception ex)
                {
                    OnLogEventHandler($"Erro ao buscar {book.BookTitle}: {ex.Message}");
                }
            }

            OnLogEventHandler("Busca no Goodreads finalizada.");
        }
    }


}
