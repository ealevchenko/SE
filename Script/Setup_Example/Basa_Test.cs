using Newtonsoft.Json.Linq;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.GameSystems;
using Sandbox.Game.World;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI.Ingame;
using VRage.Scripting;
using VRageMath;
using static VRage.Game.MyObjectBuilder_CurveDefinition;

/* Скрипт тестирования на базе классов обмен сообщений и определения портов посадки
        tag_antena = "[antena]" - на базе пометить программный блок

        tag_port= "[port]"; - на базе пометить коннекторы парковки ....
        type_base = 1;      - указать тип бахы
 */
namespace Basa_Test
{
    public sealed class Program : MyGridProgram
    {
        string NameObj = "[BT]";
        static string tag_antena = "[antena]";
        static string tag_port = "[port]";
        static int type_base = 1; // планетарная

        const char igreen = '\uE001';
        const char iblue = '\uE002';
        const char ired = '\uE003';
        const char iyellow = '\uE004';
        const char idarkGrey = '\uE00F';

        static MyStorage strg;
        static LCD lcd_storage;
        static LCD lcd_info, lcd_debug;
        static LCD lcd_lstr;
        static MessHandler mess_handler;
        static Program _scr;

        public class PText
        {
            static public string GetPersent(double perse) { return " - " + Math.Round((perse * 100), 1) + "%"; }
            static public string GetScalePersent(double perse, int scale) { string prog = "["; for (int i = 0; i < Math.Round((perse * scale), 0); i++) { prog += "|"; } for (int i = 0; i < scale - Math.Round((perse * scale), 0); i++) { prog += "'"; } prog += "]" + GetPersent(perse); return prog; }
            static public string GetCurrentOfMax(float cur, float max, string units) { return "[ " + Math.Round(cur, 1) + units + " / " + Math.Round(max, 1) + units + " ]"; }
            static public string GetCurrentOfMinMax(float min, float cur, float max, string units) { return "[ " + Math.Round(min, 1) + units + " / " + Math.Round(cur, 1) + units + " / " + Math.Round(max, 1) + units + " ]"; }
            static public string GetThrust(float value) { return Math.Round(value / 1000000, 1) + "МН"; }
            static public string GetFarm(float value) { return Math.Round(value, 1) + "L"; }
            static public string GetGPS(string name, Vector3D target) { return "GPS:" + name + ":" + target.GetDim(0) + ":" + target.GetDim(1) + ":" + target.GetDim(2) + ":\n"; }
            static public string GetGPSMatrixD(string name, MatrixD target) { return "MatrixD:" + name + "\n" + "M11:" + target.M11 + "M12:" + target.M12 + "M13:" + target.M13 + "M14:" + target.M14 + ":\n" + "M21:" + target.M21 + "M22:" + target.M22 + "M23:" + target.M23 + "M24:" + target.M24 + ":\n" + "M31:" + target.M31 + "M32:" + target.M32 + "M33:" + target.M33 + "M34:" + target.M34 + ":\n" + "M41:" + target.M41 + "M42:" + target.M42 + "M43:" + target.M43 + "M44:" + target.M44 + ":\n"; }
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
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
            _scr = this;
            lcd_storage = new LCD(NameObj + "-LCD [storage]");
            lcd_info = new LCD(NameObj + "-LCD-INFO");
            lcd_debug = new LCD(NameObj + "-LCD-DEBUG");
            lcd_lstr = new LCD(NameObj + "-LCD-Listener");
            mess_handler = new MessHandler(NameObj);
            strg = new MyStorage();
            strg.LoadFromStorage();
        }
        void Main(string argument, UpdateType updateSource)
        {
            mess_handler.Logic(argument, updateSource); // обработаем сообщенияя
            //upr.Logic(argument, updateSource);
        }
        public class LCD : BaseTerminalBlock<IMyTextPanel>
        {
            public LCD(string name) : base(name) { if (base.obj != null) { base.obj.SetValue("Content", (Int64)1); } }
            public void OutText(StringBuilder values) { if (base.obj != null) { base.obj.WriteText(values, false); } }
            public void OutText(string text, bool append) { if (base.obj != null) { base.obj.WriteText(text, append); } }
            public StringBuilder GetText() { StringBuilder values = new StringBuilder(); if (base.obj != null) { base.obj.ReadText(values); } return values; }
        }
        public class Connector : BaseTerminalBlock<IMyShipConnector>
        {
            public MyShipConnectorStatus Status { get { return base.obj.Status; } }
            public bool Connected { get { return base.obj.Status == MyShipConnectorStatus.Connected ? true : false; } }
            public bool Unconnected { get { return base.obj.Status == MyShipConnectorStatus.Unconnected ? true : false; } }
            public bool Connectable { get { return base.obj.Status == MyShipConnectorStatus.Connectable ? true : false; } }
            public Connector(string name) : base(name) { if (base.obj != null) { } }
            public string TextStatus() { StringBuilder values = new StringBuilder(); values.Append(Connected ? igreen.ToString() : (Connectable ? iyellow.ToString() : ired.ToString())); return values.ToString(); }
            public string TextInfo(string name) { StringBuilder values = new StringBuilder(); values.Append((name != null ? name : "КОННЕКТОР") + " : " + TextStatus()); return values.ToString(); }
            public void Connect() { obj.Connect(); }
            public void Disconnect() { obj.Disconnect(); }
            public long? getEntityIdRemoteConnector() { List<IMyShipConnector> list_conn = new List<IMyShipConnector>(); _scr.GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(list_conn); foreach (IMyShipConnector conn in list_conn.Where(c => c.Status == MyShipConnectorStatus.Connected).ToList()) { if (conn.EntityId != base.obj.EntityId && (conn.GetPosition() - base.obj.GetPosition()).Length() < 3) return conn.EntityId; } return null; }
            public IMyShipConnector getRemoteConnector() { List<IMyShipConnector> list_conn = new List<IMyShipConnector>(); _scr.GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(list_conn); foreach (IMyShipConnector conn in list_conn.Where(c => c.Status == MyShipConnectorStatus.Connected).ToList()) { if (conn.EntityId != base.obj.EntityId && (conn.GetPosition() - base.obj.GetPosition()).Length() < 3) return conn; } return null; }
        }
        public class Cockpit : BaseController { public Cockpit(string name) : base(name) { } }
        public class Ports
        {
            public Ports()
            {

            }
        }
        public class MessHandler
        {
            public string name_base { get; set; }
            public enum type_ship : int
            {
                none = 0,
                v_miner = 1,    // верт. буровик
                h_miner = 2,    // гор. буровик
                welder = 3,     // сварщик
                cutter = 4,     // резак
                truck = 5,      // грузовик
                rocket = 6,     // ракето-носитель
                scout = 7,      // разветчик
                corvette = 8,   // корвет
                drone = 9,      // дрон
            };
            public static string[] name_type_ship = { "?", "В-Буровик", "Г-Буровик", "Сварщик", "Резак", "Грузовик", "Ракетоноситель", "Разведчик", "Корвет", "Дрон" };
            public class ships
            {
                public string name { get; set; }
                public long addr { get; set; }
                public type_ship type { get; set; }
                public string thruster { get; set; }
            }

