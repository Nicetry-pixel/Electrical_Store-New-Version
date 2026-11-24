using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.IO;

namespace Electrical_Store
{
    public partial class ProductForm : Form
    {
        private int currentPage = 1;
        private int pageSize = 10;
        private int totalRecords = 0;
        private string searchTerm = "";
        private bool _canEdit;

        private int _selectedProductId = -1;
        private Panel _selectedCard = null;

        public ProductForm(bool canEdit = true)
        {
            InitializeComponent();
            _canEdit = canEdit;

            this.txtSearch.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    btnSearch_Click(s, e);
                }
            };

            this.cmbSort.SelectedIndexChanged += new EventHandler(this.cmbSort_SelectedIndexChanged);

            this.btnSearch.Click += new EventHandler(this.btnSearch_Click);

            if (!_canEdit)
            {
                if (button1 != null) button1.Visible = false; 
                if (button2 != null) button2.Visible = false; 
            }
            else 
            {

                if (button1 != null) button1.Visible = true;
                if (button2 != null) button2.Visible = true;
            }

            if (Session.CurrentUserRoleId == 1)
            {
                if (button5 != null) button5.Visible = true; 
            }

            if (Session.CurrentUserRoleId == 4)
            {
                if (button5 != null) button5.Visible = true;
            }

