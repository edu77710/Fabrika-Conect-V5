using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace FotoEnvio
{
    public static class FileHelper
    {
        private const int MAX_PATH_SEGMENT = 50;

        /// <summary>
        /// Sanitiza string para uso em caminhos de pasta: remove caracteres inválidos e limita tamanho.
        /// </summary>
        public static string SanitizarNomePasta(string nome)
        {
            if (string.IsNullOrWhiteSpace(nome)) return "SemNome";

            // Remove acentos
            string semAcento = RemoverAcentos(nome);

            // Remove caracteres inválidos para nome de pasta
            string invalidos = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            string padrao = "[" + Regex.Escape(invalidos) + "]";
            string sanitizado = Regex.Replace(semAcento, padrao, "_");

            // Substitui espaços múltiplos por underscore
            sanitizado = Regex.Replace(sanitizado, @"\s+", "_");

            // Limita tamanho para evitar MAX_PATH
            if (sanitizado.Length > MAX_PATH_SEGMENT)
                sanitizado = sanitizado.Substring(0, MAX_PATH_SEGMENT).TrimEnd('_', ' ');

            return sanitizado.Trim();
        }

        private static string RemoverAcentos(string texto)
        {
            string normalizado = texto.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (char c in normalizado)
            {
                var categoria = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
                if (categoria != System.Globalization.UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        /// <summary>
        /// Cria caminho de pasta para o cliente no diretório padrão.
        /// Formato: YYYYMMDD_Nome_Telefone
        /// </summary>
        public static string CriarPastaCliente(string diretorioPadrao, string nome, string telefone)
        {
            string data = DateTime.Now.ToString("yyyyMMdd");
            string nomeS = SanitizarNomePasta(nome);
            string telS = SanitizarNomePasta(telefone.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", ""));

            string nomePasta = $"{data}_{nomeS}_{telS}";
            if (nomePasta.Length > 80) nomePasta = nomePasta.Substring(0, 80);

            string caminho = Path.Combine(diretorioPadrao, nomePasta);
            Directory.CreateDirectory(caminho);
            return caminho;
        }

        /// <summary>
        /// Copia arquivo com suporte a caminhos longos (prefixo \\?\UNC\ para UNC).
        /// </summary>
        public static void CopiarArquivo(string origem, string destino)
        {
            string destinoLongo = PrepararCaminhoLongo(destino);
            string dirDestino = Path.GetDirectoryName(destino);
            string dirDestinoLongo = PrepararCaminhoLongo(dirDestino);

            // Garante que o diretório destino existe
            if (!Directory.Exists(dirDestino))
                Directory.CreateDirectory(dirDestino);

            File.Copy(origem, destino, overwrite: true);
        }

        private static string PrepararCaminhoLongo(string caminho)
        {
            if (string.IsNullOrEmpty(caminho)) return caminho;
            if (caminho.StartsWith(@"\\"))
                return @"\\?\UNC\" + caminho.Substring(2);
            if (!caminho.StartsWith(@"\\?\"))
                return @"\\?\" + caminho;
            return caminho;
        }

        /// <summary>
        /// Garante nome de arquivo único no destino (adiciona _1, _2 etc se já existir).
        /// </summary>
        public static string ObterCaminhoUnico(string caminho)
        {
            if (!File.Exists(caminho)) return caminho;

            string dir = Path.GetDirectoryName(caminho);
            string semExt = Path.GetFileNameWithoutExtension(caminho);
            string ext = Path.GetExtension(caminho);

            int contador = 1;
            string novo;
            do
            {
                novo = Path.Combine(dir, $"{semExt}_{contador}{ext}");
                contador++;
            } while (File.Exists(novo));

            return novo;
        }
    }
}
