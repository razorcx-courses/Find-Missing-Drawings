using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Tekla.Structures;
using Tekla.Structures.Drawing;
using Tekla.Structures.Model;
using Tekla.Structures.Model.UI;
using Tekla.Structures.ModelInternal;
using ModelObjectSelector = Tekla.Structures.Model.UI.ModelObjectSelector;
using Part = Tekla.Structures.Model.Part;

namespace RazorCX.FindMissingDrawings.Models
{
    public class MissingDrawingFinder
    {
        private readonly Model _model;
        private HashSet<string> _drawingNumbers = new HashSet<string>();

        private List<PartSummary> _partSummaries = new List<PartSummary>();

        private List<PartSummary> _steelParts;
        public List<PartSummary> SteelParts
        {
            get => _partSummaries.Where(predicate: p => p.Material.Contains(value: "STEEL")).ToList();
            set => _steelParts = value;
        }

        private List<PartSummary> _concreteParts;
        public List<PartSummary> ConcreteParts
        {
            get => _partSummaries.Where(predicate: p => !p.Material.Contains(value: "STEEL")).ToList();
            set => _steelParts = value;
        }

        private DisplayOptions _displayOptions;
        public DisplayOptions DisplayOptions
        {
            get => _displayOptions;
            set => _displayOptions = value;
        }

        public bool Imperial;

        private readonly ArrayList _stringNames = new ArrayList
        {
            "PART_POS",
            "ASSEMBLY_POS",
            "CAST_UNIT_POS",
            "NAME",
            "MATERIAL_TYPE"
        };

        private readonly ArrayList _intNames = new ArrayList
        {
            "MAIN_PART",
            "PHASE"
        };

        private readonly ArrayList _doubleNames = new ArrayList
        {
        };


        public MissingDrawingFinder()
        {
            try
            {
                _model = new Model();
                var modelFolder = _model.GetInfo().ModelPath;
                var modelAttributesFolder = Path.Combine(path1: modelFolder, path2: "attributes");
                if (!Directory.Exists(path: modelAttributesFolder))
                    Directory.CreateDirectory(path: modelAttributesFolder);

                DisplayOptions = new DisplayOptions()
                {
                    MainPart = -1,
                    MaterialType = "STEEL",
                    Phase = -1,
                    IncludeNonSteel = false,
                    ZoomSelected = false
                };
            }
            catch
            {
                // ignored
            }
        }

        public List<PartSummary> GetMissingDrawings()
        {
            GetDrawingNumbers();

            GetPartSummaries();

            var missingPartSummaries = GetMissingPartSummaries(partSummaries: SteelParts, displayOptions: DisplayOptions);

            var partsToColor = new ArrayList(c: missingPartSummaries.Select(selector: p => p.Part).ToList());
            SetModelObjectColorInModel(modelObjects: partsToColor);

            return missingPartSummaries;
        }

        private void GetDrawingNumbers()
        {
            var drawingHandler = new DrawingHandler();
            var drawingEnumerator = drawingHandler.GetDrawingSelector().GetSelected();

            if (drawingEnumerator.GetSize() < 1)
                drawingEnumerator = drawingHandler.GetDrawings();

            drawingEnumerator.SelectInstances = false;
            var drawings = drawingEnumerator.ToAList<Drawing>();

            drawings.ForEach(action: d =>
            {
                var str = d.Mark;
                foreach (var ch in "[].")
                    str = str.Replace(oldValue: ch.ToString(), newValue: "");
                _drawingNumbers.Add(item: str);
            });
        }

        private void GetPartSummaries()
        {
            ModelObjectEnumerator.AutoFetch = true;

            var parts = new ModelObjectSelector().GetSelectedObjects().ToAList<Part>();

            if (!parts.Any())
                parts = _model.GetModelObjectSelector().GetAllObjectsWithType(TypeFilter: new[] { typeof(Part) }).ToAList<Part>();

            if (!parts.Any()) return;

            _partSummaries = parts
                .Where(p => !p.Name.ToLower().Contains("joist"))
                .Select(p =>
                {
                    var partSummary = GetPartSummary(p);
                    OnMissingDrawingFinderUpdated(new MissingDrawingFinderEventArgs { PartSummary = partSummary });
                    return partSummary;
                })
                .OrderBy(keySelector: p => p.Phase)
                .ThenBy(keySelector: p => p.Mark.ToString().Length)
                .ThenBy(keySelector: p => p.Mark.ToString())
                .ToList();
        }

