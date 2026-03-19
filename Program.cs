/*************************
* Автор: Черненко Никита.*
* Дата: 19.03.2026       *
* Вариант -              *
**************************/

using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;

namespace TextEditor {
  public interface IOriginator {
    object GetMemento();
    void SetMemento(object memento);
  }

  [Serializable]
  public class TextFileMemento {
    public string Content { get; set; }
    public DateTime LastModified { get; set; }
  }

  [Serializable]
  public class TextFile : IOriginator {
    private string _filePath;
    private string _content;
    private DateTime _lastModified;

    public string FilePath {
      get { return _filePath; }
      set { _filePath = value; }
    }

    public string Content {
      get { return _content; }
      set {
        _content = value;
        _lastModified = DateTime.Now;
      }
    }

    public DateTime LastModified {
      get { return _lastModified; }
      set { _lastModified = value; }
    }

    public TextFile() {
      _filePath = string.Empty;
      _content = string.Empty;
      _lastModified = DateTime.Now;
    }

    public TextFile(string filePath, string content) {
      _filePath = filePath;
      _content = content;
      _lastModified = DateTime.Now;
    }

    public void LoadFromDisk() {
      if (File.Exists(_filePath)) {
        FileStream fileStream = new FileStream(_filePath, FileMode.Open, FileAccess.Read);
        StreamReader streamReader = new StreamReader(fileStream);
        _content = streamReader.ReadToEnd();
        streamReader.Close();
        fileStream.Close();
        _lastModified = File.GetLastWriteTime(_filePath);
      }
    }

    public void SaveToDisk() {
      FileStream fileStream = new FileStream(_filePath, FileMode.OpenOrCreate, FileAccess.Write);
      StreamWriter streamWriter = new StreamWriter(fileStream);
      streamWriter.Write(_content);
      streamWriter.Flush();
      streamWriter.Close();
      fileStream.Close();
      _lastModified = DateTime.Now;
    }

    public void BinarySerialize(FileStream fileStream) {
      BinaryFormatter binaryFormatter = new BinaryFormatter();
      binaryFormatter.Serialize(fileStream, this);
      fileStream.Flush();
      fileStream.Close();
    }

    public void BinaryDeserialize(FileStream fileStream) {
      BinaryFormatter binaryFormatter = new BinaryFormatter();
      TextFile deserialized = (TextFile)binaryFormatter.Deserialize(fileStream);
      _filePath = deserialized._filePath;
      _content = deserialized._content;
      _lastModified = deserialized._lastModified;
      fileStream.Close();
    }

    public void XmlSerialize(FileStream fileStream) {
      XmlSerializer xmlSerializer = new XmlSerializer(typeof(TextFile));
      xmlSerializer.Serialize(fileStream, this);
      fileStream.Flush();
      fileStream.Close();
    }

    public void XmlDeserialize(FileStream fileStream) {
      XmlSerializer xmlSerializer = new XmlSerializer(typeof(TextFile));
      TextFile deserialized = (TextFile)xmlSerializer.Deserialize(fileStream);
      _filePath = deserialized._filePath;
      _content = deserialized._content;
      _lastModified = deserialized._lastModified;
      fileStream.Close();
    }

    public object GetMemento() {
      return new TextFileMemento {
        Content = this._content,
        LastModified = this._lastModified
      };
    }

    public void SetMemento(object memento) {
      if (memento is TextFileMemento) {
        TextFileMemento textFileMemento = (TextFileMemento)memento;
        _content = textFileMemento.Content;
        _lastModified = textFileMemento.LastModified;
      }
    }

    public void Print() {
      Console.WriteLine("FilePath: {0} Content: {1} LastModified: {2}", _filePath, _content, _lastModified);
    }
  }

  public class Caretaker {
    private Stack<object> _undoStack;
    private Stack<object> _redoStack;

    public Caretaker() {
      _undoStack = new Stack<object>();
      _redoStack = new Stack<object>();
    }

    public void SaveState(IOriginator originator) {
      _undoStack.Push(originator.GetMemento());
      _redoStack.Clear();
    }

    public void Undo(IOriginator originator) {
      if (_undoStack.Count > 1) {
        _redoStack.Push(_undoStack.Pop());
        originator.SetMemento(_undoStack.Peek());
      }
    }

