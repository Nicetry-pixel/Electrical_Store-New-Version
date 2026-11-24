using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace Electrical_Store
{
    public partial class OrderMechendaizerForm : Form
    {
        private int currentPage = 1;
        private int pageSize = 10;
        private int totalRecords = 0;
        private string searchTerm = "";

        public OrderMechendaizerForm()
        {
            InitializeComponent();

            // Настройка таблицы
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView1.MultiSelect = false;
            dataGridView1.ReadOnly = true;
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView1.DefaultCellStyle.Font = new Font("Verdana", 9F, FontStyle.Regular);
            dataGridView1.RowTemplate.Height = 25;

            // Настройка ComboBox для сортировки (если он есть на форме)
            try
            {
                cmbSort.Items.Clear();
                cmbSort.Items.AddRange(new object[] {
                    "Сначала новые",
                    "Сначала старые"
                });
                cmbSort.SelectedIndex = 0;
                cmbSort.SelectedIndexChanged += (s, e) => { currentPage = 1; LoadOrders(); };
            }
            catch { }

            // --- НАСТРОЙКА ПОИСКА И АВТОДОПОЛНЕНИЯ ---
            SetupAutoComplete();

            // Привязка кнопки "Найти"
            if (btnSearch != null)
                btnSearch.Click += new EventHandler(btnSearch_Click);

            // Поиск по нажатию Enter в текстовом поле
            if (txtSearch != null)
            {
                txtSearch.KeyDown += (s, e) => {
                    if (e.KeyCode == Keys.Enter)
                    {
                        e.SuppressKeyPress = true; // Убираем звук "дзынь"
                        btnSearch_Click(s, e);
                    }
                };
            }

            this.dataGridView1.DataBindingComplete += new DataGridViewBindingCompleteEventHandler(this.DataGridView1_DataBindingComplete);

            LoadOrders();
        }

        private void DataGridView1_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            ApplyRowColors();
        }

        // НОВЫЙ МЕТОД: Подгрузка номеров заказов для автодополнения
        private void SetupAutoComplete()
        {
            if (txtSearch == null) return;

            try
            {
                // Берем все существующие номера заказов из БД
                DataTable dt = DbHelper.ExecuteQuery("SELECT order_number FROM orders");

                AutoCompleteStringCollection collection = new AutoCompleteStringCollection();
                foreach (DataRow row in dt.Rows)
                {
                    collection.Add(row["order_number"].ToString());
                }

                // Привязываем подсказки к TextBox
                txtSearch.AutoCompleteCustomSource = collection;
                txtSearch.AutoCompleteMode = AutoCompleteMode.SuggestAppend; // Показывать список и дописывать
                txtSearch.AutoCompleteSource = AutoCompleteSource.CustomSource;
            }
            catch (Exception ex)
            {
                // Если произошла ошибка, просто не включаем автодополнение, чтобы не ломать форму
                Console.WriteLine("Ошибка автодополнения: " + ex.Message);
            }
        }

        // Обработчик нажатия на кнопку "Найти"
        private void btnSearch_Click(object sender, EventArgs e)
        {
            searchTerm = txtSearch.Text.Trim();
            currentPage = 1; // Сбрасываем на первую страницу
            LoadOrders();    // Перезагружаем данные
        }

        private void LoadOrders()
        {
            try
            {
                string whereClause = string.IsNullOrEmpty(searchTerm)
                    ? ""
                    : $"WHERE o.order_number LIKE '%{searchTerm}%' OR o.customer_name LIKE '%{searchTerm}%'";

                string orderBy = "ORDER BY o.created_at DESC";
                try
                {
                    if (cmbSort.SelectedIndex == 1) orderBy = "ORDER BY o.created_at ASC";
                    if (cmbSort.SelectedIndex == 2) orderBy = "ORDER BY o.total_amount DESC";
                    if (cmbSort.SelectedIndex == 3) orderBy = "ORDER BY o.total_amount ASC";
                }
                catch { }

                DataTable dtCount = DbHelper.ExecuteQuery($"SELECT COUNT(*) FROM orders o {whereClause}");
                totalRecords = Convert.ToInt32(dtCount.Rows[0][0]);

                // ВЫВОД КОЛИЧЕСТВА ЗАПИСЕЙ (если вы добавили Label с именем lblCount)
                try { lblCount.Text = $"Всего записей: {totalRecords}"; } catch { }

                int offset = (currentPage - 1) * pageSize;
                string query = $@"SELECT o.id, o.order_number, o.customer_name, o.status, 
                                 o.total_amount, o.created_at
                          FROM orders o 
                          {whereClause}
                          {orderBy}
                          LIMIT {pageSize} OFFSET {offset}";

                DataTable dt = DbHelper.ExecuteQuery(query);

                if (dt != null)
                {
                    dataGridView1.DataSource = dt;

                    if (dataGridView1.Columns["id"] != null) dataGridView1.Columns["id"].Visible = false;
                    if (dataGridView1.Columns["order_number"] != null) dataGridView1.Columns["order_number"].HeaderText = "Номер заказа";
                    if (dataGridView1.Columns["customer_name"] != null) dataGridView1.Columns["customer_name"].HeaderText = "Клиент";
                    if (dataGridView1.Columns["status"] != null) dataGridView1.Columns["status"].HeaderText = "Статус";
                    if (dataGridView1.Columns["total_amount"] != null) dataGridView1.Columns["total_amount"].HeaderText = "Сумма";
                    if (dataGridView1.Columns["created_at"] != null) dataGridView1.Columns["created_at"].HeaderText = "Дата оформления";

                    // Маскировка ФИО
                    dataGridView1.CellFormatting -= DataGridView1_CellFormatting;
                    dataGridView1.CellFormatting += DataGridView1_CellFormatting;

                    // --- УСЛОВНОЕ ФОРМАТИРОВАНИЕ (ЦВЕТА) ---
                   // ApplyRowColors();
                }

                UpdatePageButtons();
            }
            catch (Exception ex) { MessageBox.Show("Ошибка загрузки: " + ex.Message); }
        }

        // НОВЫЙ МЕТОД ДЛЯ ОКРАШИВАНИЯ СТРОК
        private void ApplyRowColors()
        {
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.Cells["status"].Value == null) continue;

                string status = row.Cells["status"].Value.ToString();

                switch (status)
                {
                    case "new":
                    case "processing":
                        row.DefaultCellStyle.BackColor = Color.LightGreen; // Зелёный (Активный)
                        break;
                    case "shipped":
                        row.DefaultCellStyle.BackColor = Color.Khaki;      // Жёлтый (Задерживается)
                        break;
                    case "cancelled":
                        row.DefaultCellStyle.BackColor = Color.LightCoral; // Красный (Отменён)
                        break;
                    case "delivered":
                        row.DefaultCellStyle.BackColor = Color.White;      // Белый (Завершён)
                        break;
                    default:
                        row.DefaultCellStyle.BackColor = Color.White;
                        break;
                }
            }
        }

        private void UpdatePageButtons()
        {
            int totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);
            if (totalPages == 0) totalPages = 1;

            try
            {
                // Проверьте имена кнопок пагинации в вашем дизайнере!
                // В OrderMechendaizerForm они называются button6 (<) и button4 (>)
                button6.Enabled = currentPage > 1;
                button4.Enabled = currentPage < totalPages;
            }
            catch { }
        }

        private void DataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.ColumnIndex < 0 || e.Value == null || dataGridView1.Columns.Count == 0) return;
            if (dataGridView1.Columns["customer_name"] != null && e.ColumnIndex == dataGridView1.Columns["customer_name"].Index)
                e.Value = Utils.MaskName(e.Value.ToString());
        }

        // Кнопка "Назад" (<)
        private void button1_Click(object sender, EventArgs e)
        {
            if (currentPage > 1) { currentPage--; LoadOrders(); }
        }

        // Кнопка "Вперед" (>)
        private void button2_Click(object sender, EventArgs e)
        {
            int totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);
            if (currentPage < totalPages) { currentPage++; LoadOrders(); }
        }

        // Кнопка "Выход"
        private void button3_Click(object sender, EventArgs e)
        {
            if (Session.CurrentUserRoleId == 1) { new AdminForm().Show(); }
            else if (Session.CurrentUserRoleId == 2) { new MechendaizerForm().Show(); }
            else { new BuyerForm().Show(); }
            this.Close();
        }
    }
}