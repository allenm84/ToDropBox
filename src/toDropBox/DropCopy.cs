using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace toDropBox
{
  public enum DropCopyErrorCode : int
  {
    InvalidDropBox = 100,
    JsonParseError,
    InvalidParameters,
    GenericException = 1337,
  }

  public class DropCopy
  {
    static readonly string[] sTokens = { "\":", "{", "}", "\"", "," };

    public DropCopy(string source, string destinationRelative)
    {
      Source = source;
      DestinationRelative = destinationRelative;
    }

    public string Source { get; }
    public string DestinationRelative { get; }
    public string[] Extensions { get; set; }

    public void Run()
    {
      var paths = new string[]
      {
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
      };

      var info = paths
        .Select(p => Path.Combine(p, "Dropbox", "info.json"))
        .FirstOrDefault(File.Exists);

      if (info == null)
      {
        Console.Error.WriteLine("Dropbox v2.8 or later must be installed");
        Exit(DropCopyErrorCode.InvalidDropBox);
        return;
      }

      var dropbox = ParseJson(info);
      try
      {
        // generate the set of allowed extensions
        var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (Extensions?.Length > 0)
        {
          foreach (var ext in Extensions)
            allowedExtensions.Add(ext);
        }
        else
        {
          allowedExtensions.Add(".dll");
          allowedExtensions.Add(".exe");
        }

        // get the destination directory
        var destination = Path.Combine(dropbox, DestinationRelative);
        if (!Directory.Exists(destination))
        {
          Directory.CreateDirectory(destination);
        }

        // copy the contents over
        CopyDirectory(allowedExtensions, Source, destination);
      }
      catch (Exception ex)
      {
        Console.Error.WriteLine(ex.ToString());
        Exit(DropCopyErrorCode.GenericException);
      }
    }

    private static void CopyFile(string source, string destination, int indent = 0)
    {
      var directory = Path.GetDirectoryName(destination);
      if (!Directory.Exists(directory))
      {
        Directory.CreateDirectory(directory);
      }

      File.Copy(source, destination, true);
      Console.WriteLine("{0}{1} => {2}", Tabs(indent), source, destination);
    }

    private static void CopyDirectory(HashSet<string> allowed, string source, string destination, int indent = 0)
    {
      Console.WriteLine("{0}{1} => {2}", Tabs(indent), source, destination);

      var subdirs = Directory.EnumerateDirectories(source);
      foreach (var dir in subdirs)
      {
        var name = Path.GetFileName(dir);

        var newPath = Path.Combine(destination, name);
        if (!Directory.Exists(newPath))
        {
          Directory.CreateDirectory(newPath);
        }

        CopyDirectory(allowed, dir, newPath, indent + 1);
      }

      var files = Directory.EnumerateFiles(source);
      foreach (var file in files)
      {
        var name = Path.GetFileName(file);
        if (allowed.Contains(Path.GetExtension(file)))
        {
          CopyFile(file, Path.Combine(destination, name), indent);
        }
        else
        {
          Console.WriteLine("{0}{1} skipped", Tabs(indent), name);
        }
      }
    }

    private static void Exit(DropCopyErrorCode code)
    {
      Environment.Exit((int)code);
    }

    private static string ParseViaSerializer(string filepath)
    {
      var jss = new JavaScriptSerializer();
      var info = jss.Deserialize<Dictionary<string, dynamic>>(File.ReadAllText(filepath));

      if (!info.TryGetValue("personal", out dynamic dropbox))
      {
        dropbox = info["business"];
      }

      return dropbox["path"];
    }

    private static string ParseJson(string filepath)
    {
      try
      {
        return ParseViaSerializer(filepath);
      }
      catch (Exception ex)
      {
        Console.Error.WriteLine(ex);
        Exit(DropCopyErrorCode.JsonParseError);
      }

      return null;
    }

    private static string Tabs(int count)
    {
      if (count <= 0)
      {
        return string.Empty;
      }
      else
      {
        return new string(' ', count * 2);
      }
    }
  }
}