    public void Redo(IOriginator originator) {
      if (_redoStack.Count > 0) {
        object memento = _redoStack.Pop();
        _undoStack.Push(memento);
        originator.SetMemento(memento);
      }
    }
  }

  public class FileSearcher {
    private readonly string[] _searchPatterns = new string[] { "*.txt", "*.cs", "*.xml" };

    public TextFile[] Search(string directoryPath, string[] keywords) {
      List<TextFile> resultFiles = new List<TextFile>();

      if (!Directory.Exists(directoryPath)) {
        return resultFiles.ToArray();
      }

      for (int patternIndex = 0; patternIndex < _searchPatterns.Length; ++patternIndex) {
        string pattern = _searchPatterns[patternIndex];
        string[] files = Directory.GetFiles(directoryPath, pattern);

        for (int fileIndex = 0; fileIndex < files.Length; ++fileIndex) {
          string file = files[fileIndex];

          try {
            TextFile textFile = new TextFile(file, "");
            textFile.LoadFromDisk();

            bool containsAllKeywords = true;

            for (int keywordIndex = 0; keywordIndex < keywords.Length; ++keywordIndex) {
              string keyword = keywords[keywordIndex];

              if (!textFile.Content.ToLower().Contains(keyword.ToLower())) {
                containsAllKeywords = false;
                break;
              }
            }

            if (containsAllKeywords) {
              resultFiles.Add(textFile);
            }
          }
          catch (Exception exception) {
            Console.WriteLine("Error processing file {0}: {1}", file, exception.Message);
          }
        }
      }

      return resultFiles.ToArray();
    }
  }

  public class FileIndexer {
    private string[] _indexedFiles;
    private string[] _indexKeywords;
    private int _indexCount;
    private const int _maxIndexSize = 100;

    public FileIndexer() {
      _indexedFiles = new string[_maxIndexSize];
      _indexKeywords = new string[_maxIndexSize];
      _indexCount = 0;
    }

    public void IndexDirectory(string directoryPath, string[] keywords) {
      FileSearcher fileSearcher = new FileSearcher();
      TextFile[] foundFiles = fileSearcher.Search(directoryPath, keywords);

      for (int fileIndex = 0; fileIndex < foundFiles.Length; ++fileIndex) {
        if (_indexCount < _maxIndexSize) {
          _indexedFiles[_indexCount] = foundFiles[fileIndex].FilePath;

          string keywordsString = "";
          for (int keywordIndex = 0; keywordIndex < keywords.Length; ++keywordIndex) {
            if (keywordIndex > 0) {
              keywordsString += ",";
            }
            keywordsString += keywords[keywordIndex];
          }

          _indexKeywords[_indexCount] = keywordsString;
          ++_indexCount;
        }
      }

      Console.WriteLine("Indexed files: {0}", foundFiles.Length);
    }

    public string[] SearchIndex(string[] keywords) {
      List<string> resultFiles = new List<string>();

      string searchKeywords = "";
      for (int keywordIndex = 0; keywordIndex < keywords.Length; ++keywordIndex) {
        if (keywordIndex > 0) {
          searchKeywords += ",";
        }
        searchKeywords += keywords[keywordIndex];
      }

      for (int indexPosition = 0; indexPosition < _indexCount; ++indexPosition) {
        if (_indexKeywords[indexPosition] == searchKeywords) {
          resultFiles.Add(_indexedFiles[indexPosition]);
        }
      }

      return resultFiles.ToArray();
    }
  }

  class Program {
    private static TextFile _currentFile;
    private static Caretaker _caretaker;
    private static FileSearcher _fileSearcher;
    private static FileIndexer _fileIndexer;

