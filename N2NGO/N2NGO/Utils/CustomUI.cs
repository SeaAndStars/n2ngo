using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace N2NGO.Utils;

internal static class CustomUI
{
    public static void Update()
    {
        MouseDownColorAnimation = new() { To = (Color)((ResourceDictionary)Application.Current.Resources["CurrentColorPalette"])["Palette_500"], Duration = TimeSpan.FromMilliseconds(100) };
        MouseUpColorAnimation = new() { To = (Color)((ResourceDictionary)Application.Current.Resources["CurrentColorPalette"])["Palette_300"], Duration = TimeSpan.FromMilliseconds(300) };
        MouseEnterColorAnimation = new() { To = (Color)((ResourceDictionary)Application.Current.Resources["CurrentColorPalette"])["Palette_300"], Duration = TimeSpan.FromMilliseconds(120) };
        MouseLeaveColorAnimation = new() { To = (Color)((ResourceDictionary)Application.Current.Resources["CurrentColorPalette"])["Palette_500"], Duration = TimeSpan.FromMilliseconds(170) };
    }

    public static ColorAnimation MouseDownColorAnimation { get; private set; } = new() { To = (Color)((ResourceDictionary)Application.Current.Resources["CurrentColorPalette"])["Palette_500"], Duration = TimeSpan.FromMilliseconds(100) };
    public static ColorAnimation MouseUpColorAnimation { get; private set; } = new() { To = (Color)((ResourceDictionary)Application.Current.Resources["CurrentColorPalette"])["Palette_300"], Duration = TimeSpan.FromMilliseconds(300) };
    public static ColorAnimation MouseEnterColorAnimation { get; private set; } = new() { To = (Color)((ResourceDictionary)Application.Current.Resources["CurrentColorPalette"])["Palette_200"], Duration = TimeSpan.FromMilliseconds(120) };
    public static ColorAnimation MouseLeaveColorAnimation { get; private set; } = new() { To = (Color)((ResourceDictionary)Application.Current.Resources["CurrentColorPalette"])["Palette_300"], Duration = TimeSpan.FromMilliseconds(170) };

    public static DoubleAnimation SmallerScaleAnimation { get; private set; } = new() { To = 0.97, Duration = TimeSpan.FromSeconds(0.15), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };
    public static DoubleAnimation SmallSmallerScaleAnimation { get; private set; } = new() { To = 0.92, Duration = TimeSpan.FromSeconds(0.15), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };
    public static DoubleAnimation NormalScaleAnimation { get; private set; } = new() { To = 1, Duration = TimeSpan.FromSeconds(0.25), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };
    public static DoubleAnimation BiggerScaleAnimation { get; private set; } = new() { To = 1.03, Duration = TimeSpan.FromSeconds(0.15), EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };


