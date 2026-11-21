using System;
using System.Collections.Generic;
using System.Text;

namespace VisualizadorEscPos
{
    class Estilo
    {
        public bool Negrito;
        public bool Sublinhado;
        public bool Invertido;
        public bool FonteB;
        public bool Italico;
        public int Largura = 1;
        public int Altura = 1;

        public Estilo Copia()
        {
            return (Estilo)MemberwiseClone();
        }

        public bool Igual(Estilo o)
        {
            return o != null && Negrito == o.Negrito && Sublinhado == o.Sublinhado && Invertido == o.Invertido
                && FonteB == o.FonteB && Italico == o.Italico && Largura == o.Largura && Altura == o.Altura;
        }

        public int LarguraPontos { get { return (FonteB ? 9 : 12) * Largura; } }
        public int AlturaPontos { get { return (FonteB ? 17 : 24) * Altura; } }
    }

    class Trecho
    {
        public StringBuilder Texto = new StringBuilder();
        public Estilo Estilo;
    }

    enum TipoLinha { Texto, Corte, Aviso }

    class Linha
    {
        public List<Trecho> Trechos = new List<Trecho>();
        public int Alinhamento;
        public TipoLinha Tipo;
        public string Aviso;
        public int Pontos;

        public bool Vazia { get { return Trechos.Count == 0; } }

        public int AlturaPontos
        {
            get
            {
                int a = 24;
                foreach (Trecho t in Trechos)
                    a = Math.Max(a, t.Estilo.AlturaPontos);
                return a;
            }
        }
    }

    class Comando
    {
        public int Posicao;
        public string Bytes;
        public string Significado;
    }

    enum Modelo { Epson, Bematech, Elgin, Daruma }

    class InterpretadorEscPos
    {
        public static readonly string[] NomesModelos = { "Epson e compatíveis", "Bematech", "Elgin", "Daruma" };

        readonly byte[] d;
        int p;
        readonly Modelo modelo;
        bool utf8;
        readonly int larguraPapel;
        Estilo estilo;
        int alinhamento;
        char[] tabela;
        string nomeTabela;
        Linha atual = new Linha();
        string dadosQr;
        readonly StringBuilder textoPendente = new StringBuilder();
        int inicioTexto;

        public readonly List<Linha> Linhas = new List<Linha>();
        public readonly List<Comando> Comandos = new List<Comando>();

        public InterpretadorEscPos(byte[] dados, int colunas, Modelo modeloImpressora)
        {
            d = dados;
            modelo = modeloImpressora;
            larguraPapel = colunas * 12;
            estilo = new Estilo();
            if (modelo == Modelo.Epson)
                DefinirTabela(16);
            else
                DefinirTabelaPorCodigo(850, "CP850");
        }

        bool Bema { get { return modelo == Modelo.Bematech || modelo == Modelo.Elgin; } }

        public void Interpretar()
        {
            while (p < d.Length)
            {
                int ini = p;
                byte b = d[p++];
                if (b == 0x1B) Esc(ini);
                else if (b == 0x1D) Gs(ini);
                else if (b == 0x1C) Fs(ini);
                else if (b == 0x10) Dle(ini);
                else if (b == 0x0A) { NovaLinha(); Registrar(ini, "LF  avança uma linha"); }
                else if (b == 0x0D) Registrar(ini, "CR  retorno de carro (ignorado)");
                else if (b == 0x09) { Registrar(ini, "HT  tabulação"); Tabular(); }
                else if (b == 0x0C) { NovaLinha(); Registrar(ini, "FF  fim de página"); }
                else if (b == 0x0F) { estilo.FonteB = true; Registrar(ini, "SI  condensado ligado"); }
                else if (b == 0x12) { estilo.FonteB = false; Registrar(ini, "DC2  condensado desligado"); }
                else if (b == 0x14 && modelo == Modelo.Daruma) { estilo.FonteB = false; Registrar(ini, "DC4  fonte A (normal)"); }
                else if (b < 0x20) Registrar(ini, "Controle 0x" + b.ToString("X2") + " (ignorado)");
                else if (utf8 && b >= 0x80) Caractere(ini, LerUtf8(b));
                else Caractere(ini, tabela[b]);
            }
            FecharTexto();
            if (!atual.Vazia)
                Linhas.Add(atual);
        }

