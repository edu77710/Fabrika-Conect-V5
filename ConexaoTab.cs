using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FotoEnvio
{
    public partial class MainForm
    {
        // ── Seletor de modo ────────────────────────────────────────────
        private RadioButton rbManual, rbAuto;

        // ── PAINEL MANUAL ──────────────────────────────────────────────
        private Panel        panelManual;
        private TextBox      txtIpManual, txtPortaManual, txtUserManual, txtSenhaManual;
        private Panel        pnlSinalManual;
        private Label        lblSinalManual, lblStatusManual, lblContadorManual;
        private Button       btnConectarManual, btnDesconectarManual, btnBuscarManual, btnPararManual;
        private CheckBox     chkLoopManual;
        private DataGridView dgvArqManual;
        private ProgressBar  pbManual;

        // ── PAINEL AUTO ────────────────────────────────────────────────
        private Panel        panelAuto;
        private TextBox      txtPrefixoAuto, txtRangeIni, txtRangeFim;
        private TextBox      txtPortaAuto, txtUserAuto, txtSenhaAuto;
        private Panel        pnlSinalAuto;
        private Label        lblSinalAuto, lblStatusAuto, lblIpAutoConectado, lblContadorAuto;
        private Button       btnIniciarScan, btnPararScan;
        private DataGridView dgvArqAuto;
        private ProgressBar  pbScan;

        // ── Estado ─────────────────────────────────────────────────────
        private FtpHelper               _ftpManual;
        private CancellationTokenSource _ctsManual, _ctsAuto;
        private string _autoUser, _autoSenha;
        private int    _autoPorta;

        // intervalo do loop em ms
        private const int LOOP_INTERVALO_MS = 5000;

        // ==============================================================
        private void BuildAbaConexao()
        {
            tabConexao.BackColor = Color.FromArgb(245, 247, 250);

            var pnlModo = new Panel { Dock = DockStyle.Top, Height = 50,
                BackColor = Color.FromArgb(30, 41, 59) };

            var lblM = new Label { Text = "Modo FTP:", ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f), AutoSize = true, Location = new Point(14, 15) };

            rbManual = new RadioButton { Text = "  Manual", ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), AutoSize = true,
                Location = new Point(105, 14), Checked = true, Cursor = Cursors.Hand };

            rbAuto = new RadioButton { Text = "  Automático (Scan de Rede)", ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), AutoSize = true,
                Location = new Point(215, 14), Cursor = Cursors.Hand };

            rbManual.CheckedChanged += (s, e) => { if (rbManual.Checked) AlternarModo(true); };
            rbAuto.CheckedChanged   += (s, e) => { if (rbAuto.Checked)   AlternarModo(false); };

            pnlModo.Controls.AddRange(new Control[] { lblM, rbManual, rbAuto });
            tabConexao.Controls.Add(pnlModo);

            panelManual = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(245, 247, 250) };
            panelAuto   = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(245, 247, 250), Visible = false };

            tabConexao.Controls.Add(panelAuto);
            tabConexao.Controls.Add(panelManual);

            BuildPanelManual();
            BuildPanelAuto();
        }

        private void AlternarModo(bool manual)
        {
            panelManual.Visible = manual;
            panelAuto.Visible   = !manual;
            if (!manual && string.IsNullOrEmpty(txtPrefixoAuto.Text))
                txtPrefixoAuto.Text = FtpHelper.ObterPrefixoRedeLocal();
        }

        // ==============================================================
        // PAINEL MANUAL
        // ==============================================================
        private void BuildPanelManual()
        {
            var grpCfg = new GroupBox { Text = "Servidor FTP",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 80, 120),
                Location = new Point(10, 8), Size = new Size(860, 115) };

            int x = 10;
            grpCfg.Controls.Add(L("IP:", x, 20));
            txtIpManual = TB(x, 36, 155); grpCfg.Controls.Add(txtIpManual); x += 165;

            grpCfg.Controls.Add(L("Porta:", x, 20));
            txtPortaManual = TB(x, 36, 55);
            txtPortaManual.Text = SettingsManager.Current.FtpPortaManual;
            grpCfg.Controls.Add(txtPortaManual); x += 65;

            grpCfg.Controls.Add(L("Usuário:", x, 20));
            txtUserManual = TB(x, 36, 120);
            txtUserManual.Text = SettingsManager.Current.FtpUsuarioManual;
            grpCfg.Controls.Add(txtUserManual); x += 130;

            grpCfg.Controls.Add(L("Senha:", x, 20));
            txtSenhaManual = TB(x, 36, 120);
            txtSenhaManual.PasswordChar = '●';
            txtSenhaManual.Text = SettingsManager.Current.FtpSenhaManual;
            grpCfg.Controls.Add(txtSenhaManual); x += 135;

            pnlSinalManual = Sinal(x, 33);
            grpCfg.Controls.Add(pnlSinalManual);
            lblSinalManual = L("Desconectado", x + 26, 38, Color.Gray);
            grpCfg.Controls.Add(lblSinalManual);

            btnConectarManual    = Btn("🔌  Conectar",        Color.FromArgb(37, 99, 235),  10,  72, 130, 34);
            btnDesconectarManual = Btn("✕  Desconectar",      Color.FromArgb(160, 50, 50),  150, 72, 140, 34);
            btnBuscarManual      = Btn("🔍  Buscar e Baixar",  Color.FromArgb(100, 60, 180), 300, 72, 190, 34);
            btnPararManual       = Btn("⏹  Parar",            Color.FromArgb(180, 80, 0),   500, 72, 100, 34);

            chkLoopManual = new CheckBox { Text = "Loop contínuo (5s)", AutoSize = true,
                Location = new Point(610, 82), Font = new Font("Segoe UI", 9f), Cursor = Cursors.Hand };

            btnDesconectarManual.Enabled = false;
            btnBuscarManual.Enabled      = false;
            btnPararManual.Enabled       = false;

            btnConectarManual.Click    += BtnConectarManual_Click;
            btnDesconectarManual.Click += (s, e) => DesconectarManual();
            btnBuscarManual.Click      += BtnBuscarManual_Click;
            btnPararManual.Click       += (s, e) => _ctsManual?.Cancel();

            grpCfg.Controls.AddRange(new Control[]
                { btnConectarManual, btnDesconectarManual, btnBuscarManual, btnPararManual, chkLoopManual });

            lblStatusManual = new Label { Text = "Aguardando...", AutoSize = true,
                Font = new Font("Segoe UI", 8.5f), ForeColor = Color.FromArgb(100, 110, 130),
                Location = new Point(10, 128) };

            pbManual = new ProgressBar { Location = new Point(10, 146), Size = new Size(850, 12),
                Visible = false, Style = ProgressBarStyle.Marquee, MarqueeAnimationSpeed = 30 };

            lblContadorManual = new Label { Text = "", AutoSize = true,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(22, 163, 74), Location = new Point(10, 162) };

            var grpArq = new GroupBox { Text = "Arquivos baixados do servidor FTP",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 80, 120),
                Location = new Point(10, 178), Size = new Size(860, 340) };

            dgvArqManual = GridBase();
            ConfigurarColunasArquivos(dgvArqManual);
            dgvArqManual.Dock = DockStyle.Fill;
            grpArq.Controls.Add(dgvArqManual);

            panelManual.Controls.AddRange(new Control[]
                { grpCfg, lblStatusManual, pbManual, lblContadorManual, grpArq });

            panelManual.Resize += (s, e) =>
            {
                int w = panelManual.Width - 20;
                grpCfg.Width  = w;
                grpArq.Width  = w;
                grpArq.Height = panelManual.Height - 195;
                pbManual.Width = w;
            };
        }

        // ── Eventos Manual ─────────────────────────────────────────────
        private async void BtnConectarManual_Click(object sender, EventArgs e)
        {
            string ip = txtIpManual.Text.Trim();
            if (string.IsNullOrWhiteSpace(ip)) { Info("Informe o IP do servidor FTP."); return; }

            int porta = int.TryParse(txtPortaManual.Text, out int p) ? p : 21;
            SetSinal(pnlSinalManual, lblSinalManual, null);
            SetStatus(lblStatusManual, $"Conectando a {ip}:{porta}...");
            btnConectarManual.Enabled = false;

            _ftpManual = new FtpHelper(ip, porta, txtUserManual.Text.Trim(), txtSenhaManual.Text.Trim());
            bool ok = await _ftpManual.TestarConexaoAsync();

            btnConectarManual.Enabled = true;
            if (ok)
            {
                SetSinal(pnlSinalManual, lblSinalManual, true);
                SetStatus(lblStatusManual, $"✅ Conectado em {ip}:{porta}");
                btnDesconectarManual.Enabled = true;
                btnBuscarManual.Enabled      = true;

                // Salva credenciais imediatamente
                SalvarCredsManual();
            }
            else
            {
                SetSinal(pnlSinalManual, lblSinalManual, false);
                SetStatus(lblStatusManual, $"❌ Falha ao conectar em {ip}:{porta}");
                _ftpManual = null;
            }
        }

        private async void BtnBuscarManual_Click(object sender, EventArgs e)
        {
            if (_ftpManual == null) return;

            string destino = ResolverDestinoDownload();
            if (destino == null)
            {
                Info("Configure o Diretório de Download FTP na aba Configurações antes de baixar.");
                tabControl.SelectedTab = tabConfig;
                return;
            }

            btnBuscarManual.Enabled      = false;
            btnPararManual.Enabled       = true;
            btnDesconectarManual.Enabled = false;
            pbManual.Visible             = true;

            _ctsManual?.Cancel();
            _ctsManual = new CancellationTokenSource();
            var ct = _ctsManual.Token;

            int loopManualIteracao = 0;

            try
            {
                do
                {
                    loopManualIteracao++;
                    if (chkLoopManual.Checked)
                        SafeUI(() => lblContadorManual.Text = $"🔁 Loop #{loopManualIteracao}");
                    // Verifica se ainda está conectado
                    bool conectado = await _ftpManual.TestarConexaoAsync(ct);
                    if (!conectado)
                    {
                        SetSinal(pnlSinalManual, lblSinalManual, false);
                        SafeUI(() =>
                        {
                            MessageBox.Show(
                                "O celular está desconectado!",
                                "Aviso de Conexão",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
                        });
                        break;
                    }

                    dgvArqManual.Rows.Clear();
                    SetStatus(lblStatusManual, "🔍 Buscando arquivos no servidor FTP...");
                    if (!chkLoopManual.Checked)
                        SafeUI(() => lblContadorManual.Text = "");

                    var prog = new Progress<string>(msg => SetStatus(lblStatusManual, msg));
                    var arquivos = await _ftpManual.ListarArquivosAsync("/", prog, ct);

                    if (ct.IsCancellationRequested) break;

                    if (arquivos.Count == 0)
                    {
                        SetStatus(lblStatusManual,
                            chkLoopManual.Checked
                                ? $"✅ Sem novos arquivos. Próxima verificação em {LOOP_INTERVALO_MS / 1000}s..."
                                : "✅ Nenhum arquivo encontrado.");
                    }
                    else
                    {
                        SetStatus(lblStatusManual, $"⬇ Baixando {arquivos.Count} arquivo(s)...");
                        int ok = 0, falha = 0;

                        foreach (var arq in arquivos)
                        {
                            if (ct.IsCancellationRequested) break;

                            string nomeLocal = FileHelper.ObterCaminhoUnico(
                                Path.Combine(destino,
                                    FileHelper.SanitizarNomePasta(Path.GetFileNameWithoutExtension(arq.Nome))
                                    + Path.GetExtension(arq.Nome)));

                            SetStatus(lblStatusManual, $"⬇ {arq.Nome}");

                            bool baixou = await _ftpManual.MoverArquivoAsync(arq, nomeLocal, ct);
                            if (baixou) { ok++;    AdicionarLinhaGrid(dgvArqManual, arq, "✅ Baixado"); }
                            else        { falha++;  AdicionarLinhaGrid(dgvArqManual, arq, "❌ Falhou"); }

                            SafeUI(() => lblContadorManual.Text =
                                $"🔁 Loop #{loopManualIteracao}   ✅ {ok} baixado(s)   ❌ {falha} falha(s)   de {arquivos.Count}");
                        }
                    }

                    if (chkLoopManual.Checked && !ct.IsCancellationRequested)
                    {
                        SetStatus(lblStatusManual,
                            $"⏳ Próxima varredura em {LOOP_INTERVALO_MS / 1000}s...");
                        await Task.Delay(LOOP_INTERVALO_MS, ct);
                    }

                } while (chkLoopManual.Checked && !ct.IsCancellationRequested);

                if (!ct.IsCancellationRequested)
                    SetStatus(lblStatusManual, "✅ Varredura concluída.");
                else
                    SetStatus(lblStatusManual, "⏹ Parado pelo usuário.");
            }
            catch (OperationCanceledException)
            {
                SetStatus(lblStatusManual, "⏹ Parado.");
            }
            catch (Exception ex)
            {
                SetStatus(lblStatusManual, $"❌ Erro: {ex.Message}");
                SetSinal(pnlSinalManual, lblSinalManual, false);
            }
            finally
            {
                SafeUI(() =>
                {
                    pbManual.Visible             = false;
                    btnBuscarManual.Enabled      = true;
                    btnPararManual.Enabled       = false;
                    btnDesconectarManual.Enabled = true;
                });
            }
        }

        private void DesconectarManual()
        {
            _ctsManual?.Cancel();
            _ftpManual = null;
            SetSinal(pnlSinalManual, lblSinalManual, false);
            SetStatus(lblStatusManual, "Desconectado.");
            btnDesconectarManual.Enabled = false;
            btnBuscarManual.Enabled      = false;
            btnPararManual.Enabled       = false;
            pbManual.Visible             = false;
        }

        private void SalvarCredsManual()
        {
            SettingsManager.Current.FtpPortaManual   = txtPortaManual.Text.Trim();
            SettingsManager.Current.FtpUsuarioManual = txtUserManual.Text.Trim();
            SettingsManager.Current.FtpSenhaManual   = txtSenhaManual.Text.Trim();
            SettingsManager.Save();
        }

        // ==============================================================
        // PAINEL AUTO
        // ==============================================================
        private void BuildPanelAuto()
        {
            var grpCfg = new GroupBox { Text = "Configuração do Scan",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 80, 120),
                Location = new Point(10, 8), Size = new Size(860, 120) };

            int x = 10;
            grpCfg.Controls.Add(L("Prefixo de Rede:", x, 20));
            txtPrefixoAuto = TB(x, 36, 115);
            txtPrefixoAuto.Text = FtpHelper.ObterPrefixoRedeLocal();  // auto-detecta ao iniciar
            grpCfg.Controls.Add(txtPrefixoAuto); x += 125;

            grpCfg.Controls.Add(L("Início:", x, 20));
            txtRangeIni = TB(x, 36, 48);
            txtRangeIni.Text = SettingsManager.Current.FtpRangeInicio;
            grpCfg.Controls.Add(txtRangeIni); x += 58;

            grpCfg.Controls.Add(L("Fim:", x, 20));
            txtRangeFim = TB(x, 36, 48);
            txtRangeFim.Text = SettingsManager.Current.FtpRangeFim;
            grpCfg.Controls.Add(txtRangeFim); x += 60;

            grpCfg.Controls.Add(L("Porta:", x, 20));
            txtPortaAuto = TB(x, 36, 50);
            txtPortaAuto.Text = SettingsManager.Current.FtpPortaAuto;
            grpCfg.Controls.Add(txtPortaAuto); x += 60;

            grpCfg.Controls.Add(L("Usuário:", x, 20));
            txtUserAuto = TB(x, 36, 110);
            txtUserAuto.Text = SettingsManager.Current.FtpUsuarioAuto;
            grpCfg.Controls.Add(txtUserAuto); x += 120;

            grpCfg.Controls.Add(L("Senha:", x, 20));
            txtSenhaAuto = TB(x, 36, 110);
            txtSenhaAuto.PasswordChar = '●';
            txtSenhaAuto.Text = SettingsManager.Current.FtpSenhaAuto;
            grpCfg.Controls.Add(txtSenhaAuto); x += 120;

            pnlSinalAuto = Sinal(x, 33);
            grpCfg.Controls.Add(pnlSinalAuto);
            lblSinalAuto = L("Desconectado", x + 26, 38, Color.Gray);
            grpCfg.Controls.Add(lblSinalAuto);

            btnIniciarScan = Btn("▶  Iniciar Scan", Color.FromArgb(37, 99, 235),  10, 74, 155, 36);
            btnPararScan   = Btn("⏹  Parar Tudo",   Color.FromArgb(180, 50, 50), 175, 74, 130, 36);
            btnPararScan.Enabled = false;

            btnIniciarScan.Click += BtnIniciarScan_Click;
            btnPararScan.Click   += (s, e) => _ctsAuto?.Cancel();

            grpCfg.Controls.AddRange(new Control[] { btnIniciarScan, btnPararScan });

            lblStatusAuto = new Label { Text = "Configure o range e clique em Iniciar Scan.",
                AutoSize = true, Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(100, 110, 130), Location = new Point(10, 134) };

            pbScan = new ProgressBar { Location = new Point(10, 152), Size = new Size(860, 12),
                Visible = false };

            lblIpAutoConectado = new Label { Text = "", AutoSize = true,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(37, 99, 235), Location = new Point(10, 168) };

            lblContadorAuto = new Label { Text = "", AutoSize = true,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(22, 163, 74), Location = new Point(10, 186) };

            // Painel direito ocupa toda a largura (sem split — sem log de IPs)
            var grpArq = new GroupBox { Text = "Fotos baixadas do servidor conectado",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 80, 120),
                Location = new Point(10, 204), Size = new Size(860, 370) };

            dgvArqAuto = GridBase();
            ConfigurarColunasArquivos(dgvArqAuto);
            dgvArqAuto.Dock = DockStyle.Fill;
            grpArq.Controls.Add(dgvArqAuto);

            panelAuto.Controls.AddRange(new Control[]
                { grpCfg, lblStatusAuto, pbScan, lblIpAutoConectado, lblContadorAuto, grpArq });

            panelAuto.Resize += (s, e) =>
            {
                int w = panelAuto.Width - 20;
                grpCfg.Width  = w;
                pbScan.Width  = w;
                grpArq.Width  = w;
                grpArq.Height = panelAuto.Height - 218;
            };
        }

        // ── Eventos Auto ───────────────────────────────────────────────
        private async void BtnIniciarScan_Click(object sender, EventArgs e)
        {
            string destino = ResolverDestinoDownload();
            if (destino == null)
            {
                Info("Configure o Diretório de Download FTP na aba Configurações antes de iniciar.");
                tabControl.SelectedTab = tabConfig;
                return;
            }

            string prefixo = txtPrefixoAuto.Text.Trim().TrimEnd('.');
            if (!int.TryParse(txtRangeIni.Text,  out int ini)   || ini < 1)   ini = 1;
            if (!int.TryParse(txtRangeFim.Text,   out int fim)   || fim > 254) fim = 254;
            if (!int.TryParse(txtPortaAuto.Text,  out int porta))              porta = 21;

            string user  = txtUserAuto.Text.Trim();
            string senha = txtSenhaAuto.Text.Trim();

            // Salva tudo antes de iniciar
            SettingsManager.Current.FtpPortaAuto   = txtPortaAuto.Text.Trim();
            SettingsManager.Current.FtpUsuarioAuto = user;
            SettingsManager.Current.FtpSenhaAuto   = senha;
            SettingsManager.Current.FtpRangeInicio = ini.ToString();
            SettingsManager.Current.FtpRangeFim    = fim.ToString();
            SettingsManager.Save();

            _autoUser  = user;
            _autoSenha = senha;
            _autoPorta = porta;

            _ctsAuto?.Cancel();
            _ctsAuto = new CancellationTokenSource();
            var ct = _ctsAuto.Token;

            btnIniciarScan.Enabled = false;
            btnPararScan.Enabled   = true;
            pbScan.Visible         = true;
            pbScan.Style           = ProgressBarStyle.Marquee;
            pbScan.MarqueeAnimationSpeed = 25;
            dgvArqAuto.Rows.Clear();

            SetSinal(pnlSinalAuto, lblSinalAuto, null);
            SetStatus(lblStatusAuto, $"Varrendo {prefixo}.{ini} → {prefixo}.{fim}...");
            SafeUI(() => lblIpAutoConectado.Text = "");

            try
            {
                string ipEncontrado = await Task.Run(() => ScanParaEncontrarFtp(prefixo, ini, fim, porta, user, senha, ct), ct);

                if (ct.IsCancellationRequested) return;

                if (string.IsNullOrEmpty(ipEncontrado))
                {
                    SetSinal(pnlSinalAuto, lblSinalAuto, false);
                    SetStatus(lblStatusAuto, "❌ Nenhum servidor FTP encontrado no range.");
                    SafeUI(() => lblIpAutoConectado.Text = "");
                    return;
                }

                // IP encontrado — entra no loop
                SafeUI(() =>
                {
                    SetSinal(pnlSinalAuto, lblSinalAuto, true);
                    lblIpAutoConectado.Text = $"🟢  Servidor conectado: {ipEncontrado}   (loop a cada {LOOP_INTERVALO_MS / 1000}s)";
                });

                var ftp = new FtpHelper(ipEncontrado, porta, user, senha);
                await LoopDownloadAuto(ftp, ipEncontrado, destino, ct);
            }
            catch (OperationCanceledException)
            {
                SetStatus(lblStatusAuto, "⏹ Scan interrompido.");
            }
            finally
            {
                SafeUI(() =>
                {
                    btnIniciarScan.Enabled = true;
                    btnPararScan.Enabled   = false;
                    pbScan.Visible         = false;
                });
            }
        }

        // Varre a rede e retorna o primeiro IP com FTP acessível
        private async Task<string> ScanParaEncontrarFtp(string prefixo, int ini, int fim,
            int porta, string user, string senha, CancellationToken ct)
        {
            var semaforo = new SemaphoreSlim(30);
            string resultado = null;
            var tarefas = new List<Task>();

            for (int i = ini; i <= fim && resultado == null; i++)
            {
                if (ct.IsCancellationRequested) break;
                string ip = $"{prefixo}.{i}";
                await semaforo.WaitAsync(ct);

                tarefas.Add(Task.Run(async () =>
                {
                    try
                    {
                        if (resultado != null || ct.IsCancellationRequested) return;

                        SafeUI(() => SetStatus(lblStatusAuto, $"🔍 Testando {ip}..."));

                        bool aberta = await FtpHelper.PortaAbertaAsync(ip, porta, 800, ct);
                        if (!aberta) return;

                        SafeUI(() => SetStatus(lblStatusAuto, $"🔌 Conectando FTP em {ip}..."));

                        var ftp = new FtpHelper(ip, porta, user, senha);
                        bool conectou = await ftp.TestarConexaoAsync(ct);
                        if (conectou)
                            Interlocked.CompareExchange(ref resultado, ip, null);
                    }
                    catch { }
                    finally { semaforo.Release(); }
                }, ct));
            }

            await Task.WhenAll(tarefas);
            return resultado;
        }

        // Loop de download contínuo (automático)
        private async Task LoopDownloadAuto(FtpHelper ftp, string ip, string destino, CancellationToken ct)
        {
            int falhasConsecutivas = 0;
            int loopAutoIteracao   = 0;

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    loopAutoIteracao++;
                    SafeUI(() => lblContadorAuto.Text = $"🔁 Loop #{loopAutoIteracao}");
                    SetStatus(lblStatusAuto, $"[{ip}] 🔍 Verificando conexão...");

                    bool ainda = await ftp.TestarConexaoAsync(ct);
                    if (!ainda)
                    {
                        falhasConsecutivas++;
                        SafeUI(() =>
                        {
                            SetSinal(pnlSinalAuto, lblSinalAuto, false);
                            lblIpAutoConectado.Text = $"🔴  Desconectado de {ip} — tentando reconectar...";
                        });

                        if (falhasConsecutivas >= 3)
                        {
                            bool voltou = await TentarReconectarAuto(ip, ct);
                            if (!voltou)
                            {
                                SafeUI(() =>
                                {
                                    SetStatus(lblStatusAuto, $"❌ Não foi possível reconectar em {ip}.");
                                    lblIpAutoConectado.Text = $"🔴  Desconectado de {ip}";
                                    MessageBox.Show(
                                        "O celular está desconectado!",
                                        "Aviso de Conexão",
                                        MessageBoxButtons.OK,
                                        MessageBoxIcon.Warning);
                                });
                                return;
                            }
                            falhasConsecutivas = 0;
                            SafeUI(() =>
                            {
                                SetSinal(pnlSinalAuto, lblSinalAuto, true);
                                lblIpAutoConectado.Text =
                                    $"🟢  Reconectado: {ip}   (loop a cada {LOOP_INTERVALO_MS / 1000}s)";
                            });
                        }

                        await Task.Delay(2000, ct);
                        continue;
                    }

                    falhasConsecutivas = 0;

                    var prog     = new Progress<string>(msg => SetStatus(lblStatusAuto, $"[{ip}] {msg}"));
                    var arquivos = await ftp.ListarArquivosAsync("/", prog, ct);

                    if (arquivos.Count == 0)
                    {
                        SetStatus(lblStatusAuto,
                            $"[{ip}] ✅ Sem novos arquivos. Próxima verificação em {LOOP_INTERVALO_MS / 1000}s...");
                        await Task.Delay(LOOP_INTERVALO_MS, ct);
                        continue;
                    }

                    int ok = 0, err = 0;
                    foreach (var arq in arquivos)
                    {
                        if (ct.IsCancellationRequested) break;

                        string nomeLocal = FileHelper.ObterCaminhoUnico(
                            Path.Combine(destino,
                                FileHelper.SanitizarNomePasta(Path.GetFileNameWithoutExtension(arq.Nome))
                                + Path.GetExtension(arq.Nome)));

                        SetStatus(lblStatusAuto, $"[{ip}] ⬇ {arq.Nome}");

                        bool baixou = await ftp.MoverArquivoAsync(arq, nomeLocal, ct);
                        if (baixou) { ok++;  AdicionarLinhaGrid(dgvArqAuto, arq, "✅ Baixado"); }
                        else        { err++; AdicionarLinhaGrid(dgvArqAuto, arq, "❌ Falhou"); }
                    }

                    SetStatus(lblStatusAuto,
                        $"[{ip}] ✅ {ok} baixado(s)  ❌ {err} falha(s) — próxima verificação em {LOOP_INTERVALO_MS / 1000}s...");
                    SafeUI(() => lblContadorAuto.Text = $"🔁 Loop #{loopAutoIteracao}   ✅ {ok} baixado(s)   ❌ {err} falha(s)");
                    await Task.Delay(LOOP_INTERVALO_MS, ct);
                }
                catch (OperationCanceledException) { return; }
                catch { await Task.Delay(3000, ct); }
            }
        }

        private async Task<bool> TentarReconectarAuto(string ip, CancellationToken ct)
        {
            for (int t = 1; t <= 5; t++)
            {
                if (ct.IsCancellationRequested) return false;
                SetStatus(lblStatusAuto, $"[{ip}] 🔄 Tentativa de reconexão {t}/5...");

                bool aberta = await FtpHelper.PortaAbertaAsync(ip, _autoPorta, 1500, ct);
                if (aberta)
                {
                    var ftp = new FtpHelper(ip, _autoPorta, _autoUser, _autoSenha);
                    if (await ftp.TestarConexaoAsync(ct)) return true;
                }
                await Task.Delay(3000, ct);
            }
            return false;
        }

        // ==============================================================
        // HELPERS COMPARTILHADOS
        // ==============================================================

        /// <summary>
        /// Retorna o diretório de destino para downloads FTP.
        /// Se CriarSubpastaData estiver ativo, cria (ou reutiliza) uma
        /// subpasta com a data de hoje (yyyyMMdd) dentro do diretório base.
        /// Retorna null se o diretório base não estiver configurado.
        /// </summary>
        private string ResolverDestinoDownload()
        {
            string base_ = SettingsManager.Current.DiretorioDownloadFtp;
            if (string.IsNullOrWhiteSpace(base_) || !Directory.Exists(base_))
                return null;

            if (!SettingsManager.Current.CriarSubpastaData)
                return base_;

            string subpasta = Path.Combine(base_, DateTime.Now.ToString("yyyyMMdd"));
            if (!Directory.Exists(subpasta))
                Directory.CreateDirectory(subpasta);
            return subpasta;
        }

        private void AdicionarLinhaGrid(DataGridView dgv, FtpArquivo arq, string status)
        {
            SafeUI(() =>
            {
                dgv.Rows.Add(arq.Nome, arq.Tamanho, status);
                try { if (dgv.Rows.Count > 0) dgv.FirstDisplayedScrollingRowIndex = dgv.Rows.Count - 1; }
                catch { }
            });
        }

        private void SetStatus(Label lbl, string msg)
        {
            if (lbl == null) return;
            SafeUI(() => lbl.Text = msg);
        }

        private static void SetSinal(Panel pnl, Label lbl, bool? conectado)
        {
            if (pnl == null) return;
            Color c; string t; Color tc;
            if      (conectado == null)  { c = Color.Goldenrod;            t = "Conectando..."; tc = Color.Goldenrod; }
            else if (conectado == true)  { c = Color.FromArgb(34, 197, 94); t = "Conectado ✔";  tc = Color.FromArgb(22, 163, 74); }
            else                         { c = Color.FromArgb(220, 50, 50); t = "Desconectado"; tc = Color.FromArgb(180, 50, 50); }
            pnl.BackColor = c; pnl.Invalidate();
            if (lbl != null) { lbl.Text = t; lbl.ForeColor = tc; }
        }

        private void SafeUI(Action a)
        {
            if (IsDisposed) return;
            try { if (InvokeRequired) Invoke(a); else a(); }
            catch { }
        }

        private static void Info(string msg) =>
            MessageBox.Show(msg, "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Warning);

        // ── Fábricas ───────────────────────────────────────────────────
        private static Panel Sinal(int x, int y)
        {
            var p = new Panel { Location = new Point(x, y), Size = new Size(20, 20),
                BackColor = Color.FromArgb(180, 180, 180) };
            p.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.FillEllipse(new SolidBrush(p.BackColor), 0, 0, p.Width - 1, p.Height - 1);
            };
            return p;
        }

        private static Button Btn(string txt, Color cor, int x, int y, int w, int h)
        {
            var b = new Button { Text = txt, BackColor = cor, ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand, Location = new Point(x, y), Size = new Size(w, h) };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

        private static Label L(string txt, int x, int y, Color? cor = null) =>
            new Label { Text = txt, Location = new Point(x, y), AutoSize = true,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = cor ?? Color.FromArgb(60, 70, 90) };

        private static TextBox TB(int x, int y, int w) =>
            new TextBox { Location = new Point(x, y), Size = new Size(w, 26),
                Font = new Font("Segoe UI", 9.5f), BorderStyle = BorderStyle.FixedSingle };

        private static DataGridView GridBase() =>
            new DataGridView { ReadOnly = true, AllowUserToAddRows = false,
                AllowUserToDeleteRows = false, BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None, RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                Font = new Font("Segoe UI", 8.5f),
                AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
                    { BackColor = Color.FromArgb(248, 250, 252) } };

        private static void ConfigurarColunasArquivos(DataGridView g)
        {
            g.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Arquivo", AutoSizeMode = DataGridViewAutoSizeColumnMode.None, Width = 220 });
            g.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tamanho", AutoSizeMode = DataGridViewAutoSizeColumnMode.None, Width = 80 });
            g.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Status",  AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        }
    }
}
