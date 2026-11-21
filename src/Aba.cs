using System;
using System.Drawing;
using System.Windows.Forms;

namespace VisualizadorEscPos
{
    abstract class Aba : Panel
    {
        protected readonly Visualizacao Visao = new Visualizacao();
        int colunas = 48;
        Modelo modelo = Modelo.Epson;
        float zoom = 1f;
        string situacao = "";
        bool novidade;

        public int Colunas { get { return colunas; } set { colunas = value; } }
        public Modelo Modelo { get { return modelo; } set { modelo = value; } }
        public float Zoom { get { return zoom; } set { zoom = value; } }
        public string Situacao { get { return situacao; } }
        public bool Novidade { get { return novidade; } set { novidade = value; } }

        public abstract string Titulo { get; }
        public abstract string Dica { get; }
        public abstract char Glifo { get; }
        public abstract void Carregar();
        public virtual bool Acompanhar { get { return true; } set { } }
        public virtual bool PermiteAcompanhar { get { return false; } }
        public virtual string PastaInicial { get { return null; } }
        public virtual string NomeImagem { get { return "impressao.png"; } }

        public event EventHandler SituacaoMudou;
        public event Action<int> PedirZoom;

        protected Aba()
        {
            Dock = DockStyle.Fill;
            BackColor = Tema.Fundo;
            Visao.PedirZoom += delegate(int passo) { if (PedirZoom != null) PedirZoom(passo); };
        }

        public void LigarArrastar(DragEventHandler aoArrastar, DragEventHandler aoSoltar)
        {
            foreach (Control c in Visao.AlvosDeArrastar)
            {
                c.AllowDrop = true;
                c.DragEnter += aoArrastar;
                c.DragDrop += aoSoltar;
            }
        }

        public bool MostrarComandos
        {
            get { return Visao.MostrarComandos; }
            set { Visao.MostrarComandos = value; }
        }

        protected void DefinirSituacao(string texto)
        {
            situacao = texto;
            if (SituacaoMudou != null)
                SituacaoMudou(this, EventArgs.Empty);
        }

        public void AplicarZoom(float novoZoom)
        {
            zoom = novoZoom;
            Visao.AplicarZoom(novoZoom);
        }

        public bool GravarImagem(string destino)
        {
            using (Bitmap imagem = Visao.GerarImagem())
            {
                if (imagem == null)
                    return false;
                imagem.Save(destino, System.Drawing.Imaging.ImageFormat.Png);
            }
            DefinirSituacao("Imagem salva em " + destino);
            return true;
        }
    }
}