        int A()
        {
            return p < d.Length ? d[p++] : 0;
        }

        void Pular(long quantidade)
        {
            p = (int)Math.Min(d.Length, p + Math.Max(0, quantidade));
        }

        void PularAteNulo()
        {
            while (p < d.Length && d[p] != 0) p++;
            if (p < d.Length) p++;
        }

        static string Ligado(int n)
        {
            return (n % 48) != 0 ? "ligado" : "desligado";
        }

        char LerUtf8(byte b)
        {
            int extras = b >= 0xF0 ? 3 : b >= 0xE0 ? 2 : b >= 0xC0 ? 1 : 0;
            int inicio = p - 1;
            int fim = Math.Min(d.Length, p + extras);
            while (p < fim && (d[p] & 0xC0) == 0x80) p++;
            string s = Encoding.UTF8.GetString(d, inicio, p - inicio);
            return s.Length > 0 ? s[0] : '?';
        }

        void Esc(int ini)
        {
            if (p >= d.Length) { Registrar(ini, "ESC incompleto no fim do arquivo"); return; }
            byte c = d[p++];
            if (Bema && EscBematech(ini, c)) return;
            if (modelo == Modelo.Daruma && EscDaruma(ini, c)) return;
            int n;
            switch ((char)c)
            {
                case '@':
                    estilo = new Estilo();
                    alinhamento = 0;
                    Registrar(ini, "ESC @  inicializa a impressora (estilo normal)");
                    break;
                case '!':
                    n = A();
                    estilo.FonteB = (n & 0x01) != 0;
                    estilo.Negrito = (n & 0x08) != 0;
                    estilo.Altura = (n & 0x10) != 0 ? 2 : 1;
                    estilo.Largura = (n & 0x20) != 0 ? 2 : 1;
                    estilo.Sublinhado = (n & 0x80) != 0;
                    Registrar(ini, "ESC ! " + n + "  modo: " + DescreverModo(n));
                    break;
                case 'E':
                    n = A();
                    estilo.Negrito = (n & 1) != 0;
                    Registrar(ini, "ESC E  negrito " + ((n & 1) != 0 ? "ligado" : "desligado"));
                    break;
                case 'G':
                    n = A();
                    estilo.Negrito = (n & 1) != 0;
                    Registrar(ini, "ESC G  batida dupla " + ((n & 1) != 0 ? "ligada" : "desligada"));
                    break;
                case '-':
                    n = A();
                    estilo.Sublinhado = (n % 48) != 0;
                    Registrar(ini, "ESC -  sublinhado " + Ligado(n));
                    break;
                case 'a':
                    n = A() % 48;
                    alinhamento = n > 2 ? 0 : n;
                    if (atual.Vazia) atual.Alinhamento = alinhamento;
                    Registrar(ini, "ESC a " + n + "  alinhamento: " + (alinhamento == 1 ? "centro" : alinhamento == 2 ? "direita" : "esquerda"));
                    break;
                case 't':
                    n = A();
                    DefinirTabela(n);
                    Registrar(ini, "ESC t " + n + "  página de código " + nomeTabela);
                    break;
                case 'M':
                    n = A() % 48;
                    estilo.FonteB = n == 1;
                    Registrar(ini, "ESC M  fonte " + (n == 1 ? "B (pequena)" : "A (normal)"));
                    break;
                case '2':
                    Registrar(ini, "ESC 2  espaçamento de linha padrão");
                    break;
                case '3':
                    n = A();
                    Registrar(ini, "ESC 3 " + n + "  espaçamento de linha de " + n + " pontos");
                    break;
                case 'd':
                    n = A();
                    for (int i = 0; i < n; i++) NovaLinha();
                    Registrar(ini, "ESC d " + n + "  avança " + n + " linha(s)");
                    break;
                case 'J':
                    n = A();
                    NovaLinha();
                    Registrar(ini, "ESC J " + n + "  avança " + n + " pontos");
                    break;
                case 'e':
                    n = A();
                    Registrar(ini, "ESC e " + n + "  recua o papel (ignorado)");
                    break;
                case 'p':
                    A(); A(); A();
                    Aviso("[abre a gaveta]");
                    Registrar(ini, "ESC p  pulso para abrir a gaveta");
                    break;
                case 'i':
                    Corte(true);
                    Registrar(ini, "ESC i  corte parcial");
                    break;
                case 'm':
                    Corte(true);
                    Registrar(ini, "ESC m  corte parcial");
                    break;
                case '{':
                    n = A();
                    Registrar(ini, "ESC {  impressão de cabeça para baixo " + Ligado(n) + " (não simulado)");
                    break;
                case 'V':
                    n = A();
                    Registrar(ini, "ESC V  rotação de 90° " + Ligado(n) + " (não simulado)");
                    break;
                case 'U':
                    n = A();
                    Registrar(ini, "ESC U  impressão unidirecional " + Ligado(n));
                    break;
                case 'R':
                    n = A();
                    Registrar(ini, "ESC R " + n + "  conjunto de caracteres internacional");
                    break;
                case 'r':
                    n = A();
                    Registrar(ini, "ESC r " + n + "  cor de impressão");
                    break;
                case 'c':
                    A(); A();
                    Registrar(ini, "ESC c  configuração de painel/sensor");
                    break;
                case 'B':
                    A(); A();
                    Registrar(ini, "ESC B  bipe");
                    break;
                case '$':
                    A(); A();
                    Registrar(ini, "ESC $  posição absoluta (ignorado)");
                    break;
                case '\\':
                    A(); A();
                    Registrar(ini, "ESC \\  posição relativa (ignorado)");
                    break;
                case 'D':
                    PularAteNulo();
                    Registrar(ini, "ESC D  define tabulações");
                    break;
                case '*':
                    {
                        int m = A();
                        int k = A() + A() * 256;
                        Pular(m == 0 || m == 1 ? k : k * 3);
                        Aviso("[imagem de " + k + " pontos de largura]");
                        Registrar(ini, "ESC *  imagem em bits");
                    }
                    break;
                case 'S':
                    Registrar(ini, "ESC S  modo padrão");
                    break;
                case '4':
                    estilo.Italico = true;
                    Registrar(ini, "ESC 4  itálico ligado (Diebold)");
                    break;
                case '5':
                    estilo.Italico = false;
                    Registrar(ini, "ESC 5  itálico desligado (Diebold)");
                    break;
                case 'L':
                    Registrar(ini, "ESC L  modo página (não simulado)");
                    break;
                default:
                    Registrar(ini, "ESC " + NomeByte(c) + "  comando não reconhecido");
                    break;
            }
        }

