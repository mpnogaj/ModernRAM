﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RAMEditorMultiplatform.Converters;
using RAMEditorMultiplatform.Helpers;
using RAMEditorMultiplatform.Models;

namespace RAMEditorMultiplatform.ViewModels
{
    public class HostViewModel : ViewModelBase
    {
        public CancellationTokenSource Token { get; set; }

        //private readonly HostViewModel _instance;
        //public HostViewModel Instance { get => _instance; }

        private ObservableCollection<MemoryCell> _memory;
        public ObservableCollection<MemoryCell> Memory { get => _memory; set { SetProperty(ref _memory, value); } }

        private string _header;
        public string Header { get => _header; set { SetProperty(ref _header, value); } }

        private string _outputTapeString;
        public string OutputTapeString { get => _outputTapeString; set { SetProperty(ref _outputTapeString, value); } }

        private string _inputTapeString;
        public string InputTapeString { get => _inputTapeString; set { SetProperty(ref _inputTapeString, value); } }

        private string _programString;
        public string ProgramString { get => _programString; set { SetProperty(ref _programString, value); } }

        private bool _programmRunning;
        public bool ProgrammRunning { get => _programmRunning; set { SetProperty(ref _programmRunning, value); } }

        private string _path;
        public string Path { get => _path; set { SetProperty(ref _path, value); } }

        private int _fontSize;
        public int FontSize { get => _fontSize; set { SetProperty(ref _fontSize, value); } }
        
        private SimpleEditorViewModel _simpleEditorViewModel;
        public SimpleEditorViewModel SimpleEditorViewModel { get => _simpleEditorViewModel; set { SetProperty(ref _simpleEditorViewModel, value); } }

        private bool _simpleEditorUsage = false;
        public bool SimpleEditorUsage { get => _simpleEditorUsage; set { SetProperty(ref _simpleEditorUsage, value); } }

        private readonly ParameterBaseCommand<HostViewModel> _closePage;
        public ParameterBaseCommand<HostViewModel> ClosePage { get => _closePage; }

        public HostViewModel()
        {
            //_instance = this;
            _memory = new ObservableCollection<MemoryCell>();
            _outputTapeString = string.Empty;
            _inputTapeString = string.Empty;
            _programString = string.Empty;
            _programmRunning = false;
            _fontSize = 13;
            _closePage = new(Logic.Logic.ClosePage, () => true);
            _simpleEditorViewModel = new SimpleEditorViewModel();
        }

        public void HandleEditorSwitch()
        {
            if(SimpleEditorUsage == true)
            {
                this.ProgramString = ProgramLineTextConverter.ToText(SimpleEditorViewModel.Program.ToList());
            }
            else
            {
                Task.Run(() => SimpleEditorViewModel.Program = new(ProgramLineTextConverter.ToProgramLines(this.ProgramString))).Wait();
            }
            SimpleEditorUsage = !SimpleEditorUsage;
        }
    }
}
