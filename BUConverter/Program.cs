// Program.cs

using System;
using System.IO;
using System.Linq;
using System.Management; // Für Hardware-ID-Abfragen (WMI)
using System.Security.Cryptography; // Für MD5-Hashing der HWID
using System.Text; // Für StringBuilder
using System.Windows.Forms; // Für MessageBox, Application.Exit und Clipboard
using System.Threading; // Für Thread.Sleep zur Behebung des Clipboard-Problems
using System.Reflection; // Für dynamisches Laden und Reflection

// Der 'using BUConverter.License;' wird entfernt, da wir die DLL nicht mehr direkt referenzieren.
// Stattdessen laden wir sie dynamisch.

namespace BUConverter
{
    static class Program
    {
        // Name der Lizenz-DLL. Diese Datei MUSS neben der EXE liegen.
        private const string LicenseDllName = "BUConverter.License.dll";

        /// <summary>
        /// Der Haupteinstiegspunkt für die Anwendung.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string extractedHwidFromDll = string.Empty;
            string extractedLicenseeName = string.Empty; // NEU: Variable für den Lizenznehmernamen
            bool licenseLoadedSuccessfully = false;

            try
            {
                // Versuche, die Lizenz-DLL dynamisch zu laden
                string licenseDllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LicenseDllName);

                if (!File.Exists(licenseDllPath))
                {
                    ShowMissingLicenseDllError();
                    return; // Beende die Anwendung
                }

                // Lade die Assembly
                Assembly licenseAssembly = Assembly.LoadFrom(licenseDllPath);

                // Suche den Typ (Klasse) LicenseData in der geladenen Assembly
                Type licenseDataType = licenseAssembly.GetType("BUConverter.License.LicenseData");

                if (licenseDataType == null)
                {
                    ShowLicenseDataClassNotFoundError();
                    return;
                }

                // Hole den Wert des Feldes AuthorizedHWID
                FieldInfo authorizedHwidField = licenseDataType.GetField("AuthorizedHWID", BindingFlags.Public | BindingFlags.Static);
                if (authorizedHwidField == null || authorizedHwidField.FieldType != typeof(string))
                {
                    ShowAuthorizedHwidFieldNotFoundError();
                    return;
                }
                extractedHwidFromDll = (string)authorizedHwidField.GetValue(null); // null, weil es ein statisches Feld ist

                // NEU: Hole den Wert des Feldes LicenseeName
                FieldInfo licenseeNameField = licenseDataType.GetField("LicenseeName", BindingFlags.Public | BindingFlags.Static);
                if (licenseeNameField == null || licenseeNameField.FieldType != typeof(string))
                {
                    // Wenn LicenseeName fehlt, verwenden wir einen Standardwert oder zeigen einen Fehler.
                    // Da es nur für die Anzeige ist, ist ein Standardwert akzeptabler als ein Absturz.
                    extractedLicenseeName = "Unknown Licensee";
                    // Optional: MessageBox.Show("Warning: LicenseeName field not found in DLL. Using default.", "License Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    extractedLicenseeName = (string)licenseeNameField.GetValue(null);
                }

                licenseLoadedSuccessfully = true;
            }
            catch (Exception ex)
            {
                // Allgemeine Fehler beim Laden der DLL oder beim Zugriff per Reflection
                ShowGeneralLicenseError(ex);
                return;
            }

