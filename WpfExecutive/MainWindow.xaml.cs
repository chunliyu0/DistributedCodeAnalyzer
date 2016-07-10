///////////////////////////////////////////////////////////////////////////
// MainWindow.Xaml.cs - C# Code Analysis Form View                       //
//                                                                       //
// Platform:    Dell 2720, Windows 8.1                                   //
// Language:    Visual 2013 C#                                           //
// Application: Analysis of C# Source Code                               //
// Author:      Jim Fawcett, CST 4-187, Syracuse University              //
///////////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * -------------------
 * Displays C# Code Analysis results in either full or summary views.
 * Also supports printing.
 * 
 * Displays size and complexity metrics where:
 * - Size is number of lines for complete scope including opening 
 *   and closing braces.
 * - Complexity is the number of scopes for the cited item, including
 *   it's own scope, e.g., minimum complexity is 1.
 * - Doesn't yet handle braceless scopes.
 * 
 * Required Files and modules:
 * ---------------------------
 * MainWindow.xaml, MainWindow.xaml.cs, FileMgr.cs, Analyzer.cs
 * Parser ver 1.5
 * 
 * Build Command:
 * --------------
 * devenv Parser-Fall14.sln /rebuild debug
 * 
 * Maintenance History:
 * --------------------
 * Ver 1.1 : 13 Oct 2014
 * - cleaned up code
 * - added child thread for analysis
 * - added Display package
 * - fixed bug in Tokenizer for multi-line strings
 * Ver 1.0 : 10 Oct 2014
 * - first release
 * 
 * To Do:
 * ------
 * - Count braceless scopes in complexity analysis
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;
using System.Printing;
using System.IO;
using System.Threading;
using CodeAnalysis;

namespace WpfExecutive
{
  public partial class MainWindow : Window
  {
    FolderBrowserDialog dlg = new FolderBrowserDialog();
    string selectedPath = "";
    object sync = new object();
    MemoryStream stream;
    System.Windows.Controls.PrintDialog printDialog = new System.Windows.Controls.PrintDialog();
    
    //----< write out view header and footer >-----------------------------

    public MainWindow()
    {
      InitializeComponent();
      listBox0.Items.Add(
        new { 
          itemFileName = "File Name", itemMethodName = "Analysis Item", itemStart="Line", 
          itemSize = "Size", itemComplexity = "Cmplx" 
        }
      );
      listBox2.Items.Add(
        new {}
      );

      Display.showDirectories = true; 
      dirsCkBx.IsChecked = true;
      Display.showFiles = true;
      filesCkBx.IsChecked = true;
      Display.showActions = false;
      actionsCkBx.IsChecked = false;
      Display.showRules = false;
      rulesCkBx.IsChecked = false;
      Display.useFooter = true;
      footerCkBx.IsChecked = true;
      Display.useConsole = true;
      consoleCkBx.IsChecked = true;
      Display.showSemi = true;
      semiCkBx.IsChecked = true;
      Display.goSlow = false;
      goSlowCkBx.IsChecked = false;
      Display.width = 70;

      textBox1.Text = Properties.Settings.Default.Path;
    }

    void writeFooter(string text)
    {
      listBox2.Items.Clear();
      listBox2.Items.Add(new { itemFileName = text });
    }

    void ChildWriteFooter(string str)
    {
      Action act = () => writeFooter(str);
      Dispatcher.Invoke(act, System.Windows.Threading.DispatcherPriority.Background);
    }

    void addAnalPublic(AnalDataPublic ad)
    {
      listBox1.Items.Add(ad);
    }
    void addAnalItem(AnalDataItem ad)
    {
      listBox1.Items.Add(ad);
    }

