using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace VisualizadorEscPos
{
    class PainelCupom : Control
    {
        const int Margem = 16;
        const int Topo = 20;
        const int EspacoLinha = 6;
        const int AlturaLinhaVazia = 30;
        const int AlturaCorte = 40;
        const int AlturaAviso = 30;

        List<Linha> linhas;
        int colunas = 48;
        float zoom = 1f;
        readonly Font fonteNormal = new Font("Consolas", 20f, FontStyle.Regular, GraphicsUnit.Pixel);
        readonly Font fonteNegrito = new Font("Consolas", 20f, FontStyle.Bold, GraphicsUnit.Pixel);
        readonly Font fonteAviso = new Font("Segoe UI", 15f, FontStyle.Italic, GraphicsUnit.Pixel);
        readonly Font fonteMensagem = new Font("Segoe UI", 11f, FontStyle.Regular, GraphicsUnit.Point);
        float avanco;
        float alturaFonte;

        public PainelCupom()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = Tema.Fundo;
        }

        public void Definir(List<Linha> novasLinhas, int novasColunas)
        {
            linhas = novasLinhas;
            colunas = novasColunas;
            Invalidate();
        }

        public float Zoom
        {
            get { return zoom; }
            set { zoom = value; Invalidate(); }
        }

        int LarguraPapel { get { return colunas * 12 + Margem * 2; } }

        int AlturaPapel
        {
            get
            {
                int h = Margem * 2;
                if (linhas == null)
                    return h;
                foreach (Linha l in linhas)
                    h += AlturaDe(l);
                return h;
            }
        }

        static int AlturaDe(Linha l)
        {
            if (l.Tipo == TipoLinha.Corte) return AlturaCorte;
            if (l.Tipo == TipoLinha.Aviso) return AlturaAviso;
            if (l.Vazia) return AlturaLinhaVazia;
            return l.AlturaPontos + EspacoLinha;
        }

        public Size TamanhoNecessario
        {
            get
            {
                if (linhas == null)
                    return new Size(0, 0);
                return new Size((int)Math.Ceiling((LarguraPapel + 40) * zoom), (int)Math.Ceiling((AlturaPapel + Topo * 2) * zoom));
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(BackColor);

            if (linhas != null)
                Desenhar(g, Width);
        }

        public Bitmap GerarImagem()
        {
            Size tamanho = TamanhoNecessario;
            if (linhas == null || tamanho.Width <= 0 || tamanho.Height <= 0)
                return null;
            Bitmap imagem = new Bitmap(tamanho.Width, tamanho.Height);
            using (Graphics g = Graphics.FromImage(imagem))
            {
                g.Clear(BackColor);
                Desenhar(g, tamanho.Width);
            }
            return imagem;
        }

        void Desenhar(Graphics g, int larguraArea)
        {
            if (avanco <= 0)
            {
                avanco = g.MeasureString("M", fonteNormal, PointF.Empty, StringFormat.GenericTypographic).Width;
                alturaFonte = fonteNormal.GetHeight(g);
            }

            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            g.SmoothingMode = SmoothingMode.None;
            g.ScaleTransform(zoom, zoom);

            float x0 = Math.Max(20f, (larguraArea / zoom - LarguraPapel) / 2f);
            float y = Topo;
            int alturaPapel = AlturaPapel;

            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath papel = ContornoPapel(x0, y, LarguraPapel, alturaPapel))
            {
                for (int i = 6; i >= 1; i--)
                {
                    using (GraphicsPath sombra = ContornoPapel(x0 - i + 1, y + 2, LarguraPapel + 2 * i - 2, alturaPapel + i))
                    using (SolidBrush pincel = new SolidBrush(Color.FromArgb(7, 15, 23, 42)))
                        g.FillPath(pincel, sombra);
                }
                g.FillPath(Brushes.White, papel);
            }
            g.SmoothingMode = SmoothingMode.None;

            y += Margem;
            float esquerda = x0 + Margem;
            int areaUtil = colunas * 12;

            foreach (Linha l in linhas)
            {
                int altura = AlturaDe(l);
                if (l.Tipo == TipoLinha.Corte)
                    DesenharCorte(g, x0, y, l.Aviso);
                else if (l.Tipo == TipoLinha.Aviso)
                    DesenharAviso(g, esquerda, y, areaUtil, l);
                else if (!l.Vazia)
                    DesenharTexto(g, esquerda, y, areaUtil, l);
                y += altura;
            }
        }

        static GraphicsPath ContornoPapel(float x, float y, float largura, float altura)
        {
            const float dente = 8f;
            GraphicsPath caminho = new GraphicsPath();
            caminho.AddLine(x, y, x + largura, y);
            caminho.AddLine(x + largura, y, x + largura, y + altura);
            int quantidade = Math.Max(1, (int)(largura / (dente * 2)));
            float passo = largura / quantidade;
            for (int i = 0; i < quantidade; i++)
            {
                float xa = x + largura - i * passo;
                caminho.AddLine(xa, y + altura, xa - passo / 2, y + altura - dente / 1.4f);
                caminho.AddLine(xa - passo / 2, y + altura - dente / 1.4f, xa - passo, y + altura);
            }
            caminho.CloseFigure();
            return caminho;
        }

        void DesenharTexto(Graphics g, float esquerda, float y, int areaUtil, Linha l)
        {
            int alturaLinha = l.AlturaPontos;
            float x = esquerda;
            if (l.Alinhamento == 1)
                x += Math.Max(0, (areaUtil - l.Pontos) / 2f);
            else if (l.Alinhamento == 2)
                x += Math.Max(0, areaUtil - l.Pontos);

            foreach (Trecho t in l.Trechos)
            {
                Estilo es = t.Estilo;
                float w = es.LarguraPontos;
                float h = es.AlturaPontos;
                float topo = y + alturaLinha - h;
                string texto = t.Texto.ToString();

                if (es.Invertido)
                    g.FillRectangle(Brushes.Black, x, topo, w * texto.Length, h);

                Brush pincel = es.Invertido ? Brushes.White : Brushes.Black;
                Font fonte = es.Negrito ? fonteNegrito : fonteNormal;
                foreach (char c in texto)
                {
                    if (c != ' ')
                    {
                        GraphicsState estado = g.Save();
                        g.TranslateTransform(es.Italico ? x + h * 0.2f : x, topo);
                        if (es.Italico)
                            using (Matrix inclinacao = new Matrix(1, 0, -0.2f, 1, 0, 0))
                                g.MultiplyTransform(inclinacao);
                        g.ScaleTransform(w / avanco, h / alturaFonte);
                        g.DrawString(c.ToString(), fonte, pincel, 0, 0, StringFormat.GenericTypographic);
                        if (es.Negrito)
                            g.DrawString(c.ToString(), fonte, pincel, 0.6f, 0, StringFormat.GenericTypographic);
                        g.Restore(estado);
                    }
                    x += w;
                }

                if (es.Sublinhado)
                {
                    using (Pen caneta = new Pen(es.Invertido ? Color.White : Color.Black, 2f))
                        g.DrawLine(caneta, x - w * texto.Length, topo + h - 1, x, topo + h - 1);
                }
            }
        }

        void DesenharCorte(Graphics g, float x0, float y, string texto)
        {
            float meio = y + AlturaCorte / 2f;
            using (Pen caneta = new Pen(Color.FromArgb(150, 150, 150), 1.5f))
            {
                caneta.DashStyle = DashStyle.Dash;
                g.DrawLine(caneta, x0 - 12, meio, x0 + LarguraPapel + 12, meio);
            }
            SizeF tamanho = g.MeasureString(texto, fonteAviso);
            float xt = x0 + (LarguraPapel - tamanho.Width) / 2f;
            g.FillRectangle(Brushes.White, xt - 6, meio - tamanho.Height / 2f, tamanho.Width + 12, tamanho.Height);
            using (SolidBrush pincel = new SolidBrush(Color.FromArgb(120, 120, 120)))
                g.DrawString(texto, fonteAviso, pincel, xt, meio - tamanho.Height / 2f);
        }

        void DesenharAviso(Graphics g, float esquerda, float y, int areaUtil, Linha l)
        {
            SizeF tamanho = g.MeasureString(l.Aviso, fonteAviso);
            float x = esquerda;
            if (l.Alinhamento == 1)
                x += Math.Max(0, (areaUtil - tamanho.Width) / 2f);
            else if (l.Alinhamento == 2)
                x += Math.Max(0, areaUtil - tamanho.Width);
            using (SolidBrush pincel = new SolidBrush(Color.FromArgb(37, 99, 160)))
                g.DrawString(l.Aviso, fonteAviso, pincel, x, y + (AlturaAviso - tamanho.Height) / 2f);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                fonteNormal.Dispose();
                fonteNegrito.Dispose();
                fonteAviso.Dispose();
                fonteMensagem.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