    public static void Button_MouseUp(object sender, MouseEventArgs? e)
    {
        if ((bool)((Control)sender).Resources["UIA_Locked"])
            return;

        ((Control)sender).Background.BeginAnimation(SolidColorBrush.ColorProperty, MouseUpColorAnimation);

        TransformGroup TG = (TransformGroup)((Control)sender).RenderTransform;
        ScaleTransform st = (ScaleTransform)TG.Children[0];

        st.BeginAnimation(ScaleTransform.ScaleXProperty, NormalScaleAnimation);
        st.BeginAnimation(ScaleTransform.ScaleYProperty, NormalScaleAnimation);

        var ue = (UIElement)sender;
        var uers = ue.RenderSize;
        if (e != null)
        {
            var rpos = e.GetPosition(ue);
            var rpx = rpos.X; var rpy = rpos.Y;
            if (!(
                rpx > uers.Width || rpy > uers.Height ||
                rpx < 0 || rpy < 0
                ))
            { Button_MouseEnter(sender, e); }
        }
    }
    public static void Button_MouseDown(object sender, MouseEventArgs? e)
    {
        if ((bool)((Control)sender).Resources["UIA_Locked"])
            return;

        ((Control)sender).Background.BeginAnimation(SolidColorBrush.ColorProperty, MouseDownColorAnimation);

        TransformGroup TG = (TransformGroup)((Control)sender).RenderTransform;
        ScaleTransform st = (ScaleTransform)TG.Children[0];

        if (e != null)
        {
            var p = e.MouseDevice.GetPosition((Control)sender);

            st.CenterX = p.X;
            st.CenterY = p.Y;
        }

        st.BeginAnimation(ScaleTransform.ScaleXProperty, SmallSmallerScaleAnimation);
        st.BeginAnimation(ScaleTransform.ScaleYProperty, SmallSmallerScaleAnimation);
    }
    public static void Button_MouseLeave(object sender, MouseEventArgs? e)
    {
        if ((bool)((Control)sender).Resources["UIA_Locked"])
            return;

        ((Control)sender).Background.BeginAnimation(SolidColorBrush.ColorProperty, MouseLeaveColorAnimation);
    }
    public static void Button_MouseEnter(object sender, MouseEventArgs? e)
    {
        if ((bool)((Control)sender).Resources["UIA_Locked"])
            return;

        ((Control)sender).Background.BeginAnimation(SolidColorBrush.ColorProperty, MouseEnterColorAnimation);
    }
    public static void Button_MouseMove(object sender, MouseEventArgs? e)
    {
        if (e != null)
        {
            var p = e.MouseDevice.GetPosition((Control)sender);

            TransformGroup TG = (TransformGroup)((Control)sender).RenderTransform;
            ScaleTransform st = (ScaleTransform)TG.Children[0];
            // Inverted
            st.CenterX = p.X;
            st.CenterY = p.Y;
        }
    }
    public static void InitButton(Control button)
    {
        try
        {
            button.Resources["UIA_Locked"] = false;
            button.RenderTransform = new TransformGroup { Children = new TransformCollection(new Transform[] { new ScaleTransform(1, 1, 1, 1) }) };
            var color = (SolidColorBrush)button.Background == null ? Colors.Transparent : ((SolidColorBrush)button.Background).Color;
            button.Background = new SolidColorBrush(color); // reinit

            button.MouseEnter -= Button_MouseEnter;
            button.MouseLeave -= Button_MouseLeave;
            button.PreviewMouseDown -= Button_MouseDown;
            button.PreviewMouseUp -= Button_MouseUp;
            button.PreviewMouseMove -= Button_MouseMove;

            button.MouseEnter += Button_MouseEnter;
            button.MouseLeave += Button_MouseLeave;
            button.PreviewMouseDown += Button_MouseDown;
            button.PreviewMouseUp += Button_MouseUp;
            button.PreviewMouseMove += Button_MouseMove;

            Button_MouseLeave(button, null);
        }
        catch (Exception)
        {
            InitCard(button);
        }
    }
    public static void InitButtons(IEnumerable<Button> buttons)
    {
        foreach (var button in buttons)
        {
            InitButton(button);
        }
    }