    void enableAllButtons()
    {
      browseButton.IsEnabled = true;
      analyzeButton.IsEnabled = true;
      summaryButton.IsEnabled = true;
      printButton.IsEnabled = true;
    }
    //----< Browse for analysis root folder >------------------------------

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
      if (selectedPath == "")
        selectedPath = Directory.GetCurrentDirectory();
      listBox1.Items.Clear();
      listBox2.Items[0] = new object { };
      dlg.ShowNewFolderButton = false;
      dlg.Description = "\n  Select Analysis Folder";
      dlg.SelectedPath = selectedPath;
      DialogResult dr = dlg.ShowDialog();
      if (dr.ToString() == "OK")
      {
        selectedPath = textBox1.Text = dlg.SelectedPath;
        Properties.Settings.Default.Path = selectedPath;
        Properties.Settings.Default.Save();
      }
    }
    //----< Run full analysis on child thread >------------------------

    private void AnalyzeButton_Click(object sender, RoutedEventArgs ev)
    {
      analyzeButton.IsEnabled = false;
      summaryButton.IsEnabled = false;
      listBox1.Items.Clear();
      listBox2.Items[0] = new { itemFileName = "Getting and Analyzing files" };

      Thread t = new Thread(new ParameterizedThreadStart(ThreadProcAnalyze));
      t.IsBackground = true;
      t.Start(textBox1.Text);

    }
    //----< Run summary analysis on child thread >---------------------

    private void SummaryButton_Click(object sender, RoutedEventArgs ea)
    {
      summaryButton.IsEnabled = false;
      analyzeButton.IsEnabled = false;
      listBox1.Items.Clear();
      listBox2.Items[0] = new { itemFileName = "Getting and analyzing files" };

      Thread t = new Thread(new ParameterizedThreadStart(ThreadProcSummary));
      t.IsBackground = true;
      t.Start(textBox1.Text);
    }
    //----< do analysis and write all important results to listBox >-------

    void ThreadProcAnalyze(object path)
    {
      setupCallbacks();

      if (!analyze(path))
        return;

      packageAndSendResults();
      Action actFooter = () => writeFooter("Finished");
      Dispatcher.Invoke(actFooter);
      Dispatcher.Invoke(() => enableAllButtons());
    }
    //----< setup child thread communication with MainWindow >---------

    private void setupCallbacks()
    {
      CodeAnalysis.Analyzer.act = (string str) => ChildWriteFooter(str);
      CodeAnalysis.FileMgr.act = (string str) => ChildWriteFooter(str);
      CodeAnalysis.AAction.actionDelegate = (string str) => ChildWriteFooter(str);
      CodeAnalysis.ARule.actionDelegate = (string str) => ChildWriteFooter(str);
    }
    //----< run Analyzer with error handling >-------------------------

    private static bool analyze(object path)
    {
      try
      {
        CodeAnalysis.Analyzer.doAnalysis(CodeAnalysis.Analyzer.getFiles(path as string));
        return true;
      }
      catch (Exception exc)
      {
        Console.Write("\n  {0}\n\n", exc.Message);
        return false;
      }
    }
    //----< Write full analysis items to MainWindow ListBox >----------

    private void packageAndSendResults()
    {
      Repository rep = Repository.getInstance();

      foreach (string key in rep.LocationsTable.Keys)
      {
        string filename = System.IO.Path.GetFileName(key);

        foreach (Elem e in rep.LocationsTable[key])
        {
          if (e.type.Contains("public"))
          {
            AnalDataPublic ad = new AnalDataPublic();
            ad.itemFileName = filename.Truncate(33);
            ad.itemMethodName = (e.type + " " + e.name).Truncate(33);
            ad.itemStart = e.beginLine;
            Action act = () => addAnalPublic(ad);
            Dispatcher.Invoke(act);
          }
          else
          {
            int size = e.endLine - e.beginLine + 1;
            int complexity = e.endScopeCount - e.beginScopeCount + 1;
            string item = String.Format("{0,25}  {1,-25} {2,7} {3,7}", filename, e.name, size, complexity);
            AnalDataItem ai = new AnalDataItem();
            ai.itemFileName = filename.Truncate(33);
            ai.itemMethodName = (e.type + " " + e.name).Truncate(33);
            ai.itemStart = e.beginLine;
            ai.itemSize = size;
            ai.itemComplexity = complexity;
            Action actItem = () => addAnalItem(ai);
            Dispatcher.Invoke(actItem);
          }
        }
      }
    }
    //----< do analysis and collect violations for summary output >--------

