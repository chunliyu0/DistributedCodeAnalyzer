/////////////////////////////////////////////////////////////////////////
// Analyzer.cs  -  Type and Function analysis of C# Source Code        //
// ver 1.0                                                             //
// Language:    C#, Visual Studio 2013, .Net Framework 4.5             //
// Platform:    Dell XPS 2720 , Win 8.1 Pro                            //
// Application: Pr#2 Help, CSE681, Fall 2014                           //
// Author:      Jim Fawcett, CST 2-187, Syracuse University            //
//              (315) 443-3948, jfawcett@twcny.rr.com                  //
/////////////////////////////////////////////////////////////////////////
/*
 * Package Operations
 * ==================
 * Analyzer finds all the defined types in some set of C# source code
 * and computes size and complexity metrics for the types and their
 * methods.
 * 
 * Public Interface
 * ================
 * getFiles(path)      // recursively finds C# source files on specified path
 * doAnalysis(files);  // analyzes each file in the files array
 */
/*
 * Build Process
 * =============
 * Required Files:
 *   Analyzer.cs
 *   
 * Required References:
 *   Parser, FileMgr
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

namespace CodeAnalysis
{
  public class Analyzer
  {
    static public Action<string> act;

    static public string[] getFiles(string path)
    {
      FileMgr fm = new FileMgr();
      fm.addPattern("*.cs");
      fm.findFiles(path);
      return fm.getFiles();
    }
    static public void doAnalysis(string[] files)
    {
      int skipCount = 0;

      CSsemi.CSemiExp semi = new CSsemi.CSemiExp();
      semi.displayNewLines = false;

      BuildCodeAnalyzer builder = new BuildCodeAnalyzer(semi);
      Parser parser = builder.build();
      Repository rep = Repository.getInstance();

      foreach (string file in files)
      {
        if(Display.showFiles)
          Display.displayString("");
        Display.displayFiles(act, "Processing file: " + file);

        if (!semi.open(file as string))
        {
          Console.Write("\n  Can't open {0}\n\n", file);
          return;
        }

        try
        {
          while (semi.getSemi())
          {
            parser.parse(semi);
          }
        }
        catch (Exception ex)
        {
          Console.Write("\n\n  {0}\n", ex.Message);
        }
        List<Elem> table = rep.locations;
        if (table.Count == 0)
        {
          ++skipCount;
          continue;
        }

        semi.close();
        rep.LocationsTable[file] = table;
        rep.locations = new List<Elem>();
      }

      displaySkipped(skipCount);
      displayAnalysis(rep);
      analysisSummary(rep);
    }

    private static void displaySkipped(int skipCount)
    {
      Display.displayString("");
      Display.displayString(act, "skipped " + skipCount.ToString() + " files with no Type definitions");
      Display.displayString("\n");

      Display.displayString("Stored Repository Data:");
    }

    private static void displayAnalysis(Repository rep)
    {
      string dispStr = "";

      foreach (string key in rep.LocationsTable.Keys)
      {
        dispStr = String.Format("\n  {0}", key);
        Display.displayString(dispStr);
        dispStr = String.Format(
            "\n    {0,25} {1,35} {2,5} {3,5} {4,5}",
            "category", "name", "bLine", "size", "cmplx"
        );
        Display.displayString(dispStr);
        dispStr = String.Format(
            "  {0,25} {1,35} {2,5} {3,5} {4,5}",
            "------------------", "-----------------------", "-----", "----", "-----"
        );
        Display.displayString(dispStr);

        foreach (Elem e in rep.LocationsTable[key])
        {
          if (e.type == "class" || e.type == "interface")
            Display.displayString("");
          dispStr = String.Format(
            "  {0,25} {1,35} {2,5} {3,5} {4,5}",
            e.type, e.name, e.beginLine,
            e.endLine - e.beginLine + 1, e.endScopeCount - e.beginScopeCount + 1
          );
          Display.displayString(dispStr);
        }
      }
    }

    private static void analysisSummary(Repository rep)
    {
      Console.Write("\n\n");
      Console.Write("\n  Analysis Summary - not counting braceless scopes yet");
      Console.Write("\n ======================================================");

      string dispStr;
      foreach (string key in rep.LocationsTable.Keys)
      {
        string filename = System.IO.Path.GetFileName(key);
        foreach (Elem e in rep.LocationsTable[key])
        {
          if (e.type.Contains("public"))
          {
            dispStr = String.Format("  {0,25}   {1}", filename, e.type);
            Display.displayString(dispStr);
          }
          if (e.type == "function")
          {
            int size = e.endLine - e.beginLine + 1;
            int complexity = e.endScopeCount - e.beginScopeCount + 1;
            if (size > 50 || complexity > 10)
            {
              dispStr = String.Format("  {0,25}   {1,-35} {2,5} {3,5}", filename, e.name, size, complexity);
              Display.displayString(dispStr);
            }
          }
        }
      }
      Console.Write("\n\n");
      //return dispStr;
    }
    public static void ShowCommandLine(string[] args)
    {
      Console.Write("\n  Commandline args are:\n  ");
      foreach (string arg in args)
      {
        Console.Write("  {0}", arg);
      }
      Console.Write("\n  current directory: {0}", System.IO.Directory.GetCurrentDirectory());
      Console.Write("\n");
    }
#if(TEST_ANALYZER)
    static void Main(string[] args)
    {
      string path;
      ShowCommandLine(args);
      if (args.Length > 0)
        path = args[0];
      else
        path = "../../../";
      try
      {
        doAnalysis(getFiles(path));
      }
      catch
      {
        Console.Write("\n  Can't open file {0}\n\n", path);
        return;
      }
    }
#endif
  }
}
