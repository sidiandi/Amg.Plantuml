using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.IO;
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
using Amg.Build;
using Amg.FileSystem;
using Amg.Plantuml;
using Amg.Util;

namespace Amg.EditPlantuml
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            plantuml = Amg.Plantuml.Plantuml.Local();
            InitializeComponent();

            this.Source.Text = @"@startuml
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
        }

        Amg.Plantuml.IPlantuml plantuml;
        Task update = null;

        string? previewedSource = null;
        MemoryStream previewStream = null;

        System.Threading.SemaphoreSlim updateInProgress = new System.Threading.SemaphoreSlim(1, 1);

        private async void Source_TextChanged(object sender, TextChangedEventArgs e)
        {
            await UpdatePreview();
        }

        async Task UpdatePreview()
        {
            await updateInProgress.WaitAsync();

            var source = this.Source.Text;
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

        async Task WriteOutputFile()
        {
            await UpdatePreview();

            await updateInProgress.WaitAsync();
            try
            {
                if (outputFileName is { })
                {
                    previewStream.Seek(0, SeekOrigin.Begin);
                    using (var w = File.Open(outputFileName.EnsureParentDirectoryExists(), FileMode.Create))
                    {
                        await previewStream.CopyToAsync(w);
                    }
                }
            }
            finally
            {
                updateInProgress.Release();
            }
        }

        string outputFileName = @"C:\temp\out.png";

        private async void Preview_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var data = new DataObject();
                var files = new StringCollection();
                await WriteOutputFile();
                files.Add(outputFileName);
                data.SetFileDropList(files);
                DragDrop.DoDragDrop(this.Preview,
                    data,
                    DragDropEffects.Copy);
            }
        }

        private void Preview_DragEnter(object sender, DragEventArgs e)
        {
        }

        string TempDir => typeof(MainWindow).GetProgramDataDirectory();

        async Task ImportPng(Stream content)
        {
            var pngFile = TempDir.Combine($"import-{DateTime.UtcNow.ToFileName()}.png");
            using (var w = File.Open(pngFile.EnsureParentDirectoryExists(), FileMode.Create))
            {
                await content.CopyToAsync(w);
            }

            var plantuml = Tools.Default.WithFileName("java.exe")
                .WithArguments("-jar", @"C:\ProgramData\chocolatey\lib\plantuml\tools\plantuml.jar");

            var r = await plantuml.DoNotCheckExitCode().Run("-metadata", pngFile);
            if (r.ExitCode == 0)
            {
                this.Source.Text = r.Output;
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
            catch (Exception ex)
            {
                MessageBox.Show("Cannot read dropped data.", "Error");
            }
        }

        private void Preview_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Copy;
        }
    }
}
