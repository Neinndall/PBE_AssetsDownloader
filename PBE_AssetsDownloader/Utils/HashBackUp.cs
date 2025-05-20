using System;
using System.IO;
using System.Threading.Tasks;
using Serilog;

namespace PBE_AssetsDownloader.Utils
{
    public class BackUp
    {
        private readonly DirectoriesCreator _directoriesCreator;

        public BackUp(DirectoriesCreator directoriesCreator)
        {
            _directoriesCreator = directoriesCreator;
        }

        public async Task<string> HandleBackUpAsync(bool createBackUp)
        {
            if (createBackUp)
            {
                // Copiar archivos específicos a la carpeta de respaldo ya creada
                return await CopyFilesToBackUp();
            }
            else
            {
                Log.Information("BackUpOldHashes is disabled.");  // Solo el log, sin devolver el mensaje
                return string.Empty;  // Puedes devolver un string vacío o algún otro valor si no es necesario
            }
        }

        public async Task<string> CopyFilesToBackUp()
        {
            try
            {
                // Crear la carpeta de respaldo asincrónicamente
                await _directoriesCreator.CreateBackUpOldHashesAsync();

                // Obtener la ruta de la carpeta de respaldo
                string backupDirectory = _directoriesCreator.GetBackUpOldHashesPath();

                // Verifica que el directorio de respaldo exista antes de copiar
                if (!Directory.Exists(backupDirectory))
                {
                    return "Backup directory does not exist";
                }

                // Definir los archivos específicos que se deben copiar
                var filesToCopy = new[] { "hashes.game.txt", "hashes.lcu.txt" };

                foreach (var fileName in filesToCopy)
                {
                    string sourceFilePath = Path.Combine("hashes", "olds", fileName);
                    string destinationFilePath = Path.Combine(backupDirectory, fileName);

                    // Verificar que el archivo de origen exista antes de copiar
                    if (File.Exists(sourceFilePath))
                    {
                        // Copiar el archivo
                        File.Copy(sourceFilePath, destinationFilePath, true);
                    }
                }

                Log.Information("Backup created successfully at {0}", backupDirectory);
                return backupDirectory;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred while creating backup");
                return "Error occurred while creating backup";
            }
        }
    }
}
