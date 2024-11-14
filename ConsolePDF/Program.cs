using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using MySql.Data.MySqlClient;

class Program
{
    static void Main(string[] args)
    {
        string pathPDF = @"C:\Users\vladi\Desktop\pdf";
        string[] arrayPDF = Directory.GetFiles(pathPDF, "*.pdf");

        foreach (string filePDF in arrayPDF)
        {
            ProcessPDF(filePDF);
        }
    }

    static void ProcessPDF(string pathPDF)
    {
        byte[] byteArray = File.ReadAllBytes(pathPDF);

        string text = ParsingPDF(pathPDF);
        int? number = OrderNumber(text);

        if (number.HasValue)
        {
            string binFolder = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(pathPDF), "bin");
            Directory.CreateDirectory(binFolder);
            string binName = number.Value + ".bin";
            string binResult = System.IO.Path.Combine(binFolder, binName);

            File.WriteAllBytes(binResult, byteArray);

            WriteMySql(number.Value, binResult);
        }
    }

    static string ParsingPDF(string path)
    {
        using (PdfReader reader = new PdfReader(path))
        {
            try
            {
                StringWriter result = new StringWriter();

                for (int i = 1; i <= reader.NumberOfPages; i++)
                {
                    string textPage = PdfTextExtractor.GetTextFromPage(reader, i);
                    result.WriteLine(textPage);
                }

                return result.ToString();
            }
            catch (Exception error)
            {
                LogError(path, error.ToString());
                return $"Ошибка в {path}";
            }
        }
    }

    static int? OrderNumber(string text)
    {
        string searchPattern = @"Номер заказа\s*:\s*(\d+)";
        Match match = Regex.Match(text, searchPattern);

        if (match.Success)
        {
            return int.Parse(match.Groups[1].Value);
        }

        return null;
    }

    static void WriteMySql(int number, string binPath)
    {
        string connectionString = "server=localhost;user=root;password=root;database=files";

        using (MySqlConnection connection = new MySqlConnection(connectionString))
        {
            try
            {
                connection.Open();

                string check = "SELECT COUNT(*) FROM info WHERE Name = @Number";
                using (MySqlCommand checkCommand = new MySqlCommand(check, connection))
                {
                    checkCommand.Parameters.AddWithValue("@Number", number);
                    int count = Convert.ToInt32(checkCommand.ExecuteScalar());
                    if (count > 0)
                    {
                        return;
                    }
                }

                string query = "INSERT INTO info (Name, Path) VALUES (@Number, @Path)";
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Number", number);
                    command.Parameters.AddWithValue("@Path", binPath);
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                LogError(binPath, ex.ToString());
            }
            finally
            {
                connection.Close();
            }
        }
    }

    static void LogError(string path, string error)
    {
        string connectionLogs = "server=localhost;user=root;password=root;database=logs";

        using (MySqlConnection connection = new MySqlConnection(connectionLogs))
        {
            try
            {
                connection.Open();

                string query = "INSERT INTO info (Path, Error) VALUES (@Path, @Error)";
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Path", path);
                    command.Parameters.AddWithValue("@Error", error);
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при записи логов: {ex.Message}");
            }
            finally
            {
                connection.Close();
            }
        }
    }
}
