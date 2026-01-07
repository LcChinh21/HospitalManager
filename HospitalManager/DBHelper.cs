using Microsoft.Data.SqlClient; // Dùng thư viện này cho SQL Server
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace HospitalManager
{
    public class DbHelper
    {
        // CHUỖI KẾT NỐI
        string strCon = @"Server=.;Database=HospitalDB;Integrated Security=True;TrustServerCertificate=True";

        // Hàm mở kết nối
        private SqlConnection GetConnection()
        {
            return new SqlConnection(strCon);
        }

        public DbHelper()
        {
            // Không cần LoadData hay InitFakeData nữa
            // Dữ liệu đã nằm cố định trong SQL Server
        }

        // ==========================================================
        // PHẦN 1: QUẢN LÝ USER & LOGIN
        // ==========================================================
        public User CheckLogin(string username, string password)
        {
            User user = null;
            using (SqlConnection conn = GetConnection())
            {
                conn.Open();
                string sql = "SELECT * FROM Users WHERE Username = @u AND Password = @p";
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@u", username);
                cmd.Parameters.AddWithValue("@p", password);

                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    user = new User
                    {
                        Username = reader["Username"].ToString(),
                        DisplayName = reader["DisplayName"].ToString(),
                        Role = reader["Role"].ToString()
                    };
                }
            }
            return user;
        }

        // ==========================================================
        // PHẦN 2: QUẢN LÝ KHO THUỐC (Categories & Services)
        // ==========================================================
        public List<Category> GetCategories()
        {
            List<Category> list = new List<Category>();
            using (SqlConnection conn = GetConnection())
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("SELECT * FROM Categories", conn);
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new Category { Id = (int)reader["Id"], Name = reader["Name"].ToString() });
                }
            }
            return list;
        }

        public List<Service> GetServices()
        {
            List<Service> list = new List<Service>();
            using (SqlConnection conn = GetConnection())
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("SELECT * FROM Services ORDER BY CategoryName", conn);
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new Service
                    {
                        Id = (int)reader["Id"],
                        Name = reader["Name"].ToString(),
                        Unit = reader["Unit"].ToString(),
                        Price = (int)reader["Price"],
                        CategoryName = reader["CategoryName"].ToString()
                    });
                }
            }
            return list;
        }

        public void AddCategory(string name)
        {
            using (SqlConnection conn = GetConnection())
            {
                conn.Open();
                string sql = "INSERT INTO Categories (Name) VALUES (@name)";
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@name", name);
                cmd.ExecuteNonQuery();
            }
        }

        public void AddService(string name, string unit, int price, string categoryName)
        {
            using (SqlConnection conn = GetConnection())
            {
                conn.Open();
                string sql = "INSERT INTO Services (Name, Unit, Price, Type, CategoryName) VALUES (@n, @u, @p, 'Drug', @c)";
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@n", name);
                cmd.Parameters.AddWithValue("@u", unit);
                cmd.Parameters.AddWithValue("@p", price);
                cmd.Parameters.AddWithValue("@c", categoryName);
                cmd.ExecuteNonQuery();
            }
        }

        // ==========================================================
        // PHẦN 3: NGHIỆP VỤ KHÁM CHỮA BỆNH (ĐÃ CHUYỂN SANG SQL)
        // ==========================================================

        // 1. Thêm bệnh nhân & Tạo lượt khám (Logic phức tạp: Insert 2 bảng liên tiếp)
        public void AddPatientAndVisit(string name, string phone)
        {
            using (SqlConnection conn = GetConnection())
            {
                conn.Open();

                // Bước 1: Thêm bệnh nhân và lấy ID vừa sinh ra (SELECT SCOPE_IDENTITY)
                string sqlPatient = "INSERT INTO Patients (FullName, Phone) VALUES (@name, @phone); SELECT SCOPE_IDENTITY();";
                SqlCommand cmdP = new SqlCommand(sqlPatient, conn);
                cmdP.Parameters.AddWithValue("@name", name);
                cmdP.Parameters.AddWithValue("@phone", phone);

                // Lấy ID bệnh nhân mới
                int newPatientId = Convert.ToInt32(cmdP.ExecuteScalar());

                // Bước 2: Tạo lượt khám cho ID đó
                string sqlVisit = "INSERT INTO Visits (PatientId, Status) VALUES (@pid, 'Waiting')";
                SqlCommand cmdV = new SqlCommand(sqlVisit, conn);
                cmdV.Parameters.AddWithValue("@pid", newPatientId);
                cmdV.ExecuteNonQuery();
            }
        }

        // 2. Kê đơn thuốc
        public void AddPrescription(int visitId, int drugId, int qty)
        {
            // Cần lấy thông tin thuốc để có tên và giá
            int price = 0;
            string drugName = "";

            using (SqlConnection conn = GetConnection())
            {
                conn.Open();
                // Lấy giá và tên thuốc
                SqlCommand cmdGetDrug = new SqlCommand("SELECT Name, Price FROM Services WHERE Id = @id", conn);
                cmdGetDrug.Parameters.AddWithValue("@id", drugId);
                SqlDataReader reader = cmdGetDrug.ExecuteReader();
                if (reader.Read())
                {
                    drugName = reader["Name"].ToString();
                    price = (int)reader["Price"];
                }
                reader.Close(); // Đóng reader để chạy lệnh tiếp theo

                // Insert vào bảng kê đơn
                string sql = "INSERT INTO Prescriptions (VisitId, ServiceId, DrugName, Quantity, Price) VALUES (@vid, @sid, @dname, @qty, @price)";
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@vid", visitId);
                cmd.Parameters.AddWithValue("@sid", drugId);
                cmd.Parameters.AddWithValue("@dname", drugName);
                cmd.Parameters.AddWithValue("@qty", qty);
                cmd.Parameters.AddWithValue("@price", price);
                cmd.ExecuteNonQuery();
            }
        }

        // 3. Hoàn tất khám (Cập nhật chẩn đoán)
        public void UpdateDiagnosis(int visitId, string diagnosis)
        {
            using (SqlConnection conn = GetConnection())
            {
                conn.Open();
                string sql = "UPDATE Visits SET Diagnosis = @diag, Status = 'Examined' WHERE Id = @id";
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@diag", diagnosis);
                cmd.Parameters.AddWithValue("@id", visitId);
                cmd.ExecuteNonQuery();
            }
        }

        // 4. Thanh toán
        public void PayVisit(int visitId)
        {
            using (SqlConnection conn = GetConnection())
            {
                conn.Open();
                string sql = "UPDATE Visits SET Status = 'Paid' WHERE Id = @id";
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@id", visitId);
                cmd.ExecuteNonQuery();
            }
        }
        public List<Patient> GetPatients()
        {
            List<Patient> list = new List<Patient>();
            using (SqlConnection conn = GetConnection())
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("SELECT * FROM Patients ORDER BY Id DESC", conn);
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new Patient
                    {
                        Id = (int)reader["Id"],
                        FullName = reader["FullName"].ToString(),
                        Phone = reader["Phone"].ToString()
                    });
                }
            }
            return list;
        }

        // 2. Lấy danh sách lượt khám kèm tên bệnh nhân (Cho Tab Bác Sĩ & Thu Ngân)
        // Chúng ta tạo một class nhỏ để chứa dữ liệu gộp này
        public class VisitView
        {
            public int Id { get; set; }
            public string FullName { get; set; }
        }

        public List<VisitView> GetVisitsByStatus(string status)
        {
            List<VisitView> list = new List<VisitView>();
            using (SqlConnection conn = GetConnection())
            {
                conn.Open();
                // JOIN bảng Visits và Patients để lấy tên
                string sql = @"SELECT v.Id, p.FullName 
                               FROM Visits v 
                               JOIN Patients p ON v.PatientId = p.Id 
                               WHERE v.Status = @status";

                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@status", status);

                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new VisitView
                    {
                        Id = (int)reader["Id"],
                        FullName = reader["FullName"].ToString()
                    });
                }
            }
            return list;
        }

        // 3. Lấy chi tiết đơn thuốc của 1 lượt khám (Hiển thị cho Bác sĩ & Thu ngân)
        public List<Prescription> GetPrescriptions(int visitId)
        {
            List<Prescription> list = new List<Prescription>();
            using (SqlConnection conn = GetConnection())
            {
                conn.Open();
                string sql = "SELECT * FROM Prescriptions WHERE VisitId = @vid";
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@vid", visitId);

                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new Prescription
                    {
                        Id = (int)reader["Id"],
                        VisitId = (int)reader["VisitId"],
                        ServiceId = (int)reader["ServiceId"],
                        DrugName = reader["DrugName"].ToString(),
                        Quantity = (int)reader["Quantity"],
                        Price = (int)reader["Price"]
                        // Total sẽ tự tính trong Model (Quantity * Price)
                    });
                }
            }
            return list;
        }

        // ... (Giữ nguyên code cũ) ...

        // ==========================================================
        // PHẦN 5: THỐNG KÊ DASHBOARD (MỚI THÊM)
        // ==========================================================

        // Class chứa dữ liệu thống kê
        public class DashboardStats
        {
            public int NewPatientsToday { get; set; }
            public int ExaminedToday { get; set; }
            public long RevenueToday { get; set; }
        }

        public DashboardStats GetDashboardStats()
        {
            var stats = new DashboardStats();
            using (SqlConnection conn = GetConnection())
            {
                conn.Open();

                // 1. Đếm số bệnh nhân (Tổng số, hoặc hôm nay tùy bạn. Ở đây đếm tổng cho xôm)
                SqlCommand cmd1 = new SqlCommand("SELECT COUNT(*) FROM Patients", conn);
                stats.NewPatientsToday = (int)cmd1.ExecuteScalar();

                // 2. Đếm số ca đã khám HÔM NAY (Status = Examined hoặc Paid)
                // Dùng CAST(Date AS DATE) để so sánh chỉ ngày, bỏ qua giờ phút
                string sql2 = @"SELECT COUNT(*) FROM Visits 
                                WHERE (Status = 'Examined' OR Status = 'Paid') 
                                AND CAST(Date AS DATE) = CAST(GETDATE() AS DATE)";
                SqlCommand cmd2 = new SqlCommand(sql2, conn);
                stats.ExaminedToday = (int)cmd2.ExecuteScalar();

                // 3. Tính doanh thu HÔM NAY (Tổng tiền các đơn thuốc đã Paid)
                // JOIN bảng Prescriptions với Visits
                string sql3 = @"SELECT ISNULL(SUM(p.Price * p.Quantity), 0)
                                FROM Prescriptions p
                                JOIN Visits v ON p.VisitId = v.Id
                                WHERE v.Status = 'Paid' 
                                AND CAST(v.Date AS DATE) = CAST(GETDATE() AS DATE)";
                SqlCommand cmd3 = new SqlCommand(sql3, conn);
                object result = cmd3.ExecuteScalar();
                stats.RevenueToday = result != DBNull.Value ? Convert.ToInt64(result) : 0;
            }
            return stats;
        }
    }
}