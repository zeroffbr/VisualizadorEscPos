using System;
using System.IO;
using System.Windows.Forms;

namespace VisualizadorEscPos
{
    class AbaArquivo : Aba
    {
        readonly FileSystemWatcher vigia = new FileSystemWatcher();
        readonly Timer atraso = new Timer();
        int tentativas;
        bool acompanhar = true;

        public readonly string Caminho;

        public AbaArquivo(string caminho)
        {
            Caminho = caminho;
            Controls.Add(Visao);

            vigia.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName;
            vigia.Changed += delegate { AgendarRecarga(); };
            vigia.Created += delegate { AgendarRecarga(); };
            vigia.Renamed += delegate { AgendarRecarga(); };
            try
            {
                vigia.Path = Path.GetDirectoryName(caminho);
                vigia.Filter = Path.GetFileName(caminho);
                vigia.EnableRaisingEvents = true;
            }
            catch (ArgumentException)
            {
                acompanhar = false;
            }

            atraso.Interval = 300;
            atraso.Tick += delegate { atraso.Stop(); Carregar(); };
        }

        public override string Titulo { get { return Path.GetFileName(Caminho); } }
        public override string Dica { get { return Caminho; } }
        public override char Glifo { get { return ''; } }
        public override bool PermiteAcompanhar { get { return true; } }
        public override string PastaInicial { get { return Path.GetDirectoryName(Caminho); } }
        public override string NomeImagem { get { return Path.GetFileNameWithoutExtension(Caminho) + ".png"; } }

        public override bool Acompanhar
        {
            get { return acompanhar; }
            set
            {
                acompanhar = value;
                try { vigia.EnableRaisingEvents = value; }
                catch (ArgumentException) { acompanhar = false; }
            }
        }

        void AgendarRecarga()
        {
            if (IsDisposed || !IsHandleCreated)
                return;
            BeginInvoke((MethodInvoker)delegate
            {
                atraso.Stop();
                atraso.Start();
            });
        }

        public override void Carregar()
        {
            if (!File.Exists(Caminho))
            {
                DefinirSituacao(Caminho + "  —  arquivo não encontrado (aguardando ser criado)");
                return;
            }

            byte[] dados;
            try
            {
                using (FileStream fs = new FileStream(Caminho, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                {
                    dados = new byte[fs.Length];
                    int lidos = 0;
                    while (lidos < dados.Length)
                    {
                        int n = fs.Read(dados, lidos, dados.Length - lidos);
                        if (n <= 0) break;
                        lidos += n;
                    }
                }
                tentativas = 0;
            }
            catch (IOException ex)
            {
                if (tentativas++ < 10)
                {
                    atraso.Stop();
                    atraso.Start();
                }
                DefinirSituacao("Arquivo em uso, tentando de novo... (" + ex.Message + ")");
                return;
            }
            catch (UnauthorizedAccessException ex)
            {
                DefinirSituacao("Sem permissão para ler o arquivo: " + ex.Message);
                return;
            }

            int comandos = Visao.Exibir(dados, Colunas, Modelo, Zoom);
            Novidade = true;
            DefinirSituacao(Caminho + "  —  " + dados.Length + " bytes  —  " + comandos
                + " comandos  —  lido às " + DateTime.Now.ToString("HH:mm:ss"));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                vigia.EnableRaisingEvents = false;
                vigia.Dispose();
                atraso.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
