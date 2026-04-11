using System;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AudioBookManager.Core;
using OpenTelemetry.Trace;

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
            var span = Program.MainActivitySource.StartActivity("Botao Renomear");
            if (CheckReady())
            {
                using (var span1 = Program.MainActivitySource.StartActivity("RenameData"))
                {
                    span1.SetTag("Album", textBox4.Text);
                    CurrentBookCollection.SetAlbum(textBox4.Text);
                    span1.SetTag("Artist", textBox3.Text);
                    CurrentBookCollection.SetArtist(textBox3.Text);
                }
                using var span2 = Program.MainActivitySource.StartActivity("DoRename");
                await CurrentBookCollection.CreateBaseDirectory(textBox1.Text).ContinueWith(task => ResetScreen());
                span2.Stop();

            }
            span.Stop();

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

            // Disable button during operation
            var button = (Button)sender;
            button.Enabled = false;
            AppendTextBox($"Iniciando busca no Goodreads...{Environment.NewLine}");

            try
            {
                await CurrentBookCollection.LoadGoodReadsScraperAsync();
                LoadGrid();
                AppendTextBox($"Busca no Goodreads concluída!{Environment.NewLine}");
            }
            catch (Exception ex)
            {
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