    static void Main(string[] args) {
      _caretaker = new Caretaker();
      _fileSearcher = new FileSearcher();
      _fileIndexer = new FileIndexer();

      Console.InputEncoding = System.Text.Encoding.UTF8;
      Console.OutputEncoding = System.Text.Encoding.UTF8;
      Console.Title = "Text Editor";

      bool isRunning = true;

      while (isRunning) {
        try {
          PrintMenu();
          string choice = Console.ReadLine();

          switch (choice) {
            case "1": {
                CreateNewFile();
                break;
              }
            case "2": {
                OpenFile();
                break;
              }
            case "3": {
                EditFile();
                break;
              }
            case "4": {
                SaveFile();
                break;
              }
            case "5": {
                UndoChanges();
                break;
              }
            case "6": {
                RedoChanges();
                break;
              }
            case "7": {
                SearchFiles();
                break;
              }
            case "8": {
                IndexDirectory();
                break;
              }
            case "9": {
                BinarySerialize();
                break;
              }
            case "10": {
                XmlSerialize();
                break;
              }
            case "11": {
                BinaryDeserialize();
                break;
              }
            case "12": {
                XmlDeserialize();
                break;
              }
            case "0": {
                isRunning = false;
                break;
              }
            default: {
                Console.WriteLine("Invalid choice. Try again.");
                break;
              }
          }

          if (isRunning) {
            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
            Console.Clear();
          }
        }
        catch (Exception exception) {
          Console.WriteLine("Error: {0}", exception.Message);
          Console.WriteLine("\nPress any key to continue...");
          Console.ReadKey();
          Console.Clear();
        }
      }
    }

    private static void PrintMenu() {
      Console.Clear();
      Console.WriteLine("=================================");
      Console.WriteLine("         TEXT EDITOR");
      Console.WriteLine("=================================");

      if (_currentFile != null) {
        Console.WriteLine("Current file: {0}", _currentFile.FilePath);
        Console.WriteLine("---------------------------------");
      }

      Console.WriteLine(
          "1. Create new file\n" +
          "2. Open file\n" +
          "3. Edit file\n" +
          "4. Save file\n" +
          "5. Undo changes\n" +
          "6. Redo changes\n" +
          "7. Search files by keywords\n" +
          "8. Index directory\n" +
          "9. Binary serialize\n" +
          "10. XML serialize\n" +
          "11. Binary deserialize\n" +
          "12. XML deserialize\n" +
          "0. Exit\n"
      );

      Console.Write("\nChoose action: ");
    }

    private static void CreateNewFile() {
      Console.Write("Enter path for new file: ");
      string filePath = Console.ReadLine();

      if (string.IsNullOrWhiteSpace(filePath)) {
        Console.WriteLine("Path cannot be empty");
        return;
      }

      try {
        FileStream fileStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write);
        fileStream.Close();

        _currentFile = new TextFile(filePath, "");
        _caretaker.SaveState(_currentFile);

        Console.WriteLine("Created new file: {0}", filePath);
      }
      catch (Exception exception) {
        Console.WriteLine("Error creating file: {0}", exception.Message);
      }
    }

    private static void OpenFile() {
      Console.Write("Enter path to file: ");
      string filePath = Console.ReadLine();

      if (!File.Exists(filePath)) {
        Console.WriteLine("File not found: {0}", filePath);
        return;
      }

      _currentFile = new TextFile(filePath, "");
      _currentFile.LoadFromDisk();
      _caretaker.SaveState(_currentFile);

      Console.WriteLine("File opened: {0}", filePath);
      _currentFile.Print();
    }

    private static void EditFile() {
      if (_currentFile == null) {
        Console.WriteLine("Open or create file first.");
        return;
      }

      Console.WriteLine("Current file content:");
      Console.WriteLine("---------------------------------");
      Console.WriteLine(_currentFile.Content);
      Console.WriteLine("---------------------------------");
      Console.WriteLine("Enter new content (empty line to finish):");

      string newContent;
      newContent = "";

      string line = Console.ReadLine();

      while (line != "") {
        newContent += line + Environment.NewLine;
        line = Console.ReadLine();
      }

      _currentFile.Content = newContent;
      _caretaker.SaveState(_currentFile);

      Console.WriteLine("File updated.");
    }

    private static void SaveFile() {
      if (_currentFile == null) {
        Console.WriteLine("No file to save.");
        return;
      }

      _currentFile.SaveToDisk();
      Console.WriteLine("File saved: {0}", _currentFile.FilePath);
    }

    private static void UndoChanges() {
      if (_currentFile == null) {
        Console.WriteLine("No open file.");
        return;
      }

      _caretaker.Undo(_currentFile);
      Console.WriteLine("Changes undone.");
    }

    private static void RedoChanges() {
      if (_currentFile == null) {
        Console.WriteLine("No open file.");
        return;
      }

      _caretaker.Redo(_currentFile);
      Console.WriteLine("Changes redone.");
    }

