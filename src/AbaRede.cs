using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Windows.Forms;

namespace VisualizadorEscPos
{
    class AbaRede : Aba
    {
        const int MaximoTrabalhos = 300;

        readonly ServidorRede servidor = new ServidorRede();
        readonly List<Trabalho> trabalhos = new List<Trabalho>();
        readonly Panel cabecalho = new Panel();
        readonly Label rotuloEstado = new Label();
        readonly Label rotuloPorta = new Label();
        readonly NumericUpDown campoPorta = new NumericUpDown();
        readonly CheckBox checkRede = new CheckBox();
        readonly Button botaoLigar = new Button();
        readonly Label rotuloDica = new Label();
        readonly Button botaoCopiar = new Button();
        readonly ListView lista = new ListView();
        readonly Splitter divisor = new Splitter();
        readonly ToolTip dicas = new ToolTip();
        string erro;

        public AbaRede()
        {
            cabecalho.Dock = DockStyle.Top;
            cabecalho.Height = 52;
            cabecalho.BackColor = Tema.Superficie;
            cabecalho.Padding = new Padding(14, 0, 14, 0);
            cabecalho.Paint += delegate(object s, PaintEventArgs e)
            {
                using (Pen caneta = new Pen(Tema.Borda))
                    e.Graphics.DrawLine(caneta, 0, cabecalho.Height - 1, cabecalho.Width, cabecalho.Height - 1);
                Color cor = servidor.Ativo ? Tema.Verde : erro != null ? Tema.Vermelho : Tema.TextoSuave;
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (SolidBrush ponto = new SolidBrush(cor))
                    e.Graphics.FillEllipse(ponto, 16, cabecalho.Height / 2 - 5, 10, 10);
            };

            rotuloEstado.AutoSize = false;
            rotuloEstado.SetBounds(32, 8, 250, 36);
            rotuloEstado.TextAlign = ContentAlignment.MiddleLeft;
            rotuloEstado.Font = Tema.FonteForte;
            rotuloEstado.ForeColor = Tema.Texto;

            rotuloPorta.Text = "Porta";
            rotuloPorta.AutoSize = true;
            rotuloPorta.ForeColor = Tema.TextoSuave;
            rotuloPorta.Location = new Point(290, 17);

            campoPorta.Minimum = 1;
            campoPorta.Maximum = 65535;
            campoPorta.Value = Config.Inteiro("porta", 9100);
            campoPorta.Width = 70;
            campoPorta.Location = new Point(330, 14);
            campoPorta.BorderStyle = BorderStyle.FixedSingle;

            checkRede.Text = "Aceitar de outros computadores";
            checkRede.AutoSize = true;
            checkRede.ForeColor = Tema.Texto;
            checkRede.Location = new Point(414, 16);
            checkRede.Checked = Config.Logico("aceitarRede", false);
            dicas.SetToolTip(checkRede, "Desmarcado, só aceita impressões deste computador (127.0.0.1). Marcado, o Windows pode pedir liberação no firewall.");

            botaoLigar.FlatStyle = FlatStyle.Flat;
            botaoLigar.FlatAppearance.BorderSize = 0;
            botaoLigar.Font = Tema.FonteForte;
            botaoLigar.Size = new Size(96, 30);
            botaoLigar.Location = new Point(640, 11);
            botaoLigar.Cursor = Cursors.Hand;
            botaoLigar.Click += delegate { if (servidor.Ativo) Desligar(); else Ligar(); };

            cabecalho.Controls.Add(rotuloEstado);
            cabecalho.Controls.Add(rotuloPorta);
            cabecalho.Controls.Add(campoPorta);
            cabecalho.Controls.Add(checkRede);
            cabecalho.Controls.Add(botaoLigar);

            Panel barraDica = new Panel();
            barraDica.Dock = DockStyle.Top;
            barraDica.Height = 40;
            barraDica.BackColor = Tema.AcentoClaro;
            rotuloDica.AutoSize = true;
            rotuloDica.Font = new Font("Consolas", 11f, FontStyle.Bold);
            rotuloDica.ForeColor = Tema.Acento;
            botaoCopiar.Text = "Copiar";
            botaoCopiar.FlatStyle = FlatStyle.Flat;
            botaoCopiar.FlatAppearance.BorderColor = Tema.Acento;
            botaoCopiar.ForeColor = Tema.Acento;
            botaoCopiar.BackColor = Tema.Superficie;
            botaoCopiar.AutoSize = true;
            botaoCopiar.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            botaoCopiar.Padding = new Padding(8, 2, 8, 2);
            botaoCopiar.Cursor = Cursors.Hand;
            botaoCopiar.Click += delegate
            {
                try { Clipboard.SetText(PortaAcbr); DefinirSituacao("Copiado: " + PortaAcbr); }
                catch (Exception) { }
            };
            barraDica.Layout += delegate
            {
                rotuloDica.Location = new Point(16, (barraDica.Height - rotuloDica.Height) / 2);
                botaoCopiar.Location = new Point(rotuloDica.Right + 12, (barraDica.Height - botaoCopiar.Height) / 2);
            };
            barraDica.Controls.Add(rotuloDica);
            barraDica.Controls.Add(botaoCopiar);

            lista.Dock = DockStyle.Left;
            lista.Width = 270;
            lista.View = View.Details;
            lista.FullRowSelect = true;
            lista.HideSelection = false;
            lista.MultiSelect = false;
            lista.BorderStyle = BorderStyle.None;
            lista.Columns.Add("#", 44);
            lista.Columns.Add("Hora", 74);
            lista.Columns.Add("Bytes", 64, HorizontalAlignment.Right);
            lista.Columns.Add("Origem", 84);
            lista.SelectedIndexChanged += delegate { Carregar(); };

            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Renderer = new RenderizadorModerno();
            menu.Items.Add("Salvar como .prn...", null, delegate { SalvarTrabalho(); });
            menu.Items.Add("Apagar", null, delegate { ApagarSelecionado(); });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Limpar histórico", null, delegate { LimparTudo(); });
            lista.ContextMenuStrip = menu;
            lista.KeyDown += delegate(object s, KeyEventArgs e) { if (e.KeyCode == Keys.Delete) ApagarSelecionado(); };

            divisor.Dock = DockStyle.Left;
            divisor.Width = 4;
            divisor.BackColor = Tema.Borda;

            Controls.Add(Visao);
            Controls.Add(divisor);
            Controls.Add(lista);
            Controls.Add(barraDica);
            Controls.Add(cabecalho);

            servidor.TrabalhoRecebido += delegate(Trabalho t) { NaTela(delegate { Receber(t); }); };
            servidor.Erro += delegate(string m) { NaTela(delegate { DefinirSituacao("Erro na impressora de rede: " + m); }); };

            AtualizarCabecalho();
        }

