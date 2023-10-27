using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Tekla.Structures;
using Tekla.Structures.Geometry3d;
using Tekla.Structures.Model;
using Tekla.Structures.Model.UI;
using ModelObjectSelector = Tekla.Structures.Model.UI.ModelObjectSelector;

namespace RazorCX.FindMissingDrawings.Models
{
    public class TeklaHandler
    {
        private readonly Model _model;
        private readonly GraphicsDrawer _graphicsDrawer;

        private readonly Events _events;
        private readonly object _selectionEventHandlerLock = new object();
        private readonly object _changedObjectHandlerLock = new object();

        public ProjectInfo ProjectInfo { get; private set; }
        public ModelInfo ModelInfo { get; private set; }

        public List<Part> SelectedParts { get; set; }

        public TeklaHandler()
        {
            _model = new Model();
            _graphicsDrawer = new GraphicsDrawer();

            ProjectInfo = _model.GetProjectInfo();
            ModelInfo = _model.GetInfo();

            _events = new Events();
            RegisterEventHandler();
        }

        public void RegisterEventHandler()
        {
            _events.SelectionChange += Events_SelectionChangeEvent;
            _events.ModelObjectChanged += Events_ModelObjectChangedEvent;
            _events.Register();
        }

        public void UnRegisterEventHandler()
        {
            _events.UnRegister();
        }

        public event EventHandler PartSelectionChanged;
        public void OnPartSelectionChanged()
        {
            var handler = PartSelectionChanged;
            handler?.Invoke(sender: this, e: EventArgs.Empty);
        }


        void Events_SelectionChangeEvent()
        {
            /* Make sure that the inner code block is running synchronously */
            lock (_selectionEventHandlerLock)
            {
                SelectedParts = new ModelObjectSelector().GetSelectedObjects().ToAList<Part>();
                if (SelectedParts.Any())
                    OnPartSelectionChanged();
            }
        }

        void Events_ModelObjectChangedEvent(List<ChangeData> changes)
        {
            /* Make sure that the inner code block is running synchronously */
            lock (_changedObjectHandlerLock)
            {
                foreach (ChangeData data in changes)
                {
                }
            }
        }

        public void SelectPartsInModel(List<Part> parts)
        {
            new ModelObjectSelector().Select(ModelObjects: new ArrayList(c: parts));
        }

        public void SelectPartsInModel(List<string> guids)
        {
            var selected = new ArrayList();
            guids.ForEach(g => selected.Add(_model.SelectModelObject(new Identifier(g))));

            new ModelObjectSelector().Select(ModelObjects: selected, ShowDimensions: true);
        }

        public Part SelectPart()
        {
            try
            {
                var part = new Picker().PickObject(Enum: Picker.PickObjectEnum.PICK_ONE_PART, Prompt: "Pick ONE part") as Part;

                SelectPartsInModel(parts: new List<Part> { part });

                return part;
            }
            catch (Exception ex)
            {
                return null;
                // ignored
            }
        }

        public void ZoomJoint(Part part)
        {
            //var jointLocation = GetLocation(joint.Sec0Id, joint.Sec0End, 100);

            //var minPoint = new Point
            //{
            //	X = jointLocation.X - 700,
            //	Y = jointLocation.Y - 700,
            //	Z = jointLocation.Z - 700
            //};

            //var maxpoint = new Point
            //{
            //	X = jointLocation.X + 100,
            //	Y = jointLocation.Y + 100,
            //	Z = jointLocation.Z + 100
            //};

            //var aabb = new AABB(minPoint, maxpoint);

            //var selectedViews = ViewHandler.GetPermanentViews();
            //while (selectedViews.MoveNext())
            //{
            //	var view = selectedViews.Current;
            //	ViewHandler.ZoomToBoundingBox(view, aabb);
            //}

            //var primary = _model.SelectModelObject(new Identifier(joint.PrimaryId)) as Beam;
            //var secondary = _model.SelectModelObject(new Identifier(joint.Sec0Id)) as Beam;

            //new ModelObjectSelector().Select(new ArrayList
            //	{primary, secondary});
        }

        public void LabelJoint(Part part)
        {
            //var location = GetLocation(joint.Sec0Id, joint.Sec0End, 100);

            //var text = $@"{joint.Type}_{joint.PrimaryProfile}_{joint.Sec0Profile}_{joint.DesignVr}";
            //var color = new Color(1, 1, 0);

            //_graphicsDrawer.DrawText(location, text, color);
        }

        private Point GetLocation(string id, string sec0End, int offset)
        {
            var secondary = _model.SelectModelObject(ID: new Identifier(guid: id)) as Beam;
            if (secondary == null) return new Point();

            return sec0End == "A"
                ? new Point(X: secondary.StartPoint.X, Y: secondary.StartPoint.Y,
                    Z: secondary.StartPoint.Z + offset)
                : new Point(X: secondary.EndPoint.X, Y: secondary.EndPoint.Y,
                    Z: secondary.EndPoint.Z + offset);
        }

    }
}