    public static void Card_MouseUp(object sender, MouseEventArgs? e)
    {
        if ((bool)((Control)sender).Resources["UIA_Locked"])
            return;

        TransformGroup TG = (TransformGroup)((Control)sender).RenderTransform;
        ScaleTransform st = (ScaleTransform)TG.Children[0];

        st.BeginAnimation(ScaleTransform.ScaleXProperty, NormalScaleAnimation);
        st.BeginAnimation(ScaleTransform.ScaleYProperty, NormalScaleAnimation);
    }
    public static void Card_MouseDown(object sender, MouseEventArgs? e)
    {
        if ((bool)((Control)sender).Resources["UIA_Locked"])
            return;

        TransformGroup TG = (TransformGroup)((Control)sender).RenderTransform;
        ScaleTransform st = (ScaleTransform)TG.Children[0];

        if (e != null)
        {
            var p = e.MouseDevice.GetPosition((Control)sender);

            st.CenterX = p.X;
            st.CenterY = p.Y;
        }

        st.BeginAnimation(ScaleTransform.ScaleXProperty, SmallerScaleAnimation);
        st.BeginAnimation(ScaleTransform.ScaleYProperty, SmallerScaleAnimation);
    }
    public static void Card_MouseLeave(object sender, MouseEventArgs? e)
    {
        if ((bool)((Control)sender).Resources["UIA_Locked"])
            return;

        TransformGroup TG = (TransformGroup)((Control)sender).RenderTransform;
        ScaleTransform st = (ScaleTransform)TG.Children[0];

        st.BeginAnimation(ScaleTransform.ScaleXProperty, NormalScaleAnimation);
        st.BeginAnimation(ScaleTransform.ScaleYProperty, NormalScaleAnimation);

        //var anim = MouseLeaveColorAnimation;
        //anim.To = (Color)((Control)sender).Resources["_UIA_Color"];
        //((Control)sender).Background.BeginAnimation(SolidColorBrush.ColorProperty, anim);
    }
    public static void Card_MouseEnter(object sender, MouseEventArgs? e)
    {
        if ((bool)((Control)sender).Resources["UIA_Locked"])
            return;

        TransformGroup TG = (TransformGroup)((Control)sender).RenderTransform;
        ScaleTransform st = (ScaleTransform)TG.Children[0];

        if (e != null)
        {
            var p = e.MouseDevice.GetPosition((Control)sender);

            st.CenterX = p.X;
            st.CenterY = p.Y;
        }

        st.BeginAnimation(ScaleTransform.ScaleXProperty, BiggerScaleAnimation);
        st.BeginAnimation(ScaleTransform.ScaleYProperty, BiggerScaleAnimation);
        //((Control)sender).Background.BeginAnimation(SolidColorBrush.ColorProperty, MouseEnterColorAnimation);
    }
    public static void Card_MouseMove(object sender, MouseEventArgs? e)
    {
        if (e != null)
        {
            var p = e.MouseDevice.GetPosition((Control)sender);

            TransformGroup TG = (TransformGroup)((Control)sender).RenderTransform;
            ScaleTransform st = (ScaleTransform)TG.Children[0];
            st.CenterX = p.X;
            st.CenterY = p.Y;
        }
    }
    public static void InitCard(Control card)
    {
        try
        {
            //InitButton(card);
            card.Resources["UIA_Locked"] = false;
            card.RenderTransform = new TransformGroup { Children = new TransformCollection(new Transform[] { new ScaleTransform(1, 1, .5, .5) }) };
            //var color = (SolidColorBrush)card.Background == null ? Colors.Transparent : ((SolidColorBrush)card.Background).Color;
            //card.Background = new SolidColorBrush(color); // reinit

            //card.Resources["_UIA_Color"] = color;
            card.MouseEnter -= Card_MouseEnter;
            card.MouseLeave -= Card_MouseLeave;
            card.PreviewMouseDown -= Card_MouseDown;
            card.PreviewMouseUp -= Card_MouseUp;
            card.PreviewMouseMove -= Card_MouseMove;

            card.MouseEnter += Card_MouseEnter;
            card.MouseLeave += Card_MouseLeave;
            card.PreviewMouseDown += Card_MouseDown;
            card.PreviewMouseUp += Card_MouseUp;
            card.PreviewMouseMove += Card_MouseMove;

            Card_MouseLeave(card, null);
        }
        catch (Exception)
        { }
    }
    public static void InitCards(IEnumerable<Control> cards)
    {
        foreach (var card in cards)
        {
            InitCard(card);
        }
    }

}
public static class CustomUIHelpers
{
    public static IEnumerable<T> FindVisualChildren<T>(this DependencyObject parent) where T : DependencyObject
    {
        if (parent != null)
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);

                if (child is T typedChild)
                {
                    yield return typedChild;
                }

                foreach (T foundChild in FindVisualChildren<T>(child))
                {
                    yield return foundChild;
                }
            }
        }
    }

    public static void InitializeWithCustomUI(this DependencyObject parent)
    {
        CustomUI.InitButtons(parent.FindVisualChildren<Button>());
        CustomUI.InitCards(parent.FindVisualChildren<Label>());
        CustomUI.InitCards(parent.FindVisualChildren<TextBox>());
        CustomUI.InitCards(parent.FindVisualChildren<CheckBox>());
    }
}
