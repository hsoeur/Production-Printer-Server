using Serilog;

namespace Api.PrinterProd.Services.Printer
{
    public sealed class TemporaryPrintFile : IDisposable
    {
        private TemporaryPrintFile(string path)
        {
            Path = path;
        }

        public string Path { get; }

        public static TemporaryPrintFile Create()
        {
            string directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Api.PrinterProd", "print-jobs");
            Directory.CreateDirectory(directory);
            string path = System.IO.Path.Combine(directory, $"{Guid.NewGuid():N}.txt");
            return new TemporaryPrintFile(path);
        }

        public void Dispose()
        {
            try
            {
                if (System.IO.File.Exists(Path))
                {
                    System.IO.File.Delete(Path);
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Impossible de supprimer le fichier temporaire d'impression {Path}", Path);
            }
        }
    }
}
