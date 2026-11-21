using System;
using System.Collections.Generic;
using System.IO;

namespace VisualizadorEscPos
{
    static class Config
    {
        static readonly string arquivo = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VisualizadorEscPos", "config.ini");
        static readonly Dictionary<string, string> valores = Ler();

        static Dictionary<string, string> Ler()
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            try
            {
                if (File.Exists(arquivo))
                    foreach (string linha in File.ReadAllLines(arquivo))
                    {
                        int igual = linha.IndexOf('=');
                        if (igual > 0)
                            d[linha.Substring(0, igual).Trim()] = linha.Substring(igual + 1).Trim();
                    }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            return d;
        }

        public static string Texto(string chave, string padrao)
        {
            string v;
            return valores.TryGetValue(chave, out v) ? v : padrao;
        }

        public static int Inteiro(string chave, int padrao)
        {
            int v;
            return int.TryParse(Texto(chave, ""), out v) ? v : padrao;
        }

        public static bool Logico(string chave, bool padrao)
        {
            string v = Texto(chave, "");
            return v == "" ? padrao : v == "1";
        }

        public static void Gravar(string chave, object valor)
        {
            valores[chave] = valor is bool ? ((bool)valor ? "1" : "0") : Convert.ToString(valor);
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(arquivo));
                List<string> linhas = new List<string>();
                foreach (KeyValuePair<string, string> par in valores)
                    linhas.Add(par.Key + "=" + par.Value);
                File.WriteAllLines(arquivo, linhas.ToArray());
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
