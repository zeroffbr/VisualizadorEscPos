using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace VisualizadorEscPos
{
    class FaixaAbas : Control
    {
        const int AlturaFaixa = 40;
        const int LarguraMinima = 120;
        const int LarguraMaxima = 240;
        const int TamanhoX = 18;

        public readonly List<Aba> Abas = new List<Aba>();
        readonly List<Rectangle> retangulos = new List<Rectangle>();
        readonly ToolTip dica = new ToolTip();
        readonly Dictionary<string, Bitmap> icones = new Dictionary<string, Bitmap>();
        int selecionada = -1;
        int sobMouse = -1;
        bool sobreX;
        string dicaAtual;

        public event EventHandler SelecaoMudou;
        public event Action<Aba> PedirFechar;

        public FaixaAbas()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            Height = AlturaFaixa;
            Dock = DockStyle.Top;
            BackColor = Tema.Fundo;
            Font = Tema.Fonte;
        }

        public Aba Selecionada
        {
            get { return selecionada >= 0 && selecionada < Abas.Count ? Abas[selecionada] : null; }
            set
            {
                int indice = Abas.IndexOf(value);
                if (indice == selecionada)
                    return;
                selecionada = indice;
                if (value != null)
                    value.Novidade = false;
                Invalidate();
                if (SelecaoMudou != null)
                    SelecaoMudou(this, EventArgs.Empty);
            }
        }

        public void Adicionar(Aba aba)
        {
            Abas.Add(aba);
            Selecionada = aba;
            Invalidate();
        }

        public void Remover(Aba aba)
        {
            int indice = Abas.IndexOf(aba);
            if (indice < 0)
                return;
            bool eraSelecionada = indice == selecionada;
            Abas.RemoveAt(indice);
            retangulos.Clear();
            sobMouse = -1;
            if (eraSelecionada)
            {
                selecionada = -1;
                Selecionada = Abas.Count == 0 ? null : Abas[Math.Min(indice, Abas.Count - 1)];
                if (Abas.Count == 0 && SelecaoMudou != null)
                    SelecaoMudou(this, EventArgs.Empty);
            }
            else if (indice < selecionada)
                selecionada--;
            Invalidate();
        }

        void Calcular(Graphics g)
        {
            retangulos.Clear();
            int x = 8;
            int disponivel = Width - 16;
            int[] larguras = new int[Abas.Count];
            int total = 0;
            for (int i = 0; i < Abas.Count; i++)
            {
                int texto = TextRenderer.MeasureText(g, Abas[i].Titulo, i == selecionada ? Tema.FonteForte : Font).Width;
                larguras[i] = Math.Max(LarguraMinima, Math.Min(LarguraMaxima, texto + 64));
                total += larguras[i] + 4;
            }
            float escala = total > disponivel && total > 0 ? (float)disponivel / total : 1f;
            for (int i = 0; i < Abas.Count; i++)
            {
                int w = Math.Max(70, (int)(larguras[i] * escala));
                retangulos.Add(new Rectangle(x, 6, w, AlturaFaixa - 6));
                x += w + 4;
            }
        }

        Rectangle RetanguloX(Rectangle aba)
        {
            return new Rectangle(aba.Right - TamanhoX - 8, aba.Top + (aba.Height - TamanhoX) / 2, TamanhoX, TamanhoX);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(BackColor);
            Calcular(g);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            for (int i = 0; i < Abas.Count; i++)
            {
                Rectangle r = retangulos[i];
                bool ativa = i == selecionada;
                bool hover = i == sobMouse;

                if (ativa || hover)
                {
                    using (GraphicsPath caminho = TopoArredondado(r, 8))
                    using (SolidBrush fundo = new SolidBrush(ativa ? Tema.Superficie : Color.FromArgb(226, 229, 235)))
                        g.FillPath(fundo, caminho);
                }
                if (ativa)
                {
                    using (SolidBrush acento = new SolidBrush(Tema.Acento))
                    using (GraphicsPath barra = Tema.Arredondado(new RectangleF(r.Left + 10, r.Top, r.Width - 20, 3), 1.5f))
                        g.FillPath(acento, barra);
                }

                Bitmap icone = IconeDe(Abas[i].Glifo, ativa);
                int xTexto = r.Left + 12;
                if (icone != null)
                {
                    g.DrawImage(icone, r.Left + 12, r.Top + (r.Height - 16) / 2);
                    xTexto += 22;
                }

                Rectangle areaTexto = new Rectangle(xTexto, r.Top, RetanguloX(r).Left - xTexto - 4, r.Height);
                TextRenderer.DrawText(g, Abas[i].Titulo, ativa ? Tema.FonteForte : Font, areaTexto,
                    ativa ? Tema.Texto : Tema.TextoSuave,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

                Rectangle x = RetanguloX(r);
                if (!ativa && Abas[i].Novidade && !hover)
                {
                    using (SolidBrush ponto = new SolidBrush(Tema.Verde))
                        g.FillEllipse(ponto, x.Left + 5, x.Top + 5, 8, 8);
                }
                else if (ativa || hover)
                {
                    bool xAceso = hover && sobreX;
                    if (xAceso)
                        using (SolidBrush fundoX = new SolidBrush(Tema.Borda))
                            g.FillEllipse(fundoX, x);
                    using (Pen caneta = new Pen(xAceso ? Tema.Texto : Tema.TextoSuave, 1.4f))
                    {
                        g.DrawLine(caneta, x.Left + 6, x.Top + 6, x.Right - 6, x.Bottom - 6);
                        g.DrawLine(caneta, x.Right - 6, x.Top + 6, x.Left + 6, x.Bottom - 6);
                    }
                }
            }
        }

        Bitmap IconeDe(char glifo, bool ativa)
        {
            string chave = glifo + (ativa ? "a" : "i");
            Bitmap b;
            if (!icones.TryGetValue(chave, out b))
            {
                b = Tema.Icone(glifo, ativa ? Tema.Acento : Tema.TextoSuave, 16);
                icones[chave] = b;
            }
            return b;
        }

        static GraphicsPath TopoArredondado(Rectangle r, int raio)
        {
            GraphicsPath caminho = new GraphicsPath();
            int d = raio * 2;
            caminho.AddArc(r.Left, r.Top, d, d, 180, 90);
            caminho.AddArc(r.Right - d, r.Top, d, d, 270, 90);
            caminho.AddLine(r.Right, r.Bottom, r.Left, r.Bottom);
            caminho.CloseFigure();
            return caminho;
        }

        int Indice(Point ponto)
        {
            for (int i = 0; i < Math.Min(retangulos.Count, Abas.Count); i++)
                if (retangulos[i].Contains(ponto))
                    return i;
            return -1;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int indice = Indice(e.Location);
            bool x = indice >= 0 && RetanguloX(retangulos[indice]).Contains(e.Location);
            if (indice != sobMouse || x != sobreX)
            {
                sobMouse = indice;
                sobreX = x;
                Invalidate();
            }
            string texto = indice >= 0 ? Abas[indice].Dica : null;
            if (texto != dicaAtual)
            {
                dicaAtual = texto;
                dica.SetToolTip(this, texto);
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            sobMouse = -1;
            sobreX = false;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            int indice = Indice(e.Location);
            if (indice < 0 || e.Button != MouseButtons.Left)
                return;
            if (!RetanguloX(retangulos[indice]).Contains(e.Location))
                Selecionada = Abas[indice];
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            int indice = Indice(e.Location);
            if (indice < 0 || PedirFechar == null)
                return;
            if (e.Button == MouseButtons.Middle || (e.Button == MouseButtons.Left && RetanguloX(retangulos[indice]).Contains(e.Location)))
                PedirFechar(Abas[indice]);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                dica.Dispose();
                foreach (Bitmap b in icones.Values)
                    if (b != null) b.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
