using Microsoft.Xml.Serialization.GeneratedAssembly;
using Sandbox.Definitions;
using Sandbox.Game.Entities.Planet;
using Sandbox.Game.GameSystems;
using Sandbox.Game.World;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI.Ingame;
using VRage.Noise.Combiners;
using VRageMath;
using static VRageMath.Base6Directions;

/// <summary>
/// v3.0
/// Управление дверями и освещение
/// </summary>
namespace MUL_H2_NAV
{
    public sealed class Program : MyGridProgram
    {        // v1
        string NameObj = "[MUL-H2]";
        static string tag_batterys_duty = "[batterys_duty]"; // дежурная батарея
        static float GyroMult = 1f;
        static float AlignAccelMult = 0.01f;
        static float TargetSize = 100;
        static float OnCharge = 0.2f;       // Процент заряда - вкл на под  заряд
        static float OffCharge = 0.9f;      // Процент заряда - выкл под  заряд
        static float MinHeight = 1000f;     // минимальное растояние от земли, начинаем тормозить
        static float DistFlyHeight = 500f;  // минимальное растояние от коннектора до зоны безопасного полета
        static int dist_con_d = 10;         // расстояние от коннектора D до кокпита
        static int dist_con_b = 30;         // расстояние от коннектора И до кокпита

        const char igreen = '\uE001';
        const char iblue = '\uE002';
        const char ired = '\uE003';
        const char iyellow = '\uE004';
        const char idarkGrey = '\uE00F';

