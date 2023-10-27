using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Tekla.Structures.Geometry3d;
using Tekla.Structures.Model;
using ModelObjectSelector = Tekla.Structures.Model.UI.ModelObjectSelector;

namespace RazorCX.FindMissingDrawings.Models
{
    public static class ExtensionMethods
    {
        public static Phase GetPhase(this ModelObject modelObject)
        {
            modelObject.GetPhase(phase: out Phase phase);
            return phase;
        }

        public static List<T> GetSelectedObjects<T>(this Model model)
        {
            ModelObjectEnumerator.AutoFetch = true;
            var enumerator = new ModelObjectSelector().GetSelectedObjects();
            enumerator.SelectInstances = false;
            return enumerator.ToAList<T>();
        }

        public static List<T> ToAList<T>(this IEnumerator enumerator)
        {
            var list = new List<T>();
            while (enumerator.MoveNext())
            {
                try
                {
                    var current = (T)enumerator.Current;

                    if (current != null)
                        list.Add(item: current);
                }
                catch (Exception ex)
                {

                }
            }
            return list;
        }

        public static string ToCsv(this DataTable table, string delimator = ",", bool headers = true)
        {
            var result = new StringBuilder();

            if (headers)
                for (int i = 0; i < table.Columns.Count; i++)
                {
                    result.Append(value: table.Columns[index: i].ColumnName);
                    result.Append(value: i == table.Columns.Count - 1 ? "\n" : delimator);
                }

            foreach (DataRow row in table.Rows)
            {
                for (int i = 0; i < table.Columns.Count; i++)
                {
                    result.Append(value: row[columnIndex: i].ToString().Replace(oldChar: ',', newChar: ' '));
                    result.Append(value: i == table.Columns.Count - 1 ? "\n" : delimator);
                }
            }
            return result.ToString().TrimEnd(trimChars: new char[] { '\r', '\n' });
        }

        public static IReadOnlyCollection<ModelObject> GetBolts(this Connection connection)
        {
            if (connection == null) return null;

            ModelObjectEnumerator.AutoFetch = true;
            return (
                from child in connection.GetChildren().ToList()
                where child.GetType().Name.ToUpper().Contains("BOLT")
                select child).ToList();
        }

        public static IReadOnlyCollection<ModelObject> ToList(this ModelObjectEnumerator modelObjectEnumerator)
        {
            ModelObjectEnumerator.AutoFetch = true;

            try
            {
                if (modelObjectEnumerator == null) return null;

                var objs = new List<ModelObject>();
                modelObjectEnumerator.Reset();

                foreach (ModelObject modelObject in modelObjectEnumerator)
                    objs.Add(item: modelObject);

                return objs;
            }
            catch
            {
                return new List<ModelObject>();
            };
        }

        public static void SelectUiParts(this Model model, List<Part> modelObjects)
        {
            new Tekla.Structures.Model.UI.ModelObjectSelector().Select(ModelObjects: new ArrayList(c: modelObjects));
        }

        public static void SelectUiModelObjects(this Model model, List<ModelObject> modelObjects)
        {
            new Tekla.Structures.Model.UI.ModelObjectSelector().Select(ModelObjects: new ArrayList(c: modelObjects));
        }

        public static bool IsMainPart(this Part part)
        {
            return part.GetReportPropertyInt("MAIN_PART") == 1;
        }

        public static double GetReportValueDouble(this ModelObject part, string name)
        {
            try
            {
                if (part == null) return 0;
                double value = 0;
                part.GetReportProperty(name: name, value: ref value);
                return Math.Round(value: value, digits: 2);
            }
            catch
            {
                return 0;
            };
        }

        public static int GetReportValueInt(this ModelObject part, string name)
        {
            try
            {
                if (part == null) return 0;
                int value = 0;
                part.GetReportProperty(name: name, value: ref value);
                return value;
            }
            catch
            {
                return 0;
            };
        }

