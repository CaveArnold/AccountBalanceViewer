# Account Balance Viewer

**Version:** 1.0.1  
**Date:** February 11, 2026  
**Developer:** Cave Arnold  
**AI Assistant:** Gemini

## Overview

The **Account Balance Viewer** is a specialized Windows Forms application designed to streamline the process of updating the *My Retirement Tracking Spreadsheet*. 

It connects to a local SQL Server database to aggregate account balances by tax status (Taxable, Tax Free, Tax Deferred, Cash) and provides a "click-to-copy" interface. The application automatically formats the copied value with a normalization factor formula, ready to be pasted directly into the tracking spreadsheet.

## Features

* **Automated Aggregation:** Connects to `(localdb)\MSSQLLocalDB` and sums balances using the `TaxType` column.
* **Visual Dashboard:** Displays four distinct categories:
    * Taxable
    * Tax Free
    * Tax Deferred
    * Cash
* **One-Click Copy:** Clicking any header or value copies a pre-formatted Excel formula to the clipboard.
* **Normalization Logic:** The copied value includes a hardcoded reference to a normalization factor (cell `$L$60`) for immediate use in the target spreadsheet.

## How It Works

1.  **Data Fetch:** On startup, the application queries the `Guyton-Klinger-Withdrawals` database. It executes a `GROUP BY` query on the `vw_AccountBalancesByTaxTypeAndCategory` view.
2.  **Mapping:** The SQL results are mapped to the UI headers.
3.  **Clipboard Action:** When a user clicks a value (e.g., `12,345.67`), the application generates the string `=12345.67*$L$60` and copies it to the Windows Clipboard.

## Prerequisites & Configuration

### Database Requirements
The application expects a LocalDB instance with the following configuration:

* **Server:** `(localdb)\MSSQLLocalDB`
* **Database:** `Guyton-Klinger-Withdrawals`
* **Required View:** `[dbo].[vw_AccountBalancesByTaxTypeAndCategory]`
* **Required Columns:** `TaxType`, `TotalBalance`
* All the required database schema objects can be created using the DDL kept in this GitHub repository: [Guyton-Klinger-Withdrawals](https://github.com/CaveArnold/Guyton-Klinger-Withdrawals)

### SQL View Schema
Ensure your database view matches the logic expected by the application:

```sql
SELECT TaxType, SUM(TotalBalance) as Total 
FROM [dbo].[vw_AccountBalancesByTaxTypeAndCategory] 
GROUP BY TaxType
```

## License

This project is licensed under the [GPL-3.0 License](LICENSE.txt).