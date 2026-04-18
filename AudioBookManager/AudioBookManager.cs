using System;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AudioBookManager.Core;
using AudioBookManager.Core.Telemetry;
using Serilog;

namespace AudioBookManager
{
    public partial class AudioBookManager : Form
    {
        public BookCollection CurrentBookCollection = null;
        public AudioBookManager()
        {
            InitializeComponent();
            AppStart.Configure();
            ConfigureApp();

        }

        private void ConfigureApp()
        {
            textBox1.Text = AppStart.DefaultPath;
            textBox2.Text = AppStart.DownloadPath;
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.SelectedPath = textBox2.Text;
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                using var activity = AudioBookTelemetry.ActivitySource.StartActivity("UI.LoadFromFolder");
                activity?.SetTag("source.path", folderBrowserDialog1.SelectedPath);
                Log.Information("Carregando livros da pasta: {Path}", folderBrowserDialog1.SelectedPath);
                CreateNewBookCollection();
                CurrentBookCollection.AddBook(folderBrowserDialog1.SelectedPath, true);
                LoadGrid();
            }
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            openFileDialog1.InitialDirectory = textBox2.Text;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                using var activity = AudioBookTelemetry.ActivitySource.StartActivity("UI.LoadFromFiles");
                activity?.SetTag("file.count", openFileDialog1.FileNames.Length);
                Log.Information("Carregando {FileCount} arquivo(s)", openFileDialog1.FileNames.Length);
                CreateNewBookCollection();
                openFileDialog1.FileNames.ToList().ForEach(file => CurrentBookCollection.AddBook(file));
                LoadGrid();
            }
        }

        public void LoadGrid()
        {
            CleanDataGrid();
            CleanTextBox3();
            CleanTextBox4();
            textBox3.Text = CurrentBookCollection.ReturnArtist();
            textBox4.Text = CurrentBookCollection.ReturnAlbum();
            dataGridView1.AutoGenerateColumns = false;
            dataGridView1.DataSource = CurrentBookCollection.Books;
        }

        private async void Button3_Click(object sender, EventArgs e)
        {
            using var activity = AudioBookTelemetry.ActivitySource.StartActivity("UI.Rename");
            if (CheckReady())
            {
                using (var renameActivity = AudioBookTelemetry.ActivitySource.StartActivity("UI.Rename.SetData"))
                {
                    renameActivity?.SetTag("album", textBox4.Text);
                    renameActivity?.SetTag("artist", textBox3.Text);
                    CurrentBookCollection.SetAlbum(textBox4.Text);
                    CurrentBookCollection.SetArtist(textBox3.Text);
                }
                Log.Information("Iniciando renomeação - Artist: {Artist}, Album: {Album}", textBox3.Text, textBox4.Text);
                using var processActivity = AudioBookTelemetry.ActivitySource.StartActivity("UI.Rename.Process");
                await CurrentBookCollection.CreateBaseDirectory(textBox1.Text).ContinueWith(task => ResetScreen());
            }
        }

        public bool CheckReady()
        {
            StringBuilder errors = new StringBuilder();
            if (CurrentBookCollection.Books.Any(x => x.BookNumber == 0))
                errors.AppendLine($"Nao pode esquecer de preencher o numero do livro");

            if (errors.Length > 0)
            {
                MessageBox.Show(errors.ToString());
                return false;
            }
            return true;
        }


        public void CleanTextBox3()
        {
            if (textBox3.InvokeRequired)
            {
                textBox3.Invoke(new Action(CleanTextBox3));
                return;
            }
            textBox3.Text = string.Empty;
        }

        public void CleanTextBox4()
        {
            if (textBox4.InvokeRequired)
            {
                textBox4.Invoke(new Action(CleanTextBox4));
                return;
            }
            textBox4.Text = string.Empty;
        }

        public void CleanDataGrid()
        {
            if (dataGridView1.InvokeRequired)
            {
                dataGridView1.Invoke(new System.Action(CleanDataGrid));
                return;
            }
            dataGridView1.DataSource = null;
            dataGridView1.Refresh();
        }

        private async void Button4_Click(object sender, EventArgs e)
        {
            using var activity = AudioBookTelemetry.ActivitySource.StartActivity("UI.LoadCurrentFolder");
            Log.Information("Carregando pasta atual: {Path}", textBox1.Text);
            await CurrentBookCollection.LoadCurrentFolder(textBox1.Text, textBox3.Text, textBox4.Text);
            LoadGrid();
        }

        private void Button5_Click(object sender, EventArgs e)
        {
            ResetScreen();
        }

        public void ResetScreen()
        {
            CleanDataGrid();
            CleanTextBox3();
            CleanTextBox4();
            CurrentBookCollection = null;
            AppendTextBox($"Resetando o processamento{System.Environment.NewLine}");
        }

        private void CreateNewBookCollection()
        {
            CurrentBookCollection = BookCollection.Create();
            CurrentBookCollection.LogEventHandler += AppendTextBox;
        }

        public void AppendTextBox(string value)
        {
            if (textBox5.InvokeRequired)
            {
                textBox5.BeginInvoke(new Action<string>(AppendTextBox), new object[] { value });
                return;
            }
            textBox5.SuspendLayout();
            textBox5.AppendText(value);
            textBox5.SelectionStart = textBox5.TextLength;
            textBox5.ScrollToCaret();
            textBox5.ResumeLayout();
        }

        private async void Button6_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.SelectedPath = textBox1.Text;
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                using var activity = AudioBookTelemetry.ActivitySource.StartActivity("UI.LoadSelectedFolder");
                activity?.SetTag("selected.path", folderBrowserDialog1.SelectedPath);
                Log.Information("Carregando pasta selecionada: {Path}", folderBrowserDialog1.SelectedPath);
                await CurrentBookCollection.LoadCurrentFolderSelected(folderBrowserDialog1.SelectedPath);
                LoadGrid();
            }
        }

        private async void Button7_Click(object sender, EventArgs e)
        {
            if (CurrentBookCollection == null || !CurrentBookCollection.Books.Any())
            {
                MessageBox.Show("Nenhum livro carregado. Adicione livros primeiro.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var activity = AudioBookTelemetry.ActivitySource.StartActivity("UI.GoodreadsScrape");
            activity?.SetTag("book.count", CurrentBookCollection.Books.Count);

            var button = (Button)sender;
            button.Enabled = false;
            AppendTextBox($"Iniciando busca no Goodreads...{Environment.NewLine}");
            Log.Information("Iniciando busca no Goodreads para {BookCount} livros", CurrentBookCollection.Books.Count);

            try
            {
                await CurrentBookCollection.LoadGoodReadsScraperAsync();
                LoadGrid();
                AppendTextBox($"Busca no Goodreads concluída!{Environment.NewLine}");
                Log.Information("Busca no Goodreads concluída");
            }
            catch (Exception ex)
            {
                activity?.SetStatus(System.Diagnostics.ActivityStatusCode.Error, ex.Message);
                Log.Error(ex, "Erro ao buscar no Goodreads");
                AppendTextBox($"Erro ao buscar no Goodreads: {ex.Message}{Environment.NewLine}");
                MessageBox.Show($"Erro ao buscar no Goodreads: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                button.Enabled = true;
            }
        }
    }
}
