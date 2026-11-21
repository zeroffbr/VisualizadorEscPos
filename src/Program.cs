using System;
using System.Windows.Forms;

namespace VisualizadorEscPos
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            if (args.Length >= 3 && string.Equals(args[1], "/png", StringComparison.OrdinalIgnoreCase))
            {
                int colunas = 48;
                if (args.Length >= 4) int.TryParse(args[3], out colunas);
                Modelo modelo = args.Length >= 5 ? LerModelo(args[4]) : Modelo.Epson;
                Environment.ExitCode = FormPrincipal.ExportarImagem(args[0], args[2], colunas, modelo);
                return;
            }
            Application.Run(new FormPrincipal(args));
        }

        static Modelo LerModelo(string texto)
        {
            switch (texto.ToLowerInvariant())
            {
                case "bematech": return Modelo.Bematech;
                case "elgin": return Modelo.Elgin;
                case "daruma": return Modelo.Daruma;
                default: return Modelo.Epson;
            }
        }
    }
}
