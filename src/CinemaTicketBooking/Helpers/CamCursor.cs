using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using LottieSharp.WPF;

namespace CinemaTicketBooking.Helpers;

/// <summary>
/// Plays the camera Lottie in place of the pointer, then lets the button click through.
/// The clip draws the camera over about two seconds, then runs on for a few more.
/// The pointer holds for the drawing, then the click opens the page.
/// </summary>
public static class CamCursor
{
    private const int PlayMilliseconds = 2200;

    private static bool _busy;
    private static CamOverlay? _overlay;

    public static void Install()
    {
        EventManager.RegisterClassHandler(
            typeof(ButtonBase),
            UIElement.PreviewMouseLeftButtonDownEvent,
            new MouseButtonEventHandler(OnButtonDown),
            handledEventsToo: true);
    }

    private static void OnButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not ButtonBase button)
            return;

        if (button is CheckBox or RadioButton or RepeatButton)
            return;

        if (!button.IsEnabled)
            return;

        if (_busy)
        {
            e.Handled = true;
            return;
        }

        e.Handled = true;
        button.Dispatcher.BeginInvoke(new Action(() => _ = PlayThenClickAsync(button)));
    }

    private static async Task PlayThenClickAsync(ButtonBase button)
    {
        _busy = true;
        try
        {
            if (TryShow(button))
            {
                Mouse.OverrideCursor = Cursors.None;
                await Task.Delay(PlayMilliseconds);
            }
        }
        catch
        {
            // A broken clip must not trap the click.
        }
        finally
        {
            _overlay?.End();
            Mouse.OverrideCursor = null;
            _busy = false;
        }

        if (button.IsEnabled)
            InvokeClick(button);
    }

    private static bool TryShow(ButtonBase button)
    {
        var file = Path.Combine(AppContext.BaseDirectory, "Assets", "Animations", "cam.json");
        if (!File.Exists(file))
            return false;

        _overlay ??= new CamOverlay(file);
        _overlay.Begin(Window.GetWindow(button));
        return true;
    }

    private static void InvokeClick(ButtonBase button)
    {
        var click = typeof(ButtonBase).GetMethod(
            "OnClick",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
            binder: null,
            Type.EmptyTypes,
            modifiers: null);
        click?.Invoke(button, null);
    }

    private sealed class CamOverlay : Window
    {
        private const int GwlExStyle = -20;
        private const int WsExNoActivate = 0x08000000;
        private const int WsExTransparent = 0x00000020;
        private const int WsExToolWindow = 0x00000080;

        private readonly LottieAnimationView _view;
        private readonly DispatcherTimer _follow;

        public CamOverlay(string file)
        {
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            Topmost = true;
            ShowInTaskbar = false;
            ShowActivated = false;
            Focusable = false;
            ResizeMode = ResizeMode.NoResize;
            Width = 220;
            Height = 220;
            IsHitTestVisible = false;

            _view = new LottieAnimationView
            {
                Width = 220,
                Height = 220,
                AutoPlay = false,
                RepeatCount = -1,
                FileName = file,
                IsHitTestVisible = false
            };
            Content = _view;

            _follow = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            _follow.Tick += (_, _) => Follow();
        }

        public void Begin(Window? owner)
        {
            if (owner is not null && owner != this && Owner != owner)
            {
                if (IsVisible)
                    Hide();
                Owner = owner;
            }

            if (!IsVisible)
                Show();

            _view.PlayAnimation();
            Follow();
            _follow.Start();
        }

        public void End()
        {
            _follow.Stop();
            _view.StopAnimation();
            if (IsVisible)
                Hide();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var handle = new WindowInteropHelper(this).Handle;
            var style = GetWindowLongPtr(handle, GwlExStyle);
            SetWindowLongPtr(handle, GwlExStyle, style | WsExNoActivate | WsExTransparent | WsExToolWindow);
        }

        private void Follow()
        {
            if (!GetCursorPos(out var point))
                return;

            var source = PresentationSource.FromVisual(this);
            if (source?.CompositionTarget is null)
                return;

            var dip = source.CompositionTarget.TransformFromDevice.Transform(new Point(point.X, point.Y));
            Left = dip.X - ActualWidth / 2;
            Top = dip.Y - ActualHeight / 2;
        }

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out NativePoint point);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
        private static extern nint GetWindowLongPtr(nint hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
        private static extern nint SetWindowLongPtr(nint hWnd, int nIndex, nint dwNewLong);

        [StructLayout(LayoutKind.Sequential)]
        private struct NativePoint
        {
            public int X;
            public int Y;
        }
    }
}