            public List<ships> list_ships = new List<ships>();

            public IMyRadioAntenna antenna;
            public IMyProgrammableBlock pb;
            public IMyUnicastListener base_lstr; // Одноадресный прослушиватель базы
            public MyIGCMessage message;
            public long pb_address { get; set; }
            public MessHandler(string name)
            {
                name_base = name;
                base_lstr = _scr.IGC.UnicastListener;
                List<IMyRadioAntenna> list_anten = new List<IMyRadioAntenna>();
                List<IMyProgrammableBlock> list_pb = new List<IMyProgrammableBlock>();
                _scr.GridTerminalSystem.GetBlocksOfType<IMyRadioAntenna>(list_anten, r => r.CustomName.Contains(name));
                _scr.Echo("MessHandler : Найдено IMyRadioAntenna - " + list_anten.Count());
                _scr.GridTerminalSystem.GetBlocksOfType<IMyProgrammableBlock>(list_pb, r => r.CustomName.Contains(name));
                pb = list_pb.Where(n => ((IMyTerminalBlock)n).CustomName.Contains(tag_antena)).FirstOrDefault();
                _scr.Echo("MessHandler : IMyProgrammableBlock " + tag_antena + " - " + ((pb != null) ? ("Найден") : ("Ошибка")));
                pb_address = pb != null ? pb.EntityId : 0;
                //strg.SaveToStorage();
            }
            public bool Registration(String str, long addr)
            {
                string name = strg.GetValString("name", str);
                type_ship type = (type_ship)strg.GetValInt("type", str);
                string thruster = strg.GetValString("thruster", str);
                ships sh = list_ships.Where(s => s.addr == addr).FirstOrDefault();
                if (sh == null)
                {
                    sh = new ships()
                    {
                        name = name,
                        type = type,
                        addr = addr,
                        thruster = thruster,
                    };
                    list_ships.Add(sh);
                }
                else
                {
                    sh.name = name; sh.type = type; sh.thruster = thruster;
                }
                strg.SaveToStorage();
                return true;
            }
            public void Logic(string argument, UpdateType updateSource)
            {
                switch (argument)
                {
                    case "test": _scr.IGC.SendUnicastMessage<string>(pb_address, "tag", argument); break;
                    default: break;
                }
                if (updateSource == UpdateType.Update100)
                {
                    if (base_lstr.HasPendingMessage)
                    {
                        message = base_lstr.AcceptMessage();
                        StringBuilder values = new StringBuilder();
                        string mess_inp = Convert.ToString(message.Data);
                        string mess_tag = Convert.ToString(message.Tag);
                        string mess_source = Convert.ToString(message.Source);
                        long addr = !String.IsNullOrWhiteSpace(mess_source) ? Convert.ToInt64(mess_source) : 0;
                        values.Append("Data   : " + mess_inp + "\n");
                        values.Append("Tag    : " + mess_tag + "\n");
                        values.Append("Source : " + mess_source + "\n");
                        lcd_lstr.OutText(values);
                        string[] args = mess_inp.Split('=');
                        if (args.Count() > 0)
                        {
                            switch (args[0])
                            {
                                case "upd_ship":
                                    {
                                        bool res = Registration(args[1], addr);
                                        if (res)
                                        {
                                            string response = String.Format("upd_base=name:{0};type:{1};", name_base, type_base);
                                            _scr.IGC.SendUnicastMessage<string>(addr, name_base, response);
                                        }
                                        break;
                                    };
                            }
                        }
                    }
                }
            }
        }
        public class MyStorage
        {
            public MyStorage() { }
            public void LoadFromStorage()
            {
                StringBuilder str = lcd_storage.GetText();
                int count = GetValInt("count_ships", str.ToString());
                mess_handler.list_ships.Clear();
                for (int i = 0; i < count; i++)
                {
                    MessHandler.ships ships = new MessHandler.ships()
                    {
                        name = GetValString("ship[" + i + "].name", str.ToString()),
                        addr = GetValInt64("ship[" + i + "].addr", str.ToString()),
                        type = (MessHandler.type_ship)GetValInt("ship[" + i + "].type", str.ToString()),
                        thruster = GetValString("ship[" + i + "].thruster", str.ToString()),
                    };
                }
            }
            public void SaveToStorage()
            {
                StringBuilder values = new StringBuilder();

                int i = 0;
                foreach (MessHandler.ships shp in mess_handler.list_ships)
                {
                    values.Append("ship[" + i + "].name: " + shp.name + ";\n");
                    values.Append("ship[" + i + "].addr: " + shp.addr.ToString() + ";\n");
                    values.Append("ship[" + i + "].type: " + ((int)shp.type).ToString() + ";\n");
                    values.Append("ship[" + i + "].thruster: " + shp.thruster + ";\n");
                    i++;
                }
                values.Append("count_ships: " + mess_handler.list_ships.Count().ToString() + ";\n");
                lcd_storage.OutText(values);
            }
            private string GetVal(string Key, string str, string val) { string pattern = @"(" + Key + "):([^:^;]+);"; System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(str.Replace("\n", ""), pattern); if (match.Success) { val = match.Groups[2].Value; } return val; }
            public string GetValString(string Key, string str) { return GetVal(Key, str, ""); }
            public double GetValDouble(string Key, string str) { return Convert.ToDouble(GetVal(Key, str, "0")); }
            public int GetValInt(string Key, string str) { return Convert.ToInt32(GetVal(Key, str, "0")); }
            public long GetValInt64(string Key, string str) { return Convert.ToInt64(GetVal(Key, str, "0")); }
            public bool GetValBool(string Key, string str) { return Convert.ToBoolean(GetVal(Key, str, "False")); }
            public MatrixD GetValMatrixD(string Key, string str)
            {
                return new MatrixD(GetValDouble(Key + "11", str.ToString()), GetValDouble(Key + "12", str.ToString()), GetValDouble(Key + "13", str.ToString()), GetValDouble(Key + "14", str.ToString()),
                GetValDouble(Key + "21", str.ToString()), GetValDouble(Key + "22", str.ToString()), GetValDouble(Key + "23", str.ToString()), GetValDouble(Key + "24", str.ToString()),
                GetValDouble(Key + "31", str.ToString()), GetValDouble(Key + "32", str.ToString()), GetValDouble(Key + "33", str.ToString()), GetValDouble(Key + "34", str.ToString()),
                GetValDouble(Key + "41", str.ToString()), GetValDouble(Key + "42", str.ToString()), GetValDouble(Key + "43", str.ToString()), GetValDouble(Key + "44", str.ToString()));
            }
            public Vector3D GetValVector3D(string Key, string str) { return new Vector3D(GetValDouble(Key + "X", str.ToString()), GetValDouble(Key + "Y", str.ToString()), GetValDouble(Key + "Z", str.ToString())); }
            public string SetValVector3D(string Key, Vector3D val) { return val.ToString().Replace("}", "").Replace("{", "").Replace(" ", " ").Replace(" ", ";\n").Replace("X", Key + "X").Replace("Y", Key + "Y").Replace("Z", Key + "Z"); }
            public string SetValMatrixD(string Key, MatrixD val) { return val.ToString().Replace("}", "").Replace("{", "").Replace(" ", " ").Replace(" ", ";\n").Replace("M", Key + "M"); }
        }
    }
}
