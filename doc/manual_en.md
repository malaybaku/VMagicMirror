
# VMagicMirror Manual

Contents

1. Load Character
2. Adjust Chromakey and Light
3. Adjust Character Motion and Camera Position
4. Startup Setting
5. Troubleshooting

## 1. Load Character

After start the application by double click `VMagicMirror.exe`, two windows will appear.

* Config Window (small)
* Character Window (large)

![Start Image](https://github.com/malaybaku/VMagicMirror/blob/master/doc/pic/started.png)

From config window, choose "VRMロード" button and then select `.vrm` file in your PC.

Then Character window shows meta data including license notation. Confirm the license and click `OK` to proceed actual loading.

Then, the character will appear and he / she will move according to your mouse and keyboard input.


## 2. Adjust Chromakey and Light

Choose "背景色と光" on the config window to open detail.

`R`, `G`, `B` means the background color in RGB.

`光の強さ[%]` means the light intensity in percentage.

`既定値にリセット` will reset the values to default.

`設定を保存` can save the setting to the file, and `設定をロード` can load the saved setting.

## 3. Adjust Layout

Choose "動き方/カメラ" on the config window to open detail.

* Following slider will be helpful when your VRM has strange wrist angle.
    + `手首から指先までの長さ[cm]`: (`Length from wrist to hand tip [cm]`)
    + `手首から手のひらまでの長さ[cm]`: (`Length from wrist to palm [cm]`)

Other UI means:

* `タッチタイピング風に視線を動かす` : Enable touch typing like head motion.
    * When On : Character looks towards mouse cursor
    * When Off : Character looks towards in-screen mouse or keyboard that he / she touches
* `カメラの配置` : Camera position
    * `高さ[cm]` : Height of the camera in centimeter.
    * `キャラまでの距離[cm]` : The distance to the character in centimeter.
    * `カメラの角度[deg]` : The camera tilt angle in degree.
* `キーボード・マウスパッドの配置` : The layout of keyboard and mouse pad
    * `キーボードの高さ[cm]` : Height of the keyboard in centimeter.
    * `キーボードのサイズ[%]` : When the value is small, the keyboard and mouse pad smallen and be close to the character.
* `キーボード・マウスパッドを表示` : Show or hide the keyboard and mouse pad

`既定値にリセット` will reset the values to default.

`設定を保存` can save the setting to the file, and `設定をロード` can load the saved setting.

## 4. Startup Setting

Choose "スタートアップ" on the config window to open detail.

The selected settings are applied from next time the application starts.

* `いま表示しているキャラクター` : The current character
* `いまの背景色と光` : Current chromakey color and light
* `いまのカメラ位置、動き方の設定` : Current layout

## 5. Troubleshooting

* When the character does not move even you move the mouse, then please click the character window once.
* If the application crashes soon after start, then please check the directory `VMagicMirror.exe` exists and open `ConfigApp` directory, then delete following files.
    + `_currentBackground`
    + `_currentLayout`
    + `_currentVrm`
    + `_startup`