    void ThreadProcSummary(object path)
    {
      setupCallbacks();

      if(!analyze(path))
        return;

      packageAndSendSummary();
      Action footerAct = () => writeFooter("Finished");
      Dispatcher.Invoke(footerAct, System.Windows.Threading.DispatcherPriority.Render);
      Dispatcher.Invoke(() => enableAllButtons());
    }
    //----< write summary analysis items to MainWindow ListBox >-------

    private void packageAndSendSummary()
    {
      Repository rep = Repository.getInstance();

      foreach (string key in rep.LocationsTable.Keys)
      {
        string filename = System.IO.Path.GetFileName(key);
        foreach (Elem e in rep.LocationsTable[key])
        {
          if (e.type.Contains("public"))
          {
            AnalDataPublic ad = new AnalDataPublic();
            ad.itemFileName = filename.Truncate(33);
            ad.itemMethodName = (e.type + " " + e.name).Truncate(33);
            ad.itemStart = e.beginLine;
            Action act = () => addAnalPublic(ad);
            Dispatcher.Invoke(act);
          }
          if (e.type == "function")
          {

            int size = e.endLine - e.beginLine + 1;
            int complexity = e.endScopeCount - e.beginScopeCount + 1;
            if (size > 50 || complexity > 10)
            {
              AnalDataItem ai = new AnalDataItem();
              ai.itemFileName = filename.Truncate(33);
              ai.itemMethodName = e.name.Truncate(33);
              ai.itemStart = e.beginLine;
              ai.itemSize = size;
              ai.itemComplexity = complexity;
              Action act = () => addAnalItem(ai);
              Dispatcher.Invoke(act);
            }
          }
        }
      }
    }

    //----< Transfer results from ListBox to FlowDocument then print >-----

    struct PrintDialogInfo
    {
      public double height;
      public double width;
    }
    private void PrintButton_Click(object sender, RoutedEventArgs e)
    {
      Thread t = null;
      bool? dr = printDialog.ShowDialog();  // PrintDialog must be used on main UI Thread
      if (dr == true)
      {
        PrintDialogInfo pi;
        pi.height = printDialog.PrintableAreaHeight;
        pi.width = printDialog.PrintableAreaWidth;

        t = new Thread(new ParameterizedThreadStart(ThreadProcPrint));
        t.IsBackground = true;
        t.Start(pi);
      }
    }

    void ThreadProcPrint(object pi)
    {
      // Here's how you print a visual:
      //   System.Windows.Controls.PrintDialog pd = new System.Windows.Controls.PrintDialog();
      //   pd.PrintVisual(listBox1, "printing");

      FlowDocument fd = new System.Windows.Documents.FlowDocument();
      buildFlowDocumentHeader(fd);
      buildFlowDocumentBody(fd);
      printFlowDocument(fd, (PrintDialogInfo)pi);
    }

    private void buildFlowDocumentHeader(FlowDocument fd)
    {
      string headerStr = "Code Analysis Results  ";
      Paragraph pr = new Paragraph(new Run(headerStr));
      pr.FontFamily = new System.Windows.Media.FontFamily("Tahoma");
      pr.FontSize = 16.0;
      pr.FontWeight = FontWeights.Bold;
      pr.LineHeight = 10;
      fd.Blocks.Add(pr);

      headerStr = DateTime.Now.ToString();
      pr = new Paragraph(new Run(headerStr));
      pr.FontFamily = new System.Windows.Media.FontFamily("Tahoma");
      pr.FontSize = 14.0;
      pr.FontWeight = FontWeights.Normal;
      pr.LineHeight = 10;
      fd.Blocks.Add(pr);

      headerStr = dlg.SelectedPath;
      pr = new Paragraph(new Run(headerStr));
      pr.FontFamily = new System.Windows.Media.FontFamily("Tahoma");
      pr.FontSize = 14.0;
      pr.FontWeight = FontWeights.Normal;
      pr.LineHeight = 10;
      fd.Blocks.Add(pr);

      pr = new Paragraph(new Run(new string('-', 100)));
      pr.LineHeight = 18;
      fd.Blocks.Add(pr);

      string itemStr;
      itemStr = String.Format("{0,35}  {1,-35}  {2,7}  {3,7}  {4,7}", "FileName", "Method Name", "Start", "Size", "Cmplx");
      fd.Blocks.Add(new Paragraph(new Run(itemStr)));
      itemStr = String.Format("{0,35}  {1,-35}  {2,7}  {3,7}  {4,7}", "--------", "-----------", "-----", "----", "-----");
      fd.Blocks.Add(new Paragraph(new Run(itemStr)));
    }

