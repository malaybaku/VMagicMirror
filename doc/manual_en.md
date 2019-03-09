
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

(TODO: スクリーンショット)

From config window, choose "VRM読込" button and then select `.vrm` file in your PC.

Then Character window shows meta data including license notation. Confirm the license and click `OK` to proceed actual loading.

Then, the character will appear and he / she will move according to your mouse and keyboard input.


## 2. Adjust Chromakey and Light

Choose "背景色と光" on the config window to open detail.

(TODO: 書く)
(TODO: リアルタイムに変更反映)

`Save (.mvv_light)` button can save the current setting, and saved data is available by click `Load (.mvv_light)`.



## 3. Adjust Character Motion and Camera Position

Choose "動き方/カメラ" on the config window to open detail.

(TODO: 書く)
(TODO: リアルタイムに変更反映)

`Save (.mvv_move)` button can save the current setting, and saved data is available by click `Load (.mvv_move)`.

## 4. Startup Setting

Choose "スタートアップ" on the config window to open detail.

The selected settings are applied from next time the application starts.

* "いま表示しているキャラクター" : The current character
* "いまの背景色と光": The current chromakey color and light
* "いまのカメラ位置、動き方の設定": The current camera position and how the character moves


## 5. Troubleshooting

* When the character does not move even you move the mouse, then please click the character window once.
* If the application crashes soon after start, then please check the directory `VMagicMirror.exe` exists and delete `startup.txt`, and restart `VMagicMirror`.