        static LCD lcd_storage;
        static LCD lcd_debug;
        static LCD lcd_info_1;
        static LCD lcd_info_2;
        static LCD lcd_info_3;
        static Batterys batterys;
        static Cockpit cockpit;
        static Connector con_d;
        static Connector con_b;
        static Thrusts thrusts;
        static Gyros gyros;
        static Camera camera_course;
        static Camera camera_con_d;
        static Camera camera_con_b;
        Navigation navigation;
        static Program _scr;
        public class PText
        {
            static public string GetPersent(double perse)
            {
                return " - " + Math.Round((perse * 100), 1) + "%";
            }
            static public string GetScalePersent(double perse, int scale)
            {
                string prog = "[";
                for (int i = 0; i < Math.Round((perse * scale), 0); i++)
                {
                    prog += "|";
                }
                for (int i = 0; i < scale - Math.Round((perse * scale), 0); i++)
                {
                    prog += "'";
                }
                prog += "]" + GetPersent(perse);
                return prog;
            }
            static public string GetCurrentOfMax(float cur, float max, string units)
            {
                return "[ " + Math.Round(cur, 1) + units + " / " + Math.Round(max, 1) + units + " ]";
            }
            static public string GetThrust(float value)
            {
                return Math.Round(value / 1000000, 1) + "МН";
            }
            static public string GetGPS(string name, Vector3D target)
            {
                return "GPS:" + name + ":" + target.GetDim(0) + ":" + target.GetDim(1) + ":" + target.GetDim(2) + ":\n";
            }
        }
        public class BaseListTerminalBlock<T> where T : class
        {
            public List<T> list_obj = new List<T>();
            public int Count { get { return list_obj.Count(); } }
            public BaseListTerminalBlock(string name_obj)
            {
                _scr.GridTerminalSystem.GetBlocksOfType<T>(list_obj, r => ((IMyTerminalBlock)r).CustomName.Contains(name_obj));
                _scr.Echo("Найдено" + typeof(T).Name + "[" + name_obj + "]: " + list_obj.Count());
            }
            public BaseListTerminalBlock(string name_obj, string tag)
            {
                _scr.GridTerminalSystem.GetBlocksOfType<T>(list_obj, r => ((IMyTerminalBlock)r).CustomName.Contains(name_obj));
                if (!String.IsNullOrWhiteSpace(tag))
                {
                    list_obj = list_obj.Where(n => ((IMyTerminalBlock)n).CustomName.Contains(tag)).ToList();
                }
                _scr.Echo("Найдено" + typeof(T).Name + "[" + name_obj + "],[" + tag + "]: " + list_obj.Count());
            }
            private void Off(List<T> list)
            {
                foreach (IMyTerminalBlock obj in list)
                {
                    obj.ApplyAction("OnOff_Off");
                }
            }
            public void Off()
            {
                Off(list_obj);
            }
            private void OffOfTag(List<T> list, string tag)
            {
                foreach (IMyTerminalBlock obj in list)
                {
                    if (obj.CustomName.Contains(tag))
                    {
                        obj.ApplyAction("OnOff_Off");
                    }
                }
            }
            public void OffOfTag(string tag)
            {
                OffOfTag(list_obj, tag);
            }
            private void On(List<T> list)
            {
                foreach (IMyTerminalBlock obj in list)
                {
                    obj.ApplyAction("OnOff_On");
                }
            }
            public void On()
            {
                On(list_obj);
            }
            private void OnOfTag(List<T> list, string tag)
            {
                foreach (IMyTerminalBlock obj in list)
                {
                    if (obj.CustomName.Contains(tag))
                    {
                        obj.ApplyAction("OnOff_On");
                    }
                }
            }
            public void OnOfTag(string tag)
            {
                OnOfTag(list_obj, tag);
            }
            public bool Enabled(string tag)
            {
                foreach (IMyTerminalBlock obj in list_obj)
                {
                    if (obj.CustomName.Contains(tag) && !((IMyFunctionalBlock)obj).Enabled)
                    {
                        return false;
                    }
                }
                return true;
            }
            public bool Enabled()
            {
                foreach (IMyTerminalBlock obj in list_obj)
                {
                    if (!((IMyFunctionalBlock)obj).Enabled)
                    {
                        return false;
                    }
                }
                return true;
            }
        }
        public class BaseTerminalBlock<T> where T : class
        {
            public T obj;
            public string CustomName { get { return ((IMyTerminalBlock)this.obj).CustomName; } set { ((IMyTerminalBlock)this.obj).CustomName = value; } }
            public BaseTerminalBlock(string name)
            {
                obj = _scr.GridTerminalSystem.GetBlockWithName(name) as T;
                _scr.Echo("block:[" + name + "]: " + ((obj != null) ? ("Ок") : ("not Block")));
            }
            public BaseTerminalBlock(T myobj)
            {
                obj = myobj;
                _scr.Echo("block:[" + obj.ToString() + "]: " + ((obj != null) ? ("Ок") : ("not Block")));
            }
            public Vector3D GetPosition()
            {
                return ((IMyEntity)obj).GetPosition();
            }
            public void Off()
            {
                if (obj != null) ((IMyTerminalBlock)obj).ApplyAction("OnOff_Off");
            }
            public void On()
            {
                if (obj != null) ((IMyTerminalBlock)obj).ApplyAction("OnOff_On");
            }
        }
        public class BaseController
        {
            public IMyShipController obj;
            private double current_height = 0;
            public double CurrentHeight { get { return this.current_height; } }
            public Matrix GetCockpitMatrix()
            {
                Matrix CockpitMatrix = new MatrixD();
                this.obj.Orientation.GetMatrix(out CockpitMatrix);
                return CockpitMatrix;
            }
            public BaseController(string name)
            {
                obj = _scr.GridTerminalSystem.GetBlockWithName(name) as IMyShipController;
                _scr.Echo("base_controller:[" + name + "]: " + ((obj != null) ? ("Ок") : ("not Block")));
            }
            public void Dampeners(bool on)
            {
                this.obj.DampenersOverride = on;
            }
            public void OutText(StringBuilder values, int num_lcd)
            {
                if (this.obj is IMyTextSurfaceProvider)
                {
                    IMyTextSurfaceProvider ipp = this.obj as IMyTextSurfaceProvider;
                    if (num_lcd > ipp.SurfaceCount) return;
                    IMyTextSurface ts = ipp.GetSurface(num_lcd);
                    if (ts != null)
                    {
                        ts.WriteText(values, false);
                    }
                }
            }
            public void OutText(string text, bool append, int num_lcd)
            {
                if (this.obj is IMyTextSurfaceProvider)
                {
                    IMyTextSurfaceProvider ipp = this.obj as IMyTextSurfaceProvider;
                    if (num_lcd > ipp.SurfaceCount) return;
                    IMyTextSurface ts = ipp.GetSurface(num_lcd);
                    if (ts != null)
                    {
                        ts.WriteText(text, append);
                    }
                }
            }
            public StringBuilder GetText(int num_lcd)
            {
                StringBuilder values = new StringBuilder();
                if (this.obj is IMyTextSurfaceProvider)
                {
                    IMyTextSurfaceProvider ipp = this.obj as IMyTextSurfaceProvider;
                    if (num_lcd > ipp.SurfaceCount) return null;
                    IMyTextSurface ts = ipp.GetSurface(num_lcd);
                    if (ts != null)
                    {
                        ts.ReadText(values);
                    }
                }
                return values;
            }
            public double GetCurrentHeight()
            {
                double cur_h = 0;
                this.obj.TryGetPlanetElevation(MyPlanetElevation.Surface, out cur_h);
                return cur_h;
            }
            public void Logic(string argument, UpdateType updateSource)
            {
                switch (argument)
                {
                    default:
                        break;
                }
                if (updateSource == UpdateType.Update10)
                {
                    // Получить высоту над поверхностью
                    current_height = GetCurrentHeight();
                }
            }
            public string TextInfo()
            {
                StringBuilder values = new StringBuilder();
                values.Append("Гравитация: " + this.obj.GetNaturalGravity().Length() + "\n");
                values.Append("PhysicalMass: " + this.obj.CalculateShipMass().PhysicalMass + "\n");
                values.Append("Скорость: " + this.obj.GetShipSpeed() + "\n");
                values.Append("Высота: " + current_height + "\n");
                return values.ToString();
            }
        }
        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            _scr = this;
            lcd_storage = new LCD(NameObj + "-LCD [storage]");
            lcd_debug = new LCD(NameObj + "-LCD-DEBUG");
            lcd_info_1 = new LCD(NameObj + "-LCD-INFO-1");
            lcd_info_2 = new LCD(NameObj + "-LCD-INFO-2");
            lcd_info_3 = new LCD(NameObj + "-LCD-INFO-3");
            cockpit = new Cockpit(NameObj + "-Cocpit [LCD]");
            batterys = new Batterys(NameObj);
            con_d = new Connector(NameObj + "-Connector down");
            con_b = new Connector(NameObj + "-Connector back");
            gyros = new Gyros(NameObj);
            thrusts = new Thrusts(NameObj, cockpit);
            camera_course = new Camera(NameObj + "-Camera curse");
            camera_con_d = new Camera(NameObj + "-Camera down");
            camera_con_b = new Camera(NameObj + "-Camera back");
            navigation = new Navigation();
        }
        public void Save()
        {

        }
        // Переключить батареи
        public void Main(string argument, UpdateType updateSource)
        {
            StringBuilder values_info = new StringBuilder();
            batterys.Logic(argument, updateSource);
            navigation.Logic(argument, updateSource);
            switch (argument)
            {
                default:
                    break;
            }
            if (updateSource == UpdateType.Update10)
            {
                values_info.Append(batterys.TextInfo());
                values_info.Append("back:" + con_b.TextInfo());
                values_info.Append("down:" + con_d.TextInfo());
                values_info.Append(thrusts.TextInfo());
                lcd_info_1.OutText(values_info);
                lcd_info_2.OutText(navigation.TextInfo1(), false);
                lcd_info_3.OutText(navigation.TextInfo2(), false);
                lcd_info_3.OutText(gyros.TextDebug(), true);
            }
        }
        public class LCD : BaseTerminalBlock<IMyTextPanel>
        {
            public LCD(string name) : base(name)
            {
                if (base.obj != null)
                {
                    base.obj.SetValue("Content", (Int64)1);
                }
            }
            public void OutText(StringBuilder values)
            {
                if (base.obj != null)
                {
                    base.obj.WriteText(values, false);
                }
            }
            public void OutText(string text, bool append)
            {
                if (base.obj != null)
                {
                    base.obj.WriteText(text, append);
                }
            }
            public StringBuilder GetText()
            {
                StringBuilder values = new StringBuilder();
                if (base.obj != null)
                {
                    base.obj.ReadText(values);
                }
                return values;
            }
        }
        public class Batterys : BaseListTerminalBlock<IMyBatteryBlock>
        {
            public int count_work_batterys { get { return list_obj.Where(n => !((IMyTerminalBlock)n).CustomName.Contains(tag_batterys_duty)).Count(); } }
            public Batterys(string name_obj) : base(name_obj) { }
            public Batterys(string name_obj, string tag) : base(name_obj, tag) { }
            public float MaxPower() { return base.list_obj.Select(b => b.MaxStoredPower).Sum(); }
            public float CurrentPower() { return base.list_obj.Select(b => b.CurrentStoredPower).Sum(); }
            public float CurrentPersent() { return base.list_obj.Select(b => b.CurrentStoredPower).Sum() / base.list_obj.Select(b => b.MaxStoredPower).Sum(); }
            public int CountCharger()
            {
                List<IMyBatteryBlock> res = base.list_obj.Where(b => ((IMyBatteryBlock)b).ChargeMode == ChargeMode.Recharge).ToList();
                return res.Count();
            }
            public int CountAuto()
            {
                List<IMyBatteryBlock> res = base.list_obj.Where(b => ((IMyBatteryBlock)b).ChargeMode == ChargeMode.Auto).ToList();
                return res.Count();
            }
            public float MaxInput()
            {
                return base.list_obj.Select(b => b.MaxInput).Sum();
            }
            public float MaxOutput()
            {
                return base.list_obj.Select(b => b.MaxOutput).Sum();
            }
            public float CurrentInput()
            {
                return base.list_obj.Select(b => b.CurrentInput).Sum();
            }
            public float CurrentOutput()
            {
                return base.list_obj.Select(b => b.CurrentOutput).Sum();
            }
            public bool IsCharger()
            {
                int count_charger = CountCharger();
                return count_work_batterys > 0 && count_charger > 0 && count_work_batterys == count_charger ? true : false;
            }
            public bool IsAuto()
            {
                int count_auto = CountAuto();
                return Count > 0 && count_auto > 0 && Count == count_auto ? true : false;
            }
            public void Charger()
            {
                foreach (IMyBatteryBlock obj in base.list_obj)
                {
                    // проверка батарея дежурного режима
                    if (!obj.CustomName.Contains(tag_batterys_duty))
                    {
                        obj.ChargeMode = ChargeMode.Recharge;
                    }
                }
            }
            public void Auto()
            {
                foreach (IMyBatteryBlock obj in base.list_obj)
                {
                    obj.ChargeMode = ChargeMode.Auto;
                }
            }
            public void Logic(string argument, UpdateType updateSource)
            {
                switch (argument)
                {
                    default:
                        break;
                }

                if (updateSource == UpdateType.Update10)
                {

                }

            }
            public string TextInfo()
            {
                StringBuilder values = new StringBuilder();
                values.Append("БАТАРЕЯ :[" + Count + "]" + "\n");
                values.Append("|- ЗАРЯД: " + PText.GetCurrentOfMax(CurrentPower(), MaxPower(), "W") + "\n");
                float max = MaxPower();
                values.Append("|  " + PText.GetScalePersent(max > 0f ? CurrentPower() / MaxPower() : 0f, 40) + "\n");
                int count = CountCharger();
                values.Append("|- IN   : [" + count + "] " + (count > 0 ? igreen.ToString() : iyellow.ToString()) + " " + PText.GetCurrentOfMax(CurrentInput(), MaxInput(), "W") + "\n");
                max = MaxInput();
                values.Append("|  " + PText.GetScalePersent(max > 0f ? CurrentInput() / MaxInput() : 0f, 40) + "\n");
                count = CountAuto();
                values.Append("|- OUT  : [" + count + "] " + (count > 0 ? igreen.ToString() : iyellow.ToString()) + " " + PText.GetCurrentOfMax(CurrentOutput(), MaxOutput(), "W") + "\n");
                max = MaxOutput();
                values.Append("|  " + PText.GetScalePersent(max > 0f ? CurrentOutput() / MaxOutput() : 0f, 40) + "\n");
                return values.ToString();
            }
        }
        public class Cockpit : BaseController { public Cockpit(string name) : base(name) { } }
        /// <summary>
        /// v06.07.2023
        /// </summary>
        public class Thrusts : BaseListTerminalBlock<IMyThrust>
        {
            private BaseController remote_control;
            //------------------------------------------------
            public List<IMyThrust> UpThrusters = new List<IMyThrust>();
            public List<IMyThrust> DownThrusters = new List<IMyThrust>();
            public List<IMyThrust> LeftThrusters = new List<IMyThrust>();
            public List<IMyThrust> RightThrusters = new List<IMyThrust>();
            public List<IMyThrust> ForwardThrusters = new List<IMyThrust>();
            public List<IMyThrust> BackwardThrusters = new List<IMyThrust>();
            public double UpThrMax { get { return UpThrusters.Sum(t => t.MaxEffectiveThrust); } }
            public double DownThrMax { get { return DownThrusters.Sum(t => t.MaxEffectiveThrust); } }
            public double LeftThrMax { get { return LeftThrusters.Sum(t => t.MaxEffectiveThrust); } }
            public double RightThrMax { get { return RightThrusters.Sum(t => t.MaxEffectiveThrust); } }
            public double ForwardThrMax { get { return ForwardThrusters.Sum(t => t.MaxEffectiveThrust); } }
            public double BackwardThrMax { get { return BackwardThrusters.Sum(t => t.MaxEffectiveThrust); } }
            public Thrusts(string name_obj, BaseController remote_control) : base(name_obj)
            {
                InitThrusts(remote_control, "F");
            }
            public Thrusts(string name_obj, string tag, BaseController remote_control) : base(name_obj, tag)
            {
                InitThrusts(remote_control, "F");
            }
            public void InitThrusts(BaseController remote_control, string axis)
            {
                this.remote_control = remote_control;
                MatrixD OrientationCocpit = this.remote_control.GetCockpitMatrix();
                // Список трастеров
                UpThrusters.Clear();
                DownThrusters.Clear();
                LeftThrusters.Clear();
                RightThrusters.Clear();
                ForwardThrusters.Clear();
                BackwardThrusters.Clear();
                // Орентация трастеров
                Matrix ThrusterMatrix = new MatrixD();
                Vector3D vUp = OrientationCocpit.Up;
                Vector3D vDown = OrientationCocpit.Down;
                Vector3D vLeft = OrientationCocpit.Left;
                Vector3D vRight = OrientationCocpit.Right;
                Vector3D vForward = OrientationCocpit.Forward;
                Vector3D vBackward = OrientationCocpit.Backward;
                if (axis == "D")
                {
                    vUp = OrientationCocpit.Forward;
                    vDown = OrientationCocpit.Backward;
                    vForward = OrientationCocpit.Down;
                    vBackward = OrientationCocpit.Up;
                }
                else if (axis == "B")
                {
                    vUp = OrientationCocpit.Up;
                    vDown = OrientationCocpit.Down;
                    vForward = OrientationCocpit.Backward;
                    vBackward = OrientationCocpit.Forward;
                }
                foreach (IMyThrust thrust in this.list_obj)
                {
                    thrust.Orientation.GetMatrix(out ThrusterMatrix);
                    //Y
                    if (ThrusterMatrix.Forward == vUp)
                    {
                        UpThrusters.Add(thrust);
                    }
                    else if (ThrusterMatrix.Forward == vDown)
                    {
                        DownThrusters.Add(thrust);
                    }
                    //X
                    else if (ThrusterMatrix.Forward == vLeft)
                    {
                        LeftThrusters.Add(thrust);
                    }
                    else if (ThrusterMatrix.Forward == vRight)
                    {
                        RightThrusters.Add(thrust);
                    }
                    //Z
                    else if (ThrusterMatrix.Forward == vForward)
                    {
                        ForwardThrusters.Add(thrust);
                    }
                    else if (ThrusterMatrix.Forward == vBackward)
                    {
                        BackwardThrusters.Add(thrust);
                    }
                }
            }
            public void ClearThrustOverridePersent()
            {
                SetOverridePercent(UpThrusters, 0f);
                SetOverridePercent(DownThrusters, 0f);
                SetOverridePercent(LeftThrusters, 0f);
                SetOverridePercent(RightThrusters, 0f);
                SetOverridePercent(ForwardThrusters, 0f);
                SetOverridePercent(BackwardThrusters, 0f);
            }
            public void SetOverridePercent(List<IMyThrust> Thrusts, float persent)
            {

                foreach (IMyThrust tr in Thrusts)
                {
                    tr.ThrustOverridePercentage = persent;
                }
            }
            public void SetOverridePercent(string axis, float persentValue)
            {
                if (axis == "U")
                {
                    SetOverridePercent(DownThrusters, persentValue);
                }
                else if (axis == "D")
                {
                    SetOverridePercent(UpThrusters, persentValue);
                }
                else if (axis == "L")
                {
                    SetOverridePercent(RightThrusters, persentValue);
                }
                else if (axis == "R")
                {
                    SetOverridePercent(LeftThrusters, persentValue);
                }
                else if (axis == "F")
                {
                    SetOverridePercent(BackwardThrusters, persentValue);
                }
                else if (axis == "B")
                {
                    SetOverridePercent(ForwardThrusters, persentValue);
                }
            }
            public void SetOverrideN(string axis, float OverrideValue)
            {
                double MaxThrust = 0;
                float Value = 0;
                if (axis == "D") { MaxThrust = UpThrMax; SetOverridePercent("U", 0f); }
                else if (axis == "U") { MaxThrust = DownThrMax; SetOverridePercent("D", 0f); }
                else if (axis == "F") { MaxThrust = BackwardThrMax; SetOverridePercent("B", 0f); }
                else if (axis == "B") { MaxThrust = ForwardThrMax; SetOverridePercent("F", 0f); }
                else if (axis == "R") { MaxThrust = LeftThrMax; SetOverridePercent("L", 0f); }
                else if (axis == "L") { MaxThrust = RightThrMax; SetOverridePercent("R", 0f); }
                if (OverrideValue == 0)
                {
                    Value = 0;
                }
                else
                {
                    Value = (float)Math.Max(OverrideValue / MaxThrust, 0.1f);
                }
                SetOverridePercent(axis, Value);
            }
            public void SetOverrideAccel(string axis, float OverrideValue)
            {
                switch (axis)
                {
                    case "U":
                        OverrideValue += (float)this.remote_control.obj.GetNaturalGravity().Length();
                        if (OverrideValue < 0)
                        {
                            axis = "D";
                            OverrideValue = -OverrideValue;
                        }
                        break;
                    case "D":
                        OverrideValue += (float)this.remote_control.obj.GetNaturalGravity().Length();
                        if (OverrideValue < 0)
                        {
                            axis = "U";
                            OverrideValue = -OverrideValue;
                        }
                        break;
                    case "L":
                        if (OverrideValue < 0)
                        {
                            axis = "R";
                            OverrideValue = -OverrideValue;
                        }
                        break;
                    case "R":
                        if (OverrideValue < 0)
                        {
                            axis = "L";
                            OverrideValue = -OverrideValue;
                        }
                        break;
                    case "F":
                        if (OverrideValue < 0)
                        {
                            axis = "B";
                            OverrideValue = -OverrideValue;
                        }
                        break;
                    case "B":
                        if (OverrideValue < 0)
                        {
                            axis = "F";
                            OverrideValue = -OverrideValue;
                        }
                        break;
                }
                SetOverrideN(axis, OverrideValue * this.remote_control.obj.CalculateShipMass().PhysicalMass);
            }
            public string TextInfo()
            {
                StringBuilder values = new StringBuilder();
                values.Append("PhysicalMass : " + Math.Round(this.remote_control.obj.CalculateShipMass().PhysicalMass) + "\n");
                values.Append("Grav         : " + Math.Round(this.remote_control.obj.GetNaturalGravity().Length()) + "\n");
                values.Append("------------------------------------------\n");
                values.Append("UP MAX       : " + PText.GetThrust((float)UpThrMax) + "\n");
                values.Append("DOWN MAX     : " + PText.GetThrust((float)DownThrMax) + "\n");
                values.Append("Forward MAX  : " + PText.GetThrust((float)ForwardThrMax) + "\n");
                values.Append("Backward MAX : " + PText.GetThrust((float)BackwardThrMax) + "\n");
                values.Append("Left MAX     : " + PText.GetThrust((float)LeftThrMax) + "\n");
                values.Append("Right MAX    : " + PText.GetThrust((float)RightThrMax) + "\n");
                //values.Append("UP       : " + PText.GetThrust((float)UpThrust) + "\t, MAX : " + PText.GetThrust((float)UpThrMax) + "\n");
                //values.Append("DOWN     : " + PText.GetThrust((float)DownThrust) + "\t, MAX : " + PText.GetThrust((float)DownThrMax) + "\n");
                //values.Append("Forward  : " + PText.GetThrust((float)ForwardThrust) + "\t, MAX : " + PText.GetThrust((float)ForwardThrMax) + "\n");
                //values.Append("Backward : " + PText.GetThrust((float)BackwardThrust) + "\t, MAX : " + PText.GetThrust((float)BackwardThrMax) + "\n");
                //values.Append("Left     : " + PText.GetThrust((float)LeftThrust) + "\t, MAX : " + PText.GetThrust((float)LeftThrMax) + "\n");
                //values.Append("Right    : " + PText.GetThrust((float)RightThrust) + "\t, MAX : " + PText.GetThrust((float)RightThrMax) + "\n");
                return values.ToString();
            }
        }
        public class Connector : BaseTerminalBlock<IMyShipConnector>
        {
            public MyShipConnectorStatus Status { get { return base.obj.Status; } }
            public bool Connected { get { return base.obj.Status == MyShipConnectorStatus.Connected ? true : false; } }
            public bool Unconnected { get { return base.obj.Status == MyShipConnectorStatus.Unconnected ? true : false; } }
            public bool Connectable { get { return base.obj.Status == MyShipConnectorStatus.Connectable ? true : false; } }
            public Connector(string name) : base(name) { if (base.obj != null) { } }
            public string TextInfo()
            {
                StringBuilder values = new StringBuilder();
                values.Append("КОННЕКТОР: " + (Connected ? igreen.ToString() : (Connectable ? iyellow.ToString() : ired.ToString())) + "\n");
                return values.ToString();
            }
            public long? getRemoteConnector()
            {
                List<IMyShipConnector> list_conn = new List<IMyShipConnector>();
                _scr.GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(list_conn);
                foreach (IMyShipConnector conn in list_conn.Where(c => c.Status == MyShipConnectorStatus.Connected).ToList())
                { if (conn.EntityId != base.obj.EntityId && (conn.GetPosition() - base.obj.GetPosition()).Length() < 2) return conn.EntityId; }
                return null;
            }
        }
        public class Gyros : BaseListTerminalBlock<IMyGyro>
        {
            public Gyros(string name_obj) : base(name_obj) { }
            public Gyros(string name_obj, string tag) : base(name_obj, tag) { }
            public void SetOverride(bool OverrideOnOff, Vector3 settings, float Power = 1)
            {
                foreach (IMyGyro gyro in base.list_obj)
                {
                    if ((!gyro.GyroOverride) && OverrideOnOff)
                        gyro.ApplyAction("Override");
                    gyro.GyroPower = Power;
                    gyro.Yaw = settings.GetDim(0);
                    gyro.Pitch = settings.GetDim(1);
                    gyro.Roll = settings.GetDim(2);
                }
            }
            public void SetOverride(bool OverrideOnOff = true, float OverrideValue = 0, float Power = 1)
            {
                foreach (IMyGyro gyro in base.list_obj)
                {
                    if (((!gyro.GyroOverride) && OverrideOnOff) || ((gyro.GyroOverride) && !OverrideOnOff))
                        gyro.ApplyAction("Override");
                    gyro.GyroPower = Power;
                    gyro.Yaw = OverrideValue;
                    gyro.Pitch = OverrideValue;
                    gyro.Roll = OverrideValue;
                }
            }
            public string TextDebug()
            {
                StringBuilder values = new StringBuilder();
                values.Append("Yaw :" + base.list_obj.Select(g => g.Yaw).Average() + "\n");
                values.Append("Pitch :" + base.list_obj.Select(g => g.Pitch).Average() + "\n");
                values.Append("Roll :" + base.list_obj.Select(g => g.Roll).Average() + "\n");
                return values.ToString();
            }
        }
        public class Camera : BaseTerminalBlock<IMyCameraBlock>
        {
            public MyBlockOrientation Orientation { get { return base.obj.Orientation; } }
            public Camera(string name) : base(name)
            {
                base.obj.EnableRaycast = true;
            }
            public bool CanScan(double distance)
            {
                return base.obj.CanScan(distance);
            }
            public MyDetectedEntityInfo? Raycast(double dist_scan, float pitch_scan, float yaw_scan)
            {
                MyDetectedEntityInfo? result = null;
                if (this.CanScan(dist_scan))
                {
                    result = base.obj.Raycast(dist_scan, pitch_scan, yaw_scan);
                }
                return result;
            }
            public Vector3D GetVectorForward()
            {
                return obj.WorldMatrix.Forward;
            }
            public string TextInfo()
            {
                StringBuilder values = new StringBuilder();
                return values.ToString();
            }
            public string GetTextDetectedEntityInfo(MyDetectedEntityInfo? info)
            {
                StringBuilder values = new StringBuilder();
                if (info != null)
                {
                    values.Append("Name: " + ((MyDetectedEntityInfo)info).Name + "\n");
                    values.Append("Type: " + ((MyDetectedEntityInfo)info).Type + "\n");
                    values.Append("HitPosition: " + ((MyDetectedEntityInfo)info).HitPosition + "\n");
                    values.Append("Orientation: " + ((MyDetectedEntityInfo)info).Orientation + "\n");
                    values.Append("Velocity: " + ((MyDetectedEntityInfo)info).Velocity + "\n");
                    values.Append("Relationship: " + ((MyDetectedEntityInfo)info).Relationship + "\n");
                    values.Append("BoundingBox: " + ((MyDetectedEntityInfo)info).BoundingBox + "\n");
                    //values.Append("TimeStamp: " + ((MyDetectedEntityInfo)info).TimeStamp + "\n");
                    //values.Append("EntityId: " + ((MyDetectedEntityInfo)info).EntityId + "\n");
                };
                return values.ToString();
            }
        }
        public class Navigation
        {
            public bool go_home = false; // вернутся домой и остатся
            public bool paused = false;
            public bool gravity = false;
            public string manual_vector_axis = null;
            public string current_vector_axis = null;
            public Vector3D? TackVector { get; set; } = null;
            public enum programm : int
            {
                none = 0,
                fly_connect_base1 = 1,   // лететь на базу1
                fly_connect_base2 = 2,   // лететь на базу2
            };
            public static string[] name_programm = { "", "ПОЛЕТ НА БАЗУ 1", "ПОЛЕТ НА БАЗУ 2" };
            programm curent_programm = programm.none;
            public enum mode : int
            {
                none = 0,
                base_operation = 1,
                un_dock = 2,
                to_base = 3,
                dock = 4,
                takeoff = 5,
                landing = 6,
                course = 7
            };
            public static string[] name_mode = { "", "БАЗА", "РАСТЫКОВКА", "К БАЗЕ", "СТЫКОВКА", "ВЗЛЕТ", "ПОСАДКА", "КУРС" };
            mode curent_mode = mode.none;
            //------------------------------
            public Vector3D MyPos { get; private set; }
            public Vector3D MyPrevPos { get; private set; }
            public Vector3D VelocityVector { get; private set; }
            public Vector3D UpVelocityVector { get; private set; }
            public Vector3D ForwVelocityVector { get; private set; }
            public Vector3D LeftVelocityVector { get; private set; }
            public Vector3D GravVector { get; private set; }
            public float PhysicalMass { get; private set; } // Физическая масса
            public float TotalMass { get; private set; } // Физическая масса
            public MatrixD WMCocpit { get; private set; } //
            public class MyWorldMatrix
            {
                public Vector3D Forward { get; set; }
                public Vector3D Up { get; set; }
                public Vector3D Left { get; set; }
            }

