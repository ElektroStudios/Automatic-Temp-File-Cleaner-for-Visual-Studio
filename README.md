# Automatic Temp File Cleaner for Visual Studio 2019 and 2022

### An extension that automatically cleans up temporary files generated by Visual Studio when the IDE is closed.

![](Images/App.png)

------------------

## 👋 Introduction

Do you knew that if you do use Visual Studio with frequently, and not do a system cleaning frequently, the size of your system's temp directory at the end will grow up by several gigabytes and the number of files inside will be multiplied by hundreds because of Visual Studio's lack of deleting its own temp files?.

Here is the final solution for that.

After you install this extension, every time that you close Visual Studio this extension will keep your system clean by removing temporary files from disk that are generated by Visual studio in 'C:\Users\\[USERNAME]\AppData\Local\Temp\' and other locations.

Don't worry, this extension is very safe to be used, it will not and cannot remove important files accidentally. In fact it will keep / ignore any temp file that is not related with Visual Studio.

Please be aware that after closing Visual Studio, a log file will be generated in 'C:\Users\\[USERNAME]\AppData\Local\Temp\VsAutomaticTempFileCleaner.log' containing the records for the temp files that had been removed.

This extension care to delete temp files related to Background Download, Diagnostic Tools, NuGet, VS settings log files, VS telemetry and VSIX package extraction between other kind of VS temp files, including those disgusting files and directories in your system's temp folder having random alphanumeric naming pattern like: ########.###

I hope this finally helps people to say goodbye to the thousands of Visual Studio files that remain in the temporary folder pending for clean.

## 📝 Requirements

- Visual Studio 2019 or 2022.

## 🔄 Change Log

Explore the complete list of changes, bug fixes, and improvements across different releases by clicking [here](/Docs/CHANGELOG.md).

## ⚠️ Disclaimer:

This Work (the repository and the content provided in) is provided "as is", without warranty of any kind, express or implied, including but not limited to the warranties of merchantability, fitness for a particular purpose and noninfringement. In no event shall the authors or copyright holders be liable for any claim, damages or other liability, whether in an action of contract, tort or otherwise, arising from, out of or in connection with the Work or the use or other dealings in the Work.

## 💪 Contributing

Your contribution is highly appreciated!. If you have any ideas, suggestions, or encounter issues, feel free to open an issue by clicking [here](https://github.com/ElektroStudios/Automatic-Temp-File-Cleaner-for-Visual-Studio/issues/new/choose). 

Your input helps make this Work better for everyone. Thank you for your support! 🚀

## 💰 Beyond Contribution 

This work is distributed for free and without any profit motive. However, if you find value in my efforts and wish to support and motivate my ongoing work, you may consider contributing financially through the following options:

 - ### Paypal:
    You can donate any amount you like via **Paypal** by clicking on this button:

    [![Donation Account](Images/Paypal_Donate.png)](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=E4RQEV6YF5NZY)

 - ### Envato Market:
   If you are a .NET developer, you may want to explore '**DevCase Class Library for .NET**', a huge set of APIs that I have on sale.
   Almost all reusable code that you can find across my works is condensed, refined and provided through DevCase Class Library.

    Check out the product:
    
   [![DevCase Class Library for .NET](Images/DevCase_Banner.png)](https://codecanyon.net/item/elektrokit-class-library-for-net/19260282)

<u>**Your support means the world to me! Thank you for considering it!**</u> 👍
