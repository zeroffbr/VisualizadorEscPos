using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace VisualizadorEscPos
{
    class Trabalho
    {
        public int Numero;
        public DateTime Hora;
        public string Origem;
        public byte[] Dados;
    }

    class ServidorRede
    {
        const int SilencioFimTrabalho = 700;

        TcpListener ouvinte;
        Thread aceitador;
        volatile bool ativo;
        int numero;

        public event Action<Trabalho> TrabalhoRecebido;
        public event Action<string> Erro;

        public bool Ativo { get { return ativo; } }
        public int Porta { get; private set; }
        public bool AceitaRede { get; private set; }

        public void Iniciar(int porta, bool aceitarRede)
        {
            Parar();
            ouvinte = new TcpListener(aceitarRede ? IPAddress.Any : IPAddress.Loopback, porta);
            ouvinte.Start();
            Porta = porta;
            AceitaRede = aceitarRede;
            ativo = true;
            aceitador = new Thread(Aceitar);
            aceitador.IsBackground = true;
            aceitador.Name = "Aceitar conexões " + porta;
            aceitador.Start();
        }

        public void Parar()
        {
            ativo = false;
            if (ouvinte != null)
            {
                try { ouvinte.Stop(); }
                catch (SocketException) { }
                ouvinte = null;
            }
        }

        void Aceitar()
        {
            TcpListener atual = ouvinte;
            while (ativo && atual != null)
            {
                TcpClient cliente;
                try
                {
                    cliente = atual.AcceptTcpClient();
                }
                catch (SocketException ex)
                {
                    if (ativo && Erro != null) Erro(ex.Message);
                    return;
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                Thread leitor = new Thread(delegate() { Atender(cliente); });
                leitor.IsBackground = true;
                leitor.Start();
            }
        }

        void Atender(TcpClient cliente)
        {
            string origem = "?";
            try
            {
                origem = ((IPEndPoint)cliente.Client.RemoteEndPoint).Address.ToString();
                cliente.ReceiveTimeout = SilencioFimTrabalho;
                NetworkStream fluxo = cliente.GetStream();
                MemoryStream acumulado = new MemoryStream();
                byte[] buffer = new byte[8192];
                while (ativo)
                {
                    int lidos;
                    try
                    {
                        lidos = fluxo.Read(buffer, 0, buffer.Length);
                    }
                    catch (IOException ex)
                    {
                        SocketException se = ex.InnerException as SocketException;
                        if (se != null && se.SocketErrorCode == SocketError.TimedOut)
                        {
                            Entregar(acumulado, origem);
                            continue;
                        }
                        break;
                    }
                    if (lidos <= 0)
                        break;
                    ResponderStatus(fluxo, buffer, lidos);
                    acumulado.Write(buffer, 0, lidos);
                }
                Entregar(acumulado, origem);
            }
            catch (Exception ex)
            {
                if (Erro != null) Erro("Conexão de " + origem + ": " + ex.Message);
            }
            finally
            {
                cliente.Close();
            }
        }

        static void ResponderStatus(NetworkStream fluxo, byte[] buffer, int tamanho)
        {
            for (int i = 0; i + 2 < tamanho; i++)
            {
                if (buffer[i] == 0x10 && buffer[i + 1] == 0x04 && buffer[i + 2] >= 1 && buffer[i + 2] <= 4)
                {
                    try { fluxo.WriteByte(0x12); }
                    catch (IOException) { }
                }
            }
        }

        void Entregar(MemoryStream acumulado, string origem)
        {
            if (acumulado.Length == 0)
                return;
            Trabalho t = new Trabalho();
            t.Numero = Interlocked.Increment(ref numero);
            t.Hora = DateTime.Now;
            t.Origem = origem;
            t.Dados = acumulado.ToArray();
            acumulado.SetLength(0);
            if (TrabalhoRecebido != null)
                TrabalhoRecebido(t);
        }
    }
}