        bool EscBematech(int ini, byte c)
        {
            int n;
            switch (c)
            {
                case (byte)'E':
                    estilo.Negrito = true;
                    Registrar(ini, "ESC E  negrito ligado");
                    return true;
                case (byte)'F':
                    estilo.Negrito = false;
                    Registrar(ini, "ESC F  negrito desligado");
                    return true;
                case (byte)'W':
                    n = A();
                    estilo.Largura = (n % 48) != 0 ? 2 : 1;
                    Registrar(ini, "ESC W " + n + "  expandido " + Ligado(n));
                    return true;
                case (byte)'d':
                    n = A();
                    estilo.Altura = (n % 48) != 0 ? 2 : 1;
                    Registrar(ini, "ESC d " + n + "  altura dupla " + Ligado(n));
                    return true;
                case (byte)'4':
                    estilo.Italico = true;
                    Registrar(ini, "ESC 4  itálico ligado");
                    return true;
                case (byte)'5':
                    estilo.Italico = false;
                    Registrar(ini, "ESC 5  itálico desligado");
                    return true;
                case 0x0F:
                    estilo.FonteB = true;
                    Registrar(ini, "ESC SI  condensado ligado");
                    return true;
                case (byte)'H':
                    estilo.FonteB = false;
                    Registrar(ini, "ESC H  condensado desligado");
                    return true;
                case (byte)'w':
                    Corte(false);
                    Registrar(ini, "ESC w  corte total");
                    return true;
                case (byte)'m':
                    Corte(true);
                    Registrar(ini, "ESC m  corte parcial");
                    return true;
                case (byte)'t':
                    n = A();
                    if (n == 2) DefinirTabelaPorCodigo(850, "CP850");
                    else if (n == 3) DefinirTabelaPorCodigo(437, "CP437");
                    else if (n == 4) DefinirTabelaPorCodigo(860, "CP860");
                    else if (n == 8) { utf8 = true; nomeTabela = "UTF-8"; }
                    else nomeTabela = "tabela " + n + " (desconhecida)";
                    if (n != 8) utf8 = false;
                    Registrar(ini, "ESC t " + n + "  página de código " + nomeTabela);
                    return true;
                case (byte)'v':
                    if (modelo == Modelo.Elgin) A();
                    A();
                    Aviso("[abre a gaveta]");
                    Registrar(ini, "ESC v  abre a gaveta");
                    return true;
                case 0x80:
                    A();
                    Aviso("[abre a gaveta 2]");
                    Registrar(ini, "ESC 0x80  abre a gaveta 2");
                    return true;
                case (byte)'(':
                    {
                        int fn = A();
                        Pular(A() + A() * 256);
                        if (fn == 'A')
                            Aviso("[bipe]");
                        Registrar(ini, "ESC ( " + NomeByte((byte)fn) + (fn == 'A' ? "  bipe" : "  função estendida"));
                    }
                    return true;
            }
            return false;
        }

