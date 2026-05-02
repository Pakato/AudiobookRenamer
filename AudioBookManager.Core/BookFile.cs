using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ATL;
using AudioBookManager.Core.Interface;
using AudioBookManager.Core.Telemetry;
using Serilog;

namespace AudioBookManager.Core
{
    public class BookFile : BookItem
    {
        public BookFile(string path) : base(path)
        {
            bool error = false;
            try
            {
                Track tfile = new Track(path);
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
                    using (var tgfile = TagLib.File.Create(path))
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

            Path = path;

        }

        public override IEnumerable<string> SourceFiles => new[] { Path };

        protected override async Task HandleFiles(string bookFolder)
        {
            using var activity = AudioBookTelemetry.ActivitySource.StartActivity("BookFile.HandleFiles");
            activity?.SetTag("book.title", BookTitle);
            activity?.SetTag("source.path", Path);

            var newFilePath = $"{StringHelper.ToTitleCase(BookTitle, TitleCase.All).ToSafeFileName()} - {(1).ToString().PadLeft(2, '0')}{System.IO.Path.GetExtension(Path)}";
            var newAudioPath = System.IO.Path.Combine(bookFolder, newFilePath);

            if (!System.IO.File.Exists(newAudioPath))
            {
                try
                {
                    using (FileStream SourceStream = File.Open(Path, FileMode.Open))
                    {
                        using (FileStream DestinationStream = File.Create(newAudioPath))
                        {
                            await SourceStream.CopyToAsync(DestinationStream);
                        }
                    }
                    //File.Copy(Path, newAudioPath);
                    await HandleTagFile(newAudioPath, 1);
                    //File.Delete(Path);
                }
                catch (Exception e)
                {
                    ErrorStack.Add(e);
                }
            }
        }


        protected override async Task CleanFiles()
        {
            try
            {
                File.Delete(Path);
            }
            catch (Exception e)
            {
                ErrorStack.Add(e);
            }
        }
    }
}
