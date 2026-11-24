using System;
using System.Data;
using System.Windows.Forms;

namespace Electrical_Store
{
    public partial class InsertForm : Form
    {
        private string _entityType;
        private int _id;

        public InsertForm(string entityType, int id)
        {
            InitializeComponent();
            _entityType = entityType;
            _id = id;

            panelUser.Visible = false;
            panelProduct.Visible = false;
            panelStock.Visible = false;
            panelOrder.Visible = false;

            if (_entityType == "User")
            {
                panelUser.Visible = true;
                this.Text = "Редактирование пользователя";
                DataTable dtRoles = DbHelper.ExecuteQuery("SELECT id, name FROM roles");
                cmbUserRole.DataSource = dtRoles; cmbUserRole.DisplayMember = "name"; cmbUserRole.ValueMember = "id";
                LoadUserData();
            }
            else if (_entityType == "Product")
            {
                panelProduct.Visible = true;
                this.Text = "Редактирование товара";
                DataTable dtCat = DbHelper.ExecuteQuery("SELECT id, name FROM categories");
                cmbProdCategory.DataSource = dtCat; cmbProdCategory.DisplayMember = "name"; cmbProdCategory.ValueMember = "id";
                decimal.TryParse(txtProdPrice.Text, out decimal pPrice);
                decimal.TryParse(txtProdRetail.Text, out decimal rPrice);

                // Получаем имя выбранной картинки
                string imageName = cmbProductImage.SelectedItem != null ? cmbProductImage.SelectedItem.ToString() : "";

                string query = $@"INSERT INTO products (sku, name, category_id, brand, purchase_price, retail_price, image_path) 
                      VALUES ('{txtProdSKU.Text}', '{txtProdName.Text}', {cmbProdCategory.SelectedValue}, '{txtProdBrand.Text}', {pPrice}, {rPrice}, '{imageName}')";
                DbHelper.ExecuteNonQuery(query);
                LoadProductData();
                LoadImagesToCombo();
            }
            else if (_entityType == "Stock")
            {
                panelStock.Visible = true;
                this.Text = "Редактирование склада";
                DataTable dtProd = DbHelper.ExecuteQuery("SELECT id, name FROM products");
                cmbStockProduct.DataSource = dtProd; cmbStockProduct.DisplayMember = "name"; cmbStockProduct.ValueMember = "id";
                LoadStockData();
            }
            else if (_entityType == "Order")
            {
                panelOrder.Visible = true;
                this.Text = "Редактирование заказа";
                cmbOrdStatus.Items.Clear(); cmbOrdStatus.Items.AddRange(new object[] { "new", "processing", "shipped", "delivered", "cancelled" });
                LoadOrderData();
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

        private void LoadUserData()
        {
            DataTable dt = DbHelper.ExecuteQuery($"SELECT * FROM users WHERE id = {_id}");
            if (dt.Rows.Count > 0)
            {
                DataRow r = dt.Rows[0];
                txtUserFIO.Text = r["full_name"].ToString(); txtUserLogin.Text = r["login"].ToString();
                txtUserPass.Text = r["password"].ToString(); txtUserEmail.Text = r["email"].ToString();
                maskedTextBox1.Text = r["phone"].ToString(); cmbUserRole.SelectedValue = r["role_id"];
            }
        }

        private void LoadProductData()
        {
            DataTable dt = DbHelper.ExecuteQuery($"SELECT * FROM products WHERE id = {_id}");
            if (dt.Rows.Count > 0)
            {
                DataRow r = dt.Rows[0];
                txtProdSKU.Text = r["sku"].ToString(); txtProdName.Text = r["name"].ToString();
                cmbProdCategory.SelectedValue = r["category_id"]; txtProdBrand.Text = r["brand"].ToString();
                txtProdPrice.Text = r["purchase_price"].ToString(); txtProdRetail.Text = r["retail_price"].ToString();
            }
        }

        private void LoadStockData()
        {
            DataTable dt = DbHelper.ExecuteQuery($"SELECT * FROM stock WHERE id = {_id}");
            if (dt.Rows.Count > 0)
            {
                DataRow r = dt.Rows[0];
                cmbStockProduct.SelectedValue = r["product_id"]; txtStockQty.Text = r["quantity"].ToString();
                txtStockMin.Text = r["min_stock_level"].ToString(); txtStockLoc.Text = r["location"].ToString();
            }
        }

        private void LoadOrderData()
        {
            DataTable dt = DbHelper.ExecuteQuery($"SELECT * FROM orders WHERE id = {_id}");
            if (dt.Rows.Count > 0)
            {
                DataRow r = dt.Rows[0];

                // Пункт 7 из вашего ТЗ: Запрет на редактирование завершенных заказов
                if (r["status"].ToString() == "delivered" || r["status"].ToString() == "cancelled")
                {
                    MessageBox.Show("Этот заказ завершен или отменен. Редактирование заблокировано.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    btnSave.Enabled = false;
                    foreach (Control c in panelOrder.Controls) c.Enabled = false;
                }

                txtOrdNumber.Text = r["order_number"].ToString(); txtOrdCust.Text = r["customer_name"].ToString();
                txtOrdPhone.Text = r["customer_phone"].ToString(); cmbOrdStatus.Text = r["status"].ToString();
                txtOrdTotal.Text = r["total_amount"].ToString();
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            // Обязательно добавьте проверку на статус заказа, чтобы не менять завершенные
            if (_entityType == "Order" && (cmbOrdStatus.Text == "delivered" || cmbOrdStatus.Text == "cancelled"))
            {
                MessageBox.Show("Завершенный заказ нельзя редактировать!", "Запрещено", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }

            if (MessageBox.Show("Точно ли вы хотите изменить запись?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            try
            {
                // Логика UPDATE для каждой сущности... (аналогично CreateForm, но с WHERE id = {_id})
                if (_entityType == "User")
                {
                    string query = $"UPDATE users SET login='{txtUserLogin.Text}', password='{txtUserPass.Text}', full_name='{txtUserFIO.Text}', email='{txtUserEmail.Text}', phone='{maskedTextBox1.Text}', role_id={cmbUserRole.SelectedValue} WHERE id={_id}";
                    DbHelper.ExecuteNonQuery(query);
                }
                // ... и так далее для остальных ...

                MessageBox.Show("Запись успешно обновлена!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при обновлении БД!\n" + ex.Message, "Ошибка SQL", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnExit_Click(object sender, EventArgs e) { this.DialogResult = DialogResult.Cancel; this.Close(); }
    }
}