        public static string GetReportValueString(this ModelObject part, string name)
        {
            try
            {
                if (part == null) return string.Empty;
                string value = string.Empty;
                part.GetReportProperty(name: name, value: ref value);
                return value;
            }
            catch
            {
                return string.Empty;
            };
        }

        public static T FromJson<T>(this string json)
        {
            return JsonConvert.DeserializeObject<T>(value: json);
        }

        public static double GetReportPropertyDouble(this Part part, string name)
        {
            try
            {
                if (part == null) return 0;
                double value = 0;
                part.GetReportProperty(name: name, value: ref value);
                return Math.Round(value: value, digits: 2);
            }
            catch
            {
                return 0;
            };
        }

        public static int GetReportPropertyInt(this Part part, string name)
        {
            try
            {
                if (part == null) return 0;
                int value = 0;
                part.GetReportProperty(name: name, value: ref value);
                return value;
            }
            catch
            {
                return 0;
            };
        }

        public static string ToCsv(this DataTable table, string delimator = ",")
        {
            var result = new StringBuilder();
            for (int i = 0; i < table.Columns.Count; i++)
            {
                result.Append(value: table.Columns[index: i].ColumnName);
                result.Append(value: i == table.Columns.Count - 1 ? "\n" : delimator);
            }
            foreach (DataRow row in table.Rows)
            {
                for (int i = 0; i < table.Columns.Count; i++)
                {
                    result.Append(value: row[columnIndex: i].ToString().Replace(oldChar: ',', newChar: ' '));
                    result.Append(value: i == table.Columns.Count - 1 ? "\n" : delimator);
                }
            }
            return result.ToString().TrimEnd(trimChars: new char[] { '\r', '\n' });
        }

        public static string ToJson(this object obj, bool formatting = true)
        {
            return
                formatting
                    ? JsonConvert.SerializeObject(value: obj, formatting: Newtonsoft.Json.Formatting.Indented)
                    : JsonConvert.SerializeObject(value: obj, formatting: Newtonsoft.Json.Formatting.None);
        }

        public static void WriteAllText(this string text, string path)
        {
            File.WriteAllText(path: path, contents: text);
        }

        public static GeometricPlane GetPlaneWeb(this ModelObject modelObject)
        {
            try
            {
                if (!(modelObject is Beam)) return null;

                return new Tekla.Structures.Geometry3d.GeometricPlane(CoordSys: ((Beam)modelObject).GetCoordinateSystem());   //plane parallel with web at member neutral axis
            }
            catch
            {
                return null;
            }
        }

        public static GeometricPlane GetPlaneFlange(this ModelObject modelObject)
        {
            if (!(modelObject is Beam)) return null;

            try
            {
                var coordSystem = ((Beam)modelObject).GetCoordinateSystem();

                return new Tekla.Structures.Geometry3d.GeometricPlane(CoordSys: new Tekla.Structures.Geometry3d.CoordinateSystem
                {
                    Origin = coordSystem.Origin,
                    AxisX = coordSystem.AxisX,
                    AxisY = Tekla.Structures.Geometry3d.Vector.Cross(Vector1: coordSystem.AxisX, Vector2: coordSystem.AxisY)
                });  //plane parallel with flange at member neutral axis
            }
            catch
            {
                return null;
            }
        }

        public static Line GetLine(this ModelObject obj)
        {
            return (obj is Beam) ? new Tekla.Structures.Geometry3d.Line(p1: ((Beam)obj).StartPoint, p2: ((Beam)obj).EndPoint) : null;
        }

        public static Vector Round(this Tekla.Structures.Geometry3d.Vector vector, int decimals)
        {
            var x = Math.Round(value: vector.X, digits: decimals);
            var y = Math.Round(value: vector.Y, digits: decimals);
            var z = Math.Round(value: vector.Z, digits: decimals);

            return new Tekla.Structures.Geometry3d.Vector(X: x, Y: y, Z: z);
        }

    }
}