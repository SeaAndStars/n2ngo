using N2NGO.UtilsClass;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace N2NGO.Views.SubPages.Dialogs
{
    public partial class DialogMessage : Page
    {
        private readonly List<Action<object>>? actsRet;
        private static readonly DoubleAnimation animOpIn = new DoubleAnimation { To = 1, Duration = TimeSpan.FromSeconds(0.3), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } };
        private static readonly DoubleAnimation animOpInDe = new DoubleAnimation { To = 1, BeginTime = TimeSpan.FromSeconds(0.2), Duration = TimeSpan.FromSeconds(0.4), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } };
        private static readonly DoubleAnimation animOpOut = new DoubleAnimation { To = 0, Duration = TimeSpan.FromSeconds(0.3), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } };

        private static readonly DoubleAnimation animScIn = new DoubleAnimation { To = 1, Duration = TimeSpan.FromSeconds(0.43), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } };   // 1    0.3
        private static readonly DoubleAnimation animScOut = new DoubleAnimation { To = 1.6, Duration = TimeSpan.FromSeconds(0.75), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } }; // 1.4  0.75

        private Page? messageContent = null;
        public Page? MessageContent
        {
            get => messageContent;

            set
            {
                if (value != null)
                {
                    ContentPresenter.Navigate(value);
                }

                messageContent = value;
            }
        }

        public enum DialogType
        {
            OK,
            YesNo,
            InputOK
        }

        void ButtonClicked()
        {
            IsEnabled = false;
            ((ScaleTransform)ContentGrid.RenderTransform).BeginAnimation(ScaleTransform.ScaleXProperty, animScOut);
            ((ScaleTransform)ContentGrid.RenderTransform).BeginAnimation(ScaleTransform.ScaleYProperty, animScOut);

            TaskCompletionSource<object> animationCompletedTask = new ();
            animOpOut.Completed += (s, _) =>
            {
                animationCompletedTask.SetResult(0);
            };

            BeginAnimation(OpacityProperty, animOpOut);
            Task.Run(() =>
            {
                animationCompletedTask.Task.Wait();

                Dispatcher.Invoke(() =>
                {
                    if (actsRet != null)
                        foreach (var act in actsRet)
                            act(this);
                });
            });
        }

        public DialogMessage(List<Action<object>>? ActsRet = null)
        {
            InitializeComponent();
            ContentBackground.Opacity = 0;
            ContentGrid.Opacity = 0;

            SharedData.UIAnimation.InitButtons(SharedData.FindVisualChildren<Button>((Grid)Content));
            SharedData.UIAnimation.InitCards(SharedData.FindVisualChildren<Label>((Grid)Content));

            actsRet = ActsRet;
        }
        public DialogMessage(string MessageText, string? MessageTitle = null, DialogType type = DialogType.OK, List<Action<object>>? ActsRet = null) : this(ActsRet)
        {
            if (MessageTitle != null)
                if (MessageTitle.StartsWith('@'))
                    this.MessageTitle.SetResourceReference(Label.ContentProperty, MessageTitle.Substring(1));
                else
                    this.MessageTitle.Content = MessageTitle;

            Page? c = null;

            switch (type)
            {
                default:
                    break;

                case DialogType.OK:
                    {
                        var _c = new MessageDialogs.DialogOK();
                        if (MessageText.StartsWith('@'))
                            _c.MessageContentRunner.SetResourceReference(TextBlock.TextProperty, MessageText.Substring(1));
                        else
                            _c.MessageContentRunner.Text = MessageText;

                        SharedData.UIAnimation.InitButton(_c.ButtonOK);
                        _c.ButtonOK.Click += (_, __) =>
                        {
                            ButtonClicked();
                        };
                        c = _c;

                        break;
                    }

                case DialogType.YesNo:
                    {
                        var _c = new MessageDialogs.DialogYesNo();
                        if (MessageText.StartsWith('@'))
                            _c.MessageContentRunner.SetResourceReference(TextBlock.TextProperty, MessageText.Substring(1));
                        else
                            _c.MessageContentRunner.Text = MessageText;

                        SharedData.UIAnimation.InitButtons(new Button[] { _c.ButtonYes, _c.ButtonNo });
                        _c.ButtonYes.Click += (_, __) =>
                        {
                            _c.YesNo = MessageDialogs.DialogYesNo.YesNoE.Yes;
                            ButtonClicked();
                        };
                        _c.ButtonNo.Click += (_, __) =>
                        {
                            _c.YesNo = MessageDialogs.DialogYesNo.YesNoE.No;
                            ButtonClicked();
                        };
                        c = _c;

                        break;
                    }

                case DialogType.InputOK:
                    {
                        var _c = new MessageDialogs.DialogInput();
                        if (MessageText.StartsWith('@'))
                            _c.MessageContentRunner.SetResourceReference(TextBlock.TextProperty, MessageText.Substring(1));
                        else
                            _c.MessageContentRunner.Text = MessageText;

                        SharedData.UIAnimation.InitButtons(new Button[] { _c.ButtonOK });
                        _c.ButtonOK.Click += (_, __) =>
                        {
                            ButtonClicked();
                        };
                        c = _c;

                        break;
                    }
            }

            MessageContent = c;
        }

        private void Page_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            ContentBackground.BeginAnimation(Border.OpacityProperty, animOpIn);
            ContentGrid.BeginAnimation(Grid.OpacityProperty, animOpInDe);
            ((ScaleTransform)ContentGrid.RenderTransform).BeginAnimation(ScaleTransform.ScaleXProperty, animScIn);
            ((ScaleTransform)ContentGrid.RenderTransform).BeginAnimation(ScaleTransform.ScaleYProperty, animScIn);
        }
    }
}
