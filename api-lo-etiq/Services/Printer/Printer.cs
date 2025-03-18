using CmdGenerator;
using Serilog;
using System.Diagnostics;

namespace api_lo_etiq.Services.Printer
{
    public class Printer : IPrinter
    {
        private readonly IConfiguration _configuration;

        public Printer(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void PrintFtp()
        {
            string ftpServer = "192.168.1.200";
            string ftpUser = "ftpprint";
            string ftpPassword = "print";
            string fileName = "out_file_art_borne.txt";

            Log.Debug("🚀 Début du processus FTP...\n");

            if (!File.Exists(fileName))
            {
                Log.Debug($"❌ ERREUR : Le fichier '{fileName}' n'existe pas !");
                return;
            }

            ExecuteFtpCommand(ftpServer, ftpUser, ftpPassword, fileName);
        }

        public void ExecuteFtpCommand(string ftpServer, string ftpUser, string ftpPassword, string filePath)
        {
            string ftpUrl = $"ftp://{ftpUser}:{ftpPassword}@{ftpServer}/";

            // Construire la commande `curl` pour transférer le fichier via FTP
            string command = $"curl -T {filePath} {ftpUrl}";

            Log.Information($"🚀 Envoi du fichier {filePath} vers {ftpServer}...");

            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "/bin/sh",
                    Arguments = $"-c \"{command}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using Process process = new Process { StartInfo = startInfo };
                process.Start();

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                process.WaitForExit();
                int exitCode = process.ExitCode;

                Log.Information($"📜 Sortie : {output}");
                if (!string.IsNullOrEmpty(error))
                {
                    Log.Error($"❌ Erreur FTP : {error}");
                }

                Console.WriteLine($"✅ Envoi terminé (Code de sortie : {exitCode})");
            }
            catch (Exception ex)
            {
                Log.Error($"🚨 Erreur lors de l'envoi FTP : {ex.Message}");
            }
        }


        /// <summary>
        /// Méthode qui exécute la commande d'impression sur le fichier généré à partir du template.
        /// </summary>
        /// <param name="usage"></param>
        public void ExecutePrinting(string usage)
        {
            Log.Information("Printer::ExecutePrinting() => Lancement de la commande d'impression");

            string fileNameOutForBrady = "out_file.txt";
            string printer = _configuration["Printer:Share"];

            if (usage != "Etiquette Lot")
            {
                printer = _configuration["Printer:OtherThanLot"];
            }

            string commandBrady = $"copy \"{fileNameOutForBrady}\" \"{printer}\"";

            Log.Information($"CommandBrady : {commandBrady}");

            CommandGenerator commandGenerator = new CommandGenerator(commandBrady);
            commandGenerator.Execute();
        }
        public bool PrintFileTest(string usage)
        {
            string printer = _configuration["Printer:Share"];

            if (usage != "Etiquette Lot")
            {
                printer = _configuration["Printer:OtherThanLot"];
            }

            
            string user = "AMPHI-PROD\\adminprod"; // Utilisateur avec domaine
            string password = "Paris2024JO*"; // Mot de passe

            // Conversion du chemin d'imprimante Windows pour Linux (Samba)
            string printerLinux = $"//{printer.Replace("\\\\", "").Replace("\\", "/")}";

            // Construction de la commande `smbclient`
            string commandBrady = $"echo \"{password}\" | smbclient {printerLinux} -U \"{user}\" -c \"put out_file.txt\"";

            Log.Information($"CommandBrady : {commandBrady}");

            try
            {
                Process process = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "/bin/sh",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                process.StartInfo = startInfo;
                process.Start();

                process.StandardInput.WriteLine(commandBrady);
                process.StandardInput.Flush();
                process.StandardInput.Close();

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                process.WaitForExit();
                int exitCode = process.ExitCode;
                process.Close();

                Log.Information($"Sortie : {output}");
                if (!string.IsNullOrEmpty(error))
                {
                    Log.Error($"Erreur : {error}");
                    return false; // Retourne false si une erreur est détectée
                }

                Console.WriteLine($"Code de sortie : {exitCode}");
                return exitCode == 0; // Retourne true si le code de sortie est 0 (succès)
            }
            catch (Exception ex)
            {
                Log.Error($"Erreur lors de l'exécution de la commande : {ex.Message}");
                return false;
            }
        }

    }
}
