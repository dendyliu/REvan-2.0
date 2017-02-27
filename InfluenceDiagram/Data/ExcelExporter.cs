using NPOI.HSSF.Model;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace InfluenceDiagram.Data
{
    public class ExcelExporter
    {

        static public void ExportToXLS(SpreadsheetComponentData spreadsheet, string path)
        {
            CultureInfo cc = Thread.CurrentThread.CurrentCulture, cuc = Thread.CurrentThread.CurrentUICulture;
            CultureInfo ci = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentCulture = ci;
            Thread.CurrentThread.CurrentUICulture = ci;

            HSSFWorkbook wb = HSSFWorkbook.Create(InternalWorkbook.CreateWorkbook());
            HSSFSheet sh = (HSSFSheet)wb.CreateSheet("Sheet1");

            // column header
            var headerFont = wb.CreateFont();
            headerFont.Boldweight = (short)FontBoldWeight.Bold;
            var headerStyle = wb.CreateCellStyle();
            headerStyle.SetFont(headerFont);
            bool hasHeader = false;
            if (spreadsheet.HasCustomLabeledColumnHeader())
            {
                var r = sh.CreateRow(0);
                for (int col = 0; col < spreadsheet.columnDatas.Count; ++col)
                {
                    var cell = r.CreateCell(col);
                    cell.CellStyle = headerStyle;
                    cell.SetCellValue(spreadsheet.columnDatas[col].HasCustomLabel() ? spreadsheet.columnDatas[col].label : SpreadsheetComponentData.GetDefaultColumnName(col));
                }
                hasHeader = true;
            }

            for (int row = 0; row < spreadsheet.rowDatas.Count; ++row)
            {
                var r = sh.CreateRow(row + (hasHeader ? 1 : 0));
                for (int col = 0; col < spreadsheet.columnDatas.Count; ++col)
                {
                    var cell = r.CreateCell(col);
                    SetCellValue(cell, spreadsheet.cells[row][col]);
                }
            }
            try
            {
                using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
                {
                    wb.Write(fs);
                }
            }
            catch (Exception e)
            {
                throw new Exception("Cannot write to Excel file. Another program might be using it.");
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = cc;
                Thread.CurrentThread.CurrentUICulture = cuc;
            }
        }

        static public void ExportToXLSX(SpreadsheetComponentData spreadsheet, string path)
        {
            CultureInfo cc = Thread.CurrentThread.CurrentCulture, cuc = Thread.CurrentThread.CurrentUICulture;
            CultureInfo ci = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentCulture = ci;
            Thread.CurrentThread.CurrentUICulture = ci;

            XSSFWorkbook wb = new XSSFWorkbook();
            XSSFSheet sh = (XSSFSheet)wb.CreateSheet("Sheet1");

            // column header
            var headerFont = wb.CreateFont();
            headerFont.Boldweight = (short)FontBoldWeight.Bold;
            var headerStyle = wb.CreateCellStyle();
            headerStyle.SetFont(headerFont);
            bool hasHeader = false;
            if (spreadsheet.HasCustomLabeledColumnHeader())
            {
                var r = sh.CreateRow(0);
                for (int col = 0; col < spreadsheet.columnDatas.Count; ++col)
                {
                    var cell = r.CreateCell(col);
                    cell.CellStyle = headerStyle;
                    cell.SetCellValue(spreadsheet.columnDatas[col].HasCustomLabel() ? spreadsheet.columnDatas[col].label : SpreadsheetComponentData.GetDefaultColumnName(col));
                }
                hasHeader = true;
            }

            for (int row = 0; row < spreadsheet.rowDatas.Count; ++row)
            {
                var r = sh.CreateRow(row + (hasHeader ? 1 : 0));
                for (int col = 0; col < spreadsheet.columnDatas.Count; ++col)
                {
                    var cell = r.CreateCell(col);
                    SetCellValue(cell, spreadsheet.cells[row][col]);
                }
            }
            try
            {
                using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
                {
                    wb.Write(fs);
                }
            }
            catch (Exception e)
            {
                throw new Exception("Cannot write to Excel file. Another program might be using it.");
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = cc;
                Thread.CurrentThread.CurrentUICulture = cuc;
            }
        }

        static private void SetCellValue(ICell cell, SpreadsheetCellData data)
        {
            object value = data.GetValue();
            if (value == null) return;
            if (value is string)
            {
                cell.SetCellValue(value as string);
            }
            else
            {
                try
                {
                    double x = Convert.ToDouble(value);
                    cell.SetCellValue(x);
                }
                catch (Exception e)
                {
                    cell.SetCellValue(data.GetValueAsString());
                }
            }
        }

    }
}
