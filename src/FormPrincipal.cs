using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace VisualizadorEscPos
{
    class FormPrincipal : Form
    {
        readonly ToolStrip barra = new ToolStrip();
        readonly ToolStripButton botaoAbrir = new ToolStripButton("Abrir");
        readonly ToolStripButton botaoRecarregar = new ToolStripButton("Recarregar");
        readonly ToolStripButton botaoImagem = new ToolStripButton("Salvar imagem");
        readonly ToolStripButton botaoAcompanhar = new ToolStripButton("Atualizar sozinho");
        readonly ToolStripButton botaoRede = new ToolStripButton("Rede");
        readonly ToolStripButton botaoImpressao = new ToolStripButton("Impressão");
        readonly ToolStripButton botaoComandos = new ToolStripButton("Comandos");
        readonly ToolStripComboBox comboModelo = new ToolStripComboBox();
        readonly ToolStripComboBox comboColunas = new ToolStripComboBox();
        readonly ToolStripDropDownButton botaoZoom = new ToolStripDropDownButton("100%");
        static readonly int[] Zooms = { 50, 75, 100, 125, 150, 200, 300 };
        int zoomAtual = 100;
        readonly FaixaAbas faixa = new FaixaAbas();
        readonly Panel conteudo = new Panel();
        readonly ZonaVazia vazio = new ZonaVazia();
        readonly StatusStrip status = new StatusStrip();
        readonly ToolStripStatusLabel rotuloStatus = new ToolStripStatusLabel();
        bool sincronizando;

        public FormPrincipal(string[] arquivosIniciais)
        {
            Text = "Visualizador ESC/POS";
            Width = 960;
            Height = 960;
            MinimumSize = new Size(640, 400);
            StartPosition = FormStartPosition.CenterScreen;
            KeyPreview = true;
            AllowDrop = true;
            BackColor = Tema.Fundo;
            Font = Tema.Fonte;
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);

            Botao(botaoAbrir, '\uE8E5', "Abrir arquivos de impressão (Ctrl+O)");
            Botao(botaoRecarregar, '\uE72C', "Ler o arquivo de novo (F5)");
            Botao(botaoImagem, '\uE74E', "Salvar a prévia como imagem PNG");
            SoIcone(botaoRecarregar);
            SoIcone(botaoImagem);
            Botao(botaoAcompanhar, '\uE895', "Recarrega quando o arquivo for regravado por uma nova impressão");
            Botao(botaoRede, '\uE968', "Impressora de rede: recebe as impressões por TCP (porta 9100), sem precisar de arquivo");
            Botao(botaoImpressao, '\uE749', "Ver a prévia da impressão");
            Botao(botaoComandos, '\uE8FD', "Ver cada comando com a posição, os bytes e o significado");

            botaoAbrir.Click += delegate { EscolherArquivo(); };
            botaoRede.Click += delegate { AbrirRede(false); };
            botaoRecarregar.Click += delegate { if (Ativa != null) Ativa.Carregar(); };
            botaoImagem.Click += delegate { SalvarImagem(); };
            botaoAcompanhar.CheckOnClick = true;
            botaoAcompanhar.Checked = true;
            botaoAcompanhar.CheckedChanged += delegate
            {
                if (!sincronizando && Ativa != null)
                    Ativa.Acompanhar = botaoAcompanhar.Checked;
            };
            botaoImpressao.Alignment = ToolStripItemAlignment.Right;
            botaoComandos.Alignment = ToolStripItemAlignment.Right;
            botaoImpressao.Checked = true;
            botaoImpressao.Click += delegate { MostrarComandos(false); };
            botaoComandos.Click += delegate { MostrarComandos(true); };

            Combo(comboModelo, InterpretadorEscPos.NomesModelos, 0, 150);
            comboModelo.ToolTipText = "Modelo configurado na impressora do Lógica (Diebold usa Epson e compatíveis)";
            comboModelo.SelectedIndexChanged += delegate
            {
                if (sincronizando || Ativa == null) return;
                Ativa.Modelo = (Modelo)comboModelo.SelectedIndex;
                Ativa.Carregar();
            };

            Combo(comboColunas, new object[] { "48", "42", "40", "32" }, 0, 52);
            comboColunas.ToolTipText = "Largura do papel em caracteres: 48 para 80 mm, 32 para 58 mm";
            comboColunas.SelectedIndexChanged += delegate
            {
                if (sincronizando || Ativa == null) return;
                Ativa.Colunas = ColunasEscolhidas;
                Ativa.Carregar();
            };

            botaoZoom.Alignment = ToolStripItemAlignment.Right;
            botaoZoom.ToolTipText = "Zoom (Ctrl + roda do mouse)";
            botaoZoom.Image = Tema.Icone('\uE71E', Tema.TextoSuave, 14);
            foreach (int z in Zooms)
            {
                int valor = z;
                botaoZoom.DropDownItems.Add(z + "%", null, delegate { DefinirZoom(valor); });
            }

            barra.GripStyle = ToolStripGripStyle.Hidden;
            barra.Padding = new Padding(10, 6, 10, 6);
            barra.Renderer = new RenderizadorModerno();
            barra.Font = Tema.Fonte;
            barra.ImageScalingSize = new Size(16, 16);
            barra.Items.Add(botaoAbrir);
            barra.Items.Add(botaoRecarregar);
            barra.Items.Add(botaoImagem);
            barra.Items.Add(new ToolStripSeparator());
            barra.Items.Add(botaoAcompanhar);
            barra.Items.Add(botaoRede);
            barra.Items.Add(new ToolStripSeparator());
            barra.Items.Add(new ToolStripLabel("Modelo"));
            barra.Items.Add(comboModelo);
            barra.Items.Add(new ToolStripLabel("Colunas"));
            barra.Items.Add(comboColunas);
            barra.Items.Add(botaoComandos);
            barra.Items.Add(botaoImpressao);

            faixa.SelecaoMudou += delegate { Sincronizar(); };
            faixa.PedirFechar += Fechar;

            conteudo.Dock = DockStyle.Fill;
            conteudo.BackColor = Tema.Fundo;
            conteudo.Controls.Add(vazio);
            conteudo.Controls.Add(faixa);
            vazio.BringToFront();
            vazio.Click += delegate { EscolherArquivo(); };

            rotuloStatus.Spring = true;
            rotuloStatus.TextAlign = ContentAlignment.MiddleLeft;
            rotuloStatus.ForeColor = Tema.TextoSuave;
            status.Renderer = new RenderizadorModerno();
            status.SizingGrip = false;
            status.Padding = new Padding(10, 2, 10, 2);
            status.Font = new Font("Segoe UI", 9f);
            status.Items.Add(rotuloStatus);
            status.Items.Add(botaoZoom);

            Controls.Add(conteudo);
            Controls.Add(barra);
            Controls.Add(status);

            foreach (Control c in new Control[] { this, conteudo, vazio, faixa })
            {
                c.AllowDrop = true;
                c.DragEnter += AoArrastar;
                c.DragDrop += AoSoltar;
                c.DragLeave += delegate { vazio.Arrastando = false; };
            }

            Sincronizar();
            if (arquivosIniciais != null && Config.Logico("redeLigada", false))
                AbrirRede(true);
            if (arquivosIniciais != null)
                foreach (string a in arquivosIniciais)
                    Abrir(a);
        }

        static void Botao(ToolStripButton botao, char glifo, string dica)
        {
            botao.Image = Tema.Icone(glifo, Tema.Texto, 16);
            botao.DisplayStyle = botao.Image != null ? ToolStripItemDisplayStyle.ImageAndText : ToolStripItemDisplayStyle.Text;
            botao.TextImageRelation = TextImageRelation.ImageBeforeText;
            botao.Padding = new Padding(6, 3, 6, 3);
            botao.Margin = new Padding(1, 0, 1, 0);
            botao.ToolTipText = dica;
        }

        static void SoIcone(ToolStripButton botao)
        {
            if (botao.Image != null)
                botao.DisplayStyle = ToolStripItemDisplayStyle.Image;
        }

        static void Combo(ToolStripComboBox combo, object[] itens, int indice, int largura)
        {
            combo.DropDownStyle = ComboBoxStyle.DropDownList;
            combo.FlatStyle = FlatStyle.Flat;
            combo.BackColor = Tema.Realce;
            combo.ForeColor = Tema.Texto;
            combo.Items.AddRange(itens);
            combo.SelectedIndex = indice;
            combo.AutoSize = false;
            combo.Width = largura;
            combo.Margin = new Padding(2, 0, 10, 0);
        }

        Aba Ativa { get { return faixa.Selecionada; } }

        int ColunasEscolhidas { get { return int.Parse((string)comboColunas.SelectedItem); } }

        float ZoomEscolhido { get { return zoomAtual / 100f; } }

        void DefinirZoom(int zoom)
        {
            zoomAtual = zoom;
            botaoZoom.Text = zoom + "%";
            if (!sincronizando && Ativa != null)
                Ativa.AplicarZoom(ZoomEscolhido);
        }

        void PassoZoom(int direcao)
        {
            int i = Array.IndexOf(Zooms, zoomAtual);
            if (i < 0) i = 2;
            i = Math.Max(0, Math.Min(Zooms.Length - 1, i + direcao));
            DefinirZoom(Zooms[i]);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Control && e.KeyCode == Keys.O)
            {
                EscolherArquivo();
                e.Handled = true;
            }
            else if (e.Control && (e.KeyCode == Keys.W || e.KeyCode == Keys.F4))
            {
                if (Ativa != null) Fechar(Ativa);
                e.Handled = true;
            }
            else if (e.Control && (e.KeyCode == Keys.Oemplus || e.KeyCode == Keys.Add))
            {
                PassoZoom(1);
                e.Handled = true;
            }
            else if (e.Control && (e.KeyCode == Keys.OemMinus || e.KeyCode == Keys.Subtract))
            {
                PassoZoom(-1);
                e.Handled = true;
            }
            else if (e.Control && (e.KeyCode == Keys.D0 || e.KeyCode == Keys.NumPad0))
            {
                DefinirZoom(100);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.F5)
            {
                if (Ativa != null) Ativa.Carregar();
                e.Handled = true;
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            Keys tecla = keyData & Keys.KeyCode;
            if (tecla == Keys.Tab && (keyData & Keys.Control) != 0)
            {
                if (faixa.Abas.Count > 1)
                {
                    int i = faixa.Abas.IndexOf(Ativa) + ((keyData & Keys.Shift) != 0 ? -1 : 1);
                    faixa.Selecionada = faixa.Abas[(i + faixa.Abas.Count) % faixa.Abas.Count];
                }
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        void AoArrastar(object sender, DragEventArgs e)
        {
            bool arquivo = e.Data.GetDataPresent(DataFormats.FileDrop);
            e.Effect = arquivo ? DragDropEffects.Copy : DragDropEffects.None;
            vazio.Arrastando = arquivo;
        }

        void AoSoltar(object sender, DragEventArgs e)
        {
            vazio.Arrastando = false;
            string[] lista = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (lista == null)
                return;
            foreach (string a in lista)
                if (File.Exists(a))
                    Abrir(a);
        }

        void EscolherArquivo()
        {
            using (OpenFileDialog dialogo = new OpenFileDialog())
            {
                dialogo.Title = "Abrir arquivo de impressão";
                dialogo.Filter = "Impressão ESC/POS (*.prn;*.txt;*.bin)|*.prn;*.txt;*.bin|Todos os arquivos (*.*)|*.*";
                dialogo.Multiselect = true;
                if (Ativa != null && Ativa.PastaInicial != null)
                    dialogo.InitialDirectory = Ativa.PastaInicial;
                if (dialogo.ShowDialog(this) == DialogResult.OK)
                    foreach (string a in dialogo.FileNames)
                        Abrir(a);
            }
        }

        void Abrir(string arquivo)
        {
            string caminho = Path.GetFullPath(arquivo);
            foreach (Aba a in faixa.Abas)
            {
                AbaArquivo existente = a as AbaArquivo;
                if (existente != null && string.Equals(existente.Caminho, caminho, StringComparison.OrdinalIgnoreCase))
                {
                    faixa.Selecionada = existente;
                    existente.Carregar();
                    return;
                }
            }

            AbaArquivo aba = new AbaArquivo(caminho);
            Preparar(aba);
            aba.Carregar();
        }

        void AbrirRede(bool ligar)
        {
            foreach (Aba a in faixa.Abas)
            {
                AbaRede existente = a as AbaRede;
                if (existente != null)
                {
                    faixa.Selecionada = existente;
                    if (ligar && !existente.Ativo) existente.Ligar();
                    return;
                }
            }
            AbaRede rede = new AbaRede();
            Preparar(rede);
            if (ligar) rede.Ligar();
        }

        void Preparar(Aba aba)
        {
            aba.LigarArrastar(AoArrastar, AoSoltar);
            aba.Colunas = ColunasEscolhidas;
            aba.Modelo = (Modelo)comboModelo.SelectedIndex;
            aba.Zoom = ZoomEscolhido;
            aba.Acompanhar = botaoAcompanhar.Checked;
            aba.MostrarComandos = botaoComandos.Checked;
            aba.Visible = false;
            aba.PedirZoom += PassoZoom;
            aba.SituacaoMudou += delegate(object s, EventArgs e)
            {
                Aba origem = (Aba)s;
                if (origem == Ativa)
                {
                    origem.Novidade = false;
                    rotuloStatus.Text = origem.Situacao;
                }
                faixa.Invalidate();
            };
            conteudo.Controls.Add(aba);
            aba.BringToFront();
            faixa.Adicionar(aba);
        }

        void Fechar(Aba aba)
        {
            AbaRede rede = aba as AbaRede;
            if (rede != null && rede.Ativo)
                rede.Desligar();
            faixa.Remover(aba);
            conteudo.Controls.Remove(aba);
            aba.Dispose();
            Sincronizar();
        }

        void MostrarComandos(bool comandos)
        {
            botaoComandos.Checked = comandos;
            botaoImpressao.Checked = !comandos;
            foreach (Aba aba in faixa.Abas)
                aba.MostrarComandos = comandos;
        }

        void Sincronizar()
        {
            Aba aba = Ativa;
            bool tem = aba != null;
            foreach (Aba outra in faixa.Abas)
                outra.Visible = outra == aba;
            vazio.Visible = !tem;
            faixa.Visible = faixa.Abas.Count > 0;
            botaoRecarregar.Enabled = tem;
            botaoImagem.Enabled = tem;
            if (!tem)
            {
                Text = "Visualizador ESC/POS";
                rotuloStatus.Text = "Nenhum arquivo aberto";
                return;
            }

            sincronizando = true;
            comboModelo.SelectedIndex = (int)aba.Modelo;
            comboColunas.SelectedItem = aba.Colunas.ToString();
            DefinirZoom((int)Math.Round(aba.Zoom * 100));
            botaoAcompanhar.Checked = aba.Acompanhar;
            botaoAcompanhar.Enabled = aba.PermiteAcompanhar;
            sincronizando = false;

            Text = aba.Titulo + " - Visualizador ESC/POS";
            rotuloStatus.Text = aba.Situacao;
        }

        void SalvarImagem()
        {
            Aba aba = Ativa;
            if (aba == null)
                return;
            using (SaveFileDialog dialogo = new SaveFileDialog())
            {
                dialogo.Title = "Salvar a impressão como imagem";
                dialogo.Filter = "Imagem PNG (*.png)|*.png";
                dialogo.FileName = aba.NomeImagem;
                if (aba.PastaInicial != null)
                    dialogo.InitialDirectory = aba.PastaInicial;
                if (dialogo.ShowDialog(this) == DialogResult.OK)
                    aba.GravarImagem(dialogo.FileName);
            }
        }

        public static int ExportarImagem(string arquivo, string destino, int colunas, Modelo modelo)
        {
            using (FormPrincipal form = new FormPrincipal(null))
            {
                form.comboColunas.SelectedItem = colunas.ToString();
                form.comboModelo.SelectedIndex = (int)modelo;
                form.botaoAcompanhar.Checked = false;
                form.Abrir(arquivo);
                return form.Ativa.GravarImagem(destino) ? 0 : 1;
            }
        }
    }
}
