using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
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
using Amg.Build;
using Amg.Extensions;
using Amg.FileSystem;
using Amg.Plantuml;
using Amg.Util;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using Microsoft.Win32;

namespace Amg.EditPlantuml
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public string Text
        {
            get
            {
                return Source.Text;
            }
            set
            {
                Source.Text = value;
                UpdatePreview();
            }
        }

        public MainWindow()
        {
            plantuml = Amg.Plantuml.Plantuml.Local();
            InitializeComponent();
        }

        Amg.Plantuml.IPlantuml plantuml;

        string? previewedSource = null;
        MemoryStream? previewStream = null;

        System.Threading.SemaphoreSlim updateInProgress = new System.Threading.SemaphoreSlim(1, 1);

        async Task UpdatePreview()
        {
            await updateInProgress.WaitAsync();

            var source = Text;
            if (previewedSource is null || !previewedSource.Equals(source))
            {
                previewStream = new MemoryStream();
                var stream = previewStream;

                await plantuml.Convert(new StringReader(source), stream);
                stream.Seek(0, SeekOrigin.Begin);
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = stream;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                this.Preview.Source = bitmap;
            }

            previewedSource = source;

            updateInProgress.Release();
        }

        async Task<string> WriteOutputFile()
        {
            await UpdatePreview();
            await updateInProgress.WaitAsync();
            try
            {
                var filename = Util.Filename(DateTime.UtcNow, PlantumlTitle ?? "export");
                var outputFile = DocDir.Combine(filename + pngExtension);
                if (previewStream is null)
                {
                    throw new InvalidOperationException();
                }
                previewStream.Seek(0, SeekOrigin.Begin);
                using (var w = File.Open(outputFile.EnsureParentDirectoryExists(), FileMode.Create))
                {
                    await previewStream.CopyToAsync(w);
                }
                return outputFile;
            }
            finally
            {
                updateInProgress.Release();
            }
        }

        private async void Preview_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var data = new DataObject();
                var files = new StringCollection();
                var outputFile = await WriteOutputFile();
                files.Add(outputFile);
                data.SetFileDropList(files);
                DragDrop.DoDragDrop(this.Preview,
                    data,
                    DragDropEffects.Copy);
            }
        }

        private void Preview_DragEnter(object sender, DragEventArgs e)
        {
        }

        string pngExtension => ".png";

        string DocDir => System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments).Combine("edit-plantuml").EnsureDirectoryExists();

        async Task ImportPng(Stream content)
        {
            var filename = Util.Filename(DateTime.UtcNow, PlantumlTitle ?? "import");
            var pngFile = DocDir.Combine(filename + pngExtension);
            using (var w = File.Open(pngFile.EnsureParentDirectoryExists(), FileMode.Create))
            {
                await content.CopyToAsync(w);
            }

            var plantuml = Tools.Default.WithFileName("java.exe")
                .WithArguments("-jar", @"C:\ProgramData\chocolatey\lib\plantuml\tools\plantuml.jar");

            var r = await plantuml.DoNotCheckExitCode().Run("-metadata", pngFile);
            if (r.ExitCode == 0)
            {
                this.Text = r.Output;
            }
        }

        private async void Preview_Drop(object sender, DragEventArgs e)
        {
            try
            {
                var m = e.Data.GetFileContents();
                if (m is { })
                {
                    await ImportPng(m[0]);
                }
            }
            catch (Exception)
            {
                // MessageBox.Show("Cannot read dropped data.", "Error");
            }
        }

        private void Preview_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Copy;
        }

        IDisposable codeCompletion;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            codeCompletion = new PlantumlCodeCompletion(Source);
            Source.TextChanged += Source_TextChanged;
            Source.FontSize = 14;
            Source.FontFamily = new System.Windows.Media.FontFamily("Courier New");
            Source.TextArea.Document.TextChanged += Document_TextChanged;
            Source.Options.ConvertTabsToSpaces = false;
            Source.Options.ShowSpaces = true;
            Source.Options.IndentationSize = 2;
            Source.ShowLineNumbers = true;

            SetInitialSource();

            Source.Focus();
        }

        void SetInitialSource()
        {
            var dateTitle = DateTime.Now.ToString("yyyy-MM-dd HH:mm");

            this.Text = $@"@startuml

title {dateTitle}

actor actor
agent agent
artifact artifact
boundary boundary
card card
cloud cloud
component component
control control
database database
entity entity
file file
folder folder
frame frame
interface  interface
node node
package package
queue queue
stack stack
rectangle rectangle
storage storage
usecase usecase

@enduml";
            var s = Source.Document.Find(dateTitle);
            Source.SelectionStart = s.Offset;
            Source.SelectionLength = s.Length;
        }

        private void Document_TextChanged(object? sender, EventArgs e)
        {
            plantumlTitle = Parser.GetTitle(Source.Document.Text);
            if (PlantumlTitle is { })
            {
                this.Title = PlantumlTitle;
            }
        }

        string? plantumlTitle = null;

        public string? PlantumlTitle => plantumlTitle;

        CompletionWindow completionWindow;

        private async void Source_TextChanged(object? sender, EventArgs e)
        {
            await UpdatePreview();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            codeCompletion.Dispose();
        }

        private void New(object sender, RoutedEventArgs e)
        {
            var newWindow = new MainWindow();
            newWindow.Show();
        }
    }
}
