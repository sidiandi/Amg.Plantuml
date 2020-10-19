using Amg.Plantuml;
using GitVersion;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Amg.EditPlantuml
{
    sealed class PlantumlCodeCompletion : IDisposable
    {
        private static readonly Serilog.ILogger Logger = Serilog.Log.Logger.ForContext(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public void Dispose()
        {
            this.editor.TextChanged -= Editor_TextChanged;
            this.editor.TextArea.KeyDown -= TextArea_KeyDown;
        }

        readonly TextEditor editor;

        public PlantumlCodeCompletion(TextEditor editor)
        {
            this.editor = editor;
            this.editor.TextChanged += Editor_TextChanged;
            this.editor.TextArea.KeyDown += TextArea_KeyDown;

            sprites = LoadSpritesCompletionData();
        }

        async Task<IEnumerable<ICompletionData>> LoadSpritesCompletionData()
        {
            var m = await (new SpriteReader()).StdlibCached();
            return m.Select(_ => (ICompletionData)new SpriteCompletion(_)).ToList();
        }

        private void TextArea_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Space && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                StartAllElementsCompletion();
                e.Handled = true;
            }
        }

        static string? GetCurrentWord(TextEditor editor)
        {
            var segment = GetWordSegment(editor.Document, editor.CaretOffset);
            return editor.Document.GetText(segment);
        }

        static ISegment GetWordSegment(ITextSource document, int offset)
        {
            var end = offset;
            var wordBegin = end;
            for (; wordBegin > 0; --wordBegin)
            {
                var c = document.GetCharAt(wordBegin - 1);
                if (!Char.IsLetterOrDigit(c))
                {
                    break;
                }
            }
            return new TextSegment { StartOffset = wordBegin, EndOffset = end };
        }

        static IEnumerable<string> GetWords(ITextSource textSource)
        {
            using (var r = textSource.CreateSnapshot().CreateReader())
            {
                while (true)
                {
                    var line = r.ReadLine();
                    if (line == null) break;
                    foreach (var w in Regex.Split(line, @"[^\w]+"))
                    {
                        yield return w;
                    }
                }
            }
        }

        class CompletionData : ICompletionData
        {
            private readonly string word;

            public CompletionData(string word)
            {
                this.word = word;
            }

            public ImageSource Image => null;

            public string Text => word;

            public object Content => word;

            public object Description => null;

            public double Priority => 0.0;

            public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
            {
                var wordSegment = GetWordSegment(textArea.Document, completionSegment.EndOffset);
                textArea.Document.Replace(wordSegment, this.Text);
            }
        }

        class MacroCompletion : ICompletionData
        {
            private readonly Macro macro;

            public MacroCompletion(Macro macro)
            {
                this.macro = macro;
            }

            public ImageSource Image => null;

            public string Text => macro.Name;

            public object Content => macro.Name;

            public object Description => null;

            public double Priority => 0.0;

            public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
            {
                var wordSegment = GetWordSegment(textArea.Document, completionSegment.EndOffset);
                textArea.Document.Replace(wordSegment, $"!include <{macro.Include}>\r\n{macro.Name}{macro.Args}");
            }
        }

        class SpriteCompletion : ICompletionData
        {
            private readonly Sprite sprite;

            public SpriteCompletion(Sprite sprite)
            {
                this.sprite = sprite;
            }

            public ImageSource Image => null;

            public string Text => $"{sprite.Name} ({sprite.Include})";

            public object Content => $"{sprite.Name} ({sprite.Include})";

            public object Description => null;

            public double Priority => 0.0;

            public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
            {
                var wordSegment = GetWordSegment(textArea.Document, completionSegment.EndOffset);
                textArea.Document.Replace(wordSegment, $"rectangle \"<${sprite.Name}>\" as  {sprite.Name}");
                InsertInclude(textArea.Document, sprite.Include);
            }
        }

        static void InsertInclude(TextDocument document, string include)
        {
            var includes = GetIncludeSection(document);

            if (!document.GetText(includes).Contains(include, StringComparison.OrdinalIgnoreCase))
            {
                var includeLine = $"!include <{include}>\r\n";
                document.Insert(includes.EndOffset, includeLine);
            }
        }

        static int GetIndex<T>(IList<T> list, Func<T, bool> condition, int start = 0)
        {
            for (int i=start; i<list.Count;++i)
            {
                if (condition(list[i]))
                {
                    return i;
                }
            }
            return -1;
        }

        static ISegment GetIncludeSection(TextDocument document)
        {
            var lines = document.Lines;

            // find @startuml
            int includeStartLine = GetIndex(lines, _ => document.GetText(_).StartsWith("@startuml")) + 1;
            var includeEndLine = GetIndex(lines, _ => !document.GetText(_).StartsWith("!include"), includeStartLine);

            return new TextSegment
            {
                StartOffset = lines[includeStartLine].Offset,
                EndOffset = lines[includeEndLine].Offset
            };
        }

        private void Editor_TextChanged(object? sender, EventArgs e)
        {
            if (window is null)
            {
                StartExistingWordCompletion();
            }
            else
            {
                UpdateCompletion();
            }
        }

        void StartExistingWordCompletion()
        {
            if (HasCurrentWord)
            {
                StartCompletion(ExistingWordsCompletionData());
            }
        }

        bool HasCurrentWord
        {
            get
            {
                var s = GetWordSegment(editor.Document, editor.CaretOffset);
                return s.Length > 0;
            }
        }

        void StartAllElementsCompletion()
        {
            var candidates = Enumerable.Empty<ICompletionData>();

            if (sprites.IsCompleted)
            {
                candidates = candidates.Concat(sprites.Result);
            }

            StartCompletion(candidates);
        }

        IEnumerable<ICompletionData> ExistingWordsCompletionData()
        {
            var word = GetCurrentWord(editor);
            if (word is null)
            {
                word = String.Empty;
            }

            var candidates = GetWords(editor.Document).Where(_ => _.StartsWith(word) && _.Length > word.Length).Distinct()
                .Select(_ => (ICompletionData)new CompletionData(_))
                .ToList();

            return candidates;
        }

        void StartCompletion(IEnumerable<ICompletionData> candidates)
        {
            if (window is { })
            {
                window.Close();
            }

            this.currentCompletionCandidates = candidates;
            window = new CompletionWindow(editor.TextArea);
            window.Width = 600;
            window.Closed += Window_Closed;

            UpdateCompletion();
        }

        IEnumerable<ICompletionData>? currentCompletionCandidates = null;

        void UpdateCompletion()
        {
            if (window is null) return;

            var word = GetCurrentWord(this.editor);

            if (word is null)
            {
                word = String.Empty;
            }

            var data = window.CompletionList.CompletionData;
            data.Clear();
            foreach (var i in currentCompletionCandidates.Where(_ => _.Text.Contains(word)))
            {
                data.Add(i);
            }
            if (data.Count > 0)
            {
                window.Show();
            }
            else
            {
                window.Close();
            }
        }

        private void Window_Closed(object? sender, EventArgs e)
        {
            window = null;
        }

        CompletionWindow? window = null;
        readonly Task<IEnumerable<ICompletionData>> sprites;
    }
}