    private void buildFlowDocumentBody(FlowDocument fd)
    {
      string itemStr;
      foreach (object item in listBox1.Items)
      {
        dynamic pi = item;
        string fileName = pi.itemFileName;
        string methodName = pi.itemMethodName;
        int start = pi.itemStart;
        if (!methodName.Contains("public"))
        {
          int size = pi.itemSize;
          int complexity = pi.itemComplexity;
          itemStr = String.Format("{0,35}  {1,-35}  {2,7}  {3,7}  {4,7}", fileName, methodName, start, size, complexity);
        }
        else
        {
          itemStr = String.Format("{0,35}  {1,-35}  {2,7}", fileName, methodName, start);
        }
        fd.Blocks.Add(new Paragraph(new Run(itemStr)));
      }
    }
    //----< WPF text printing is based on FlowDocuments >--------------

    private void printFlowDocument(FlowDocument fd, PrintDialogInfo pi)
    {
      IDocumentPaginatorSource idoc = fd as IDocumentPaginatorSource;
      fd.FontFamily = new System.Windows.Media.FontFamily("Consolas");
      fd.FontSize = 12.0;
      fd.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;  // enable reducing line height
      fd.LineHeight = 8;
      fd.PageHeight = pi.height;
      fd.PageWidth = pi.width;
      fd.ColumnGap = 0;
      fd.PagePadding = new Thickness(75, 75, 75, 100);
      fd.ColumnWidth = (fd.PageWidth - fd.ColumnGap - fd.PagePadding.Left - fd.PagePadding.Right);

      // Can't pass FlowDocuments between threads, so we have to serialize

      stream = new MemoryStream();
      System.Windows.Markup.XamlWriter.Save(fd, stream);
      stream.Position = 0;
      
      Action act = () => printFromStream(stream);
      Dispatcher.Invoke(act);
    }
    //----< prints FlowDocument deserialized from MemoryStream >-------
    void printFromStream(MemoryStream stream)
    {
      FlowDocument fd = (FlowDocument)System.Windows.Markup.XamlReader.Load(stream);
      IDocumentPaginatorSource idoc = fd as IDocumentPaginatorSource;
      printDialog.PrintDocument(idoc.DocumentPaginator, "printing analysis");
    }
    //----< event handler for any checkbox click >---------------------

    private void filesCkBx_Click(object sender, RoutedEventArgs e)
    {
      Display.showDirectories = (bool)dirsCkBx.IsChecked;
      Display.showFiles = (bool)filesCkBx.IsChecked;
      Display.showRules = (bool)rulesCkBx.IsChecked;
      Display.showActions = (bool)actionsCkBx.IsChecked;
      Display.showSemi = (bool)semiCkBx.IsChecked;
      Display.useConsole = (bool)consoleCkBx.IsChecked;
      Display.useFooter = (bool)footerCkBx.IsChecked;
      Display.goSlow = (bool)goSlowCkBx.IsChecked;
    }
  }

  //----< extension method to truncate strings
  public static class StringExt
  {
    public static string Truncate(this string value, int maxLength)
    {
      if (string.IsNullOrEmpty(value)) return value;
      return value.Length <= maxLength ? value : value.Substring(0, maxLength);
    }
  }
  class AnalDataItem
  {
    public string itemFileName { get; set; }
    public string itemMethodName { get; set; }
    public int itemStart { get; set; }
    public int itemSize { get; set; }
    public int itemComplexity { get; set; }
  }
  class AnalDataPublic
  {
    public string itemFileName { get; set; }
    public string itemMethodName { get; set; }
    public int itemStart { get; set; }
  }
}
