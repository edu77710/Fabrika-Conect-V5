using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace FotoEnvio
{
    public class FtpArquivo
    {
        public string Nome            { get; set; }
        public string Tamanho         { get; set; }
        public string CaminhoCompleto { get; set; }
        public long   Bytes           { get; set; }
    }

    public class FtpHelper
    {
        public string Host     { get; }
        public int    Port     { get; }
        public string Usuario  { get; }
        public string Senha    { get; }

        private static readonly HashSet<string> _extsImagem = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { ".jpg",".jpeg",".png",".bmp",".tif",".tiff",".webp",".gif",".raw",".cr2",".nef",".arw",".dng" };

        public FtpHelper(string host, int port = 21, string usuario = "anonymous", string senha = "anonymous@")
        {
            Host    = host;
            Port    = port;
            Usuario = usuario;
            Senha   = senha;
        }

        // ── Testar conexão ─────────────────────────────────────────────
        public async Task<bool> TestarConexaoAsync(CancellationToken ct = default)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var req = Req("/", WebRequestMethods.Ftp.ListDirectory);
                    req.Timeout = 4000;
                    using var resp = (FtpWebResponse)req.GetResponse();
                    return true;
                }
                catch { return false; }
            }, ct);
        }

        // ── Listar arquivos de imagem recursivamente ───────────────────
        public async Task<List<FtpArquivo>> ListarArquivosAsync(
            string pasta = "/",
            IProgress<string> prog = null,
            CancellationToken ct = default,
            int maxProf = 5)
        {
            var lista = new List<FtpArquivo>();
            await ListarRec(pasta, lista, prog, ct, 0, maxProf);
            return lista;
        }

        private async Task ListarRec(string pasta, List<FtpArquivo> lista,
            IProgress<string> prog, CancellationToken ct, int prof, int maxProf)
        {
            if (prof > maxProf || ct.IsCancellationRequested) return;

            List<string> entradas;
            try { entradas = await ListDir(pasta, ct); }
            catch { return; }

            foreach (string entrada in entradas)
            {
                if (ct.IsCancellationRequested) break;
                string caminho = pasta.TrimEnd('/') + "/" + entrada;
                string ext     = Path.GetExtension(entrada);

                if (string.IsNullOrEmpty(ext))
                {
                    prog?.Report($"📁 {caminho}");
                    await ListarRec(caminho, lista, prog, ct, prof + 1, maxProf);
                }
                else if (_extsImagem.Contains(ext))
                {
                    long bytes = await GetSize(caminho);
                    lista.Add(new FtpArquivo
                    {
                        Nome            = entrada,
                        CaminhoCompleto = caminho,
                        Bytes           = bytes,
                        Tamanho         = FormatBytes(bytes)
                    });
                    prog?.Report($"🖼 {entrada}  ({FormatBytes(bytes)})");
                }
            }
        }

        private async Task<List<string>> ListDir(string pasta, CancellationToken ct)
        {
            return await Task.Run(() =>
            {
                var lista = new List<string>();
                var req = Req(pasta, WebRequestMethods.Ftp.ListDirectory);
                req.Timeout = 6000;
                using var resp = (FtpWebResponse)req.GetResponse();
                using var sr   = new StreamReader(resp.GetResponseStream());
                string linha;
                while ((linha = sr.ReadLine()) != null)
                {
                    linha = linha.Trim();
                    if (!string.IsNullOrEmpty(linha))
                        lista.Add(Path.GetFileName(linha));
                }
                return lista;
            }, ct);
        }

        // ── Baixar e apagar do servidor (mover) ────────────────────────
        public async Task<bool> MoverArquivoAsync(FtpArquivo arq, string destino, CancellationToken ct)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Download
                    var reqDl = Req(arq.CaminhoCompleto, WebRequestMethods.Ftp.DownloadFile);
                    reqDl.Timeout = 60000;
                    using (var resp = (FtpWebResponse)reqDl.GetResponse())
                    using (var stream = resp.GetResponseStream())
                    using (var fs = new FileStream(destino, FileMode.Create, FileAccess.Write))
                        stream.CopyTo(fs);

                    // Delete no servidor
                    var reqDel = Req(arq.CaminhoCompleto, WebRequestMethods.Ftp.DeleteFile);
                    reqDel.Timeout = 10000;
                    using var respDel = (FtpWebResponse)reqDel.GetResponse();
                    return true;
                }
                catch { return false; }
            }, ct);
        }

        private async Task<long> GetSize(string caminho)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var req = Req(caminho, WebRequestMethods.Ftp.GetFileSize);
                    req.Timeout = 3000;
                    using var resp = (FtpWebResponse)req.GetResponse();
                    return resp.ContentLength;
                }
                catch { return 0L; }
            });
        }

        private FtpWebRequest Req(string caminho, string metodo)
        {
            string url = $"ftp://{Host}:{Port}{caminho}";
            var r = (FtpWebRequest)WebRequest.Create(url);
            r.Method      = metodo;
            r.Credentials = new NetworkCredential(Usuario, Senha);
            r.UsePassive  = true;
            r.UseBinary   = true;
            r.KeepAlive   = false;
            return r;
        }

        private static string FormatBytes(long b)
        {
            if (b <= 0)          return "—";
            if (b < 1024)        return $"{b} B";
            if (b < 1024*1024)   return $"{b/1024.0:F1} KB";
            return $"{b/(1024.0*1024):F1} MB";
        }

        // ── Utilitários de rede ────────────────────────────────────────
        public static string ObterPrefixoRedeLocal()
        {
            try
            {
                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != OperationalStatus.Up) continue;
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                    foreach (var ip in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            string[] p = ip.Address.ToString().Split('.');
                            if (p[0] is "10" or "172" or "192")
                                return $"{p[0]}.{p[1]}.{p[2]}";
                        }
                    }
                }
            }
            catch { }
            return "192.168.1";
        }

        public static async Task<bool> PortaAbertaAsync(string ip, int porta = 21,
            int ms = 800, CancellationToken ct = default)
        {
            try
            {
                using var tcp  = new TcpClient();
                var conn  = tcp.ConnectAsync(ip, porta);
                var delay = Task.Delay(ms, ct);
                var done  = await Task.WhenAny(conn, delay);
                return done == conn && tcp.Connected;
            }
            catch { return false; }
        }
    }
}
