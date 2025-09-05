<div align="center">
  <img src="https://github.com/Neinndall/PBE_AssetsManager/blob/main/PBE_AssetsManager/Resources/img/full_logo.ico" alt="PBE_AssetsManager Logo" width="150">
</div>

## ✅ APP Reliability
I decided to create this section to provide reliability to the tool I've been developing for some time, so that all GitHub developers and users can trust my projects and future ones. Therefore, I decided to do it for your security. So you can verify that no file has been modified since its publication and that it is free of malware.

GitHub includes a **SHA-256** security code linked to the .rar package. You can find this **SHA-256** number in the [releases of the tool](https://github.com/Neinndall/PBE_AssetsManager/releases) section, in the **ASSETS** section. It gives you the option to copy it and verify it with the **VIRUSTOTAL** link I will provide with each version release.

If both the **SHA-256** from **GitHub** and **VIRUSTOTAL** match, it means that the .rar package that was scanned by virustotal has not been modified at any point, so you can confidently verify that the scan was performed on the .rar package of the latest version. With each release, I will provide you with the URL of each virustotal scan with its details and the package name for the version that will appear just above with its details. Most importantly, **SHA-256**, don't forget to check every detail.

*   **Latest Version v2.3.0.0:** **[VirusTotal details and scans](https://www.virustotal.com/gui/file/26c66289016a37ae3500d06e71aee48531927c4f9c02e42c7b13c78ccdeec27d/details)** 
     *   *GITHUB* --> You cant get the sha-256 [here](https://github.com/Neinndall/PBE_AssetsManager/releases)
     *   *VIRUSTOTAL* --> You cant get the sha-256 in `details` from the link of VirusTotal in details

## 🛠️ PBE_AssetsManager

This tool is designed to automatically download and manage new assets from League of Legends PBE server updates. It helps content creators, League of legends players, and more stay up-to-date with the latest changes and additions to the game, leveraging data from [Communitydragon](https://raw.communitydragon.org/). Forget manual checks and downloads; PBE_AssetsManager streamlines your workflow, ensuring you always have the most current game files.

## ✨ Key Features

*   **Advanced WAD File Comparison:** Compare `.wad` archives between two PBE directories to identify new, modified, deleted, or renamed assets. Includes a side-by-side visual diff for image changes (`ImageDiffWindow`).
*   **Portable Comparison Packages:** Save comparison results asynchronously as a lightweight, self-contained package. This includes only the changed file chunks, allowing for easy sharing and review without the original PBE directories.
*   **Powerful PBE File Explorer:** Browse game files with a familiar file-tree interface, featuring real-time search and lazy-loading. The integrated previewer supports a wide range of formats, including textures (`.tex`, `.dds`), images (`.png`, `.svg`), text, audio, video, and provides a readable visualization for binary `.bin` files.
*   **3D Model Viewer:** A fully integrated tool to load and inspect 3D models (`.skn`), skeletons (`.skl`), and play animations (`.anm`) directly within the application.
*   **Automated Asset Downloading:** Efficiently compares local and server hashes to download only new or modified assets from Community Dragon.
*   **Flexible Asset Filtering:** Customize downloads by excluding file extensions and applying complex URL-based rules.
*   **Modern & Responsive Architecture:** Built with an event-driven, asynchronous-first approach to ensure a smooth, non-blocking user experience. It leverages Dependency Injection (DI) for a clean and maintainable codebase.

## 🦾 Advanced Functionality

### Monitoring Suite (`MonitorWindow`)

PBE_AssetsManager now includes a powerful suite of tools to automatically track changes in game assets without manual intervention.

*   **File Watcher:** Monitor a list of remote JSON files for any updates. When a change is detected, the app automatically saves the old and new versions and logs the difference, allowing you to view the changes at any time.
*   **Asset Tracker:** Keep a persistent list of specific assets you want to track. The tool will periodically check their status (e.g., "OK", "Not Found", "Pending") in the background. It even includes fallback logic for assets with multiple possible extensions (like `.jpg` and `.png`).
*   **History View:** All detected changes from the File Watcher are saved in a persistent history. You can browse past changes, view the diffs, and manage the history log.

### 3D Model Viewer (`ModelWindow`)

Explore League of Legends 3D assets like never before.

*   **Model & Skeleton Loading:** Load `.skn` (mesh) and `.skl` (skeleton) files to view character and environment models.
*   **Animation Playback:** Apply `.anm` (animation) files to a loaded skeleton to see the model come to life with full skinning support.
*   **Scene Control:** Manipulate the 3D camera, manage loaded parts, and inspect model geometry.

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
5.  **Explore & Monitor:** Use the `Explorer`, `Comparator`, `Monitor`, and `Model Viewer` tabs to access the advanced features of the application.

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

All application settings are managed through the `Settings` window and persisted in the `config.json` file. This allows you to customize the application's behavior to fit your workflow.

Key configurable options include:

*   **Default Paths:** Set the default directories for new hashes, old hashes, and the starting folder for the PBE File Explorer.
*   **Workflow Automation:** Enable or disable automatic copying of new hashes and the creation of backups for old hash lists after a download.
*   **Advanced Data Monitoring:** Specify a list of remote JSON files to monitor for updates and manage their change history.
*   **Application Updates:** Control whether the application automatically checks for new versions on startup.

## 🤝 Contributing

Contributions are welcome! If you have suggestions for improvements, bug reports, or want to contribute code, please feel free to:

1.  Fork the repository and submit a [pull requests](https://github.com/Neinndall/PBE_AssetsManager/pulls). 
2.  Open an [issues](https://github.com/Neinndall/PBE_AssetsManager/issues) to discuss your ideas or report bugs.

Please ensure your code adheres to the project's existing style and conventions.

## 📄 License

This project is licensed under the [GNU General Public License v3.0](LICENSE).