        public override string Titulo { get { return "Impressora de rede"; } }
        public override string Dica { get { return "Recebe impressões pela rede (TCP) na porta " + campoPorta.Value; } }
        public override char Glifo { get { return ''; } }
        public override string NomeImagem { get { return "impressao-rede.png"; } }

        string PortaAcbr { get { return "TCP:127.0.0.1:" + campoPorta.Value; } }

        public bool Ativo { get { return servidor.Ativo; } }

        void NaTela(MethodInvoker acao)
        {
            if (IsDisposed)
                return;
            try { BeginInvoke(acao); }
            catch (InvalidOperationException) { }
        }

        public void Ligar()
        {
            int porta = (int)campoPorta.Value;
            try
            {
                servidor.Iniciar(porta, checkRede.Checked);
                erro = null;
                Config.Gravar("porta", porta);
                Config.Gravar("aceitarRede", checkRede.Checked);
                Config.Gravar("redeLigada", true);
                DefinirSituacao("Impressora de rede ligada na porta " + porta);
            }
            catch (SocketException ex)
            {
                erro = ex.SocketErrorCode == SocketError.AddressAlreadyInUse
                    ? "A porta " + porta + " já está em uso por outro programa"
                    : ex.Message;
                DefinirSituacao("Não foi possível ligar: " + erro);
            }
            AtualizarCabecalho();
        }

        public void Desligar()
        {
            servidor.Parar();
            Config.Gravar("redeLigada", false);
            DefinirSituacao("Impressora de rede desligada");
            AtualizarCabecalho();
        }

