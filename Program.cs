using System;
using System.Collections.Generic;
using System.IO;
using System.Security.AccessControl;
using System.Threading.Tasks;

namespace From_CSV_to_Files
{
    internal static class Program
    {
        /// <summary>
        /// Путь к месту где лежит приложение
        /// </summary>
        private static string Programpath { get; } = Path.GetDirectoryName(
            System.Reflection.Assembly.GetExecutingAssembly().Location
        );
        /// <summary>
        /// Путь где лежат csv файлы
        /// </summary>
        private static string Csvpath { get; } = Programpath + "\\csvFile";
        /// <summary>
        /// Путь в который необходимо сохранять файлы
        /// </summary>
        private static string Savepath { get; } = Programpath + "\\files";
        /// <summary>
        /// Тип искомых файлов
        /// </summary>
        private static string Filetype { get; } = "*.csv";
        /// <summary>
        /// Обязательные поля, без них магии не будет
        /// </summary>
        private static List<string> MainFieldList => new List<string>() {"BODY", "NAME", "PARENT.NAME" };

        private static void Main()
        {
            var task = StartProcess();
            task.Wait();
            Console.Write("\r\nPress any button to close app");
            Console.ReadKey();
        }
        /// <summary>
        /// Запуск процесса обработки файлов
        /// </summary>
        private static async Task StartProcess()
        {
            var filesPath = GetFilesPath(Csvpath, Filetype);
            for (var i = filesPath.Length - 1; i >= 0; --i)
            {
                await StartRead(filesPath[i]);
            }
        }
        /// <summary>
        /// Функция для полученияя словаря, где ключ - название поля, а значение - все возможные значения этого поля
        /// </summary>
        /// <param name="filePath">Строка содержащая путь к файлу</param>
        /// <returns>Славорь где ключ - название поля, а значение - все возможные значения этого поля</returns>
        private static async Task StartRead(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return;
            var reader = new StreamReader(File.OpenRead(filePath));
            await ReadFile(reader);
        }
        /// <summary>
        /// Функция для чтения данных из файла.
        /// </summary>
        /// <param name="reader">StreamReader для считывания данных из файла.</param>
        private static async Task ReadFile(StreamReader reader)
        {
            if (reader == null) return;
            var lineNumber = 0;
            var keyIndexes = new Dictionary<int, string>();
            var datas = new Dictionary<string, string>();
            while (!reader.EndOfStream)
            {
                string line;
                string[] values;
                if (lineNumber == 0)
                {
                    ++lineNumber;
                    line = await reader.ReadLineAsync();
                    if (line == null) continue;
                    values = line.Split(new[] { "\",\"" }, StringSplitOptions.None);
                    for (var i = values.Length - 1; i >= 0; --i)
                    {
                        keyIndexes.Add(i, values[i].Replace("\"", ""));
                    }
                }
                else
                {
                    line = await reader.ReadLineAsync();
                    if (line == null) continue;
                    values = line.Split(new[]{ "\",\"" }, StringSplitOptions.None);
                    for (var i = values.Length - 1; i >= 0; --i)
                    {
                        datas.Add(keyIndexes[i], string.Empty);
                        datas[keyIndexes[i]] = values[i].Replace("\"", "");
                    }
                    var task = new Task(() => DoSave(datas[MainFieldList[0]], datas[MainFieldList[1]], datas[MainFieldList[2]]));
                    task.Start();
                    task.Wait();
                    datas.Clear();
                }
            }
        }
        /// <summary>
        /// Функция для получения путей всех файлов указанного типа по указанному пути
        /// </summary>
        /// <param name="path">Путь к папке в которой необходимо искать</param>
        /// <param name="type">Тип файлов которые необходимо искать</param>
        /// <returns></returns>
        private static string[] GetFilesPath(string path, string type)
        {
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(type)) return new[] {""};
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            return Directory.GetFiles(path, type);
        }
        /// <summary>
        /// Функция для сохранения полученного в виде Base64 строки файла
        /// </summary>
        /// <param name="base64Content">Содержимое файла</param>
        /// <param name="name">Имя этого файла вместе с расширением</param>
        /// <param name="subfolder">Необязательный параметр. Имя подпапки для сохранения</param>
        private static void DoSave(string base64Content, string name, string subfolder = "")
        {
            if (!string.IsNullOrEmpty(base64Content) && !string.IsNullOrEmpty(name))
            {
                var fullPath = Savepath + '\\';
                fullPath += (string.IsNullOrEmpty(subfolder)) ? "" : subfolder ;
                if (!Directory.Exists(Savepath))
                {
                    Directory.CreateDirectory(Savepath);
                }
                else if (!string.IsNullOrEmpty(subfolder) && !Directory.Exists(fullPath))
                {
                    Directory.CreateDirectory(fullPath);
                }
                if (!HasWritePermissionOnDir(fullPath))
                {
                    ChangeWritePermissionOnDir(fullPath);
                }
                using (var stream = File.Create(fullPath + '\\' + name))
                {
                    Console.Write("\r\nFile path - {0},\r\nFile name - {1}", subfolder, name);
                    var byteArray = Convert.FromBase64String(base64Content);
                    stream.Write(byteArray, 0, byteArray.Length);
                }
            }
            else
            {
                Console.Write("Content or name are empty.");
            }
        }
        /// <summary>
        /// Изменение атрибутов папки если она ReadOnly
        /// </summary>
        /// <param name="path">Путь к папке которую необходимо расшарить для записи</param>
        private static void ChangeWritePermissionOnDir(string path)
        {
            var accessControlList = Directory.GetAccessControl(path);
            var accessRules = accessControlList?.GetAccessRules(true, true,
                typeof(System.Security.Principal.SecurityIdentifier));
            if (accessRules == null)
                return;

            foreach (FileSystemAccessRule rule in accessRules)
            {
                if ((FileSystemRights.Write & rule.FileSystemRights) != FileSystemRights.Write)
                    continue;

                if (rule.AccessControlType != AccessControlType.Allow &&
                    rule.AccessControlType == AccessControlType.Deny)
                {
                    File.SetAttributes(path, FileAttributes.Normal);
                }
            }
        }
        /// <summary>
        /// Метод для определения прав записи к указанной папке
        /// </summary>
        /// <param name="path">Путь к папке</param>
        /// <returns>Флаг о возможности записи в указанныю папку</returns>
        private static bool HasWritePermissionOnDir(string path)
        {
            var writeAllow = false;
            var writeDeny = false;
            var accessControlList = Directory.GetAccessControl(path);
            var accessRules = accessControlList?.GetAccessRules(true, true,
                typeof(System.Security.Principal.SecurityIdentifier));
            if (accessRules == null)
                return false;

            foreach (FileSystemAccessRule rule in accessRules)
            {
                if ((FileSystemRights.Write & rule.FileSystemRights) != FileSystemRights.Write)
                    continue;

                if (rule.AccessControlType == AccessControlType.Allow)
                    writeAllow = true;
                else if (rule.AccessControlType == AccessControlType.Deny)
                    writeDeny = true;
            }

            return writeAllow && !writeDeny;
        }
    }
}