            public MyWorldMatrix Current_WMCocpit = new MyWorldMatrix();
            public float XMaxA { get; private set; } // LR
            public float YMaxA { get; private set; } // UD
            public float ZMaxA { get; private set; } // FB
            // ---------------------------------------
            public float Distance { get; private set; }
            public double Hd { get; private set; }
            public double Vd { get; private set; }
            //----------------------------------------
            public Vector3D PlanetCenter = new Vector3D(0.50, 0.50, 0.50);
            public double FlyHeight { get; set; } = 0;
            private int CurrNumBase { get; set; } = 0;
            public class BaseStorage
            {
                public string ConnectorTag { get; set; }
                public long id { get; set; } = 0;
                public Vector3D BaseDockPoint = new Vector3D(0, 0, 0);
                public Vector3D ConnectorPoint = new Vector3D(0, 0, 0);
                public Vector3D PlanetCenter = new Vector3D(0.0, 0.0, 0.0);
                public double FlyHeight { get; set; } = 0;
                public MatrixD DockMatrix { get; set; }
            }
            public int CountBase { get; private set; } = 2;
            BaseStorage[] BasePoints;
            public BaseStorage CurrBase { get; set; } = new BaseStorage();
            // тестовые переменные
            public double S { get; set; } = 0;
            public string name { get; set; }
            public string status { get; set; }
            //--
            public Navigation()
            {
                LoadFromStorage();
                InitBasePoints();
                GetCurrentBase(0);
                UpdateCalc();
            }
            public MyWorldMatrix GetMyWorldMatrix()
            {
                MyWorldMatrix result = new MyWorldMatrix()
                {
                    Forward = WMCocpit.Forward,
                    Up = WMCocpit.Up,
                    Left = WMCocpit.Left,
                };
                if (current_vector_axis == "D")
                {
                    result.Forward = WMCocpit.Down;
                    result.Up = WMCocpit.Forward;
                    result.Left = WMCocpit.Left;
                }
                else if (current_vector_axis == "B")
                {
                    result.Forward = WMCocpit.Backward;
                    result.Up = WMCocpit.Up;
                    result.Left = WMCocpit.Right;
                }
                return result;
            }
            public Vector3D GetMyGyros(Vector3D gyrAng)
            {

                if (current_vector_axis == "D")
                {
                    return new Vector3D(gyrAng.Z, gyrAng.Y, -gyrAng.X);
                }
                else if (current_vector_axis == "B")
                {
                    return new Vector3D(gyrAng.X, -gyrAng.Y, -gyrAng.Z);
                }
                return gyrAng;
            }
            public void UpdateCalc()
            {
                PlanetCenter = FindPlanetCenter();
                GravVector = cockpit.obj.GetNaturalGravity();
                gravity = GravVector.LengthSquared() > 0.2f;
                PhysicalMass = cockpit.obj.CalculateShipMass().PhysicalMass;
                TotalMass = cockpit.obj.CalculateShipMass().TotalMass;
                MyPrevPos = MyPos;
                MyPos = cockpit.obj.GetPosition();
                VelocityVector = (MyPos - MyPrevPos) * 6;
                WMCocpit = cockpit.obj.WorldMatrix;
                Current_WMCocpit = GetMyWorldMatrix();
                UpVelocityVector = Current_WMCocpit.Up * Vector3D.Dot(VelocityVector, Current_WMCocpit.Up);
                ForwVelocityVector = Current_WMCocpit.Forward * Vector3D.Dot(VelocityVector, Current_WMCocpit.Forward);
                LeftVelocityVector = Current_WMCocpit.Left * Vector3D.Dot(VelocityVector, Current_WMCocpit.Left);
                YMaxA = Math.Abs((float)Math.Min(thrusts.UpThrMax / PhysicalMass - GravVector.Length(), thrusts.DownThrMax / PhysicalMass + GravVector.Length()));
                ZMaxA = (float)Math.Min(thrusts.ForwardThrMax, thrusts.BackwardThrMax) / PhysicalMass;
                XMaxA = (float)Math.Min(thrusts.RightThrMax, thrusts.LeftThrMax) / PhysicalMass;
            }
            public void InitBasePoints()
            {
                BasePoints = new BaseStorage[CountBase];
                for (int i = 0; i < CountBase; i++)
                {
                    BasePoints[i] = LoadBaseStorage(i);
                }
            }
            //-------------------------------------
            public Vector3D GetNavAngles(Vector3D Target, MatrixD InvMatrix)
            {
                double Yaw = 0, Roll = 0, Pitch = 0;
                double gF = 0, gL = 0, gU = 0;
                // Получим локальные
                Vector3D V3Dcenter = cockpit.obj.GetPosition();
                Vector3D V3Dfow = Current_WMCocpit.Forward + V3Dcenter;
                Vector3D V3Dup = Current_WMCocpit.Up + V3Dcenter;
                Vector3D V3Dleft = Current_WMCocpit.Left + V3Dcenter;
                // Ререведем в глобальные
                V3Dcenter = Vector3D.Transform(V3Dcenter, InvMatrix);
                V3Dfow = (Vector3D.Transform(V3Dfow, InvMatrix)) - V3Dcenter;
                V3Dup = (Vector3D.Transform(V3Dup, InvMatrix)) - V3Dcenter;
                V3Dleft = (Vector3D.Transform(V3Dleft, InvMatrix)) - V3Dcenter;

                Vector3D GravNorm = Vector3D.Normalize(new Vector3D(-0, -1, -0));
                Vector3D TargetNorm = Vector3D.Normalize(Target - V3Dcenter);

                if (gravity)
                {
                    GravNorm = Vector3D.Normalize(GravVector);
                    GravNorm = Vector3D.Normalize(Vector3D.Transform(GravNorm + cockpit.obj.GetPosition(), InvMatrix) - V3Dcenter);
                }
                // держим горизонт(по оси -y), Получаем проекции вектора гравитации на все три оси блока ДУ. 
                gF = GravNorm.Dot(V3Dfow);
                gL = GravNorm.Dot(V3Dleft);
                gU = GravNorm.Dot(V3Dup);

                Roll = (float)Math.Atan2(gL, -gU);
                Pitch = -(float)Math.Atan2(gF, -gU);

                //Рысканием прицеливаемся на точку Target.
                double tF = TargetNorm.Dot(V3Dfow);
                double tL = TargetNorm.Dot(V3Dleft);
                Yaw = -(float)Math.Atan2(tL, tF);

                return GetMyGyros(new Vector3D(Yaw, Pitch, Roll));
            }
            public Vector3D GetNavAnglesCurse(Vector3D Target, MatrixD InvMatrix)
            {
                double Yaw = 0, Roll = 0, Pitch = 0;
                double gF = 0, gL = 0, gU = 0;
                // Получим локальные
                Vector3D V3Dcenter = cockpit.obj.GetPosition();
                Vector3D V3Dfow = Current_WMCocpit.Forward + V3Dcenter;
                Vector3D V3Dup = Current_WMCocpit.Up + V3Dcenter;
                Vector3D V3Dleft = Current_WMCocpit.Left + V3Dcenter;
                // Ререведем в глобальные
                V3Dcenter = Vector3D.Transform(V3Dcenter, InvMatrix);
                V3Dfow = (Vector3D.Transform(V3Dfow, InvMatrix)) - V3Dcenter;
                V3Dup = (Vector3D.Transform(V3Dup, InvMatrix)) - V3Dcenter;
                V3Dleft = (Vector3D.Transform(V3Dleft, InvMatrix)) - V3Dcenter;

                Vector3D TargetNorm = Vector3D.Normalize(Target - V3Dcenter);

                // держим горизонт(по оси -y), Получаем проекции вектора гравитации на все три оси блока ДУ. 
                gF = TargetNorm.Dot(V3Dup);
                gL = TargetNorm.Dot(V3Dleft);
                gU = TargetNorm.Dot(-V3Dfow);

                Roll = (float)Math.Atan2(gL, -gU);
                Pitch = -(float)Math.Atan2(gF, -gU);

                Yaw = cockpit.obj.RotationIndicator.Y;

                return new Vector3D(-Roll, Pitch, Yaw);
            }
            public Vector3D GetNavAngles(Vector3D? Vector)
            {
                Vector3D GravNorm = Vector3D.Normalize(GravVector);
                double gF = GravNorm.Dot(Current_WMCocpit.Forward);
                double gL = GravNorm.Dot(Current_WMCocpit.Left);
                double gU = GravNorm.Dot(Current_WMCocpit.Up);
                //Получаем сигналы по тангажу и крены операцией atan2
                float Roll = (float)Math.Atan2(gL, -gU);
                float Pitch = -(float)Math.Atan2(gF, -gU);
                double Yaw = 0;
                if (Vector != null)
                {
                    Vector3D TargetNorm = Vector3D.Normalize((Vector3D)Vector);
                    //Рысканием прицеливаемся на точку Target.
                    double tF = TargetNorm.Dot(Current_WMCocpit.Forward);
                    double tL = TargetNorm.Dot(Current_WMCocpit.Left);
                    Yaw = -(float)Math.Atan2(tL, tF);
                }
                else
                {
                    Yaw = cockpit.obj.RotationIndicator.Y;
                }
                return new Vector3D(Yaw, Pitch, Roll);
            }
            public Vector3D GetNavAnglesCurse(Vector3D? Vector)
            {
                double gF = ((Vector3D)Vector).Dot(Current_WMCocpit.Up);
                double gL = ((Vector3D)Vector).Dot(Current_WMCocpit.Left);
                double gU = ((Vector3D)Vector).Dot(-Current_WMCocpit.Forward);
                //Получаем сигналы по тангажу и крены операцией atan2
                double Roll = (float)Math.Atan2(gL, -gU);
                double Pitch = -(float)Math.Atan2(gF, -gU);

                double Yaw = cockpit.obj.RotationIndicator.Y;
                return new Vector3D(Yaw, Pitch, Roll);
            }
            //-------------------------------------
            public MatrixD GetNormTransMatrixFromMyPos(Vector3D Up, Vector3D Left)
            {
                MatrixD mRot;
                Vector3D V3Dcenter = MyPos;
                Vector3D V3Dup = Up;
                if (gravity) V3Dup = -Vector3D.Normalize(GravVector);
                Vector3D V3Dleft = Vector3D.Normalize(Vector3D.Reject(Left, V3Dup));
                Vector3D V3Dfow = Vector3D.Normalize(Vector3D.Cross(V3Dleft, V3Dup));
                mRot = new MatrixD(V3Dleft.GetDim(0), V3Dleft.GetDim(1), V3Dleft.GetDim(2), 0, V3Dup.GetDim(0), V3Dup.GetDim(1), V3Dup.GetDim(2), 0, V3Dfow.GetDim(0), V3Dfow.GetDim(1), V3Dfow.GetDim(2), 0, 0, 0, 0, 1);
                mRot = MatrixD.Invert(mRot);
                return new MatrixD(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, -V3Dcenter.GetDim(0), -V3Dcenter.GetDim(1), -V3Dcenter.GetDim(2), 1) * mRot; // Конкатенация матриц
            }
            public void SetRCVectorAxis(string axis)
            {
                if (current_vector_axis != axis)
                {
                    thrusts.InitThrusts(cockpit, axis);
                    current_vector_axis = axis;
                }
            }
            public void SetDockMatrix(int num)
            {
                if (num > CountBase || num <= 0) return;
                if (con_d.Connected || con_b.Connected)
                {
                    string tag = con_d.Connected ? "D" : "B";
                    MatrixD DockMatrix = new MatrixD();
                    Vector3D ConnectorPoint = new Vector3D();
                    Vector3D BaseDockPoint = new Vector3D();
                    if (tag == "D")
                    {
                        DockMatrix = GetNormTransMatrixFromMyPos(WMCocpit.Up, WMCocpit.Left);
                        ConnectorPoint = new Vector3D(0, 0, dist_con_d);
                        BaseDockPoint = new Vector3D(0, 0, -200);
                    }
                    else if (tag == "B")
                    {
                        DockMatrix = GetNormTransMatrixFromMyPos(WMCocpit.Up, WMCocpit.Left);
                        ConnectorPoint = new Vector3D(0, 0, -dist_con_b);
                        BaseDockPoint = new Vector3D(0, 0, 200);
                    }
                    Vector3D pc = FindPlanetCenter();
                    BasePoints[num - 1] = new BaseStorage()
                    {
                        ConnectorTag = tag,
                        id = 0,
                        BaseDockPoint = BaseDockPoint,
                        ConnectorPoint = ConnectorPoint,
                        PlanetCenter = pc,
                        FlyHeight = gravity ? (Vector3D.Normalize(MyPos - pc) * DistFlyHeight + MyPos).Length() : 0,
                        DockMatrix = DockMatrix,
                    };
                    SaveToStorage();
                }
            }
            public Vector3D FindPlanetCenter()
            {
                Vector3D PlanetCenter;
                cockpit.obj.TryGetPlanetPosition(out PlanetCenter);
                return PlanetCenter;
            }
            public double GetBrakingLanding(double max_thrusts)
            {
                double a = (max_thrusts / 1000) * (1 / (TotalMass / 1000));
                double t = (0 - cockpit.obj.GetShipSpeed()) / -a; //t = (V - V[0]) / a
                S = (cockpit.obj.GetShipSpeed() * t) + ((-a) * Math.Pow(t, 2)) / 2; //S = V[0] * t + ( a * t^2 ) / 2
                return S;
            }
            //------------------------------------------------
            public void FlyConnectBase()
            {
                if (curent_mode == mode.none)
                {
                    curent_mode = mode.to_base;
                    SaveToStorage();
                }
                if (curent_mode == mode.to_base && ToBase())
                {
                    curent_mode = mode.dock;
                    SaveToStorage();
                }
                if (curent_mode == mode.dock && Dock())
                {
                    Clear();
                    curent_programm = programm.none;
                    GetCurrentBase(0);
                    SaveToStorage();
                }
            }
            //------------------------------------------------
            public void Horizon()
            {
                Vector3D gyrAng = GetNavAngles(TackVector);
                gyros.SetOverride(true, GetMyGyros(gyrAng) * GyroMult, 1);
            }
            public void GetCurrentBase(int num)
            {
                if (num > CountBase || num < 0) return;
                CurrNumBase = num;
                if (num > 0)
                {
                    CurrBase = BasePoints[num - 1];
                }
                else
                {
                    CurrBase = new BaseStorage();
                }
                SetRCVectorAxis(!string.IsNullOrWhiteSpace(CurrBase.ConnectorTag) ? CurrBase.ConnectorTag.Trim() : null);
            }
            public void Pause(bool enable)
            {
                if (enable)
                {
                    thrusts.ClearThrustOverridePersent();
                    gyros.SetOverride(false, 1);
                    paused = true;
                }
                else { paused = false; }
                SaveToStorage();
            }
            public void Clear()
            {
                thrusts.ClearThrustOverridePersent();
                gyros.SetOverride(false, 1);
                curent_mode = mode.none;
                manual_vector_axis = null;
                SaveToStorage();
            }
            public void Stop()
            {
                Clear();
                GetCurrentBase(0);
                curent_programm = programm.none;
                go_home = false;
                paused = false;
                SaveToStorage();
            }
            public bool ToBase()
            {
                bool Complete = false;
                if (string.IsNullOrWhiteSpace(CurrBase.ConnectorTag)) return Complete;
                float MaxUSpeed = 0, MaxFSpeed = 0; double VDistance = 0, HDistance = 0; float UpAccel = 0;
                if (gravity)
                {
                    SetRCVectorAxis("D");
                }
                else
                {
                    SetRCVectorAxis("F");
                }
                Vector3D gyrAng = GetNavAngles(CurrBase.BaseDockPoint, CurrBase.DockMatrix);
                Vector3D MyPosCon = Vector3D.Transform(MyPos, CurrBase.DockMatrix);
                gyros.SetOverride(true, gyrAng * GyroMult, 1);
                thrusts.SetOverridePercent("R", 0);
                thrusts.SetOverridePercent("L", 0);
                if (gravity)
                {
                    Vector3D vecTarget = CurrBase.BaseDockPoint - new Vector3D(MyPosCon.GetDim(0), 0, MyPosCon.GetDim(2));
                    Distance = (float)(vecTarget).Length();
                    if (FlyHeight == 0)
                    {
                        Vector3D grav = GravVector;
                        VDistance = Vector3D.ProjectOnVector(ref vecTarget, ref grav).Length();
                        HDistance = Vector3D.ProjectOnPlane(ref vecTarget, ref grav).Length();
                        MaxUSpeed = (float)Math.Sqrt(2 * Math.Abs(HDistance) * YMaxA) / 5f;
                        MaxFSpeed = (float)Math.Sqrt(2 * Math.Abs(VDistance) * ZMaxA) / 5f;
                    }
                    else
                    {
                        VDistance = Distance;
                        HDistance = (float)(FlyHeight - (MyPos - PlanetCenter).Length());
                        MaxUSpeed = (float)Math.Sqrt(2 * Math.Abs(HDistance) * YMaxA) / 5f;
                        MaxFSpeed = (float)Math.Sqrt(2 * Math.Abs(VDistance) * ZMaxA) / 5f;
                    }
                }
                else
                {
                    Distance = (float)(CurrBase.BaseDockPoint - MyPosCon).Length();
                    VDistance = Distance;
                    HDistance = MyPosCon.GetDim(1);
                    MaxUSpeed = (float)Math.Sqrt(2 * Math.Abs(HDistance) * YMaxA) / 5f;
                    MaxFSpeed = (float)Math.Sqrt(2 * Math.Abs(VDistance) * ZMaxA) / 5f;
                }
                if (Distance > TargetSize)
                {
                    if (UpVelocityVector.Length() < MaxUSpeed)
                        if (gravity)
                        {
                            if ((cockpit.CurrentHeight - GetBrakingLanding(thrusts.DownThrMax)) < MinHeight)
                            {
                                thrusts.SetOverridePercent("U", 0);
                            }
                            else
                            {
                                UpAccel = (float)(HDistance * AlignAccelMult);
                                thrusts.SetOverrideAccel("U", UpAccel);
                            }
                        }
                        else
                        {
                            UpAccel = -(float)(HDistance * AlignAccelMult);
                            thrusts.SetOverrideAccel("U", UpAccel);
                        }
                    else
                    {
                        thrusts.SetOverridePercent("U", 0);
                        thrusts.SetOverridePercent("D", 0);
                    }
                    if (ForwVelocityVector.Length() < MaxFSpeed)
                    {
                        thrusts.SetOverrideAccel("F", (float)(VDistance * AlignAccelMult));
                        //thrusts.SetOverridePercent("B", 0);
                    }
                    else
                    {
                        thrusts.SetOverridePercent("F", 0);
                        thrusts.SetOverridePercent("B", 0);
                    }
                }
                else
                {
                    Clear();
                    Complete = true;
                }
                OutStatusMode(MaxFSpeed, MaxUSpeed, 0, UpAccel, VDistance, HDistance);
                return Complete;
            }
            //public bool Dock()
            //{
            //    bool Complete = false;
            //    if (string.IsNullOrWhiteSpace(CurrBase.ConnectorTag)) return Complete;
            //    float MaxUSpeed, MaxLSpeed, MaxFSpeed;
            //    Vector3D MyPosCon = Vector3D.Transform(MyPos, CurrBase.DockMatrix);
            //    Vector3D gyrAng = GetNavAngles(CurrBase.ConnectorPoint, CurrBase.DockMatrix);
            //    if (gravity)
            //    {
            //        Distance = (float)((Vector3D.Reject(MyPosCon, Vector3D.Normalize(Vector3D.Transform(PlanetCenter, CurrBase.DockMatrix)))).Length() + CurrBase.ConnectorPoint.Length());
            //    }
            //    else
            //    {
            //       // Distance = (float)(CurrBase.BaseDockPoint - MyPosCon).Length();
            //        Distance = (float)((Vector3D.Reject(MyPosCon, Vector3D.Normalize(Vector3D.Transform(PlanetCenter, CurrBase.DockMatrix)))).Length() + CurrBase.ConnectorPoint.Length());
            //    }
            //    MaxLSpeed = (float)Math.Sqrt(2 * Math.Abs(MyPosCon.GetDim(0)) * XMaxA) / 3f;
            //    MaxUSpeed = (float)Math.Sqrt(2 * Math.Abs(MyPosCon.GetDim(1)) * YMaxA) / 5f;
            //    MaxFSpeed = (float)Math.Sqrt(2 * Distance * ZMaxA) / 3f;
            //    if (Distance < 15 + (CurrBase.ConnectorTag == "B" ? dist_con_b : dist_con_d))
            //        MaxFSpeed = MaxFSpeed / 5;
            //    if (Math.Abs(MyPosCon.GetDim(1)) < 1)
            //        MaxUSpeed = 0.1f;
            //    gyros.SetOverride(true, gyrAng * GyroMult, 1);
            //    if (LeftVelocityVector.Length() < MaxLSpeed)
            //        thrusts.SetOverrideAccel("R", (float)(MyPosCon.GetDim(0) * AlignAccelMult));
            //    else
            //    {
            //        thrusts.SetOverridePercent("R", 0);
            //        thrusts.SetOverridePercent("L", 0);
            //    }
            //    float UpAccel = -(float)(MyPosCon.GetDim(1) * AlignAccelMult);
            //    float minUpAccel = 0.1f;
            //    if ((UpAccel < 0) && (UpAccel > -minUpAccel))
            //        UpAccel = -minUpAccel;
            //    if ((UpAccel > 0) && (UpAccel < minUpAccel))
            //        UpAccel = minUpAccel;
            //    if (UpVelocityVector.Length() < MaxUSpeed)
            //        thrusts.SetOverrideAccel("U", UpAccel);
            //    else { thrusts.SetOverridePercent("U", 0); }
            //    if (((Distance > 100) || ((Math.Abs(MyPosCon.GetDim(0)) < (Distance / 10 + 0.2f)) && (Math.Abs(MyPosCon.GetDim(1)) < (Distance / 10 + 0.2f)))) && (ForwVelocityVector.Length() < MaxFSpeed))
            //    {
            //        thrusts.SetOverrideAccel("F", (float)(Distance * AlignAccelMult));
            //        thrusts.SetOverridePercent("B", 0);
            //    }
            //    else
            //    {
            //        thrusts.SetOverridePercent("F", 0);
            //        thrusts.SetOverridePercent("B", 0);
            //    }
            //    if (Distance < 6 + (CurrBase.ConnectorTag == "B" ? dist_con_b : dist_con_d))
            //    {
            //        if (con_b.Status == MyShipConnectorStatus.Connectable)
            //        {
            //            con_b.obj.Connect();
            //        }
            //        if (con_d.Status == MyShipConnectorStatus.Connectable)
            //        {
            //            con_d.obj.Connect();
            //        }
            //        if (con_b.Status == MyShipConnectorStatus.Connected || con_d.Status == MyShipConnectorStatus.Connected)
            //        {
            //            Clear();
            //            Complete = true;
            //        }
            //    }
            //    OutStatusMode(MaxFSpeed, MaxUSpeed, MaxLSpeed, UpAccel, 0, 0);
            //    return Complete;
            //}
            public bool Dock()
            {
                bool Complete = false;
                if (string.IsNullOrWhiteSpace(CurrBase.ConnectorTag)) return Complete;
                float MaxUSpeed, MaxLSpeed, MaxFSpeed;
                Vector3D MyPosCon = Vector3D.Transform(MyPos, CurrBase.DockMatrix);
                Vector3D gyrAng = GetNavAngles(CurrBase.ConnectorPoint, CurrBase.DockMatrix);
                float Distance = (float)((Vector3D.Reject(MyPosCon, Vector3D.Normalize(Vector3D.Transform(PlanetCenter, CurrBase.DockMatrix)))).Length() + CurrBase.ConnectorPoint.Length());

                MaxLSpeed = (float)Math.Sqrt(2 * Math.Abs(MyPosCon.GetDim(0)) * XMaxA) / 3;
                MaxUSpeed = (float)Math.Sqrt(2 * Math.Abs(MyPosCon.GetDim(1)) * YMaxA) / 3;
                MaxFSpeed = (float)Math.Sqrt(2 * Distance * ZMaxA) / 3;
                if (Distance < 15 + (CurrBase.ConnectorTag == "B" ? dist_con_b : dist_con_d))
                    MaxFSpeed = MaxFSpeed / 5;
                if (Math.Abs(MyPosCon.GetDim(1)) < 1)
                    MaxUSpeed = 0.1f;
                gyros.SetOverride(true, gyrAng * GyroMult, 1);
                if (LeftVelocityVector.Length() < MaxLSpeed)
                    thrusts.SetOverrideAccel("R", (float)(MyPosCon.GetDim(0) * AlignAccelMult));
                else
                {
                    thrusts.SetOverridePercent("R", 0);
                    thrusts.SetOverridePercent("L", 0);
                }
                float UpAccel = -(float)(MyPosCon.GetDim(1) * AlignAccelMult);
                float minUpAccel = 0.3f;
                if ((UpAccel < 0) && (UpAccel > -minUpAccel))
                    UpAccel = -minUpAccel;
                if ((UpAccel > 0) && (UpAccel < minUpAccel))
                    UpAccel = minUpAccel;
                if (UpVelocityVector.Length() < MaxUSpeed)
                    thrusts.SetOverrideAccel("U", UpAccel);
                else
                {
                    thrusts.SetOverridePercent("U", 0);
                }
                if (((Distance > 100) || ((Math.Abs(MyPosCon.GetDim(0)) < (Distance / 10 + 0.2f)) && (Math.Abs(MyPosCon.GetDim(1)) < (Distance / 10 + 0.2f)))) && (ForwVelocityVector.Length() < MaxFSpeed))
                {
                    thrusts.SetOverrideAccel("F", (float)(Distance * AlignAccelMult));
                    thrusts.SetOverridePercent("B", 0);
                }
                else
                {
                    thrusts.SetOverridePercent("F", 0);
                    thrusts.SetOverridePercent("B", 0);
                }
                if (Distance < 6 + (CurrBase.ConnectorTag == "B" ? dist_con_b : dist_con_d))
                {
                    if (con_b.Status == MyShipConnectorStatus.Connectable)
                    {
                        con_b.obj.Connect();
                    }
                    if (con_d.Status == MyShipConnectorStatus.Connectable)
                    {
                        con_d.obj.Connect();
                    }
                    if (con_b.Status == MyShipConnectorStatus.Connected || con_d.Status == MyShipConnectorStatus.Connected)
                    {
                        Clear();
                        Complete = true;
                    }
                }
                OutStatusMode(MaxFSpeed, MaxUSpeed, MaxLSpeed, UpAccel, 0, 0);
                return Complete;
            }
            public bool UnDock()
            {
                bool Complete = false;
                if (string.IsNullOrWhiteSpace(CurrBase.ConnectorTag)) return Complete;
                thrusts.On();
                Distance = 0;
                if (con_b.Status == MyShipConnectorStatus.Connected) con_b.obj.Disconnect();
                if (con_d.Status == MyShipConnectorStatus.Connected) con_d.obj.Disconnect();
                if (con_b.Status != MyShipConnectorStatus.Connected && con_d.Status != MyShipConnectorStatus.Connected)
                {
                    Vector3D MyPosCon = Vector3D.Transform(MyPos, CurrBase.DockMatrix);
                    Vector3D gyrAng = GetNavAngles(CurrBase.ConnectorPoint, CurrBase.DockMatrix);
                    Distance = (float)((Vector3D.Reject(MyPosCon, Vector3D.Normalize(Vector3D.Transform(PlanetCenter, CurrBase.DockMatrix)))).Length() + CurrBase.ConnectorPoint.Length());
                    gyros.SetOverride(true, gyrAng * GyroMult, 1);
                    thrusts.SetOverridePercent("U", 0);
                    thrusts.SetOverridePercent("R", 0);
                    thrusts.SetOverridePercent("L", 0);
                    thrusts.SetOverridePercent("F", 0);
                    thrusts.SetOverrideAccel("B", 3);
                    if (Distance > 50 + (CurrBase.ConnectorTag == "B" ? 30 : 9))
                    {
                        thrusts.SetOverrideAccel("B", 0);
                        Complete = true;
                    }
                }
                OutStatusMode(0, 0, 0, 0, 0, 0);
                return Complete;
            }
            public bool Takeoff()
            {
                bool Complete = false;
                if (string.IsNullOrWhiteSpace(CurrBase.ConnectorTag)) return Complete;
                Vector3D MyPosCon = Vector3D.Transform(MyPos, CurrBase.DockMatrix);
                Vector3D gyrAng = new Vector3D();
                Vector3D vecTarget = CurrBase.BaseDockPoint - new Vector3D(MyPosCon.GetDim(0), 0, MyPosCon.GetDim(2));
                Distance = (float)(vecTarget).Length();
                Vector3D grav = GravVector;
                Vector3D vert = Vector3D.ProjectOnVector(ref vecTarget, ref grav);
                Vector3D hors = Vector3D.ProjectOnPlane(ref vecTarget, ref grav);
                thrusts.SetOverridePercent("R", 0);
                thrusts.SetOverridePercent("L", 0);
                if (Distance > TargetSize)
                {
                    if (gravity)
                    {
                        SetRCVectorAxis("D");
                        gyrAng = GetNavAngles(CurrBase.ConnectorPoint, CurrBase.DockMatrix);
                        gyros.SetOverride(true, gyrAng * GyroMult, 1);
                        if (UpVelocityVector.Length() < 100f) //MaxUSpeed
                        {
                            thrusts.SetOverrideAccel("U", (float)(vert.Length() * AlignAccelMult));
                        }
                        else
                        {
                            thrusts.SetOverrideAccel("U", 0);
                        }
                        if (ForwVelocityVector.Length() < 100f)
                        {
                            thrusts.SetOverrideAccel("F", (float)(hors.Length() * AlignAccelMult));
                        }
                        else
                        {
                            thrusts.SetOverrideAccel("F", 0);
                        }
                    }
                    else
                    {
                        SetRCVectorAxis("F");
                        gyrAng = GetNavAnglesCurse(CurrBase.ConnectorPoint, CurrBase.DockMatrix);
                        gyros.SetOverride(true, gyrAng * GyroMult, 1);
                        if (ForwVelocityVector.Length() < 100f)
                        {
                            thrusts.On();
                            thrusts.SetOverridePercent("F", 1);
                            thrusts.SetOverridePercent("B", 0);
                        }
                        else
                        {
                            thrusts.Off();
                            thrusts.SetOverridePercent("F", 0);
                            thrusts.SetOverridePercent("B", 0);
                        }
                    }
                }
                else
                {
                    Clear();
                    Complete = true;
                }
                OutStatusMode(0, 0, 0, 0, vert.Length(), hors.Length());
                return Complete;
            }
            public bool Landing()
            {
                bool Complete = false;
                if (string.IsNullOrWhiteSpace(CurrBase.ConnectorTag)) return Complete;
                Vector3D MyPosCon = Vector3D.Transform(MyPos, CurrBase.DockMatrix);
                Vector3D gyrAng = new Vector3D();
                Vector3D vecTarget = CurrBase.BaseDockPoint - new Vector3D(MyPosCon.GetDim(0), 0, MyPosCon.GetDim(2));
                Distance = (float)(vecTarget).Length();
                if (gravity && ((cockpit.CurrentHeight - GetBrakingLanding(thrusts.DownThrMax)) < MinHeight))
                {
                    thrusts.On();
                    SetRCVectorAxis("D");
                    gyrAng = GetNavAngles(CurrBase.ConnectorPoint, CurrBase.DockMatrix);
                    gyros.SetOverride(true, gyrAng * GyroMult, 1);
                    Clear();
                    Complete = true;
                }
                else
                {
                    thrusts.SetOverridePercent("R", 0);
                    thrusts.SetOverridePercent("L", 0);
                    SetRCVectorAxis("F");
                    gyrAng = GetNavAnglesCurse(CurrBase.ConnectorPoint, CurrBase.DockMatrix);
                    gyros.SetOverride(true, gyrAng * GyroMult, 1);
                    thrusts.SetOverridePercent("U", 0);
                    thrusts.SetOverridePercent("D", 0);
                    if (ForwVelocityVector.Length() < 100f)
                    {
                        thrusts.On();
                        thrusts.SetOverridePercent("F", 1);
                        thrusts.SetOverridePercent("B", 0);
                    }
                    else
                    {
                        thrusts.Off();
                        thrusts.SetOverridePercent("F", 0);
                        thrusts.SetOverridePercent("B", 0);
                    }
                }
                return Complete;
            }
            public bool Сourse()
            {
                bool Complete = false;
                Vector3D gyrAng = GetNavAnglesCurse(TackVector);
                gyros.SetOverride(true, gyrAng * GyroMult, 1);
                return Complete;
            }
            public void OutStatusMode(float MaxFSpeed, float MaxUSpeed, float MaxLSpeed, float UpAccel, double vert, double hors)
            {
                StringBuilder values = new StringBuilder();
                values.Append(" STATUS\n");
                //values.Append("ПРОГРАММА   : " + name_programm[(int)curent_programm] + "\n");
                values.Append("ЭТАП        : " + name_mode[(int)curent_mode] + "\n");
                values.Append("Distance : " + Math.Round(Distance).ToString() + "\n");
                values.Append("VER      : " + Math.Round(vert).ToString() + ", UpAccel: " + Math.Round(UpAccel).ToString() + "\n");
                values.Append("HOR      : " + Math.Round(hors).ToString() + "\n");
                values.Append("DeltaHeight: " + Math.Round(FlyHeight - (MyPos - PlanetCenter).Length()).ToString() + "\n");
                //values.Append("UpAccel: " + Math.Round(UpAccel).ToString() + "\n");
                values.Append("Гравитация: " + (gravity ? igreen.ToString() : ired.ToString()) + "\n");
                values.Append("------------------------------------------\n");
                values.Append("ZMaxA (F-B) : " + Math.Round(ZMaxA, 2).ToString() + ", MaxFSpeed: " + Math.Round(MaxFSpeed, 2).ToString() + "\n");
                values.Append("YMaxA (U-D) : " + Math.Round(YMaxA, 2).ToString() + ", MaxUSpeed: " + Math.Round(MaxUSpeed, 2).ToString() + "\n");
                values.Append("XMaxA (L-R) : " + Math.Round(XMaxA, 2).ToString() + ", MaxLSpeed: " + Math.Round(MaxLSpeed, 2).ToString() + "\n");
                values.Append(thrusts.TextInfo());
                lcd_debug.OutText(values);
            }
            public string GetStringVal(string Key, string str)
            {
                string val = null;
                string pattern = @"(" + Key + "):([^:^;]+);";
                System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(str.Replace("\n", ""), pattern);
                if (match.Success)
                {
                    val = match.Groups[2].Value.Trim();
                    if (val == "null") val = null;
                }
                return val;
            }
            public double GetVal(string Key, string str)
            {
                string val = "0";
                string pattern = @"(" + Key + "):([^:^;]+);";
                System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(str.Replace("\n", ""), pattern);
                if (match.Success)
                {
                    val = match.Groups[2].Value;
                }
                return Convert.ToDouble(val);
            }
            public int GetValInt(string Key, string str)
            {
                string val = "0";
                string pattern = @"(" + Key + "):([^:^;]+);";
                System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(str.Replace("\n", ""), pattern);
                if (match.Success)
                {
                    val = match.Groups[2].Value;
                }
                return Convert.ToInt32(val);
            }
            public long GetValInt64(string Key, string str)
            {
                string val = "0";
                string pattern = @"(" + Key + "):([^:^;]+);";
                System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(str.Replace("\n", ""), pattern);
                if (match.Success)
                {
                    val = match.Groups[2].Value;
                }
                return Convert.ToInt64(val);
            }
            public bool GetValBool(string Key, string str)
            {
                string val = "False";
                string pattern = @"(" + Key + "):([^:^;]+);";
                System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(str.Replace("\n", ""), pattern);
                if (match.Success)
                {
                    val = match.Groups[2].Value;
                }
                return Convert.ToBoolean(val);
            }
            public void LoadFromStorage()
            {
                StringBuilder str = lcd_storage.GetText();
                curent_programm = (programm)GetValInt("curent_programm", str.ToString());
                curent_mode = (mode)GetValInt("curent_mode", str.ToString());
                CountBase = GetValInt("CountBase", str.ToString());
            }
            public BaseStorage LoadBaseStorage(int num)
            {
                StringBuilder str = lcd_storage.GetText();
                string ConnectorTag = GetStringVal("ConnectorTag" + num, str.ToString());
                BaseStorage result = new BaseStorage()
                {
                    ConnectorTag = ConnectorTag,
                    id = GetValInt64("ID" + num, str.ToString()),
                    BaseDockPoint = new Vector3D(GetVal("BDPX" + num, str.ToString()), GetVal("BDPY" + num, str.ToString()), GetVal("BDPZ" + num, str.ToString())),
                    ConnectorPoint = new Vector3D(GetVal("CPX" + num, str.ToString()), GetVal("CPY" + num, str.ToString()), GetVal("CPZ" + num, str.ToString())),
                    PlanetCenter = new Vector3D(GetVal("PCX" + num, str.ToString()), GetVal("PCY" + num, str.ToString()), GetVal("PCZ" + num, str.ToString())),
                    FlyHeight = GetVal("FlyHeight" + num, str.ToString()),
                    DockMatrix = new MatrixD(GetVal("DM" + num + "_11", str.ToString()), GetVal("DM" + num + "_12", str.ToString()), GetVal("DM" + num + "_13", str.ToString()), GetVal("DM" + num + "_14", str.ToString()),
                    GetVal("DM" + num + "_21", str.ToString()), GetVal("DM" + num + "_22", str.ToString()), GetVal("DM" + num + "_23", str.ToString()), GetVal("DM" + num + "_24", str.ToString()),
                    GetVal("DM" + num + "_31", str.ToString()), GetVal("DM" + num + "_32", str.ToString()), GetVal("DM" + num + "_33", str.ToString()), GetVal("DM" + num + "_34", str.ToString()),
                    GetVal("DM" + num + "_41", str.ToString()), GetVal("DM" + num + "_42", str.ToString()), GetVal("DM" + num + "_43", str.ToString()), GetVal("DM" + num + "_44", str.ToString())),
                };
                return result;
            }
            public void SaveToStorage()
            {
                StringBuilder values = new StringBuilder();
                values.Append("curent_programm: " + ((int)curent_programm).ToString() + ";\n");
                values.Append("curent_mode: " + ((int)curent_mode).ToString() + ";\n");
                values.Append("CountBase: " + CountBase.ToString() + ";\n");
                for (int i = 0; i < CountBase; i++)
                {
                    values.Append("ConnectorTag" + i + ":" + (BasePoints[i].ConnectorTag != null ? BasePoints[i].ConnectorTag : "null") + ";\n");
                    values.Append("ID" + i + ":" + BasePoints[i].id + ";\n");
                    values.Append(BasePoints[i].BaseDockPoint.ToString().Replace("}", "").Replace("{", "").Replace(" ", " ").Replace(" ", ";\n").Replace("X", "BDPX" + i).Replace("Y", "BDPY" + i).Replace("Z", "BDPZ" + i) + ";\n");
                    values.Append(BasePoints[i].ConnectorPoint.ToString().Replace("}", "").Replace("{", "").Replace(" ", " ").Replace(" ", ";\n").Replace("X", "CPX" + i).Replace("Y", "CPY" + i).Replace("Z", "CPZ" + i) + ";\n");
                    values.Append(BasePoints[i].PlanetCenter.ToString().Replace("}", "").Replace("{", "").Replace(" ", " ").Replace(" ", ";\n").Replace("X", "PCX" + i).Replace("Y", "PCY" + i).Replace("Z", "PCZ" + i) + ";\n");
                    values.Append("FlyHeight" + i + ":" + Math.Round(BasePoints[i].FlyHeight, 0) + ";\n");
                    values.Append(BasePoints[i].DockMatrix.ToString().Replace("}", "").Replace("{", "").Replace(" ", " ").Replace(" ", ";\n").Replace("M", "DM" + i + "_"));
                }
                lcd_storage.OutText(values);
            }
            //------------------------------------------------
            public string TextInfo1()
            {
                StringBuilder values = new StringBuilder();
                values.Append("СКОРОСТЬ    : " + Math.Round(cockpit.obj.GetShipSpeed(), 2) + "\n");
                values.Append("ВЫСОТА    : " + Math.Round(cockpit.CurrentHeight, 2) + ", Sт : " + Math.Round(S, 2) + "\n");
                values.Append("ГОРИЗОНТ    : " + (current_vector_axis != null ? igreen.ToString() : ired.ToString()) + ",  Vector : " + (TackVector != null ? igreen.ToString() : ired.ToString()) + "\n");
                values.Append("ПРОГРАММА   : " + name_programm[(int)curent_programm] + "\n");
                values.Append("ЭТАП        : " + name_mode[(int)curent_mode] + "\n");
                values.Append("ПАУЗА : " + (paused ? igreen.ToString() : ired.ToString()) + "\n");
                values.Append("ДОМОЙ : " + (go_home ? igreen.ToString() : ired.ToString()) + "\n");
                return values.ToString();
            }
            public string TextInfo2()
            {
                StringBuilder values = new StringBuilder();
                values.Append("current_vector_axis: " + current_vector_axis + "\n");
                values.Append("ForwVel  : " + Math.Round(ForwVelocityVector.Length(), 1) + "\n");
                values.Append("UpVel    : " + Math.Round(UpVelocityVector.Length(), 1) + "\n");
                values.Append("LeftVel  : " + Math.Round(LeftVelocityVector.Length(), 1) + "\n");
                //values.Append("DockMatrix: " + CurrBase.DockMatrix.ToString() + "\n");
                //values.Append(PText.GetGPS("DockPoint:", CurrBase.BaseDockPoint) + "\n");
                //values.Append(PText.GetGPS("ConnPoint:", CurrBase.ConnectorPoint) + "\n");
                values.Append("Curr_base: " + CurrNumBase + "\n");
                values.Append("status: " + status + "\n");
                values.Append("Curr_Connector: " + CurrBase.ConnectorTag + "\n");
                //values.Append(PText.GetGPS("PlanetCenter:", PlanetCenter) + "\n");
                values.Append("Height            : " + Math.Round((MyPos - PlanetCenter).Length()).ToString() + " / " + Math.Round(FlyHeight).ToString() + "\n");
                values.Append("Distance          : " + Math.Round(Distance).ToString() + "\n");
                //values.Append("Phys./Crit.(Mass) : " + Math.Round(PhysicalMass).ToString() + " / " + CriticalMass + " " + (CriticalMassReached ? red.ToString() : green.ToString()) + "\n");
                //values.Append("Volume/Mass       : " + cargos.CurrentVolume + " / " + cargos.CurrentMass + "\n");
                //values.Append("Батарея %         : " + PText.GetPersent(batterys.CurrentPersent()) + " " + (batterys.CurrentPersent() <= OnCharge ? ired.ToString() : (batterys.CurrentPersent() >= OffCharge ? igreen.ToString() : iyellow.ToString())) + "\n");
                return values.ToString();
            }
            //------------------------------------------------
            public void Logic(string argument, UpdateType updateSource)
            {
                switch (argument)
                {
                    case "0":
                        thrusts.ClearThrustOverridePersent();
                        break;
                    case "save_course":
                        TackVector = camera_course.GetVectorForward();
                        break;
                    case "U+":
                        thrusts.SetOverridePercent("U", 1f);
                        break;
                    case "D+":
                        thrusts.SetOverridePercent("D", 1f);
                        break;
                    case "dh":
                        if (curent_programm == programm.none) { if (manual_vector_axis == "D") { manual_vector_axis = null; } else { manual_vector_axis = "D"; } }
                        break;
                    case "df":
                        if (curent_programm == programm.none) { if (manual_vector_axis == "F") { manual_vector_axis = null; } else { manual_vector_axis = "F"; } }
                        break;
                    case "db":
                        if (curent_programm == programm.none) { if (manual_vector_axis == "B") { manual_vector_axis = null; } else { manual_vector_axis = "B"; } }
                        break;
                    case "load":
                        LoadFromStorage();
                        break;
                    case "save":
                        SaveToStorage();
                        break;
                    case "pause":
                        Pause(!paused);
                        break;
                    case "stop":
                        Stop();
                        break;
                    case "clear":
                        Clear();
                        curent_programm = programm.none;
                        break;
                    case "save_base1":
                        SetDockMatrix(1);
                        break;
                    case "save_base2":
                        SetDockMatrix(2);
                        break;
                    case "fly_base1":
                        curent_programm = programm.fly_connect_base1;
                        SaveToStorage();
                        break;
                    case "fly_base2":
                        curent_programm = programm.fly_connect_base2;
                        SaveToStorage();
                        break;
                    case "go_home": { go_home = true; break; }
                    case "to_base1":
                        GetCurrentBase(1);
                        curent_mode = mode.to_base;
                        SaveToStorage();
                        break;
                    case "dock1":
                        GetCurrentBase(1);
                        curent_mode = mode.dock;
                        SaveToStorage();
                        break;
                    case "un_dock1":
                        GetCurrentBase(1);
                        curent_mode = mode.un_dock;
                        SaveToStorage();
                        break;
                    case "to_base2":
                        GetCurrentBase(2);
                        curent_mode = mode.to_base;
                        SaveToStorage();
                        break;
                    case "dock2":
                        GetCurrentBase(2);
                        curent_mode = mode.dock;
                        SaveToStorage();
                        break;
                    case "un_dock2":
                        GetCurrentBase(2);
                        curent_mode = mode.un_dock;
                        SaveToStorage();
                        break;
                    case "takeoff":
                        GetCurrentBase(2);
                        curent_mode = mode.takeoff;
                        SaveToStorage();
                        break;
                    case "landing":
                        GetCurrentBase(1);
                        curent_mode = mode.landing;
                        SaveToStorage();
                        break;
                    case "course":
                        GetCurrentBase(0);
                        curent_mode = mode.course;
                        SaveToStorage();
                        break;
                    default:
                        break;
                }
                if (updateSource == UpdateType.Update10)
                {
                    cockpit.Logic(argument, updateSource);
                    if (!con_b.Connected && !con_d.Connected)
                    {
                        if (gravity && cockpit.CurrentHeight > 5.0f)
                        {
                            batterys.Auto();
                            thrusts.On();
                        }
                    }
                    else
                    {
                        // Припаркован
                        batterys.Charger();
                        thrusts.Off();
                    }
                    // Обновим состояние навигации
                    UpdateCalc();
                    if (curent_programm == programm.none && curent_mode == mode.none)
                    {
                        if (manual_vector_axis != null)
                        {
                            SetRCVectorAxis(manual_vector_axis);
                            Horizon();
                        }
                        else
                        {
                            GetCurrentBase(0);
                            gyros.SetOverride(false, 1);
                        }
                    }
                    else
                    {
                        manual_vector_axis = null;
                    }
                    if (curent_programm == programm.fly_connect_base1 && !paused)
                    {
                        if (CurrNumBase != 1) GetCurrentBase(1);
                        FlyConnectBase();
                    }
                    if (curent_programm == programm.fly_connect_base2 && !paused)
                    {
                        if (CurrNumBase != 2) GetCurrentBase(2);
                        FlyConnectBase();
                    }
                    if (curent_mode == mode.un_dock && !paused)
                    {
                        if (UnDock() && curent_programm == programm.none)
                        {
                            GetCurrentBase(0);
                            curent_mode = mode.none;
                        }
                    }
                    if (curent_mode == mode.to_base && !paused)
                    {
                        if (ToBase() && curent_programm == programm.none)
                        {
                            GetCurrentBase(0);
                            curent_mode = mode.none;
                        }
                    }
                    if (curent_mode == mode.dock && !paused)
                    {
                        if (Dock() && curent_programm == programm.none)
                        {
                            GetCurrentBase(0);
                            curent_mode = mode.none;
                        }
                    }
                    if (curent_mode == mode.takeoff && !paused)
                    {
                        if (Takeoff() && curent_programm == programm.none)
                        {
                            GetCurrentBase(0);
                            curent_mode = mode.none;
                        }
                    }
                    if (curent_mode == mode.landing && !paused)
                    {
                        if (Landing() && curent_programm == programm.none)
                        {
                            GetCurrentBase(0);
                            curent_mode = mode.none;
                        }
                    }
                    if (curent_mode == mode.course && !paused)
                    {
                        if (Сourse() && curent_programm == programm.none)
                        {
                            GetCurrentBase(0);
                            curent_mode = mode.none;
                        }
                    }
                }
            }
        }
    }
}

