# BarcodeGenerator Documentation

## Table of Contents
- [Overview](#overview)
- [System Requirements](#system-requirements)
- [Installation](#installation)
- [Usage Guide](#usage-guide)
  - [Generating Barcodes](#generating-barcodes)
  - [Managing Templates](#managing-templates)
  - [Working with Results](#working-with-results)
  - [History and Saved Barcodes](#history-and-saved-barcodes)
- [Technical Information](#technical-information)
- [Troubleshooting](#troubleshooting)
- [Version History](#version-history)

## Overview

BarcodeGenerator is a C# Windows desktop application for creating, managing, and printing barcode labels. It supports multiple barcode types, custom label templates, and offers flexible output options including PDF generation and printing capabilities.

## System Requirements

- Windows 7 or higher
- .NET Framework 4.6.1 or higher
- Minimum 4GB RAM recommended
- Screen resolution 1366x768 or higher

## Installation

1. Download the latest release from the GitHub repository
2. Extract the ZIP file to your preferred location
3. Run `BarcodeGenerator.exe` to start the application

## Usage Guide

### Generating Barcodes

1. **Select a Template**: Choose an existing label template from the dropdown or create a new one
2. **Choose Barcode Type**: Select the desired barcode format (EAN-13, CODE 128, QR Code, etc.)
3. **Generate Options**:
   - **Automatic Generation**: Enter the number of unique barcodes to generate
   - **Manual Entry**: Switch to Manual mode and input specific barcode values
4. **Generate**: Click the Generate button to create barcodes based on your settings
5. **Export or Print**: Use the buttons in the result window to save as PDF or print

### Managing Templates

1. **Create Template**:
   - Click the Settings button
   - Select "Add Template"
   - Choose paper size or enter custom dimensions
   - Set margins, number of labels, and columns
   - Enter a template name and click Save
   
2. **Edit Template**:
   - Click the Settings button
   - Select "Edit Template"
   - Choose the template to modify
   - Update settings as needed
   - Click Save to update the template

3. **Delete Template**:
   - Click the Settings button
   - Select "Delete Template"
   - Choose the template to remove
   - Confirm deletion

### Working with Results

In the Results window:
- Use the border checkbox to toggle label borders on/off
- Navigate between pages using arrow buttons
- Save barcodes to PDF using the save button
- Print directly using the print button
- Close the preview using the exit button

### History and Saved Barcodes

1. Click the History button to view previously generated barcode batches
2. Select a history entry to view the individual barcodes
3. Use "Load to Manual" to transfer history barcodes to manual entry

## Technical Information

### Architecture

BarcodeGenerator uses a WPF (Windows Presentation Foundation) architecture with XAML for the UI and C# for the business logic. The application follows a standard Windows desktop application pattern with multiple windows for different functions.

### Key Components

- **MainWindow**: Primary interface for generating barcodes and managing templates
- **ResultWindow**: Displays generated barcodes and provides export options
- **QuestPDF**: Used for PDF generation (Community License)
- **ZXing.Net**: Used for barcode generation and encoding

### Data Storage

- User settings are stored using the built-in .NET Settings mechanism
- Templates are stored as XML in the user settings
- Barcode history is maintained between sessions

### Customization

The application stores all settings locally in the user profile, making it portable and easily configurable. All templates and history are preserved between sessions.

### Third-Party Libraries

- **QuestPDF**: This application uses QuestPDF under the Community License, which allows for free use in open source projects. For more information about QuestPDF licensing, visit [https://www.questpdf.com/license.html](https://www.questpdf.com/license.html)
- **ZXing.Net**: Licensed under Apache License 2.0

## Troubleshooting

### Common Issues

1. **Barcodes not generating correctly**:
   - Verify the input format matches the selected barcode type
   - For EAN codes, ensure correct check digits are used

2. **Template preview not showing**:
   - Check that all dimensions and margins are valid
   - Ensure numerical values are entered for all fields

3. **PDF export fails**:
   - Verify you have write permissions to the selected save location
   - Check that no other application is using the output file

### Application Reset

If you encounter persistent issues, you can reset the application:

1. Open MainWindow.xaml.cs
2. Find the reset code block at the beginning of MainWindow constructor
3. Uncomment the reset code lines
4. Run the application once to reset all settings
5. Re-comment the code block to prevent accidental resets
6. Restart the application

## Version History

- **1.0.0** - Initial Release
  - Basic barcode generation and template management
  - PDF export functionality
  
- **1.1.0** - Current Version
  - Added border visibility toggle
  - Improved multi-page support
  - Enhanced PDF generation quality
  - Various bug fixes and performance improvements

---

*Documentation last updated: 2025-03-10*