    private static void SearchFiles() {
      Console.Write("Enter path to search: ");
      string searchPath = Console.ReadLine();

      Console.Write("Enter keywords (space separated): ");
      string keywordsInput = Console.ReadLine();

      string[] keywords;
      int keywordCount;
      string currentKeyword;

      keywords = new string[10];
      keywordCount = 0;
      currentKeyword = "";

      for (int charIndex = 0; charIndex < keywordsInput.Length; ++charIndex) {
        if (keywordsInput[charIndex] == ' ') {
          if (currentKeyword != "") {
            keywords[keywordCount] = currentKeyword;
            ++keywordCount;
            currentKeyword = "";
          }
        }
        else {
          currentKeyword += keywordsInput[charIndex];
        }
      }

      if (currentKeyword != "") {
        keywords[keywordCount] = currentKeyword;
        ++keywordCount;
      }

      string[] finalKeywords = new string[keywordCount];
      for (int copyIndex = 0; copyIndex < keywordCount; ++copyIndex) {
        finalKeywords[copyIndex] = keywords[copyIndex];
      }

      Console.WriteLine("Searching files...");
      TextFile[] foundFiles = _fileSearcher.Search(searchPath, finalKeywords);

      Console.WriteLine("\nFiles found: {0}", foundFiles.Length);
      for (int fileIndex = 0; fileIndex < foundFiles.Length; ++fileIndex) {
        foundFiles[fileIndex].Print();
      }
    }

    private static void IndexDirectory() {
      Console.Write("Enter path to index: ");
      string indexPath = Console.ReadLine();

      Console.Write("Enter keywords (space separated): ");
      string keywordsInput = Console.ReadLine();

      string[] keywords;
      int keywordCount;
      string currentKeyword;

      keywords = new string[10];
      keywordCount = 0;
      currentKeyword = "";

      for (int charIndex = 0; charIndex < keywordsInput.Length; ++charIndex) {
        if (keywordsInput[charIndex] == ' ') {
          if (currentKeyword != "") {
            keywords[keywordCount] = currentKeyword;
            ++keywordCount;
            currentKeyword = "";
          }
        }
        else {
          currentKeyword += keywordsInput[charIndex];
        }
      }

      if (currentKeyword != "") {
        keywords[keywordCount] = currentKeyword;
        ++keywordCount;
      }

      string[] finalKeywords = new string[keywordCount];
      for (int copyIndex = 0; copyIndex < keywordCount; ++copyIndex) {
        finalKeywords[copyIndex] = keywords[copyIndex];
      }

      _fileIndexer.IndexDirectory(indexPath, finalKeywords);
    }

    private static void BinarySerialize() {
      if (_currentFile == null) {
        Console.WriteLine("No file to serialize.");
        return;
      }

      Console.Write("Enter path to save: ");
      string targetPath = Console.ReadLine();

      FileStream fileStream = new FileStream(targetPath, FileMode.OpenOrCreate, FileAccess.Write);
      _currentFile.BinarySerialize(fileStream);

      Console.WriteLine("File binary serialized to: {0}", targetPath);
    }

    private static void XmlSerialize() {
      if (_currentFile == null) {
        Console.WriteLine("No file to serialize.");
        return;
      }

      Console.Write("Enter path to save: ");
      string targetPath = Console.ReadLine();

      FileStream fileStream = new FileStream(targetPath, FileMode.OpenOrCreate, FileAccess.Write);
      _currentFile.XmlSerialize(fileStream);

      Console.WriteLine("File XML serialized to: {0}", targetPath);
    }

    private static void BinaryDeserialize() {
      Console.Write("Enter path to file: ");
      string sourcePath = Console.ReadLine();

      FileStream fileStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read);
      _currentFile = new TextFile();
      _currentFile.BinaryDeserialize(fileStream);

      _caretaker.SaveState(_currentFile);
      Console.WriteLine("File binary deserialized.");
      _currentFile.Print();
    }

    private static void XmlDeserialize() {
      Console.Write("Enter path to file: ");
      string sourcePath = Console.ReadLine();

      FileStream fileStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read);
      _currentFile = new TextFile();
      _currentFile.XmlDeserialize(fileStream);

      _caretaker.SaveState(_currentFile);
      Console.WriteLine("File XML deserialized.");
      _currentFile.Print();
    }
  }
}