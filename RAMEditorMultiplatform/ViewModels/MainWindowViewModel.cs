﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using RAMEditorMultiplatform.Helpers;
using System.Threading;
using Avalonia.Input;
using System.IO;
using Avalonia.Controls;
using Common;
using RAMEditorMultiplatform.Converters;
using RAMEditorMultiplatform.Models;

namespace RAMEditorMultiplatform.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private ObservableCollection<HostViewModel> _pages;

        public ObservableCollection<HostViewModel> Pages
        {
            get => _pages;
            set => SetProperty(ref _pages, value);
        }

        private HostViewModel? _page;

        public HostViewModel? Page
        {
            get => _page;
            set => SetProperty(ref _page, value);
        }

        private readonly RelayCommand _addPage;

        public RelayCommand AddPageCommand
        {
            get => _addPage;
        }

        private readonly AsyncRelayCommand _openFile;

        public AsyncRelayCommand OpenFile
        {
            get => _openFile;
        }

        private readonly RelayCommand _saveFileAs;

        public RelayCommand SaveFileAs
        {
            get => _saveFileAs;
        }

        private readonly RelayCommand _saveFile;

        public RelayCommand SaveFile
        {
            get => _saveFile;
        }

        private readonly AsyncRelayCommand _runProgram;

        public AsyncRelayCommand RunProgram
        {
            get => _runProgram;
        }

        private readonly RelayCommand _stopProgram;

        public RelayCommand StopProgram
        {
            get => _stopProgram;
        }

        private readonly RelayCommand _closeProgram;

        public RelayCommand CloseProgram
        {
            get => _closeProgram;
        }

        private readonly RelayCommand _increaseFontSize;

        public RelayCommand IncreaseFontSize
        {
            get => _increaseFontSize;
        }

        private readonly RelayCommand _decreaseFontSize;

        public RelayCommand DecreaseFontSize
        {
            get => _decreaseFontSize;
        }

        private readonly RelayCommand _switchEditors;

        public RelayCommand SwitchEditors
        {
            get => _switchEditors;
        }

        private readonly RelayCommand<string> _clear;

        public RelayCommand<string> Clear
        {
            get => _clear;
        }

        private readonly AsyncRelayCommand<string> _export;

        public AsyncRelayCommand<string> Export
        {
            get => _export;
        }

        private readonly AsyncRelayCommand _import;

        public AsyncRelayCommand Import
        {
            get => _import;
        }

        public MainWindowViewModel()
        {
            _pages = new ObservableCollection<HostViewModel>();
            _page = null;
            _addPage = new(() => CreateEmptyPage());
            _openFile = new(async () =>
            {
                var ofd = new OpenFileDialog
                {
                    Title = "Open file",
                    AllowMultiple = true,
                    Filters = Constant.RamcodeFilter
                };
                var files = await ofd.ShowAsync(Essentials.GetAppInstance().MainWindow);
                if (files == null)
                {
                    return;
                }

                CreateCodePagesFromFiles(files);
            });
            _saveFileAs = new(() => SaveCodeFileAs(Page!), () => IsFileOpened());
            _saveFile = new(() =>
            {
                if (string.IsNullOrEmpty(Page!.Path))
                {
                    SaveCodeFileAs(Page!);
                }
                else
                {
                    Essentials.WriteToFile(Page!.Path, Page!.ProgramString);
                }
            }, () => IsFileOpened());
            _closeProgram = new(Essentials.Exit, () => true);

            _increaseFontSize = new(() => Page!.FontSize += 1, IsFileOpened);
            _decreaseFontSize = new(() => Page!.FontSize -= 1, () => IsFileOpened() && Page!.FontSize > 1);

            _runProgram = new(async () =>
            {
                Page!.Token = new CancellationTokenSource();
                try
                {
                    Essentials.SetCursor(StandardCursorType.Wait);
                    Page.ProgramRunning = true;
                    await Task.Run(() => { CreateAndRunProgram(Page.Token.Token); });
                }
                catch (OperationCanceledException)
                {
                    /*Ignore*/
                }
                finally
                {
                    Page.ProgramRunning = false;
                    Essentials.SetCursor(StandardCursorType.Arrow);
                }
            }, () => IsFileOpened() && !IsProgramRunning());

            _stopProgram = new(() => { Page!.Token!.Cancel(); }, () => IsFileOpened() && IsProgramRunning());

            _switchEditors = new(() => { Page!.HandleEditorSwitch(); }, () => IsFileOpened() && !IsProgramRunning());

            _clear = new((target) =>
            {
                switch (target)
                {
                    case "memory":
                        Page!.Memory = new();
                        break;
                    case "inputTape":
                        Page!.InputTapeString = string.Empty;
                        break;
                    case "outputTape":
                        Page!.OutputTapeString = string.Empty;
                        break;
                }
            }, () => IsFileOpened() && !IsProgramRunning());

            _import = new(async () =>
            {
                OpenFileDialog ofd = new()
                {
                    AllowMultiple = false,
                    Title = "Open memory file",
                    Filters = Constant.TextFileFilter
                };

                var res = await ofd.ShowAsync(Essentials.GetAppInstance().MainWindow);
                if (res.Length > 0)
                {
                    string content = Essentials.ReadFromFile(res[0]);
                    Page!.InputTapeString = content.Replace(",", "");
                }
            }, () => IsFileOpened() && !IsProgramRunning());

            _export = new(async (target) =>
            {
                string content;
                switch (target)
                {
                    case "memory":
                        content = MemoryRowToStringConverter.MemoryRowsToString(new(Page!.Memory));
                        break;
                    case "inputTape":
                        content = Page!.InputTapeString.Replace(" ", ", ");
                        break;
                    case "outputTape":
                        content = Page!.OutputTapeString.Replace(" ", ", ");
                        break;
                    default:
                        return;
                }

                SaveFileDialog sfd = new()
                {
                    Title = "Save file",
                    InitialFileName = $"{Page!.Header}_{target}",
                    Filters = Constant.TextFileFilter
                };

                var res = await sfd.ShowAsync(Essentials.GetAppInstance().MainWindow);
                if (!string.IsNullOrEmpty(res))
                {
                    Essentials.WriteToFile(res, content);
                }
            }, () => IsFileOpened() && !IsProgramRunning());

            CreateEmptyPage();
        }

        private bool IsFileOpened()
        {
            return Page != null;
        }

        private bool IsProgramRunning()
        {
            return Page != null && Page.ProgramRunning;
        }

        public void FileOver(object? sender, DragEventArgs e)
        {
            bool dropEnabled = false;
            if (e.Data.GetDataFormats().Any(format => format == DataFormats.FileNames))
            {
                if (e.Data.GetFileNames() != null &&
                    e.Data.GetFileNames()!.Any(name => Path.GetExtension(name) == ".RAMCode"))
                {
                    dropEnabled = true;
                }
            }

            if (dropEnabled) return;
            e.DragEffects = DragDropEffects.None;
            e.Handled = true;
        }

        public void FileDropped(object? sender, DragEventArgs e)
        {
            string[] files = e.Data.GetFileNames()!.Where(fileName => Path.GetExtension(fileName) == ".RAMCode")
                .ToArray();
            CreateCodePagesFromFiles(files);
        }

        private void CreateEmptyPage(string header = Constant.DefaultHeader)
        {
            Pages.Add(new HostViewModel(header));
        }

        private void CreatePageWithCode(string code, string header = Constant.DefaultHeader)
        {
            Pages.Add(new HostViewModel(header)
            {
                ProgramString = code
            });
        }

        public void CreateCodePagesFromFiles(string[] files)
        {
            foreach (string file in files)
            {
                if (!string.IsNullOrWhiteSpace(file))
                {
                    CreatePageWithCode(Essentials.ReadFromFile(file), Path.GetFileNameWithoutExtension(file));
                }
            }
        }

        private async void SaveCodeFileAs(HostViewModel? page)
        {
            var file = page ?? Page!;
            var sfd = new SaveFileDialog
            {
                InitialFileName = file.Header,
                Title = "Save file",
                Filters = Constant.RamcodeFilter
            };

            var res = await sfd.ShowAsync(Essentials.GetAppInstance().MainWindow);
            if (!string.IsNullOrEmpty(res))
            {
                file.Path = res;
                Essentials.WriteToFile(res, file.ProgramString);
            }
        }
        
        private void CreateAndRunProgram(CancellationToken token)
        {
            var ct = token;
            HostViewModel host = Page!;
            string input = host.InputTapeString;
            string program = host.ProgramString;
            List<Command> commands;
            if (host.SimpleEditorUsage)
            {
                commands = ProgramLineToCommandConverter.ProgramLinesToCommands(host.Program.ToList());
            }
            else
            {
                var sc = new StringCollection();
                sc.AddRange(program.Split('\n'));
                commands = Creator.CreateCommandList(sc);
            }  
            ct.ThrowIfCancellationRequested();
            Interpreter.RunCommands(commands, Creator.CreateInputTapeFromString(input), ct);
            ct.ThrowIfCancellationRequested();
            Queue<string> output = Interpreter.OutputTape;
            string finalOutput = "";
            foreach (string s in output)
            {
                finalOutput += s + " ";
            }
            finalOutput = finalOutput.Trim();
            host.OutputTapeString = finalOutput;
            host.Memory.Clear();
            var newMemory = new ObservableCollection<MemoryRow>();
            foreach (var (add, value) in Interpreter.Memory)
            {
                newMemory.Add(new MemoryRow
                {
                    Address = add,
                    Value = value
                });
            }
            host.Memory = newMemory;
        }
    }
}