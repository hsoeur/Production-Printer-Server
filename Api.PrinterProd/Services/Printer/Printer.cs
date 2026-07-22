using Api.PrinterProd.Configuration;
using CmdGenerator;
using Serilog;
using System.Diagnostics;
using System.Collections.Concurrent;

namespace Api.PrinterProd.Services.Printer
{
    public class Printer : IPrinter
    {
        private readonly IConfiguration _configuration;
        private readonly ConcurrentDictionary<string, object> _machineLocks = new(StringComparer.OrdinalIgnoreCase);

        public Printer(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void PrintFtp(string filePath)
        {
            string ftpServer = _configuration.GetRequiredString("Printer:Ftp:ArtBorne:Server");
            string ftpUser = _configuration.GetRequiredString("Printer:Ftp:ArtBorne:User");
            string ftpPassword = _configuration.GetRequiredString("Printer:Ftp:ArtBorne:Password");

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
                int maxLogContentLength = _configuration.GetRequiredInt("Printing:LogContentMaxLength");
                if (fileContent.Length > maxLogContentLength)
                {
                    Log.Debug($"📄 Contenu du fichier '{filePath}' (tronqué à {maxLogContentLength} caractères) :\n{fileContent.Substring(0, maxLogContentLength)}\n...");
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

            if (usage != _configuration.GetRequiredString("Application:DefaultLotUsage"))
            {
                printer = _configuration["Printer:OtherThanLot"];
            }


            string user = _configuration.GetRequiredString("Printer:Samba:User");
            string password = _configuration.GetRequiredString("Printer:Samba:Password");

            // Conversion du chemin d'imprimante Windows pour Linux (Samba)
            string printerLinux = $"//{printer.Replace("\\\\", "").Replace("\\", "/")}";

            // Construction de la commande `smbclient`
            string commandBrady = $"echo \"{password}\" | smbclient {printerLinux} -U \"{user}\" -c \"put \\\"{filePath}\\\"\"";

            Log.Information("CommandBrady : smbclient {PrinterLinux} -U {User} -c put {FilePath}", printerLinux, user, filePath);

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

            if (usage != _configuration.GetRequiredString("Application:DefaultLotUsage"))
            {
                printer = _configuration["Printer:OtherThanLot"];
            }


            string user = _configuration.GetRequiredString("Printer:Samba:User");
            string password = _configuration.GetRequiredString("Printer:Samba:Password");

            // Conversion du chemin d'imprimante Windows pour Linux (Samba)
            string printerLinux = $"//{printer.Replace("\\\\", "").Replace("\\", "/")}";

            // Construction de la commande `smbclient`
            string commandBrady = $"echo \"{password}\" | smbclient {printerLinux} -U \"{user}\" -c \"put \\\"{filePath}\\\"\"";

            Log.Information("CommandBrady : smbclient {PrinterLinux} -U {User} -c put {FilePath}", printerLinux, user, filePath);

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
            string ftpServer = _configuration.GetRequiredString("Printer:Ftp:SerialNumber:Server");
            string ftpUser = _configuration.GetRequiredString("Printer:Ftp:SerialNumber:User");
            string ftpPassword = _configuration.GetRequiredString("Printer:Ftp:SerialNumber:Password");
            return PrintBySerialNumberFtp("legacy", ftpServer, ftpUser, ftpPassword, filePath);
        }

        public bool IsSerialNumberPrinterConfigured(string machineName)
        {
            return !string.IsNullOrWhiteSpace(machineName)
                && _configuration.GetSection($"Printer:Ftp:Machines:{machineName.Trim()}").Exists();
        }

        public bool PrintBySerialNumberFtp(string machineName, string filePath)
        {
            string configurationPath = $"Printer:Ftp:Machines:{machineName.Trim()}";
            string ftpServer = _configuration.GetRequiredString($"{configurationPath}:Server");
            string ftpUser = _configuration.GetRequiredString($"{configurationPath}:User");
            string ftpPassword = _configuration.GetRequiredString($"{configurationPath}:Password");
            return PrintBySerialNumberFtp(machineName.Trim(), ftpServer, ftpUser, ftpPassword, filePath);
        }

        private bool PrintBySerialNumberFtp(string machineName, string ftpServer, string ftpUser, string ftpPassword, string filePath)
        {
            Log.Information("Début du processus d'impression FTP pour le poste {MachineName}", machineName);

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
                int maxLogContentLength = _configuration.GetRequiredInt("Printing:LogContentMaxLength");
                if (fileContent.Length > maxLogContentLength)
                {
                    Log.Debug($"📄 Contenu du fichier '{filePath}' (tronqué à {maxLogContentLength} caractères) :\n{fileContent.Substring(0, maxLogContentLength)}\n...");
                }
                else
                {
                    Log.Debug($"📄 Contenu complet du fichier '{filePath}' :\n{fileContent}");
                }

                // 🔹 Exécution de la commande FTP
                object machineLock = _machineLocks.GetOrAdd(machineName, _ => new object());
                lock (machineLock)
                {
                    return ExecuteFtpCommandForSerial(ftpServer, ftpUser, ftpPassword, filePath);
                }
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
            string ftpUrl = $"ftp://{ftpServer}/";

            Log.Information($"🚀 Envoi du fichier {filePath} vers {ftpServer} via FTP...");

            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "curl",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                startInfo.ArgumentList.Add("--fail");
                startInfo.ArgumentList.Add("--silent");
                startInfo.ArgumentList.Add("--show-error");
                startInfo.ArgumentList.Add("--user");
                startInfo.ArgumentList.Add($"{ftpUser}:{ftpPassword}");
                startInfo.ArgumentList.Add("-T");
                startInfo.ArgumentList.Add(filePath);
                startInfo.ArgumentList.Add(ftpUrl);

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
