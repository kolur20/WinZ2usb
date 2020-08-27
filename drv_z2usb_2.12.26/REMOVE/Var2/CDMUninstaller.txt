ReadMe.txt

== CDMUninstaller v1.4 ==
This readme provides an overview of how to use the command line version of the FTDI CDM Uninstaller.  This application can be used to remove installed FT device drivers from the user's system.  For a graphical version of this application please refer to CDMUninstallerGUI also available from the Utilities section of the FTDI website.

== Supported Operating System ==
	Windows XP
	Windows XP 64bit
	Windows Vista
	Windows Vista 64bit
	Windows 7
	Windows 7 64bit

== Installation ==
The application can be downloaded directly from the FTDI website as a zip file.  This zip folder contains the CDM Uninstaller executable which should be extracted and placed in the desired location on the user's system.

== Running the Application ==

Open the command line and browse to the location on the system where the executable has been extracted to.

The command to run the executable comes in the following format:	
	CDMUninstaller.exe  VendorID  ProductID  [-l]
-l is an optional paramater that will generate a log file giving details of the result of the removal. 

Note: Vendor ID and product ID are both hex values.

== Example Usage ==

	CDMUninstaller.exe 0403 6001 -l

Removing device with Vendor ID of 0403 and Product ID 6001 and auto-generating an uninstall log.