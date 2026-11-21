using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace VisualizadorEscPos
{
    static class Tema
    {
        public static readonly Color Fundo = Color.FromArgb(236, 238, 242);
        public static readonly Color Superficie = Color.White;
        public static readonly Color Borda = Color.FromArgb(222, 225, 231);
        public static readonly Color Texto = Color.FromArgb(31, 41, 55);
        public static readonly Color TextoSuave = Color.FromArgb(107, 114, 128);
        public static readonly Color Acento = Color.FromArgb(37, 99, 235);
        public static readonly Color AcentoClaro = Color.FromArgb(225, 234, 253);
        public static readonly Color Realce = Color.FromArgb(241, 243, 246);
        public static readonly Color Verde = Color.FromArgb(22, 128, 72);
        public static readonly Color Vermelho = Color.FromArgb(200, 45, 45);

        public static readonly Font Fonte = new Font("Segoe UI", 9.5f);
        public static readonly Font FonteForte = new Font("Segoe UI Semibold", 9.5f);

        static readonly string fonteIcones = DescobrirFonteIcones();

        static string DescobrirFonteIcones()
        {
            using (InstalledFontCollection instaladas = new InstalledFontCollection())
            {
                string achada = null;
                foreach (FontFamily f in instaladas.Families)
                {
                    if (f.Name == "Segoe Fluent Icons") return f.Name;
                    if (f.Name == "Segoe MDL2 Assets") achada = f.Name;
                }
                return achada;
            }
        }

        public static Bitmap Icone(char glifo, Color cor, int tamanho)
        {
            if (fonteIcones == null)
                return null;
            Bitmap b = new Bitmap(tamanho, tamanho);
            using (Graphics g = Graphics.FromImage(b))
            using (Font f = new Font(fonteIcones, tamanho * 0.72f, GraphicsUnit.Pixel))
            using (SolidBrush pincel = new SolidBrush(cor))
            using (StringFormat formato = new StringFormat())
            {
                formato.Alignment = StringAlignment.Center;
                formato.LineAlignment = StringAlignment.Center;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                g.DrawString(glifo.ToString(), f, pincel, new RectangleF(0, 0, tamanho, tamanho), formato);
            }
            return b;
        }

        public static GraphicsPath Arredondado(RectangleF r, float raio)
        {
            GraphicsPath caminho = new GraphicsPath();
            float d = raio * 2;
            caminho.AddArc(r.Left, r.Top, d, d, 180, 90);
            caminho.AddArc(r.Right - d, r.Top, d, d, 270, 90);
            caminho.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            caminho.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
            caminho.CloseFigure();
            return caminho;
        }
    }

    class RenderizadorModerno : ToolStripProfessionalRenderer
    {
        public RenderizadorModerno()
        {
            RoundedEdges = false;
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            using (SolidBrush fundo = new SolidBrush(Tema.Superficie))
                e.Graphics.FillRectangle(fundo, e.AffectedBounds);
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            if (e.ToolStrip is ToolStripDropDown)
            {
                using (Pen caneta = new Pen(Tema.Borda))
                    e.Graphics.DrawRectangle(caneta, 0, 0, e.AffectedBounds.Width - 1, e.AffectedBounds.Height - 1);
                return;
            }
            int y = e.ToolStrip is StatusStrip ? 0 : e.AffectedBounds.Bottom - 1;
            using (Pen caneta = new Pen(Tema.Borda))
                e.Graphics.DrawLine(caneta, 0, y, e.AffectedBounds.Right, y);
        }

        protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
        {
            ToolStripButton botao = e.Item as ToolStripButton;
            bool marcado = botao != null && botao.Checked;
            if (!e.Item.Selected && !e.Item.Pressed && !marcado)
                return;
            Color cor = marcado ? Tema.AcentoClaro : e.Item.Pressed ? Tema.Borda : Tema.Realce;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath caminho = Tema.Arredondado(new RectangleF(1, 1, e.Item.Width - 2, e.Item.Height - 2), 5))
            using (SolidBrush fundo = new SolidBrush(cor))
                e.Graphics.FillPath(fundo, caminho);
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            ToolStripButton botao = e.Item as ToolStripButton;
            if (!e.Item.Enabled)
                e.TextColor = Color.FromArgb(170, 175, 182);
            else if (botao != null && botao.Checked)
                e.TextColor = Tema.Acento;
            else if (e.Item is ToolStripLabel && !(e.Item is ToolStripStatusLabel))
                e.TextColor = Tema.TextoSuave;
            else
                e.TextColor = Tema.Texto;
            base.OnRenderItemText(e);
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            int x = e.Item.Width / 2;
            using (Pen caneta = new Pen(Tema.Borda))
                e.Graphics.DrawLine(caneta, x, 6, x, e.Item.Height - 6);
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (!e.Item.Selected)
                return;
            using (SolidBrush fundo = new SolidBrush(Tema.Realce))
                e.Graphics.FillRectangle(fundo, 2, 0, e.Item.Width - 4, e.Item.Height);
        }
    }
}
