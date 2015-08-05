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
            var task = new Task(StartProcess);
            task.Start();
            task.Wait();
            Console.Write("\r\nPress any button to close app");
            Console.ReadKey();
        }
        /// <summary>
        /// Запуск процесса обработки файлов
        /// </summary>
        private static void StartProcess()
        {
            var filesPath = GetFilesPath(Csvpath, Filetype);
            for (var i = filesPath.Length - 1; i >= 0; --i)
            {
                var temDictionary = GetContentAndType(filesPath[i]);
                for (var j = temDictionary[MainFieldList[0]].Count - 1; j >= 0; --j)
                {
                    DoSave(
                        temDictionary[MainFieldList[0]][j],
                        temDictionary[MainFieldList[1]][j],
                        temDictionary[MainFieldList[2]][j]
                    );
                    Console.Write("\r\nFile name - {0}", temDictionary[MainFieldList[1]][j]);
                }
            }
        }
        /// <summary>
        /// Функция для полученияя словаря, где ключ - название поля, а значение - все возможные значения этого поля
        /// </summary>
        /// <param name="filePath">Строка содержащая путь к файлу</param>
        /// <returns>Славорь где ключ - название поля, а значение - все возможные значения этого поля</returns>
        private static Dictionary<string, List<string>> GetContentAndType(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return new Dictionary<string, List<string>>();
            var contentDictionary = new Dictionary<string, List<string>>();
            var reader = new StreamReader(File.OpenRead(filePath));
            ReadFile(reader, ref contentDictionary);
            return contentDictionary;
        }
        /// <summary>
        /// Функция для чтения данных из файла.
        /// </summary>
        /// <param name="reader">StreamReader для считывания данных из файла.</param>
        /// <param name="datas">Структура в которую будут добавлятся данные.</param>
        private static void ReadFile(StreamReader reader, ref Dictionary<string, List<string>> datas)
        {
            if (reader == null || datas == null) return;
            var lineNumber = 0;
            var keyIndexes = new Dictionary<int, string>();
            while (!reader.EndOfStream)
            {
                if (lineNumber == 0)
                {
                    ++lineNumber;
                    var fieldNamesLine = reader.ReadLine();
                    if (fieldNamesLine == null) continue;
                    var fieldNamesList = fieldNamesLine.Split(',');
                    for (var i = fieldNamesList.Length - 1; i >= 0; --i)
                    {
                        datas.Add(fieldNamesList[i].Replace("\"", ""), new List<string>());
                        keyIndexes.Add(i, fieldNamesList[i].Replace("\"", ""));
                    }
                }
                else
                {
                    var line = reader.ReadLine();
                    if (line == null) continue;
                    var values = line.Split(',');
                    for (var i = values.Length - 1; i >= 0; --i)
                    {
                        datas[keyIndexes[i]].Add(values[i].Replace("\"", ""));
                    }
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
            if (!string.IsNullOrEmpty(path) && !string.IsNullOrEmpty(type))
            {
                return Directory.GetFiles(path, type);
            }
            return new[] { "" };
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
                /*
                   !!!!!!!!Допилить доступ к папке, по дефолту он поскуда папку создаёт только для чтения!!!!!!!!
                */
                Console.Write("\r\nSome rule - " + HasWritePermissionOnDir(Savepath));
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