        bool EscDaruma(int ini, byte c)
        {
            int n;
            switch (c)
            {
                case (byte)'E':
                    estilo.Negrito = true;
                    Registrar(ini, "ESC E  negrito ligado");
                    return true;
                case (byte)'F':
                    estilo.Negrito = false;
                    Registrar(ini, "ESC F  negrito desligado");
                    return true;
                case (byte)'W':
                    n = A();
                    estilo.Largura = (n % 48) != 0 ? 2 : 1;
                    Registrar(ini, "ESC W " + n + "  expandido " + Ligado(n));
                    return true;
                case (byte)'w':
                    n = A();
                    estilo.Altura = (n % 48) != 0 ? 2 : 1;
                    Registrar(ini, "ESC w " + n + "  altura dupla " + Ligado(n));
                    return true;
                case (byte)'4':
                    n = A();
                    estilo.Italico = (n % 48) != 0;
                    Registrar(ini, "ESC 4 " + n + "  itálico " + Ligado(n));
                    return true;
                case 0x0F:
                    estilo.FonteB = true;
                    Registrar(ini, "ESC SI  condensado ligado");
                    return true;
                case (byte)'j':
                    n = A() % 48;
                    alinhamento = n > 2 ? 0 : n;
                    if (atual.Vazia) atual.Alinhamento = alinhamento;
                    Registrar(ini, "ESC j " + n + "  alinhamento: " + (alinhamento == 1 ? "centro" : alinhamento == 2 ? "direita" : "esquerda"));
                    return true;
                case (byte)'m':
                    Corte(false);
                    Registrar(ini, "ESC m  corte");
                    return true;
                case (byte)'p':
                    Aviso("[abre a gaveta]");
                    Registrar(ini, "ESC p  abre a gaveta");
                    return true;
                case (byte)'b':
                    A(); A(); A(); A();
                    {
                        int inicio = p;
                        PularAteNulo();
                        Aviso("[código de barras: " + Resumir(Decodificar(inicio, p - inicio - 1), 40) + "]");
                    }
                    Registrar(ini, "ESC b  código de barras");
                    return true;
                case 0x81:
                    {
                        int tamanho = A() + A() * 256;
                        int inicio = p;
                        Pular(tamanho);
                        string dados = tamanho > 2 ? Decodificar(inicio + 2, tamanho - 2) : "";
                        Aviso(dados.Length == 0 ? "[QR code]" : "[QR code: " + Resumir(dados, 60) + "]");
                        Registrar(ini, "ESC 0x81  QR code");
                    }
                    return true;
                case 0xC6:
                    Pular(40);
                    Registrar(ini, "ESC 0xC6  configuração da Daruma");
                    return true;
            }
            return false;
        }

