///////////////////////////////////////////////////////////////////////////
// FileMgr.cs  -  Recursively find files on specified path and patterns  //
// ver 1.0                                                               //
// Language:    C#, Visual Studio 2013, .Net Framework 4.5               //
// Platform:    Dell XPS 2720 , Win 8.1 Pro                              //
// Application: Pr#2 Help, CSE681, Fall 2014                             //
// Author:      Jim Fawcett, CST 2-187, Syracuse University              //
//              (315) 443-3948, jfawcett@twcny.rr.com                    //
///////////////////////////////////////////////////////////////////////////
/*
 * Package Operations
 * ==================
 * FileMgr finds all the C# source code files on some directory tree
 * rooted at a specified path.
 * 
 * Public Interface
 * ================
 * addPattern           // adds a pattern to be used for file matching
 * findFiles(path)      // recursively finds C# source files on specified path
 * getFiles()           // returns array of all the found files
 */
/*
 * Build Process
 * =============
 * Required Files:
 *   FileMgr.cs
 *   
 * Compiler Command:
 *   devenv Parser-Fall14 /rebuild debug
 * 
 * Maintenance History
 * ===================
 * ver 1.0 : 19 Oct 14
 *   - first release
 * 
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows;


namespace CodeAnalysis
{
  public class FileMgr
  {
    List<string> patterns = new List<string>();
    List<string> files = new List<string>();

    static public Action<string> act { get; set; }
    public bool recurse { get; set; }

    public FileMgr() { recurse = true; }
    public void addPattern(string pattern)
    {
      patterns.Add(pattern);
    }
    public void findFiles(string path)
    {
      if (patterns.Count == 0)
        patterns.Add("*.*");
      path = Path.GetFullPath(path);
      Display.displayDirectory(act, path);

      foreach(string pattern in patterns)
      {
        files.AddRange(Directory.GetFiles(path, pattern));
      }
      if(recurse)
      {
        string[] directories = Directory.GetDirectories(path);
        foreach (string dir in directories)
          findFiles(dir);
      }
    }
    public string[] getFiles()
    {
      return files.ToArray();
    }
#if(TEST_FILEMGR)
    static void Main(string[] args)
    {
      Console.Write("\n  Testing FileMgr class");
      Console.Write("\n =======================\n");
      FileMgr fm = new FileMgr();
      //fm.addPattern("*.cs");
      fm.findFiles("../../../");
      string[] files = fm.getFiles();
      foreach (string file in files)
        Console.Write("\n  {0}", Path.GetFileName(file));
      Console.Write("\n\n");
    }
#endif
  }
}