            LoadProducts();
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            searchTerm = txtSearch.Text.Trim();
            currentPage = 1;
            LoadProducts();
        }

        private void LoadProducts()
        {
            try
            {
                // 1. Поиск
                string whereClause = string.IsNullOrEmpty(searchTerm)
                    ? " "
                    : $"WHERE p.name LIKE '%{searchTerm}%' OR p.sku LIKE '%{searchTerm}%'";

                // 2. Сортировка (определяем по индексу выбранного пункта в ComboBox)
                string orderBy = "ORDER BY p.name ASC"; // По умолчанию (по алфавиту)
                try
                {
                    if (cmbSort.SelectedIndex == 1) orderBy = "ORDER BY p.name DESC"; // Сначала старые/Я-А
                    if (cmbSort.SelectedIndex == 2) orderBy = "ORDER BY p.retail_price DESC"; // Сумма по убыванию
                    if (cmbSort.SelectedIndex == 3) orderBy = "ORDER BY p.retail_price ASC"; // Сумма по возрастанию
                }
                catch { }

                // 3. Подсчет общего количества
                DataTable dtCount = DbHelper.ExecuteQuery($"SELECT COUNT(*) FROM products p {whereClause}");
                totalRecords = Convert.ToInt32(dtCount.Rows[0][0]);

                // 4. Загрузка данных
                int offset = (currentPage - 1) * pageSize;
                string query = $@"SELECT p.id, p.sku, p.name, c.name as category, p.brand, p.retail_price, p.warranty_months, p.image_path 
                          FROM products p 
                          LEFT JOIN categories c ON p.category_id = c.id 
                          {whereClause} 
                          {orderBy} 
                          LIMIT {pageSize} OFFSET {offset}";

                DataTable dt = DbHelper.ExecuteQuery(query);

                // 5. Отрисовка карточек
                flowLayoutPanel1.Controls.Clear();
                foreach (DataRow row in dt.Rows)
                {
                    CreateProductCard(
                        Convert.ToInt32(row["id"]),
                        row["sku"].ToString(),
                        row["name"].ToString(),
                        row["category"]?.ToString() ?? "Без категории",
                        row["brand"]?.ToString() ?? "Без бренда",
                        Convert.ToDecimal(row["retail_price"]),
                        Convert.ToInt32(row["warranty_months"]),
                        row["image_path"]?.ToString() ?? ""
                    );
                }

                UpdatePageButtons(); // Обновляем текст "Всего записей" и кнопки пагинации
            }
            catch (Exception ex) { MessageBox.Show("Ошибка загрузки: " + ex.Message); }
        }

        // МЕТОД СОЗДАНИЯ ОДНОЙ КАРТОЧКИ
        private void CreateProductCard(int id, string sku, string name, string category, string brand, decimal price, int warranty, string imageName)
        {
            // 1. Создаем панель-карточку
            Panel card = new Panel();
            card.Size = new Size(220, 350); // Размер одной карточки
            card.BackColor = Color.White;
            card.BorderStyle = BorderStyle.FixedSingle;
            card.Margin = new Padding(10); // Отступы между карточками
            card.Cursor = Cursors.Hand;
            card.Tag = id;

            PictureBox pic = new PictureBox();
            pic.Size = new Size(200, 150);
            pic.Location = new Point(10, 10);
            pic.SizeMode = PictureBoxSizeMode.Zoom;

            try
            {
                // Формируем имя ресурса (например, "GEN-5KW-01")
                // Если в БД сохранено с расширением (.jpg), обрезаем его
                string resName = System.IO.Path.GetFileNameWithoutExtension(sku);

                // Ищем ресурс по имени через рефлексию
                object imgObj = Properties.Resources.ResourceManager.GetObject(resName);

                if (imgObj != null)
                {
                    pic.Image = (Image)imgObj;
                }
                else
                {
                    pic.Image = Properties.Resources.path;
                }
            }
            catch
            {
                pic.Image = Properties.Resources.path;
            }
            card.Controls.Add(pic);

            // 3. Цена (Красная, как на Ozon)
            Label lblPrice = new Label();
            lblPrice.Text = $"{price:N0} ₽";
            lblPrice.Font = new Font("Verdana", 14F, FontStyle.Bold);
            lblPrice.ForeColor = Color.Red;
            lblPrice.Location = new Point(10, 170);
            lblPrice.AutoSize = true;
            card.Controls.Add(lblPrice);

            // 4. Название товара
            Label lblName = new Label();
            lblName.Text = name;
            lblName.Font = new Font("Verdana", 9F, FontStyle.Regular);
            lblName.Location = new Point(10, 205);
            lblName.Size = new Size(200, 40);
            card.Controls.Add(lblName);

            // 5. Бренд и Гарантия
            Label lblInfo = new Label();
            lblInfo.Text = $"{brand} | Гарантия: {warranty} мес.";
            lblInfo.Font = new Font("Verdana", 8F, FontStyle.Regular);
            lblInfo.ForeColor = Color.Gray;
            lblInfo.Location = new Point(10, 250);
            lblInfo.AutoSize = true;
            card.Controls.Add(lblInfo);

            EventHandler cardClick = (s, e) =>
            {
                // Сбрасываем выделение у старой карточки
                if (_selectedCard != null) _selectedCard.BackColor = Color.White;

                // Выделяем новую
                _selectedCard = card;
                _selectedCard.BackColor = Color.LightBlue; // Цвет выделения

                // Запоминаем ID
                _selectedProductId = id;
            };

            card.Click += cardClick;
            pic.Click += cardClick;
            lblName.Click += cardClick;
            lblPrice.Click += cardClick;
            lblInfo.Click += cardClick;

            // 6. Кнопка Удаления (Вместо двойного клика, так удобнее)
            if (_canEdit || Session.CurrentUserRoleId == 1) // Админ и Товаровед могут удалять
            {
                Button btnDelete = new Button();
                btnDelete.Text = "Удалить";
                btnDelete.BackColor = Color.Black;
                btnDelete.ForeColor = Color.White;
                btnDelete.FlatStyle = FlatStyle.Flat;
                btnDelete.Location = new Point(10, 280);
                btnDelete.Size = new Size(200, 80);
                btnDelete.Click += (s, e) => {
                    DeleteProduct(id, name); // Вызов метода удаления
                };
                card.Controls.Add(btnDelete);
            }

            // Двойной клик по всей карточке (для открытия редактирования, если есть права)
            if (_canEdit)
            {
                card.DoubleClick += (s, e) => {
                    if (new InsertForm("Product", id).ShowDialog() == DialogResult.OK) LoadProducts();
                };
            }

            // Добавляем карточку на форму
            flowLayoutPanel1.Controls.Add(card);
        }

        // МЕТОД УДАЛЕНИЯ (Вынесен отдельно для кнопки)
        private void DeleteProduct(int id, string productName)
        {
            if (MessageBox.Show($"Вы точно хотите удалить товар:\n{productName}?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                try
                {
                    DbHelper.ExecuteNonQuery($"DELETE FROM products WHERE id = {id}");
                    MessageBox.Show("Товар удален!");
                    LoadProducts(); // Обновляем карточки
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("foreign key") || ex.Message.Contains("Cannot delete"))
                        MessageBox.Show("Невозможно удалить товар! Он присутствует в заказах или на складе.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    else
                        MessageBox.Show("Ошибка при удалении: " + ex.Message, "Ошибка БД", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void UpdatePageButtons()
        {
            int totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);
            if (totalPages == 0) totalPages = 1;

            if (label1 != null) label1.Text = $"Всего записей: {totalRecords} | Стр. {currentPage} из {totalPages}";

            try
            {
                button4.Enabled = currentPage < totalPages; // Вперёд
                button6.Enabled = currentPage > 1;          // Назад
            }
            catch { }
        }

        // Обработчики кнопок
        private void button4_Click(object sender, EventArgs e) { int t = (int)Math.Ceiling((double)totalRecords / pageSize); if (currentPage < t) { currentPage++; LoadProducts(); } }
        private void button6_Click(object sender, EventArgs e) { if (currentPage > 1) { currentPage--; LoadProducts(); } }

        private void buttonAdd_Click(object sender, EventArgs e)
        {
            if (new CreateForm("Product").ShowDialog() == DialogResult.OK) { currentPage = 1; LoadProducts(); }
        }

        private void buttonEdit_Click(object sender, EventArgs e)
        {
            if (_selectedProductId != -1)
            {
                // Открываем форму редактирования для выбранного ID
                if (new InsertForm("Product", _selectedProductId).ShowDialog() == DialogResult.OK)
                {
                    LoadProducts();
                }
            }
            else
            {
                MessageBox.Show("Пожалуйста, кликните левой кнопкой мыши по карточке товара, который хотите изменить.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void buttonExit_Click(object sender, EventArgs e)
        {
            // Проверяем роль из сессии
            if (Session.CurrentUserRoleId == 2) 
            {
                MechendaizerForm merch = new MechendaizerForm();
                merch.Show();
            }
            else if (Session.CurrentUserRoleId == 1 )
            {
                AdminForm admin = new AdminForm();
                admin.Show();
            }
            else if (Session.CurrentUserRoleId == 4)
            {
                BuyerForm buyer = new BuyerForm();
                buyer.Show();
            }

            this.Close();
        }
        private void button5_Click(object sender, EventArgs e) {
            if (Session.CurrentUserRoleId == 2)
            {
                button5.Enabled = false;
            }
            else
            {
                BukketForm bukket = new BukketForm();
                bukket.Show();
            }
        }

        private void cmbSort_SelectedIndexChanged(object sender, EventArgs e)
        {
            currentPage = 1;
            LoadProducts();
        }
    }

}