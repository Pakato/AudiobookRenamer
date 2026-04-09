using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AudioBookManager.Core;
using AudioBookManager.Core.Interface;
using Goodreads;
using Goodreads.Models.Response;
using TagLib.Riff;
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
                //await bookItem.GenerateFolder(baseDirectory);
                tasks.Add(bookItem.GenerateFolder(baseDirectory).ContinueWith(e =>
                {
                    OnLogEventHandler($"Livro Processado {bookItem.BookTitle}");
                }));
            }
            //Task.WaitAll(tasks.ToArray());
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

        private async Task HandleFolderLoad(string directory)
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
        }

        internal virtual void OnLogEventHandler(string logtext)
        {
            LogEventHandler?.Invoke($"{DateTime.Now.ToString()} {logtext}{System.Environment.NewLine}");
        }

        public void LoadGoodReads()
        {
            IGoodreadsClient GoodReadsClient = GoodreadsClient.Create("OXYjCfxKJuD3mprEP0zQg", "9YXp6q0AFZL7aRznsF9eKCY9L6PiTXdlmiLhphyKQ");
            foreach (var fol in Books)
            {
                if (fol.GoodReadsBook != null)
                    continue;
                var search = GoodReadsClient.Books.Search(StringHelper.ToTitleCase(fol.Artist, TitleCase.All) + " " +
                                                          StringHelper.ToTitleCase(fol.BookTitle, TitleCase.All));
                search.Wait();
                Work book = null;
                if (search.Result.List != null && search.Result.List.Count > 0)
                {
                    if (search.Result.List.Count == 1)
                        book = search.Result.List.First();
                    else
                    {
                        foreach (var work in search.Result.List)
                        {
                            int ss = work.BestBook.Title.ToLower().IndexOf("(");
                            if (ss <= 0)
                                ss = work.BestBook.Title.ToLower().Length;
                            if (work.BestBook.Title.ToLower().Substring(0, ss).Contains(
                                StringHelper.ToTitleCase(fol.BookTitle, TitleCase.All).ToLower()))
                            {
                                book = work;
                                break;
                            }
                        }
                    }
                }
                if (book != null)
                {
                    var getBook = GoodReadsClient.Books.GetByBookId(book.BestBook.Id);
                    getBook.Wait();
                    if (getBook.Result != null)
                    {
                        fol.GoodReadsBook = getBook.Result;
                        if (fol.GoodReadsBook.PublicationDate.HasValue)
                            fol.Year = fol.GoodReadsBook.PublicationDate.Value.Year;
                    }
                }
            }
        }
    }


}