        void Gs(int ini)
        {
            if (p >= d.Length) { Registrar(ini, "GS incompleto no fim do arquivo"); return; }
            byte c = d[p++];
            int n;
            switch ((char)c)
            {
                case '!':
                    n = A();
                    estilo.Largura = ((n >> 4) & 7) + 1;
                    estilo.Altura = (n & 7) + 1;
                    Registrar(ini, "GS ! " + n + "  tamanho: largura " + estilo.Largura + "x, altura " + estilo.Altura + "x");
                    break;
                case 'B':
                    n = A();
                    estilo.Invertido = (n & 1) != 0;
                    Registrar(ini, "GS B  impressão invertida (branco no preto) " + ((n & 1) != 0 ? "ligada" : "desligada"));
                    break;
                case 'V':
                    {
                        int m = A();
                        bool parcial = m == 1 || m == 49 || m == 66;
                        if (m == 65 || m == 66) A();
                        Corte(parcial);
                        Registrar(ini, "GS V  " + (parcial ? "corte parcial" : "corte total"));
                    }
                    break;
                case 'k':
                    {
                        int m = A();
                        if (Bema && m == 'Q')
                        {
                            A(); A(); A(); A();
                            int tamanho = A() + A() * 256;
                            int inicio = p;
                            Pular(tamanho);
                            string dados = Decodificar(inicio, tamanho);
                            Aviso(dados.Length == 0 ? "[QR code]" : "[QR code: " + Resumir(dados, 60) + "]");
                            Registrar(ini, "GS k Q  QR code (Bematech)");
                            break;
                        }
                        if (modelo == Modelo.Elgin && m == 11)
                        {
                            int inicio = p;
                            PularAteNulo();
                            string dados = Decodificar(inicio, p - inicio - 1);
                            int virgula = dados.IndexOf(',');
                            if (virgula >= 0 && virgula < 3) dados = dados.Substring(virgula + 1);
                            Aviso(dados.Length == 0 ? "[QR code]" : "[QR code: " + Resumir(dados, 60) + "]");
                            Registrar(ini, "GS k 11  QR code (Elgin)");
                            break;
                        }
                        if (m <= 6)
                            PularAteNulo();
                        else
                            Pular(A());
                        Aviso("[código de barras]");
                        Registrar(ini, "GS k  código de barras");
                    }
                    break;
                case '(':
                    {
                        int fn = A();
                        int tamanho = A() + A() * 256;
                        int inicio = p;
                        Pular(tamanho);
                        if (fn == 'k' && tamanho >= 2 && inicio + 1 < d.Length && d[inicio] == 49)
                        {
                            int sub = d[inicio + 1];
                            if (sub == 80 && tamanho > 3)
                            {
                                dadosQr = Decodificar(inicio + 3, tamanho - 3);
                                Registrar(ini, "GS ( k  grava os dados do QR code");
                            }
                            else if (sub == 81)
                            {
                                Aviso(string.IsNullOrEmpty(dadosQr) ? "[QR code]" : "[QR code: " + Resumir(dadosQr, 60) + "]");
                                Registrar(ini, "GS ( k  imprime o QR code");
                            }
                            else
                                Registrar(ini, "GS ( k  configuração do QR code");
                        }
                        else
                            Registrar(ini, "GS ( " + NomeByte((byte)fn) + "  função estendida (" + tamanho + " bytes)");
                    }
                    break;
                case 'v':
                    {
                        A();
                        A();
                        int x = A() + A() * 256;
                        int y = A() + A() * 256;
                        Pular((long)x * y);
                        Aviso("[imagem " + (x * 8) + " x " + y + " pontos]");
                        Registrar(ini, "GS v 0  imagem raster");
                    }
                    break;
                case '8':
                    {
                        int fn = A();
                        if (fn == 'L')
                        {
                            long tamanho = A() + ((long)A() << 8) + ((long)A() << 16) + ((long)A() << 24);
                            Pular(tamanho);
                        }
                        Aviso("[imagem]");
                        Registrar(ini, "GS 8  imagem");
                    }
                    break;
                case '*':
                    {
                        int x = A();
                        int y = A();
                        Pular((long)x * y * 8);
                        Registrar(ini, "GS *  define imagem para download");
                    }
                    break;
                case '/':
                    A();
                    Aviso("[imagem]");
                    Registrar(ini, "GS /  imprime imagem gravada");
                    break;
                case 'L':
                    A(); A();
                    Registrar(ini, "GS L  margem esquerda");
                    break;
                case 'o':
                    A(); A(); A(); A();
                    Registrar(ini, "GS o  configuração do QR code (Elgin)");
                    break;
                case (char)0xF9:
                    n = A();
                    if (n == 32) A();
                    else if (n == 39) A();
                    Registrar(ini, "GS 0xF9  configuração da Bematech");
                    break;
                case 'W':
                    A(); A();
                    Registrar(ini, "GS W  largura da área de impressão");
                    break;
                case 'P':
                    A(); A();
                    Registrar(ini, "GS P  unidades de movimento");
                    break;
                case 'H':
                case 'h':
                case 'w':
                case 'f':
                case 'b':
                case 'a':
                case 'r':
                case 'I':
                    n = A();
                    Registrar(ini, "GS " + (char)c + " " + n + "  configuração (código de barras/status)");
                    break;
                default:
                    Registrar(ini, "GS " + NomeByte(c) + "  comando não reconhecido");
                    break;
            }
        }

