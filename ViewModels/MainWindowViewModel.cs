using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using RazorCX.FindMissingDrawings.Models;
using Tekla.Structures;
using Tekla.Structures.Model;

namespace RazorCX.FindMissingDrawings.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
	    private readonly Model _model = new Model();
	    private string _oldId;
	    private string _newId;
	    private MissingDrawingFinder _missingDrawingFinder;
	    private TeklaHandler _teklaHandler;
	    private List<PartSummary> _allPartSummaries;

		private string _title = "Missing Drawings Finder by RazorCX Technologies Inc.";
	    public string Title
	    {
		    get => _title;
		    set => SetProperty(storage: ref _title, value: value);
	    }

	    private string _appLabel = "Missing Drawings Finder";
	    public string AppLabel
	    {
		    get => _appLabel;
		    set => SetProperty(storage: ref _appLabel, value: value);
	    }

	    private ProjectInfo _projectInfo;
	    public ProjectInfo ProjectInfo
	    {
		    get => _projectInfo;
		    set => SetProperty(storage: ref _projectInfo, value: value);
	    }

	    private ModelInfo _modelInfo;
	    public ModelInfo ModelInfo
	    {
		    get => _modelInfo;
		    set => SetProperty(storage: ref _modelInfo, value: value);
	    }

	    private ObservableCollection<PartSummary> _partSummaries;
	    public ObservableCollection<PartSummary> PartSummaries
	    {
		    get => _partSummaries;
		    set
		    {
			    SetProperty(storage: ref _partSummaries, value: value);
			    MissingAssemblyDrawings = PartSummaries.Count(predicate: p => p.MainPart > 0);
			    MissingPartDrawings = PartSummaries.Count(predicate: p => p.MainPart <1);
			}
		}

	    private ObservableCollection<string> _statusLog;
	    public ObservableCollection<string> StatusLog
	    {
		    get => _statusLog;
		    set => SetProperty(storage: ref _statusLog, value: value);
	    }

	    private string _statusLogRichText;
	    public string StatusLogRichText
		{
		    get => _statusLogRichText;
		    set => SetProperty(storage: ref _statusLogRichText, value: value);
	    }

		private PartSummary _selectedRow;
	    public PartSummary SelectedRow
	    {
		    get => _selectedRow;
		    set
		    {
			    SetProperty(storage: ref _selectedRow, value: value);

			    if (value == null) return;
			    if (string.IsNullOrEmpty(value: value.Guid)) return;

			    var identifier = _model.GetIdentifierByGUID(guid: value.Guid);
			    var modelObject = _model.SelectModelObject(ID: identifier);
			    var modelObjects = new List<ModelObject> { modelObject };
			    //HighlightMembersInModel(modelObjects, true);
		    }
	    }

	    private object _selectedValue;
	    public object SelectedValue
		{
		    get => _selectedValue;
		    set => SetProperty(storage: ref _selectedValue, value: value);
	    }

		private DisplayOptions _displayOptions;
	    public DisplayOptions DisplayOptions
	    {
		    get => _displayOptions;
		    set => SetProperty(storage: ref _displayOptions, value: value);
	    }

	    private string _zoomSelectedQty;
		public string ZoomSelectedQty
		{
			get => string.IsNullOrEmpty(value: _zoomSelectedQty) ? $"Zoom Selected (0)" : $"Zoom Selected ({_zoomSelectedQty})";
			set => SetProperty(storage: ref _zoomSelectedQty, value: value);
		}

		private string _message;
		public string Message
		{
			get => _message;
			set => SetProperty(storage: ref _message, value: value);
		}

		private bool _isMessageVisible;
		public bool IsMessageVisible
		{
			get => _isMessageVisible;
			set => SetProperty(storage: ref _isMessageVisible, value: value);
		}

		private bool _isDataGridVisible;
		public bool IsDataGridVisible
		{
			get => _isDataGridVisible;
			set => SetProperty(storage: ref _isDataGridVisible, value: value);
		}

		private bool _isSpinnerVisible;
		public bool IsSpinnerVisible
		{
			get => _isSpinnerVisible;
			set => SetProperty(storage: ref _isSpinnerVisible, value: value);
		}

		private ObservableCollection<PartSummary> _selectedRows;
		public ObservableCollection<PartSummary> SelectedRows
		{
			get => _selectedRows;
			set
			{
				SetProperty(storage: ref _selectedRows, value: value);
				ZoomSelectedQty = SelectedRows.Count.ToString();
			}
		}

		private ObservableCollection<string> _phases;
		public ObservableCollection<string> Phases
		{
			get => _phases;
			set => SetProperty(ref _phases, value);
		}

		private string _selectedPhaseValue = "ALL";
		public string SelectedPhaseValue
		{
			get => _selectedPhaseValue;
			set
			{
				SetProperty(ref _selectedPhaseValue, value);
				FilterPartSummaries();
				//if (SelectedPhaseValue == "ALL")
				//	ColorParts();
			}
		}

		private ObservableCollection<string> _materialTypes;
		public ObservableCollection<string> MaterialTypes
		{
			get => _materialTypes;
			set => SetProperty(ref _materialTypes, value);
		}

		private string _selectedMaterialTypeValue = "ALL";
		public string SelectedMaterialTypeValue
		{
			get => _selectedMaterialTypeValue;
			set
			{
				SetProperty(ref _selectedMaterialTypeValue, value);
				FilterPartSummaries();
				//if(SelectedMaterialTypeValue == "ALL")
				//	ColorParts();
			}
		}
		
		private int _missingAssemblyDrawings = 0;
		public int MissingAssemblyDrawings
		{
			get => _missingAssemblyDrawings;
			set => SetProperty(storage: ref _missingAssemblyDrawings, value: value);
		}

		private int _missingPartDrawings = 0;
		public int MissingPartDrawings
		{
			get => _missingPartDrawings;
			set => SetProperty(storage: ref _missingPartDrawings, value: value);
		}

		private string _webViewLink;
		public string WebViewLink
		{
			get => _webViewLink;
			set => SetProperty(storage: ref _webViewLink, value: value);
		}

		public List<ModelObject> ModelObjects { get; set; }

		private readonly IEventAggregator _eventAggregator;

		public DelegateCommand FindMissingDrawingsCommand { get; set; }
		public DelegateCommand ZoomSelectedCommand { get; set; }
		public DelegateCommand<object> SelectionChangedCommand { get; set; }
		public DelegateCommand MissingAssemblyDrawingsCommand { get; set; }
		public DelegateCommand MissingPartDrawingsCommand { get; set; }
		public DelegateCommand RefreshCommand { get; set; }

		public MainWindowViewModel(IEventAggregator eventAggregator)
		{
			if (!TeklaStructures.Connect() || !new Model().GetConnectionStatus())
			{
				Process.GetCurrentProcess().Kill();
			}

			_eventAggregator = eventAggregator;
			_missingDrawingFinder = new MissingDrawingFinder();
			_teklaHandler = new TeklaHandler();
			_allPartSummaries = new List<PartSummary>();

			WebViewLink = "https://stackoverflow.com/questions/52876813/open-local-html-or-pdf-file-in-wpf-web-view";

			ModelInfo = _model.GetInfo();
			ProjectInfo = _model.GetProjectInfo();
			StatusLog = new ObservableCollection<string>();

			//AppLabel = "Missing Drawings Finder | " + ProjectInfo.Name + " | " + ProjectInfo.ProjectNumber;

			Directory.CreateDirectory(path: $@"{ModelInfo.ModelPath}\RazorCX");
			Directory.CreateDirectory(path: $@"{ModelInfo.ModelPath}\RazorCX\MissingDrawingsFinder");

			//ui commands
			FindMissingDrawingsCommand = new DelegateCommand(executeMethod: FindMissingDrawings);
			ZoomSelectedCommand = new DelegateCommand(executeMethod: ZoomSelected);
			SelectionChangedCommand = new DelegateCommand<object>(executeMethod: SelectionChanged);
			MissingAssemblyDrawingsCommand = new DelegateCommand(executeMethod: FilterMissingAssemblyDrawings);
			MissingPartDrawingsCommand = new DelegateCommand(executeMethod: FilterMissingPartDrawings);
			RefreshCommand = new DelegateCommand(executeMethod: RefreshPartColors);

			DisplayOptions = _missingDrawingFinder.DisplayOptions;

			IsDataGridVisible = false;
			IsMessageVisible = true;
			IsSpinnerVisible = false;

			Message = "Select Parts or\n Click Button";

			var phases = _model.GetPhases().GetEnumerator().ToAList<Phase>()
				.Select(p => p.PhaseNumber.ToString())
				.OrderBy(p => p.Length)
				.ThenBy(p => p)
				.ToList();

			phases.Insert(0, "ALL");

			Phases = new ObservableCollection<string>(phases);

			Log(message: "Startup successful");

			StatusLog.Add("Startup successful");
		}

		private void RefreshPartColors()
		{
			System.Threading.Tasks.Task.Run(action: () =>
			{
				_missingDrawingFinder.SetModelObjectColorInModel(
					new ArrayList(_allPartSummaries.Select(p => p.Part).ToList()));
			});
		}

		private void ColorParts()
		{
			System.Threading.Tasks.Task.Run(action: () =>
			{
				_missingDrawingFinder.SetModelObjectColorInModel(
					new ArrayList(PartSummaries.Select(p => p.Part).ToList()));
			});
		}

		private void FilterMissingAssemblyDrawings()
		{
			SelectedMaterialTypeValue = "ALL";
			PartSummaries = new ObservableCollection<PartSummary>(GetFilteredPartSummaries().Where(p => p.MainPart > 0).ToList());
			ColorParts();
		}

		private void FilterMissingPartDrawings()
		{
			SelectedMaterialTypeValue = "ALL";
			PartSummaries = new ObservableCollection<PartSummary>(GetFilteredPartSummaries().Where(p => p.MainPart < 1).ToList());
			ColorParts();
		}

		private void FilterPartSummaries()
		{
			PartSummaries = new ObservableCollection<PartSummary>(GetFilteredPartSummaries());
			//ColorParts();
		}

		private List<PartSummary> GetFilteredPartSummaries()
		{
			var filteredByPhase = SelectedPhaseValue == "ALL" ?
				_allPartSummaries :
				_allPartSummaries.Where(p => p.Phase.ToString() == SelectedPhaseValue)
					.ToList();

			var filteredByMaterial = SelectedMaterialTypeValue == "ALL" ?
				filteredByPhase :
				filteredByPhase.Where(p => p.Material == SelectedMaterialTypeValue)
					.ToList();

			return filteredByMaterial;
		}

		private void FindMissingDrawings()
		{
			try
			{
				Message = "Please Wait ...";

				System.Threading.Tasks.Task.Run(action: () =>
				{
					IsMessageVisible = true;
					IsDataGridVisible = false;
					IsMessageVisible = true;
					IsSpinnerVisible = true;

					_allPartSummaries = _missingDrawingFinder.GetMissingDrawings()
						.Where(p => p.Name.ToLower() != "joist").ToList();

					PartSummaries = new ObservableCollection<PartSummary>(list: _allPartSummaries);

					IsDataGridVisible = true;
					IsMessageVisible = false;
					IsSpinnerVisible = false;
					IsMessageVisible = false; 

					var materialTypes = _allPartSummaries.Select(p => p.Material).Distinct().ToList();
					materialTypes.Insert(0, "ALL");
					MaterialTypes = new ObservableCollection<string>(list: materialTypes);

					SelectedPhaseValue = "ALL";
					SelectedMaterialTypeValue = "ALL";
				});
			}
			catch (Exception ex)
			{
				Message = "Find Missing Drawings";
			}
		}

		private void MissingDrawingFinderEvent(object obj, EventArgs e)
		{
			var partSummary = ((MissingDrawingFinderEventArgs)e).PartSummary;

			if (Application.Current?.Dispatcher != null)
				Application.Current.Dispatcher.Invoke(() => { StatusLogRichText += $"\n{partSummary.Guid}"; });
		}

		private void ZoomSelected()
		{
			System.Threading.Tasks.Task.Run(action: () =>
			{
				var guids = SelectedRows?.Select(selector: r => r.Guid).Where(predicate: g => g != null).ToList();
				if (!guids.Any()) return;

				_teklaHandler.SelectPartsInModel(guids: guids);

				var partsToColor = new ArrayList(_allPartSummaries.Select(p => p.Part).ToList());
				_missingDrawingFinder.ZoomSelectedObjects(partsToColor, guids);
			});
		}

		private static void Log(string message, string method = "Missing Drawings Finder")
		{
			var log = $"[{method}] {DateTime.Now.ToShortDateString()} {DateTime.Now.ToLongTimeString()}:  {message}";
			Console.WriteLine(value: log);
		}

		private void SelectionChanged(object value)
		{
			var dataGrid = value as DataGrid;
			if (dataGrid == null) return;

			var rows = dataGrid.SelectedItems.OfType<PartSummary>().ToList();
			if (!rows.Any()) return;

			SelectedRows = new ObservableCollection<PartSummary>(list: rows);

			var guids = SelectedRows?.Select(selector: r => r.Guid).Where(predicate: g => g != null).ToList();
			if (!guids.Any()) return; 

			_teklaHandler.SelectPartsInModel(guids: guids);
		}
	}
}
