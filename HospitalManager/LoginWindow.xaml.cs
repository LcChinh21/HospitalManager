using System.Windows;
using System.Windows.Input;

namespace HospitalManager
{
    public partial class LoginWindow : Window
    {
        DbHelper db = new DbHelper(); // 1. Khởi tạo DbHelper   
        public LoginWindow()
        {
            InitializeComponent();
        }

        // 1. Hàm cho phép kéo thả cửa sổ (vì đã tắt viền mặc định)
        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        // 2. Xử lý nút Đăng Nhập
        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string u = txtUsername.Text;
            string p = txtPassword.Password;

            // Kiểm tra đăng nhập
            User user = db.CheckLogin(u, p);

            if (user != null)
            {
                // Đăng nhập thành công!

                // --- QUAN TRỌNG: Truyền 'user' vào MainWindow ---
                MainWindow main = new MainWindow(user);

                main.Show();
                this.Close();
            }
            else
            {
                MessageBox.Show("Sai tên đăng nhập hoặc mật khẩu!", "Lỗi đăng nhập", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 3. Xử lý nút Thoát
        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}