        void Fs(int ini)
        {
            if (p >= d.Length) { Registrar(ini, "FS incompleto no fim do arquivo"); return; }
            byte c = d[p++];
            switch ((char)c)
            {
                case 'p':
                    A(); A();
                    Aviso("[logotipo gravado na impressora]");
                    Registrar(ini, "FS p  imprime logotipo gravado");
                    break;
                case '.':
                    Registrar(ini, "FS .  desliga o modo de caracteres orientais");
                    break;
                case '&':
                    Registrar(ini, "FS &  liga o modo de caracteres orientais");
                    break;
                case 'C':
                    A();
                    Registrar(ini, "FS C  codificação de caracteres orientais");
                    break;
                default:
                    Registrar(ini, "FS " + NomeByte(c) + "  comando não reconhecido");
                    break;
            }
        }

        void Dle(int ini)
        {
            if (p >= d.Length) { Registrar(ini, "DLE incompleto no fim do arquivo"); return; }
            byte c = d[p++];
            if (c == 0x04 || c == 0x05)
            {
                A();
                Registrar(ini, "DLE " + (c == 0x04 ? "EOT" : "ENQ") + "  pedido de status");
            }
            else if (c == 0x14)
            {
                A(); A(); A();
                Registrar(ini, "DLE DC4  comando em tempo real");
            }
            else
                Registrar(ini, "DLE " + NomeByte(c) + "  comando não reconhecido");
        }

        static string DescreverModo(int n)
        {
            List<string> partes = new List<string>();
            if ((n & 0x01) != 0) partes.Add("fonte B");
            if ((n & 0x08) != 0) partes.Add("negrito");
            if ((n & 0x10) != 0) partes.Add("altura dupla");
            if ((n & 0x20) != 0) partes.Add("largura dupla");
            if ((n & 0x80) != 0) partes.Add("sublinhado");
            return partes.Count == 0 ? "normal" : string.Join(", ", partes.ToArray());
        }

        static string NomeByte(byte c)
        {
            if (c >= 0x21 && c < 0x7F)
                return ((char)c).ToString() + " (0x" + c.ToString("X2") + ")";
            return "0x" + c.ToString("X2");
        }

        static string Resumir(string texto, int maximo)
        {
            return texto.Length <= maximo ? texto : texto.Substring(0, maximo) + "…";
        }

        static int CodigoPagina(int n)
        {
            switch (n)
            {
                case 0: return 437;
                case 2: return 850;
                case 3: return 860;
                case 4: return 863;
                case 5: return 865;
                case 16: return 1252;
                case 17: return 866;
                case 19: return 858;
                default: return -1;
            }
        }

