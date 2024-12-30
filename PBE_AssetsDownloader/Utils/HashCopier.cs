using System;
using System.IO;
using System.Threading.Tasks;

namespace PBE_AssetsDownloader.Utils
{
    public class HashCopier
    {
        public async Task<string> CopyNewHashesToOlds(string sourcePath, string destinationPath)
        {
            if (Directory.Exists(sourcePath))
            {
                // Elimina el directorio de destino si existe
                if (Directory.Exists(destinationPath))
                {
                    Directory.Delete(destinationPath, true);
                }

                // Copia el directorio
                await Task.Run(() => DirectoryCopy(sourcePath, destinationPath, true));

                // Devuelve el mensaje de éxito
                return "Hashes replaced successfully.";
            }

            // Devuelve el mensaje de fracaso si el directorio fuente no existe
            return "Hashes were not replaced because the source directory does not exist.";
        }

        private void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);
            DirectoryInfo[] dirs = dir.GetDirectories();

            // Si el directorio fuente no existe, lanza una excepción
            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException("Source directory does not exist or could not be found: " + sourceDirName);
            }

            // Si el directorio de destino no existe, créalo
            Directory.CreateDirectory(destDirName);

            // Obtiene los archivos en el directorio y los copia al nuevo destino
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }

            // Si se deben copiar subdirectorios, cópialos y sus contenidos al nuevo destino
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }
    }
}