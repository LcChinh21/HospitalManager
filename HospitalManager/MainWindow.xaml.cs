using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace HospitalManager
{
    public partial class MainWindow : Window
    {
        DbHelper db = new DbHelper();
        User _currentUser; // Biến lưu user đăng nhập

        // Biến lưu trạng thái xử lý
        int currentVisitId = 0;
        int billingVisitId = 0;

        // --- HÀM KHỞI TẠO (CẬP NHẬT USER) ---
        public MainWindow(User user)
        {
            InitializeComponent();
            _currentUser = user;

            // 1. Hiển thị tên User lên Sidebar
            if (_currentUser != null)
            {
                lblCurrentUserName.Text = _currentUser.DisplayName;

                switch (_currentUser.Role)
                {
                    case "Admin": lblCurrentUserRole.Text = "Quản trị viên"; break;
                    case "Doctor": lblCurrentUserRole.Text = "Bác sĩ chuyên khoa"; break;
                    default: lblCurrentUserRole.Text = "Nhân viên y tế"; break;
                }
            }

            // 2. Load dữ liệu
            LoadDashboard();
            LoadReceptionData();
        }

        // --- CÁC HÀM LOAD DỮ LIỆU ---

        void LoadDashboard()
        {
            try
            {
                var stats = db.GetDashboardStats();
                if (lblStatPatient != null) lblStatPatient.Text = $"{stats.NewPatientsToday} người";
                if (lblStatExamined != null) lblStatExamined.Text = $"{stats.ExaminedToday} ca";
                if (lblStatRevenue != null) lblStatRevenue.Text = stats.RevenueToday > 1000000
                    ? $"{(stats.RevenueToday / 1000000.0):0.0} tr"
                    : $"{stats.RevenueToday:N0} đ";
            }
            catch { }
        }

        void LoadReceptionData()
        {
            gridPatients.ItemsSource = null;
            gridPatients.ItemsSource = db.GetPatients();
        }

        void LoadDoctorData()
        {
            lstWaiting.ItemsSource = db.GetVisitsByStatus("Waiting");
            lstWaiting.DisplayMemberPath = "FullName";
            lstWaiting.SelectedValuePath = "Id";
            LoadDrugCombobox("");
        }

        void LoadPharmacyData()
        {
            gridServiceList.ItemsSource = db.GetServices();
            cbCategories.ItemsSource = db.GetCategories();
        }

        void LoadCashierData()
        {
            lstCashier.ItemsSource = db.GetVisitsByStatus("Examined");
            lstCashier.DisplayMemberPath = "FullName";
            lstCashier.SelectedValuePath = "Id";
        }

        // --- SỰ KIỆN MENU ---
        private void Menu_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as RadioButton;
            if (btn?.Tag == null) return;

            int index = int.Parse(btn.Tag.ToString());
            MainTabControl.SelectedIndex = index;

            switch (index)
            {
                case 0: lblPageTitle.Text = "DASHBOARD TỔNG QUAN"; break;
                case 1: lblPageTitle.Text = "LỄ TÂN - ĐĂNG KÝ"; LoadReceptionData(); break;
                case 2: lblPageTitle.Text = "PHÒNG KHÁM BÁC SĨ"; LoadDoctorData(); break;
                case 3: lblPageTitle.Text = "KHO THUỐC"; LoadPharmacyData(); break;
                case 4: lblPageTitle.Text = "THU NGÂN"; LoadCashierData(); break;
            }
            LoadDashboard();
        }

        private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

        // --- CÁC HÀM XỬ LÝ SỰ KIỆN (BỊ THIẾU TRƯỚC ĐÓ) ---

        // 1. Lễ tân thêm bệnh nhân
        private void BtnReception_Add_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtRecName.Text)) return;
            db.AddPatientAndVisit(txtRecName.Text, txtRecPhone.Text);
            MessageBox.Show("Đã thêm bệnh nhân!");
            txtRecName.Clear(); txtRecPhone.Clear();
            LoadReceptionData();
            LoadDashboard();
        }

        // 2. Bác sĩ chọn bệnh nhân từ hàng đợi
        private void LstWaiting_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstWaiting.SelectedValue == null) return;
            currentVisitId = (int)lstWaiting.SelectedValue;

            // Hiển thị tên người đang khám
            dynamic selectedItem = lstWaiting.SelectedItem;
            // Lưu ý: Dùng dynamic hoặc cast về VisitView tùy theo code DbHelper của bạn
            // Nếu lỗi dòng này, hãy thử: var item = lstWaiting.SelectedItem as DbHelper.VisitView; 
            // if(item != null) lblDocCurrentPatient.Text = "Đang khám: " + item.FullName;
            if (selectedItem != null) lblDocCurrentPatient.Text = "Đang khám: " + selectedItem.FullName;

            txtDiagnosis.Clear();
            gridPrescription.ItemsSource = db.GetPrescriptions(currentVisitId);
        }

        // 3. Tìm thuốc
        private void TxtSearchDrug_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadDrugCombobox(txtSearchDrug.Text);
            cbDrugs.IsDropDownOpen = true;
        }

        void LoadDrugCombobox(string keyword)
        {
            var all = db.GetServices().Where(s => s.Type == "Thuốc").ToList();
            if (!string.IsNullOrEmpty(keyword)) all = all.Where(s => s.Name.ToLower().Contains(keyword.ToLower())).ToList();
            cbDrugs.ItemsSource = all;
            cbDrugs.DisplayMemberPath = "Name"; cbDrugs.SelectedValuePath = "Id";
        }

        // 4. Bác sĩ thêm thuốc vào đơn
        private void BtnAddDrug_Click(object sender, RoutedEventArgs e)
        {
            if (currentVisitId == 0 || cbDrugs.SelectedValue == null) return;
            int drugId = (int)cbDrugs.SelectedValue;
            int qty = int.TryParse(txtQty.Text, out int q) ? q : 1;

            db.AddPrescription(currentVisitId, drugId, qty);
            gridPrescription.ItemsSource = db.GetPrescriptions(currentVisitId);
        }

        // 5. Bác sĩ hoàn tất khám
        private void BtnDoctor_Finish_Click(object sender, RoutedEventArgs e)
        {
            if (currentVisitId == 0) return;
            db.UpdateDiagnosis(currentVisitId, txtDiagnosis.Text);
            MessageBox.Show("Hoàn tất khám. Chuyển sang thu ngân.");

            currentVisitId = 0;
            lblDocCurrentPatient.Text = "Chọn bệnh nhân...";
            gridPrescription.ItemsSource = null;
            LoadDoctorData();
            LoadDashboard();
        }

        // 6. Kho thuốc: Thêm nhóm mới
        private void BtnAddCategory_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNewCategory.Text)) return;
            db.AddCategory(txtNewCategory.Text);
            MessageBox.Show("Đã thêm nhóm: " + txtNewCategory.Text);
            txtNewCategory.Clear();
            LoadPharmacyData();
        }

        // 7. Kho thuốc: Nhập thuốc mới
        private void BtnAddService_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtDrugName.Text)) return;
            string cat = cbCategories.SelectedValue?.ToString() ?? "Khác";
            int price = int.TryParse(txtDrugPrice.Text, out int p) ? p : 0;

            db.AddService(txtDrugName.Text, txtDrugUnit.Text, price, cat);
            MessageBox.Show("Đã nhập kho!");
            txtDrugName.Clear(); txtDrugUnit.Clear(); txtDrugPrice.Clear();
            LoadPharmacyData();
        }

        // 8. Thu ngân chọn hóa đơn
        private void LstCashier_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstCashier.SelectedValue == null) return;
            billingVisitId = (int)lstCashier.SelectedValue;

            dynamic item = lstCashier.SelectedItem;
            if (item != null) lblBillInfo.Text = "Hóa đơn: " + item.FullName;

            var list = db.GetPrescriptions(billingVisitId);
            gridBillDetails.ItemsSource = list;

            long total = list.Sum(x => (long)x.Total); // Giả sử class Prescription có thuộc tính Total
            lblBillTotal.Text = $"{total:N0} đ";
        }

        // 9. Thu ngân xác nhận thanh toán
        private void BtnConfirmPay_Click(object sender, RoutedEventArgs e)
        {
            if (billingVisitId == 0) return;
            db.PayVisit(billingVisitId);
            MessageBox.Show("Thanh toán thành công!");

            billingVisitId = 0;
            lblBillInfo.Text = "...";
            lblBillTotal.Text = "0 đ";
            gridBillDetails.ItemsSource = null;
            LoadCashierData();
            LoadDashboard();
        }
    }
}