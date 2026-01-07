using System;
using System.Collections.Generic;

namespace HospitalManager
{
    // 1. Tài khoản đăng nhập (Khớp bảng Users)
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string DisplayName { get; set; }
        public string Role { get; set; }
    }

    // 2. Bệnh nhân (Khớp bảng Patients)
    public class Patient
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Phone { get; set; }
    }

    // 3. Nhóm thuốc (Khớp bảng Categories)
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    // 4. Thuốc / Dịch vụ (Khớp bảng Services)
    public class Service
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Unit { get; set; }
        public int Price { get; set; }
        public string Type { get; set; }
        public string CategoryName { get; set; }
    }

    // 5. Lượt khám (Khớp bảng Visits)
    public class Visit
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public string Status { get; set; } // Waiting, Examined, Paid
        public string Diagnosis { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;

        // Property phụ trợ: Không lưu trực tiếp trong bảng Visits của SQL
        // nhưng dùng để hứng dữ liệu hiển thị lên giao diện (Hóa đơn)
        public List<Prescription> Prescriptions { get; set; } = new List<Prescription>();
    }

    // 6. Chi tiết đơn thuốc (Khớp bảng Prescriptions)
    public class Prescription
    {
        public int Id { get; set; }       // Cần thêm ID của dòng này
        public int VisitId { get; set; }  // Cần thêm ID để biết thuộc đợt khám nào
        public int ServiceId { get; set; }
        public string DrugName { get; set; }
        public int Quantity { get; set; }
        public int Price { get; set; }

        // Chỉ để hiển thị, không lưu xuống DB
        public int Total => Quantity * Price;
    }
}