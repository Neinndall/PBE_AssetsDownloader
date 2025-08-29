<div align="center">
  <img src="https://github.com/Neinndall/PBE_AssetsManager/blob/main/PBE_AssetsManager/Resources/img/full_logo.ico" alt="PBE_AssetsManager Logo" width="150">
</div>

## ✅ APP Reliability
I decided to create this section to provide reliability to the tool I've been developing for some time, so that all GitHub developers and users can trust my projects and future ones. Therefore, I decided to do it for your security. So you can verify that no file has been modified since its publication and that it is free of malware.

GitHub includes a **SHA-256** security code linked to the .rar package. You can find this **SHA-256** number in the [releases of the tool](https://github.com/Neinndall/PBE_AssetsManager/releases) section, in the **ASSETS** section. It gives you the option to copy it and verify it with the **VIRUSTOTAL** link I will provide with each version release.

If both the **SHA-256** from **GitHub** and **VIRUSTOTAL** match, it means that the .rar package that was scanned by virustotal has not been modified at any point, so you can confidently verify that the scan was performed on the .rar package of the latest version. With each release, I will provide you with the URL of each virustotal scan with its details and the package name for the version that will appear just above with its details. Most importantly, **SHA-256**, don't forget to check every detail.

*   **Latest Version v2.2.0.0:** **[VirusTotal details and scans](https://www.virustotal.com/gui/file/a83a4c44c4a030a2828409875b013bc870aa31baaff3c9d43ba09ba9ede9ec95/details)** 
     *   *GITHUB* --> You cant get the sha-256 [here](https://github.com/Neinndall/PBE_AssetsManager/releases)
     *   *VIRUSTOTAL* --> You cant get the sha-256 in `details` from the link of VirusTotal in details

## 🛠️ PBE_AssetsManager

This tool is designed to automatically download and manage new assets from League of Legends PBE server updates. It helps content creators, League of legends players, and more stay up-to-date with the latest changes and additions to the game, leveraging data from [Communitydragon](https://raw.communitydragon.org/). Forget manual checks and downloads; PBE_AssetsManager streamlines your workflow, ensuring you always have the most current game files.

## ✨ Key Features

*   **Advanced WAD File Comparison:** Compare `.wad` archives between two PBE directories to identify new, modified, deleted, or renamed assets. Includes a side-by-side visual diff for image changes.
*   **Portable Comparison Packages:** Save comparison results as a lightweight, self-contained package. This includes only the changed file chunks, allowing for easy sharing and review without the original PBE directories.
*   **Powerful PBE File Explorer:** Browse game files with a familiar file-tree interface, featuring real-time search and lazy-loading for efficient navigation of large .wad archives.
*   **Integrated Asset Preview:** Directly preview a wide range of file types: visualize `.bin` files as readable data, view textures (`.tex`, `.dds`), images (`.png`, `.svg`), text (`.json`, `.lua`, `.xml`), audio, and video.
*   **Automated Hash-Based Downloading:** Efficiently compares local and server hashes to download only new or modified assets from Community Dragon, saving bandwidth.
*   **Flexible Asset Filtering:** Customize downloads by excluding specific file extensions and applying custom rules for asset URLs, including complex patterns for `.bin` files.
*   **Robust File & Settings Management:** Features automatic directory creation, smart cleanup, hash file backups, and persistent settings for a streamlined workflow.
*   **In-App Update Checks:** Stay current with the latest version of PBE_AssetsManager through integrated GitHub update checks.
*   **Modern & Responsive Architecture:**
    *   Built with an event-driven approach and asynchronous operations to ensure a smooth, non-blocking user experience.
    *   Utilizes Dependency Injection (DI) for a clean, maintainable, and testable codebase.
    *   Features a dual-log system for simplified debugging, with separate logs for general information and detailed errors.

## 🚀 Getting Started

### Prerequisites

*   [.NET 8.0 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-8.0.8-windows-x64-installer) (or higher) installed on your system.

### Installation

1.  **Download the latest release:** Visit the [Releases page](https://github.com/Neinndall/PBE_AssetsManager/releases) (replace with your actual releases URL) and download the `PBE_AssetsManager.zip` file.
2.  **Extract the contents:** Unzip the downloaded file to your desired location (e.g., `C:\PBE_AssetsManager`).
3.  **Run the application:** Navigate to the extracted folder and run `PBE_AssetsManager.exe`.

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

1.  Fork the repository and submit a [pull requests](https://github.com/Neinndall/PBE_AssetsManager/pulls). 
2.  Open an [issues](https://github.com/Neinndall/PBE_AssetsManager/issues) to discuss your ideas or report bugs.

Please ensure your code adheres to the project's existing style and conventions.

## 📄 License

This project is licensed under the [GNU General Public License v3.0](LICENSE).