            // Wenn die Lizenz-DLL erfolgreich geladen und die HWID extrahiert wurde,
            // führe die eigentliche HWID-Prüfung durch.
            if (licenseLoadedSuccessfully && VerifyHardwareIdAgainstExtracted(extractedHwidFromDll))
            {
                // HWID übereinstimmend, Programm starten
                // NEU: Übergabe des Lizenznehmernamens an den Form1-Konstruktor
                Application.Run(new Form1(extractedLicenseeName));
            }
            else
            {
                // HWID stimmt NICHT überein, oder Extract fehlgeschlagen (obwohl es oben abgefangen sein sollte)
                // Zeige die Access Denied Meldung
                ShowAccessDeniedError();
                return;
            }
        }

        /// <summary>
        /// Generiert eine Hardware-ID basierend auf der CPU-ID und der Motherboard-Seriennummer.
        /// </summary>
        /// <returns>Die generierte HWID als MD5-Hash (32 Zeichen hexadezimal) oder eine leere Zeichenkette bei Fehlern.</returns>
        private static string GetHardwareId()
        {
            try
            {
                string cpuId = string.Empty;
                string baseBoardSerial = string.Empty;

                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT ProcessorId FROM Win32_Processor"))
                {
                    foreach (ManagementObject mo in searcher.Get())
                    {
                        cpuId = mo["ProcessorId"]?.ToString() ?? string.Empty;
                        break;
                    }
                }

                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BaseBoard"))
                {
                    foreach (ManagementObject mo in searcher.Get())
                    {
                        baseBoardSerial = mo["SerialNumber"]?.ToString() ?? string.Empty;
                        break;
                    }
                }

                string rawHwid = $"{cpuId}-{baseBoardSerial}".Trim('-');
                if (string.IsNullOrEmpty(rawHwid))
                {
                    // Fallback, falls CPU-ID und Motherboard-Seriennummer nicht ermittelt werden können
                    rawHwid = Environment.MachineName + "_" + Environment.UserName;
                }

                // Wichtig: HWID immer in Großbuchstaben umwandeln, um Probleme mit Groß-/Kleinschreibung zu vermeiden
                rawHwid = rawHwid.ToUpper();

                using (MD5 md5 = MD5.Create())
                {
                    byte[] inputBytes = Encoding.ASCII.GetBytes(rawHwid);
                    byte[] hashBytes = md5.ComputeHash(inputBytes);

                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < hashBytes.Length; i++)
                    {
                        sb.Append(hashBytes[i].ToString("X2"));
                    }
                    return sb.ToString();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error retrieving ID: {ex.Message}\n\n" +
                                "The application cannot verify the license without hardware information.",
                                "Hardware ID Retrieval Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return string.Empty;
            }
        }

        /// <summary>
        /// Überprüft die Hardware-ID des aktuellen Computers gegen eine übergebene HWID.
        /// Diese Methode ist nun unabhängig vom Laden der DLL.
        /// </summary>
        /// <param name="extractedHwidFromDll">Die aus der Lizenz-DLL gelesene HWID.</param>
        /// <returns>True, wenn die HWID übereinstimmt, sonst False.</returns>
        private static bool VerifyHardwareIdAgainstExtracted(string extractedHwidFromDll)
        {
            string currentMachineHwid = GetHardwareId();

            if (string.IsNullOrEmpty(currentMachineHwid))
            {
                return false; // Fehler wurde bereits von GetHardwareId() behandelt
            }

            // --- OPTIONALER DEBUG-CODE ---
            // Dieser Block zeigt eine Debug-MessageBox mit den verglichenen HWIDs an.
            // ENTFERNEN SIE DIESEN BLOCK IM PRODUKTIONS-BUILD!
            // MessageBox.Show(
            //     $"HWID check details (DLL-based):\n\n" +
            //     $"HWID from DLL: '{extractedHwidFromDll}' (Length: {extractedHwidFromDll.Length})\n" +
            //     $"HWID of this computer: '{currentMachineHwid}' (Length: {currentMachineHwid.Length})\n\n" +
            //     $"Match: {extractedHwidFromDll.Equals(currentMachineHwid, StringComparison.OrdinalIgnoreCase)}",
            //     "HWID Verification Debug", MessageBoxButtons.OK, MessageBoxIcon.Information
            // );
            // --- ENDE OPTIONALER DEBUG-CODE ---

            return extractedHwidFromDll.Equals(currentMachineHwid, StringComparison.OrdinalIgnoreCase);
        }

        // --- HILFSMETHODEN FÜR FEHLERMELDUNGEN (UNVERÄNDERT) ---
        private static void ShowMissingLicenseDllError()
        {
            string currentMachineHwid = GetHardwareId();
            string message = $"Access Denied: The license file '{LicenseDllName}' was not found.\n\n" +
                             "Please ensure that the license file is placed in the same directory as the application.\n\n";

            if (!string.IsNullOrEmpty(currentMachineHwid))
            {
                message += "Your current Key is:\n" + currentMachineHwid + "\n\n";
                message += "The Key has been copied to your clipboard.";

                try { Clipboard.SetText(currentMachineHwid); Thread.Sleep(200); }
                catch (Exception ex) { message += "\n\nWarning: Could not copy Key to clipboard: " + ex.Message; }
            }
            else
            {
                message += "Could not determine your Key. Please contact the provider.";
            }

            MessageBox.Show(message, "License Error: DLL Missing", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            Application.Exit();
        }

        private static void ShowLicenseDataClassNotFoundError()
        {
            MessageBox.Show($"Access Denied: The required license class 'BUConverter.License.LicenseData' was not found.\n\n" +
                            "This indicates a corrupted or invalid license. Please contact support.",
                            "License Error: Invalid DLL", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            Application.Exit();
        }

        private static void ShowAuthorizedHwidFieldNotFoundError()
        {
            MessageBox.Show($"Access Denied: The required Key field 'AuthorizedKey' was not found or is invalid within '{LicenseDllName}'.\n\n" +
                           "This indicates a corrupted or invalid license DLL. Please contact support.",
                           "License Error: Invalid DLL Content", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            Application.Exit();
        }

        private static void ShowGeneralLicenseError(Exception ex)
        {
            MessageBox.Show($"Access Denied: An unexpected error occurred while processing the license file '{LicenseDllName}':\n\n" +
                            $"{ex.Message}\n\n" +
                            "This might indicate a corrupted or invalid license DLL. Please contact support.",
                            "License Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            Application.Exit();
        }

        private static void ShowAccessDeniedError()
        {
            string currentMachineHwid = GetHardwareId();
            string message = "Access Denied: Your Key does not match the license.\n\n";

            if (!string.IsNullOrEmpty(currentMachineHwid))
            {
                message += "Your current Key is:\n" + currentMachineHwid + "\n\n";
                message += "This Key has been copied to your clipboard.";
                try { Clipboard.SetText(currentMachineHwid); Thread.Sleep(200); }
                catch (Exception ex) { message += "\n\nWarning: Could not copy Key to clipboard: " + ex.Message; }
            }
            else
            {
                message += "Could not determine your Key. Please contact support.";
            }
            MessageBox.Show(message, "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            Application.Exit();
        }
    }
}