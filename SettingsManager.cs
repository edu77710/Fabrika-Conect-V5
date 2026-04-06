using System;
using System.IO;
using System.Xml.Serialization;

namespace FotoEnvio
{
    [Serializable]
    public class AppSettings
    {
        public string DiretorioPadrao        { get; set; } = "";
        public string ServidorNAS            { get; set; } = @"\\192.168.1.2\fabrikahd60tb\TESTE";
        public bool   VerificarConexaoAoIniciar { get; set; } = true;

        // FTP download destination
        public string DiretorioDownloadFtp   { get; set; } = "";

        // FTP manual defaults
        public string FtpPortaManual         { get; set; } = "2221";
        public string FtpUsuarioManual       { get; set; } = "android";
        public string FtpSenhaManual         { get; set; } = "android@";

        // FTP auto defaults
        public string FtpPortaAuto           { get; set; } = "2221";
        public string FtpUsuarioAuto         { get; set; } = "android";
        public string FtpSenhaAuto           { get; set; } = "android@";
        public string FtpRangeInicio         { get; set; } = "1";
        public string FtpRangeFim            { get; set; } = "254";
    }

    public static class SettingsManager
    {
        private static readonly string _settingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FotoEnvio", "settings.xml");

        private static AppSettings _current;

        public static AppSettings Current
        {
            get { if (_current == null) Load(); return _current; }
        }

        public static void Load()
        {
            try
            {
                if (File.Exists(_settingsPath))
                {
                    var xs = new XmlSerializer(typeof(AppSettings));
                    using var fs = new FileStream(_settingsPath, FileMode.Open);
                    _current = (AppSettings)xs.Deserialize(fs);
                }
                else _current = new AppSettings();
            }
            catch { _current = new AppSettings(); }
        }

        public static void Save()
        {
            string dir = Path.GetDirectoryName(_settingsPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            var xs = new XmlSerializer(typeof(AppSettings));
            using var fs = new FileStream(_settingsPath, FileMode.Create);
            xs.Serialize(fs, _current);
        }
    }
}
