using CmdGenerator;
using Serilog;
using System.Diagnostics;

namespace Api.PrinterProd.Services.Printer
{
    public class Printer : IPrinter
    {
        private readonly IConfiguration _configuration;

        public Printer(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void PrintFtp(string filePath)
        {
            string ftpServer = "192.168.1.200";
            string ftpUser = "ftpprint";
            string ftpPassword = "print";

            Log.Debug("🚀 Début du processus FTP...\n");

            if (!File.Exists(filePath))
            {
                Log.Debug($"❌ ERREUR : Le fichier '{filePath}' n'existe pas !");
                return;
            }

            try
            {
                // 🔹 Lecture du contenu du fichier
                string fileContent = File.ReadAllText(filePath);

                // 🔹 Log du contenu (tronqué si trop long pour éviter de polluer les logs)
                if (fileContent.Length > 2000)
                {
                    Log.Debug($"📄 Contenu du fichier '{filePath}' (tronqué à 2000 caractères) :\n{fileContent.Substring(0, 2000)}\n...");
                }
                else
                {
                    Log.Debug($"📄 Contenu complet du fichier '{filePath}' :\n{fileContent}");
                }

                // 🔹 Exécution de la commande FTP
                ExecuteFtpCommand(ftpServer, ftpUser, ftpPassword, filePath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Erreur lors de la lecture du fichier '{filePath}'");
            }
        }


        public void ExecuteFtpCommand(string ftpServer, string ftpUser, string ftpPassword, string filePath)
        {
            string ftpUrl = $"ftp://{ftpUser}:{ftpPassword}@{ftpServer}/";

            // Construire la commande `curl` pour transférer le fichier via FTP
            string command = $"curl -T \"{filePath}\" {ftpUrl}";

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
        public void ExecutePrinting(string usage, string filePath)
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
            string commandBrady = $"echo \"{password}\" | smbclient {printerLinux} -U \"{user}\" -c \"put \\\"{filePath}\\\"\"";

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
                }

                Console.WriteLine($"Code de sortie : {exitCode}");

            }
            catch (Exception ex)
            {
                Log.Error($"Erreur lors de l'exécution de la commande : {ex.Message}");
                throw;
            }
        }
        public bool PrintFileTest(string usage, string filePath)
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
            string commandBrady = $"echo \"{password}\" | smbclient {printerLinux} -U \"{user}\" -c \"put \\\"{filePath}\\\"\"";

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

        /// <summary>
        /// Méthode pour imprimer le fichier out_file.txt via FTP pour l'impression par numéro de série.
        /// Utilisée en production depuis le conteneur Docker.
        /// </summary>
        public bool PrintBySerialNumberFtp(string filePath)
        {
            string ftpServer = "192.168.1.201";
            string ftpUser = "ftpprint";
            string ftpPassword = "print";
            Log.Information("🚀 Début du processus d'impression FTP pour PrintBySerialNumber...");

            if (!File.Exists(filePath))
            {
                Log.Error($"❌ ERREUR : Le fichier '{filePath}' n'existe pas !");
                return false;
            }

            try
            {
                // 🔹 Lecture du contenu du fichier pour logs
                string fileContent = File.ReadAllText(filePath);

                // 🔹 Log du contenu (tronqué si trop long)
                if (fileContent.Length > 2000)
                {
                    Log.Debug($"📄 Contenu du fichier '{filePath}' (tronqué à 2000 caractères) :\n{fileContent.Substring(0, 2000)}\n...");
                }
                else
                {
                    Log.Debug($"📄 Contenu complet du fichier '{filePath}' :\n{fileContent}");
                }

                // 🔹 Exécution de la commande FTP
                return ExecuteFtpCommandForSerial(ftpServer, ftpUser, ftpPassword, filePath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Erreur lors de la lecture du fichier '{filePath}'");
                return false;
            }
        }

        /// <summary>
        /// Exécute la commande curl pour envoyer le fichier via FTP (pour PrintBySerialNumber).
        /// </summary>
        private bool ExecuteFtpCommandForSerial(string ftpServer, string ftpUser, string ftpPassword, string filePath)
        {
            string ftpUrl = $"ftp://{ftpUser}:{ftpPassword}@{ftpServer}/";
            string command = $"curl -v -T \"{filePath}\" {ftpUrl}";

            Log.Information($"🚀 Envoi du fichier {filePath} vers {ftpServer} via FTP...");
            Log.Debug($"Commande curl : {command}");

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

                Log.Information($"📜 Sortie curl : {output}");

                if (exitCode == 0)
                {
                    Log.Information($"✅ Envoi FTP réussi (Code de sortie : {exitCode})");
                    return true;
                }
                else
                {
                    Log.Error($"❌ Erreur FTP (Code de sortie : {exitCode}) : {error}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"🚨 Exception lors de l'envoi FTP : {ex.Message}");
                return false;
            }
        }

    }
}
