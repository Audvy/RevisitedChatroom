using ChatClient.MVVM.ViewModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace InitialChatroom
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }


        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                if (ResizeMode != ResizeMode.CanResize &&
                    ResizeMode != ResizeMode.CanResizeWithGrip)
                {
                    return;
                }

                WindowState = WindowState == WindowState.Maximized
                    ? WindowState.Normal
                    : WindowState.Maximized;
            }
            else
            {
                mRestoreForDragMove = WindowState == WindowState.Maximized;
                DragMove();
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (mRestoreForDragMove)
            {
                mRestoreForDragMove = false;

                var point = PointToScreen(e.MouseDevice.GetPosition(this));

                Left = point.X - (RestoreBounds.Width * 0.5);
                Top = point.Y;

                WindowState = WindowState.Normal;

                DragMove();
            }
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            mRestoreForDragMove = false;
        }

        private bool mRestoreForDragMove;




    }
}