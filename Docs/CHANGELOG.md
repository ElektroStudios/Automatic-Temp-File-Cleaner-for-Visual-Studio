# Automatic Temp File Cleaner Version History 📋

## v1.2 (VS 2022) *(current)* 🆕

#### 🌟 Improvements:
 - Added support for cleaning additional types of temporary files.

#### 🛠️ Fixes:
 - Prevented deletion of temporary .vsix package files. This resolves an issue affecting installation of Visual Studio extensions (VSIX) from the IDE. Thanks to Caslav Pavlovic for reporting the problem.

> [!IMPORTANT]
> The VSIX package for Visual Studio 2019 remains unchanged. It is no longer supported.

## v1.1 🔄

Support for Visual Studio 2019 hass been removed.

#### 🚀 New Features:
 - Added support for installing the extension in Visual Studio 2022.
    
#### 🌟 Improvements:
 - Added additional checks for temp. directory names: "VS", "VSFeedbackIntelliCodeLogs", "VSLogs" and "VsTempFiles".

## v1.0 🔄
Initial Release.