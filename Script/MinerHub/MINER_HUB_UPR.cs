using Sandbox.Definitions;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.GameSystems;
using Sandbox.Game.WorldEnvironment.Modules;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using VRage.Game;
using VRage.Game.ModAPI.Ingame;
using VRage.Noise.Combiners;
using VRageMath;
/// <summary>
/// v1.0
/// </summary>
namespace MINER_HUB_UPR
{
    public sealed class Program : MyGridProgram
    {
        // v1
        string NameObj = "[MINER_HUB]";
        static string tag_batterys_duty = "[batterys_duty]"; // дежурная батарея
        const char green = '\uE001';
        const char blue = '\uE002';
        const char red = '\uE003';
        const char yellow = '\uE004';
        const char darkGrey = '\uE00F';
        static LCD lcd_storage;
        static LCD lcd_debug;
        static LCD lcd_name;
        Batterys bats;
        //Connector connector;
        ReflectorsLight reflectors_light;
        Cockpit cockpit;
        Cargos cargos;
        Power power;
        static Program _scr;
        public class PText
        {
            static public string GetVector3D(Vector3D vector)
            {
                return "X: " + Math.Round(vector.GetDim(0), 2).ToString() + "Y: " + Math.Round(vector.GetDim(1), 2).ToString() + "Z: " + Math.Round(vector.GetDim(2), 2).ToString();
            }
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
            //static public string GetCurrentOfMax(float cur, float max, string units)
            //{
            //    return "[ " + Math.Round(cur, 1) + units + " / " + Math.Round(max, 1) + units + " ]";
            //}
            static public string GetValueOfUnits(float value, string units)
            {
                if (value < 0.1)
                {
                    value = value * 1000; // K
                    return Math.Round(value, 1).ToString() + "k" + units;

                }
                else if (value < 0.001)
                {
                    value = value * 1000000; // K
                    return Math.Round(value, 1).ToString() + units;
                }
                return Math.Round(value, 1).ToString() + "M" + units;
            }
            static public string GetCurrentOfMax(float cur, float max, string units)
            {
                return "[ " + GetValueOfUnits(cur, units) + " / " + GetValueOfUnits(max, units) + " ]";
            }
            static public string GetSign(int x, int y)
            {
                if (x == y) { return "="; }
                if (x > y) { return ">"; }
                if (x < y) { return "<"; }
                return "";
            }
            static public string GetCountObj(int count, int count_on)
            {
                return count_on + GetSign(count_on, count) + count;
            }
            //------------------------------------------------------------------------
            static public string GetCapacityTanks(double perse, float capacity)
            {
                return "[ " + Math.Round(((perse * capacity) / 1000000), 1) + "МЛ / " + Math.Round((capacity / 1000000), 1) + "МЛ ]";
            }
            static public string GetThrust(float value)
            {
                return Math.Round(value / 1000000, 1) + "МН";
            }
            static public string GetCurrentThrust(float to, float cur, float max)
            {
                return "[ " + GetThrust(to) + " / " + GetThrust(cur) + " / " + GetThrust(max) + " ]";
            }
            static public string GetCurrentThrust(float cur, float max)
            {
                return "[ " + GetThrust(cur) + " / " + GetThrust(max) + " ]";
            }
            static public string GetCountDetaliThrust(int count_a, int count_h, int count_i, int count_on_a, int count_on_h, int count_on_i)
            {
                string result = "[";
                if (count_a > 0)
                {
                    result += "{А-" + count_on_a + "|" + count_a + "}";
                }
                if (count_h > 0)
                {
                    result += "{В-" + count_on_h + "|" + count_h + "}";
                }
                if (count_i > 0)
                {
                    result += "{И-" + count_on_i + "|" + count_i + "}";
                }
                result += "]";
                return result;
            }
            static public string GetGPS(string name, Vector3D target)
            {
                return "GPS:" + name + ":" + target.GetDim(0) + ":" + target.GetDim(1) + ":" + target.GetDim(2) + ":\n";
            }
        }
        public class InpResurs
        {
            public string SubtypeId { get; set; } = null;
            public float current { get; set; } = 0;
            public float max { get; set; } = 0;
            public bool is_power_by { get; set; } = false;
        }
        public class OutResurs
        {
            public string SubtypeId { get; set; } = null;
            public float current { get; set; } = 0;
            public float max { get; set; } = 0;
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
            // Команды включения\выключения
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
            public List<InpResurs> GetInpResurs(string name)
            {
                List<InpResurs> result = new List<InpResurs>();
                // Потребляемый ресурсы
                MyResourceSinkComponent sink;
                foreach (T obj in list_obj)
                {
                    ((IMyTerminalBlock)obj).Components.TryGet<MyResourceSinkComponent>(out sink);
                    if (sink != null)
                    {
                        var list = sink.AcceptedResources;
                        foreach (MyDefinitionId def in list)
                        {
                            if (String.IsNullOrWhiteSpace(name) || !String.IsNullOrWhiteSpace(name) && def.SubtypeId.ToString() == name)
                            {
                                InpResurs resurs = result.Where(r => r.SubtypeId == def.SubtypeId.ToString()).FirstOrDefault();
                                if (resurs == null)
                                {
                                    resurs = new InpResurs()
                                    {
                                        SubtypeId = def.SubtypeId.ToString(),
                                        is_power_by = sink.IsPoweredByType(def),
                                        max = sink.MaxRequiredInputByType(def),
                                        current = sink.CurrentInputByType(def)
                                    };
                                }
                                else
                                {
                                    bool is_power_by = sink.IsPoweredByType(def);
                                    resurs.max = sink.MaxRequiredInputByType(def);
                                    resurs.current = sink.CurrentInputByType(def);
                                }
                                result.Add(resurs);
                            }
                        }
                    }
                }
                return result;
            }
            public List<OutResurs> GetOutResurs(string name)
            {
                List<OutResurs> result = new List<OutResurs>();
                MyResourceSourceComponent source;
                foreach (T obj in list_obj)
                {
                    ((IMyTerminalBlock)obj).Components.TryGet<MyResourceSourceComponent>(out source);
                    if (source != null)
                    {
                        var list = source.ResourceTypes;
                        foreach (MyDefinitionId def in list)
                        {
                            if (String.IsNullOrWhiteSpace(name) || !String.IsNullOrWhiteSpace(name) && def.SubtypeId.ToString() == name)
                            {
                                OutResurs resurs = result.Where(r => r.SubtypeId == def.SubtypeId.ToString()).FirstOrDefault();
                                if (resurs == null)
                                {
                                    resurs = new OutResurs()
                                    {
                                        SubtypeId = def.SubtypeId.ToString(),
                                        max = source.DefinedOutputByType(def),
                                        current = source.CurrentOutputByType(def)
                                    };
                                }
                                else
                                {
                                    resurs.max = source.DefinedOutputByType(def);
                                    resurs.current = source.CurrentOutputByType(def);
                                }
                                result.Add(resurs);
                            }
                        }
                    }
                }
                return result;
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
            // Команды включения\выключения
            public void Off()
            {
                if (obj != null) ((IMyTerminalBlock)obj).ApplyAction("OnOff_Off");
            }
            public void On()
            {
                if (obj != null) ((IMyTerminalBlock)obj).ApplyAction("OnOff_On");
            }
            public List<InpResurs> GetInpResurs(string name)
            {
                List<InpResurs> result = new List<InpResurs>();
                // Потребляемый ресурсы
                MyResourceSinkComponent sink;
                ((IMyTerminalBlock)obj).Components.TryGet<MyResourceSinkComponent>(out sink);
                if (sink != null)
                {
                    var list = sink.AcceptedResources;
                    foreach (MyDefinitionId def in list)
                    {
                        if (String.IsNullOrWhiteSpace(name) || !String.IsNullOrWhiteSpace(name) && def.SubtypeId.ToString() == name)
                        {
                            InpResurs resurs = new InpResurs()
                            {
                                SubtypeId = def.SubtypeId.ToString(),
                                is_power_by = sink.IsPoweredByType(def),
                                max = sink.MaxRequiredInputByType(def),
                                current = sink.CurrentInputByType(def)
                            };
                            result.Add(resurs);
                        }
                    }
                }
                return result;
            }
            public List<OutResurs> GetOutResurs(string name)
            {
                List<OutResurs> result = new List<OutResurs>();
                MyResourceSourceComponent source;
                ((IMyTerminalBlock)obj).Components.TryGet<MyResourceSourceComponent>(out source);
                if (source != null)
                {
                    var list = source.ResourceTypes;
                    foreach (MyDefinitionId def in list)
                    {
                        if (String.IsNullOrWhiteSpace(name) || !String.IsNullOrWhiteSpace(name) && def.SubtypeId.ToString() == name)
                        {
                            OutResurs resurs = new OutResurs()
                            {
                                SubtypeId = def.SubtypeId.ToString(),
                                max = source.DefinedOutputByType(def),
                                current = source.CurrentOutputByType(def)
                            };
                            result.Add(resurs);
                        }
                    }
                }
                return result;
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
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
            _scr = this;
            lcd_storage = new LCD(NameObj + "-LCD [storage]");
            lcd_debug = new LCD(NameObj + "-LCD-DEBUG");
            lcd_name = new LCD(NameObj + "-LCD-Name");
            bats = new Batterys(NameObj);
            //connector = new Connector(NameObj + "-Connector parking");
            reflectors_light = new ReflectorsLight(NameObj);
            reflectors_light.Off();
            cockpit = new Cockpit(NameObj + "-Cocpit [LCD]");
            cargos = new Cargos(NameObj);
            power = new Power(NameObj);
        }
        public void Save()
        {

        }
        public void Main(string argument, UpdateType updateSource)
        {
            StringBuilder values_info = new StringBuilder();
            bats.Logic(argument, updateSource);
            switch (argument)
            {
                default:
                    break;
            }
            if (updateSource == UpdateType.Update100)
            {
                bats.TextInfo();
                bats.Update();
            }
            lcd_debug.OutText(bats.TextInfo(), false);
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
            //public bool charger { get; private set; } = false;
            public InpResurs res_inp { get; private set; }
            public OutResurs res_out { get; private set; }
            public int count_work_batterys { get { return list_obj.Where(n => !((IMyTerminalBlock)n).CustomName.Contains(tag_batterys_duty)).Count(); } }
            public Batterys(string name_obj) : base(name_obj)
            {

            }
            public Batterys(string name_obj, string tag) : base(name_obj, tag)
            {

            }
            //public void init()
            //{
            //    charger = IsCharger();
            //}
            public float MaxPower()
            {
                return base.list_obj.Select(b => b.MaxStoredPower).Sum();
            }
            public float MaxOutput()
            {
                return base.list_obj.Select(b => b.MaxOutput).Sum();
            }
            public float CurrentPower()
            {
                return base.list_obj.Select(b => b.CurrentStoredPower).Sum();
            }
            public float CurrentInput()
            {
                return base.list_obj.Select(b => b.CurrentInput).Sum();
            }
            public float CurrentOutput()
            {
                return base.list_obj.Select(b => b.CurrentOutput).Sum();
            }
            public float CurrentPersent()
            {
                return base.list_obj.Select(b => b.CurrentStoredPower).Sum() / base.list_obj.Select(b => b.MaxStoredPower).Sum();
            }
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
                //charger = IsCharger();
            }
            public void Auto()
            {
                foreach (IMyBatteryBlock obj in base.list_obj)
                {
                    obj.ChargeMode = ChargeMode.Auto;
                }
                //charger = IsCharger();
            }
            public void Update()
            {
                res_inp = GetInpResurs("Electricity").FirstOrDefault();
                res_out = GetOutResurs("Electricity").FirstOrDefault();
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
                //БАТАРЕЯ: [10 - 10] [0.0MW / 0.0MW]
                //|- ЗАР:  [''''''''''''''''''''''''']-0%
                values.Append("БАТАРЕЯ: [" + Count + "] [А-" + CountAuto() + " З-" + CountCharger() + "]" + PText.GetCurrentOfMax(CurrentPower(), MaxPower(), "W") + "\n");
                values.Append("|- ЗАР:  " + PText.GetScalePersent(CurrentPower() / MaxPower(), 40) + "\n");
                if (res_inp != null) { values.Append("|- ВХ :  " + PText.GetScalePersent(res_inp.current / res_inp.max, 40) + PText.GetCurrentOfMax(res_inp.current, res_inp.max, "W") + "\n"); }
                if (res_out != null) { values.Append("|- ВЫХ:  " + PText.GetScalePersent(res_out.current / res_out.max, 40) + PText.GetCurrentOfMax(res_out.current, res_out.max, "W") + "\n"); }
                values.Append("|- CurrentInput:  " + PText.GetValueOfUnits(CurrentInput(),"W") + "\n"); 
                values.Append("|- MaxOutput:  " + PText.GetValueOfUnits(MaxOutput(),"W") + "\n");  
                values.Append("|- CurrentOutput:  " + PText.GetValueOfUnits(CurrentOutput(),"W") + "\n");                 
                return values.ToString();
            }
        }
        public class Connector : BaseTerminalBlock<IMyShipConnector>
        {
            public MyShipConnectorStatus Status { get { return base.obj.Status; } }
            public bool Connected { get { return base.obj.Status == MyShipConnectorStatus.Connected ? true : false; } }
            public bool Unconnected { get { return base.obj.Status == MyShipConnectorStatus.Unconnected ? true : false; } }
            public bool Connectable { get { return base.obj.Status == MyShipConnectorStatus.Connectable ? true : false; } }
            public Connector(string name) : base(name)
            {
                if (base.obj != null)
                {

                }
            }
            public string GetInfoStatus()
            {
                switch (base.obj.Status)
                {
                    case MyShipConnectorStatus.Connected:
                        {
                            return "ПОДКЛЮЧЕН";
                        }
                    case MyShipConnectorStatus.Connectable:
                        {
                            return "ГОТОВ";
                        }
                    case MyShipConnectorStatus.Unconnected:
                        {
                            return "НЕПОДКЛЮЧЕН";
                        }
                    default:
                        {
                            return "";
                        }
                }
            }
            public string TextInfo()
            {
                StringBuilder values = new StringBuilder();
                values.Append("КОННЕКТОР: " + (Connected ? green.ToString() : (Connectable ? yellow.ToString() : red.ToString())) + "\n");
                return values.ToString();
            }
            public void Connect()
            {
                obj.Connect();
            }
            public void Disconnect()
            {
                obj.Disconnect();
            }
            public long? getRemoteConnector()
            {
                List<IMyShipConnector> list_conn = new List<IMyShipConnector>();
                _scr.GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(list_conn);
                //return list_conn.Where(c => c.Status == MyShipConnectorStatus.Connected).Count().ToString();
                foreach (IMyShipConnector conn in list_conn.Where(c => c.Status == MyShipConnectorStatus.Connected).ToList())
                {
                    //_scr.Echo("remote_control: " + conn.DisplayNameText);
                    //if (conn.DisplayNameText.Trim() != conn.DisplayNameText.Trim() && (conn.GetPosition() - this.GetPosition()).Length() < 2) return conn.DisplayNameText;
                    if (conn.EntityId != base.obj.EntityId && (conn.GetPosition() - base.obj.GetPosition()).Length() < 2) return conn.EntityId;
                }
                return null;
            }
        }
        public class ReflectorsLight : BaseListTerminalBlock<IMyReflectorLight>
        {
            public ReflectorsLight(string name_obj) : base(name_obj)
            {
            }
            public ReflectorsLight(string name_obj, string tag) : base(name_obj, tag)
            {

            }
        }
        public class Cockpit : BaseController
        {
            public Cockpit(string name) : base(name)
            {

            }

        }
        public class Cargos
        {
            private List<IMyTerminalBlock> list = new List<IMyTerminalBlock>();
            public List<IMyTerminalBlock> cargos = new List<IMyTerminalBlock>();
            public string name_obj;
            public int MaxVolume { get; private set; }
            public int CurrentVolume { get; private set; }
            public int CurrentMass { get; private set; }
            //--
            public int FeAmount { get; private set; }
            public int CbAmount { get; private set; }
            public int NiAmount { get; private set; }
            public int MgAmount { get; private set; }
            public int AuAmount { get; private set; }
            public int AgAmount { get; private set; }
            public int PtAmount { get; private set; }
            public int SiAmount { get; private set; }
            public int UAmount { get; private set; }
            public int StoneAmount { get; private set; }
            public int IceAmount { get; private set; }
            public Cargos(string name_obj)
            {
                this.name_obj = name_obj;
                _scr.GridTerminalSystem.GetBlocksOfType(list, r => r.CustomName.Contains(name_obj));
                foreach (IMyTerminalBlock cargo in list)
                {
                    if ((cargo is IMyShipDrill) || (cargo is IMyCargoContainer) || (cargo is IMyShipConnector))
                    {
                        MaxVolume += (int)cargo.GetInventory(0).MaxVolume;
                        CurrentVolume += (int)(cargo.GetInventory(0).CurrentVolume * 1000);
                        CurrentMass += (int)cargo.GetInventory(0).CurrentMass;
                        cargos.Add(cargo);
                    }
                }
                _scr.Echo("Найдено Cargos : " + cargos.Count());
            }
            public void Update()
            {
                CurrentVolume = 0;
                CurrentMass = 0;
                FeAmount = 0;
                CbAmount = 0;
                NiAmount = 0;
                MgAmount = 0;
                AuAmount = 0;
                AgAmount = 0;
                PtAmount = 0;
                SiAmount = 0;
                UAmount = 0;
                StoneAmount = 0;
                IceAmount = 0;
                foreach (IMyTerminalBlock cargo in cargos)
                {
                    if (cargo != null)
                    {
                        CurrentVolume += (int)cargo.GetInventory(0).CurrentVolume;
                        CurrentMass += (int)cargo.GetInventory(0).CurrentMass;
                        var crateItems = new List<MyInventoryItem>();
                        cargo.GetInventory(0).GetItems(crateItems);
                        for (int j = crateItems.Count - 1; j >= 0; j--)
                        {
                            if (crateItems[j].Type.SubtypeId == "Iron")
                                FeAmount += (int)crateItems[j].Amount;
                            else if (crateItems[j].Type.SubtypeId == "Cobalt")
                                CbAmount += (int)crateItems[j].Amount;
                            else if (crateItems[j].Type.SubtypeId == "Nickel")
                                NiAmount += (int)crateItems[j].Amount;
                            else if (crateItems[j].Type.SubtypeId == "Magnesium")
                                MgAmount += (int)crateItems[j].Amount;
                            else if (crateItems[j].Type.SubtypeId == "Gold")
                                AuAmount += (int)crateItems[j].Amount;
                            else if (crateItems[j].Type.SubtypeId == "Silver")
                                AgAmount += (int)crateItems[j].Amount;
                            else if (crateItems[j].Type.SubtypeId == "Platinum")
                                PtAmount += (int)crateItems[j].Amount;
                            else if (crateItems[j].Type.SubtypeId == "Silicon")
                                SiAmount += (int)crateItems[j].Amount;
                            else if (crateItems[j].Type.SubtypeId == "Uranium")
                                UAmount += (int)crateItems[j].Amount;
                            else if (crateItems[j].Type.SubtypeId == "Stone")
                                StoneAmount += (int)crateItems[j].Amount;
                            else if (crateItems[j].Type.SubtypeId == "Ice")
                                IceAmount += (int)crateItems[j].Amount;
                        }
                    }
                }
            }
            public void UnLoad()
            {
                List<IMyCargoContainer> base_cargos = new List<IMyCargoContainer>();
                _scr.GridTerminalSystem.GetBlocksOfType(base_cargos, r => r.CustomName.Contains(this.name_obj) != true);
                foreach (IMyCargoContainer bc in base_cargos)
                {
                    var Destination = bc.GetInventory(0);
                    foreach (IMyTerminalBlock cargo in cargos)
                    {
                        var containerInv = cargo.GetInventory(0);
                        var containerItems = new List<MyInventoryItem>();
                        containerInv.GetItems(containerItems);
                        foreach (MyInventoryItem inv in containerItems)
                        {
                            containerInv.TransferItemTo(Destination, 0, null, true, null);
                        }
                    }
                }
                Update();
            }
        }
        public class PowerResurs
        {
            public IMyTerminalBlock myTerminalBlock { get; set; }
            public string TyepID { get; set; }
            public string SubtyepID { get; set; }
            public float cur_input { get; set; } = 0;
            public float max_input { get; set; } = 0;
            public bool is_power_by { get; set; } = false;
            public float max_output { get; set; } = 0;
            public float cur_output { get; set; } = 0;
        }
        public class Power
        {
            private List<IMyTerminalBlock> list = new List<IMyTerminalBlock>();
            public List<PowerResurs> power_resurses = new List<PowerResurs>();
            public string name_obj;
            public float sum_cur_input { get; private set; } = 0;
            public float sum_max_input { get; private set; } = 0;
            public float sum_max_output { get; private set; } = 0;
            public float sum_cur_output { get; private set; } = 0;
            public Power(string name_obj)
            {
                this.name_obj = name_obj;
                //_scr.Echo("Найдено power_resurses : " + power_resurses.Count());
            }
            public void Update()
            {
                power_resurses.Clear();
                sum_cur_input = 0;
                sum_max_input = 0;
                sum_max_output = 0;
                sum_cur_output = 0;
                _scr.GridTerminalSystem.GetBlocksOfType(list, r => r.CustomName.Contains(name_obj));
                foreach (IMyTerminalBlock tb in list)
                {
                    bool power_inp = false;
                    bool power_out = false;
                    float cur_input = 0;
                    float max_input = 0;
                    bool is_power_by = false;
                    float max_output = 0;
                    float cur_output = 0;
                    // Потребляемый ресурсы
                    MyResourceSinkComponent sink;
                    tb.Components.TryGet<MyResourceSinkComponent>(out sink);
                    if (sink != null)
                    {
                        var list = sink.AcceptedResources;
                        foreach (MyDefinitionId def in list)
                        {
                            is_power_by = false;
                            if (def.SubtypeId.ToString() == "Electricity")
                                power_inp = true;
                            cur_input = sink.CurrentInputByType(def);
                            is_power_by = sink.IsPoweredByType(def);
                            max_input = sink.MaxRequiredInputByType(def);
                        }
                    }
                    // Выдает ресурсы
                    MyResourceSourceComponent source;
                    tb.Components.TryGet<MyResourceSourceComponent>(out source);
                    if (source != null)
                    {
                        var list = source.ResourceTypes;
                        foreach (MyDefinitionId def in list)
                        {
                            if (def.SubtypeId.ToString() == "Electricity")
                                power_out = true;
                            max_output = source.DefinedOutputByType(def);
                            cur_output = source.CurrentOutputByType(def);

                        }
                    }
                    if (power_inp || power_out)
                    {
                        if (power_inp)
                        {
                            sum_cur_input += cur_input;
                            sum_max_input += max_input;

                        }
                        if (power_out)
                        {
                            sum_max_output += max_output;
                            sum_cur_output += cur_output;
                        }
                        PowerResurs pr = new PowerResurs()
                        {
                            myTerminalBlock = tb,
                            TyepID = tb.BlockDefinition.TypeIdString,
                            SubtyepID = tb.BlockDefinition.SubtypeId,
                            is_power_by = power_inp ? is_power_by : false,
                            cur_input = power_inp ? cur_input : 0,
                            max_input = power_inp ? max_input : 0,
                            cur_output = power_out ? cur_output : 0,
                            max_output = power_out ? max_output : 0,
                        };
                        power_resurses.Add(pr);
                    }
                }
            }
            public string TextInfo()
            {
                StringBuilder values = new StringBuilder();
                values.Append("ВЫХОД       : " + PText.GetCurrentOfMax(sum_cur_output, sum_max_output, "MW") + "\n");
                values.Append("ПОТРЕБЛЕНИЕ : " + PText.GetCurrentOfMax(sum_cur_input, sum_max_input, "MW") + "\n");

                foreach (PowerResurs pr in power_resurses)
                {
                    values.Append("------------------------------- \n");
                    values.Append(pr.TyepID + ", " + pr.SubtyepID + "\n");
                    values.Append("max_input : " + pr.max_input + ", cur_input : " + pr.cur_input + ", is_power_by : " + pr.is_power_by + "\n");
                    values.Append("max_output : " + pr.max_output + ", cur_output : " + pr.cur_output + "\n");
                }
                return values.ToString();
            }
        }
    }
}
// Накопленно : CurrentPower() / MaxPower()
// Зарядка : CurrentInput / MaxInput 

//Батареи:       (IN 4.4 MW / OUT 88.5 kW) CurrentInput/CurrentOutput

//  Накоплено: 45 MWh / 48 MWh
//[|||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||| ''''']  93.7%[16] [А-16 З-0][ 45MW / 48MW]

//  Потребление: 88.5 kW / 192 MW    CurrentOutput/MaxOutput
//[''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''']   0.0%

//  Зарядка: 4.4 MW / 192 MW     CurrentInput/
//[| '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''']    2.3 %
//Общее потребление: 4.4 MW / 196.4 MW
//[| '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''']   2.2 %
//Время зарядки: 42m 32s

//|- ЗАРЯД: [16] [ 45MW / 48MW]
//|  [||||||||||||||||||||||||||||||||||||||''] -93.8 %
//|- IN   : [0] [ 4.4 MW / 192 MW ]
//|  [||||||||||||||||||||||||||||||||||||||''] -93.8 %
//|- OUT  : [0] [ 88.5 kW / 192 MW ]
//|  [||||||||||||||||||||||||||||||||||||||''] -93.8 %


//| -ВХ :  [|'''''''''''''''''''''''''''''''''''''''] - 2.3%[ 0.3MW / 12MW ]
//|- ВЫХ:  [''''''''''''''''''''''''''''''''''''''''] -0 %[5.5kW / 12MW]
//| -CurrentInput:  4.4MW
//| -MaxOutput:  192MW
//| -CurrentOutput:  88.5kW
