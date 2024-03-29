
# About Localization Contribution

This page writes about how to apply your localization to VMagicMirror.

Author writes this page mainly for the contributor who is motivated to localize this app.

## Requirement and Limitations

Requirement

- Programming background is not needed
- XML understanding will be help to treat some specific characters (like '&') in the file.

Limitations

- You cannot localize What shown in Character Window. VRM loading confirmation UI and VRoid Hub UI only support Japanese and English.


## How to create and test Localization

With VMagicMirror v1.6.2 or later, you can try localization by following steps.

- Create a new folder named `Localization`, at `(Folder of VMagicMirror.exe)/ConfigApp` folder.
- Copy latest version's English localization file from [here](https://raw.githubusercontent.com/malaybaku/VMagicMirror/master/WPF/VMagicMirrorConfig/Resources/English.xaml) (right-click and select 'Save Link as...').
- Change file name to the language, e.g. "German.xaml". This file name will be displayed on the app.
- Put the file to `ConfigApp/Localization` folder.
- Edit the file. See detail in next section.
- Start VMagicMirror.

The app will recognized newly added language option and you can choose it to apply.

When you change some localization texts, please restart VMagicMirror to reload the file changes.


## How to edit localization file

There are some points to be careful when editing localization file, coming from XML file format.

The following sections show example case, from English to Japanese translation.

### Basic Rule

Basic concept is, you only need to edit value inside XML tag.

```
(in English.xaml)
    <sys:String x:Key="TopBar_Home">Home</sys:String>

(in Japanese.xaml)
    <sys:String x:Key="TopBar_Home">ホーム</sys:String>
```

Please leave starting tag (`<sys:String x:Key="...">`) and end tag (`</sys:String>`) as is.

Also, there are some additional rules.

### Multi-lined text

There are some XML tags with `xml:space="preserve"` description. It means the line break in content string is reflected as is.

This option is used for some long instruction text, and in this case you should provide similar volume of text in the localization.

```
(In English.xaml)
   <sys:String x:Key="ExTracker_FaceSwitch_Instruction" xml:space="preserve">Activates seleceted BlendShape by making specific face expression.
Check "Keep LipSync" to make the lipsync continue.</sys:String>

(In Japanese.xaml)
    <sys:String x:Key="ExTracker_FaceSwitch_Instruction" xml:space="preserve">顔をはっきりと特定の表情にすることでアバターの表情を切り替えます。
「リップシンクを続行」をオンにすると、表情を切り替えたままリップシンクを動かせます。</sys:String>
```


### Formatted text

Some messages include placeholder `{0}` or `{1}` etc. in the text.

These placeholders are used by program to set content at runtime.

When replacing these messages, include same placeholder in somewhere of the localized text. 

```
(In English.xaml)
    <sys:String x:Key="DialogMessage_DeleteWtmItem">Are you sure to delete this item '{0}'?</sys:String>

(In Japanese.xaml)
    <sys:String x:Key="DialogMessage_DeleteWtmItem">このモーション'{0}'を削除しますか？</sys:String>
```


## Apply localization to distributed app

To apply your localization to distributed app, you have some options.

1. Pull Request based
2. Issue based

First way is to create a new pull request on this repository's `develop` branch, to add a new file at "./WPF/VMagicMirrorConfig/Localization/xxx.xaml". You will find `Chinese_Simplified.xaml` on same directory.

Second way is to create a new issue on this repository, with localization file attached and title like `localization proposal to (language name)`.

I recommend first way because it is git based way, and will be reflected on GitHub contributor indication.
