# Automatic Temp File Cleaner for Visual Studio 2022

### An extension that automatically cleans up temporary files generated by Visual Studio when the IDE gets closed.

![](Images/App.png)

------------------

## 👋 Introduction

Do you knew that if you do use Visual Studio with frequently, and not do a system cleaning frequently, the size of your system's temp directory at the end will grow up by several gigabytes and the number of files inside will be multiplied by hundreds because of Visual Studio's lack of deleting its own temp files?.

Here is the final solution for that.

After you install this extension, every time that you close Visual Studio this extension will keep your system clean by removing temporary files from disk that are generated by Visual studio in `C:\Users\[USERNAME]\AppData\Local\Temp\` and other locations.

Don't worry, this extension is very safe to be used, it will not and cannot remove important files accidentally. In fact it will keep / ignore any temp file that is not related with Visual Studio.

This extension care to delete temp files related to Background Download, Diagnostic Tools, NuGet, VS settings log files, VS telemetry and VSIX package extraction between other kind of VS temp files, including those disgusting files and directories in your system's temp folder having random alphanumeric naming pattern like: ########.###

I hope this finally helps people to say goodbye to the thousands of Visual Studio files that remain in the temporary folder pending for clean.

## 📝 Requirements

- Visual Studio 2022.

## 🤖 Getting Started

Download the latest extension release by clicking [here](https://github.com/ElektroStudios/Automatic-Temp-File-Cleaner-for-Visual-Studio/releases/latest) or [from Visual Studio Market Place](https://marketplace.visualstudio.com/items?itemName=elektroHacker.AutoTempFileCleanerVS2022),
install it, and whenever you close Visual Studio it will silently on background delete all the garbage.

You can check the log file `C:\Users\[USERNAME]\AppData\Local\Temp\AutoTempFileCleaner_VS2022.log` for more details on the last clean operation did by the extension.

## 🔄 Change Log

Explore the complete list of changes, bug fixes, and improvements across different releases by clicking [here](/Docs/CHANGELOG.md).

## ⚠️ Disclaimer:

This Work (the repository and the content provided in) is provided "as is", without warranty of any kind, express or implied, including but not limited to the warranties of merchantability, fitness for a particular purpose and noninfringement. In no event shall the authors or copyright holders be liable for any claim, damages or other liability, whether in an action of contract, tort or otherwise, arising from, out of or in connection with the Work or the use or other dealings in the Work.

## 💪 Contributing

Your contribution is highly appreciated!. If you have any ideas, suggestions, or encounter issues, feel free to open an issue by clicking [here](https://github.com/ElektroStudios/Automatic-Temp-File-Cleaner-for-Visual-Studio/issues/new/choose). 

Your input helps make this Work better for everyone. Thank you for your support! 🚀

## 💰 Beyond Contribution 

This work is distributed for educational purposes and without any profit motive. However, if you find value in my efforts and wish to support and motivate my ongoing work, you may consider contributing financially through the following options:

<br></br>
<p align="center"><img src="/Images/github_circle.png" height=100></p>
<p align="center">__________________</p>
<h3 align="center">Becoming my sponsor on Github:</h3>
<p align="center">You can show me your support by clicking <a href="https://github.com/sponsors/ElektroStudios/">here</a>, <br align="center">contributing any amount you prefer, and unlocking rewards!</br></p>
<br></br>

<p align="center"><img src="/Images/paypal_circle.png" height=100></p>
<p align="center">__________________</p>
<h3 align="center">Making a Paypal Donation:</h3>
<p align="center">You can donate to me any amount you like via Paypal by clicking <a href="https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=E4RQEV6YF5NZY">here</a>.</p>
<br></br>

<p align="center"><img src="/Images/envato_circle.png" height=100></p>
<p align="center">__________________</p>
<h3 align="center">Purchasing software of mine at Envato's Codecanyon marketplace:</h3>
<p align="center">If you are a .NET developer, you may want to explore '<b>DevCase Class Library for .NET</b>', <br align="center">a huge set of APIs that I have on sale. Check out the product by clicking <a href="https://codecanyon.net/item/elektrokit-class-library-for-net/19260282">here</a></br><br align="center"><i>It also contains all piece of reusable code that you can find across the source code of my open source works.</i></p>
<br></br>

<h2 align="center"><u>Your support means the world to me! Thank you for considering it!</u> 👍</h2>
