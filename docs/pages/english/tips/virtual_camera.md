---
layout: page
title: Use Virtual Camera
permalink: /en/tips/virtual_camera/
---

[Japanese](../../tips/virtual_camera)

# Tips: Use Virtual Camera

Virtual Camera Output is the feature from v0.9.9, to use VMagicMirror screen as a web camera output.

This feature easily connect VMagicMirror to online meeting systems, etc.

**NOTE:** There are other ways to capture VMagicMirror screen and use it as web camera output, like [OBS-VirtualCam](https://obsproject.com/forum/resources/obs-virtualcam.539/). If you need more customization than VMagicMirror's raw output, please consider to use them.


#### Setup for First Time Use
{: .doc-sec2 }

If it is first time to use VMagicMirror virtual camera output, please install virtual camera by following steps.

In `Streaming` tab `Virtual Camera Output` menu, click `*How to setup`.

{% include docimg.html file="/images/tips/virtual_camera_first_setup.png" %}

Then you will see the installation instruction dialog so `Open Folder` to open the folder, and double-click `Install.bat` to execute installation.

{% include docimg.html file="/images/tips/virtual_camera_run_bat.png" %}

After the installation, you will see the info dialog about `DllRegisterServer` operation was successful.

{% include docimg.html file="/images/tips/virtual_camera_success_dialog.png" %}

Close this dialog, and also close the installation instruction dialog to complete setup.


#### Use Virtual Camera in Application
{: .doc-sec2 }


In `Streaming` tab `Virtual Camera Output`, turn on `Enable Camera Output`.

Then open the target application, and select `Unity Video Capture` as web camera.

Following image is an example in Zoom.

{% include docimg.html file="/images/tips/virtual_camera_camera_select_example.png" %}

If `Unity Video Capture` does not appear in the list, please turn off the target app (Zoom, in the above example) and restart.

If the camera still does not appear, try to restart VMagicMirror, Windows, and check if the virtual camera output works in other software.

**NOTE:** Virtual Camera is not supported in some of the application, though they supports real web cameras.

If you could select the camera but there is no image, then confirm the size is set to `640` x `480`. Also, if target application has option to set camera resolution, specify the size to `640` x `480`.

When the image is stretched, `Resise` button can adjust VMagicMirror window size to correct image aspect ratio.


#### Limitation about Resolution

The virtual camera in VMagicMirror has fixed resolution of `640` x `480`.

When you specify other resolution, the target application might fails to receive the image. In this case, please reset the resolution to `640` x ` 480`.
