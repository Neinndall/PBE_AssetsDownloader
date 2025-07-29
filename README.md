<div align="center">
  <img src="https://github.com/Neinndall/PBE_AssetsDownloader/blob/dev/PBE_AssetsDownloader/img/new/full_logo.ico?raw=true" alt="PBE_AssetsDownloader Logo" width="150">
</div>

## ✅ APP Reliability
I decided to create this section to provide reliability to the tool I've been developing for some time, so that all GitHub developers and users can trust my projects and future ones. Therefore, I decided to do it for your security. So you can verify that no file has been modified since its publication and that it is free of malware.

GitHub includes a **SHA-256** security code linked to the .rar package. You can find this **SHA-256** number in the [releases of the tool](https://github.com/Neinndall/PBE_AssetsDownloader/releases) section, in the **ASSETS** section. It gives you the option to copy it and verify it with the **VIRUSTOTAL** link I will provide with each version release.

If both the **SHA-256** from **GitHub** and **VIRUSTOTAL** match, it means that the .rar package that was scanned by virustotal has not been modified at any point, so you can confidently verify that the scan was performed on the .rar package of the latest version. With each release, I will provide you with the URL of each virustotal scan with its details and the package name for the version that will appear just above with its details. Most importantly, **SHA-256**, don't forget to check every detail.

*   **Latest Version v2.0.0.0:** **[VirusTotal details and scans](https://www.virustotal.com/gui/file/6ac9b8259fa39b5221d3d272211e55cf1272fe03d4446ba3e640e6f177af935f/details)** 
     *   `(Github: sha256:6ac9b8259fa39b5221d3d272211e55cf1272fe03d4446ba3e640e6f177af935f)` 
     *   `(VirusTotal: SHA-256 6ac9b8259fa39b5221d3d272211e55cf1272fe03d4446ba3e640e6f177af935f)` 

## 🛠️ PBE_AssetsDownloader

This tool is designed to automatically download and manage new assets from League of Legends PBE server updates. It helps content creators, League of Legends players, and more stay up-to-date with the latest changes and additions to the game, leveraging data from [Communitydragon](https://raw.communitydragon.org/). Forget manual checks and downloads; PBE_AssetsDownloader streamlines your workflow, ensuring you always have the most current game files.

## ✨ Key Features

*   **Automated Hash Comparison:** Efficiently compares local file hashes with server-side data to identify only new or modified assets.
*   **Intelligent Asset Downloading:** Downloads new and updated assets from Community Dragon, minimizing bandwidth usage.
*   **Flexible Asset Filtering:** Customize downloads by excluding specific file extensions and applying custom rules for URLs.
*   **Robust File Management:**
    *   Automatic directory creation for downloaded assets.
    *   Smart cleanup of empty directories post-download.
    *   Automated backup of old hash files.
    *   Seamless copying of new hashes to the old hashes folder for continuous tracking.
*   **In-App Update Checks:** Stay current with the latest version of PBE_AssetsDownloader through integrated GitHub update checks.
*   **Intuitive Graphical Interface (GUI):**
    *   Real-time download progress display.
    *   Comprehensive application settings configuration.
    *   Detailed operation logs for transparency.
*   **Advanced Asset Preview System:**
    *   Direct in-app preview for images and text-based assets (JSON, XML, TXT) without temporary files.
    *   Robust temporary file handling for external media (audio, video) with automated cleanup.

## 🚀 Getting Started

### Prerequisites

*   [.NET 8.0 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-8.0.8-windows-x64-installer) (or higher) installed on your system.

### Installation

1.  **Download the latest release:** Visit the [Releases page](https://github.com/Neinndall/PBE_AssetsDownloader/releases) (replace with your actual releases URL) and download the `PBE_AssetsDownloader.zip` file.
2.  **Extract the contents:** Unzip the downloaded file to your desired location (e.g., `C:\PBE_AssetsDownloader`).
3.  **Run the application:** Navigate to the extracted folder and run `PBE_AssetsDownloader.exe`.

## 📖 Usage

1.  **Configure Settings:** Open the `Settings` tab to set up your preferences, including hash synchronization, auto-copy, and backup options.
2.  **Select Directories:** In the `Home` tab, specify your "New Hashes Directory" and "Old Hashes Directory".
3.  **Start Download:** Click the "Start Download" button to begin the asset extraction and download process. The application will compare hashes and download only the necessary files.
4.  **Preview Assets:** Use the `Export` tab to preview specific asset types (images, audio, etc.) and download selected items.

## ⚙️ Configuration

All application settings are managed through the `Settings` window within the application. These settings are persisted in a `config.json` file located in the application's root directory. You can configure:

*   Hash synchronization with Community Dragon.
*   Automatic copying of new hashes.
*   Backup options for old hashes.
*   Paths for new and old hash directories.
*   JSON data updates.

## 🤝 Contributing

Contributions are welcome! If you have suggestions for improvements, bug reports, or want to contribute code, please feel free to:

1.  Fork the repository and submit a [pull requests](https://github.com/Neinndall/PBE_AssetsDownloader/pulls). 
2.  Open an [issues](https://github.com/Neinndall/PBE_AssetsDownloader/issues) to discuss your ideas or report bugs.

Please ensure your code adheres to the project's existing style and conventions.

## 📄 License

This project is licensed under the [GNU General Public License v3.0](LICENSE).