        private PartSummary GetPartSummary(Part part)
        {
            var values = new Hashtable();
            part.GetAllReportProperties(stringNames: _stringNames, doubleNames: _doubleNames, integerNames: _intNames, values: ref values);

            var phase = values.ContainsKey(key: "PHASE") ? (int)values[key: "PHASE"] : 0;
            var name = values.ContainsKey(key: "NAME") ? (string)values[key: "NAME"] : "";
            var mainPart = values.ContainsKey(key: "MAIN_PART") ? (int)values[key: "MAIN_PART"] : -1;
            var partPos = values.ContainsKey(key: "PART_POS") ? (string)values[key: "PART_POS"] : "";
            var assyPos = values.ContainsKey(key: "ASSEMBLY_POS") ? (string)values[key: "ASSEMBLY_POS"] : "";
            var castUnitPos = values.ContainsKey(key: "CAST_UNIT_POS") ? (string)values[key: "CAST_UNIT_POS"] : "";
            var materialType = values.ContainsKey(key: "MATERIAL_TYPE") ? (string)values[key: "MATERIAL_TYPE"] : "";
            var id = part.Identifier.ID;

            var mark = mainPart > 0 ? assyPos : partPos;

            mark = materialType.Contains(value: "CONCRETE")
                ? castUnitPos
                : mark;

            var material = materialType.Contains(value: "STEEL")
                ? mainPart > 0
                    ? "STEEL ASSEMBLY"
                        : "STEEL PART"
                            : "NON-STEEL";

            material = materialType.Contains(value: "CONCRETE")
                ? mainPart > 0
                    ? "CAST UNIT"
                        : "CONCRETE PART"
                            : material;

            return new PartSummary
            {
                Guid = part.Identifier.GUID.ToString(),
                Phase = phase,
                Mark = mark,
                Name = name,
                Material = material,
                Id = id,
                MainPart = mainPart,
                Part = part
            };
        }

        private List<PartSummary> GetMissingPartSummaries(List<PartSummary> partSummaries, DisplayOptions displayOptions)
        {
            var partDefinitions = partSummaries
                .Where(predicate: p =>
                {
                    var drawingsDoesNotExist =
                        !_drawingNumbers.Contains(item: p.Mark.ToString());
                    return drawingsDoesNotExist;
                }).ToList();

            return partDefinitions;
        }

        public List<string> GetSelectedPartMarks()
        {
            var partMarks = new HashSet<string>();
            try
            {
                ModelObjectEnumerator.AutoFetch = true;
                var selectedObjects = new ModelObjectSelector().GetSelectedObjects().ToAList<Part>();

                var names = new ArrayList { "PART_POS", "ASSEMBLY_POS", "CAST_UNIT_POS" };

                selectedObjects.ForEach(action: p =>
                {
                    var values = new Hashtable();
                    p.GetStringReportProperties(names: names, values: ref values);

                    var partPos = values.ContainsKey(key: "PART_POS") ? (string)values[key: "PART_POS"] : "";
                    var assyPos = values.ContainsKey(key: "ASSEMBLY_POS") ? (string)values[key: "ASSEMBLY_POS"] : "";
                    var castUnitPos = values.ContainsKey(key: "CAST_UNIT_POS") ? (string)values[key: "CAST_UNIT_POS"] : "";

                    if (!string.IsNullOrEmpty(value: partPos))
                        partMarks.Add(item: partPos);
                    if (!string.IsNullOrEmpty(value: partPos))
                        partMarks.Add(item: assyPos);
                    if (!string.IsNullOrEmpty(value: partPos))
                        partMarks.Add(item: castUnitPos);
                });
            }
            catch
            {
                // ignored
            }

            return partMarks.ToList();
        }

        public void ZoomSelectedObjects(ArrayList modelObjects, List<string> guids)
        {
            SetModelObjectColorInModel(modelObjects);

            //var selected = new ArrayList(_model.FetchModelObjects(Guids: guids));

            var selected = new ArrayList();
            guids.ForEach(g => selected.Add(_model.SelectModelObject(new Identifier(g))));

            new ModelObjectSelector().Select(selected);

            dotTemporaryState.SetColor_FAST(selected, new Color(0, 0.1, 1));

            if (!TeklaStructures.Connect())
                return;
            try
            {
                var macroBuilder = new MacroBuilder();
                macroBuilder.Callback(callback: "acmdZoomToSelected", parameter: "", frame: "main_frame");
                macroBuilder.Run(connection: TeklaStructures.Connection);
            }
            catch
            {
                // ignored
            }
        }

        public void SetModelObjectColorInModel(ArrayList modelObjects)
        {
            var nameGroups = modelObjects.OfType<Part>().GroupBy(p => p.Name).OrderBy(g => g.Key).ToList();
            dotTemporaryState.ClearAllStates();
            dotTemporaryState.SetState(State: dotTemporaryStatesEnum.DOT_TEMPORARY_STATE_UNCHANGED,
                Transparency: dotTemporaryTransparenciesEnum.DOT_TEMPORARY_TRANSPARENCY_SEMITRANSPARENT);
            //var Transparency = dotTemporaryTransparenciesEnum.DOT_TEMPORARY_TRANSPARENCY_VISIBLE;
            //dotTemporaryState.SetState(ModelObjects: modelObjects, State: dotTemporaryStatesEnum.DOT_TEMPORARY_STATE_ACTIVE, Transparency: Transparency);
            dotTemporaryState.SetColor_FAST(modelObjects, new Color(1, 0.7, 0));
        }

        public event EventHandler MissingDrawingFinderUpdated;
        public void OnMissingDrawingFinderUpdated(EventArgs e)
        {
            var handler = MissingDrawingFinderUpdated;
            handler?.Invoke(this, e);
        }
    }
}