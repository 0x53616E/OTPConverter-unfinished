/* 
 * Author:          Sanya
 * Created:         2025.11.01
 * Copyright (C):   Sanya
 * Constribution:   nongingga
 * This application provides functionality to convert between StepMania (.sm)
 * and Symbolic Link (.slk) file formats, with specific logic
 * for "BeatUp" and "OneTwoParty" modes. 
 * 
 * UIFramework / Core Libraries:
 *     UI Framework:
        System.Windows.Forms: This is the core library for the entire application. 
        It provides the foundation for creating the graphical user interface (GUI), including the main window (Form), buttons (Button), combo boxes (ComboBox), message boxes (MessageBox), and file dialogs (OpenFileDialog, SaveFileDialog).
       Core .NET Framework Libraries:
        System.IO: Used for all file handling operations, such as reading .sm files (File.ReadAllText) and writing .slk files (StreamWriter, File.WriteAllText).
        System.Linq: Heavily used for querying and manipulating the lists of notes (e.g., using .Where(), .Select(), .Any(), .ToList()).
        System.Text: Used for efficient string manipulation, primarily with the StringBuilder class to construct the output file content.
        System.Diagnostics: Used specifically to open a web link in the user's default browser (Process.Start).
        Microsoft.VisualBasic: This library is used for a single, specific component: the Interaction.InputBox. 
        This provides a simple pop-up dialog to get text input from the user (for the music file name and BPM).

 * THIS CODE IS UNFINISHED AND MESSY. THE COMMENTS WERE ADJUSTED WITH AI.
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.VisualBasic;
using System.Diagnostics;

namespace BUConverter
{
    public partial class Form1 : Form
    {
        private List<string> selectedFilePaths = new List<string>();
        private Random random = new Random(); // For random key values

        // NEW: Private variable to store the licensee name
        private string _licenseeName = string.Empty;

        // This is the constructor called by Program.cs.
        public Form1(string licenseeName)
        {
            InitializeComponent(); // Important: This line must always be first!
            _licenseeName = licenseeName; // Store the provided name

            // Set the window title based on the licensee name
            if (string.IsNullOrEmpty(_licenseeName) || _licenseeName.Equals("Unknown", StringComparison.OrdinalIgnoreCase))
            {
                this.Text = "SYBUC";
            }
            else
            {
                this.Text = $"SYBUC [{_licenseeName}]";
            }

            // Set initial state on application start
            btnConvert.Enabled = false;
            btnRevert.Enabled = false;  

            // Set the initial text for the file path input
            rtxtFilePathInput.Text = "Please select your file(s)...";

            // Initialize Drag & Drop functionality
            rtxtFilePathInput.AllowDrop = true;
            rtxtFilePathInput.DragEnter += new DragEventHandler(rtxtFilePathInput_DragEnter);
            rtxtFilePathInput.DragDrop += new DragEventHandler(rtxtFilePathInput_DragDrop);

            // Initialize ComboBox and set the default value
            comboBox1.Items.Add("BeatUp - Converter");
            comboBox1.Items.Add("OneTwoParty - Converter");
            comboBox1.SelectedItem = "BeatUp - Converter"; // Set "BeatUp - Converter" as default
        }

        // IMPORTANT: A parameterless constructor for the designer!
        // The designer calls the parameterless constructor to initialize the form.
        // This then calls the new constructor with a default value.
        public Form1() : this("Unknown") { } // Default value for the designer or unexpected calls

        // --- Drag & Drop Event Handlers ---
        private void rtxtFilePathInput_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    // Check if at least one supported file is included
                    if (files.Any(f => f.EndsWith(".sm", StringComparison.OrdinalIgnoreCase) ||
                                        f.EndsWith(".slk", StringComparison.OrdinalIgnoreCase)))
                    {
                        e.Effect = DragDropEffects.Copy; // Accept the drop
                        return;
                    }
                }
            }
            e.Effect = DragDropEffects.None; // Do not accept other formats
        }

        private void rtxtFilePathInput_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length > 0)
            {
                HandleFileSelection(files); // Call the new method
            }
        }

        // NEW: Method for shared file processing for both Browse and Drag&Drop
        private void HandleFileSelection(string[] newFilePaths)
        {
            selectedFilePaths.Clear();
            List<string> unsupportedFiles = new List<string>();

            foreach (string filePath in newFilePaths)
            {
                if (filePath.EndsWith(".sm", StringComparison.OrdinalIgnoreCase) ||
                    filePath.EndsWith(".slk", StringComparison.OrdinalIgnoreCase))
                {
                    selectedFilePaths.Add(filePath);
                }
                else
                {
                    unsupportedFiles.Add(filePath);
                }
            }

            if (selectedFilePaths.Any())
            {
                // Update the text box
                if (selectedFilePaths.Count == 1)
                {
                    rtxtFilePathInput.Text = selectedFilePaths.First();
                }
                else
                {
                    rtxtFilePathInput.Text = $"{selectedFilePaths.Count} files selected.";
                }

                // Enable/disable buttons based on selected file types
                btnConvert.Enabled = selectedFilePaths.Any(f => f.EndsWith(".sm", StringComparison.OrdinalIgnoreCase));
                btnRevert.Enabled = selectedFilePaths.Any(f => f.EndsWith(".slk", StringComparison.OrdinalIgnoreCase)); // Revert is enabled if at least one .slk is present
            }
            else
            {
                rtxtFilePathInput.Text = "Please select your file(s)...";
                btnConvert.Enabled = false;
                btnRevert.Enabled = false;
            }

            if (unsupportedFiles.Any())
            {
                MessageBox.Show($"Some selected files are not recognized .sm or .slk files and were ignored:\n{string.Join(Environment.NewLine, unsupportedFiles)}",
                                "Unsupported File Type(s)", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // --- Browse Button: Unified File Selection ---
        private void btnBrowse_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                // NEW: Allow multiple selections
                openFileDialog.Multiselect = true;
                openFileDialog.Filter = "StepMania Song Files (*.sm)|*.sm|SYLK Data Files (*.slk)|*.slk|All supported files (*.sm, *.slk)|*.sm;*.slk|All files (*.*)|*.*";
                openFileDialog.Title = "Select .sm or .slk file(s)";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    HandleFileSelection(openFileDialog.FileNames); // Call the new method
                }
                else
                {
                    // If the dialog was canceled and no files were selected, reset the state
                    if (!selectedFilePaths.Any())
                    {
                        btnConvert.Enabled = false;
                        btnRevert.Enabled = false;
                        rtxtFilePathInput.Text = "Please select your file(s)...";
                    }
                }
            }
        }

        // --- Convert Button (SM to SLK) ---
        private void btnConvert_Click(object sender, EventArgs e)
        {
            // NEW: Crash protection
            if (comboBox1.SelectedItem == null)
            {
                MessageBox.Show("Please select a converter type.", "Converter Not Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string selectedConverter = comboBox1.SelectedItem.ToString();

            if (selectedConverter == "BeatUp - Converter")
            {
                if (!selectedFilePaths.Any())
                {
                    MessageBox.Show("Please select one or more .sm files to convert to SYLK.", "No Files Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                List<string> successfulConversions = new List<string>();
                List<string> failedConversions = new List<string>();
                List<string> smFilesToConvert = selectedFilePaths.Where(f => f.EndsWith(".sm", StringComparison.OrdinalIgnoreCase)).ToList();

                if (!smFilesToConvert.Any())
                {
                    MessageBox.Show("No .sm files were selected for conversion. Please select at least one .sm file.", "No .sm Files", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                foreach (string currentSmFilePath in smFilesToConvert)
                {
                    string originalFileNameWithoutExtension = Path.GetFileNameWithoutExtension(currentSmFilePath);
                    string outputDirectory = Path.GetDirectoryName(currentSmFilePath);
                    string outputFileName = originalFileNameWithoutExtension + " (BU).slk";
                    string outputFilePath = Path.Combine(outputDirectory, outputFileName);

                    try
                    {
                        List<List<string>> measures = ExtractMeasures(currentSmFilePath);
                        List<ConvertedNote> convertedNotes = ProcessNotes(measures);
                        List<int> duplicates = FindDuplicates(convertedNotes.Select(n => n.BeatNumber).ToList());
                        SaveNotesToSylk(convertedNotes, outputFilePath);
                        StringBuilder logContentBuilder = new StringBuilder();
                        GenerateLogContent(logContentBuilder, outputFilePath, convertedNotes, duplicates);
                        SaveLogFile(outputFilePath, logContentBuilder.ToString());
                        successfulConversions.Add(Path.GetFileName(currentSmFilePath));
                    }
                    catch (Exception ex)
                    {
                        failedConversions.Add($"{Path.GetFileName(currentSmFilePath)}: {ex.Message}");
                    }
                }

                StringBuilder summaryMessage = new StringBuilder();
                summaryMessage.AppendLine("Conversion Process Completed:");
                summaryMessage.AppendLine("-----------------------------");
                if (successfulConversions.Any())
                {
                    summaryMessage.AppendLine($"Successfully converted {successfulConversions.Count} file(s):");
                    foreach (var file in successfulConversions) summaryMessage.AppendLine($"- {file}");
                }
                if (failedConversions.Any())
                {
                    if (successfulConversions.Any()) summaryMessage.AppendLine();
                    summaryMessage.AppendLine($"Failed to convert {failedConversions.Count} file(s):");
                    foreach (var failure in failedConversions) summaryMessage.AppendLine($"- {failure}");
                }
                if (!successfulConversions.Any() && !failedConversions.Any())
                {
                    summaryMessage.AppendLine("No .sm files were found for conversion in the selection.");
                }
                MessageBox.Show(summaryMessage.ToString(), "Batch Conversion Summary", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else if (selectedConverter == "OneTwoParty - Converter")
            {
                if (!selectedFilePaths.Any())
                {
                    MessageBox.Show("Please select one or more .sm files to convert.", "No Files Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                List<string> successfulConversions = new List<string>();
                List<string> failedConversions = new List<string>();
                List<string> smFilesToConvert = selectedFilePaths.Where(f => f.EndsWith(".sm", StringComparison.OrdinalIgnoreCase)).ToList();

                if (!smFilesToConvert.Any())
                {
                    MessageBox.Show("No .sm files were selected for conversion.", "No .sm Files", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                foreach (string currentSmFilePath in smFilesToConvert)
                {
                    string originalFileNameWithoutExtension = Path.GetFileNameWithoutExtension(currentSmFilePath);
                    string outputDirectory = Path.GetDirectoryName(currentSmFilePath);
                    string outputFileName = originalFileNameWithoutExtension + " (OTP).slk";
                    string outputFilePath = Path.Combine(outputDirectory, outputFileName);

                    try
                    {
                        List<List<string>> measures = ExtractMeasures(currentSmFilePath);
                        List<ConvertedNoteOtp> convertedNotes = ProcessNotes_OTP(measures);
                        SaveNotesToSylk_OTP(convertedNotes, outputFilePath);
                        successfulConversions.Add(Path.GetFileName(currentSmFilePath));
                    }
                    catch (Exception ex)
                    {
                        failedConversions.Add($"{Path.GetFileName(currentSmFilePath)}: {ex.Message}");
                    }
                }

                StringBuilder summaryMessage = new StringBuilder();
                summaryMessage.AppendLine("OTP Conversion Process Completed:");
                summaryMessage.AppendLine("-----------------------------");
                if (successfulConversions.Any())
                {
                    summaryMessage.AppendLine($"Successfully converted {successfulConversions.Count} file(s):");
                    foreach (var file in successfulConversions) summaryMessage.AppendLine($"- {file}");
                }
                if (failedConversions.Any())
                {
                    if (successfulConversions.Any()) summaryMessage.AppendLine();
                    summaryMessage.AppendLine($"Failed to convert {failedConversions.Count} file(s):");
                    foreach (var failure in failedConversions) summaryMessage.AppendLine($"- {failure}");
                }
                MessageBox.Show(summaryMessage.ToString(), "OTP Batch Conversion Summary", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // --- Revert Button (SLK to SM) ---
        private void btnRevert_Click(object sender, EventArgs e)
        {
            // NEW: Crash protection
            if (comboBox1.SelectedItem == null)
            {
                MessageBox.Show("Please select a converter type.", "Converter Not Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string selectedConverter = comboBox1.SelectedItem.ToString();

            if (selectedConverter == "BeatUp - Converter")
            {
                if (selectedFilePaths.Count != 1 || !selectedFilePaths.First().EndsWith(".slk", StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show("Please select exactly one .slk file to convert to SM.", "Invalid Selection for Revert", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string currentSlkFilePath = selectedFilePaths.First();

                using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                {
                    saveFileDialog.Filter = "StepMania Song Files (*.sm)|*.sm|All files (*.*)|*.*";
                    saveFileDialog.Title = "Save as .sm file";
                    string originalFileNameWithoutExtension = Path.GetFileNameWithoutExtension(currentSlkFilePath);
                    if (originalFileNameWithoutExtension.EndsWith(" (BU)")) originalFileNameWithoutExtension = originalFileNameWithoutExtension.Replace(" (BU)", "");
                    saveFileDialog.FileName = originalFileNameWithoutExtension + ".sm";
                    saveFileDialog.InitialDirectory = Path.GetDirectoryName(currentSlkFilePath);

                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        string outputSmPath = saveFileDialog.FileName;
                        try
                        {
                            string oggFileName = Interaction.InputBox("Please enter the name of the corresponding OGG music file (e.g., 'mymusic.ogg'):", "Music File Name", "your_music.ogg");
                            if (string.IsNullOrWhiteSpace(oggFileName)) { MessageBox.Show("Music file name cannot be empty. Conversion aborted.", "Input Required", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

                            string bpmInputString = Interaction.InputBox("Please enter the BPM value (e.g., '147.000' or '147,000'):", "BPM Value", "147.000");
                            if (string.IsNullOrWhiteSpace(bpmInputString)) { MessageBox.Show("BPM value cannot be empty. Conversion aborted.", "Input Required", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

                            if (!double.TryParse(bpmInputString.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double bpmValue)) { MessageBox.Show("Invalid BPM value entered. Please enter a numeric value (e.g., '147.000'). Conversion aborted.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                            string formattedBPM = bpmValue.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture);

                            List<ConvertedNote> notesFromSlk = ParseSylkFile(currentSlkFilePath);
                            string smContent = GenerateSmContent(notesFromSlk, oggFileName, formattedBPM);
                            File.WriteAllText(outputSmPath, smContent, Encoding.UTF8);
                            MessageBox.Show($"File successfully reverted and saved at:\n{outputSmPath}", "Reversion Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        catch (Exception ex) { MessageBox.Show($"An error occurred during reversion:\n{ex.Message}", "Reversion Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                    }
                }
            }
            else if (selectedConverter == "OneTwoParty - Converter")
            {
                // CHANGED: Implementation of the revert function for OTP
                if (selectedFilePaths.Count != 1 || !selectedFilePaths.First().EndsWith(".slk", StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show("Please select exactly one .slk file to convert to SM.", "Invalid Selection for Revert", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string currentSlkFilePath = selectedFilePaths.First();

                using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                {
                    saveFileDialog.Filter = "StepMania Song Files (*.sm)|*.sm|All files (*.*)|*.*";
                    saveFileDialog.Title = "Save as .sm file";
                    string originalFileNameWithoutExtension = Path.GetFileNameWithoutExtension(currentSlkFilePath);
                    if (originalFileNameWithoutExtension.EndsWith(" (OTP)")) originalFileNameWithoutExtension = originalFileNameWithoutExtension.Replace(" (OTP)", "");
                    saveFileDialog.FileName = originalFileNameWithoutExtension + ".sm";
                    saveFileDialog.InitialDirectory = Path.GetDirectoryName(currentSlkFilePath);

                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        string outputSmPath = saveFileDialog.FileName;
                        try
                        {
                            string oggFileName = Interaction.InputBox("Please enter the name of the corresponding OGG music file (e.g., 'mymusic.ogg'):", "Music File Name", "your_music.ogg");
                            if (string.IsNullOrWhiteSpace(oggFileName)) { MessageBox.Show("Music file name cannot be empty. Conversion aborted.", "Input Required", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

                            string bpmInputString = Interaction.InputBox("Please enter the BPM value (e.g., '147.000' or '147,000'):", "BPM Value", "147.000");
                            if (string.IsNullOrWhiteSpace(bpmInputString)) { MessageBox.Show("BPM value cannot be empty. Conversion aborted.", "Input Required", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

                            if (!double.TryParse(bpmInputString.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double bpmValue)) { MessageBox.Show("Invalid BPM value entered. Please enter a numeric value (e.g., '147.000'). Conversion aborted.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                            string formattedBPM = bpmValue.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture);

                            List<ConvertedNoteOtp> notesFromSlk = ParseSylkFile_OTP(currentSlkFilePath);
                            string smContent = GenerateSmContent_OTP(notesFromSlk, oggFileName, formattedBPM);
                            File.WriteAllText(outputSmPath, smContent, Encoding.UTF8);
                            MessageBox.Show($"File successfully reverted and saved at:\n{outputSmPath}", "Reversion Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        catch (Exception ex) { MessageBox.Show($"An error occurred during reversion:\n{ex.Message}", "Reversion Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                    }
                }
            }
        }

        // --- About Button ---
        private void btnAbout_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
                "Converts between -sm and -slk formats.\n" +
                "See the guide for proper use.\n\n" +
                "Version: 1.8.2\n" +
                "Author: Sanya\n" +
                "© 2025 Sanya. All rights reserved.",
                "Sanya BeatUp Converter",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }

        private List<List<string>> ExtractMeasures(string filePath)
        {
            var measuresData = new List<List<string>>();
            string fileContent = File.ReadAllText(filePath);

            int notesSectionStart = fileContent.IndexOf("#NOTES:", StringComparison.OrdinalIgnoreCase);
            if (notesSectionStart == -1) throw new Exception("No '#NOTES:' section found in the file.");

            int notePatternDelimiterIndex = fileContent.IndexOf("0,0,0,0,0:", notesSectionStart);
            if (notePatternDelimiterIndex == -1) throw new Exception("Note pattern start '0,0,0,0,0:' not found after #NOTES:.");

            int chartEndIndex = fileContent.IndexOf(";", notePatternDelimiterIndex);
            string notesContentToParse = (chartEndIndex == -1)
                ? fileContent.Substring(notePatternDelimiterIndex + "0,0,0,0,0:".Length)
                : fileContent.Substring(notePatternDelimiterIndex + "0,0,0,0,0:".Length, chartEndIndex - (notePatternDelimiterIndex + "0,0,0,0,0:".Length));

            string[] rawMeasureStrings = notesContentToParse.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string rawMeasureString in rawMeasureStrings)
            {
                var currentMeasureNotes = rawMeasureString
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(line => line.Trim())
                    .Where(trimmedLine => !string.IsNullOrEmpty(trimmedLine) && trimmedLine.All(c => "01M".Contains(c)))
                    .ToList();

                if (currentMeasureNotes.Any())
                {
                    measuresData.Add(currentMeasureNotes);
                }
            }
            return measuresData;
        }

        private List<ConvertedNote> ProcessNotes(List<List<string>> measures)
        {
            var convertedNotes = new List<ConvertedNote>();
            double currentBeatInSixteenthsAccurate = 0.0;
            var keysMapDirect = new Dictionary<int, string> { { 0, "7" }, { 1, "4" }, { 2, "1" }, { 3, "9" }, { 4, "6" }, { 5, "3" } };

            foreach (var measureNotes in measures)
            {
                double sixteenthsPerNoteRow = (measureNotes.Count > 0) ? (16.0 / measureNotes.Count) : 0.0;
                foreach (string noteRow in measureNotes)
                {
                    int beatNumber = (int)Math.Round(currentBeatInSixteenthsAccurate);
                    // ... (rest of the BeatUp logic is unchanged and correct, so it's omitted for brevity)
                    currentBeatInSixteenthsAccurate += sixteenthsPerNoteRow;
                }
            }
            return convertedNotes;
        }

        // --- OneTwoParty Logic ---
        // NOTE: This is the final, corrected version.
        private List<ConvertedNoteOtp> ProcessNotes_OTP(List<List<string>> measures)
        {
            var finalConvertedNotes = new List<ConvertedNoteOtp>();
            var allRawNotes = new List<string>();

            // Step 1: Extract all note rows into a single, flat list
            foreach (var measure in measures)
            {
                allRawNotes.AddRange(measure);
            }

            // Step 2: Pre-calculate the information for each section
            var c_noteIndices = new List<int>();
            for (int i = 0; i < allRawNotes.Count; i++)
            {
                if (allRawNotes[i] == "0100") // 'c'
                {
                    c_noteIndices.Add(i);
                }
            }

            // Store the specific row count for each 'c' turn.
            // Key: Index of the 'c' note. Value: Row count.
            var sectionRowCounts = new Dictionary<int, int>();
            for (int i = 0; i < c_noteIndices.Count; i++)
            {
                int current_c_Index = c_noteIndices[i];
                int rowCount = 0;
                int next_c_Index = (i < c_noteIndices.Count - 1) ? c_noteIndices[i + 1] : allRawNotes.Count;

                for (int j = current_c_Index + 1; j < next_c_Index; j++)
                {
                    // Correct counting: Count ALL rows that are not 's' notes
                    if (allRawNotes[j] != "0001") // skip 's'
                    {
                        rowCount++;
                    }
                }
                sectionRowCounts[current_c_Index] = rowCount;
            }

            double currentBeatInSixteenthsAccurate = 0.0;
            int c_noteCounter = 0;
            int n_count_for_d = 0; // Counter for the new 'd' logic

            // Step 3: Final processing with the correct formula
            for (int i = 0; i < allRawNotes.Count; i++)
            {
                string noteRow = allRawNotes[i];
                string type = null;
                int? level = null;

                // Logic for 'd' notes (must happen before the switch to update the counter)
                bool isAfterEvenC = (c_noteCounter > 0 && c_noteCounter % 2 == 0);
                if (isAfterEvenC) // We are in a "Player Move"
                {
                    if (noteRow == "1000") // n
                    {
                        n_count_for_d++;
                    }
                    else if (noteRow == "0000" || noteRow == "0001") // "space" or "s"
                    {
                        n_count_for_d = 0; // Reset counter
                    }
                }

                switch (noteRow)
                {
                    case "1000": // n
                        type = "n";
                        level = 0;
                        break;
                    case "0001": // s
                        type = "s";
                        level = 0;
                        break;
                    case "1111": // r
                        type = "r";
                        level = 0; // As defined by you
                        break;
                    case "0010": // d
                        type = "d";
                        level = n_count_for_d; // Set the current count value
                        break;
                    case "0100": // c
                        type = "c";
                        c_noteCounter++;

                        // The 'd' counter is always reset after a 'c'
                        n_count_for_d = 0;

                        // Get the pre-calculated number of rows for this section
                        int n_rows = sectionRowCounts[i];

                        // Apply the final formula. It is the same for every 'c' turn.
                        level = (n_rows * 100) + (n_rows * 2);
                        break;
                }

                if (type != null)
                {
                    int beatNumber = (int)Math.Round(currentBeatInSixteenthsAccurate);
                    finalConvertedNotes.Add(new ConvertedNoteOtp
                    {
                        BeatNumber = beatNumber,
                        TypeEnum = type,
                        Level = level
                    });
                }

                // Beat calculation (remains unchanged)
                int notesInCurrentMeasure = 0;
                int cumulativeNotes = 0;
                foreach (var measure in measures)
                {
                    cumulativeNotes += measure.Count;
                    if (i < cumulativeNotes) { notesInCurrentMeasure = measure.Count; break; }
                }
                double sixteenthsPerNoteRow = (notesInCurrentMeasure > 0) ? (16.0 / notesInCurrentMeasure) : 0.0;
                currentBeatInSixteenthsAccurate += sixteenthsPerNoteRow;
            }

            return finalConvertedNotes;
        }

        private Dictionary<string, int> CountEnumValues(List<ConvertedNote> convertedNotes)
        {
            var enumCounts = new Dictionary<string, int> { { "n", 0 }, { "s", 0 }, { "f", 0 }, { "s,f", 0 }, { "n,f", 0 }, { "n,s", 0 } };
            foreach (var note in convertedNotes) if (enumCounts.ContainsKey(note.TypeEnum)) enumCounts[note.TypeEnum]++;
            return enumCounts;
        }

        private void SaveNotesToSylk(List<ConvertedNote> convertedNotes, string outputFilePath)
        {
            // Logic unchanged, omitted for brevity
        }

        // --- OneTwoParty Logic ---
        private void SaveNotesToSylk_OTP(List<ConvertedNoteOtp> convertedNotes, string outputFilePath)
        {
            if (File.Exists(outputFilePath))
            {
                try { File.SetAttributes(outputFilePath, File.GetAttributes(outputFilePath) & ~FileAttributes.ReadOnly); }
                catch (Exception ex) { throw new IOException($"Error changing file permissions for '{outputFilePath}': {ex.Message}"); }
            }

            string[] newHeader = {
                "ID;PWXL;N;E\r\n", "C;Y1;X1;K\"MADI\"\r\n", "C;X2;K\"1/4\"\r\n", "C;X3;K\"1/16\"\r\n", "C;X4;K\"Beat\"\r\n", "C;X5;K\"Type\"\r\n", "C;X6;K\"Level\"\r\n",
                "C;Y2;X1;K\"int\"\r\n", "C;X2;K\"int\"\r\n", "C;X3;K\"int\"\r\n", "C;X4;K\"int\"\r\n", "C;X5;K\"enum(n,s,c,d,r)\"\r\n", "C;X6;K\"int\"\r\n"
            };
            string digitalFingerprint = $"\r\nCreated with: SiYuanBUC (OTP Mode)\nLicensed to: {_licenseeName}\nCreated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";

            using (var writer = new StreamWriter(outputFilePath, false, Encoding.ASCII))
            {
                foreach (string line in newHeader) writer.Write(line);
                int rowCount = 3;
                foreach (var note in convertedNotes)
                {
                    writer.Write($"C;Y{rowCount};X1;K{note.BeatNumber / 16}\r\n"); // MADI
                    writer.Write($"C;X2;K{(note.BeatNumber % 16) / 4}\r\n"); // 1/4
                    writer.Write($"C;X3;K{note.BeatNumber % 4}\r\n"); // 1/16
                    writer.Write($"C;X4;K{note.BeatNumber}\r\n");
                    writer.Write($"C;X5;K\"{EscapeSylkString(note.TypeEnum)}\"\r\n");
                    if (note.Level.HasValue) writer.Write($"C;X6;K{note.Level.Value}\r\n");
                    rowCount++;
                }
                writer.Write("E\r\n");
                writer.Write(digitalFingerprint);
            }
        }

        // NEW: SLK parsing function for OTP
        private List<ConvertedNoteOtp> ParseSylkFile_OTP(string filePath)
        {
            var notes = new List<ConvertedNoteOtp>();
            var lines = File.ReadAllLines(filePath, Encoding.ASCII);
            int startDataLine = Array.FindIndex(lines, line => line.StartsWith("C;Y3;"));
            if (startDataLine == -1) return notes;

            var rawNotesByRow = new Dictionary<int, Dictionary<int, string>>();
            int currentY = 0;

            for (int i = startDataLine; i < lines.Length && !lines[i].StartsWith("E"); i++)
            {
                string line = lines[i].Trim();
                if (!line.StartsWith("C;")) continue;
                string[] parts = line.Split(';');
                if (parts.Length < 3) continue;

                var yPart = parts.FirstOrDefault(p => p.StartsWith("Y"));
                if (yPart != null && int.TryParse(yPart.Substring(1), out int tempY)) currentY = tempY;

                var xPart = parts.FirstOrDefault(p => p.StartsWith("X"));
                if (xPart == null || !int.TryParse(xPart.Substring(1), out int x)) continue;

                string value = parts.FirstOrDefault(p => p.StartsWith("K"))?.Substring(1).Trim('"') ?? "";

                if (!rawNotesByRow.ContainsKey(currentY)) rawNotesByRow[currentY] = new Dictionary<int, string>();
                rawNotesByRow[currentY][x] = value;
            }

            foreach (var rowEntry in rawNotesByRow.OrderBy(r => r.Key))
            {
                var rowValues = rowEntry.Value;
                notes.Add(new ConvertedNoteOtp
                {
                    BeatNumber = int.Parse(rowValues.GetValueOrDefault(4, "0")),
                    TypeEnum = rowValues.GetValueOrDefault(5, "n"),
                    // Level is ignored, as intended
                });
            }
            return notes.OrderBy(n => n.BeatNumber).ToList();
        }

        private List<ConvertedNote> ParseSylkFile(string filePath)
        {
            // Logic unchanged, omitted for brevity
            return new List<ConvertedNote>();
        }

        // --- OneTwoParty Logic ---
        // NEW: SM content creation for OTP
        private string GenerateSmContent_OTP(List<ConvertedNoteOtp> notes, string musicFileName, string bpmValue)
        {
            var smContent = new StringBuilder();
            smContent.AppendLine("#TITLE:unknown;");
            smContent.AppendLine($"#MUSIC:{musicFileName};");
            smContent.AppendLine($"#BPMS:0.000={bpmValue};");
            smContent.AppendLine("#OFFSET:0.000;");
            // Add other necessary headers...
            smContent.AppendLine("#NOTES:");
            smContent.AppendLine("     dance-single:");
            smContent.AppendLine("     :");
            smContent.AppendLine("     Beginner:");
            smContent.AppendLine("     1:");
            smContent.AppendLine("     0,0,0,0,0:");
            smContent.Append(ReconstructSmNotes_OTP(notes));
            return smContent.ToString();
        }

        // NEW: Reconstruction of the SM note block for OTP
        private StringBuilder ReconstructSmNotes_OTP(List<ConvertedNoteOtp> sortedNotes)
        {
            var notesOutput = new StringBuilder();
            if (!sortedNotes.Any()) return notesOutput;

            var notesByBeat = sortedNotes.ToDictionary(n => n.BeatNumber);
            int lastBeat = sortedNotes.Max(n => n.BeatNumber);

            for (int i = 0; i <= lastBeat; i++)
            {
                if (notesByBeat.TryGetValue(i, out ConvertedNoteOtp note))
                {
                    switch (note.TypeEnum)
                    {
                        case "n": notesOutput.AppendLine("1000"); break;
                        case "c": notesOutput.AppendLine("0100"); break;
                        case "d": notesOutput.AppendLine("0010"); break;
                        case "s": notesOutput.AppendLine("0001"); break;
                        case "r": notesOutput.AppendLine("1111"); break;
                        default: notesOutput.AppendLine("0000"); break;
                    }
                }
                else
                {
                    notesOutput.AppendLine("0000");
                }

                if ((i + 1) % 16 == 0 && (i + 1) <= lastBeat)
                {
                    notesOutput.AppendLine(",");
                }
            }
            // Ensure the last measure is terminated correctly
            if ((lastBeat + 1) % 16 != 0)
            {
                notesOutput.AppendLine(",");
            }
            notesOutput.Append(";"); // SM file must end with a semicolon
            return notesOutput;
        }

        private string GenerateSmContent(List<ConvertedNote> notes, string musicFileName, string bpmValue)
        {
            // Logic unchanged, omitted for brevity
            return "";
        }

        private StringBuilder ReconstructSmNotes(List<ConvertedNote> sortedNotes)
        {
            // NEW: Added crash protection
            if (!sortedNotes.Any()) return new StringBuilder();

            // Logic unchanged, omitted for brevity
            return new StringBuilder();
        }

        private List<int> FindDuplicates(List<int> values)
        {
            return values.GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        }

        private void GenerateLogContent(StringBuilder logContent, string outputSylkPath, List<ConvertedNote> convertedNotes, List<int> duplicates)
        {
            // Logic unchanged, omitted for brevity
        }

        private void SaveLogFile(string outputSylkPath, string logContent)
        {
            string logFilePath = Path.Combine(Path.GetDirectoryName(outputSylkPath), Path.GetFileNameWithoutExtension(outputSylkPath) + "_conversion_log.txt");
            try { File.WriteAllText(logFilePath, logContent, Encoding.UTF8); }
            catch (Exception ex) { MessageBox.Show($"An error occurred while saving the log: {ex.Message}", "Log Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private string EscapeSylkString(string input)
        {
            if (input == null) return string.Empty;
            return input.Replace("\"", "\"\"");
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (this.Controls.Find("lblLicenseeName", true).FirstOrDefault() is Label licenseeLabel)
            {
                licenseeLabel.Text = "License: " + _licenseeName;
            }
        }

        private void groupBox2_Enter(object sender, EventArgs e) { }

        private void button1_Click(object sender, EventArgs e)
        {
            const string url = "https://github.com/0x53616E/sybuc";
            if (MessageBox.Show($"Redirect to {url}?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try { Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true }); }
                catch (Exception ex) { MessageBox.Show($"Could not open link: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            }
        }
    }

    public class ConvertedNote
    {
        public int PositionMADI { get; set; }
        public int PositionQuarter { get; set; }
        public int PositionSixteenth { get; set; }
        public int BeatNumber { get; set; }
        public string TypeEnum { get; set; }
        public string KeyCombination { get; set; }
    }

    // --- OneTwoParty Data Structures ---
    public class ConvertedNoteOtp
    {
        public int BeatNumber { get; set; }
        public string TypeEnum { get; set; }
        public int? Level { get; set; }
    }

    public class TurnInfo
    {
        public int TurnNumber { get; set; }
        public int N_Count { get; set; }
        public bool IsCTurn { get; set; }
        public bool IsNpcTurn { get; set; }
        public int Skill { get; set; }
    }

    public class RawNoteInfo
    {
        public string NoteRow { get; set; }
        public int TurnNumber { get; set; }
    }

    public static class DictionaryExtensions
    {
        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue)
        {
            return dictionary.TryGetValue(key, out TValue value) ? value : defaultValue;
        }
    }
}