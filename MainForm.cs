using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;

namespace FotoEnvio
{
    public partial class MainForm : Form
    {
        // ── Controles principais ───────────────────────────────────────
        private TabControl tabControl;
        private TabPage tabEnvio, tabConexao, tabConfig;

        // ── Aba Envio ──────────────────────────────────────────────────
        private Panel panelInicio, panelDados, panelProgresso, panelSucesso;
        private Button btnNovoCliente;
        private ListView lstFotos;
        private ImageList imageListFotos;
        private Button btnAdicionarFotos, btnRemoverFoto;
        private Label lblFotosCount;
        private TextBox txtNome, txtEmail, txtTelefone;
        private Button btnEnviar;
        private ProgressBar progressBar;
        private Label lblProgresso, lblSucesso;
        private Button btnNovoClienteApos;

        // Labels do painel sucesso (para reposicionar)
        private Label lblSucessoIcon, lblSucessoDetalhe;

        // ── Aba Configurações ──────────────────────────────────────────
        private TextBox txtDiretorio, txtServidorNAS, txtDownloadFtp;
        private Button btnBrowseDiretorio, btnSalvarConfig, btnBrowseDownloadFtp;
        private CheckBox chkCriarSubpastaData;
        private CheckBox chkVerificarConexao;

        // ── Estado ────────────────────────────────────────────────────
        private List<string> _fotos = new List<string>();

        // ─────────────────────────────────────────────────────────────
        public MainForm()
        {
            DatabaseHelper.Initialize();
            SettingsManager.Load();
            InitializeComponent();
            CarregarConfiguracoes();
        }

        private void InitializeComponent()
        {
            this.Text = "FotoEnvio – Gestão de Fotos de Clientes";
            this.Size = new Size(900, 660);
            this.MinimumSize = new Size(780, 560);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 9.5f);
            this.BackColor = Color.FromArgb(245, 247, 250);

            tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10f),
                Padding = new Point(16, 6)
            };

            tabEnvio   = new TabPage("  📷  Envio de Fotos  ");
            tabConexao = new TabPage("  🌐  Conexão  ");
            tabConfig  = new TabPage("  ⚙  Configurações  ");

            tabControl.TabPages.AddRange(new[] { tabEnvio, tabConexao, tabConfig });
            this.Controls.Add(tabControl);

            BuildAbaEnvio();
            BuildAbaConexao();
            BuildAbaConfig();
        }

        // ══════════════════════════════════════════════════════════════
        // ABA ENVIO
        // ══════════════════════════════════════════════════════════════
        private void BuildAbaEnvio()
        {
            tabEnvio.BackColor = Color.FromArgb(245, 247, 250);

            // ── Painel Início ──────────────────────────────────────────
            panelInicio = new Panel { Dock = DockStyle.Fill };

            var lblBV = MakeLabel("Bem-vindo ao FotoEnvio", 0, 60,
                new Font("Segoe UI", 16f, FontStyle.Bold), Color.FromArgb(40, 60, 100));

            var lblDesc = MakeLabel("Organize e envie fotos de clientes para o servidor NAS.", 0, 106,
                new Font("Segoe UI", 10f), Color.FromArgb(100, 110, 130));

            btnNovoCliente = MakeButton("＋  Novo Cliente", Color.FromArgb(37, 99, 235));
            btnNovoCliente.Size = new Size(200, 52);
            btnNovoCliente.Location = new Point(0, 158);
            btnNovoCliente.Click += (s, e) => OnNovoCliente();

            panelInicio.Controls.AddRange(new Control[] { lblBV, lblDesc, btnNovoCliente });
            panelInicio.Resize += (s, e) =>
            {
                int cx = panelInicio.Width / 2;
                lblBV.Left   = cx - lblBV.Width / 2;
                lblDesc.Left = cx - lblDesc.Width / 2;
                btnNovoCliente.Left = cx - btnNovoCliente.Width / 2;
            };

            // ── Painel Dados ───────────────────────────────────────────
            panelDados = new Panel { Dock = DockStyle.Fill, Visible = false,
                BackColor = Color.FromArgb(245, 247, 250) };
            BuildPanelDados();

            // ── Painel Progresso ───────────────────────────────────────
            panelProgresso = new Panel { Dock = DockStyle.Fill, Visible = false,
                BackColor = Color.FromArgb(245, 247, 250) };
            BuildPanelProgresso();

            // ── Painel Sucesso ─────────────────────────────────────────
            panelSucesso = new Panel { Dock = DockStyle.Fill, Visible = false,
                BackColor = Color.FromArgb(245, 247, 250) };
            BuildPanelSucesso();

            tabEnvio.Controls.AddRange(new Control[]
                { panelSucesso, panelProgresso, panelDados, panelInicio });
        }

        private void BuildPanelDados()
        {
            var lblTitulo = MakeLabel("Novo Cliente", 20, 14,
                new Font("Segoe UI", 14f, FontStyle.Bold), Color.FromArgb(37, 99, 235));

            // GroupBox dados
            var grpDados = new GroupBox
            {
                Text = "Dados do Cliente",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 80, 120),
                Location = new Point(20, 48),
                Size = new Size(340, 185)
            };

            int y = 26;
            grpDados.Controls.Add(MakeLabel("Nome *", 10, y));
            txtNome = MakeTextBox(10, y + 18, 314); grpDados.Controls.Add(txtNome); y += 46;
            grpDados.Controls.Add(MakeLabel("E-mail", 10, y));
            txtEmail = MakeTextBox(10, y + 18, 314); grpDados.Controls.Add(txtEmail); y += 46;
            grpDados.Controls.Add(MakeLabel("Telefone", 10, y));
            txtTelefone = MakeTextBox(10, y + 18, 314); grpDados.Controls.Add(txtTelefone);

            // GroupBox fotos
            var grpFotos = new GroupBox
            {
                Text = "Fotos do Cliente",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 80, 120),
                Location = new Point(374, 48),
                Size = new Size(470, 360)
            };

            btnAdicionarFotos = MakeButton("📂  Adicionar Fotos", Color.FromArgb(37, 99, 235));
            btnAdicionarFotos.Size = new Size(175, 34);
            btnAdicionarFotos.Location = new Point(10, 24);
            btnAdicionarFotos.Click += BtnAdicionarFotos_Click;

            btnRemoverFoto = MakeButton("✕  Remover", Color.FromArgb(200, 50, 50));
            btnRemoverFoto.Size = new Size(120, 34);
            btnRemoverFoto.Location = new Point(193, 24);
            btnRemoverFoto.Click += BtnRemoverFoto_Click;

            lblFotosCount = MakeLabel("0 foto(s)", 322, 33, null, Color.FromArgb(120, 130, 150));

            imageListFotos = new ImageList { ImageSize = new Size(160, 120), ColorDepth = ColorDepth.Depth32Bit };

            lstFotos = new ListView
            {
                Location = new Point(10, 66),
                Size = new Size(445, 280),
                Font = new Font("Segoe UI", 8.5f),
                View = View.LargeIcon,
                MultiSelect = true,
                BorderStyle = BorderStyle.FixedSingle,
                HideSelection = false,
                LargeImageList = imageListFotos
            };
            // Enable drag & drop
            lstFotos.AllowDrop = true;
            lstFotos.DragEnter += (s, e) =>
            {
                if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
            };
            lstFotos.DragDrop += (s, e) =>
            {
                if (e.Data == null || !e.Data.GetDataPresent(DataFormats.FileDrop)) return;
                var files = ((string[])e.Data.GetData(DataFormats.FileDrop)).Where(f => File.Exists(f));
                AddFiles(files);
            };

            grpFotos.Controls.AddRange(new Control[]
                { btnAdicionarFotos, btnRemoverFoto, lblFotosCount, lstFotos });

            // Botão Enviar
            btnEnviar = MakeButton("🚀  Enviar Fotos para o Servidor", Color.FromArgb(22, 163, 74));
            btnEnviar.Size = new Size(310, 46);
            btnEnviar.Location = new Point(20, 248);
            btnEnviar.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            btnEnviar.Click += BtnEnviar_Click;

            var btnCancelar = new Button
            {
                Text = "Cancelar",
                Size = new Size(110, 34),
                Location = new Point(20, 302),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.FromArgb(100, 110, 130),
                Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand
            };
            btnCancelar.FlatAppearance.BorderColor = Color.FromArgb(200, 210, 220);
            btnCancelar.Click += (s, e) => ShowPanel(panelInicio);

            panelDados.Controls.AddRange(new Control[]
                { lblTitulo, grpDados, grpFotos, btnEnviar, btnCancelar });

            panelDados.Resize += (s, e) =>
            {
                int avail = panelDados.Width - 394 - 24;
                if (avail < 200) avail = 200;
                grpFotos.Width = avail;
                lstFotos.Width = avail - 25;
            };
        }

        private void BuildPanelProgresso()
        {
            var lblTit = MakeLabel("Enviando fotos...", 0, 0,
                new Font("Segoe UI", 14f, FontStyle.Bold), Color.FromArgb(37, 99, 235));

            progressBar = new ProgressBar { Size = new Size(480, 28), Style = ProgressBarStyle.Continuous };

            lblProgresso = MakeLabel("Preparando...", 0, 0, null, Color.FromArgb(80, 90, 110));

            panelProgresso.Controls.AddRange(new Control[] { lblTit, progressBar, lblProgresso });
            panelProgresso.Resize += (s, e) =>
            {
                if (panelProgresso.Width < 10) return;
                int cx = panelProgresso.Width / 2;
                int cy = panelProgresso.Height / 2;
                progressBar.Width = Math.Min(480, panelProgresso.Width - 80);
                progressBar.Location = new Point(cx - progressBar.Width / 2, cy - 14);
                lblTit.Location      = new Point(cx - lblTit.Width / 2,  cy - 80);
                lblProgresso.Location = new Point(cx - lblProgresso.Width / 2, cy + 26);
            };
        }

        private void BuildPanelSucesso()
        {
            lblSucessoIcon = MakeLabel("✅", 0, 0,
                new Font("Segoe UI", 48f), Color.FromArgb(22, 163, 74));

            lblSucesso = MakeLabel("Fotos enviadas com sucesso!", 0, 0,
                new Font("Segoe UI", 16f, FontStyle.Bold), Color.FromArgb(22, 163, 74));

            lblSucessoDetalhe = MakeLabel(
                "Todas as fotos foram enviadas para o servidor e registradas no banco de dados.",
                0, 0, new Font("Segoe UI", 9.5f), Color.FromArgb(100, 110, 130));

            btnNovoClienteApos = MakeButton("＋  Novo Cliente", Color.FromArgb(37, 99, 235));
            btnNovoClienteApos.Size = new Size(200, 50);
            btnNovoClienteApos.Click += (s, e) => { LimparFormulario(); ShowPanel(panelInicio); };

            panelSucesso.Controls.AddRange(new Control[]
                { lblSucessoIcon, lblSucesso, lblSucessoDetalhe, btnNovoClienteApos });

            panelSucesso.Resize += (s, e) => ReposicionarSucesso();
        }

        private void ReposicionarSucesso()
        {
            if (panelSucesso.Width < 10) return;
            int cx = panelSucesso.Width / 2;
            int cy = panelSucesso.Height / 2;
            // move icon further up so it doesn't overlap the detail label
            lblSucessoIcon.Location    = new Point(cx - lblSucessoIcon.Width / 2,    cy - 200);
            lblSucesso.Location        = new Point(cx - lblSucesso.Width / 2,        cy - 100);
            lblSucessoDetalhe.Location = new Point(cx - lblSucessoDetalhe.Width / 2, cy - 56);
            btnNovoClienteApos.Location = new Point(cx - btnNovoClienteApos.Width / 2, cy + 20);
        }

        // ══════════════════════════════════════════════════════════════
        // ABA CONEXÃO
        // ══════════════════════════════════════════════════════════════
        // ══════════════════════════════════════════════════════════════
        // ABA CONFIGURAÇÕES
        // ══════════════════════════════════════════════════════════════
        private void BuildAbaConfig()
        {
            tabConfig.BackColor = Color.FromArgb(245, 247, 250);

            var grp = new GroupBox
            {
                Text = "Configurações Gerais",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 80, 120),
                Location = new Point(20, 14),
                Size = new Size(820, 340)
            };

            int y = 26;

            // ── Diretório de clientes ──────────────────────────────────
            grp.Controls.Add(MakeLabel("📁  Diretório Padrão  (pasta onde as pastas dos clientes serão criadas):", 12, y));
            y += 20;
            txtDiretorio = new TextBox
            {
                Location = new Point(12, y), Size = new Size(640, 26),
                Font = new Font("Segoe UI", 9.5f), BorderStyle = BorderStyle.FixedSingle
            };
            btnBrowseDiretorio = MakeButton("Procurar...", Color.FromArgb(100, 116, 139));
            btnBrowseDiretorio.Size = new Size(100, 26);
            btnBrowseDiretorio.Location = new Point(658, y);
            btnBrowseDiretorio.Click += BtnBrowseDiretorio_Click;
            grp.Controls.AddRange(new Control[] { txtDiretorio, btnBrowseDiretorio });
            y += 50;

            // ── Diretório de download FTP ──────────────────────────────
            grp.Controls.Add(MakeLabel(
                "📥  Diretório de Download FTP  (onde as fotos recebidas do servidor FTP serão salvas):", 12, y,
                null, Color.FromArgb(37, 99, 235)));
            y += 20;
            txtDownloadFtp = new TextBox
            {
                Location = new Point(12, y), Size = new Size(640, 26),
                Font = new Font("Segoe UI", 9.5f), BorderStyle = BorderStyle.FixedSingle
            };
            btnBrowseDownloadFtp = MakeButton("Procurar...", Color.FromArgb(37, 99, 235));
            btnBrowseDownloadFtp.Size = new Size(100, 26);
            btnBrowseDownloadFtp.Location = new Point(658, y);
            btnBrowseDownloadFtp.Click += BtnBrowseDownloadFtp_Click;
            grp.Controls.AddRange(new Control[] { txtDownloadFtp, btnBrowseDownloadFtp });
            y += 50;

            // opção: criar subpastas com a data
            chkCriarSubpastaData = new CheckBox
            {
                Text = "Criar subpasta com a data (YYYYMMDD) ao salvar downloads FTP",
                Location = new Point(12, y), AutoSize = true,
                Font = new Font("Segoe UI", 9.5f)
            };
            grp.Controls.Add(chkCriarSubpastaData);
            y += 26;

            // ── Servidor NAS ───────────────────────────────────────────
            grp.Controls.Add(MakeLabel("🌐  Caminho do Servidor NAS (destino das fotos de clientes):", 12, y));
            y += 20;
            txtServidorNAS = new TextBox
            {
                Location = new Point(12, y), Size = new Size(756, 26),
                Font = new Font("Segoe UI", 9.5f), BorderStyle = BorderStyle.FixedSingle
            };
            grp.Controls.Add(txtServidorNAS);
            y += 50;

            chkVerificarConexao = new CheckBox
            {
                Text = "Verificar conexão com o servidor ao iniciar o aplicativo",
                Location = new Point(12, y), AutoSize = true,
                Font = new Font("Segoe UI", 9.5f)
            };
            grp.Controls.Add(chkVerificarConexao);
            y += 36;

            string dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "FotoEnvio", "clientes.db");
            grp.Controls.Add(MakeLabel($"🗄  Banco de dados: {dbPath}", 12, y,
                null, Color.FromArgb(130, 140, 160)));

            btnSalvarConfig = MakeButton("💾  Salvar Configurações", Color.FromArgb(22, 163, 74));
            btnSalvarConfig.Size = new Size(220, 46);
            btnSalvarConfig.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            btnSalvarConfig.Location = new Point(20, 368);
            btnSalvarConfig.Click += BtnSalvarConfig_Click;

            tabConfig.Controls.AddRange(new Control[] { grp, btnSalvarConfig });

            tabConfig.Resize += (s, e) =>
            {
                int w = tabConfig.Width - 40;
                grp.Width = w;
                txtDiretorio.Width    = w - 170; btnBrowseDiretorio.Left   = w - 115;
                txtDownloadFtp.Width  = w - 170; btnBrowseDownloadFtp.Left = w - 115;
                txtServidorNAS.Width  = w - 24;
            };
        }

        // ══════════════════════════════════════════════════════════════
        // EVENTOS
        // ══════════════════════════════════════════════════════════════
        private void OnNovoCliente()
        {
            if (string.IsNullOrWhiteSpace(SettingsManager.Current.DiretorioPadrao) ||
                !Directory.Exists(SettingsManager.Current.DiretorioPadrao))
            {
                MessageBox.Show(
                    "Configure o Diretório Padrão na aba Configurações antes de cadastrar clientes.",
                    "Configuração necessária", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                tabControl.SelectedTab = tabConfig;
                return;
            }
            LimparFormulario();
            ShowPanel(panelDados);
        }

        private void BtnAdicionarFotos_Click(object sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog
            {
                Title = "Selecione as fotos do cliente",
                Filter = "Imagens|*.jpg;*.jpeg;*.png;*.bmp;*.tif;*.tiff;*.webp;*.gif|Todos|*.*",
                Multiselect = true
            };
            if (ofd.ShowDialog() != DialogResult.OK) return;
            AddFiles(ofd.FileNames);
        }

        private void BtnRemoverFoto_Click(object sender, EventArgs e)
        {
            if (lstFotos.SelectedItems.Count == 0) return;
            // collect item indices (positions) and remove from _fotos in descending order to avoid shifting
            var indices = lstFotos.SelectedItems.Cast<ListViewItem>()
                .Select(it => lstFotos.Items.IndexOf(it))
                .Where(i => i >= 0 && i < _fotos.Count)
                .Distinct().OrderByDescending(i => i).ToList();
            foreach (int idx in indices)
            {
                _fotos.RemoveAt(idx);
            }
            // rebuild visuals
            RebuildImageListFromFotos();
            AtualizarContador();
        }

        private void RebuildImageListFromFotos()
        {
            imageListFotos.Images.Clear();
            lstFotos.Items.Clear();
            for (int i = 0; i < _fotos.Count; i++)
            {
                string f = _fotos[i];
                try
                {
                    using var img = Image.FromFile(f);
                    var thumb = new Bitmap(img, imageListFotos.ImageSize);
                    imageListFotos.Images.Add(thumb);
                }
                catch { imageListFotos.Images.Add(SystemIcons.Warning.ToBitmap()); }
                var li = new ListViewItem(Path.GetFileName(_fotos[i])) { ImageIndex = i, Tag = _fotos[i] };
                lstFotos.Items.Add(li);
            }
        }

        private void AddFiles(IEnumerable<string> files)
        {
            bool added = false;
            foreach (string f in files)
            {
                if (!_fotos.Contains(f) && IsImageFile(f))
                {
                    _fotos.Add(f);
                    added = true;
                }
            }
            if (added)
            {
                RebuildImageListFromFotos();
                AtualizarContador();
            }
        }

        private static bool IsImageFile(string path)
        {
            string ext = Path.GetExtension(path).ToLowerInvariant();
            return new[] {".jpg", ".jpeg", ".png", ".bmp", ".tif", ".tiff", ".webp", ".gif"}.Contains(ext);
        }

        private async void BtnEnviar_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNome.Text))
            {
                MessageBox.Show("O campo Nome é obrigatório.", "Atenção",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtNome.Focus(); return;
            }
            if (string.IsNullOrWhiteSpace(txtEmail.Text))
            {
                MessageBox.Show("O campo E-mail é obrigatório.", "Atenção",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtEmail.Focus(); return;
            }
            if (string.IsNullOrWhiteSpace(txtTelefone.Text))
            {
                MessageBox.Show("O campo Telefone é obrigatório.", "Atenção",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtTelefone.Focus(); return;
            }
            if (_fotos.Count == 0)
            {
                MessageBox.Show("Selecione pelo menos uma foto.", "Atenção",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string nome      = txtNome.Text.Trim();
            string email     = txtEmail.Text.Trim();
            string telefone  = txtTelefone.Text.Trim();
            string dirLocal  = SettingsManager.Current.DiretorioPadrao;
            string servidorNAS = SettingsManager.Current.ServidorNAS;
            var fotos = new List<string>(_fotos);

            ShowPanel(panelProgresso);
            progressBar.Maximum = fotos.Count;
            progressBar.Value   = 0;

            try
            {
                await Task.Run(() =>
                {
                    SetStatus("Criando pasta do cliente...", 0);
                    string pastaLocal = FileHelper.CriarPastaCliente(dirLocal, nome, telefone);

                    SetStatus("Registrando no banco de dados...", 0);
                    int clienteId = DatabaseHelper.InserirCliente(nome, email, telefone, pastaLocal);

                    string nomePasta = FileHelper.SanitizarNomePasta(nome) + "_" +
                                       FileHelper.SanitizarNomePasta(
                                           telefone.Replace(" ","").Replace("-","").Replace("(","").Replace(")",""));
                    string prefixoData = DateTime.Now.ToString("yyyyMMdd") + "_";
                    string nomePastaFull = prefixoData + nomePasta;
                    if (nomePastaFull.Length > 80) nomePastaFull = nomePastaFull[..80];

                    string pastaServidor = Path.Combine(servidorNAS, nomePastaFull);
                    SetStatus("Criando pasta no servidor NAS...", 0);
                    Directory.CreateDirectory(pastaServidor);

                    for (int i = 0; i < fotos.Count; i++)
                    {
                        string foto = fotos[i];
                        string nomeArq = Path.GetFileName(foto);
                        string nomeS   = FileHelper.SanitizarNomePasta(
                            Path.GetFileNameWithoutExtension(nomeArq)) + Path.GetExtension(nomeArq);

                        SetStatus($"Enviando {nomeArq} ({i + 1}/{fotos.Count})", i);

                        string destLocal = FileHelper.ObterCaminhoUnico(
                            Path.Combine(pastaLocal, nomeS));
                        File.Copy(foto, destLocal, false);

                        string destServ = FileHelper.ObterCaminhoUnico(
                            Path.Combine(pastaServidor, nomeS));
                        FileHelper.CopiarArquivo(foto, destServ);

                        DatabaseHelper.InserirFoto(clienteId, nomeArq, destLocal, destServ);
                        SetStatusValue(i + 1);
                    }
                });

                lblSucesso.Text = $"✅  {fotos.Count} foto(s) enviada(s) com sucesso!";
                ShowPanel(panelSucesso);
                panelSucesso.Invoke(new Action(ReposicionarSucesso));
            }
            catch (Exception ex)
            {
                ShowPanel(panelDados);
                MessageBox.Show(
                    $"Erro ao enviar fotos:\n\n{ex.Message}\n\nVerifique a conexão com o servidor NAS.",
                    "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnBrowseDiretorio_Click(object sender, EventArgs e)
        {
            using var fbd = new FolderBrowserDialog
            {
                Description = "Selecione o diretório padrão para as pastas dos clientes",
                ShowNewFolderButton = true
            };
            if (!string.IsNullOrEmpty(txtDiretorio.Text)) fbd.SelectedPath = txtDiretorio.Text;
            if (fbd.ShowDialog() == DialogResult.OK) txtDiretorio.Text = fbd.SelectedPath;
        }

        private void BtnBrowseDownloadFtp_Click(object sender, EventArgs e)
        {
            using var fbd = new FolderBrowserDialog
            {
                Description = "Selecione onde as fotos recebidas do FTP serão salvas",
                ShowNewFolderButton = true
            };
            if (!string.IsNullOrEmpty(txtDownloadFtp.Text)) fbd.SelectedPath = txtDownloadFtp.Text;
            if (fbd.ShowDialog() == DialogResult.OK) txtDownloadFtp.Text = fbd.SelectedPath;
        }

        private void BtnSalvarConfig_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtDiretorio.Text))
            {
                MessageBox.Show("Informe o Diretório Padrão.", "Atenção",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            SettingsManager.Current.DiretorioPadrao         = txtDiretorio.Text.Trim();
            SettingsManager.Current.DiretorioDownloadFtp     = txtDownloadFtp.Text.Trim();
            SettingsManager.Current.ServidorNAS              = txtServidorNAS.Text.Trim();
            SettingsManager.Current.VerificarConexaoAoIniciar = chkVerificarConexao.Checked;
            SettingsManager.Save();
            MessageBox.Show("Configurações salvas!", "Salvo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ══════════════════════════════════════════════════════════════
        // AUXILIARES
        // ══════════════════════════════════════════════════════════════
        private void CarregarConfiguracoes()
        {
            txtDiretorio.Text           = SettingsManager.Current.DiretorioPadrao;
            txtDownloadFtp.Text         = SettingsManager.Current.DiretorioDownloadFtp;
            txtServidorNAS.Text         = SettingsManager.Current.ServidorNAS;
            chkVerificarConexao.Checked = SettingsManager.Current.VerificarConexaoAoIniciar;
            if (chkCriarSubpastaData != null) chkCriarSubpastaData.Checked = SettingsManager.Current.CriarSubpastaData;
        }

        private void LimparFormulario()
        {
            txtNome.Clear(); txtEmail.Clear(); txtTelefone.Clear();
            lstFotos.Items.Clear(); _fotos.Clear(); imageListFotos.Images.Clear();
            AtualizarContador();
        }

        private void AtualizarContador() =>
            lblFotosCount.Text = $"{_fotos.Count} foto(s)";

        private void ShowPanel(Panel p)
        {
            if (this.InvokeRequired) { this.Invoke(new Action<Panel>(ShowPanel), p); return; }
            foreach (Panel pan in new[] { panelInicio, panelDados, panelProgresso, panelSucesso })
                pan.Visible = false;
            p.Visible = true;
            p.BringToFront();
        }

        private void SetStatus(string msg, int val)
        {
            if (this.InvokeRequired) { this.Invoke(new Action<string, int>(SetStatus), msg, val); return; }
            lblProgresso.Text = msg;
            if (val >= 0) progressBar.Value = Math.Min(val, progressBar.Maximum);
        }

        private void SetStatusValue(int val)
        {
            if (this.InvokeRequired) { this.Invoke(new Action<int>(SetStatusValue), val); return; }
            progressBar.Value = Math.Min(val, progressBar.Maximum);
        }

        // ── Fábricas de controles ──────────────────────────────────────
        private static Button MakeButton(string text, Color color)
        {
            var b = new Button
            {
                Text      = text,
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

        private static Label MakeLabel(string text, int x, int y,
            Font font = null, Color? color = null)
        {
            return new Label
            {
                Text      = text,
                Location  = new Point(x, y),
                AutoSize  = true,
                Font      = font ?? new Font("Segoe UI", 8.5f),
                ForeColor = color ?? Color.FromArgb(60, 70, 90)
            };
        }

        private static TextBox MakeTextBox(int x, int y, int w)
        {
            return new TextBox
            {
                Location     = new Point(x, y),
                Size         = new Size(w, 26),
                Font         = new Font("Segoe UI", 9.5f),
                BorderStyle  = BorderStyle.FixedSingle
            };
        }
    }
}
