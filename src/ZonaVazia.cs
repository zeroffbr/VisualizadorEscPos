using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace VisualizadorEscPos
{
    class ZonaVazia : Control
    {
        readonly Font fonteTitulo = new Font("Segoe UI Semibold", 14f);
        readonly Font fonteDica = new Font("Segoe UI", 10f);
        readonly Icon icone;
        bool arrastando;

        public ZonaVazia()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = Tema.Fundo;
            Dock = DockStyle.Fill;
            Cursor = Cursors.Hand;
            try { icone = new Icon(Icon.ExtractAssociatedIcon(Application.ExecutablePath), 64, 64); }
            catch (Exception) { icone = null; }
        }

        public bool Arrastando
        {
            get { return arrastando; }
            set { arrastando = value; Invalidate(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(BackColor);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int w = Math.Min(520, Width - 64);
            int h = Math.Min(300, Height - 64);
            if (w < 100 || h < 100)
                return;
            RectangleF caixa = new RectangleF((Width - w) / 2f, (Height - h) / 2f, w, h);

            using (GraphicsPath caminho = Tema.Arredondado(caixa, 14))
            {
                using (SolidBrush fundo = new SolidBrush(arrastando ? Tema.AcentoClaro : Color.FromArgb(246, 247, 249)))
                    g.FillPath(fundo, caminho);
                using (Pen caneta = new Pen(arrastando ? Tema.Acento : Color.FromArgb(196, 201, 210), 2f))
                {
                    caneta.DashStyle = DashStyle.Dash;
                    g.DrawPath(caneta, caminho);
                }
            }

            float y = caixa.Top + 42;
            if (icone != null)
            {
                g.DrawIcon(icone, new Rectangle((int)(caixa.Left + (w - 64) / 2f), (int)y, 64, 64));
                y += 84;
            }

            Rectangle linha1 = new Rectangle((int)caixa.Left, (int)y, w, 32);
            TextRenderer.DrawText(g, arrastando ? "Solte para abrir" : "Arraste os arquivos de impressão aqui",
                fonteTitulo, linha1, Tema.Texto, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            Rectangle linha2 = new Rectangle((int)caixa.Left, (int)y + 36, w, 44);
            TextRenderer.DrawText(g, "ou clique aqui para escolher (.prn, .txt, .bin)\r\nCada arquivo abre na sua aba e atualiza sozinho a cada impressão",
                fonteDica, linha2, Tema.TextoSuave, TextFormatFlags.HorizontalCenter | TextFormatFlags.Top);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                fonteTitulo.Dispose();
                fonteDica.Dispose();
                if (icone != null) icone.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
