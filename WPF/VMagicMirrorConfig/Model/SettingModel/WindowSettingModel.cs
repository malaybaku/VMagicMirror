using Microsoft.Win32;
using System;
using System.IO;

namespace Baku.VMagicMirrorConfig
{
    class WindowSettingModel : SettingModelBase<WindowSetting>
    {
        public WindowSettingModel() : this(ModelResolver.Instance.Resolve<IMessageSender>())
        {
        }

        public WindowSettingModel(IMessageSender sender) : base(sender)
        {
            var setting = WindowSetting.Default;
            var factory = MessageFactory.Instance;

            R = new RProperty<int>(setting.R, _ => SendBackgroundColor());
            G = new RProperty<int>(setting.G, _ => SendBackgroundColor());
            B = new RProperty<int>(setting.B, _ => SendBackgroundColor());

            IsTransparent = new RProperty<bool>(setting.IsTransparent, b =>
            {
                //透明 = ウィンドウフレーム不要。逆も然り
                //NOTE: 後方互換性の都合で必ず背景色の変更の前にフレームの有無を変える。
                //逆にするとウィンドウサイズを維持するための処理が正しく走らない。
                SendMessage(factory.WindowFrameVisibility(!b));

                //ここで透明or不透明の背景を送りつけるとUnity側がよろしく背景透過にしてくれる
                SendBackgroundColor();


                if (b)
                {
                    //背景透過になった時点でクリックスルーしてほしそうなフラグが立ってる => 実際にやる
                    if (WindowDraggable?.Value == false)
                    {
                        SendMessage(factory.IgnoreMouse(true));
                    }
                }
                else
                {
                    //背景透過でない=クリックスルーできなくする
                    SendMessage(factory.IgnoreMouse(true));
                }
            });

            WindowDraggable = new RProperty<bool>(setting.WindowDraggable, b =>
            {
                SendMessage(factory.WindowDraggable(b));
                //すでにウィンドウが透明ならばクリックスルーもついでにやる。不透明の場合、絶対にクリックスルーにはしない
                if (IsTransparent.Value)
                {
                    //ドラッグできない = クリックスルー、なのでフラグが反転することに注意
                    SendMessage(factory.IgnoreMouse(!b));
                }
            });

            TopMost = new RProperty<bool>(setting.TopMost, b => SendMessage(factory.TopMost(b)));

            BackgroundImagePath = new RProperty<string>(
                setting.BackgroundImagePath,
                s => SendMessage(factory.SetBackgroundImagePath(s))
                );

            WholeWindowTransparencyLevel = new RProperty<int>(
                setting.WholeWindowTransparencyLevel,
                i => SendMessage(factory.SetWholeWindowTransparencyLevel(i))
                );

            AlphaValueOnTransparent = new RProperty<int>(
                setting.AlphaValueOnTransparent,
                i => SendMessage(factory.SetAlphaValueOnTransparent(i))
                );
        }

        public RProperty<int> R { get; }
        public RProperty<int> G { get; }
        public RProperty<int> B { get; }

        public RProperty<bool> IsTransparent { get; }
        public RProperty<bool> WindowDraggable { get; }
        public RProperty<bool> TopMost { get; }

        public RProperty<string> BackgroundImagePath { get; }

        public RProperty<int> WholeWindowTransparencyLevel { get; }
        public RProperty<int> AlphaValueOnTransparent { get; }


        #region Reset API

        public void ResetBackgroundColor()
        {
            var setting = WindowSetting.Default;
            R.Value = setting.R;
            G.Value = setting.G;
            B.Value = setting.B;
        }

        public void ResetOpacity()
        {
            var setting = WindowSetting.Default;
            WholeWindowTransparencyLevel.Value = setting.WholeWindowTransparencyLevel;
            AlphaValueOnTransparent.Value = setting.AlphaValueOnTransparent;
        }

        public override void ResetToDefault()
        {
            var setting = WindowSetting.Default;

            ResetBackgroundColor();

            IsTransparent.Value = setting.IsTransparent;
            WindowDraggable.Value = setting.WindowDraggable;
            TopMost.Value = setting.TopMost;

            BackgroundImagePath.Value = setting.BackgroundImagePath;

            ResetOpacity();
            ResetWindowPosition();
        }

        #endregion

        public void ResetWindowPosition()
        {
            //NOTE: ウィンドウが被ると困るのを踏まえ、すぐ上ではなく右わきに寄せる点にご注目
            var pos = WindowPositionUtil.GetThisWindowRightTopPosition();
            SendMessage(MessageFactory.Instance.MoveWindow(pos.X, pos.Y));
            SendMessage(MessageFactory.Instance.ResetWindowSize());
        }


        /// <summary>
        /// 背景色をリフレッシュします。
        /// </summary>
        private void SendBackgroundColor()
        {
            if (IsTransparent.Value == true)
            {
                SendMessage(MessageFactory.Instance.Chromakey(0, 0, 0, 0));
            }
            else
            {
                SendMessage(MessageFactory.Instance.Chromakey(
                    255, R.Value, G.Value, B.Value
                    ));
            }
        }

        public void SetBackgroundImage()
        {
            //NOTE: 画像形式を絞らないと辛いのでAll Filesとかは無しです。
            var dialog = new OpenFileDialog()
            {
                Title = "Select Background Image",
                Filter = "Image files (*.png;*.jpg)|*.png;*.jpg",
                Multiselect = false,
            };

            if (dialog.ShowDialog() == true && File.Exists(dialog.FileName))
            {
                BackgroundImagePath.Value = Path.GetFullPath(dialog.FileName);
            }
        }
    }
}
