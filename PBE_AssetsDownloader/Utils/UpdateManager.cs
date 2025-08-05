// PBE_AssetsDownloader/Utils/UpdateManager.cs
using System;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Reflection;
using System.Windows;
using Newtonsoft.Json;
using Serilog; // Añadimos el using para Serilog

using PBE_AssetsDownloader.UI;
using PBE_AssetsDownloader.UI.Helpers;
using PBE_AssetsDownloader.UI.Dialogs;

namespace PBE_AssetsDownloader.Utils
{
	public static class UpdateManager
	{
		public static async Task CheckForUpdatesAsync(Window owner = null, bool showNoUpdatesMessage = true)
		{
			string currentVersionRaw = Assembly.GetExecutingAssembly().GetName().Version.ToString();
			string apiUrl = "https://api.github.com/repos/Neinndall/PBE_AssetsDownloader/releases/latest";
			string downloadUrl = "";   // URL of the new version's ZIP file
			long totalBytes = 0;       // Size of the file to download

			// User's downloads folder
			string userDownloadsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

			using (HttpClient client = new HttpClient())
			{
				// GitHub requires a User-Agent to be specified
				client.DefaultRequestHeaders.Add("User-Agent", "PBE_AssetsDownloader");

				try
				{
					// Get the latest release information
					var response = await client.GetStringAsync(apiUrl);
					var releaseData = JsonConvert.DeserializeObject<dynamic>(response);

					// Extract version and ZIP download URL
					string latestVersionRaw = releaseData.tag_name;
					downloadUrl = releaseData.assets[0].browser_download_url;
					totalBytes = releaseData.assets[0].size;

					// Clean both versions to compare only numbers
					string parsedCurrentVersion = Regex.Match(currentVersionRaw, @"\d+(\.\d+){1,3}").Value;
					string parsedLatestVersion = Regex.Match(latestVersionRaw.ToString(), @"\d+(\.\d+){1,3}").Value;

					// Verify if parsing was successful
					if (string.IsNullOrEmpty(parsedCurrentVersion) || string.IsNullOrEmpty(parsedLatestVersion))
					{
						System.Windows.MessageBox.Show("Could not parse version numbers.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
						return;
					}

					// Convert version strings to Version objects for comparison
					Version currentVer = new Version(parsedCurrentVersion);
					Version latestVer = new Version(parsedLatestVersion);

					// If a newer version is available
					if (latestVer.CompareTo(currentVer) > 0)
					{
						bool? result = CustomMessageBox.ShowYesNo(
							"Update available",
							$"New version available {latestVersionRaw}. Do you want to download it?",
							owner
						);

						if (result == true)
						{
							// Instantiate your new WPF Progress Window on the UI thread
							UpdateProgressWindow progressWindow = null;
							Application.Current.Dispatcher.Invoke(() =>
							{
								progressWindow = new UpdateProgressWindow();
								progressWindow.Show(); // Show the window non-modally
								progressWindow.UpdateLayout(); // Force layout update to ensure ActualWidth is available
							});

							// Local download path
							string fileName = $"PBE_AssetsDownloader_{latestVersionRaw}.zip";
							string downloadPath = Path.Combine(userDownloadsFolder, fileName);

							// Show total size before starting download
							string downloadSize = $"{(totalBytes / 1024.0 / 1024.0):0.00} MB";

							// Update UI on the Dispatcher thread
							progressWindow.Dispatcher.Invoke(() =>
							{
								progressWindow.SetProgress(0, $"Downloading {downloadSize}...");
							});
							await Task.Delay(500); // Brief wait for UI to update

							// Start downloading the ZIP file
							using (var responseDownload = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
							{
								responseDownload.EnsureSuccessStatusCode();
								long bytesDownloaded = 0;

								using (var fs = new FileStream(downloadPath, FileMode.Create))
								{
									byte[] buffer = new byte[8192];
									int bytesRead;

									using (var stream = await responseDownload.Content.ReadAsStreamAsync())
									{
										while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
										{
											await fs.WriteAsync(buffer, 0, bytesRead);
											bytesDownloaded += bytesRead;

											// Update progress during download on the Dispatcher thread
											if (totalBytes > 0)
											{
												int progressPercentage = (int)((bytesDownloaded * 100.0) / totalBytes);
												// Using Dispatcher.Invoke to safely update UI from background thread
												progressWindow.Dispatcher.Invoke(() =>
												{
													progressWindow.SetProgress(progressPercentage,
														$"Downloading... {(bytesDownloaded / 1024.0 / 1024.0):0.00} MB / {downloadSize}");
												});
											}
										}
									}
								}
								await Task.Delay(1200); // Brief wait for UI to update

								// Close the progress window safely on the Dispatcher thread
								progressWindow.Dispatcher.Invoke(() => { progressWindow.Close(); });
								CustomMessageBox.ShowInfo("Success", "Update downloaded successfully.", owner, CustomMessageBoxIcon.Success);
							}
						}
					}
					else if (showNoUpdatesMessage)
					{
						CustomMessageBox.ShowInfo("Updates", "No updates available.", owner, CustomMessageBoxIcon.Info);
					}
				}
				catch (Exception ex)
				{
					Serilog.Log.Error(ex, "Error checking for updates in UpdateManager.cs.");
					CustomMessageBox.ShowInfo("Error", "Error checking for updates:\n" + ex.Message, owner, CustomMessageBoxIcon.Error);
				}
			}
		}
	}
}