        void DefinirTabelaPorCodigo(int cp, string nome)
        {
            Encoding e = Encoding.GetEncoding(cp);
            tabela = new char[256];
            for (int i = 0; i < 256; i++)
            {
                string s = e.GetString(new byte[] { (byte)i });
                tabela[i] = s.Length > 0 ? s[0] : '?';
            }
            nomeTabela = nome;
        }

        void DefinirTabela(int n)
        {
            utf8 = false;
            int cp = CodigoPagina(n);
            Encoding e;
            try
            {
                e = Encoding.GetEncoding(cp < 0 ? 1252 : cp);
            }
            catch (Exception)
            {
                e = Encoding.GetEncoding(1252);
                cp = -1;
            }
            tabela = new char[256];
            for (int i = 0; i < 256; i++)
            {
                string s = e.GetString(new byte[] { (byte)i });
                tabela[i] = s.Length > 0 ? s[0] : '?';
            }
            nomeTabela = cp < 0 ? "tabela " + n + " (desconhecida, exibindo como 1252)" : "CP" + cp;
        }

        string Decodificar(int inicio, int tamanho)
        {
            StringBuilder sb = new StringBuilder();
            int fim = Math.Min(d.Length, inicio + tamanho);
            for (int i = inicio; i < fim; i++)
                sb.Append(d[i] >= 0x20 ? tabela[d[i]] : ' ');
            return sb.ToString();
        }

        void Caractere(int ini, char c)
        {
            if (textoPendente.Length == 0)
                inicioTexto = ini;
            textoPendente.Append(c);
            AdicionarNaLinha(c);
        }

        void AdicionarNaLinha(char c)
        {
            int w = estilo.LarguraPontos;
            if (!atual.Vazia && atual.Pontos + w > larguraPapel)
                NovaLinha();
            if (atual.Vazia)
                atual.Alinhamento = alinhamento;
            Trecho ultimo = atual.Trechos.Count > 0 ? atual.Trechos[atual.Trechos.Count - 1] : null;
            if (ultimo == null || !ultimo.Estilo.Igual(estilo))
            {
                ultimo = new Trecho();
                ultimo.Estilo = estilo.Copia();
                atual.Trechos.Add(ultimo);
            }
            ultimo.Texto.Append(c);
            atual.Pontos += w;
        }

        void Tabular()
        {
            int coluna = atual.Pontos / 12;
            int espacos = 8 - (coluna % 8);
            for (int i = 0; i < espacos; i++)
                AdicionarNaLinha(' ');
        }

        void NovaLinha()
        {
            Linhas.Add(atual);
            atual = new Linha();
            atual.Alinhamento = alinhamento;
        }

        void FecharLinhaAtual()
        {
            if (!atual.Vazia)
                NovaLinha();
        }

        void Corte(bool parcial)
        {
            FecharLinhaAtual();
            Linha l = new Linha();
            l.Tipo = TipoLinha.Corte;
            l.Aviso = parcial ? "corte parcial" : "corte total";
            Linhas.Add(l);
        }

        void Aviso(string texto)
        {
            FecharLinhaAtual();
            Linha l = new Linha();
            l.Tipo = TipoLinha.Aviso;
            l.Aviso = texto;
            l.Alinhamento = alinhamento;
            Linhas.Add(l);
        }

        void FecharTexto()
        {
            if (textoPendente.Length == 0)
                return;
            Comando c = new Comando();
            c.Posicao = inicioTexto;
            c.Bytes = textoPendente.Length + " byte(s)";
            c.Significado = "texto: \"" + textoPendente + "\"";
            Comandos.Add(c);
            textoPendente.Clear();
        }

        void Registrar(int ini, string significado)
        {
            FecharTexto();
            Comando c = new Comando();
            c.Posicao = ini;
            c.Bytes = Hex(ini, p);
            c.Significado = significado;
            Comandos.Add(c);
        }

        string Hex(int inicio, int fim)
        {
            StringBuilder sb = new StringBuilder();
            int limite = Math.Min(fim, inicio + 12);
            for (int i = inicio; i < limite; i++)
            {
                if (sb.Length > 0) sb.Append(' ');
                sb.Append(d[i].ToString("X2"));
            }
            if (fim > limite)
                sb.Append(" … (+" + (fim - limite) + ")");
            return sb.ToString();
        }
    }
}
