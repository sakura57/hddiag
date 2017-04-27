==============================
  HD Diagnostic Utility 0.2a
        User's Manual
==============================


--------------------
  Summary of Usage  
--------------------

The Help Desk Diagnostic Utility was designed to help diagnose and fix wireless connectivity issues on RPI student laptops.
The utility was designed primarily to help solve problems revolving around rpi_wpa2 and eduroam connectivity, however, it is possible
some of the functionality would be able to help diagnose or fix issues on other networks as well.

The main usefulness of HDDU is its ability to generate reports (helpdesk.txt) which can be e-mailed to your support staff. These
reports contain useful information produced from a variety of Windows command-line utilities, which will assist your support staff
in diagnosing your issue and providing the best possible solution. Simply choose "Generate Text Report" from the main dialog,
and the text report will be saved to your desktop. The text report can then be conveniently attached and sent via e-mail. Keep in mind
however that you may need to transfer the report to another device with Internet capability before sending.

The utility is also capable of automatically diagnosing some common problems with wireless connections. Choose "Automatic Diagnosis"
under "Troubleshooting Options." The utility will collect some information, and alert you if a known problem is detected. Instructions
for a possible fix will be displayed on-screen. If more information is needed, the utility will advise that you generate a report and
e-mail it to your support staff for further diagnosis.

Your support staff may advise that you try certain Troubleshooting Options, or you may wish to try some of these yourself. These
will apply fixes for particular issues. See Description of Troubleshooting Options for more information about each of these commands.


------------------------------------------
  Description Of Troubleshooting Options  
------------------------------------------

The utility offers several options for troubleshooting. These options are all "safe;" none of them will break or damage a properly
configured Windows installation.

* Flush DNS Cache - DNS is required to successfully translate names (such as www.rpi.edu) into IP addresses (128.113.0.2). Your
  computer will sometimes try to cache mappings of domain name to IP address, because accessing a resource on your computer is faster
  than making a request over the network every time the information is needed. However, if the cache becomes corrupt or entries are
  invalid, you may experience problems when using the Internet. Flush DNS Cache option will clear the DNS cache and can resolve
  issues related to the DNS cache.

* Reset DNS Servers - RPI does not permit DNS servers which are not its own. If you have manually set your own DNS servers, or
  a program has altered your DNS server settings, you may be unable to use the Internet at all while on rpi_wpa2 or eduroam.
  You may be advised to choose this option by Automatic Diagnosis. This option will alter your wireless adapter settings so that
  DNS server addresses are automatically obtained.

* Increase rpi_wpa2 Priority - Every wireless network is assigned a priority by Windows. This priority determines which network
  is connected to if multiple preferred networks are available. This option will change rpi_wpa2 to highest priority, and eduroam
  to second-highest priority.

* Manage Network Adapters - Opens the Network Adapters location in Control Panel.