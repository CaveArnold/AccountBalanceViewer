/* * Developer: Cave Arnold
 * AI Assistant: Gemini
 * Date: January 17, 2026
 * Version: 1.0.1
 * * Abstraction: 
 * This is a Windows Forms application designed to make updateing My Retirement Tracking Spreadsheet simple by aggregating by taxable type and including the normalization factor in a preformed calculation that can be pasted directly into one of the tax type columns shown by the application.
 * * Logic Flow:
 * 1. Data Fetch: It connects to your (localdb)\MSSQLLocalDB and runs a GROUP BY query on your view to sum balances by TaxType.
 * 2. Mapping: It maps the SQL results to the specific headers you requested ("Taxable", "Tax Free", "Tax Deferred", "Cash"). Note: This assumes your TaxType column in the database contains these exact strings.
 * 3. UI Layout: Uses a TableLayoutPanel to create 4 equal columns. Row 1 is the Header, Row 2 is the formatted Currency value.
 * 4. Clipboard Logic: When you click either the Title or the Value:
 * 5. It takes the raw numeric value (e.g., 12345.67).
 * 6. Formats it as ={value}*$L$60 (e.g., =12345.67*$L$60). [Note: Thisis to be insert into My Retirement Withdrawal Tracker taxable columns with a hard coded reference to a normalization factor.]
 * 7. Copies it to the Windows Clipboard.
 *
 * Version History:
 * - v1.0.0 (Jan 17, 2026): Initial Release. Basic update functionality.
 * - v1.0.1 (Feb 11, 2026): Added Applicatioon Icon. * 
 *
 */

using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace AccountBalanceViewer
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new DashboardForm());
        }
    }

    public class DashboardForm : Form
    {
        // Configuration
        private const string ConnectionString = @"Server=(localdb)\MSSQLLocalDB;Database=Guyton-Klinger-Withdrawals;Integrated Security=true;TrustServerCertificate=true;";

        // The headers requested
        private readonly string[] _displayHeaders = { "Taxable", "Tax Free", "Tax Deferred", "Cash" };

        // Dictionary to hold sums: Key = Header Name, Value = Total Balance
        private Dictionary<string, decimal> _balanceMap = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        public DashboardForm()
        {
            // Form Setup
            this.Text = "Account Balances";
            this.Size = new Size(800, 250);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            LoadData();
            InitializeLayout();
        }

        private void LoadData()
        {
            // Initialize map with 0.00 for all headers
            foreach (var header in _displayHeaders)
            {
                _balanceMap[header] = 0m;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(ConnectionString))
                {
                    conn.Open();

                    // We aggregate by TaxType here. 
                    // Note: We assume the 'TaxType' column in the view matches the header names (Taxable, Tax Free, etc.)
                    // If 'Cash' is a Category rather than a TaxType in your specific data, you may need to adjust the logic 
                    // to check the Category column as well.
                    string query = @"
                        SELECT 
                            TaxType, 
                            SUM(TotalBalance) as Total 
                        FROM [dbo].[vw_AccountBalancesByTaxTypeAndCategory] 
                        GROUP BY TaxType";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string type = reader["TaxType"].ToString();
                            decimal amount = reader["Total"] != DBNull.Value ? (decimal)reader["Total"] : 0m;

                            // Map database TaxType to our Header keys
                            // We do a loose match. If the DB returns 'Taxable', it matches our 'Taxable' header.
                            if (_balanceMap.ContainsKey(type))
                            {
                                _balanceMap[type] += amount;
                            }
                            else
                            {
                                // Handle edge cases or mapping mismatches if necessary.
                                // For example, if DB has "Roth", map it to "Tax Free" manually here if needed.
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error connecting to database:\n{ex.Message}", "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializeLayout()
        {
            // Main container
            TableLayoutPanel table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = _displayHeaders.Length,
                RowCount = 2,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(20)
            };

            // Set percent columns (equal width)
            float percentWidth = 100f / _displayHeaders.Length;
            for (int i = 0; i < _displayHeaders.Length; i++)
            {
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, percentWidth));
            }

            // Row styles: Headers (auto), Values (remaining)
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            // Populate Grid
            for (int i = 0; i < _displayHeaders.Length; i++)
            {
                string headerTitle = _displayHeaders[i];
                decimal value = _balanceMap[headerTitle];

                // 1. Create Header Label
                Label lblTitle = new Label
                {
                    Text = headerTitle,
                    Font = new Font("Segoe UI", 12, FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Dock = DockStyle.Fill,
                    ForeColor = Color.DimGray,
                    Cursor = Cursors.Hand,
                    Tag = value // Store value in tag for easy access if needed, though we use text mostly
                };

                // 2. Create Value Label
                Label lblValue = new Label
                {
                    Text = value.ToString("C2"), // Currency Format ($1,234.56)
                    Font = new Font("Segoe UI", 16, FontStyle.Regular),
                    TextAlign = ContentAlignment.TopCenter,
                    Dock = DockStyle.Fill,
                    ForeColor = Color.Black,
                    Cursor = Cursors.Hand,
                    Tag = value // Store raw decimal
                };

                // Click Events
                lblTitle.Click += (s, e) => CopyToClipboard(value);
                lblValue.Click += (s, e) => CopyToClipboard(value);

                // Add to Table
                table.Controls.Add(lblTitle, i, 0);
                table.Controls.Add(lblValue, i, 1);
            }

            this.Controls.Add(table);
        }

        private void CopyToClipboard(decimal amount)
        {
            // Format: =1234.56*$L$60
            // We do not want commas, so we use ToString("F2") which gives standard fixed point 1234.56
            string clipboardText = $"={amount:F2}*$L$60";

            Clipboard.SetText(clipboardText);

            // Optional: Visual feedback (Flash the title bar or beep)
            System.Media.SystemSounds.Beep.Play();
        }
    }
}