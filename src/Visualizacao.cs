using System;
using System.Drawing;
using System.Windows.Forms;

namespace VisualizadorEscPos
{
    class Visualizacao : Panel
    {
        readonly Panel rolagem = new Panel();
        readonly PainelCupom painel = new PainelCupom();
        readonly ListView lista = new ListView();

        public event Action<int> PedirZoom;

        public Visualizacao()
        {
            Dock = DockStyle.Fill;
            BackColor = Tema.Fundo;

            rolagem.Dock = DockStyle.Fill;
            rolagem.AutoScroll = true;
            rolagem.BackColor = Tema.Fundo;
            rolagem.Controls.Add(painel);
            rolagem.Resize += delegate { AjustarPainel(); };
            painel.MouseEnter += delegate { if (!lista.Visible) painel.Focus(); };
            painel.MouseWheel += delegate(object s, MouseEventArgs e)
            {
                if ((Control.ModifierKeys & Keys.Control) == 0 || PedirZoom == null)
                    return;
                HandledMouseEventArgs h = e as HandledMouseEventArgs;
                if (h != null) h.Handled = true;
                PedirZoom(e.Delta > 0 ? 1 : -1);
            };

            lista.Dock = DockStyle.Fill;
            lista.View = View.Details;
            lista.FullRowSelect = true;
            lista.HideSelection = false;
            lista.BorderStyle = BorderStyle.None;
            lista.Font = new Font("Consolas", 10f);
            lista.Columns.Add("Posição", 80);
            lista.Columns.Add("Bytes", 250);
            lista.Columns.Add("Significado", 520);
            lista.Visible = false;

            Controls.Add(rolagem);
            Controls.Add(lista);
        }

        public Control[] AlvosDeArrastar { get { return new Control[] { this, rolagem, painel, lista }; } }

        public bool MostrarComandos
        {
            get { return lista.Visible; }
            set
            {
                lista.Visible = value;
                rolagem.Visible = !value;
            }
        }

        public int Exibir(byte[] dados, int colunas, Modelo modelo, float zoom)
        {
            if (dados == null)
            {
                Limpar();
                return 0;
            }

            InterpretadorEscPos interpretador = new InterpretadorEscPos(dados, colunas, modelo);
            interpretador.Interpretar();

            painel.Zoom = zoom;
            painel.Definir(interpretador.Linhas, colunas);
            AjustarPainel();
            rolagem.AutoScrollPosition = new Point(0, 0);

            lista.BeginUpdate();
            lista.Items.Clear();
            foreach (Comando c in interpretador.Comandos)
            {
                ListViewItem item = new ListViewItem(c.Posicao.ToString("X4"));
                item.SubItems.Add(c.Bytes);
                item.SubItems.Add(c.Significado);
                if (c.Significado.StartsWith("texto:"))
                    item.ForeColor = Tema.Verde;
                else if (c.Significado.Contains("não reconhecido"))
                    item.ForeColor = Tema.Vermelho;
                else
                    item.ForeColor = Tema.Texto;
                lista.Items.Add(item);
            }
            lista.EndUpdate();
            return interpretador.Comandos.Count;
        }

        public void Limpar()
        {
            painel.Definir(null, 48);
            AjustarPainel();
            lista.Items.Clear();
        }

        public void AplicarZoom(float zoom)
        {
            painel.Zoom = zoom;
            AjustarPainel();
        }

        void AjustarPainel()
        {
            Size necessario = painel.TamanhoNecessario;
            Size area = rolagem.ClientSize;
            Point posicao = rolagem.AutoScrollPosition;
            painel.Bounds = new Rectangle(posicao.X, posicao.Y, Math.Max(area.Width, necessario.Width), Math.Max(area.Height, necessario.Height));
            painel.Invalidate();
        }

        public Bitmap GerarImagem()
        {
            return painel.GerarImagem();
        }
    }
}