        void AtualizarCabecalho()
        {
            bool ligado = servidor.Ativo;
            rotuloEstado.Text = ligado
                ? "Recebendo em " + (servidor.AceitaRede ? "todas as redes" : "127.0.0.1") + ":" + servidor.Porta
                : erro ?? "Desligada";
            rotuloEstado.ForeColor = !ligado && erro != null ? Tema.Vermelho : Tema.Texto;
            campoPorta.Enabled = !ligado;
            checkRede.Enabled = !ligado;
            botaoLigar.Text = ligado ? "Desligar" : "Ligar";
            botaoLigar.BackColor = ligado ? Tema.Realce : Tema.Acento;
            botaoLigar.ForeColor = ligado ? Tema.Texto : Color.White;
            rotuloDica.Text = PortaAcbr;
            dicas.SetToolTip(rotuloDica, "Porta que deve ser usada na impressora do Lógica");
            if (rotuloDica.Parent != null) rotuloDica.Parent.PerformLayout();
            cabecalho.Invalidate();
        }

        void Receber(Trabalho t)
        {
            trabalhos.Add(t);
            ListViewItem item = new ListViewItem(t.Numero.ToString());
            item.SubItems.Add(t.Hora.ToString("HH:mm:ss"));
            item.SubItems.Add(t.Dados.Length.ToString());
            item.SubItems.Add(t.Origem);
            item.Tag = t;
            lista.Items.Insert(0, item);
            while (trabalhos.Count > MaximoTrabalhos)
            {
                Trabalho velho = trabalhos[0];
                trabalhos.RemoveAt(0);
                foreach (ListViewItem i in lista.Items)
                    if (i.Tag == velho) { lista.Items.Remove(i); break; }
            }
            lista.SelectedItems.Clear();
            item.Selected = true;
            item.Focused = true;
            Novidade = true;
            DefinirSituacao("Impressão #" + t.Numero + " recebida de " + t.Origem + " às " + t.Hora.ToString("HH:mm:ss")
                + "  —  " + t.Dados.Length + " bytes  —  " + trabalhos.Count + " no histórico");
        }

        Trabalho Selecionado
        {
            get { return lista.SelectedItems.Count > 0 ? (Trabalho)lista.SelectedItems[0].Tag : null; }
        }

        public override void Carregar()
        {
            Trabalho t = Selecionado;
            if (t == null)
            {
                Visao.Limpar();
                return;
            }
            int comandos = Visao.Exibir(t.Dados, Colunas, Modelo, Zoom);
            DefinirSituacao("Impressão #" + t.Numero + " de " + t.Origem + " às " + t.Hora.ToString("HH:mm:ss")
                + "  —  " + t.Dados.Length + " bytes  —  " + comandos + " comandos");
        }

        void SalvarTrabalho()
        {
            Trabalho t = Selecionado;
            if (t == null)
                return;
            using (SaveFileDialog dialogo = new SaveFileDialog())
            {
                dialogo.Title = "Salvar a impressão recebida";
                dialogo.Filter = "Impressão ESC/POS (*.prn)|*.prn";
                dialogo.FileName = "impressao-" + t.Numero + "-" + t.Hora.ToString("HHmmss") + ".prn";
                if (dialogo.ShowDialog(this) == DialogResult.OK)
                {
                    File.WriteAllBytes(dialogo.FileName, t.Dados);
                    DefinirSituacao("Impressão salva em " + dialogo.FileName);
                }
            }
        }

        void ApagarSelecionado()
        {
            if (lista.SelectedItems.Count == 0)
                return;
            ListViewItem item = lista.SelectedItems[0];
            int indice = item.Index;
            trabalhos.Remove((Trabalho)item.Tag);
            lista.Items.Remove(item);
            if (lista.Items.Count > 0)
                lista.Items[Math.Min(indice, lista.Items.Count - 1)].Selected = true;
            else
                Carregar();
        }

        void LimparTudo()
        {
            trabalhos.Clear();
            lista.Items.Clear();
            Carregar();
            DefinirSituacao("Histórico limpo");
        }

        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);
            botaoLigar.Left = Math.Max(checkRede.Right + 16, cabecalho.Width - botaoLigar.Width - 14);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                servidor.Parar();
                dicas.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
