using System;
using System.Data;
using System.Windows.Forms;

namespace Electrical_Store
{
    public partial class CreateForm : Form
    {
        private string _entityType;

        public CreateForm(string entityType)
        {
            InitializeComponent();
            _entityType = entityType;

            // Скрываем все панели по умолчанию
            panelUser.Visible = false;
            panelProduct.Visible = false;
            panelStock.Visible = false;
            panelOrder.Visible = false;

            // Показываем нужную панель и загружаем справочники
            if (_entityType == "User")
            {
                panelUser.Visible = true;
                this.Text = "Добавление пользователя";
                DataTable dtRoles = DbHelper.ExecuteQuery("SELECT id, name FROM roles");
                cmbUserRole.DataSource = dtRoles; cmbUserRole.DisplayMember = "name"; cmbUserRole.ValueMember = "id";
            }
            else if (_entityType == "Product")
            {
                panelProduct.Visible = true;
                this.Text = "Добавление товара";
                DataTable dtCat = DbHelper.ExecuteQuery("SELECT id, name FROM categories");
                cmbProdCategory.DataSource = dtCat; cmbProdCategory.DisplayMember = "name"; cmbProdCategory.ValueMember = "id";
                decimal.TryParse(txtProdPrice.Text, out decimal pPrice);
                decimal.TryParse(txtProdRetail.Text, out decimal rPrice);

                // Получаем имя выбранной картинки
                string imageName = cmbProductImage.SelectedItem != null ? cmbProductImage.SelectedItem.ToString() : "";

                string query = $@"INSERT INTO products (sku, name, category_id, brand, purchase_price, retail_price, image_path) 
                      VALUES ('{txtProdSKU.Text}', '{txtProdName.Text}', {cmbProdCategory.SelectedValue}, '{txtProdBrand.Text}', {pPrice}, {rPrice}, '{imageName}')";
                DbHelper.ExecuteNonQuery(query);
                LoadImagesToCombo();
            }
            else if (_entityType == "Stock")
            {
                panelStock.Visible = true;
                this.Text = "Добавление на склад";
                DataTable dtProd = DbHelper.ExecuteQuery("SELECT id, name FROM products");
                cmbStockProduct.DataSource = dtProd; cmbStockProduct.DisplayMember = "name"; cmbStockProduct.ValueMember = "id";
            }
            else if (_entityType == "Order")
            {
                panelOrder.Visible = true;
                this.Text = "Добавление заказа";
                cmbOrdStatus.Items.Clear(); cmbOrdStatus.Items.AddRange(new object[] { "new", "processing", "shipped", "delivered", "cancelled" }); cmbOrdStatus.SelectedIndex = 0;
            }
        }

        private void LoadImagesToCombo()
        {
            cmbProductImage.Items.Clear();
            // Ищем все .jpg и .png в текущей папке (где лежит .exe)
            string[] files = System.IO.Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.jpg");
            string[] filesPng = System.IO.Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.png");

            foreach (string file in files) cmbProductImage.Items.Add(System.IO.Path.GetFileName(file));
            foreach (string file in filesPng) cmbProductImage.Items.Add(System.IO.Path.GetFileName(file));

            if (cmbProductImage.Items.Count > 0) cmbProductImage.SelectedIndex = 0;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (_entityType == "User")
                {
                    // 1. ВАЛИДАЦИЯ
                    if (string.IsNullOrWhiteSpace(txtUserFIO.Text) || string.IsNullOrWhiteSpace(txtUserLogin.Text))
                    {
                        MessageBox.Show("Поля 'ФИО' и 'Логин' обязательны!", "Ошибка валидации", MessageBoxButtons.OK, MessageBoxIcon.Warning); return;
                    }
                    if (txtUserLogin.Text.Length < 4 || txtUserLogin.Text.Length > 32)
                    {
                        MessageBox.Show("Логин должен быть от 4 до 32 символов!", "Ошибка валидации", MessageBoxButtons.OK, MessageBoxIcon.Warning); return;
                    }
                    if (txtUserPass.Text.Length < 8 || txtUserPass.Text.Length > 64)
                    {
                        MessageBox.Show("Пароль должен быть от 8 до 64 символов!", "Ошибка валидации", MessageBoxButtons.OK, MessageBoxIcon.Warning); return;
                    }
                    if (maskedTextBox1.Text.Contains("_"))
                    {
                        MessageBox.Show("Введите номер телефона полностью!", "Ошибка валидации", MessageBoxButtons.OK, MessageBoxIcon.Warning); return;
                    }
                    if (cmbUserRole.SelectedValue == null)
                    {
                        MessageBox.Show("Выберите роль!", "Ошибка валидации", MessageBoxButtons.OK, MessageBoxIcon.Warning); return;
                    }

                    // 2. СБОР ДАННЫХ И СОХРАНЕНИЕ
                    int isActive = checkBox1.Checked ? 1 : 0;
                    string hireDate = dateTimePicker1.Value.ToString("yyyy-MM-dd");

                    string query = $@"INSERT INTO users (login, password, full_name, email, phone, role_id, hire_date, is_active) 
                              VALUES ('{txtUserLogin.Text}', '{txtUserPass.Text}', '{txtUserFIO.Text}', '{txtUserEmail.Text}', '{maskedTextBox1.Text}', {cmbUserRole.SelectedValue}, '{hireDate}', {isActive})";
                    DbHelper.ExecuteNonQuery(query);
                }
                else if (_entityType == "Product")
                {
                    if (string.IsNullOrWhiteSpace(txtProdSKU.Text) || string.IsNullOrWhiteSpace(txtProdName.Text))
                    {
                        MessageBox.Show("Артикул и Название обязательны!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning); return;
                    }
                    // Проверка на число в цене
                    if (!decimal.TryParse(txtProdPrice.Text, out decimal pPrice) || !decimal.TryParse(txtProdRetail.Text, out decimal rPrice))
                    {
                        MessageBox.Show("Введите корректные числа в цены!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning); return;
                    }

                    string query = $@"INSERT INTO products (sku, name, category_id, brand, purchase_price, retail_price) 
                              VALUES ('{txtProdSKU.Text}', '{txtProdName.Text}', {cmbProdCategory.SelectedValue}, '{txtProdBrand.Text}', {pPrice.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {rPrice.ToString(System.Globalization.CultureInfo.InvariantCulture)})";
                    DbHelper.ExecuteNonQuery(query);
                }

                // 3. УСПЕХ
                MessageBox.Show("Запись успешно добавлена!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                // 4. ОШИБКА БД
                MessageBox.Show("Ошибка при добавлении в БД!\n" + ex.Message, "Ошибка SQL", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnExit_Click(object sender, EventArgs e) { this.DialogResult = DialogResult.Cancel; this.Close(); }
    }
}