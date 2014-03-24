HandsOff
========

HandsOff is a configurable touchscreen toggle that sits in the notification area (the "tray"). 

On Windows 8 tablets with both a touchscreen and an active digitizer (Wacom or other), only the touchscreen will be disabled. This is especially useful when drawing, where it'll prevent touch from interfering with pen input.

As the togglable input device is configurable, it can also be used for other stuff, like disabling pen input while leaving touch active, for instance.


Usage
=====
Simply left-click or tap on the hand icon in the tray and your touchscreen will be disabled. Clicking again will enable it back.

On your first run, HandsOff will try to automatically detect your touchscreen. If clicking the icon has no effect, right-click the icon then select 'Configuration...'. Under 'Controlled Device:', select the device you think corresponds to your touchscreen, then click 'OK'. Repeat the procedure until HandsOff properly toggles your touchscreen. If you tried all devices with no success, feel free to send me your device's instance path (check below for how to get this) and I'll try to make this work.

This can also be used to toggle other devices, such as mice and keyboards (HandsOff only lists Human Interface Devices).

Right-click the icon and select 'Quit' to quit (mindblowing, I know).


Supported Platforms
===================
 - Windows 8
 - Windows 7 or Vista with .NET Framework 4.0 installed.


Getting your device's instance path
===================================
HandsOff works by simply enabling/disabling a device as you'd do manually through the Device Manager. If you tried all devices in HandsOff but still can't get it to deactivate your touchscreen, you can check if it's still possible to disable it through the Device Manager. If it is, you can send me the device instance path.

To open the Device Manager:

- Hit the Windows key on your keyboard;
- Type ```devmgmt.msc``` then hit Enter

  OR
- Open the Control Panel;
- Click System and Security, then System;
- In the left pane, click Device Manager

From there, find your touchscreen device, right-click it, then select 'Disable' (don't worry, you can easily enable it back later). 

If your touchscreen becomes effectively inactive:
 - Right-click it again;
 - Select 'Properties';
 - Click on the 'Details' tab;
 - Under 'Property', select 'Device instance path';
 - Under 'Value', right-click the string and select 'Copy'

This string contains no private information, it's basically the 'address' of your device on your PC. Send me this value and I'll try to make it work.
